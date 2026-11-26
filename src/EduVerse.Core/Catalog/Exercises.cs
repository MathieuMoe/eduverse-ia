using System.Globalization;
using System.Text;

namespace EduVerse.Core.Catalog;

/// <summary>The shape of one exercise. <see cref="MultipleChoice"/> is the classic 4-option question
/// (<see cref="QuizQuestion"/>); the rest are the Duolingo-style variety added on top of it.</summary>
public enum ExerciseKind
{
    MultipleChoice,
    TrueFalse,
    FillBlank,
    Reorder,
    Match,

    /// <summary>"Select all that apply": 2 or more of a short list of options are correct, picked by toggling rather
    /// than a single tap — a 5th answer shape, distinct from every kind above (task #96).</summary>
    MultiSelect,

    /// <summary>"Listen and answer": the prompt is spoken aloud instead of shown as text, and the student picks
    /// from the same 4 options a classic multiple-choice question would offer — a 6th answer shape built from the
    /// existing question generator (task #114), so it needs no curated content bank of its own and works for every
    /// subject, level and language the platform already has.</summary>
    Listen,
}

/// <summary>One left/right pair for a <see cref="ExerciseKind.Match"/> exercise (e.g. word ↔ translation,
/// term ↔ definition, date ↔ event).</summary>
public sealed record MatchPair(string Left, string Right);

/// <summary>
/// One exercise of a learning session. Every <see cref="ExerciseKind"/> shares <see cref="Topic"/>,
/// <see cref="Difficulty"/>, <see cref="Prompt"/> and <see cref="Explanation"/>; only the fields for its
/// own <see cref="Kind"/> are set. Code decides what counts as correct (the <c>Check*</c> methods below);
/// nothing here depends on AI.
/// </summary>
public sealed record PracticeExercise(
    string Topic,
    int Difficulty,
    ExerciseKind Kind,
    string Prompt,
    string Explanation,
    // MultipleChoice
    IReadOnlyList<string>? Options = null,
    int? CorrectIndex = null,
    // TrueFalse
    bool? CorrectBool = null,
    // FillBlank — Answer is the reference text; AcceptedAlternatives are other wordings that also count
    string? Answer = null,
    IReadOnlyList<string>? AcceptedAlternatives = null,
    // Reorder — Tokens are shown shuffled; CorrectOrder is the same tokens in the right order
    IReadOnlyList<string>? Tokens = null,
    IReadOnlyList<string>? CorrectOrder = null,
    // Match
    IReadOnlyList<MatchPair>? Pairs = null,
    string? Visual = null,
    // MultiSelect — Options are the candidates (reusing the same field MultipleChoice uses); CorrectIndices are the
    // ones that must all be picked, and only those, for a correct answer.
    IReadOnlyList<int>? CorrectIndices = null)
{
    /// <summary>Wraps an existing multiple-choice question as a <see cref="PracticeExercise"/>, unchanged.</summary>
    public static PracticeExercise FromQuizQuestion(QuizQuestion q) => new(
        q.Topic, q.Difficulty, ExerciseKind.MultipleChoice, q.Prompt, q.Explanation,
        Options: q.Options, CorrectIndex: q.CorrectIndex, Visual: q.Visual);

    /// <summary>Normalizes text for a fill-in-the-blank comparison: trims, lower-cases and strips accents,
    /// so "Café", "cafe" and " CAFE " all match.</summary>
    public static string Normalize(string text)
    {
        var folded = text.Trim().ToLowerInvariant();
        var decomposed = folded.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    public bool CheckFillBlank(string given)
    {
        if (Kind != ExerciseKind.FillBlank || Answer is null)
        {
            throw new InvalidOperationException($"{nameof(CheckFillBlank)} needs a {ExerciseKind.FillBlank} exercise.");
        }

        var candidate = Normalize(given ?? "");
        return candidate == Normalize(Answer) ||
            (AcceptedAlternatives?.Any(a => Normalize(a) == candidate) ?? false);
    }

    public bool CheckReorder(IReadOnlyList<string> given)
    {
        if (Kind != ExerciseKind.Reorder || CorrectOrder is null)
        {
            throw new InvalidOperationException($"{nameof(CheckReorder)} needs a {ExerciseKind.Reorder} exercise.");
        }

        return given is not null && given.SequenceEqual(CorrectOrder);
    }

    public bool CheckMatch(IReadOnlyDictionary<string, string> given)
    {
        if (Kind != ExerciseKind.Match || Pairs is null)
        {
            throw new InvalidOperationException($"{nameof(CheckMatch)} needs a {ExerciseKind.Match} exercise.");
        }

        return given is not null && Pairs.Count == given.Count &&
            Pairs.All(p => given.TryGetValue(p.Left, out var right) && right == p.Right);
    }

    public bool CheckTrueFalse(bool given)
    {
        if (Kind != ExerciseKind.TrueFalse || CorrectBool is null)
        {
            throw new InvalidOperationException($"{nameof(CheckTrueFalse)} needs a {ExerciseKind.TrueFalse} exercise.");
        }

        return given == CorrectBool;
    }

    /// <summary>Exact match required: every correct option picked, and no incorrect one — order doesn't matter.</summary>
    public bool CheckMultiSelect(IReadOnlyList<int> given)
    {
        if (Kind != ExerciseKind.MultiSelect || CorrectIndices is null)
        {
            throw new InvalidOperationException($"{nameof(CheckMultiSelect)} needs a {ExerciseKind.MultiSelect} exercise.");
        }

        return given is not null && given.ToHashSet().SetEquals(CorrectIndices);
    }

    /// <summary>Same check as a classic multiple-choice answer — <see cref="Listen"/> reuses the same Options/CorrectIndex
    /// shape, just spoken instead of shown.</summary>
    public bool CheckListen(int given)
    {
        if (Kind != ExerciseKind.Listen || CorrectIndex is null)
        {
            throw new InvalidOperationException($"{nameof(CheckListen)} needs a {ExerciseKind.Listen} exercise.");
        }

        return given == CorrectIndex;
    }
}
