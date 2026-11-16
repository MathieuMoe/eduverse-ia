using System.Text.Json;

namespace EduVerse.Core.Parsing;

/// <summary>Tolerant readers for the JSON returned by the models (wrong types are ignored, texts trimmed and cut).</summary>
public static class Json
{
    public static string Text(JsonElement json, string name, int max)
    {
        if (json.ValueKind != JsonValueKind.Object || !json.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.String)
        {
            return "";
        }

        var text = value.GetString()!.Trim();
        return text.Length > max ? text[..max].TrimEnd() + "…" : text;
    }

    public static int? Number(JsonElement json, string name) =>
        json.ValueKind == JsonValueKind.Object && json.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetDouble(out var d) ? (int)Math.Round(d) : null;

    public static List<string> List(JsonElement json, string name, int max, int maxLength) =>
        json.ValueKind == JsonValueKind.Object && json.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Array
            ? v.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String).Select(x => x.GetString()!.Trim())
                .Where(x => x.Length > 0).Select(x => x.Length > maxLength ? x[..maxLength] : x).Take(max).ToList()
            : [];

    public static IEnumerable<JsonElement> Objects(JsonElement json, string name) =>
        json.ValueKind == JsonValueKind.Object && json.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Array
            ? v.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.Object).ToList()
            : [];
}
