namespace Docly.Domain.Authorization;

public sealed record PermissionDefinition(string Code, string Name, string Description);

public sealed record RoleDefinition(string Name, string Description, IReadOnlyList<string> PermissionCodes);

/// <summary>Source of truth for the seeded permissions and the default permissions of built-in roles.</summary>
public static class PermissionCatalog
{
    public static readonly IReadOnlyList<PermissionDefinition> Permissions =
    [
        new(PermissionCodes.UsersView, "View users", "See the list of users and their roles."),
        new(PermissionCodes.UsersManage, "Manage users", "Activate, deactivate and assign roles to users."),
        new(PermissionCodes.RolesManage, "Manage roles", "Create roles and change their permissions."),
        new(PermissionCodes.DocumentsView, "View documents", "Browse and read documents."),
        new(PermissionCodes.DocumentsUpload, "Upload documents", "Upload new documents."),
        new(PermissionCodes.DocumentsEdit, "Edit documents", "Edit document metadata."),
        new(PermissionCodes.DocumentsDelete, "Delete documents", "Delete documents."),
        new(PermissionCodes.CategoriesManage, "Manage categories", "Create, edit and delete categories."),
        new(PermissionCodes.ChatUse, "Use the AI chat", "Ask questions to the AI assistant."),
        new(PermissionCodes.AuditView, "View audit log", "See the audit trail."),
        new(PermissionCodes.StatsView, "View statistics", "See usage statistics and metrics."),
        new(PermissionCodes.SettingsManage, "Manage settings", "Change system and RAG settings."),
    ];

    public static readonly IReadOnlyList<RoleDefinition> Roles =
    [
        new(RoleNames.Admin, "Full access to the system.",
            Permissions.Select(p => p.Code).ToArray()),

        new(RoleNames.Editor, "Manages documents and categories.",
        [
            PermissionCodes.DocumentsView,
            PermissionCodes.DocumentsUpload,
            PermissionCodes.DocumentsEdit,
            PermissionCodes.DocumentsDelete,
            PermissionCodes.CategoriesManage,
            PermissionCodes.ChatUse,
        ]),

        new(RoleNames.User, "Reads documents and uses the chat.",
        [
            PermissionCodes.DocumentsView,
            PermissionCodes.ChatUse,
        ]),
    ];
}
