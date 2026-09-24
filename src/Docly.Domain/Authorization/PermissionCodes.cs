namespace Docly.Domain.Authorization;

/// <summary>
/// Permission codes used in [HasPermission] and the seeded catalog.
/// Add new codes here (and to <see cref="PermissionCatalog"/>) when a module needs one.
/// </summary>
public static class PermissionCodes
{
    public const string UsersView = "users.view";
    public const string UsersManage = "users.manage";
    public const string RolesManage = "roles.manage";

    public const string DocumentsView = "documents.view";
    public const string DocumentsUpload = "documents.upload";
    public const string DocumentsEdit = "documents.edit";
    public const string DocumentsDelete = "documents.delete";

    public const string CategoriesManage = "categories.manage";

    public const string ChatUse = "chat.use";

    public const string AuditView = "audit.view";
    public const string StatsView = "stats.view";
    public const string SettingsManage = "settings.manage";
}
