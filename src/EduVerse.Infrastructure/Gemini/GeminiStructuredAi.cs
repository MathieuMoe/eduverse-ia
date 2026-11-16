using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using EduVerse.Core.Abstractions;
using EduVerse.Core.Parsing;
using EduVerse.Infrastructure.OpenAi;

namespace EduVerse.Infrastructure.Gemini;

/// <summary>
/// Google Gemini native API (generateContent) with forced function calling.
/// The native API accepts the new "AQ." keys through the x-goog-api-key header.
/// </summary>
public sealed partial class GeminiStructuredAi(HttpClient httpClient, OpenAiCompatibleOptions options, ILogger<GeminiStructuredAi> logger) : IStructuredAi
{
    public string EngineName => "Gemini";

    public async Task<JsonElement> GenerateAsync(StructuredPrompt prompt, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        using var message = new HttpRequestMessage(HttpMethod.Post, $"models/{Uri.EscapeDataString(options.Model)}:generateContent")
        {
            Content = new StringContent(BuildRequestBody(prompt).ToJsonString(), Encoding.UTF8, "application/json"),
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

    internal static JsonObject BuildRequestBody(StructuredPrompt prompt) => new()
    {
        ["systemInstruction"] = new JsonObject { ["parts"] = new JsonArray { new JsonObject { ["text"] = prompt.System } } },
        ["contents"] = new JsonArray
        {
            new JsonObject { ["role"] = "user", ["parts"] = UserParts(prompt) },
        },
        ["tools"] = new JsonArray
        {
            new JsonObject
            {
                ["functionDeclarations"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["name"] = prompt.ToolName,
                        ["description"] = prompt.ToolDescription,
                        ["parameters"] = JsonNode.Parse(prompt.JsonSchema),
                    },
                },
            },
        },
        ["toolConfig"] = new JsonObject
        {
            ["functionCallingConfig"] = new JsonObject
            {
                ["mode"] = "ANY",
                ["allowedFunctionNames"] = new JsonArray { prompt.ToolName },
            },
        },
    };

    private static JsonArray UserParts(StructuredPrompt prompt)
    {
        var parts = new JsonArray();
        if (prompt.Image is { } image)
        {
            parts.Add(new JsonObject { ["inline_data"] = new JsonObject { ["mime_type"] = image.MediaType, ["data"] = Convert.ToBase64String(image.Data) } });
        }

        parts.Add(new JsonObject { ["text"] = prompt.User });
        return parts;
    }

    internal static JsonElement ParseResponse(string body)
    {
        using var doc = JsonDocument.Parse(body);
        if (!doc.RootElement.TryGetProperty("candidates", out var candidates) || candidates.ValueKind != JsonValueKind.Array || candidates.GetArrayLength() == 0)
        {
            throw new AiGenerationException("The AI service returned an empty answer.");
        }

        var candidate = candidates[0];
        if (candidate.TryGetProperty("finishReason", out var finish) && finish.GetString() == "MAX_TOKENS")
        {
            throw new AiGenerationException("The answer was cut off. Try a shorter text.");
        }

        if (candidate.TryGetProperty("content", out var content) && content.TryGetProperty("parts", out var parts) && parts.ValueKind == JsonValueKind.Array)
        {
            foreach (var part in parts.EnumerateArray())
            {
                if (part.TryGetProperty("functionCall", out var call) && call.TryGetProperty("args", out var args))
                {
                    return args.Clone();
                }
            }

            foreach (var part in parts.EnumerateArray())
            {
                if (part.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(text.GetString()))
                {
                    return JsonText.Parse(text.GetString()!);
                }
            }
        }

        throw new AiGenerationException("The AI service returned an empty answer.");
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Gemini request ({Model}), tool {Tool}")]
    private static partial void LogRequest(ILogger logger, string model, string tool);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Gemini API error {Status}: {Body}")]
    private static partial void LogApiError(ILogger logger, int status, string body);
}
