using Docly.Domain.Audit;
using Docly.Domain.Authorization;
using Docly.Domain.Categories;
using Docly.Domain.Chat;
using Docly.Domain.Documents;
using Docly.Domain.Notifications;
using Docly.Domain.Search;
using Docly.Domain.Settings;
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
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentFile> DocumentFiles => Set<DocumentFile>();
    public DbSet<DocumentProcessingJob> DocumentProcessingJobs => Set<DocumentProcessingJob>();
    public DbSet<KnowledgeChunk> KnowledgeChunks => Set<KnowledgeChunk>();
    public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<MessageSource> MessageSources => Set<MessageSource>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<SearchQueryLog> SearchQueryLogs => Set<SearchQueryLog>();

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