namespace Docly.Domain.Settings;

/// <summary>
/// Setting keys used across the app. Add new keys here (and to <see cref="SystemSettingCatalog"/>)
/// when a module needs a configurable value.
/// </summary>
public static class SystemSettingKeys
{
    /// <summary>Target size, in characters, of each chunk produced when splitting a document.</summary>
    public const string RagChunkSize = "rag.chunk_size";

    /// <summary>How many characters consecutive chunks overlap by, to avoid cutting context mid-idea.</summary>
    public const string RagChunkOverlap = "rag.chunk_overlap";

    /// <summary>How many chunks the hybrid search retrieves before assembling the prompt.</summary>
    public const string RagTopK = "rag.top_k";

    /// <summary>Minimum cosine similarity (0-1) a chunk needs to be considered relevant.</summary>
    public const string RagSimilarityThreshold = "rag.similarity_threshold";

    /// <summary>How many chunks are sent to the embedding provider per request while indexing a document.</summary>
    public const string RagEmbeddingBatchSize = "rag.embedding_batch_size";

    /// <summary>Maximum upload size, in megabytes, enforced by the application (in addition to the bucket limit).</summary>
    public const string DocumentsMaxUploadSizeMb = "documents.max_upload_size_mb";
}
