using EduVerse.Core.Common;

namespace EduVerse.Core.Catalog;

/// <summary>One step of the ladder: options taken away and, at the last step, the method with the answer hidden.</summary>
public sealed record Hint(int Level, IReadOnlyList<int> Removed, string? Text);

/// <summary>
/// Hints in a ladder, decided in code: 1 → one wrong option disappears, 2 → 50/50, 3 → the method with the answer hidden
/// (or its first character for a fact). The answer itself is never given; every hint lowers the XP of the question.
/// </summary>
public static class Hints
{
    public const int Max = 3;
    private const string Hidden = "▢";

    public static Hint For(QuizQuestion question, int level, int seed, TeachLanguage language)
    {
        ArgumentNullException.ThrowIfNull(question);
        level = Math.Clamp(level, 1, Max);
        var rng = new Random(seed);
        var wrong = Enumerable.Range(0, question.Options.Count).Where(i => i != question.CorrectIndex).OrderBy(_ => rng.Next()).ToList();
        var removed = wrong.Take(Math.Min(level, wrong.Count - 1)).ToList();
        return new Hint(level, removed, level >= Max ? Method(question, language) : null);
    }

    /// <summary>The explanation with the answer hidden; if almost nothing is left (a fact), the first character of the answer.</summary>
    public static string Method(QuizQuestion question, TeachLanguage language)
    {
        ArgumentNullException.ThrowIfNull(question);
        var answer = question.Answer.Trim();
        var masked = question.Explanation;
        if (answer.Length > 0)
        {
            masked = masked.Replace(answer, Hidden, StringComparison.OrdinalIgnoreCase);
            var bare = answer.Split(' ')[0];
            if (bare.Length >= 2 && bare != answer && char.IsDigit(bare[0]))
            {
                masked = masked.Replace(bare, Hidden, StringComparison.OrdinalIgnoreCase);
            }
        }

        var rest = masked.Replace(Hidden, "", StringComparison.Ordinal).Count(char.IsLetterOrDigit);
        if (answer.Length > 0 && (masked.Contains(answer, StringComparison.OrdinalIgnoreCase) || rest < 30))
        {
            return Text.Of("It starts with «{0}…».|Empieza por «{0}…».|Ça commence par « {0}… ».|Comença per «{0}…».").Format(language, answer[..1]);
        }

        return masked;
    }
}
