using Microsoft.AspNetCore.Authorization;

namespace Docly.Web.Authorization;

/// <summary>
/// Requires the signed-in user to hold a permission. Use codes from <c>PermissionCodes</c>:
/// <c>[HasPermission(PermissionCodes.UsersManage)]</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    public const string PolicyPrefix = "Permission:";

    public HasPermissionAttribute(string permission)
    {
        Policy = PolicyPrefix + permission;
    }
}
