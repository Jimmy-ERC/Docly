namespace Docly.Domain.Authorization;

/// <summary>Names of the built-in roles created by the seeder.</summary>
public static class RoleNames
{
    public const string Admin = "Admin";
    public const string Editor = "Editor";
    public const string User = "User";

    /// <summary>Roles that cannot be renamed or deleted.</summary>
    public static readonly IReadOnlyList<string> BuiltIn = [Admin, Editor, User];

    /// <summary>Role assigned to every self-registered account.</summary>
    public const string Default = User;
}
