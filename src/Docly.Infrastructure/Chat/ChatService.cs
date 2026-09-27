using Docly.Application.Ai;
using Docly.Application.Chat;
using Docly.Application.Common;
using Docly.Application.Retrieval;
using Docly.Domain.Chat;
using Docly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Docly.Infrastructure.Chat;

public class ChatService : IChatService
{
    private const string SystemInstruction =
        "Eres el asistente de Docly, un sistema de gestión documental. Respondes preguntas basándote " +
        "únicamente en el contexto de fragmentos de documentos que se te da en cada turno del usuario. " +
        "Si el contexto no contiene información suficiente para responder, dilo explícitamente en vez de " +
        "inventar una respuesta. Responde siempre en español, de forma clara y concisa.";

    /// <summary>How many previous messages of the session are sent as conversational memory alongside
    /// each new question, on top of the RAG context retrieved fresh for that question.</summary>
    private const int MaxHistoryMessages = 10;

    private const int MaxTitleLength = 60;

    private readonly DoclyDbContext _db;
    private readonly IContextRetrievalService _contextRetrieval;
    private readonly ICompletionService _completion;

    public ChatService(DoclyDbContext db, IContextRetrievalService contextRetrieval, ICompletionService completion)
    {
        _db = db;
        _contextRetrieval = contextRetrieval;
        _completion = completion;
    }

    public async Task<ChatSessionDto> GetOrCreateActiveSessionAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        var session = await _db.ChatSessions.AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.UpdatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return session is null
            ? await CreateSessionAsync(userId, cancellationToken)
            : new ChatSessionDto(session.Id, session.Title, session.UpdatedAtUtc);
    }

    public Task<ChatSessionDto> StartNewSessionAsync(Guid userId, CancellationToken cancellationToken = default) =>
        CreateSessionAsync(userId, cancellationToken);

    public async Task<IReadOnlyList<ChatSessionDto>> GetSessionsAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        return await _db.ChatSessions.AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.UpdatedAtUtc)
            .Select(s => new ChatSessionDto(s.Id, s.Title, s.UpdatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<ChatSessionDto?> GetSessionAsync(
        Guid sessionId, Guid userId, CancellationToken cancellationToken = default)
    {
        var session = await _db.ChatSessions.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId, cancellationToken);

        return session is null ? null : new ChatSessionDto(session.Id, session.Title, session.UpdatedAtUtc);
    }

    public async Task<IReadOnlyList<ChatMessageDto>?> GetMessagesAsync(
        Guid sessionId, Guid userId, CancellationToken cancellationToken = default)
    {
        var ownsSession = await _db.ChatSessions.AsNoTracking()
            .AnyAsync(s => s.Id == sessionId && s.UserId == userId, cancellationToken);

        if (!ownsSession)
        {
            return null;
        }

        return await _db.ChatMessages.AsNoTracking()
            .Where(m => m.SessionId == sessionId)
            .OrderBy(m => m.CreatedAtUtc)
            .Select(m => new ChatMessageDto(m.Id, m.Role == ChatMessageRole.User, m.Content, m.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<Result<ChatMessageDto>> AskAsync(
        Guid sessionId, Guid userId, string question, Guid? categoryId, CancellationToken cancellationToken = default)
    {
        question = question.Trim();

        var session = await _db.ChatSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId, cancellationToken);
        if (session is null)
        {
            return Result<ChatMessageDto>.Failure("La conversación no existe.");
        }

        // Loaded before the RAG call so previous turns don't include the question currently being asked.
        var history = await _db.ChatMessages.AsNoTracking()
            .Where(m => m.SessionId == sessionId)
            .OrderByDescending(m => m.CreatedAtUtc)
            .Take(MaxHistoryMessages)
            .Select(m => new ChatTurn(m.Role == ChatMessageRole.User, m.Content))
            .ToListAsync(cancellationToken);
        history.Reverse();

        var now = DateTime.UtcNow;
        _db.ChatMessages.Add(new ChatMessage
        {
            Id = Guid.NewGuid(), SessionId = sessionId, Role = ChatMessageRole.User, Content = question, CreatedAtUtc = now,
        });

        if (history.Count == 0)
        {
            session.Title = BuildTitle(question);
        }
        session.UpdatedAtUtc = now;

        // Saved before generating the answer, so the question survives even if retrieval/generation fails.
        await _db.SaveChangesAsync(cancellationToken);

        var context = await _contextRetrieval.RetrieveAsync(question, categoryId, cancellationToken);
        if (!context.Succeeded)
        {
            return Result<ChatMessageDto>.Failure(context.Errors.ToArray());
        }

        history.Add(new ChatTurn(true, BuildPrompt(question, context.Value!.PromptContext)));

        string answer;
        try
        {
            answer = await _completion.CompleteAsync(SystemInstruction, history, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Result<ChatMessageDto>.Failure($"No se pudo generar la respuesta: {ex.Message}");
        }

        var answeredAt = DateTime.UtcNow;
        var assistantMessage = new ChatMessage
        {
            Id = Guid.NewGuid(), SessionId = sessionId, Role = ChatMessageRole.Assistant, Content = answer, CreatedAtUtc = answeredAt,
        };
        _db.ChatMessages.Add(assistantMessage);
        session.UpdatedAtUtc = answeredAt;
        await _db.SaveChangesAsync(cancellationToken);

        return Result<ChatMessageDto>.Success(new ChatMessageDto(assistantMessage.Id, false, answer, answeredAt));
    }

    public async Task<Result> DeleteSessionAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken = default)
    {
        var session = await _db.ChatSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId, cancellationToken);
        if (session is null)
        {
            return Result.Failure("La conversación no existe.");
        }

        _db.ChatSessions.Remove(session);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private async Task<ChatSessionDto> CreateSessionAsync(Guid userId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var session = new ChatSession
        {
            Id = Guid.NewGuid(), UserId = userId, Title = "Nueva conversación", CreatedAtUtc = now, UpdatedAtUtc = now,
        };

        _db.ChatSessions.Add(session);
        await _db.SaveChangesAsync(cancellationToken);

        return new ChatSessionDto(session.Id, session.Title, session.UpdatedAtUtc);
    }

    private static string BuildTitle(string question) =>
        question.Length <= MaxTitleLength ? question : question[..MaxTitleLength] + "…";

    /// <summary>The question as sent to Gemini, with the RAG context inlined. Kept out of the persisted
    /// <see cref="ChatMessage.Content"/> so history/UI show only what the user actually typed.</summary>
    private static string BuildPrompt(string question, string promptContext) =>
        string.IsNullOrEmpty(promptContext)
            ? $"{question}\n\n(No se encontró contexto relevante en la base de conocimiento para esta pregunta.)"
            : $"Contexto:\n{promptContext}\n\nPregunta: {question}";
}
