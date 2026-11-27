using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Html;

namespace EduVerse.Web;

/// <summary>
/// Original flat illustrations of a landmark of each country of the history trips, in the style of the figures'
/// dossiers (pastel sky, flat shapes, brown ink). Drawn as inline SVG: no external images, no people.
/// </summary>
public static class CountryArt
{
    private const string Ink = "#5b3d2b";

    public static IHtmlContent Svg(string key, string label, string css = "w-full h-auto") =>
        new HtmlString($"""<svg viewBox="0 0 320 160" class="{css}" xmlns="http://www.w3.org/2000/svg" role="img" aria-label="{System.Net.WebUtility.HtmlEncode(label)}" preserveAspectRatio="xMidYMid slice">{Body(key)}</svg>""");

    public static string Body(string key) => key switch
    {
        "spain" => Spain(),
        "france" => France(),
        "mexico" => Mexico(),
        "japan" => Japan(),
        "italy" => Italy(),
        "uk" => Uk(),
        "egypt" => Egypt(),
        "china" => China(),
        _ => Globe(),
    };

    /// <summary>The sky colour of the illustration, to extend it behind a banner.</summary>
    public static string Sky(string key) => key switch
    {
        "spain" => "#fde9c8",
        "france" => "#dfe9fb",
        "mexico" => "#fff0d4",
        "japan" => "#fde8ea",
        "italy" => "#fce6cf",
        "uk" => "#e2ebf6",
        "egypt" => "#fde5b6",
        "china" => "#e3f2e3",
        _ => "#e6eefb",
    };

    public static bool Has(string key) => key is "spain" or "france" or "mexico" or "japan" or "italy" or "uk" or "egypt" or "china";

    private static string N(double v) => v.ToString("0.#", CultureInfo.InvariantCulture);

    private static string Cloud(double x, double y, double s = 1) =>
        $"""<g fill="#fff" fill-opacity=".9" transform="translate({N(x)} {N(y)}) scale({N(s)})"><ellipse cx="0" cy="0" rx="18" ry="8"/><ellipse cx="12" cy="-6" rx="12" ry="9"/><ellipse cx="-10" cy="-4" rx="10" ry="7"/></g>""";

    private static string Sun(double x, double y, double r, string color) =>
        $"""<circle cx="{N(x)}" cy="{N(y)}" r="{N(r + 7)}" fill="{color}" fill-opacity=".25"/><circle cx="{N(x)}" cy="{N(y)}" r="{N(r)}" fill="{color}"/>""";

    // Spain: the Roman aqueduct of Segovia (two rows of arches, no mortar) under a hot sun
    private static string Spain()
    {
        var sb = new StringBuilder("""<rect width="320" height="160" fill="#fde9c8"/>""");
        sb.Append(Sun(262, 34, 16, "#f6a93b")).Append(Cloud(70, 30, .9));
        sb.Append(CultureInfo.InvariantCulture, $"""<g stroke="{Ink}" stroke-width="2"><rect x="14" y="56" width="292" height="80" fill="#d79b5f"/><rect x="10" y="50" width="300" height="8" fill="#c98a4f"/><rect x="10" y="84" width="300" height="6" fill="#c98a4f"/></g>""");
        sb.Append(CultureInfo.InvariantCulture, $"""<g fill="#fde9c8" stroke="{Ink}" stroke-width="1.5">""");
        for (var i = 0; i < 10; i++)
        {
            var x = 20 + i * 29;
            sb.Append(CultureInfo.InvariantCulture, $"""<path d="M{N(x + 2)} 84 V70 a7 7 0 0 1 14 0 V84 Z"/><path d="M{N(x)} 136 V104 a9 9 0 0 1 18 0 V136 Z"/>""");
        }

        sb.Append("</g>");
        sb.Append(CultureInfo.InvariantCulture, $"""<g stroke="{Ink}" stroke-width="1" stroke-opacity=".35"><path d="M14 66 H306 M14 76 H306 M14 98 H306 M14 112 H306 M14 124 H306"/></g>""");
        sb.Append("""<path d="M0 132 Q80 124 160 130 T320 128 V160 H0 Z" fill="#e6c38a"/><g fill="#7aa35a"><circle cx="36" cy="138" r="7"/><circle cx="292" cy="140" r="8"/><circle cx="304" cy="144" r="5"/></g>""");
        return sb.ToString();
    }

