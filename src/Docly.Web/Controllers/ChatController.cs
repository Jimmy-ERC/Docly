using Docly.Application.Categories;
using Docly.Application.Chat;
using Docly.Domain.Authorization;
using Docly.Web.Authorization;
using Docly.Web.ViewModels.Chat;
using Docly.Web.ViewModels.Documents;
using Microsoft.AspNetCore.Mvc;

namespace Docly.Web.Controllers;

[HasPermission(PermissionCodes.ChatUse)]
public class ChatController : Controller
{
    private readonly IChatService _chat;
    private readonly ICategoryService _categoryService;

    public ChatController(IChatService chat, ICategoryService categoryService)
    {
        _chat = chat;
        _categoryService = categoryService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(Guid? sessionId, Guid? categoryId)
    {
        var userId = User.GetUserId();
        var cancellationToken = HttpContext.RequestAborted;

        // Falls back to the active session when no session id was given, or it doesn't belong to this user.
        var session = (sessionId is { } id ? await _chat.GetSessionAsync(id, userId, cancellationToken) : null)
            ?? await _chat.GetOrCreateActiveSessionAsync(userId, cancellationToken);

        var messages = await _chat.GetMessagesAsync(session.Id, userId, cancellationToken) ?? [];
        var sessions = await _chat.GetSessionsAsync(userId, cancellationToken);

        return View(new ChatIndexViewModel
        {
            Session = session,
            Messages = messages,
            Sessions = sessions,
            CategoryId = categoryId,
            Categories = (await _categoryService.GetCategoriesAsync())
                .Select(c => new CategoryOption(c.Id, c.Name))
                .ToList(),
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ask(Guid sessionId, string question, Guid? categoryId)
    {
        if (!string.IsNullOrWhiteSpace(question))
        {
            var result = await _chat.AskAsync(sessionId, User.GetUserId(), question, categoryId, HttpContext.RequestAborted);

            if (!result.Succeeded)
            {
                TempData["Error"] = string.Join(" ", result.Errors);
            }
        }

        return RedirectToAction(nameof(Index), new { sessionId, categoryId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NewSession()
    {
        var session = await _chat.StartNewSessionAsync(User.GetUserId(), HttpContext.RequestAborted);
        return RedirectToAction(nameof(Index), new { sessionId = session.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSession(Guid sessionId)
    {
        var result = await _chat.DeleteSessionAsync(sessionId, User.GetUserId(), HttpContext.RequestAborted);

        if (!result.Succeeded)
        {
            TempData["Error"] = string.Join(" ", result.Errors);
        }

        return RedirectToAction(nameof(Index));
    }
}
