using EduVerse.Core.Curriculum;

namespace EduVerse.Core.Learning;

public enum StepKind
{
    Diagnostic,
    Learn,
    Practice,
    Review,
    Done,
}

/// <param name="Reason">Code explaining the choice (shown to the student, translated by the interface).</param>
public sealed record NextStep(StepKind Kind, string? ConceptKey, string Reason, ExplanationMethod Method, int Difficulty);

/// <summary>Decides what the student does next, in code: placement test, learn, practise, review a prerequisite or a concept learned long ago.</summary>
public static class NextStepPlanner
{
    public static readonly TimeSpan ReviewAfter = TimeSpan.FromDays(7);

    /// <summary>A prerequisite is revisited at most once in this period while a concept resists.</summary>
    public static readonly TimeSpan PrerequisiteRefresh = TimeSpan.FromDays(1);

    /// <summary>A concept "resists" after this many answers with a probability still under <see cref="StrugglingBelow"/>.</summary>
    public const int StrugglingAttempts = 3;

    public const double StrugglingBelow = 0.4;

    public static NextStep Plan(Subject subject, IReadOnlyDictionary<string, MasteryState> states, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(states);
        if (states.Count == 0)
        {
            return new NextStep(StepKind.Diagnostic, null, "FirstTime", ExplanationMethod.StepByStep, 2);
        }

        MasteryState State(string key) => states.TryGetValue(key, out var s) ? s : MasteryState.New(key);

        // Spaced review: a mastered concept not practised for a week
        var stale = subject.Ordered().Select(c => State(c.Key))
            .FirstOrDefault(s => s.Status == MasteryStatus.Mastered && s.LastAttemptAt is { } last && now - last > ReviewAfter);
        if (stale is not null)
        {
            return new NextStep(StepKind.Review, stale.ConceptKey, "SpacedReview", stale.Method, 3);
        }

        foreach (var concept in subject.Ordered())
        {
            var s = State(concept.Key);
            if (s.Status == MasteryStatus.Mastered)
            {
                continue;
            }

            // The concept resists: revisit its weakest prerequisite (not practised today), then come back
            if (s.Attempts >= StrugglingAttempts && s.PKnown < StrugglingBelow)
            {
                var weak = concept.Prerequisites.Select(State)
                    .Where(p => p.LastAttemptAt is not { } last || now - last > PrerequisiteRefresh)
                    .OrderBy(p => p.PKnown).FirstOrDefault();
                if (weak is not null)
                {
                    return new NextStep(StepKind.Review, weak.ConceptKey, "ReviewPrerequisite", weak.Method, 2);
                }
            }

            // New concept, or a new way of explaining it after mistakes: explain first, then practise
            return s.NeedsExplanation
                ? new NextStep(StepKind.Learn, concept.Key, s.Attempts == 0 ? "NewConcept" : "NewMethod", s.Method, 1)
                : new NextStep(StepKind.Practice, concept.Key, s.Status == MasteryStatus.Practising ? "AlmostThere" : "KeepPractising", s.Method, MasteryEngine.NextDifficulty(s));
        }

        return new NextStep(StepKind.Done, null, "AllMastered", ExplanationMethod.StepByStep, 3);
    }
}
