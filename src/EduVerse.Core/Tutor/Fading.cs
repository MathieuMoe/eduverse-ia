using System.Globalization;
using EduVerse.Core.Catalog;
using EduVerse.Core.Common;

namespace EduVerse.Core.Tutor;

/// <summary>The kinds of problem that can be taught with fading examples (each one has two or three steps with a number as result).</summary>
public enum FadingKind
{
    Order,
    Percent,
    Mean,
    Perimeter,
    Discount,
    Equation,
}

/// <summary>One step of a worked example: what is computed and its result.</summary>
public sealed record FadingStep(string Work, int Result);

public sealed record FadingProblem(FadingKind Kind, string Problem, IReadOnlyList<FadingStep> Steps)
{
    public int Answer => Steps[^1].Result;
}

/// <summary>What the student sees of a step at a stage: the work (or nothing) and the result (or a blank to fill).</summary>
public sealed record FadingLine(string? Work, int? Result);

/// <summary>
/// Fading examples (worked examples with decreasing help): first the Tutor solves the whole problem, then the last result is missing,
/// then every result, and at the end the student is alone. Every problem and every check is computed in code.
/// </summary>
public static class Fading
{
    public const int Stages = 4;

    public const int Xp = 15;

    public const int RewardedPerDay = 4;

    public static bool Available(Level level) => level >= Level.Primary3 && level <= Level.HighSchool;

    public static IReadOnlyList<FadingKind> Kinds(Level level) => level <= Level.Primary3
        ? [FadingKind.Order, FadingKind.Percent, FadingKind.Mean, FadingKind.Perimeter]
        : [FadingKind.Order, FadingKind.Percent, FadingKind.Mean, FadingKind.Discount, FadingKind.Equation];

    public static string Name(FadingKind kind, TeachLanguage language) => kind switch
    {
        FadingKind.Order => Lang.T(language, "Order of operations", "Orden de las operaciones", "Priorité des opérations", "Ordre de les operacions"),
        FadingKind.Percent => Lang.T(language, "Percentage of a number", "Porcentaje de un número", "Pourcentage d'un nombre", "Percentatge d'un nombre"),
        FadingKind.Mean => Lang.T(language, "Mean of some numbers", "Media de varios números", "Moyenne de plusieurs nombres", "Mitjana de diversos nombres"),
        FadingKind.Perimeter => Lang.T(language, "Perimeter of a rectangle", "Perímetro de un rectángulo", "Périmètre d'un rectangle", "Perímetre d'un rectangle"),
        FadingKind.Discount => Lang.T(language, "Price with a discount", "Precio con descuento", "Prix avec remise", "Preu amb descompte"),
        _ => Lang.T(language, "Equation ax + b = c", "Ecuación ax + b = c", "Équation ax + b = c", "Equació ax + b = c"),
    };

    /// <summary>The problem of a stage of a run: same kind, other numbers each time (stage and tries change the seed).</summary>
    public static FadingProblem Generate(FadingKind kind, int seed, int stage, int tries, TeachLanguage language) =>
        Generate(kind, unchecked((seed * 31) + (stage * 1009) + (tries * 7919)), language);

