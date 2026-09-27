using System.Globalization;
using Docly.Application.Common;
using Docly.Application.Embeddings;
using Docly.Application.Search;
using Docly.Application.Settings;
using Docly.Domain.Settings;
using Docly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Pgvector;

namespace Docly.Infrastructure.Search;

/// <summary>
/// Fuses pgvector cosine similarity with Postgres full-text search (`KnowledgeChunks."SearchVector"`,
/// a generated `tsvector` column added in the <c>AddKnowledgeChunkSearchVector</c> migration) using
/// Reciprocal Rank Fusion: each list contributes 1/(k + rank) per chunk it contains, so a chunk found by
/// both methods outranks one found by only one. Ranking-based fusion sidesteps having to normalize cosine
/// similarity (0-1) against `ts_rank` (an unbounded, corpus-dependent score) onto a common scale.
/// </summary>
public class HybridSearchService : IHybridSearchService
{
    /// <summary>Standard smoothing constant for Reciprocal Rank Fusion; keeps a single #1 rank from
    /// dominating a chunk that ranks respectably in both lists.</summary>
    private const int RrfK = 60;

    /// <summary>Each ranking retrieves this multiple of the requested top-K, so fusion has enough
    /// candidates from both methods to work with before trimming to the final result.</summary>
    private const int CandidatePoolMultiplier = 4;
    private const int MinCandidatePoolSize = 20;

    private readonly DoclyDbContext _db;
    private readonly IEmbeddingService _embedding;
    private readonly ISystemSettingService _settings;

    public HybridSearchService(DoclyDbContext db, IEmbeddingService embedding, ISystemSettingService settings)
    {
        _db = db;
        _embedding = embedding;
        _settings = settings;
    }

    public async Task<Result<HybridSearchResultDto>> SearchAsync(
        string query, Guid? categoryId, CancellationToken cancellationToken = default)
    {
        query = query.Trim();

        if (query.Length is < IHybridSearchService.MinQueryLength or > IHybridSearchService.MaxQueryLength)
        {
            return Result<HybridSearchResultDto>.Failure(
                $"La búsqueda debe tener entre {IHybridSearchService.MinQueryLength} y " +
                $"{IHybridSearchService.MaxQueryLength} caracteres.");
        }

        var settings = await _settings.GetAllAsync(cancellationToken);
        var topK = GetInt(settings, SystemSettingKeys.RagTopK);
        var similarityThreshold = GetDecimal(settings, SystemSettingKeys.RagSimilarityThreshold);
        var candidatePoolSize = Math.Max(topK * CandidatePoolMultiplier, MinCandidatePoolSize);

        var queryVector = new Vector(await _embedding.EmbedQueryAsync(query, cancellationToken));

        // Sequential on purpose: a DbContext doesn't allow concurrent operations.
        var semanticRows = await QuerySemanticAsync(queryVector, categoryId, candidatePoolSize, cancellationToken);
        var keywordRows = await QueryKeywordAsync(query, categoryId, candidatePoolSize, cancellationToken);

        var relevantSemanticRows = semanticRows.Where(r => r.Similarity >= (double)similarityThreshold).ToList();

        var hits = Fuse(relevantSemanticRows, keywordRows, topK);

        return Result<HybridSearchResultDto>.Success(
            new HybridSearchResultDto(query, topK, (double)similarityThreshold, hits));
    }

    private async Task<List<ChunkRow>> QuerySemanticAsync(
        Vector queryVector, Guid? categoryId, int candidatePoolSize, CancellationToken cancellationToken)
    {
        var rows = categoryId is { } id
            ? _db.Database.SqlQuery<ChunkRow>(
                $"""
                 SELECT c."Id" AS "ChunkId", c."DocumentId", d."Title" AS "DocumentTitle", cat."Name" AS "CategoryName",
                        c."ChunkIndex", c."Content", 1 - (c."Embedding" <=> {queryVector}) AS "Similarity"
                 FROM docly."KnowledgeChunks" c
                 JOIN docly."Documents" d ON d."Id" = c."DocumentId"
                 JOIN docly."Categories" cat ON cat."Id" = d."CategoryId"
                 WHERE c."Embedding" IS NOT NULL AND d."CategoryId" = {id}
                 ORDER BY c."Embedding" <=> {queryVector}
                 LIMIT {candidatePoolSize}
                 """)
            : _db.Database.SqlQuery<ChunkRow>(
                $"""
                 SELECT c."Id" AS "ChunkId", c."DocumentId", d."Title" AS "DocumentTitle", cat."Name" AS "CategoryName",
                        c."ChunkIndex", c."Content", 1 - (c."Embedding" <=> {queryVector}) AS "Similarity"
                 FROM docly."KnowledgeChunks" c
                 JOIN docly."Documents" d ON d."Id" = c."DocumentId"
                 JOIN docly."Categories" cat ON cat."Id" = d."CategoryId"
                 WHERE c."Embedding" IS NOT NULL
                 ORDER BY c."Embedding" <=> {queryVector}
                 LIMIT {candidatePoolSize}
                 """);

        return await rows.ToListAsync(cancellationToken);
    }

