using EduVerse.Core.Catalog;
using EduVerse.Core.Common;

namespace EduVerse.Core.Tutor;

/// <summary>A topic at the level where it is taught.</summary>
public sealed record TopicRef(Level Level, string Topic);

/// <summary>What the detective concludes about a weak topic.</summary>
public enum RootVerdict
{
    /// <summary>The step below has not been checked enough yet: check it.</summary>
    Check,

    /// <summary>The step below is weak too: the root is there.</summary>
    Root,

    /// <summary>The step below is solid: the difficulty is in the topic itself.</summary>
    Here,
}

/// <summary>What is known of a topic: answers and how many right.</summary>
public sealed record TopicEvidence(int Answers, int Correct);

/// <summary>
/// The prerequisite detective: when a topic does not work, the cause is often one step below (fractions fail because division is weak).
/// The map of "what you need first" is written here by hand, for mathematics; the verdict is decided in code from the student's answers.
/// </summary>
public static class Prerequisites
{
    /// <summary>Answers needed on the step below before concluding anything.</summary>
    public const int MinAnswers = 4;

    /// <summary>A topic is "weak" under this share of right answers (with at least <see cref="MinAnswers"/> answers).</summary>
    public const int WeakPercent = 60;

    public const int SolidPercent = 75;

    private static readonly Dictionary<TopicRef, TopicRef> Map = new()
    {
        [new(Level.Primary1, "math.subtract")] = new(Level.Primary1, "math.add"),
        [new(Level.Primary2, "math.multiply")] = new(Level.Primary2, "math.add"),
        [new(Level.Primary2, "math.divide")] = new(Level.Primary2, "math.multiply"),
        [new(Level.Primary3, "math.fraction")] = new(Level.Primary2, "math.divide"),
        [new(Level.Primary3, "math.percent")] = new(Level.Primary3, "math.fraction"),
        [new(Level.Primary3, "math.area")] = new(Level.Primary2, "math.multiply"),
        [new(Level.Primary3, "math.perimeter")] = new(Level.Primary2, "math.add"),
        [new(Level.Secondary1, "math.integers")] = new(Level.Primary1, "math.subtract"),
        [new(Level.Secondary1, "math.proportion")] = new(Level.Primary3, "math.fraction"),
        [new(Level.Secondary1, "math.equations")] = new(Level.Secondary1, "math.integers"),
        [new(Level.Secondary1, "math.powers")] = new(Level.Primary2, "math.multiply"),
        [new(Level.Secondary2, "math.pythagoras")] = new(Level.Secondary1, "math.powers"),
        [new(Level.Secondary2, "math.systems")] = new(Level.Secondary1, "math.equations"),
        [new(Level.Secondary2, "math.slope")] = new(Level.Secondary1, "math.proportion"),
        [new(Level.Secondary2, "math.quadratic")] = new(Level.Secondary1, "math.equations"),
        [new(Level.HighSchool, "math.sequences")] = new(Level.Secondary1, "math.equations"),
        [new(Level.HighSchool, "math.logarithms")] = new(Level.Secondary1, "math.powers"),
        [new(Level.HighSchool, "math.derivatives")] = new(Level.Secondary2, "math.slope"),
        [new(Level.HighSchool, "math.trigonometry")] = new(Level.Secondary2, "math.pythagoras"),
        [new(Level.HighSchool, "math.probability")] = new(Level.Primary3, "math.fraction"),
    };

    public static IReadOnlyDictionary<TopicRef, TopicRef> All => Map;

    /// <summary>The step just below a topic, or null when the map has none.</summary>
    public static TopicRef? Of(Area area, Level level, string topic) =>
        area == Area.Mathematics && Map.TryGetValue(new(level, topic), out var below) ? below : null;

    /// <summary>The chain of steps below a topic, nearest first (at most 4).</summary>
    public static IReadOnlyList<TopicRef> Chain(Area area, Level level, string topic)
    {
        var chain = new List<TopicRef>();
        var current = Of(area, level, topic);
        while (current is not null && chain.Count < 4)
        {
            chain.Add(current);
            current = Of(area, current.Level, current.Topic);
        }

        return chain;
    }

    public static bool Weak(TopicEvidence? e) => e is { Answers: >= MinAnswers } && 100 * e.Correct < WeakPercent * e.Answers;

    public static RootVerdict Verdict(TopicEvidence? below) => below switch
    {
        null or { Answers: < MinAnswers } => RootVerdict.Check,
        _ when 100 * below.Correct >= SolidPercent * below.Answers => RootVerdict.Here,
        _ when 100 * below.Correct < WeakPercent * below.Answers => RootVerdict.Root,
        _ => RootVerdict.Check,
    };

    /// <summary>What the detective says (topic names already in the student's language).</summary>
    public static string Say(RootVerdict verdict, string topic, string below, TeachLanguage language) => verdict switch
    {
        RootVerdict.Root => Lang.T(language,
            $"The problem is probably not {topic}: it is {below}, one step below. Strengthen that first and {topic} will get easier.",
            $"Seguramente el problema no es {topic}: es {below}, un escalón más abajo. Refuerza eso primero y {topic} será más fácil.",
            $"Le problème n'est sans doute pas {topic} : c'est {below}, une marche plus bas. Renforce cela d'abord et {topic} deviendra plus facile.",
            $"Segurament el problema no és {topic}: és {below}, un esglaó més avall. Reforça això primer i {topic} serà més fàcil."),
        RootVerdict.Here => Lang.T(language,
            $"Your base is solid ({below}). The difficulty is in {topic} itself: practise it directly, in short sessions.",
            $"Tu base es sólida ({below}). La dificultad está en {topic}: practícalo directamente, en sesiones cortas.",
            $"Ta base est solide ({below}). La difficulté est dans {topic} : entraîne-toi directement, en séances courtes.",
            $"La teva base és sòlida ({below}). La dificultat és a {topic}: practica-ho directament, en sessions curtes."),
        _ => Lang.T(language,
            $"{topic} needs {below} first. Let's check that step with a few questions.",
            $"{topic} necesita antes {below}. Comprobemos ese escalón con unas preguntas.",
            $"{topic} a besoin de {below} d'abord. Vérifions cette marche avec quelques questions.",
            $"{topic} necessita abans {below}. Comprovem aquest esglaó amb unes preguntes."),
    };
}
