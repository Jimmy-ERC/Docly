using Docly.Application.Statistics;
using Docly.Domain.Documents;
using Docly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Docly.Infrastructure.Statistics;

public class StatsService : IStatsService
{
    private readonly DoclyDbContext _db;

    public StatsService(DoclyDbContext db)
    {
        _db = db;
    }

    public async Task<StatsDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var totalDocuments = await _db.Documents.CountAsync(cancellationToken);

        var documentsByCategory = await (
            from d in _db.Documents.AsNoTracking()
            join c in _db.Categories.AsNoTracking() on d.CategoryId equals c.Id
            group d by c.Name into g
            orderby g.Key
            select new CategoryDocumentCountDto(g.Key, g.Count())
        ).ToListAsync(cancellationToken);

        var documentsByStatus = await (
            from d in _db.Documents.AsNoTracking()
            join j in _db.DocumentProcessingJobs.AsNoTracking() on d.Id equals j.DocumentId into jobs
            from job in jobs.DefaultIfEmpty()
            group d by (DocumentProcessingStatus?)job!.Status into g
            select new ProcessingStatusCountDto(g.Key, g.Count())
        ).ToListAsync(cancellationToken);

        var since = DateTime.UtcNow.AddDays(-7);

        var auditEventsLast7Days = await _db.AuditLogs.AsNoTracking()
            .CountAsync(a => a.CreatedAtUtc >= since, cancellationToken);

        var notificationsSentLast7Days = await _db.Notifications.AsNoTracking()
            .CountAsync(n => n.CreatedAtUtc >= since, cancellationToken);

        var totalUsers = await _db.Users.CountAsync(cancellationToken);
        var activeUsers = await _db.Users.CountAsync(u => u.IsActive, cancellationToken);

        var usersByRole = await (
            from ur in _db.UserRoles.AsNoTracking()
            join r in _db.Roles.AsNoTracking() on ur.RoleId equals r.Id
            group ur by r.Name into g
            orderby g.Key
            select new RoleUserCountDto(g.Key!, g.Count())
        ).ToListAsync(cancellationToken);

        var totalChatSessions = await _db.ChatSessions.CountAsync(cancellationToken);
        var totalChatMessages = await _db.ChatMessages.CountAsync(cancellationToken);
        var averageMessagesPerSession = totalChatSessions > 0 ? (double)totalChatMessages / totalChatSessions : 0;

        var totalKnowledgeChunks = await _db.KnowledgeChunks.CountAsync(cancellationToken);
        var averageChunksPerDocument = totalDocuments > 0 ? (double)totalKnowledgeChunks / totalDocuments : 0;

        var searchesLast7Days = await _db.SearchQueryLogs.AsNoTracking()
            .CountAsync(s => s.CreatedAtUtc >= since, cancellationToken);

        return new StatsDashboardDto(
            totalDocuments, documentsByCategory, documentsByStatus, auditEventsLast7Days, notificationsSentLast7Days,
            totalUsers, activeUsers, totalUsers - activeUsers, usersByRole,
            totalChatSessions, totalChatMessages, averageMessagesPerSession,
            totalKnowledgeChunks, averageChunksPerDocument,
            searchesLast7Days);
    }
}
