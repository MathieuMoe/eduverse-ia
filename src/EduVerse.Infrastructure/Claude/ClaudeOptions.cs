namespace EduVerse.Infrastructure.Claude;

public sealed class ClaudeOptions
{
    public const string SectionName = "Claude";

    /// <summary>Anthropic API key. Never commit it: use user-secrets or the ANTHROPIC_API_KEY environment variable.</summary>
    public string? ApiKey { get; set; }

    /// <summary>Model id. See https://docs.claude.com/en/docs/about-claude/models for the current list.</summary>
    public string Model { get; set; } = "claude-sonnet-4-5";

    public int MaxTokens { get; set; } = 4096;

    public Uri BaseUrl { get; set; } = new("https://api.anthropic.com/");

    public string ApiVersion { get; set; } = "2023-06-01";

    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(90);
}
