using Docly.Application.Settings;
using Docly.Domain.Authorization;
using Docly.Web.Authorization;
using Docly.Web.ViewModels.Settings;
using Microsoft.AspNetCore.Mvc;

namespace Docly.Web.Controllers;

[HasPermission(PermissionCodes.SettingsManage)]
public class SettingsController : Controller
{
    private readonly ISystemSettingService _settingService;

    public SettingsController(ISystemSettingService settingService)
    {
        _settingService = settingService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        return View(await BuildViewModelAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(SystemSettingsViewModel model)
    {
        var values = model.Settings.ToDictionary(s => s.Key, s => s.Value);
        var result = await _settingService.UpdateAsync(values, User.GetUserId());

        if (result.Succeeded)
        {
            TempData["Success"] = "Configuración actualizada.";
            return RedirectToAction(nameof(Index));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error);
        }

        // Keep what the user just typed instead of reloading the last saved values.
        var reloaded = await BuildViewModelAsync();
        foreach (var setting in reloaded.Settings)
        {
            setting.Value = values.GetValueOrDefault(setting.Key, setting.Value);
        }

        return View(reloaded);
    }

    private async Task<SystemSettingsViewModel> BuildViewModelAsync()
    {
        var settings = await _settingService.GetAllAsync();

        return new SystemSettingsViewModel
        {
            Settings = settings
                .Select(s => new SystemSettingItemViewModel
                {
                    Key = s.Key,
                    Name = s.Name,
                    Description = s.Description,
                    Type = s.Type,
                    Value = s.Value,
                })
                .ToList(),
        };
    }
}
