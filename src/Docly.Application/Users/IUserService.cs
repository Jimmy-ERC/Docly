using Docly.Application.Common;

namespace Docly.Application.Users;

public sealed record UserListItemDto(
    Guid Id,
    string FullName,
    string Email,
    bool IsActive,
    DateTime CreatedAt,
    IReadOnlyList<string> Roles);

public sealed record UserDetailDto(
    Guid Id,
    string FullName,
    string Email,
    bool IsActive,
    DateTime CreatedAt,
    IReadOnlyList<string> Roles);

public interface IUserService
{
    Task<PagedResult<UserListItemDto>> GetUsersAsync(
        string? search, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<UserDetailDto?> GetUserAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Activates or deactivates an account. A user cannot deactivate themselves.</summary>
    Task<Result> SetActiveAsync(Guid id, bool isActive, Guid actingUserId, CancellationToken cancellationToken = default);

    /// <summary>Replaces the roles of a user. A user cannot change their own roles.</summary>
    Task<Result> SetRolesAsync(
        Guid id, IReadOnlyCollection<string> roleNames, Guid actingUserId, CancellationToken cancellationToken = default);
}
