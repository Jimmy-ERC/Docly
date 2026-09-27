namespace Docly.Domain.Notifications;

/// <summary>
/// Type codes used when writing a <see cref="Notification"/>. Add new codes here as more
/// modules start notifying users.
/// </summary>
public static class NotificationTypes
{
    public const string DocumentProcessingCompleted = "document.processing_completed";
    public const string DocumentProcessingFailed = "document.processing_failed";
}
