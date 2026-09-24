using Docly.Application.Roles;
using Docly.Application.Users;
using Docly.Domain.Authorization;
using Docly.Web.Authorization;
using Docly.Web.ViewModels.Users;
using Microsoft.AspNetCore.Mvc;

namespace Docly.Web.Controllers;

[HasPermission(PermissionCodes.UsersView)]
public class UsersController : Controller
{
    private const int PageSize = 15;

    private readonly IUserService _userService;
    private readonly IRoleService _roleService;

    public UsersController(IUserService userService, IRoleService roleService)
    {
        _userService = userService;
        _roleService = roleService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, int page = 1)
    {
        var users = await _userService.GetUsersAsync(search, page, PageSize);

        return View(new UserIndexViewModel
        {
            Search = search,
            Users = users,
            CurrentUserId = User.GetUserId(),
        });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var model = await BuildEditModelAsync(id);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(PermissionCodes.UsersManage)]
    public async Task<IActionResult> SetRoles(Guid id, string[]? roles)
    {
        var result = await _userService.SetRolesAsync(id, roles ?? [], User.GetUserId());

        if (result.Succeeded)
        {
            TempData["Success"] = "Roles actualizados.";
        }
        else
        {
            TempData["Error"] = string.Join(" ", result.Errors);
        }

        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(PermissionCodes.UsersManage)]
    public async Task<IActionResult> SetActive(Guid id, bool isActive)
    {
        var result = await _userService.SetActiveAsync(id, isActive, User.GetUserId());

        if (result.Succeeded)
        {
            TempData["Success"] = isActive ? "Usuario activado." : "Usuario desactivado.";
        }
        else
        {
            TempData["Error"] = string.Join(" ", result.Errors);
        }

        return RedirectToAction(nameof(Edit), new { id });
    }

    private async Task<UserEditViewModel?> BuildEditModelAsync(Guid id)
    {
        var user = await _userService.GetUserAsync(id);
        if (user is null)
        {
            return null;
        }

        var allRoles = await _roleService.GetRolesAsync();

        return new UserEditViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            IsSelf = user.Id == User.GetUserId(),
            Roles = allRoles
                .Select(r => new RoleOption(r.Name, r.Description, user.Roles.Contains(r.Name)))
                .ToList(),
        };
    }
}
