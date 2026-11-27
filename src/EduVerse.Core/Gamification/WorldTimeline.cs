namespace EduVerse.Core.Gamification;

/// <summary>An era of another country that happened at the same time.</summary>
public sealed record Meanwhile(CountryHistory Country, int Era, int Overlap);

/// <summary>
/// The trips side by side: what was happening elsewhere during an era ("meanwhile in the world"), and the scale of the
/// world timeline that draws every country on one axis of time. All computed from the years of the eras.
/// </summary>
public static class WorldTimeline
{
    /// <summary>The year "today" is drawn at.</summary>
    public const int Now = 2026;

    public const int First = -3200;

    /// <summary>Years shown on the axis of the world timeline.</summary>
    public static readonly IReadOnlyList<int> Ticks = [-3000, -1500, 0, 500, 1000, 1500, 1800, 2000];

    /// <summary>
    /// For each other country, its era that overlaps this one the most (when one does), most overlap first.
    /// </summary>
    public static IReadOnlyList<Meanwhile> MeanwhileOf(CountryHistory country, HistoryEra era, IEnumerable<CountryHistory> others, int max = 4)
    {
        ArgumentNullException.ThrowIfNull(country);
        ArgumentNullException.ThrowIfNull(era);
        ArgumentNullException.ThrowIfNull(others);
        return others.Where(c => c.Key != country.Key)
            .Select(c => c.Eras.Select((e, i) => new Meanwhile(c, i, Overlap(era, e))).OrderByDescending(m => m.Overlap).ThenBy(m => m.Era).First())
            .Where(m => m.Overlap > 0).OrderByDescending(m => m.Overlap).ThenBy(m => m.Country.Key, StringComparer.Ordinal).Take(max).ToList();
    }

    /// <summary>Years two eras share (0 when they don't meet).</summary>
    public static int Overlap(HistoryEra a, HistoryEra b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        return Math.Max(0, Math.Min(End(a), End(b)) - Math.Max(a.From, b.From));
    }

    private static int End(HistoryEra e) => Math.Min(e.To, Now);

    // Piecewise scale: the long ancient past is squeezed, the last centuries (where most eras are) get more room
    private static readonly (int Year, double At)[] Scale = [(First, 0), (-500, 18), (500, 34), (1500, 64), (1800, 80), (Now, 100)];

    /// <summary>Where a year is on the world timeline, in percent of its width (0–100).</summary>
    public static double Position(int year)
    {
        var y = Math.Clamp(year, First, Now);
        for (var i = 1; i < Scale.Length; i++)
        {
            if (y <= Scale[i].Year)
            {
                var (y0, p0) = Scale[i - 1];
                var (y1, p1) = Scale[i];
                return p0 + (p1 - p0) * (y - y0) / (y1 - y0);
            }
        }

        return 100;
    }

    /// <summary>Left and width (percent) of the bar of an era.</summary>
    public static (double Left, double Width) Bar(HistoryEra era)
    {
        ArgumentNullException.ThrowIfNull(era);
        var left = Position(era.From);
        return (left, Math.Max(0.6, Position(End(era)) - left));
    }
}
