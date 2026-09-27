using Docly.Application.Common;
using Docly.Application.Notifications;
using Docly.Domain.Notifications;
using Docly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Docly.Infrastructure.Notifications;

public class NotificationService : INotificationService
{
    private readonly DoclyDbContext _db;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(DoclyDbContext db, ILogger<NotificationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task CreateAsync(
        Guid userId, string type, string title, string message, string? link,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _db.Notifications.Add(new Notification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Type = type,
                Title = title,
                Message = message,
                Link = link,
                IsRead = false,
                CreatedAtUtc = DateTime.UtcNow,
            });

            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Notifying is best-effort: losing a notification must never fail the action that triggered it.
            _logger.LogError(ex, "Failed to create notification of type {Type} for user {UserId}", type, userId);
        }
    }

    public async Task<PagedResult<NotificationListItemDto>> GetForUserAsync(
        Guid userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _db.Notifications.AsNoTracking().Where(n => n.UserId == userId);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(n => n.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NotificationListItemDto(n.Id, n.Type, n.Title, n.Message, n.Link, n.IsRead, n.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<NotificationListItemDto>(items, page, pageSize, totalCount);
    }

    public async Task<IReadOnlyList<NotificationListItemDto>> GetRecentAsync(
        Guid userId, int count, CancellationToken cancellationToken = default) =>
        await _db.Notifications.AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAtUtc)
            .Take(count)
            .Select(n => new NotificationListItemDto(n.Id, n.Type, n.Title, n.Message, n.Link, n.IsRead, n.CreatedAtUtc))
            .ToListAsync(cancellationToken);

    public Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _db.Notifications.AsNoTracking().CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);

    public async Task MarkAsReadAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var notification = await _db.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId, cancellationToken);

        if (notification is null || notification.IsRead)
        {
            return;
        }

        notification.IsRead = true;
        notification.ReadAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        await _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(n => n.IsRead, true).SetProperty(n => n.ReadAtUtc, now),
                cancellationToken);
    }
}
