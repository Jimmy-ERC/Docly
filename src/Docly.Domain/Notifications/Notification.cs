namespace Docly.Domain.Notifications;

/// <summary>An in-app message directed at a single user about something relevant that happened.</summary>
public class Notification
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    /// <summary>Relative URL the user is sent to when they open the notification, if any.</summary>
    public string? Link { get; set; }

    public bool IsRead { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ReadAtUtc { get; set; }
}
