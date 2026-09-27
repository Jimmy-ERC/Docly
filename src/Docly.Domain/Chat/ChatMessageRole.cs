namespace Docly.Domain.Chat;

/// <summary>Persisted as an integer: append new values at the end so existing rows keep their meaning.</summary>
public enum ChatMessageRole
{
    User,
    Assistant,
}
