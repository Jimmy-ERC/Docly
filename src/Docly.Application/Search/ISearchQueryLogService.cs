namespace Docly.Application.Search;

public interface ISearchQueryLogService
{
    /// <summary>
    /// Records an explicit search. Never throws: a failure to log must not break the search it
    /// describes, so implementations should catch and swallow persistence errors.
    /// </summary>
    Task LogAsync(
        Guid userId, string query, Guid? categoryId, int resultCount,
        CancellationToken cancellationToken = default);
}
