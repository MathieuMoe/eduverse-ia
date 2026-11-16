using EduVerse.Core.Common;

namespace EduVerse.Core.Catalog;

/// <summary>
/// Offline question banks for every subject and level: questions generated and solved in code (or built from checked facts),
/// so the platform works without AI and the AI is only needed for free topics.
/// </summary>
public static class QuestionBanks
{
    /// <summary>The same seed always gives the same question (used by duels: both students get the same test).</summary>
    public static QuizQuestion Generate(Area area, Level level, int difficulty, int seed, TeachLanguage language)
    {
        var rng = new Random(seed);
        var q = new Mc(rng, language, Catalog.Available(area, level) ? level : Catalog.Info(area).MinLevel, Math.Clamp(difficulty, 1, 3));
        return area switch
        {
            Area.Mathematics => MathBank.Generate(q),
            Area.Physics => PhysicsBank.Generate(q),
            Area.Chemistry => ChemistryBank.Generate(q),
            Area.Biology => BiologyBank.Generate(q),
            Area.Language => LanguageBank.Generate(q),
            Area.ForeignLanguage => ForeignLanguageBank.Generate(q),
            Area.History => HistoryBank.Generate(q),
            Area.Geography => GeographyBank.Generate(q),
            Area.Economics => EconomicsBank.Generate(q),
            _ => ProgrammingBank.Generate(q),
        };
    }

    /// <summary>A question of a given topic (for the daily review); after many tries, any question of the subject and level.</summary>
    public static QuizQuestion ForTopic(Area area, Level level, string topic, int difficulty, int seed, TeachLanguage language)
    {
        QuizQuestion? first = null;
        for (var i = 0; i < 150; i++)
        {
            var q = Generate(area, level, difficulty, unchecked(seed + i * 7919), language);
            first ??= q;
            if (q.Topic == topic)
            {
                return q;
            }
        }

        return first!;
    }

    /// <summary>Seed of question <paramref name="index"/> of a test: stable for a test, different from one question to the next.</summary>
    public static int Seed(int testSeed, int index, int difficulty) => unchecked(testSeed * 31 + index * 7919 + difficulty * 104729);

    public static TeachLanguage ForeignTarget(TeachLanguage interfaceLanguage) => ForeignLanguageBank.Target(interfaceLanguage);
}
