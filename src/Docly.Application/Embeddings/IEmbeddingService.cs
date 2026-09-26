namespace Docly.Application.Embeddings;

/// <summary>
/// Fixed embedding contract for the whole knowledge base. Vectors produced with a different model or
/// dimensionality are not comparable, so these are constants rather than settings: changing them means
/// re-embedding every chunk.
/// </summary>
public static class EmbeddingModelInfo
{
    public const string ModelName = "gemini-embedding-001";

    /// <summary>Requested `outputDimensionality`. pgvector cannot index more than 2000 dimensions.</summary>
    public const int Dimensions = 768;
}

/// <summary>Turns text into embedding vectors of <see cref="EmbeddingModelInfo.Dimensions"/> dimensions.</summary>
public interface IEmbeddingService
{
    /// <summary>
    /// Embeds document fragments to be indexed. Returns one L2-normalized vector per text, in the same order.
    /// Throws if the provider keeps failing after its retries.
    /// </summary>
    Task<IReadOnlyList<float[]>> EmbedDocumentsAsync(
        IReadOnlyList<string> texts, CancellationToken cancellationToken = default);

    /// <summary>Embeds a user question to be compared against the indexed fragments.</summary>
    Task<float[]> EmbedQueryAsync(string text, CancellationToken cancellationToken = default);
}
