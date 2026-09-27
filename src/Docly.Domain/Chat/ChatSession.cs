namespace Docly.Domain.Chat;

/// <summary>One conversation thread between a user and the AI assistant. A user can have several.</summary>
public class ChatSession
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    /// <summary>Short label for the conversation, derived from its first question.</summary>
    public string Title { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    /// <summary>Bumped on every new message; determines both the default "active session" and the
    /// ordering of the conversation history list.</summary>
    public DateTime UpdatedAtUtc { get; set; }
}
