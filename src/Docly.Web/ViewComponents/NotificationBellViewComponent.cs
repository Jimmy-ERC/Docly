using Docly.Application.Notifications;
using Docly.Web.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Docly.Web.ViewComponents;

public class NotificationBellViewComponent : ViewComponent
{
    private const int RecentCount = 5;

    private readonly INotificationService _notifications;

    public NotificationBellViewComponent(INotificationService notifications)
    {
        _notifications = notifications;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var userId = UserClaimsPrincipal.GetUserId();

        var unreadCount = await _notifications.GetUnreadCountAsync(userId, HttpContext.RequestAborted);
        var recent = await _notifications.GetRecentAsync(userId, RecentCount, HttpContext.RequestAborted);

        return View(new NotificationBellViewModel(unreadCount, recent));
    }
}

public sealed record NotificationBellViewModel(int UnreadCount, IReadOnlyList<NotificationListItemDto> Recent);
