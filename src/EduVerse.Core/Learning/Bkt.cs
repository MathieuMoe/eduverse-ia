namespace EduVerse.Core.Learning;

/// <param name="Transit">Chance of learning the concept at each practice opportunity.</param>
/// <param name="Slip">Chance of a wrong answer although the concept is known.</param>
/// <param name="Guess">Chance of a right answer without knowing it (4 options: about 0.25).</param>
public sealed record BktParameters(double Transit, double Slip, double Guess)
{
    public const double DefaultPrior = 0.15;
    public const double MasteredThreshold = 0.95;

    public static readonly BktParameters Default = new(0.15, 0.10, 0.22);

    /// <summary>Harder exercises: a right answer says more (less guessing), a wrong one says less (more slips).</summary>
    public static BktParameters ForDifficulty(int difficulty) => difficulty switch
    {
        <= 1 => new(0.12, 0.08, 0.25),
        2 => Default,
        _ => new(0.18, 0.15, 0.15),
    };
}

/// <summary>
/// Bayesian Knowledge Tracing: the probability that the student knows a concept, updated after each answer.
/// A classic, explainable model of learning (Corbett and Anderson, 1994), computed in code, not by the AI.
/// </summary>
public static class Bkt
{
    public static double Update(double pKnown, bool correct, BktParameters p)
    {
        ArgumentNullException.ThrowIfNull(p);
        var posterior = correct
            ? pKnown * (1 - p.Slip) / (pKnown * (1 - p.Slip) + (1 - pKnown) * p.Guess)
            : pKnown * p.Slip / (pKnown * p.Slip + (1 - pKnown) * (1 - p.Guess));
        var next = posterior + (1 - posterior) * p.Transit;
        return Math.Round(Math.Clamp(next, 0.001, 0.999), 4);
    }
}
