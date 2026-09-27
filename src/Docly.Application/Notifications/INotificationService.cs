using Docly.Application.Common;

namespace Docly.Application.Notifications;

public sealed record NotificationListItemDto(
    Guid Id,
    string Type,
    string Title,
    string Message,
    string? Link,
    bool IsRead,
    DateTime CreatedAtUtc);

public interface INotificationService
{
    /// <summary>
    /// Records a notification for a user. Never throws: a failure to notify must not break the
    /// action that triggered it, so implementations should catch and swallow persistence errors.
    /// </summary>
    Task CreateAsync(
        Guid userId, string type, string title, string message, string? link,
        CancellationToken cancellationToken = default);

    Task<PagedResult<NotificationListItemDto>> GetForUserAsync(
        Guid userId, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NotificationListItemDto>> GetRecentAsync(
        Guid userId, int count, CancellationToken cancellationToken = default);

    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);

    Task MarkAsReadAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default);
}
