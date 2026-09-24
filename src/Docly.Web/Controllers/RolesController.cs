using Docly.Application.Roles;
using Docly.Domain.Authorization;
using Docly.Web.Authorization;
using Docly.Web.ViewModels.Roles;
using Microsoft.AspNetCore.Mvc;

namespace Docly.Web.Controllers;

[HasPermission(PermissionCodes.RolesManage)]
public class RolesController : Controller
{
    private readonly IRoleService _roleService;

    public RolesController(IRoleService roleService)
    {
        _roleService = roleService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        return View(new RoleIndexViewModel { Roles = await _roleService.GetRolesAsync() });
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new RoleFormViewModel();
        await LoadPermissionsAsync(model, []);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RoleFormViewModel model)
    {
        if (ModelState.IsValid)
        {
            var result = await _roleService.CreateAsync(model.Name, model.Description, model.SelectedPermissions);
            if (result.Succeeded)
            {
                TempData["Success"] = $"Rol '{model.Name.Trim()}' creado.";
                return RedirectToAction(nameof(Index));
            }

            AddErrors(result.Errors);
        }

        await LoadPermissionsAsync(model, model.SelectedPermissions);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var role = await _roleService.GetRoleAsync(id);
        if (role is null)
        {
            return NotFound();
        }

        var model = new RoleFormViewModel
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            IsBuiltIn = role.IsBuiltIn,
            IsAdmin = role.Name == RoleNames.Admin,
        };

        await LoadPermissionsAsync(model, role.PermissionCodes);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, RoleFormViewModel model)
    {
        var role = await _roleService.GetRoleAsync(id);
        if (role is null)
        {
            return NotFound();
        }

        // The name is immutable: always show the stored one, whatever was posted.
        model.Id = id;
        model.Name = role.Name;
        model.IsBuiltIn = role.IsBuiltIn;
        model.IsAdmin = role.Name == RoleNames.Admin;
        ModelState.Remove(nameof(RoleFormViewModel.Name));

        if (ModelState.IsValid)
        {
            var result = await _roleService.UpdateAsync(id, model.Description, model.SelectedPermissions);
            if (result.Succeeded)
            {
                TempData["Success"] = $"Rol '{role.Name}' actualizado.";
                return RedirectToAction(nameof(Index));
            }

            AddErrors(result.Errors);
        }

        await LoadPermissionsAsync(model, model.SelectedPermissions);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _roleService.DeleteAsync(id);

        if (result.Succeeded)
        {
            TempData["Success"] = "Rol eliminado.";
        }
        else
        {
            TempData["Error"] = string.Join(" ", result.Errors);
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task LoadPermissionsAsync(RoleFormViewModel model, IReadOnlyCollection<string> selected)
    {
        var permissions = await _roleService.GetPermissionsAsync();

        model.Permissions = permissions
            .Select(p => new PermissionOption(
                p.Code, p.Name, p.Description, model.IsAdmin || selected.Contains(p.Code)))
            .ToList();
    }

    private void AddErrors(IEnumerable<string> errors)
    {
        foreach (var error in errors)
        {
            ModelState.AddModelError(string.Empty, error);
        }
    }
}
