namespace Docly.Domain.Documents;

/// <summary>
/// MIME types and size limit accepted by the "documents" bucket. Must match the bucket's own
/// configuration in Supabase (Storage &gt; documents &gt; Edit bucket) — this is only the
/// application-side check, the bucket enforces it independently.
/// </summary>
public static class DocumentFileValidation
{
    public const long MaxSizeBytes = 20 * 1024 * 1024;

    public static readonly IReadOnlyCollection<string> AllowedContentTypes =
    [
        "application/pdf",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "text/plain",
        "text/markdown",
    ];

    public static bool IsAllowed(string contentType, long sizeBytes) =>
        sizeBytes > 0 && sizeBytes <= MaxSizeBytes && AllowedContentTypes.Contains(contentType);
}
