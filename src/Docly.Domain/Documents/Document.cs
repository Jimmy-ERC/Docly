namespace Docly.Domain.Documents;

/// <summary>Logical document owned by a user, classified under one <see cref="Categories.Category"/>.</summary>
public class Document
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    public Guid CategoryId { get; set; }
    public Guid OwnerUserId { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
