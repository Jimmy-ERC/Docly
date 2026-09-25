namespace Docly.Domain.Documents;

/// <summary>
/// The current uploaded file backing a <see cref="Document"/> (1:1 — re-uploading replaces it).
/// The actual bytes live in Supabase Storage; this row only tracks where and what was uploaded.
/// </summary>
public class DocumentFile
{
    public Guid Id { get; set; }

    public Guid DocumentId { get; set; }

    /// <summary>Object path inside the "documents" bucket, e.g. "{DocumentId}/{Guid}-{fileName}".</summary>
    public string StoragePath { get; set; } = string.Empty;

    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }

    public Guid UploadedByUserId { get; set; }
    public DateTime UploadedAtUtc { get; set; }
}
