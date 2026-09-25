using System.Net.Http.Headers;
using Docly.Application.Authentication;
using Docly.Application.Categories;
using Docly.Application.Documents;
using Docly.Application.Roles;
using Docly.Application.Settings;
using Docly.Application.Storage;
using Docly.Application.Users;
using Docly.Infrastructure.Categories;
using Docly.Infrastructure.Documents;
using Docly.Infrastructure.Identity;
using Docly.Infrastructure.Persistence;
using Docly.Infrastructure.Settings;
using Docly.Infrastructure.Storage;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Pgvector.EntityFrameworkCore;

namespace Docly.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'Default' is missing. Set it with dotnet user-secrets.");
        }

        services.AddDbContext<DoclyDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
            {
                // Maps the Postgres "vector" type to Pgvector.Vector in C#.
                npgsql.UseVector();

                // Keep EF's migration history table in the same schema.
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", DoclyDbContext.Schema);
            }));

        services
            .AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                options.User.RequireUniqueEmail = true;

                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;

                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            })
            .AddEntityFrameworkStores<DoclyDbContext>()
            .AddClaimsPrincipalFactory<PermissionClaimsPrincipalFactory>()
            .AddErrorDescriber<SpanishIdentityErrorDescriber>()
            .AddDefaultTokenProviders();

        // Role/permission/deactivation changes bump the security stamp; re-check it every minute
        // so they reach already signed-in users quickly.
        services.Configure<SecurityStampValidatorOptions>(options =>
            options.ValidationInterval = TimeSpan.FromMinutes(1));

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<ISystemSettingService, SystemSettingService>();
        services.AddScoped<ICategoryService, CategoryService>();

        services.Configure<SupabaseStorageOptions>(configuration.GetSection(SupabaseStorageOptions.SectionName));

        services.AddHttpClient<IFileStorageService, SupabaseStorageService>((provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<SupabaseStorageOptions>>().Value;

            if (string.IsNullOrWhiteSpace(options.Url) || string.IsNullOrWhiteSpace(options.ServiceRoleKey))
            {
                throw new InvalidOperationException(
                    "'Supabase:Url' y 'Supabase:ServiceRoleKey' son obligatorios. Configúralos con dotnet user-secrets.");
            }

            client.BaseAddress = new Uri(options.Url.TrimEnd('/') + "/");
            client.DefaultRequestHeaders.Add("apikey", options.ServiceRoleKey);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.ServiceRoleKey);
        });

        services.AddScoped<IDocumentService, DocumentService>();

        return services;
    }

    /// <summary>Seeds permissions, built-in roles and the optional first admin. Safe to run on every start.</summary>
    public static Task SeedDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default) =>
        DatabaseSeeder.SeedAsync(services, cancellationToken);
}
