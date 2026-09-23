using Docly.Infrastructure.Identity;
using Docly.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
            })
            .AddEntityFrameworkStores<DoclyDbContext>()
            .AddDefaultTokenProviders();

        return services;
    }
}