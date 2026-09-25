namespace Docly.Domain.Documents;

/// <summary>
/// A fragment produced by chunking a document's extracted text. The embedding vector is added by
/// `feature/base-conocimiento-embeddings`; until then this only carries the chunking output.
/// </summary>
public class KnowledgeChunk
{
    public Guid Id { get; set; }

    public Guid DocumentId { get; set; }

    /// <summary>Zero-based position among the chunks of the same document.</summary>
    public int ChunkIndex { get; set; }

    public string Content { get; set; } = string.Empty;

    public int CharCount { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
