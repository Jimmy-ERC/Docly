using Docly.Application.Common;

namespace Docly.Application.Retrieval;

/// <summary>One chunk that made it into the assembled context, kept separate from the prompt text so
/// the chat can cite it individually once fuentes-referencias exists.</summary>
public sealed record ContextSourceDto(
    Guid ChunkId, Guid DocumentId, string DocumentTitle, string CategoryName, int ChunkIndex, string Content);

/// <summary><see cref="PromptContext"/> is the sources formatted as a single block of text, ready to be
/// inserted into the completion prompt; <see cref="Sources"/> lists the same chunks structured, for citing.</summary>
public sealed record RetrievedContextDto(string Query, string PromptContext, IReadOnlyList<ContextSourceDto> Sources);

/// <summary>
/// Turns a hybrid search result into the context block a completion prompt can use: caps how many
/// chunks a single document contributes and how large the whole block can get, on top of the top-K and
/// deduplication <see cref="Search.IHybridSearchService"/> already applies while fusing rankings.
/// </summary>
public interface IContextRetrievalService
{
    Task<Result<RetrievedContextDto>> RetrieveAsync(
        string query, Guid? categoryId, CancellationToken cancellationToken = default);
}
