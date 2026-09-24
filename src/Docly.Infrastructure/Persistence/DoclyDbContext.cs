using Docly.Domain.Authorization;
using Docly.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Docly.Infrastructure.Persistence;

public class DoclyDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    public const string Schema = "docly";

    public DoclyDbContext(DbContextOptions<DoclyDbContext> options) : base(options)
    {
    }

    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    // Other domain entities (Document, Category, KnowledgeChunk...) will be added here as DbSet<T>.

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Must run first: configures all Identity tables (users, roles, claims...).
        base.OnModelCreating(builder);

        // Keep Docly tables out of the "public" schema exposed by Supabase's Data API.
        builder.HasDefaultSchema(Schema);

        // Ensures the pgvector extension exists (no-op if already enabled in Supabase).
        builder.HasPostgresExtension("vector");

        // Picks up every IEntityTypeConfiguration<T> class in this assembly.
        builder.ApplyConfigurationsFromAssembly(typeof(DoclyDbContext).Assembly);
    }
}