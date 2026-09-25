namespace Docly.Domain.Documents;

/// <summary>Progress of the extraction + chunking pipeline for a <see cref="Document"/>'s current file.</summary>
public enum DocumentProcessingStatus
{
    Pending,
    ExtractingText,
    Chunking,
    Completed,
    Failed,
}
