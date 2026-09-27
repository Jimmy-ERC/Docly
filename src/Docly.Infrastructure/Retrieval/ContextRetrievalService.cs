using System.Text;
using Docly.Application.Common;
using Docly.Application.Retrieval;
using Docly.Application.Search;

namespace Docly.Infrastructure.Retrieval;

/// <summary>
/// Assembles <see cref="IHybridSearchService"/>'s ranked hits into the context block sent to Gemini.
/// Two caps on top of the fused ranking: at most <see cref="MaxChunksPerDocument"/> chunks from the same
/// document (so one long document doesn't crowd out every other relevant source), and a hard character
/// ceiling (so the prompt stays small and cheap no matter how generously RagTopK/RagChunkSize are set).
/// </summary>
public class ContextRetrievalService : IContextRetrievalService
{
    private const int MaxChunksPerDocument = 2;

    /// <summary>~2000 tokens' worth of Spanish text; comfortably below Gemini Flash's prompt budget even
    /// stacked with the question and system instructions.</summary>
    private const int MaxContextCharacters = 8000;

    private readonly IHybridSearchService _search;

    public ContextRetrievalService(IHybridSearchService search)
    {
        _search = search;
    }

    public async Task<Result<RetrievedContextDto>> RetrieveAsync(
        string query, Guid? categoryId, CancellationToken cancellationToken = default)
    {
        var searchResult = await _search.SearchAsync(query, categoryId, cancellationToken);
        if (!searchResult.Succeeded)
        {
            return Result<RetrievedContextDto>.Failure(searchResult.Errors.ToArray());
        }

        var chunksPerDocument = new Dictionary<Guid, int>();
        var sources = new List<ContextSourceDto>();
        var prompt = new StringBuilder();

        foreach (var hit in searchResult.Value!.Hits)
        {
            var countForDocument = chunksPerDocument.GetValueOrDefault(hit.DocumentId);
            if (countForDocument >= MaxChunksPerDocument)
            {
                continue;
            }

            var source = new ContextSourceDto(
                hit.ChunkId, hit.DocumentId, hit.DocumentTitle, hit.CategoryName, hit.ChunkIndex, hit.Content);
            var block = FormatSource(sources.Count + 1, source);

            // Always keep at least one source, even an oversized one, rather than returning empty context.
            if (prompt.Length > 0 && prompt.Length + block.Length > MaxContextCharacters)
            {
                break;
            }

            prompt.Append(block);
            sources.Add(source);
            chunksPerDocument[hit.DocumentId] = countForDocument + 1;
        }

        return Result<RetrievedContextDto>.Success(new RetrievedContextDto(query, prompt.ToString().TrimEnd(), sources));
    }

    private static string FormatSource(int number, ContextSourceDto source) =>
        $"[Fuente {number}: {source.DocumentTitle} ({source.CategoryName}), fragmento {source.ChunkIndex + 1}]\n{source.Content}\n\n";
}
