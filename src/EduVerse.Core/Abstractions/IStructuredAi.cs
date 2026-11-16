using System.Text.Json;

namespace EduVerse.Core.Abstractions;

/// <summary>A picture sent with a prompt (for example a photo of a notebook). It is only held in memory while the request runs.</summary>
public sealed record PromptImage(string MediaType, byte[] Data);

/// <summary>
/// A request for structured output: the model must call one tool whose input matches <see cref="JsonSchema"/>.
/// Provider-agnostic: Claude (tool use), OpenAI/Ollama (function calling) and Gemini (function declarations).
/// </summary>
public sealed record StructuredPrompt(
    string System,
    string User,
    string ToolName,
    string ToolDescription,
    string JsonSchema,
    int MaxTokens = 4096,
    PromptImage? Image = null);

/// <summary>An AI provider able to return JSON that follows a schema.</summary>
public interface IStructuredAi
{
    string EngineName { get; }

    /// <summary>Returns the tool input produced by the model (a detached JSON element).</summary>
    Task<JsonElement> GenerateAsync(StructuredPrompt prompt, CancellationToken cancellationToken = default);
}

/// <summary>Raised when the AI service fails or returns something unusable.</summary>
public sealed class AiGenerationException : Exception
{
    public AiGenerationException() { }

    public AiGenerationException(string message) : base(message) { }

    public AiGenerationException(string message, Exception innerException) : base(message, innerException) { }
}
