using System.Text.Json;

namespace EduVerse.Infrastructure;

/// <summary>Turns a provider error (status + body) into a short readable reason, shown in the fallback notice.</summary>
public static class ApiError
{
    public static string Describe(int status, string? body)
    {
        var detail = Message(body);
        var reason = status switch
        {
            401 or 403 => "invalid or unauthorised API key",
            404 => "model or endpoint not found",
            429 => "quota or rate limit reached",
            400 when detail.Contains("credit", StringComparison.OrdinalIgnoreCase) => "no credit left on the account",
            >= 500 => "the provider is unavailable",
            _ => "error",
        };
        var text = $"{reason} ({status})";
        return detail.Length > 0 ? $"{text}: {detail}" : text;
    }

    /// <summary>The provider's own message ("error.message" in Anthropic, OpenAI and Google answers), cut to one line.</summary>
    internal static string Message(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return "";
        }

        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement.ValueKind == JsonValueKind.Array && doc.RootElement.GetArrayLength() > 0 ? doc.RootElement[0] : doc.RootElement;
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("error", out var e))
            {
                var m = e.ValueKind == JsonValueKind.Object && e.TryGetProperty("message", out var msg) && msg.ValueKind == JsonValueKind.String ? msg.GetString()
                    : e.ValueKind == JsonValueKind.String ? e.GetString() : null;
                if (!string.IsNullOrWhiteSpace(m))
                {
                    var line = m.ReplaceLineEndings(" ").Trim();
                    return line.Length > 160 ? line[..157] + "…" : line;
                }
            }
        }
        catch (JsonException)
        {
            // Not JSON (an HTML error page, for instance): no detail
        }

        return "";
    }
}
