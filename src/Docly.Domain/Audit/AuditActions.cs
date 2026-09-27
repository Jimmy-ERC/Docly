namespace Docly.Domain.Audit;

/// <summary>
/// Action codes used when writing an <see cref="AuditLog"/> entry. Add new codes here as
/// more modules start auditing their own actions.
/// </summary>
public static class AuditActions
{
    public const string DocumentCreated = "document.created";
    public const string DocumentUpdated = "document.updated";
    public const string DocumentFileReplaced = "document.file_replaced";
    public const string DocumentDeleted = "document.deleted";

    public const string CategoryCreated = "category.created";
    public const string CategoryUpdated = "category.updated";
    public const string CategoryDeleted = "category.deleted";
}

/// <summary>Entity type codes used alongside an <see cref="AuditLog"/> action.</summary>
public static class AuditEntityTypes
{
    public const string Document = "Document";
    public const string Category = "Category";
}
