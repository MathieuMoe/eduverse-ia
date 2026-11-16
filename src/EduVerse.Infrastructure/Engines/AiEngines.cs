using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using EduVerse.Core.Abstractions;
using EduVerse.Infrastructure.Claude;
using EduVerse.Infrastructure.Gemini;
using EduVerse.Infrastructure.OpenAi;

namespace EduVerse.Infrastructure.Engines;

/// <summary>An AI engine the user can choose in the interface. <see cref="CreateAi"/> is null for the offline demo.</summary>
public sealed record AiEngine(string Id, string Name, bool IsDemo, Func<IStructuredAi>? CreateAi);

/// <summary>
/// The AI engines available with the current configuration (each provider appears when its key is set),
/// plus the offline demo, which is always available.
/// </summary>
public sealed class AiEngines
{
    public const string ClaudeId = "claude";
    public const string OpenAiId = "openai";
    public const string GeminiId = "gemini";
    public const string OllamaId = "ollama";
    public const string DemoId = "demo";

    private readonly Dictionary<string, AiEngine> _byId;

    public AiEngines(
        IHttpClientFactory httpClientFactory,
        IOptions<ClaudeOptions> claude,
        IOptionsMonitor<OpenAiCompatibleOptions> openAi,
        IOptions<AiOptions> ai,
        ILoggerFactory? loggerFactory = null)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentNullException.ThrowIfNull(claude);
        ArgumentNullException.ThrowIfNull(openAi);
        ArgumentNullException.ThrowIfNull(ai);
        loggerFactory ??= NullLoggerFactory.Instance;
        var engines = new List<AiEngine>();

        if (!string.IsNullOrWhiteSpace(claude.Value.ApiKey))
        {
            engines.Add(new(ClaudeId, "Claude", false, () =>
                new ClaudeStructuredAi(httpClientFactory.CreateClient(ClaudeId), claude.Value, loggerFactory.CreateLogger<ClaudeStructuredAi>())));
        }

        foreach (var (id, name, section) in new[]
        {
            (OpenAiId, "ChatGPT", OpenAiCompatibleOptions.OpenAi),
            (GeminiId, "Gemini", OpenAiCompatibleOptions.Gemini),
            (OllamaId, "Ollama (local)", OpenAiCompatibleOptions.Ollama),
        })
        {
            var options = openAi.Get(section);
            if (options.IsConfigured)
            {
                engines.Add(new(id, name, false, id == GeminiId
                    ? () => new GeminiStructuredAi(httpClientFactory.CreateClient(id), options, loggerFactory.CreateLogger<GeminiStructuredAi>())
                    : () => new OpenAiCompatibleStructuredAi(httpClientFactory.CreateClient(id), options, name, loggerFactory.CreateLogger<OpenAiCompatibleStructuredAi>())));
            }
        }

        engines.Add(new(DemoId, "Demo", true, null));

        All = engines;
        _byId = engines.ToDictionary(e => e.Id, StringComparer.OrdinalIgnoreCase);
        Default = (ai.Value.Default is { } preferred && _byId.TryGetValue(preferred, out var chosen)) ? chosen : engines[0];
    }

    public IReadOnlyList<AiEngine> All { get; }

    public AiEngine Default { get; }

    /// <summary>The engine with this id, or the default one when the id is unknown or not configured.</summary>
    public AiEngine Get(string? id) => id is not null && _byId.TryGetValue(id, out var engine) ? engine : Default;
}

/// <summary>"Ai" section: which engine is selected by default (claude, openai, gemini, ollama or demo).</summary>
public sealed class AiOptions
{
    public const string SectionName = "Ai";

    public string? Default { get; set; }
}
