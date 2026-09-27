namespace Docly.Domain.Chat;

/// <summary>
/// One knowledge chunk cited as evidence for an assistant <see cref="ChatMessage"/>'s answer. Snapshots
/// the chunk's document, category, and content as they were at answer time, so citations stay accurate
/// even if the source document is later edited, reprocessed, or deleted.
/// </summary>
public class MessageSource
{
    public Guid Id { get; set; }

    public Guid MessageId { get; set; }

    /// <summary>1-based position among the sources cited for the same message, matching the
    /// "[Fuente N]" markers the assistant was prompted with.</summary>
    public int Order { get; set; }

    public Guid DocumentId { get; set; }

    public string DocumentTitle { get; set; } = string.Empty;

    public string CategoryName { get; set; } = string.Empty;

    public Guid ChunkId { get; set; }

    public int ChunkIndex { get; set; }

    public string Content { get; set; } = string.Empty;
}
