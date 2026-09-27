using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Docly.Application.Ai;
using Microsoft.Extensions.Options;

namespace Docly.Infrastructure.Ai;

/// <summary>
/// Chat responses through the native Gemini REST API (`generateContent`). The key travels in the
/// `x-goog-api-key` header, never in the URL. HTTP 429/5xx are retried with exponential backoff.
/// </summary>
public class GeminiCompletionService : ICompletionService
{
    private const int MaxAttempts = 5;
    private static readonly TimeSpan MaxBackoff = TimeSpan.FromSeconds(60);

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;

    public GeminiCompletionService(HttpClient httpClient, IOptions<GeminiOptions> options)
    {
        _httpClient = httpClient;
        _apiKey = options.Value.ApiKey;
        _model = options.Value.ChatModel;
    }

    public async Task<string> CompleteAsync(
        string systemInstruction, IReadOnlyList<ChatTurn> history, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            throw new InvalidOperationException("'Gemini:ApiKey' no está configurada. Configúrala con dotnet user-secrets.");
        }

        var payload = new GenerateContentRequest(
            new SystemInstruction(new Content([new Part(systemInstruction)])),
            history.Select(turn => new TurnContent(turn.IsUser ? "user" : "model", [new Part(turn.Content)])).ToList());

        using var response = await SendWithRetryAsync(payload, cancellationToken);
        var body = await response.Content.ReadFromJsonAsync<GenerateContentResponse>(JsonOptions, cancellationToken);

        var text = body?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
        if (string.IsNullOrEmpty(text))
        {
            throw new InvalidOperationException("Gemini no devolvió texto en la respuesta.");
        }

        return text;
    }

    private async Task<HttpResponseMessage> SendWithRetryAsync(
        GenerateContentRequest payload, CancellationToken cancellationToken)
    {
        var url = $"v1beta/models/{_model}:generateContent";

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

                throw new InvalidOperationException($"Gemini respondió {status} al generar la respuesta: {Truncate(error, 500)}");
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

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];

    private sealed record Part(string Text);

    private sealed record Content(IReadOnlyList<Part> Parts);

    private sealed record SystemInstruction(Content Content);

    private sealed record TurnContent(string Role, IReadOnlyList<Part> Parts);

    private sealed record GenerateContentRequest(SystemInstruction SystemInstruction, IReadOnlyList<TurnContent> Contents);

    private sealed record Candidate(TurnContent? Content);

    private sealed record GenerateContentResponse(IReadOnlyList<Candidate>? Candidates);
}
