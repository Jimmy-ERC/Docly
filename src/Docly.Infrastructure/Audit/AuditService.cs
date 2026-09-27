using Docly.Application.Audit;
using Docly.Application.Common;
using Docly.Domain.Audit;
using Docly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Docly.Infrastructure.Audit;

public class AuditService : IAuditService
{
    private readonly DoclyDbContext _db;
    private readonly ILogger<AuditService> _logger;

    public AuditService(DoclyDbContext db, ILogger<AuditService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task LogAsync(
        Guid? userId, string action, string entityType, Guid? entityId, string? details,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _db.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                Details = details,
                CreatedAtUtc = DateTime.UtcNow,
            });

            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Auditing is best-effort: losing a log entry must never fail the action it describes.
            _logger.LogError(ex, "Failed to write audit log entry for action {Action} on {EntityType} {EntityId}",
                action, entityType, entityId);
        }
    }

    public async Task<PagedResult<AuditLogListItemDto>> GetLogsAsync(
        AuditLogFilter filter, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query =
            from log in _db.AuditLogs.AsNoTracking()
            join user in _db.Users.AsNoTracking() on log.UserId equals user.Id into users
            from user in users.DefaultIfEmpty()
            select new { log, user!.FullName };

        if (!string.IsNullOrWhiteSpace(filter.EntityType))
        {
            query = query.Where(x => x.log.EntityType == filter.EntityType);
        }

        if (filter.UserId is not null)
        {
            query = query.Where(x => x.log.UserId == filter.UserId);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.log.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AuditLogListItemDto(
                x.log.Id, x.log.CreatedAtUtc, x.log.UserId, x.FullName,
                x.log.Action, x.log.EntityType, x.log.EntityId, x.log.Details))
            .ToListAsync(cancellationToken);

        return new PagedResult<AuditLogListItemDto>(items, page, pageSize, totalCount);
    }
}
