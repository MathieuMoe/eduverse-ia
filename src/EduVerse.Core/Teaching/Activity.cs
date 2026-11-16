namespace EduVerse.Core.Teaching;

/// <summary>Answers of one day: all of them and the right ones (drawn as a hatched bar with a solid part).</summary>
public sealed record DayActivity(DateOnly Day, int Answers, int Correct);

/// <summary>Weekly activity in code: answers per day, totals, accuracy and the trend against the previous week.</summary>
public sealed record ActivitySummary(IReadOnlyList<DayActivity> Days, int Answers, int Correct, int PreviousAnswers, int PreviousCorrect)
{
    /// <summary>Percentage of right answers this period, or null without answers.</summary>
    public int? Accuracy => Answers == 0 ? null : (int)Math.Round(100.0 * Correct / Answers);

    public int? PreviousAccuracy => PreviousAnswers == 0 ? null : (int)Math.Round(100.0 * PreviousCorrect / PreviousAnswers);

    /// <summary>Change of the number of answers against the previous period, in %, or null when there is nothing to compare with.</summary>
    public int? AnswersTrend => Activity.Trend(Answers, PreviousAnswers);

    /// <summary>Change of the accuracy, in percentage points.</summary>
    public int? AccuracyTrend => Accuracy is { } a && PreviousAccuracy is { } p ? a - p : null;
}

public static class Activity
{
    public const int Days = 7;

    /// <param name="answers">Moments of the answers and whether they were right (any period: only the last 2 × <paramref name="days"/> days count).</param>
    public static ActivitySummary Summarize(IEnumerable<(DateTimeOffset At, bool Correct)> answers, DateOnly today, int days = Days)
    {
        ArgumentNullException.ThrowIfNull(answers);
        var first = today.AddDays(1 - days);
        var previousFirst = first.AddDays(-days);
        var list = answers.Select(a => (Day: DateOnly.FromDateTime(a.At.UtcDateTime), a.Correct)).Where(a => a.Day >= previousFirst && a.Day <= today).ToList();

        var current = Enumerable.Range(0, days).Select(i => first.AddDays(i)).Select(d =>
        {
            var of = list.Where(a => a.Day == d).ToList();
            return new DayActivity(d, of.Count, of.Count(a => a.Correct));
        }).ToList();
        var previous = list.Where(a => a.Day < first).ToList();
        return new ActivitySummary(current, current.Sum(d => d.Answers), current.Sum(d => d.Correct), previous.Count, previous.Count(a => a.Correct));
    }

    /// <summary>Relative change in % (rounded), null when the previous value is 0.</summary>
    public static int? Trend(int current, int previous) => previous == 0 ? null : (int)Math.Round(100.0 * (current - previous) / previous);
}
