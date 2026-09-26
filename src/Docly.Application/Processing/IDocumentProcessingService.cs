using Docly.Domain.Documents;

namespace Docly.Application.Processing;

public sealed record DocumentProcessingStatusDto(
    Guid DocumentId,
    DocumentProcessingStatus Status,
    string? ErrorMessage,
    int ChunkCount,
    DateTime? StartedAtUtc,
    DateTime? CompletedAtUtc);

/// <summary>
/// Orchestrates the extraction + chunking + embedding pipeline for a document's current file, and tracks
/// its progress in a <see cref="DocumentProcessingJob"/>.
/// </summary>
public interface IDocumentProcessingService
{
    /// <summary>
    /// Extracts, chunks and embeds the document's current file, replacing any previous chunks. Never throws:
    /// failures are recorded on the job (<see cref="DocumentProcessingStatus.Failed"/>) so the caller
    /// (upload/replace) can still succeed even if processing didn't.
    /// </summary>
    Task ProcessAsync(Guid documentId, CancellationToken cancellationToken = default);

    Task<DocumentProcessingStatusDto?> GetStatusAsync(Guid documentId, CancellationToken cancellationToken = default);
}
