namespace Docly.Infrastructure.Ai;

public class GeminiOptions
{
    public const string SectionName = "Gemini";

    /// <summary>Google AI Studio key. Lives only in dotnet user-secrets (`Gemini:ApiKey`), never in the repo.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Native Gemini endpoint (not the OpenAI-compatible one).</summary>
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/";
}
