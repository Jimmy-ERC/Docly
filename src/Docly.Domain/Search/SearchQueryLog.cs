namespace Docly.Domain.Search;

/// <summary>A record of an explicit search performed through the hybrid search feature, for traceability and stats.</summary>
public class SearchQueryLog
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Query { get; set; } = string.Empty;
    public Guid? CategoryId { get; set; }
    public int ResultCount { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
