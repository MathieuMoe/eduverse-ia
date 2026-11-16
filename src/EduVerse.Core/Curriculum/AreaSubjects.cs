using System.Collections.Concurrent;
using EduVerse.Core.Catalog;
using EduVerse.Core.Common;
using EduVerse.Core.Learning;

namespace EduVerse.Core.Curriculum;

/// <summary>Names of the catalog in the language of the student (they live in the resources of the web project).</summary>
public interface ICatalogNames
{
    string Area(Area area);

    string Level(Level level);

    string Topic(string topic);
}

/// <summary>Fallback names (the keys themselves), for tests and tools.</summary>
public sealed class KeyCatalogNames : ICatalogNames
{
    public string Area(Area area) => area.ToString();

    public string Level(Level level) => level.ToString();

    public string Topic(string topic) => topic;
}

/// <summary>
/// Every subject of the catalog (mathematics, physics, history, languages…) at a level, as a subject of the tutor:
/// its concepts are the topics of the question bank, and the exercises are generated and solved in code, so it works without AI.
/// </summary>
public static class AreaSubjects
{
    public const string Prefix = "area-";

    private static readonly ConcurrentDictionary<(Area, Level), IReadOnlyList<string>> TopicCache = new();

    public static string Key(Area area, Level level) => $"{Prefix}{area}-{level}";

    public static bool TryParse(string? key, out Area area, out Level level)
    {
        (area, level) = (default, default);
        if (key is null || !key.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return false;
        }

        var parts = key[Prefix.Length..].Split('-');
        return parts.Length == 2 && Enum.TryParse(parts[0], out area) && Enum.TryParse(parts[1], out level)
            && Enum.IsDefined(area) && Enum.IsDefined(level) && Catalog.Catalog.Available(area, level);
    }

    /// <summary>The topics of the bank of a subject and level, in the order they first appear.</summary>
    public static IReadOnlyList<string> Topics(Area area, Level level) => TopicCache.GetOrAdd((area, level), key =>
    {
        var topics = new List<string>();
        for (var seed = 0; seed < 240; seed++)
        {
            var q = QuestionBanks.Generate(key.Item1, key.Item2, 1 + seed % 3, unchecked(seed * 104729 + 17), TeachLanguage.English);
            if (!topics.Contains(q.Topic))
            {
                topics.Add(q.Topic);
            }
        }

        return topics.Take(Curriculum.Subject.MaxConcepts).ToList();
    });

    public static Subject Subject(Area area, Level level, ICatalogNames names, TeachLanguage language)
    {
        ArgumentNullException.ThrowIfNull(names);
        var concepts = Topics(area, level).Select(topic =>
        {
            // The method, shown with the explanation of a solved question of the topic
            var sample = QuestionBanks.ForTopic(area, level, topic, 1, 11, language);
            return new Concept(topic, names.Topic(topic), sample.Explanation, []);
        }).ToList();
        return new Subject(Key(area, level), names.Area(area) + " · " + names.Level(level), names.Area(area), concepts, true);
    }

    public static Exercise Exercise(Area area, Level level, string topic, int difficulty, Random random, TeachLanguage language)
    {
        ArgumentNullException.ThrowIfNull(random);
        var q = QuestionBanks.ForTopic(area, level, topic, difficulty, random.Next(), language);
        var hint = Lang.T(language, "Think about the method of this topic before choosing.", "Piensa en el método de este tema antes de elegir.", "Pense à la méthode de ce thème avant de choisir.", "Pensa en el mètode d'aquest tema abans de triar.");
        return new Exercise(Guid.NewGuid().ToString("N"), topic, q.Difficulty, q.Prompt, q.Options, q.CorrectIndex, q.Options.Select(_ => (string?)null).ToList(),
            hint, q.Explanation, q.Visual, q.Code);
    }
}
