using Docly.Application.Common;

namespace Docly.Application.Chat;

public sealed record ChatSessionDto(Guid Id, string Title, DateTime UpdatedAtUtc);

public sealed record ChatMessageDto(Guid Id, bool IsUser, string Content, DateTime CreatedAtUtc);

/// <summary>
/// Drives a conversation: persists <see cref="Docly.Domain.Chat.ChatMessage"/>s, retrieves RAG context
/// for each question via <see cref="Retrieval.IContextRetrievalService"/>, and generates the answer with
/// <see cref="Ai.ICompletionService"/>. Every method is scoped to a specific user's own sessions.
/// </summary>
public interface IChatService
{
    /// <summary>The caller's most recently updated session, creating one if they have none yet. Doubles
    /// as "the active session" until feature/historial-conversaciones adds a way to pick one explicitly.</summary>
    Task<ChatSessionDto> GetOrCreateActiveSessionAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<ChatSessionDto> StartNewSessionAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Null if the session doesn't exist or doesn't belong to <paramref name="userId"/>.</summary>
    Task<ChatSessionDto?> GetSessionAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Null if the session doesn't exist or doesn't belong to <paramref name="userId"/>.</summary>
    Task<IReadOnlyList<ChatMessageDto>?> GetMessagesAsync(
        Guid sessionId, Guid userId, CancellationToken cancellationToken = default);

    Task<Result<ChatMessageDto>> AskAsync(
        Guid sessionId, Guid userId, string question, Guid? categoryId, CancellationToken cancellationToken = default);
}
