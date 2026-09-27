using Docly.Application.Statistics;
using Docly.Domain.Authorization;
using Docly.Web.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Docly.Web.Controllers;

[HasPermission(PermissionCodes.StatsView)]
public class StatsController : Controller
{
    private readonly IStatsService _stats;

    public StatsController(IStatsService stats)
    {
        _stats = stats;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var dashboard = await _stats.GetDashboardAsync(HttpContext.RequestAborted);

        return View(dashboard);
    }
}
