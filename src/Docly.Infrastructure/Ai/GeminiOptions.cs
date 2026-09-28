namespace Docly.Infrastructure.Ai;

public class GeminiOptions
{
    public const string SectionName = "Gemini";

    /// <summary>Google AI Studio key. Lives only in dotnet user-secrets (`Gemini:ApiKey`), never in the repo.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Native Gemini endpoint (not the OpenAI-compatible one).</summary>
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/";

    /// <summary>Flash-Lite instead of full Flash: as of Google's Dec. 2025 free-tier cut, gemini-3.8-flash
    /// (behind "gemini-flash-latest") only grants ~20 requests/day, while Flash-Lite grants ~500/day.
    /// Revisit only after checking both models' current free-tier quota in Google AI Studio.</summary>
    public string ChatModel { get; set; } = "gemini-flash-lite-latest";
}
