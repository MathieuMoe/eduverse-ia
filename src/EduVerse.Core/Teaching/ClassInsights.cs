using EduVerse.Core.Curriculum;
using EduVerse.Core.Learning;

namespace EduVerse.Core.Teaching;

/// <summary>What the platform knows about one student of the class (first name only).</summary>
public sealed record StudentMastery(string StudentId, string Name, IReadOnlyDictionary<string, MasteryState> States, int OpenAlerts);

/// <summary>One wrong answer tagged with the misconception it reveals.</summary>
public sealed record Mistake(string StudentId, string ConceptKey, string Code);

public sealed record HeatCell(string ConceptKey, double? PKnown, MasteryStatus Status, int Attempts);

public sealed record HeatRow(string StudentId, string Name, IReadOnlyList<HeatCell> Cells, int Mastered, int Progress, bool AtRisk, int OpenAlerts);

/// <param name="Average">Mean probability of knowing the concept among the students who worked on it, or null if nobody did.</param>
public sealed record ConceptSummary(string ConceptKey, string Name, double? Average, int Mastered, int Struggling);

public sealed record MisconceptionCount(string Code, string ConceptKey, int Count, int Students);

/// <summary>The class at a glance for the teacher: heatmap students × concepts, concepts that resist, frequent mistakes.</summary>
public sealed record ClassReport(
    IReadOnlyList<HeatRow> Rows,
    IReadOnlyList<ConceptSummary> Concepts,
    IReadOnlyList<MisconceptionCount> Misconceptions,
    string? HardestConcept,
    int AverageProgress);

/// <summary>Computes the class follow-up in code (no AI): explainable numbers the teacher can trust.</summary>
public static class ClassInsights
{
    /// <summary>A concept is a struggle when the student has practised it this many times and is still below <see cref="StruggleBelow"/>.</summary>
    public const int StruggleAttempts = 3;

    public const double StruggleBelow = 0.4;

    public const int TopMisconceptions = 5;

    public static ClassReport Build(Subject subject, IReadOnlyList<StudentMastery> students, IReadOnlyList<Mistake> mistakes)
    {
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(students);
        ArgumentNullException.ThrowIfNull(mistakes);
        var concepts = subject.Ordered();

        var rows = students.Select(s =>
        {
            var cells = concepts.Select(c => s.States.TryGetValue(c.Key, out var st)
                ? new HeatCell(c.Key, st.PKnown, st.Status, st.Attempts)
                : new HeatCell(c.Key, null, MasteryStatus.NotStarted, 0)).ToList();
            var mastered = cells.Count(c => c.Status == MasteryStatus.Mastered);
            var struggling = cells.Any(IsStruggling);
            return new HeatRow(s.StudentId, s.Name, cells, mastered, concepts.Count == 0 ? 0 : (int)Math.Round(100.0 * mastered / concepts.Count),
                struggling || s.OpenAlerts > 0, s.OpenAlerts);
        })
        .OrderByDescending(r => r.AtRisk).ThenBy(r => r.Progress).ThenBy(r => r.Name, StringComparer.CurrentCulture).ToList();

        var summaries = concepts.Select(c =>
        {
            var cells = rows.Select(r => r.Cells.First(x => x.ConceptKey == c.Key)).Where(x => x.Attempts > 0).ToList();
            return new ConceptSummary(c.Key, c.Name, cells.Count == 0 ? null : Math.Round(cells.Average(x => x.PKnown ?? 0), 3),
                cells.Count(x => x.Status == MasteryStatus.Mastered), cells.Count(IsStruggling));
        }).ToList();

        var hardest = summaries.Where(s => s.Average is not null && s.Mastered < rows.Count)
            .OrderByDescending(s => s.Struggling).ThenBy(s => s.Average).Select(s => s.ConceptKey).FirstOrDefault();

        var known = concepts.Select(c => c.Key).ToHashSet();
        var ids = students.Select(s => s.StudentId).ToHashSet();
        var top = mistakes.Where(m => ids.Contains(m.StudentId) && known.Contains(m.ConceptKey) && !string.IsNullOrWhiteSpace(m.Code))
            .GroupBy(m => (m.Code, m.ConceptKey))
            .Select(g => new MisconceptionCount(g.Key.Code, g.Key.ConceptKey, g.Count(), g.Select(m => m.StudentId).Distinct().Count()))
            .OrderByDescending(m => m.Students).ThenByDescending(m => m.Count).ThenBy(m => m.Code, StringComparer.Ordinal)
            .Take(TopMisconceptions).ToList();

        var average = rows.Count == 0 ? 0 : (int)Math.Round(rows.Average(r => r.Progress));
        return new ClassReport(rows, summaries, top, hardest, average);
    }

    private static bool IsStruggling(HeatCell c) => c.Attempts >= StruggleAttempts && (c.PKnown ?? 0) < StruggleBelow;
}
