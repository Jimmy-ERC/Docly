namespace Docly.Domain.Authorization;

/// <summary>Join entity: grants a <see cref="Permission"/> to a role. The role is referenced by Guid only.</summary>
public class RolePermission
{
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }

    public Permission Permission { get; set; } = null!;
}