    private async Task<List<ChunkRow>> QueryKeywordAsync(
        string query, Guid? categoryId, int candidatePoolSize, CancellationToken cancellationToken)
    {
        var rows = categoryId is { } id
            ? _db.Database.SqlQuery<ChunkRow>(
                $"""
                 SELECT c."Id" AS "ChunkId", c."DocumentId", d."Title" AS "DocumentTitle", cat."Name" AS "CategoryName",
                        c."ChunkIndex", c."Content", NULL::double precision AS "Similarity"
                 FROM docly."KnowledgeChunks" c
                 JOIN docly."Documents" d ON d."Id" = c."DocumentId"
                 JOIN docly."Categories" cat ON cat."Id" = d."CategoryId"
                 WHERE c."SearchVector" @@ websearch_to_tsquery('spanish', {query}) AND d."CategoryId" = {id}
                 ORDER BY ts_rank(c."SearchVector", websearch_to_tsquery('spanish', {query})) DESC
                 LIMIT {candidatePoolSize}
                 """)
            : _db.Database.SqlQuery<ChunkRow>(
                $"""
                 SELECT c."Id" AS "ChunkId", c."DocumentId", d."Title" AS "DocumentTitle", cat."Name" AS "CategoryName",
                        c."ChunkIndex", c."Content", NULL::double precision AS "Similarity"
                 FROM docly."KnowledgeChunks" c
                 JOIN docly."Documents" d ON d."Id" = c."DocumentId"
                 JOIN docly."Categories" cat ON cat."Id" = d."CategoryId"
                 WHERE c."SearchVector" @@ websearch_to_tsquery('spanish', {query})
                 ORDER BY ts_rank(c."SearchVector", websearch_to_tsquery('spanish', {query})) DESC
                 LIMIT {candidatePoolSize}
                 """);

        return await rows.ToListAsync(cancellationToken);
    }

    /// <summary>Combines both ranked lists with Reciprocal Rank Fusion and returns the top-K fused hits.</summary>
    private static List<HybridSearchHitDto> Fuse(
        IReadOnlyList<ChunkRow> semanticRows, IReadOnlyList<ChunkRow> keywordRows, int topK)
    {
        var scores = new Dictionary<Guid, double>();
        var rows = new Dictionary<Guid, ChunkRow>();
        var fromSemantic = new HashSet<Guid>();
        var fromKeyword = new HashSet<Guid>();

        AddRankings(semanticRows, fromSemantic, scores, rows);
        AddRankings(keywordRows, fromKeyword, scores, rows);

        return scores
            .OrderByDescending(pair => pair.Value)
            .Take(topK)
            .Select(pair =>
            {
                var row = rows[pair.Key];
                return new HybridSearchHitDto(
                    row.ChunkId, row.DocumentId, row.DocumentTitle, row.CategoryName, row.ChunkIndex, row.Content,
                    row.Similarity, fromSemantic.Contains(pair.Key), fromKeyword.Contains(pair.Key));
            })
            .ToList();
    }

    private static void AddRankings(
        IReadOnlyList<ChunkRow> list, HashSet<Guid> membership, Dictionary<Guid, double> scores,
        Dictionary<Guid, ChunkRow> rows)
    {
        for (var rank = 0; rank < list.Count; rank++)
        {
            var row = list[rank];

            // The keyword pass's rows never carry a similarity score; keep whichever version of this
            // chunk was recorded first (the semantic pass runs first) so a chunk matched by both keeps it.
            rows.TryAdd(row.ChunkId, row);

            membership.Add(row.ChunkId);
            scores[row.ChunkId] = scores.GetValueOrDefault(row.ChunkId) + 1d / (RrfK + rank + 1);
        }
    }

    private static int GetInt(IReadOnlyList<SystemSettingDto> settings, string key) =>
        int.Parse(settings.First(s => s.Key == key).Value, CultureInfo.InvariantCulture);

    private static decimal GetDecimal(IReadOnlyList<SystemSettingDto> settings, string key) =>
        decimal.Parse(settings.First(s => s.Key == key).Value, CultureInfo.InvariantCulture);

    private sealed record ChunkRow(
        Guid ChunkId, Guid DocumentId, string DocumentTitle, string CategoryName,
        int ChunkIndex, string Content, double? Similarity);
}
