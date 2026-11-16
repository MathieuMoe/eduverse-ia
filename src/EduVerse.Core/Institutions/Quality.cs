using EduVerse.Core.Learning;

namespace EduVerse.Core.Institutions;

public sealed record MethodStat(ExplanationMethod Method, int Answers, int Correct)
{
    public int? Rate => Answers == 0 ? null : (int)Math.Round(100.0 * Correct / Answers);
}

/// <summary>Quality of the teaching: which explanation methods work, how fast the teachers answer the tutor's alerts.</summary>
public sealed record QualityReport(
    IReadOnlyList<MethodStat> Methods,
    ExplanationMethod? Best,
    int AlertsOpen,
    int AlertsResolved,
    double? AverageHoursToResolve,
    int ConceptsMastered,
    int MasteredAfterSwitch);

public static class Quality
{
    /// <summary>Answers needed before a method can be called the best one.</summary>
    public const int MinAnswersForBest = 5;

    /// <param name="answers">Practice answers (placement tests excluded) with the method in use.</param>
    /// <param name="alerts">Creation and resolution moments of the alerts.</param>
    /// <param name="states">Mastery states of the students.</param>
    public static QualityReport Build(
        IEnumerable<(ExplanationMethod Method, bool Correct)> answers,
        IEnumerable<(DateTimeOffset Created, DateTimeOffset? Resolved)> alerts,
        IEnumerable<MasteryState> states)
    {
        ArgumentNullException.ThrowIfNull(answers);
        ArgumentNullException.ThrowIfNull(alerts);
        ArgumentNullException.ThrowIfNull(states);
        var a = answers.ToList();
        var methods = Enum.GetValues<ExplanationMethod>()
            .Select(m => new MethodStat(m, a.Count(x => x.Method == m), a.Count(x => x.Method == m && x.Correct))).ToList();
        var best = methods.Where(m => m.Answers >= MinAnswersForBest).OrderByDescending(m => m.Rate).ThenByDescending(m => m.Answers)
            .Select(m => (ExplanationMethod?)m.Method).FirstOrDefault();

        var al = alerts.ToList();
        var resolved = al.Where(x => x.Resolved is not null).ToList();
        double? hours = resolved.Count == 0 ? null : Math.Round(resolved.Average(x => (x.Resolved!.Value - x.Created).TotalHours), 1);

        var st = states.ToList();
        var mastered = st.Where(s => s.Status == MasteryStatus.Mastered).ToList();
        return new QualityReport(methods, best, al.Count - resolved.Count, resolved.Count, hours, mastered.Count, mastered.Count(s => s.MethodsTried.Count > 1));
    }
}
