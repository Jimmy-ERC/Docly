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

        return new StatsDashboardDto(
            totalDocuments, documentsByCategory, documentsByStatus, auditEventsLast7Days, notificationsSentLast7Days);
    }
}
