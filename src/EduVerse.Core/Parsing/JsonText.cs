using System.Text.Json;
using EduVerse.Core.Abstractions;

namespace EduVerse.Core.Parsing;

/// <summary>Parses JSON written as plain text by a model (sometimes wrapped in ```json fences).</summary>
public static class JsonText
{
    public static JsonElement Parse(string text)
    {
        var json = (text ?? string.Empty).Trim();
        if (json.StartsWith("```", StringComparison.Ordinal))
        {
            var start = json.IndexOf('\n', StringComparison.Ordinal);
            var end = json.LastIndexOf("```", StringComparison.Ordinal);
            if (start > 0 && end > start)
            {
                json = json[(start + 1)..end].Trim();
            }
        }

        // Some models add a sentence before the JSON: keep from the first '{' to the last '}'
        var first = json.IndexOf('{', StringComparison.Ordinal);
        var last = json.LastIndexOf('}');
        if (first >= 0 && last > first)
        {
            json = json[first..(last + 1)];
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.Clone();
        }
        catch (JsonException ex)
        {
            throw new AiGenerationException("The AI returned invalid JSON.", ex);
        }
    }
}
