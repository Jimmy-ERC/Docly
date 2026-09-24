namespace Docly.Domain.Authorization;

/// <summary>A single capability a role can be granted (e.g. "documents.upload").</summary>
public class Permission
{
    public Guid Id { get; set; }

    /// <summary>Stable machine-readable code, unique. See <see cref="PermissionCodes"/>.</summary>
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
