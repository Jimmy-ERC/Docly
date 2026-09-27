using Docly.Application.Common;

namespace Docly.Application.Search;

/// <summary>
/// One <see cref="Documents.KnowledgeChunk"/> that matched a hybrid search, with enough context to render
/// it as a citation. <see cref="Similarity"/> is null when the chunk was retrieved only by keyword search
/// (it was never scored against the query embedding, or scored below <see cref="HybridSearchResultDto.SimilarityThreshold"/>).
/// </summary>
public sealed record HybridSearchHitDto(
    Guid ChunkId,
    Guid DocumentId,
    string DocumentTitle,
    string CategoryName,
    int ChunkIndex,
    string Content,
    double? Similarity,
    bool MatchedBySemanticSearch,
    bool MatchedByKeywordSearch);

public sealed record HybridSearchResultDto(
    string Query,
    int TopK,
    double SimilarityThreshold,
    IReadOnlyList<HybridSearchHitDto> Hits);

/// <summary>
/// Retrieves the <see cref="Documents.KnowledgeChunk"/>s most relevant to a user's query by fusing two
/// independent rankings: cosine similarity over embeddings (semantic search) and Postgres full-text
/// search over the chunk's own words (keyword search). Fusing catches both paraphrased questions and
/// exact-term lookups (acronyms, codes, names) that embeddings alone tend to miss.
/// </summary>
public interface IHybridSearchService
{
    public const int MinQueryLength = 3;
    public const int MaxQueryLength = 300;

    Task<Result<HybridSearchResultDto>> SearchAsync(
        string query, Guid? categoryId, CancellationToken cancellationToken = default);
}
