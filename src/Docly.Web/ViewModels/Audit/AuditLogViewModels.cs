using Docly.Application.Audit;
using Docly.Application.Common;

namespace Docly.Web.ViewModels.Audit;

public class AuditLogIndexViewModel
{
    public string? EntityType { get; set; }
    public required PagedResult<AuditLogListItemDto> Logs { get; set; }
}
