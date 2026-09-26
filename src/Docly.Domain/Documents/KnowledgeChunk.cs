namespace Docly.Domain.Documents;

/// <summary>
/// A fragment produced by chunking a document's extracted text, plus its embedding vector once the
/// pipeline has embedded it.
/// </summary>
public class KnowledgeChunk
{
    public Guid Id { get; set; }

    public Guid DocumentId { get; set; }

    /// <summary>Zero-based position among the chunks of the same document.</summary>
    public int ChunkIndex { get; set; }

    public string Content { get; set; } = string.Empty;

    public int CharCount { get; set; }

    /// <summary>
    /// L2-normalized embedding of <see cref="Content"/> (768 dimensions, stored as pgvector `vector(768)`).
    /// Null until embedded — a chunk whose embedding failed is kept but is invisible to semantic search.
    /// A plain array keeps Domain free of the pgvector package; Infrastructure maps it to a vector column.
    /// </summary>
    public float[]? Embedding { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
