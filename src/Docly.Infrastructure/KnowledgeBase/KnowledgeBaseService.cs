using Docly.Application.Common;
using Docly.Application.KnowledgeBase;
using Docly.Domain.Documents;
using Docly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Docly.Infrastructure.KnowledgeBase;

public class KnowledgeBaseService : IKnowledgeBaseService
{
    private readonly DoclyDbContext _db;

    public KnowledgeBaseService(DoclyDbContext db)
    {
        _db = db;
    }

    public async Task<KnowledgeBaseStatsDto> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        // Sequential on purpose: a DbContext doesn't allow concurrent operations.
        var documentCount = await _db.Documents.CountAsync(cancellationToken);
        var indexedDocumentCount = await _db.DocumentProcessingJobs
            .CountAsync(j => j.Status == DocumentProcessingStatus.Completed, cancellationToken);
        var chunkCount = await _db.KnowledgeChunks.CountAsync(cancellationToken);
        var embeddedChunkCount = await _db.KnowledgeChunks.CountAsync(c => c.Embedding != null, cancellationToken);

        return new KnowledgeBaseStatsDto(documentCount, indexedDocumentCount, chunkCount, embeddedChunkCount);
    }

    public async Task<PagedResult<KnowledgeBaseDocumentDto>> GetDocumentsAsync(
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var totalCount = await _db.Documents.CountAsync(cancellationToken);

        var items = await (
            from d in _db.Documents.AsNoTracking()
            join c in _db.Categories.AsNoTracking() on d.CategoryId equals c.Id
            join j in _db.DocumentProcessingJobs.AsNoTracking() on d.Id equals j.DocumentId into jobs
            from job in jobs.DefaultIfEmpty()
            orderby d.UpdatedAtUtc descending
            select new KnowledgeBaseDocumentDto(
                d.Id,
                d.Title,
                c.Name,
                job == null ? null : job.Status,
                _db.KnowledgeChunks.Count(k => k.DocumentId == d.Id),
                _db.KnowledgeChunks.Count(k => k.DocumentId == d.Id && k.Embedding != null),
                job == null ? null : job.CompletedAtUtc))
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<KnowledgeBaseDocumentDto>(items, page, pageSize, totalCount);
    }

    public async Task<DocumentChunksDto?> GetDocumentChunksAsync(
        Guid documentId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var title = await _db.Documents.AsNoTracking()
            .Where(d => d.Id == documentId)
            .Select(d => d.Title)
            .FirstOrDefaultAsync(cancellationToken);

        if (title is null)
        {
            return null;
        }

        var chunks = _db.KnowledgeChunks.AsNoTracking().Where(c => c.DocumentId == documentId);

        var totalCount = await chunks.CountAsync(cancellationToken);
        var embeddedCount = await chunks.CountAsync(c => c.Embedding != null, cancellationToken);

        // HasEmbedding is computed in SQL so the 768-float vectors are never loaded just to be listed.
        var items = await chunks
            .OrderBy(c => c.ChunkIndex)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new KnowledgeChunkDto(c.Id, c.ChunkIndex, c.Content, c.CharCount, c.Embedding != null))
            .ToListAsync(cancellationToken);

        return new DocumentChunksDto(
            documentId, title, embeddedCount, new PagedResult<KnowledgeChunkDto>(items, page, pageSize, totalCount));
    }
}