    // France: the Eiffel Tower (1889) by the Seine
    private static string France() =>
        $"""
        <rect width="320" height="160" fill="#dfe9fb"/>{Cloud(60, 36)}{Cloud(250, 50, .8)}
        <path d="M0 136 Q160 126 320 136 V160 H0 Z" fill="#a9cf8e"/><path d="M0 148 Q160 140 320 150 V160 H0 Z" fill="#8fb8e8"/>
        <g stroke="{Ink}" stroke-width="2" fill="#b98a5e" stroke-linejoin="round">
          <path d="M160 14 L166 40 L174 76 L192 136 H174 Q160 112 146 136 H128 L146 76 L154 40 Z"/>
          <rect x="140" y="74" width="40" height="6" fill="#a1764d"/><rect x="151" y="38" width="18" height="5" fill="#a1764d"/>
        </g>
        <g stroke="{Ink}" stroke-width="1" stroke-opacity=".55"><path d="M156 46 L164 62 M164 46 L156 62 M150 84 L170 110 M170 84 L150 110 M154 62 L166 74 M166 62 L154 74"/></g>
        <path d="M160 14 V6" stroke="{Ink}" stroke-width="2"/><path d="M160 6 l10 3 l-10 3 Z" fill="#e2574c"/>
        <g fill="#5f9a4a"><circle cx="70" cy="128" r="9"/><circle cx="86" cy="130" r="7"/><circle cx="244" cy="128" r="9"/><circle cx="258" cy="131" r="7"/></g>
        """;

    // Mexico: a Maya step pyramid with its temple, cactus and the sun
    private static string Mexico()
    {
        var sb = new StringBuilder($"""<rect width="320" height="160" fill="#fff0d4"/>{Sun(112, 34, 15, "#f4a340")}{Cloud(250, 34, .8)}""");
        sb.Append("""<path d="M0 134 Q160 122 320 134 V160 H0 Z" fill="#cfe0a0"/>""");
        sb.Append(CultureInfo.InvariantCulture, $"""<g fill="#c9a06a" stroke="{Ink}" stroke-width="2">""");
        for (var step = 0; step < 6; step++)
        {
            var w = 170 - step * 24;
            sb.Append(CultureInfo.InvariantCulture, $"""<rect x="{N(160 - w / 2.0)}" y="{N(122 - step * 14)}" width="{N(w)}" height="14"/>""");
        }

        sb.Append(CultureInfo.InvariantCulture, $"""<rect x="136" y="26" width="48" height="26" fill="#b5885a"/><rect x="152" y="34" width="16" height="18" fill="#5b3d2b"/><path d="M132 26 h56 l-6 -8 h-44 Z" fill="#a37a4e"/>""");
        sb.Append(CultureInfo.InvariantCulture, $"""<path d="M150 136 V52 h20 V136 Z" fill="#b5885a" stroke-width="1.5"/></g>""");
        sb.Append(CultureInfo.InvariantCulture, $"""<g stroke="{Ink}" stroke-width="1" stroke-opacity=".5">""");
        for (var y = 58; y < 134; y += 8)
        {
            sb.Append(CultureInfo.InvariantCulture, $"""<path d="M150 {y} h20"/>""");
        }

        sb.Append(CultureInfo.InvariantCulture, $"""</g><g fill="#5f9a4a" stroke="{Ink}" stroke-width="1.5"><path d="M266 138 V104 a7 7 0 0 1 14 0 V138 Z"/><path d="M266 122 h-8 v-10 a4 4 0 0 1 8 0"/><path d="M280 116 h8 v-12 a4 4 0 0 1 -8 0"/></g>""");
        return sb.ToString();
    }

