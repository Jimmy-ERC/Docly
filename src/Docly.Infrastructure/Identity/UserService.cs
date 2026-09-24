using Docly.Application.Common;
using Docly.Application.Users;
using Docly.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Docly.Infrastructure.Identity;

public class UserService : IUserService
{
    private readonly DoclyDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public UserService(DoclyDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<PagedResult<UserListItemDto>> GetUsersAsync(
        string? search, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(u => u.FullName.ToLower().Contains(term) || u.Email!.ToLower().Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);

        var users = await query
            .OrderBy(u => u.FullName).ThenBy(u => u.Email)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var rolesByUser = await GetRoleNamesAsync(users.Select(u => u.Id).ToList(), cancellationToken);

        var items = users
            .Select(u => new UserListItemDto(
                u.Id, u.FullName, u.Email ?? string.Empty, u.IsActive, u.CreatedAt,
                rolesByUser.GetValueOrDefault(u.Id) ?? []))
            .ToList();

        return new PagedResult<UserListItemDto>(items, page, pageSize, total);
    }

    public async Task<UserDetailDto?> GetUserAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var rolesByUser = await GetRoleNamesAsync([id], cancellationToken);

        return new UserDetailDto(
            user.Id, user.FullName, user.Email ?? string.Empty, user.IsActive, user.CreatedAt,
            rolesByUser.GetValueOrDefault(id) ?? []);
    }

    public async Task<Result> SetActiveAsync(
        Guid id, bool isActive, Guid actingUserId, CancellationToken cancellationToken = default)
    {
        if (id == actingUserId && !isActive)
        {
            return Result.Failure("No puedes desactivar tu propia cuenta.");
        }

        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return Result.Failure("El usuario no existe.");
        }

        if (user.IsActive == isActive)
        {
            return Result.Success();
        }

        user.IsActive = isActive;
        var update = await _userManager.UpdateAsync(user);
        if (!update.Succeeded)
        {
            return Result.Failure(update.Errors.Select(e => e.Description));
        }

        if (!isActive)
        {
            // Invalidates the user's cookie so the deactivation takes effect on their next validation.
            await _userManager.UpdateSecurityStampAsync(user);
        }

        return Result.Success();
    }

    public async Task<Result> SetRolesAsync(
        Guid id, IReadOnlyCollection<string> roleNames, Guid actingUserId, CancellationToken cancellationToken = default)
    {
        if (id == actingUserId)
        {
            // Guarantees there is always at least one active admin: the one doing the change.
            return Result.Failure("No puedes cambiar tus propios roles.");
        }

        var requested = roleNames.Distinct().ToList();
        if (requested.Count == 0)
        {
            return Result.Failure("El usuario debe tener al menos un rol.");
        }

        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return Result.Failure("El usuario no existe.");
        }

        var existingRoles = await _db.Roles
            .Where(r => requested.Contains(r.Name!))
            .Select(r => r.Name!)
            .ToListAsync(cancellationToken);

        if (existingRoles.Count != requested.Count)
        {
            return Result.Failure("Alguno de los roles seleccionados no existe.");
        }

        var current = await _userManager.GetRolesAsync(user);

        var toRemove = current.Except(requested).ToList();
        var toAdd = requested.Except(current).ToList();

        if (toRemove.Count > 0)
        {
            var removed = await _userManager.RemoveFromRolesAsync(user, toRemove);
            if (!removed.Succeeded)
            {
                return Result.Failure(removed.Errors.Select(e => e.Description));
            }
        }

        if (toAdd.Count > 0)
        {
            var added = await _userManager.AddToRolesAsync(user, toAdd);
            if (!added.Succeeded)
            {
                return Result.Failure(added.Errors.Select(e => e.Description));
            }
        }

        // Forces the cookie to be rebuilt so the new permission claims apply.
        await _userManager.UpdateSecurityStampAsync(user);
        return Result.Success();
    }

    private async Task<Dictionary<Guid, IReadOnlyList<string>>> GetRoleNamesAsync(
        IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken)
    {
        var pairs = await _db.UserRoles
            .Where(ur => userIds.Contains(ur.UserId))
            .Join(_db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, RoleName = r.Name! })
            .ToListAsync(cancellationToken);

        return pairs
            .GroupBy(p => p.UserId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(p => p.RoleName).Order().ToList());
    }
}
