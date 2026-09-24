using System.Security.Claims;
using Docly.Domain.Authorization;
using Docly.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Docly.Infrastructure.Identity;

/// <summary>Adds the user's permission codes (from all their roles) and full name as claims at sign-in.</summary>
public class PermissionClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser, ApplicationRole>
{
    private readonly DoclyDbContext _db;

    public PermissionClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IOptions<IdentityOptions> options,
        DoclyDbContext db) : base(userManager, roleManager, options)
    {
        _db = db;
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        identity.AddClaim(new Claim(PermissionClaimTypes.FullName, user.FullName));

        var permissionCodes = await _db.UserRoles
            .Where(ur => ur.UserId == user.Id)
            .Join(_db.RolePermissions, ur => ur.RoleId, rp => rp.RoleId, (_, rp) => rp.Permission.Code)
            .Distinct()
            .ToListAsync();

        foreach (var code in permissionCodes)
        {
            identity.AddClaim(new Claim(PermissionClaimTypes.Permission, code));
        }

        return identity;
    }
}
