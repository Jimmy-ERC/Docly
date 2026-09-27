namespace Docly.Domain.Audit;

/// <summary>An immutable record of a relevant action performed in the system, for traceability.</summary>
public class AuditLog
{
    public Guid Id { get; set; }

    /// <summary>Null for actions performed by the system itself (e.g. a background job), not by a signed-in user.</summary>
    public Guid? UserId { get; set; }

    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }

    /// <summary>Short human-readable summary of what changed (e.g. the affected title/name).</summary>
    public string? Details { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
