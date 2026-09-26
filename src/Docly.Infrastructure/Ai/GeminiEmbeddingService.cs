using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Docly.Application.Embeddings;
using Microsoft.Extensions.Options;

namespace Docly.Infrastructure.Ai;

/// <summary>
/// Embeddings through the native Gemini REST API (`batchEmbedContents`). The key travels in the
/// `x-goog-api-key` header, never in the URL. HTTP 429/5xx are retried with exponential backoff.
/// </summary>
public class GeminiEmbeddingService : IEmbeddingService
{
    private const string DocumentTask = "RETRIEVAL_DOCUMENT";
    private const string QueryTask = "RETRIEVAL_QUERY";

    /// <summary>Hard limit of requests per `batchEmbedContents` call.</summary>
    private const int MaxRequestsPerCall = 100;

    private const int MaxAttempts = 5;
    private static readonly TimeSpan MaxBackoff = TimeSpan.FromSeconds(60);

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public GeminiEmbeddingService(HttpClient httpClient, IOptions<GeminiOptions> options)
    {
        _httpClient = httpClient;
        _apiKey = options.Value.ApiKey;
    }

    public async Task<IReadOnlyList<float[]>> EmbedDocumentsAsync(
        IReadOnlyList<string> texts, CancellationToken cancellationToken = default)
    {
        var vectors = new List<float[]>(texts.Count);

        foreach (var batch in texts.Chunk(MaxRequestsPerCall))
        {
            vectors.AddRange(await EmbedBatchAsync(batch, DocumentTask, cancellationToken));
        }

        return vectors;
    }

    public async Task<float[]> EmbedQueryAsync(string text, CancellationToken cancellationToken = default)
    {
        var vectors = await EmbedBatchAsync([text], QueryTask, cancellationToken);
        return vectors[0];
    }

    private async Task<IReadOnlyList<float[]>> EmbedBatchAsync(
        string[] texts, string taskType, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            throw new InvalidOperationException(
                "'Gemini:ApiKey' no está configurada. Configúrala con dotnet user-secrets.");
        }

        var payload = new BatchEmbedRequest(texts.Select(text => new EmbedRequest(
            $"models/{EmbeddingModelInfo.ModelName}",
            new Content([new Part(text)]),
            taskType,
            EmbeddingModelInfo.Dimensions)).ToList());

        using var response = await SendWithRetryAsync(payload, cancellationToken);
        var body = await response.Content.ReadFromJsonAsync<BatchEmbedResponse>(JsonOptions, cancellationToken);

        if (body?.Embeddings is null || body.Embeddings.Count != texts.Length)
        {
            throw new InvalidOperationException(
                $"Gemini devolvió {body?.Embeddings?.Count ?? 0} embeddings para {texts.Length} textos.");
        }

        return body.Embeddings.Select(e => Normalize(e.Values)).ToList();
    }

    private async Task<HttpResponseMessage> SendWithRetryAsync(
        BatchEmbedRequest payload, CancellationToken cancellationToken)
    {
        var url = $"v1beta/models/{EmbeddingModelInfo.ModelName}:batchEmbedContents";

        for (var attempt = 1; ; attempt++)
        {
            // A request message can only be sent once, so build a fresh one per attempt.
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(payload, options: JsonOptions),
            };
            request.Headers.Add("x-goog-api-key", _apiKey);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return response;
            }

            var retryable = response.StatusCode is HttpStatusCode.TooManyRequests
                or HttpStatusCode.InternalServerError
                or HttpStatusCode.BadGateway
                or HttpStatusCode.ServiceUnavailable
                or HttpStatusCode.GatewayTimeout;

            if (!retryable || attempt == MaxAttempts)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                var status = (int)response.StatusCode;
                response.Dispose();

                throw new InvalidOperationException(
                    $"Gemini respondió {status} al generar embeddings: {Truncate(error, 500)}");
            }

            var delay = GetDelay(response, attempt);
            response.Dispose();
            await Task.Delay(delay, cancellationToken);
        }
    }

    /// <summary>Honors `Retry-After` when Gemini sends it; otherwise 2, 4, 8, 16 s plus up to 1 s of jitter.</summary>
    private static TimeSpan GetDelay(HttpResponseMessage response, int attempt)
    {
        var retryAfter = response.Headers.RetryAfter?.Delta;
        var delay = retryAfter ?? TimeSpan.FromSeconds(Math.Pow(2, attempt));
        delay += TimeSpan.FromMilliseconds(Random.Shared.Next(0, 1000));

        return delay > MaxBackoff ? MaxBackoff : delay;
    }

    /// <summary>
    /// Vectors requested below the model's native 3072 dimensions come back unnormalized, so scale them
    /// to unit length: cosine similarity is then a plain dot product.
    /// </summary>
    private static float[] Normalize(float[]? values)
    {
        if (values is null || values.Length != EmbeddingModelInfo.Dimensions)
        {
            throw new InvalidOperationException(
                $"Gemini devolvió un embedding de {values?.Length ?? 0} dimensiones; se esperaban {EmbeddingModelInfo.Dimensions}.");
        }

        var sumOfSquares = 0d;
        foreach (var value in values)
        {
            sumOfSquares += (double)value * value;
        }

        var norm = Math.Sqrt(sumOfSquares);
        if (norm == 0)
        {
            return values;
        }

        var normalized = new float[values.Length];
        for (var i = 0; i < values.Length; i++)
        {
            normalized[i] = (float)(values[i] / norm);
        }

        return normalized;
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];

    private sealed record Part(string Text);

    private sealed record Content(IReadOnlyList<Part> Parts);

    private sealed record EmbedRequest(string Model, Content Content, string TaskType, int OutputDimensionality);

    private sealed record BatchEmbedRequest(IReadOnlyList<EmbedRequest> Requests);

    private sealed record EmbeddingValues(float[]? Values);

    private sealed record BatchEmbedResponse(IReadOnlyList<EmbeddingValues>? Embeddings);
}
