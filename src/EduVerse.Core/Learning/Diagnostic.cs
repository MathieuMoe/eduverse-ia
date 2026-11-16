using EduVerse.Core.Curriculum;

namespace EduVerse.Core.Learning;

/// <summary>State of a placement test: the range of concepts still uncertain and the answers so far.</summary>
public sealed record DiagnosticState(IReadOnlyList<string> Order, int Low, int High, IReadOnlyDictionary<string, bool> Answers)
{
    public bool Finished => Low > High || Answers.Count >= Diagnostic.MaxQuestions(Order.Count);
}

/// <summary>
/// Adaptive placement test: a binary search along the concepts ordered by prerequisites.
/// A right answer means the earlier concepts are probably known; a wrong one means the later ones are probably not.
/// About log2(n) + 1 questions instead of testing everything.
/// </summary>
public static class Diagnostic
{
    public const double KnownPrior = 0.7;
    public const double ProbedCorrect = 0.8;
    public const double ProbedWrong = 0.1;

    public static int MaxQuestions(int concepts) => (int)Math.Ceiling(Math.Log2(Math.Max(2, concepts))) + 1;

    public static DiagnosticState Start(Subject subject)
    {
        ArgumentNullException.ThrowIfNull(subject);
        var order = subject.Ordered().Select(c => c.Key).ToList();
        return new DiagnosticState(order, 0, order.Count - 1, new Dictionary<string, bool>());
    }

    public static string? Next(DiagnosticState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.Finished ? null : state.Order[(state.Low + state.High) / 2];
    }

    public static DiagnosticState Answer(DiagnosticState state, string conceptKey, bool correct)
    {
        ArgumentNullException.ThrowIfNull(state);
        var i = state.Order.ToList().IndexOf(conceptKey);
        if (i < 0)
        {
            return state;
        }

        var answers = new Dictionary<string, bool>(state.Answers) { [conceptKey] = correct };
        return correct ? state with { Low = i + 1, Answers = answers } : state with { High = i - 1, Answers = answers };
    }

    /// <summary>Starting probability of knowing each concept, and the first concept to work on.</summary>
    public static (IReadOnlyDictionary<string, double> Priors, string? Start) Result(DiagnosticState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var priors = new Dictionary<string, double>();
        var lastKnown = state.Answers.Where(a => a.Value).Select(a => state.Order.ToList().IndexOf(a.Key)).DefaultIfEmpty(-1).Max();
        var firstUnknown = state.Answers.Where(a => !a.Value).Select(a => state.Order.ToList().IndexOf(a.Key)).DefaultIfEmpty(state.Order.Count).Min();
        for (var i = 0; i < state.Order.Count; i++)
        {
            var key = state.Order[i];
            priors[key] = state.Answers.TryGetValue(key, out var ok) ? (ok ? ProbedCorrect : ProbedWrong)
                : i < lastKnown ? KnownPrior
                : i > firstUnknown ? ProbedWrong
                : BktParameters.DefaultPrior;
        }

        var start = state.Order.FirstOrDefault(k => priors[k] < KnownPrior);
        return (priors, start);
    }
}
