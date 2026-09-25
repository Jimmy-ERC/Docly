using System.Globalization;
using Docly.Application.Processing;
using Docly.Application.Settings;
using Docly.Application.Storage;
using Docly.Domain.Documents;
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

    public DocumentProcessingService(
        DoclyDbContext db, IFileStorageService storage, ITextExtractionService textExtraction,
        ISystemSettingService settings)
    {
        _db = db;
        _storage = storage;
        _textExtraction = textExtraction;
        _settings = settings;
    }

    public async Task ProcessAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var file = await _db.DocumentFiles.AsNoTracking()
            .FirstOrDefaultAsync(f => f.DocumentId == documentId, cancellationToken);
        if (file is null)
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

            var (chunkSize, overlap) = await GetChunkSettingsAsync(cancellationToken);
            var chunks = TextChunker.Chunk(markdown, chunkSize, overlap);

            var existingChunks = _db.KnowledgeChunks.Where(c => c.DocumentId == documentId);
            _db.KnowledgeChunks.RemoveRange(existingChunks);

            var now = DateTime.UtcNow;
            for (var index = 0; index < chunks.Count; index++)
            {
                _db.KnowledgeChunks.Add(new KnowledgeChunk
                {
                    Id = Guid.NewGuid(),
                    DocumentId = documentId,
                    ChunkIndex = index,
                    Content = chunks[index],
                    CharCount = chunks[index].Length,
                    CreatedAtUtc = now,
                });
            }

            job.Status = DocumentProcessingStatus.Completed;
            job.ChunkCount = chunks.Count;
            job.CompletedAtUtc = now;

            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            job.Status = DocumentProcessingStatus.Failed;
            job.ErrorMessage = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;

            await _db.SaveChangesAsync(CancellationToken.None);
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

    private async Task<(int ChunkSize, int Overlap)> GetChunkSettingsAsync(CancellationToken cancellationToken)
    {
        var settings = await _settings.GetAllAsync(cancellationToken);

        var chunkSize = ParseInt(settings, SystemSettingKeys.RagChunkSize);
        var overlap = ParseInt(settings, SystemSettingKeys.RagChunkOverlap);

        return (chunkSize, overlap);
    }

    private static int ParseInt(IReadOnlyList<SystemSettingDto> settings, string key) =>
        int.Parse(settings.First(s => s.Key == key).Value, CultureInfo.InvariantCulture);
}
