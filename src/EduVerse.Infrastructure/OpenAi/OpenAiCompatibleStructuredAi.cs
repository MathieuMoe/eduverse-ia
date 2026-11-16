using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using EduVerse.Core.Abstractions;
using EduVerse.Core.Parsing;

namespace EduVerse.Infrastructure.OpenAi;

/// <summary>OpenAI-compatible Chat Completions (ChatGPT, Ollama) with forced function calling.</summary>
public sealed partial class OpenAiCompatibleStructuredAi(
    HttpClient httpClient, OpenAiCompatibleOptions options, string engineName, ILogger<OpenAiCompatibleStructuredAi> logger) : IStructuredAi
{
    public string EngineName => engineName;

    public async Task<JsonElement> GenerateAsync(StructuredPrompt prompt, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        using var message = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
        {
            // Buffered body with Content-Length: some OpenAI-compatible servers reject chunked requests.
            Content = new StringContent(BuildRequestBody(prompt, options).ToJsonString(), Encoding.UTF8, "application/json"),
        };

        LogRequest(logger, engineName, options.Model, prompt.ToolName);
        using var response = await httpClient.SendAsync(message, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            LogApiError(logger, engineName, (int)response.StatusCode, body);
            throw new AiGenerationException(ApiError.Describe((int)response.StatusCode, body));
        }

        return ParseResponse(body);
    }

    internal static JsonObject BuildRequestBody(StructuredPrompt prompt, OpenAiCompatibleOptions options) => new()
    {
        ["model"] = options.Model,
        ["messages"] = new JsonArray
        {
            new JsonObject { ["role"] = "system", ["content"] = prompt.System },
            new JsonObject { ["role"] = "user", ["content"] = prompt.User },
        },
        ["tools"] = new JsonArray
        {
            new JsonObject
            {
                ["type"] = "function",
                ["function"] = new JsonObject
                {
                    ["name"] = prompt.ToolName,
                    ["description"] = prompt.ToolDescription,
                    ["parameters"] = JsonNode.Parse(prompt.JsonSchema),
                },
            },
        },
        ["tool_choice"] = "required",
    };

    internal static JsonElement ParseResponse(string body)
    {
        using var doc = JsonDocument.Parse(body);
        if (!doc.RootElement.TryGetProperty("choices", out var choices) || choices.ValueKind != JsonValueKind.Array || choices.GetArrayLength() == 0)
        {
            throw new AiGenerationException("The AI service returned an empty answer.");
        }

        var choice = choices[0];
        if (choice.TryGetProperty("finish_reason", out var finish) && finish.GetString() == "length")
        {
            throw new AiGenerationException("The answer was cut off. Try a shorter text.");
        }

        if (!choice.TryGetProperty("message", out var message))
        {
            throw new AiGenerationException("The AI service returned an empty answer.");
        }

        if (message.TryGetProperty("tool_calls", out var calls) && calls.ValueKind == JsonValueKind.Array)
        {
            foreach (var call in calls.EnumerateArray())
            {
                if (call.TryGetProperty("function", out var function) && function.TryGetProperty("arguments", out var arguments))
                {
                    // OpenAI sends the arguments as a JSON string; some local servers send an object.
                    return arguments.ValueKind == JsonValueKind.String ? JsonText.Parse(arguments.GetString() ?? string.Empty) : arguments.Clone();
                }
            }
        }

        if (message.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(content.GetString()))
        {
            return JsonText.Parse(content.GetString()!);
        }

        throw new AiGenerationException("The AI service returned an empty answer.");
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "{Engine} request ({Model}), tool {Tool}")]
    private static partial void LogRequest(ILogger logger, string engine, string model, string tool);

    [LoggerMessage(Level = LogLevel.Warning, Message = "{Engine} API error {Status}: {Body}")]
    private static partial void LogApiError(ILogger logger, string engine, int status, string body);
}
