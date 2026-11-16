namespace EduVerse.Core.Learning;

/// <summary>
/// The rules of the tutor that knows the student, in code:
/// update the mastery after each answer, spot recurring misconceptions, switch the explanation method when one does not work,
/// and ask for the human teacher when several methods have failed.
/// </summary>
public static class MasteryEngine
{
    /// <summary>Wrong answers in a row with the same method before trying another one.</summary>
    public const int FailuresBeforeSwitch = 2;

    /// <summary>Same misconception this many times: it is recurring.</summary>
    public const int RecurringMisconception = 2;

    /// <summary>Methods tried without success before the teacher is alerted.</summary>
    public const int MethodsBeforeTeacher = 3;

    public const int MinAttemptsForTeacher = 6;

    public static MasteryStatus StatusFor(double pKnown, int attempts) =>
        attempts == 0 ? MasteryStatus.NotStarted
        : pKnown >= BktParameters.MasteredThreshold ? MasteryStatus.Mastered
        : pKnown >= 0.6 ? MasteryStatus.Practising
        : MasteryStatus.Learning;

    public static (MasteryState State, IReadOnlyList<LearningEvent> Events) Record(MasteryState state, bool correct, int difficulty, string? misconception, DateTimeOffset at)
    {
        ArgumentNullException.ThrowIfNull(state);
        var events = new List<LearningEvent>();
        var wasMastered = state.Status == MasteryStatus.Mastered;
        var p = Bkt.Update(state.PKnown, correct, BktParameters.ForDifficulty(difficulty));

        var misconceptions = new Dictionary<string, int>(state.Misconceptions);
        if (!correct && !string.IsNullOrWhiteSpace(misconception))
        {
            misconceptions[misconception] = misconceptions.GetValueOrDefault(misconception) + 1;
            if (misconceptions[misconception] == RecurringMisconception)
            {
                events.Add(new LearningEvent(LearningEventKind.RecurringMisconception, state.ConceptKey, misconception));
            }
        }

        var failures = correct ? 0 : state.FailuresWithMethod + 1;
        var method = state.Method;
        var tried = state.MethodsTried.ToList();
        if (failures >= FailuresBeforeSwitch)
        {
            // Change the way of explaining: the next method this student has not tried yet on this concept
            var other = Enum.GetValues<ExplanationMethod>().FirstOrDefault(m => !tried.Contains(m), method);
            if (other != method)
            {
                method = other;
                tried.Add(other);
                failures = 0;
                events.Add(new LearningEvent(LearningEventKind.SwitchMethod, state.ConceptKey, other.ToString()));
            }
        }

        var attempts = state.Attempts + 1;
        var status = StatusFor(p, attempts);
        if (status == MasteryStatus.Mastered && !wasMastered)
        {
            events.Add(new LearningEvent(LearningEventKind.Mastered, state.ConceptKey));
        }

        // Several methods did not work: a human teacher should step in (once per concept).
        // Counted on the methods already used before this answer, not the one just switched to.
        var failed = state.MethodsTried;
        var alert = !correct && !state.TeacherAlerted && failed.Count >= MethodsBeforeTeacher && attempts >= MinAttemptsForTeacher && p < 0.6;
        if (alert)
        {
            events.Add(new LearningEvent(LearningEventKind.NeedsTeacher, state.ConceptKey, string.Join(",", failed)));
        }

        var next = state with
        {
            PKnown = p,
            Attempts = attempts,
            Correct = state.Correct + (correct ? 1 : 0),
            Streak = correct ? state.Streak + 1 : 0,
            FailuresWithMethod = failures,
            Method = method,
            MethodsTried = tried,
            Misconceptions = misconceptions,
            LastAttemptAt = at,
            Status = status,
            TeacherAlerted = state.TeacherAlerted || alert,
            NeedsExplanation = state.NeedsExplanation || method != state.Method,
        };
        return (next, events);
    }

    /// <summary>The student has read the explanation with the current method: practice comes next.</summary>
    public static MasteryState Explained(MasteryState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return state with { NeedsExplanation = false };
    }

    /// <summary>Difficulty of the next exercise: start easy, go up with a streak, go down after a mistake.</summary>
    public static int NextDifficulty(MasteryState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.Attempts == 0 || state.Streak == 0 && state.Attempts > 0 && state.PKnown < 0.4)
        {
            return 1;
        }

        return state.PKnown >= 0.75 || state.Streak >= 3 ? 3 : 2;
    }
}
