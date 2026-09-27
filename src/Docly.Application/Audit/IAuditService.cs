using Docly.Application.Common;

namespace Docly.Application.Audit;

public sealed record AuditLogListItemDto(
    Guid Id,
    DateTime CreatedAtUtc,
    Guid? UserId,
    string? UserFullName,
    string Action,
    string EntityType,
    Guid? EntityId,
    string? Details);

public sealed record AuditLogFilter(string? EntityType = null, Guid? UserId = null);

public interface IAuditService
{
    /// <summary>
    /// Records an audit entry. Never throws: a failure to log must not break the action being
    /// audited, so implementations should catch and swallow persistence errors.
    /// </summary>
    Task LogAsync(
        Guid? userId, string action, string entityType, Guid? entityId, string? details,
        CancellationToken cancellationToken = default);

    Task<PagedResult<AuditLogListItemDto>> GetLogsAsync(
        AuditLogFilter filter, int page, int pageSize, CancellationToken cancellationToken = default);
}
