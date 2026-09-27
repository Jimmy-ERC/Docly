using Docly.Application.Notifications;
using Docly.Web.Authorization;
using Docly.Web.ViewModels.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Docly.Web.Controllers;

[Authorize]
public class NotificationsController : Controller
{
    private const int PageSize = 20;

    private readonly INotificationService _notifications;

    public NotificationsController(INotificationService notifications)
    {
        _notifications = notifications;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1)
    {
        var notifications = await _notifications.GetForUserAsync(User.GetUserId(), page, PageSize, HttpContext.RequestAborted);

        return View(new NotificationIndexViewModel { Notifications = notifications });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsRead(Guid id, string? returnUrl)
    {
        await _notifications.MarkAsReadAsync(id, User.GetUserId(), HttpContext.RequestAborted);

        return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : Url.Action(nameof(Index))!);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllAsRead()
    {
        await _notifications.MarkAllAsReadAsync(User.GetUserId(), HttpContext.RequestAborted);

        return RedirectToAction(nameof(Index));
    }
}
