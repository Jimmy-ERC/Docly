using Docly.Application.Common;
using Docly.Domain.Documents;

namespace Docly.Application.KnowledgeBase;

public sealed record KnowledgeBaseStatsDto(
    int DocumentCount,
    int IndexedDocumentCount,
    int ChunkCount,
    int EmbeddedChunkCount);

public sealed record KnowledgeBaseDocumentDto(
    Guid DocumentId,
    string Title,
    string CategoryName,
    DocumentProcessingStatus? Status,
    int ChunkCount,
    int EmbeddedChunkCount,
    DateTime? IndexedAtUtc);

public sealed record KnowledgeChunkDto(Guid Id, int ChunkIndex, string Content, int CharCount, bool HasEmbedding);

public sealed record DocumentChunksDto(
    Guid DocumentId, string Title, int EmbeddedChunkCount, PagedResult<KnowledgeChunkDto> Chunks);

/// <summary>Read-only view over what the RAG pipeline has indexed.</summary>
public interface IKnowledgeBaseService
{
    Task<KnowledgeBaseStatsDto> GetStatsAsync(CancellationToken cancellationToken = default);

    Task<PagedResult<KnowledgeBaseDocumentDto>> GetDocumentsAsync(
        int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>Returns null when the document doesn't exist.</summary>
    Task<DocumentChunksDto?> GetDocumentChunksAsync(
        Guid documentId, int page, int pageSize, CancellationToken cancellationToken = default);
}
