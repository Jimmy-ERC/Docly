using Docly.Application.Audit;
using Docly.Domain.Authorization;
using Docly.Web.Authorization;
using Docly.Web.ViewModels.Audit;
using Microsoft.AspNetCore.Mvc;

namespace Docly.Web.Controllers;

[HasPermission(PermissionCodes.AuditView)]
public class AuditController : Controller
{
    private const int PageSize = 25;

    private readonly IAuditService _auditService;

    public AuditController(IAuditService auditService)
    {
        _auditService = auditService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? entityType, int page = 1)
    {
        var logs = await _auditService.GetLogsAsync(new AuditLogFilter(entityType), page, PageSize);

        return View(new AuditLogIndexViewModel
        {
            EntityType = entityType,
            Logs = logs,
        });
    }
}
