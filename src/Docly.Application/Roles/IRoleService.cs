using Docly.Application.Common;

namespace Docly.Application.Roles;

public sealed record PermissionDto(Guid Id, string Code, string Name, string? Description);

public sealed record RoleListItemDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsBuiltIn,
    int UserCount,
    int PermissionCount);

public sealed record RoleDetailDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsBuiltIn,
    IReadOnlyList<string> PermissionCodes);

public interface IRoleService
{
    Task<IReadOnlyList<RoleListItemDto>> GetRolesAsync(CancellationToken cancellationToken = default);

    Task<RoleDetailDto?> GetRoleAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PermissionDto>> GetPermissionsAsync(CancellationToken cancellationToken = default);

    Task<Result<Guid>> CreateAsync(
        string name, string? description, IReadOnlyCollection<string> permissionCodes,
        CancellationToken cancellationToken = default);

    /// <summary>Updates description and permissions. The role name is immutable. Admin keeps all permissions.</summary>
    Task<Result> UpdateAsync(
        Guid id, string? description, IReadOnlyCollection<string> permissionCodes,
        CancellationToken cancellationToken = default);

    /// <summary>Deletes a custom role. Built-in roles and roles with assigned users cannot be deleted.</summary>
    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
