using System.Globalization;
using System.Text.RegularExpressions;

namespace EduVerse.Core.Catalog;

/// <summary>
/// Pictures for some questions, as a small specification ("dg:kind:arg:arg") that the browser draws as SVG.
/// Only numbers and fixed words travel: the picture never contains free text, so it needs no translation and cannot inject markup.
/// </summary>
public static partial class Diagram
{
    public const string Prefix = "dg:";

    public static readonly IReadOnlyList<string> Kinds = ["pie", "grid", "rect", "tri", "line", "angle", "bars", "polygon", "clock", "circuit", "resistors", "punnett", "market"];

    private static string Of(string kind, params object[] args) =>
        Prefix + kind + string.Concat(args.Select(a => ":" + Convert.ToString(a, CultureInfo.InvariantCulture)));

    public static string Pie(int numerator, int denominator) => Of("pie", numerator, denominator);

    /// <summary>A 10 × 10 grid with <paramref name="percent"/> squares coloured.</summary>
    public static string Grid(int percent) => Of("grid", percent);

    public static string Rect(int width, int height) => Of("rect", width, height);

    /// <summary>A right triangle with its two legs; the hypotenuse is marked "?".</summary>
    public static string RightTriangle(int a, int b) => Of("tri", a, b);

    public static string Line(int x1, int y1, int x2, int y2) => Of("line", x1, y1, x2, y2);

    public static string Angle(int degrees) => Of("angle", degrees);

    public static string Bars(IEnumerable<int> values) => Of("bars", string.Join(',', values));

    public static string Polygon(int sides) => Of("polygon", sides);

    public static string Clock(int hour, int minute) => Of("clock", hour, minute);

    public static string Circuit(double current, int resistance) => Of("circuit", current, resistance);

    public static string Resistors(int count, int resistance, bool parallel) => Of("resistors", count, resistance, parallel ? "p" : "s");

    public static string Punnett(string parent1, string parent2) => Of("punnett", parent1, parent2);

    public static string Market() => Of("market");

    /// <summary>True when the text is a well-formed specification of a known kind (only digits, dots, commas, letters A/a, p and s as arguments).</summary>
    public static bool IsValid(string? visual)
    {
        if (visual is null || !visual.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return false;
        }

        var parts = visual[Prefix.Length..].Split(':');
        return Kinds.Contains(parts[0]) && parts.Skip(1).All(p => Argument().IsMatch(p));
    }

    [GeneratedRegex(@"^[0-9.,\-Aaps]{1,40}$")]
    private static partial Regex Argument();
}
