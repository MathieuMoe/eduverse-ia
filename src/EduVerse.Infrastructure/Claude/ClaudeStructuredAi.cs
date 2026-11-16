using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using EduVerse.Core.Abstractions;
using EduVerse.Core.Parsing;

namespace EduVerse.Infrastructure.Claude;

/// <summary>Anthropic Messages API with a forced tool call, so the answer always follows the JSON schema.</summary>
public sealed partial class ClaudeStructuredAi(HttpClient httpClient, ClaudeOptions options, ILogger<ClaudeStructuredAi> logger) : IStructuredAi
{
    public string EngineName => "Claude";

    public async Task<JsonElement> GenerateAsync(StructuredPrompt prompt, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        using var message = new HttpRequestMessage(HttpMethod.Post, "v1/messages")
        {
            Content = new StringContent(BuildRequestBody(prompt, options).ToJsonString(), Encoding.UTF8, "application/json"),
        };

        LogRequest(logger, options.Model, prompt.ToolName);
        using var response = await httpClient.SendAsync(message, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            LogApiError(logger, (int)response.StatusCode, body);
            throw new AiGenerationException(ApiError.Describe((int)response.StatusCode, body));
        }

        return ParseResponse(body);
    }

    internal static JsonObject BuildRequestBody(StructuredPrompt prompt, ClaudeOptions options) => new()
    {
        ["model"] = options.Model,
        ["max_tokens"] = Math.Max(prompt.MaxTokens, options.MaxTokens),
        ["system"] = prompt.System,
        ["messages"] = new JsonArray { new JsonObject { ["role"] = "user", ["content"] = prompt.User } },
        ["tools"] = new JsonArray
        {
            new JsonObject
            {
                ["name"] = prompt.ToolName,
                ["description"] = prompt.ToolDescription,
                ["input_schema"] = JsonNode.Parse(prompt.JsonSchema),
            },
        },
        ["tool_choice"] = new JsonObject { ["type"] = "tool", ["name"] = prompt.ToolName },
    };

    internal static JsonElement ParseResponse(string body)
    {
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        if (root.TryGetProperty("stop_reason", out var stop) && stop.GetString() == "max_tokens")
        {
            throw new AiGenerationException("The answer was cut off. Try a shorter text.");
        }

        if (root.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Array)
        {
            foreach (var block in content.EnumerateArray())
            {
                if (block.TryGetProperty("type", out var type) && type.GetString() == "tool_use" && block.TryGetProperty("input", out var input))
                {
                    return input.Clone();
                }
            }

            foreach (var block in content.EnumerateArray())
            {
                if (block.TryGetProperty("type", out var type) && type.GetString() == "text" && block.TryGetProperty("text", out var text))
                {
                    return JsonText.Parse(text.GetString() ?? string.Empty);
                }
            }
        }

        throw new AiGenerationException("The AI service returned an empty answer.");
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Claude request ({Model}), tool {Tool}")]
    private static partial void LogRequest(ILogger logger, string model, string tool);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Anthropic API error {Status}: {Body}")]
    private static partial void LogApiError(ILogger logger, int status, string body);
}
