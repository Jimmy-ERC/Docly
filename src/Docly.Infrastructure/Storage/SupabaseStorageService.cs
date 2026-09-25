using System.Net;
using Docly.Application.Storage;
using Microsoft.Extensions.Options;

namespace Docly.Infrastructure.Storage;

/// <summary>
/// Talks to the Supabase Storage REST API directly (no supabase-csharp dependency) using the
/// service_role key, which never leaves the backend. Object paths are relative to the bucket
/// configured in <see cref="SupabaseStorageOptions.Bucket"/>.
/// </summary>
public class SupabaseStorageService : IFileStorageService
{
    private readonly HttpClient _httpClient;
    private readonly string _bucket;

    public SupabaseStorageService(HttpClient httpClient, IOptions<SupabaseStorageOptions> options)
    {
        _httpClient = httpClient;
        _bucket = options.Value.Bucket;
    }

    public async Task UploadAsync(
        string path, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        using var body = new StreamContent(content);
        body.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"storage/v1/object/{_bucket}/{path}")
        {
            Content = body,
        };
        request.Headers.Add("x-upsert", "true");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, "subir", path, cancellationToken);
    }

    public async Task DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.DeleteAsync($"storage/v1/object/{_bucket}/{path}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return;
        }

        await EnsureSuccessAsync(response, "eliminar", path, cancellationToken);
    }

    public async Task<Stream> DownloadAsync(string path, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(
            $"storage/v1/object/{_bucket}/{path}", HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        await EnsureSuccessAsync(response, "descargar", path, cancellationToken);

        var buffer = new MemoryStream();
        await response.Content.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;
        return buffer;
    }

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response, string action, string path, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new InvalidOperationException(
            $"No se pudo {action} el archivo '{path}' en Supabase Storage ({(int)response.StatusCode}): {body}");
    }
}
