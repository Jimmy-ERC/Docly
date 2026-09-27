namespace Docly.Domain.Chat;

/// <summary>One turn in a <see cref="ChatSession"/>, from either the user or the assistant.</summary>
public class ChatMessage
{
    public Guid Id { get; set; }

    public Guid SessionId { get; set; }

    public ChatMessageRole Role { get; set; }

    /// <summary>The raw question or answer text, without the RAG context injected for generation —
    /// that context is retrieved fresh from <see cref="Documents.KnowledgeChunk"/> for each question
    /// rather than persisted here.</summary>
    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
}
