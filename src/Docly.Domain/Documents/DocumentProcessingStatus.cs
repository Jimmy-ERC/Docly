namespace Docly.Domain.Documents;

/// <summary>
/// Progress of the extraction + chunking + embedding pipeline for a <see cref="Document"/>'s current file.
/// Persisted as an integer: append new values at the end so existing rows keep their meaning.
/// </summary>
public enum DocumentProcessingStatus
{
    Pending,
    ExtractingText,
    Chunking,
    Completed,
    Failed,
    Embedding,
}
