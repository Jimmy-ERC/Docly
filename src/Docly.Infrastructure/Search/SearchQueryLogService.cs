using Docly.Application.Search;
using Docly.Domain.Search;
using Docly.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace Docly.Infrastructure.Search;

public class SearchQueryLogService : ISearchQueryLogService
{
    private readonly DoclyDbContext _db;
    private readonly ILogger<SearchQueryLogService> _logger;

    public SearchQueryLogService(DoclyDbContext db, ILogger<SearchQueryLogService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task LogAsync(
        Guid userId, string query, Guid? categoryId, int resultCount,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _db.SearchQueryLogs.Add(new SearchQueryLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Query = query.Length > 500 ? query[..500] : query,
                CategoryId = categoryId,
                ResultCount = resultCount,
                CreatedAtUtc = DateTime.UtcNow,
            });

            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Logging a search is best-effort: losing an entry must never fail the search itself.
            _logger.LogError(ex, "Failed to log search query for user {UserId}", userId);
        }
    }
}
