using Docly.Application.Common;
using Docly.Application.Roles;
using Docly.Domain.Authorization;
using Docly.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Docly.Infrastructure.Identity;

public class RoleService : IRoleService
{
    private readonly DoclyDbContext _db;
    private readonly RoleManager<ApplicationRole> _roleManager;

    public RoleService(DoclyDbContext db, RoleManager<ApplicationRole> roleManager)
    {
        _db = db;
        _roleManager = roleManager;
    }

    public async Task<IReadOnlyList<RoleListItemDto>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        var roles = await _db.Roles.AsNoTracking().OrderBy(r => r.Name).ToListAsync(cancellationToken);

        var userCounts = await _db.UserRoles
            .GroupBy(ur => ur.RoleId)
            .Select(g => new { RoleId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.RoleId, x => x.Count, cancellationToken);

        var permissionCounts = await _db.RolePermissions
            .GroupBy(rp => rp.RoleId)
            .Select(g => new { RoleId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.RoleId, x => x.Count, cancellationToken);

        return roles
            .Select(r => new RoleListItemDto(
                r.Id, r.Name!, r.Description, IsBuiltIn(r),
                userCounts.GetValueOrDefault(r.Id), permissionCounts.GetValueOrDefault(r.Id)))
            .ToList();
    }

    public async Task<RoleDetailDto?> GetRoleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var role = await _db.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (role is null)
        {
            return null;
        }

        var codes = await _db.RolePermissions
            .Where(rp => rp.RoleId == id)
            .Select(rp => rp.Permission.Code)
            .OrderBy(c => c)
            .ToListAsync(cancellationToken);

        return new RoleDetailDto(role.Id, role.Name!, role.Description, IsBuiltIn(role), codes);
    }

    public async Task<IReadOnlyList<PermissionDto>> GetPermissionsAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Permissions
            .AsNoTracking()
            .OrderBy(p => p.Code)
            .Select(p => new PermissionDto(p.Id, p.Code, p.Name, p.Description))
            .ToListAsync(cancellationToken);
    }

    public async Task<Result<Guid>> CreateAsync(
        string name, string? description, IReadOnlyCollection<string> permissionCodes,
        CancellationToken cancellationToken = default)
    {
        name = name.Trim();
        if (name.Length == 0)
        {
            return Result<Guid>.Failure("El nombre del rol es obligatorio.");
        }

        var permissions = await ResolvePermissionsAsync(permissionCodes, cancellationToken);
        if (permissions is null)
        {
            return Result<Guid>.Failure("Alguno de los permisos seleccionados no existe.");
        }

        var role = new ApplicationRole
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = NormalizeDescription(description),
        };

        var create = await _roleManager.CreateAsync(role);
        if (!create.Succeeded)
        {
            return Result<Guid>.Failure(create.Errors.Select(e => e.Description).ToArray());
        }

        _db.RolePermissions.AddRange(permissions.Select(p => new RolePermission { RoleId = role.Id, PermissionId = p.Id }));
        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(role.Id);
    }

    public async Task<Result> UpdateAsync(
        Guid id, string? description, IReadOnlyCollection<string> permissionCodes,
        CancellationToken cancellationToken = default)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (role is null)
        {
            return Result.Failure("El rol no existe.");
        }

        role.Description = NormalizeDescription(description);

        // Admin always keeps every permission, so its permission set is never edited.
        if (role.Name != RoleNames.Admin)
        {
            var permissions = await ResolvePermissionsAsync(permissionCodes, cancellationToken);
            if (permissions is null)
            {
                return Result.Failure("Alguno de los permisos seleccionados no existe.");
            }

            var wanted = permissions.Select(p => p.Id).ToHashSet();
            var current = await _db.RolePermissions.Where(rp => rp.RoleId == id).ToListAsync(cancellationToken);

            _db.RolePermissions.RemoveRange(current.Where(rp => !wanted.Contains(rp.PermissionId)));

            var currentIds = current.Select(rp => rp.PermissionId).ToHashSet();
            _db.RolePermissions.AddRange(
                wanted.Where(pid => !currentIds.Contains(pid))
                    .Select(pid => new RolePermission { RoleId = id, PermissionId = pid }));
        }

        await _db.SaveChangesAsync(cancellationToken);

        // Members' cookies must be rebuilt so the new permission claims apply.
        var newStamp = Guid.NewGuid().ToString();
        await _db.Users
            .Where(u => _db.UserRoles.Any(ur => ur.RoleId == id && ur.UserId == u.Id))
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.SecurityStamp, newStamp), cancellationToken);

        return Result.Success();
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (role is null)
        {
            return Result.Failure("El rol no existe.");
        }

        if (IsBuiltIn(role))
        {
            return Result.Failure("Los roles integrados no se pueden eliminar.");
        }

        if (await _db.UserRoles.AnyAsync(ur => ur.RoleId == id, cancellationToken))
        {
            return Result.Failure("No se puede eliminar un rol que tiene usuarios asignados.");
        }

        var delete = await _roleManager.DeleteAsync(role);
        return delete.Succeeded
            ? Result.Success()
            : Result.Failure(delete.Errors.Select(e => e.Description));
    }

    private static bool IsBuiltIn(ApplicationRole role) => RoleNames.BuiltIn.Contains(role.Name!);

    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    /// <summary>Returns the permissions for the given codes, or null if any code is unknown.</summary>
    private async Task<List<Permission>?> ResolvePermissionsAsync(
        IReadOnlyCollection<string> codes, CancellationToken cancellationToken)
    {
        var distinct = codes.Distinct().ToList();
        var permissions = await _db.Permissions.Where(p => distinct.Contains(p.Code)).ToListAsync(cancellationToken);
        return permissions.Count == distinct.Count ? permissions : null;
    }
}
