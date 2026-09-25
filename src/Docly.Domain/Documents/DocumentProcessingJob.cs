namespace Docly.Domain.Documents;

/// <summary>
/// Tracks the extraction + chunking progress of a <see cref="Document"/>'s current file, so it can be
/// reported in the UI and re-run on demand. One row per document — reprocessing (a file replacement or
/// a manual retry) resets this same row instead of accumulating history.
/// </summary>
public class DocumentProcessingJob
{
    public Guid Id { get; set; }

    public Guid DocumentId { get; set; }

    public DocumentProcessingStatus Status { get; set; }

    /// <summary>Set only when <see cref="Status"/> is <see cref="DocumentProcessingStatus.Failed"/>.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Chunks produced by the last successful run.</summary>
    public int ChunkCount { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}
