namespace Docly.Application.Storage;

/// <summary>Access to the private "documents" bucket in Supabase Storage.</summary>
public interface IFileStorageService
{
    /// <summary>Uploads (or overwrites) the object at <paramref name="path"/>.</summary>
    Task UploadAsync(
        string path, Stream content, string contentType, CancellationToken cancellationToken = default);

    Task DeleteAsync(string path, CancellationToken cancellationToken = default);

    Task<Stream> DownloadAsync(string path, CancellationToken cancellationToken = default);
}
