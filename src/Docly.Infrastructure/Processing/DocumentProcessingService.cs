using System.Globalization;
using Docly.Application.Embeddings;
using Docly.Application.Notifications;
using Docly.Application.Processing;
using Docly.Application.Settings;
using Docly.Application.Storage;
using Docly.Domain.Documents;
using Docly.Domain.Notifications;
using Docly.Domain.Settings;
using Docly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Docly.Infrastructure.Processing;

public class DocumentProcessingService : IDocumentProcessingService
{
    private readonly DoclyDbContext _db;
    private readonly IFileStorageService _storage;
    private readonly ITextExtractionService _textExtraction;
    private readonly ISystemSettingService _settings;
    private readonly IEmbeddingService _embedding;
    private readonly INotificationService _notifications;

    public DocumentProcessingService(
        DoclyDbContext db, IFileStorageService storage, ITextExtractionService textExtraction,
        ISystemSettingService settings, IEmbeddingService embedding, INotificationService notifications)
    {
        _db = db;
        _storage = storage;
        _textExtraction = textExtraction;
        _settings = settings;
        _embedding = embedding;
        _notifications = notifications;
    }

    public async Task ProcessAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var file = await _db.DocumentFiles.AsNoTracking()
            .FirstOrDefaultAsync(f => f.DocumentId == documentId, cancellationToken);
        if (file is null)
        {
            return;
        }

        var document = await _db.Documents.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken);
        if (document is null)
        {
            return;
        }

        var job = await _db.DocumentProcessingJobs.FirstOrDefaultAsync(j => j.DocumentId == documentId, cancellationToken);
        if (job is null)
        {
            job = new DocumentProcessingJob { Id = Guid.NewGuid(), DocumentId = documentId, CreatedAtUtc = DateTime.UtcNow };
            _db.DocumentProcessingJobs.Add(job);
        }

        job.Status = DocumentProcessingStatus.ExtractingText;
        job.ErrorMessage = null;
        job.StartedAtUtc = DateTime.UtcNow;
        job.CompletedAtUtc = null;
        await _db.SaveChangesAsync(cancellationToken);

        try
        {
            await using var content = await _storage.DownloadAsync(file.StoragePath, cancellationToken);
            var markdown = await _textExtraction.ExtractMarkdownAsync(content, file.ContentType, cancellationToken);

            job.Status = DocumentProcessingStatus.Chunking;
            await _db.SaveChangesAsync(cancellationToken);

            var settings = await _settings.GetAllAsync(cancellationToken);
            var chunks = TextChunker.Chunk(
                markdown, GetInt(settings, SystemSettingKeys.RagChunkSize), GetInt(settings, SystemSettingKeys.RagChunkOverlap));

            var existingChunks = _db.KnowledgeChunks.Where(c => c.DocumentId == documentId);
            _db.KnowledgeChunks.RemoveRange(existingChunks);

            var now = DateTime.UtcNow;
            var newChunks = new List<KnowledgeChunk>(chunks.Count);
            for (var index = 0; index < chunks.Count; index++)
            {
                newChunks.Add(new KnowledgeChunk
                {
                    Id = Guid.NewGuid(),
                    DocumentId = documentId,
                    ChunkIndex = index,
                    Content = chunks[index],
                    CharCount = chunks[index].Length,
                    CreatedAtUtc = now,
                });
            }

            _db.KnowledgeChunks.AddRange(newChunks);

            // The chunks are saved before embedding, so a provider failure leaves the text indexed
            // (browsable in the knowledge base) and only the vectors missing.
            job.Status = DocumentProcessingStatus.Embedding;
            job.ChunkCount = newChunks.Count;
            await _db.SaveChangesAsync(cancellationToken);

            await EmbedChunksAsync(newChunks, GetInt(settings, SystemSettingKeys.RagEmbeddingBatchSize), cancellationToken);

            job.Status = DocumentProcessingStatus.Completed;
            job.CompletedAtUtc = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            await _notifications.CreateAsync(
                document.OwnerUserId, NotificationTypes.DocumentProcessingCompleted, "Documento procesado",
                $"El documento \"{document.Title}\" se procesó correctamente y ya está disponible en la base de conocimiento.",
                $"/Documents/Edit/{documentId}", cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            job.Status = DocumentProcessingStatus.Failed;
            job.ErrorMessage = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;

            await _db.SaveChangesAsync(CancellationToken.None);

            await _notifications.CreateAsync(
                document.OwnerUserId, NotificationTypes.DocumentProcessingFailed, "Error al procesar documento",
                $"El documento \"{document.Title}\" no pudo procesarse: {job.ErrorMessage}",
                $"/Documents/Edit/{documentId}", CancellationToken.None);
        }
    }

    public async Task<DocumentProcessingStatusDto?> GetStatusAsync(
        Guid documentId, CancellationToken cancellationToken = default)
    {
        var job = await _db.DocumentProcessingJobs.AsNoTracking()
            .FirstOrDefaultAsync(j => j.DocumentId == documentId, cancellationToken);

        return job is null
            ? null
            : new DocumentProcessingStatusDto(
                job.DocumentId, job.Status, job.ErrorMessage, job.ChunkCount, job.StartedAtUtc, job.CompletedAtUtc);
    }

    /// <summary>Embeds the chunks in batches, saving after each one so completed batches survive a later failure.</summary>
    private async Task EmbedChunksAsync(
        IReadOnlyList<KnowledgeChunk> chunks, int batchSize, CancellationToken cancellationToken)
    {
        foreach (var batch in chunks.Chunk(batchSize))
        {
            var vectors = await _embedding.EmbedDocumentsAsync(
                batch.Select(c => c.Content).ToList(), cancellationToken);

            for (var i = 0; i < batch.Length; i++)
            {
                batch[i].Embedding = vectors[i];
            }

            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    private static int GetInt(IReadOnlyList<SystemSettingDto> settings, string key) =>
        int.Parse(settings.First(s => s.Key == key).Value, CultureInfo.InvariantCulture);
}