    // Japan: Mount Fuji with snow, a red torii gate and the rising sun
    private static string Japan() =>
        $"""
        <rect width="320" height="160" fill="#fde8ea"/>{Sun(236, 46, 22, "#e2574c")}{Cloud(70, 40, .8)}
        <path d="M40 132 L140 40 Q150 32 160 40 L260 132 Z" fill="#8aa4c8" stroke="{Ink}" stroke-width="2" stroke-linejoin="round"/>
        <path d="M121 58 L140 40 Q150 32 160 40 L179 58 L168 64 L158 56 L150 66 L140 56 L130 64 Z" fill="#fff" stroke="{Ink}" stroke-width="1.5" stroke-linejoin="round"/>
        <path d="M0 132 Q160 120 320 132 V160 H0 Z" fill="#a9cf8e"/>
        <g fill="#d6453b" stroke="{Ink}" stroke-width="2"><path d="M44 82 Q76 74 108 82 L106 90 Q76 84 46 90 Z"/><rect x="50" y="96" width="52" height="6"/><rect x="56" y="88" width="7" height="50"/><rect x="89" y="88" width="7" height="50"/></g>
        <g fill="#f4a7b9"><circle cx="270" cy="120" r="10"/><circle cx="284" cy="124" r="8"/><circle cx="262" cy="128" r="7"/></g><path d="M276 132 v-8" stroke="{Ink}" stroke-width="2"/>
        """;

    // Italy: the Colosseum among cypresses
    private static string Italy()
    {
        var sb = new StringBuilder($"""<rect width="320" height="160" fill="#fce6cf"/>{Sun(268, 36, 15, "#f6a93b")}{Cloud(64, 32, .85)}""");
        sb.Append("""<path d="M0 134 Q160 124 320 134 V160 H0 Z" fill="#c8d98f"/>""");
        sb.Append(CultureInfo.InvariantCulture, $"""<path d="M70 136 V56 Q160 36 250 56 V136 Z" fill="#e0b98a" stroke="{Ink}" stroke-width="2"/><path d="M196 46 Q228 48 250 56 V136 H222 V70 Q210 58 196 60 Z" fill="#cfa274" stroke="{Ink}" stroke-width="2"/>""");
        sb.Append(CultureInfo.InvariantCulture, $"""<g fill="#8a5a3c" stroke="{Ink}" stroke-width="1">""");
        for (var row = 0; row < 3; row++)
        {
            for (var i = 0; i < 9; i++)
            {
                var x = 80 + i * 15;
                if (x > 198 && row < 3)
                {
                    continue;
                }

                var y = 64 + row * 24;
                sb.Append(CultureInfo.InvariantCulture, $"""<path d="M{N(x)} {N(y + 16)} V{N(y + 5)} a4.5 4.5 0 0 1 9 0 V{N(y + 16)} Z"/>""");
            }
        }

        sb.Append("</g>");
        sb.Append(CultureInfo.InvariantCulture, $"""<g stroke="{Ink}" stroke-width="1.5"><path d="M70 80 H196 M70 104 H196 M70 128 H196" stroke-opacity=".5"/></g>""");
        sb.Append(CultureInfo.InvariantCulture, $"""<g fill="#3f6b3a" stroke="{Ink}" stroke-width="1.5"><path d="M28 138 Q22 100 32 72 Q42 100 36 138 Z"/><path d="M282 138 Q276 104 286 80 Q296 104 290 138 Z"/></g>""");
        return sb.ToString();
    }

    // United Kingdom: the Elizabeth Tower (Big Ben) on a cloudy day
    private static string Uk() =>
        $"""
        <rect width="320" height="160" fill="#e2ebf6"/>{Cloud(70, 34)}{Cloud(232, 28, 1.1)}{Cloud(280, 58, .7)}
        <path d="M0 138 Q160 128 320 138 V160 H0 Z" fill="#a3c98a"/>
        <g stroke="{Ink}" stroke-width="2" stroke-linejoin="round">
          <rect x="136" y="64" width="48" height="74" fill="#d9b779"/><rect x="132" y="56" width="56" height="10" fill="#c9a463"/>
          <path d="M136 56 L160 14 L184 56 Z" fill="#6f7f8e"/><path d="M160 14 V4" />
          <circle cx="160" cy="84" r="15" fill="#fffbea"/><path d="M160 84 V74 M160 84 L168 88"/>
        </g>
        <g stroke="{Ink}" stroke-width="1" stroke-opacity=".5"><path d="M146 104 V136 M160 104 V136 M174 104 V136"/></g>
        <g stroke="{Ink}" stroke-width="2"><rect x="214" y="110" width="58" height="26" rx="4" fill="#d6453b"/><rect x="220" y="114" width="46" height="8" fill="#e8f1fb"/><circle cx="226" cy="138" r="5" fill="#3a3a3a"/><circle cx="260" cy="138" r="5" fill="#3a3a3a"/></g>
        <g fill="#5f9a4a"><circle cx="52" cy="130" r="10"/><circle cx="68" cy="133" r="7"/></g>
        """;

