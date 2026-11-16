using EduVerse.Core.Common;

namespace EduVerse.Core.Catalog;

/// <summary>Builds multiple-choice questions: 4 distinct options, the right one placed at random, explanations in 4 languages.</summary>
public sealed class Mc(Random rng, TeachLanguage language, Level level, int difficulty)
{
    public Random Rng { get; } = rng;

    public TeachLanguage Language { get; } = language;

    public Level Level { get; } = level;

    public int Difficulty { get; } = difficulty;

    public string T(string text) => Text.Of(text).In(Language);

    public string F(string text, params object[] args) => Text.Of(text).Format(Language, args);

    public string N(double value, int decimals = 2) =>
        Math.Round(value, decimals).ToString("0.##########", Levels.Culture(Language));

    public int Between(int min, int max) => Rng.Next(min, max + 1);

    public T Pick<T>(IReadOnlyList<T> items) => items[Rng.Next(items.Count)];

    /// <summary>A simplified fraction ("3/4", "2", "-1/3").</summary>
    public static string Frac(long numerator, long denominator)
    {
        if (denominator < 0)
        {
            (numerator, denominator) = (-numerator, -denominator);
        }

        var g = Gcd(Math.Abs(numerator), denominator);
        (numerator, denominator) = (numerator / g, denominator / g);
        return denominator == 1 ? numerator.ToString(System.Globalization.CultureInfo.InvariantCulture) : $"{numerator}/{denominator}";
    }

    public static long Gcd(long a, long b) => b == 0 ? Math.Max(1, a) : Gcd(b, a % b);

    /// <summary>Signed term for equations: " + 3x", " − 2x".</summary>
    public static string Term(long coefficient, string variable, bool first = false)
    {
        var abs = Math.Abs(coefficient);
        var body = (abs == 1 && variable.Length > 0 ? "" : abs.ToString(System.Globalization.CultureInfo.InvariantCulture)) + variable;
        return first ? (coefficient < 0 ? "−" : "") + body : (coefficient < 0 ? " − " : " + ") + body;
    }

    /// <summary>Text answer chosen among topic generators: easier questions use the first ones.</summary>
    public Func<Mc, QuizQuestion> Topic(params Func<Mc, QuizQuestion>[] generators)
    {
        ArgumentNullException.ThrowIfNull(generators);
        var count = Difficulty <= 1 && generators.Length > 1 ? generators.Length - 1 : generators.Length;
        return generators[Rng.Next(count)];
    }

    /// <summary>Numeric answer: distractors from typical mistakes, completed with close values.</summary>
    public QuizQuestion Number(string topic, string prompt, double answer, IEnumerable<double> mistakes, string explanation, string? visual = null, string? code = null, string unit = "", int decimals = 2)
    {
        var right = Math.Round(answer, decimals);
        var options = new List<double> { right };
        foreach (var m in mistakes.Select(m => Math.Round(m, decimals)))
        {
            if (options.Count < QuizQuestion.Choices && !options.Contains(m) && !double.IsNaN(m) && !double.IsInfinity(m))
            {
                options.Add(m);
            }
        }

        var step = Math.Abs(right) >= 20 ? Math.Max(1, Math.Round(Math.Abs(right) / 10)) : Math.Abs(right) < 1 && right != 0 && decimals > 0 ? 0.1 : 1;
        for (var k = 1; options.Count < QuizQuestion.Choices; k++)
        {
            foreach (var candidate in new[] { right + k * step, right - k * step })
            {
                if (options.Count < QuizQuestion.Choices && !options.Contains(Math.Round(candidate, decimals)) && (candidate >= 0 || right < 0))
                {
                    options.Add(Math.Round(candidate, decimals));
                }
            }
        }

        var texts = options.Select(o => N(o, decimals) + unit).ToList();
        return Choice(topic, prompt, texts[0], texts.Skip(1), explanation, visual, code);
    }

    /// <summary>Text answer with distractors (duplicates and the answer itself are skipped).</summary>
    public QuizQuestion Choice(string topic, string prompt, string answer, IEnumerable<string> distractors, string explanation, string? visual = null, string? code = null)
    {
        var options = new List<string> { answer };
        foreach (var d in distractors.OrderBy(_ => Rng.Next()))
        {
            if (options.Count < QuizQuestion.Choices && !options.Contains(d, StringComparer.OrdinalIgnoreCase))
            {
                options.Add(d);
            }
        }

        if (options.Count < QuizQuestion.Choices)
        {
            throw new InvalidOperationException($"Not enough distinct options for {topic}");
        }

        var shuffled = options.OrderBy(_ => Rng.Next()).ToList();
        return new QuizQuestion(topic, Difficulty, prompt, shuffled, shuffled.IndexOf(answer), explanation, visual, code);
    }
}
