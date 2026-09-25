using Docly.Domain.Authorization;
using Docly.Domain.Settings;
using Docly.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Docly.Infrastructure.Persistence;

/// <summary>
/// Idempotent seed: permission catalog, built-in roles and (optionally) the first admin account.
/// Configure the admin with the user-secrets Seed:Admin:Email / Seed:Admin:Password / Seed:Admin:FullName.
/// </summary>
public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;

        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DatabaseSeeder));
        var db = provider.GetRequiredService<DoclyDbContext>();

        // Migrations are applied explicitly (dotnet ef database update); never seed a schema that isn't ready.
        var pending = await db.Database.GetPendingMigrationsAsync(cancellationToken);
        if (pending.Any())
        {
            logger.LogWarning(
                "Seeding skipped: {Count} pending migration(s). Run 'dotnet ef database update' first.", pending.Count());
            return;
        }

        await SeedPermissionsAsync(db, cancellationToken);
        await SeedRolesAsync(db, provider.GetRequiredService<RoleManager<ApplicationRole>>(), cancellationToken);
        await SeedSystemSettingsAsync(db, cancellationToken);
        await SeedAdminUserAsync(
            provider.GetRequiredService<UserManager<ApplicationUser>>(),
            provider.GetRequiredService<IConfiguration>(),
            logger);
    }

    private static async Task SeedPermissionsAsync(DoclyDbContext db, CancellationToken cancellationToken)
    {
        var existing = await db.Permissions.ToDictionaryAsync(p => p.Code, cancellationToken);

        foreach (var definition in PermissionCatalog.Permissions)
        {
            if (existing.TryGetValue(definition.Code, out var permission))
            {
                permission.Name = definition.Name;
                permission.Description = definition.Description;
            }
            else
            {
                db.Permissions.Add(new Permission
                {
                    Id = Guid.NewGuid(),
                    Code = definition.Code,
                    Name = definition.Name,
                    Description = definition.Description,
                });
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedRolesAsync(
        DoclyDbContext db, RoleManager<ApplicationRole> roleManager, CancellationToken cancellationToken)
    {
        var permissionIds = await db.Permissions.ToDictionaryAsync(p => p.Code, p => p.Id, cancellationToken);

        foreach (var definition in PermissionCatalog.Roles)
        {
            var role = await roleManager.FindByNameAsync(definition.Name);
            var isNew = role is null;

            if (role is null)
            {
                role = new ApplicationRole
                {
                    Id = Guid.NewGuid(),
                    Name = definition.Name,
                    Description = definition.Description,
                };

                var created = await roleManager.CreateAsync(role);
                if (!created.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Could not seed role '{definition.Name}': {string.Join("; ", created.Errors.Select(e => e.Description))}");
                }
            }

            // Editor/User permissions are only set on creation so later edits from the UI are respected.
            // Admin is always synced so new permissions reach it automatically.
            if (!isNew && definition.Name != RoleNames.Admin)
            {
                continue;
            }

            var granted = await db.RolePermissions
                .Where(rp => rp.RoleId == role.Id)
                .Select(rp => rp.PermissionId)
                .ToListAsync(cancellationToken);

            foreach (var code in definition.PermissionCodes)
            {
                var permissionId = permissionIds[code];
                if (!granted.Contains(permissionId))
                {
                    db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permissionId });
                }
            }

            await db.SaveChangesAsync(cancellationToken);
        }
    }

    /// <summary>Only inserts settings that don't exist yet; never overwrites a value someone already changed.</summary>
    private static async Task SeedSystemSettingsAsync(DoclyDbContext db, CancellationToken cancellationToken)
    {
        var existingKeys = await db.SystemSettings.Select(s => s.Key).ToListAsync(cancellationToken);

        foreach (var definition in SystemSettingCatalog.Settings)
        {
            if (existingKeys.Contains(definition.Key))
            {
                continue;
            }

            db.SystemSettings.Add(new SystemSetting
            {
                Id = Guid.NewGuid(),
                Key = definition.Key,
                Name = definition.Name,
                Description = definition.Description,
                Type = definition.Type,
                Value = definition.DefaultValue,
                UpdatedAtUtc = DateTime.UtcNow,
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedAdminUserAsync(
        UserManager<ApplicationUser> userManager, IConfiguration configuration, ILogger logger)
    {
        var email = configuration["Seed:Admin:Email"];
        var password = configuration["Seed:Admin:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = configuration["Seed:Admin:FullName"] ?? "Administrator",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            };

            var created = await userManager.CreateAsync(user, password);
            if (!created.Succeeded)
            {
                logger.LogError(
                    "Could not create the seed admin: {Errors}", string.Join("; ", created.Errors.Select(e => e.Description)));
                return;
            }

            logger.LogInformation("Seed admin account created for {Email}.", email);
        }

        if (!await userManager.IsInRoleAsync(user, RoleNames.Admin))
        {
            await userManager.AddToRoleAsync(user, RoleNames.Admin);
        }
    }
}
