namespace Docly.Domain.Categories;

/// <summary>A flat, non-hierarchical topic used to classify documents (e.g. "IT", "Accounting").</summary>
public class Category
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
