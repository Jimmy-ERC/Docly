namespace Docly.Application.Ai;

/// <summary>One turn of a conversation sent to the completion model, in chronological order.</summary>
public sealed record ChatTurn(bool IsUser, string Content);

/// <summary>Generates chat responses with the Gemini Flash model. See <see cref="Embeddings.IEmbeddingService"/>
/// for the equivalent abstraction over embeddings.</summary>
public interface ICompletionService
{
    /// <summary>
    /// Generates the assistant's reply to a conversation. <paramref name="history"/> must end with the
    /// current user turn (its content already carries the RAG context to answer from); earlier turns
    /// give the model the thread of the conversation. Throws if the provider keeps failing after retries.
    /// </summary>
    Task<string> CompleteAsync(
        string systemInstruction, IReadOnlyList<ChatTurn> history, CancellationToken cancellationToken = default);
}
