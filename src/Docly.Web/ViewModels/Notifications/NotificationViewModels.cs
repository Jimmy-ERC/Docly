using Docly.Application.Common;
using Docly.Application.Notifications;

namespace Docly.Web.ViewModels.Notifications;

public class NotificationIndexViewModel
{
    public required PagedResult<NotificationListItemDto> Notifications { get; set; }
}
