namespace EduVerse.Infrastructure.OpenAi;

/// <summary>
/// Settings for any OpenAI-compatible chat API: OpenAI (ChatGPT), Google Gemini (native API, same settings)
/// or a local Ollama server. One implementation, three providers.
/// </summary>
public sealed class OpenAiCompatibleOptions
{
    public const string OpenAi = "OpenAI";
    public const string Gemini = "Gemini";
    public const string Ollama = "Ollama";

    /// <summary>API key (not needed for Ollama). Use user-secrets or environment variables, never the repository.</summary>
    public string? ApiKey { get; set; }

    public string Model { get; set; } = string.Empty;

    public Uri BaseUrl { get; set; } = new("https://api.openai.com/v1/");

    /// <summary>Turns on a provider that needs no key (Ollama).</summary>
    public bool Enabled { get; set; }

    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(120);

    public bool IsConfigured => Enabled || !string.IsNullOrWhiteSpace(ApiKey);
}
