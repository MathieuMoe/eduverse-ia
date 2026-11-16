namespace EduVerse.Core.Institutions;

/// <summary>What the platform measured about one student (first name only).</summary>
public sealed record StudentSignals(
    string StudentId,
    string Name,
    int? DaysSinceLastAnswer,
    int AnswersThisWeek,
    int AnswersPreviousWeek,
    int? Accuracy,
    int OpenAlerts,
    int Progress,
    int DaysSinceJoined);

public enum RiskLevel
{
    Low,
    Medium,
    High,
}

/// <param name="Code">Rule code (translated by the interface).</param>
/// <param name="Value">The measured value shown next to the rule (days, %, alerts).</param>
public sealed record RiskFactor(string Code, int Points, int? Value);

public sealed record RiskResult(StudentSignals Student, int Score, RiskLevel Level, IReadOnlyList<RiskFactor> Factors);

/// <summary>
/// Early dropout alert: a sum of simple, documented rules (no black box), so a director can explain every score.
/// A high score is a reason to call and listen, not a label.
/// </summary>
public static class DropoutRisk
{
    public const int HighFrom = 50;
    public const int MediumFrom = 25;

    public static RiskResult Evaluate(StudentSignals s)
    {
        ArgumentNullException.ThrowIfNull(s);
        var factors = new List<RiskFactor>();

        if (s.DaysSinceLastAnswer is null)
        {
            if (s.DaysSinceJoined >= 7)
            {
                factors.Add(new("NeverStarted", 30, s.DaysSinceJoined));
            }
        }
        else if (s.DaysSinceLastAnswer >= 14)
        {
            factors.Add(new("Inactive", 35, s.DaysSinceLastAnswer));
        }
        else if (s.DaysSinceLastAnswer >= 7)
        {
            factors.Add(new("Inactive", 20, s.DaysSinceLastAnswer));
        }
        else if (s.DaysSinceLastAnswer >= 4)
        {
            factors.Add(new("Inactive", 10, s.DaysSinceLastAnswer));
        }

        if (s.AnswersPreviousWeek >= 5 && s.AnswersThisWeek * 2 < s.AnswersPreviousWeek)
        {
            factors.Add(new("ActivityDrop", 20, (int)Math.Round(100.0 * (s.AnswersPreviousWeek - s.AnswersThisWeek) / s.AnswersPreviousWeek)));
        }

        if (s.Accuracy is { } a && a < 50 && s.AnswersThisWeek + s.AnswersPreviousWeek >= 5)
        {
            factors.Add(new("LowAccuracy", 15, a));
        }

        if (s.OpenAlerts > 0)
        {
            factors.Add(new("TeacherAlerts", Math.Min(20, 10 * s.OpenAlerts), s.OpenAlerts));
        }

        if (s.Progress < 20 && s.DaysSinceJoined >= 21)
        {
            factors.Add(new("LowProgress", 10, s.Progress));
        }

        var score = Math.Min(100, factors.Sum(f => f.Points));
        var level = score >= HighFrom ? RiskLevel.High : score >= MediumFrom ? RiskLevel.Medium : RiskLevel.Low;
        return new RiskResult(s, score, level, factors);
    }

    public static IReadOnlyList<RiskResult> Rank(IEnumerable<StudentSignals> students) =>
        students.Select(Evaluate).OrderByDescending(r => r.Score).ThenBy(r => r.Student.Name, StringComparer.CurrentCulture).ToList();
}