    public static FadingProblem Generate(FadingKind kind, int seed, TeachLanguage language)
    {
        var rng = new Random(seed);
        switch (kind)
        {
            case FadingKind.Order:
            {
                var (a, b, c) = (rng.Next(2, 10), rng.Next(2, 10), rng.Next(2, 10));
                return new(kind, $"{a} + {b} × {c}", [new($"{b} × {c}", b * c), new($"{a} + {b * c}", a + (b * c))]);
            }

            case FadingKind.Percent:
            {
                var p = new[] { 10, 20, 25, 50 }[rng.Next(4)];
                var n = 20 * rng.Next(2, 10);
                return new(kind, Lang.T(language, $"{p} % of {n}", $"{p} % de {n}", $"{p} % de {n}", $"{p} % de {n}"), [new($"{n} × {p}", n * p), new($"{n * p} ÷ 100", n * p / 100)]);
            }

            case FadingKind.Mean:
            {
                var mean = rng.Next(5, 15);
                var (x, y) = (rng.Next(1, 4), rng.Next(1, 4));
                int[] values = [mean - x, mean + x + y, mean - y];
                return new(kind, Lang.T(language, $"Mean of {string.Join(", ", values)}", $"Media de {string.Join(", ", values)}", $"Moyenne de {string.Join(", ", values)}", $"Mitjana de {string.Join(", ", values)}"),
                    [new(string.Join(" + ", values), values.Sum()), new($"{values.Sum()} ÷ 3", mean)]);
            }

            case FadingKind.Perimeter:
            {
                var (a, b) = (rng.Next(3, 13), rng.Next(3, 13));
                return new(kind, Lang.T(language, $"Perimeter of a rectangle {a} by {b}", $"Perímetro de un rectángulo de {a} por {b}", $"Périmètre d'un rectangle de {a} sur {b}", $"Perímetre d'un rectangle de {a} per {b}"),
                    [new($"{a} + {b}", a + b), new($"2 × {a + b}", 2 * (a + b))]);
            }

            case FadingKind.Discount:
            {
                var (n, price) = (rng.Next(2, 6), 10 * rng.Next(2, 9));
                var d = new[] { 10, 20, 50 }[rng.Next(3)];
                var total = n * price;
                return new(kind, Lang.T(language,
                        $"{n} items at {price} € each, with {d} % off", $"{n} artículos a {price} € cada uno, con un {d} % de descuento",
                        $"{n} articles à {price} € pièce, avec {d} % de remise", $"{n} articles a {price} € cadascun, amb un {d} % de descompte"),
                    [new($"{n} × {price}", total), new($"{total} × {d} ÷ 100", total * d / 100), new($"{total} − {total * d / 100}", total - (total * d / 100))]);
            }

            default:
            {
                var (a, x, b) = (rng.Next(2, 10), rng.Next(2, 13), rng.Next(1, 20));
                var c = (a * x) + b;
                return new(FadingKind.Equation, $"{a}x + {b} = {c}", [new($"{c} − {b}", c - b), new($"{c - b} ÷ {a}", x)]);
            }
        }
    }

    /// <summary>How many results the student must give at a stage.</summary>
    public static int Blanks(FadingProblem problem, int stage)
    {
        ArgumentNullException.ThrowIfNull(problem);
        return stage switch { <= 0 => 0, 1 => 1, 2 => problem.Steps.Count, _ => 1 };
    }

    /// <summary>What is shown at a stage. Stage 3 shows nothing: only the final answer is asked.</summary>
    public static IReadOnlyList<FadingLine> Lines(FadingProblem problem, int stage)
    {
        ArgumentNullException.ThrowIfNull(problem);
        var steps = problem.Steps;
        return stage switch
        {
            <= 0 => steps.Select(s => new FadingLine(s.Work, s.Result)).ToList(),
            1 => steps.Select((s, i) => new FadingLine(s.Work, i == steps.Count - 1 ? null : s.Result)).ToList(),
            // The work of a later step would give away the result of the one before: only the first is shown.
            2 => steps.Select((s, i) => new FadingLine(i == 0 ? s.Work : null, null)).ToList(),
            _ => [],
        };
    }

    /// <summary>The results the student must give at a stage, in order.</summary>
    public static IReadOnlyList<int> Expected(FadingProblem problem, int stage)
    {
        ArgumentNullException.ThrowIfNull(problem);
        return stage switch
        {
            <= 0 => [],
            2 => problem.Steps.Select(s => s.Result).ToList(),
            _ => [problem.Answer],
        };
    }

    /// <summary>Whether the answers (as typed) are the expected results. Spaces and a final unit sign are forgiven; nothing else.</summary>
    public static bool Check(FadingProblem problem, int stage, IReadOnlyList<string?>? answers)
    {
        var expected = Expected(problem, stage);
        if (answers is null || answers.Count != expected.Count)
        {
            return false;
        }

        for (var i = 0; i < expected.Count; i++)
        {
            var text = (answers[i] ?? "").Trim().TrimEnd('€', '%').Replace("x=", "", StringComparison.OrdinalIgnoreCase).Replace("x =", "", StringComparison.OrdinalIgnoreCase).Trim();
            if (text.Length > 12 || !int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var value) || value != expected[i])
            {
                return false;
            }
        }

        return true;
    }
}
