using Docly.Application.Common;
using Docly.Application.Users;

namespace Docly.Web.ViewModels.Users;

public class UserIndexViewModel
{
    public string? Search { get; set; }
    public required PagedResult<UserListItemDto> Users { get; set; }
    public Guid CurrentUserId { get; set; }
}

public record RoleOption(string Name, string? Description, bool Selected);

public class UserEditViewModel
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>True when the admin is looking at their own account (role and status changes are blocked).</summary>
    public bool IsSelf { get; set; }

    public IReadOnlyList<RoleOption> Roles { get; set; } = [];
}