    // Egypt: the three pyramids of Giza, the Nile and a palm tree
    private static string Egypt() =>
        $"""
        <rect width="320" height="160" fill="#fde5b6"/>{Sun(264, 40, 18, "#f08a3c")}
        <path d="M0 128 Q160 116 320 128 V160 H0 Z" fill="#edc57e"/>
        <g stroke="{Ink}" stroke-width="2" stroke-linejoin="round">
          <path d="M40 130 L110 46 L180 130 Z" fill="#e0b46a"/><path d="M110 46 L180 130 H140 Z" fill="#c99a52"/>
          <path d="M150 130 L200 70 L250 130 Z" fill="#e0b46a"/><path d="M200 70 L250 130 H222 Z" fill="#c99a52"/>
          <path d="M236 130 L264 98 L292 130 Z" fill="#e0b46a"/>
        </g>
        <path d="M0 146 Q90 138 170 148 T320 144 V160 H0 Z" fill="#7fb3dc"/>
        <g stroke="{Ink}" stroke-width="2"><path d="M30 128 Q34 104 28 86" fill="none"/></g>
        <g fill="#5f9a4a" stroke="{Ink}" stroke-width="1.5"><path d="M28 86 q-16 -2 -22 8 q12 -4 22 -8 Z"/><path d="M28 86 q16 -4 22 6 q-12 -4 -22 -6 Z"/><path d="M28 86 q-6 -14 -18 -14 q10 6 18 14 Z"/><path d="M28 86 q8 -14 20 -12 q-12 4 -20 12 Z"/></g>
        """;

    // China: the Great Wall winding over green hills, with watchtowers
    private static string China() =>
        $"""
        <rect width="320" height="160" fill="#e3f2e3"/>{Sun(118, 32, 13, "#e2574c")}{Cloud(250, 34, .9)}
        <path d="M0 110 Q60 62 120 96 T240 80 T320 92 V160 H0 Z" fill="#9cc78a"/>
        <path d="M0 132 Q80 104 160 124 T320 116 V160 H0 Z" fill="#7fb06f"/>
        <path d="M-4 120 Q40 92 84 104 Q120 114 150 92 Q186 66 222 86 Q262 108 324 84" fill="none" stroke="{Ink}" stroke-width="13" stroke-linecap="round"/>
        <path d="M-4 120 Q40 92 84 104 Q120 114 150 92 Q186 66 222 86 Q262 108 324 84" fill="none" stroke="#d9c39a" stroke-width="9" stroke-linecap="round"/>
        <path d="M-4 115 Q40 87 84 99 Q120 109 150 87 Q186 61 222 81 Q262 103 324 79" fill="none" stroke="{Ink}" stroke-width="1.5" stroke-dasharray="4 4"/>
        <g fill="#c9ad7d" stroke="{Ink}" stroke-width="2"><rect x="74" y="86" width="20" height="18"/><path d="M70 86 h28 l-4 -7 h-20 Z" fill="#b8453b"/><rect x="212" y="66" width="20" height="18"/><path d="M208 66 h28 l-4 -7 h-20 Z" fill="#b8453b"/></g>
        """;

    // Any other country (written by the AI): a globe with a compass rose
    private static string Globe() =>
        $"""
        <rect width="320" height="160" fill="#e6eefb"/>{Cloud(60, 34, .8)}{Cloud(262, 44, .7)}
        <circle cx="160" cy="82" r="56" fill="#8fc2ea" stroke="{Ink}" stroke-width="2"/>
        <path d="M128 56 q14 -10 28 0 q6 14 -6 22 q-14 4 -18 16 q-10 -6 -10 -18 Q118 64 128 56 Z M176 92 q16 -6 24 6 q-2 16 -16 20 q-12 -6 -8 -26 Z" fill="#9cc78a" stroke="{Ink}" stroke-width="1.5"/>
        <g fill="none" stroke="{Ink}" stroke-width="1" stroke-opacity=".5"><ellipse cx="160" cy="82" rx="22" ry="56"/><path d="M104 82 H216 M112 56 H208 M112 108 H208"/></g>
        <g transform="translate(262 120)" stroke="{Ink}" stroke-width="1.5"><circle r="16" fill="#fffbea"/><path d="M0 -14 L4 0 L0 14 L-4 0 Z" fill="#e2574c"/></g>
        """;
}
