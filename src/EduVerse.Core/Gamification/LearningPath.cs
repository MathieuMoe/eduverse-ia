using EduVerse.Core.Abstractions;
using EduVerse.Core.Catalog;
using EduVerse.Core.Common;
using EduVerse.Core.Parsing;

namespace EduVerse.Core.Gamification;

public enum PathNodeKind
{
    Lesson,
    Chest,
    Boss,
}

public enum PathNodeState
{
    Locked,
    Open,
    Done,
}

/// <summary>One stop of the path: a short lesson on the unit's topic, a chest, or the unit's boss.</summary>
public sealed record PathNode(string Topic, int Unit, int Step, PathNodeKind Kind, PathNodeState State);

/// <summary>What the student has done on one unit (one topic of a subject and level).</summary>
public sealed record UnitProgress(int LessonsPassed, bool ChestOpened, bool BossBeaten, bool Legendary = false);

/// <summary>
/// The learning path (Duolingo-style): each topic of a subject and level is a unit of
/// lesson → lesson → chest → lesson → boss. Lessons are 5-question tests on the topic (the existing Focus mode), the
/// boss is a short adventure that starts at the guardian and asks only that topic at the hardest difficulty.
/// Everything is unlocked in order; the state is computed from what the student did, never stored twice.
/// </summary>
public static class LearningPath
{
    public const int LessonsPerUnit = 3;

    /// <summary>A lesson counts when at least 3 of its 5 answers are right (60 %).</summary>
    public static bool LessonPassed(int correct, int length) => length > 0 && correct * 5 >= length * 3;

    private static readonly PathNodeKind[] UnitSteps = [PathNodeKind.Lesson, PathNodeKind.Lesson, PathNodeKind.Chest, PathNodeKind.Lesson, PathNodeKind.Boss];

    public static IReadOnlyList<PathNode> Build(IReadOnlyList<string> topics, Func<string, UnitProgress> progress)
    {
        ArgumentNullException.ThrowIfNull(topics);
        ArgumentNullException.ThrowIfNull(progress);
        var nodes = new List<PathNode>();
        var open = false; // the first node not done is the open one; everything after it is locked
        for (var unit = 0; unit < topics.Count; unit++)
        {
            var p = progress(topics[unit]);
            var lesson = 0;
            for (var step = 0; step < UnitSteps.Length; step++)
            {
                var kind = UnitSteps[step];
                var done = kind switch
                {
                    PathNodeKind.Lesson => p.LessonsPassed > lesson++,
                    PathNodeKind.Chest => p.ChestOpened,
                    _ => p.BossBeaten,
                };
                var state = done && !open ? PathNodeState.Done : open ? PathNodeState.Locked : PathNodeState.Open;
                open |= state == PathNodeState.Open;
                nodes.Add(new PathNode(topics[unit], unit, step, kind, state));
            }
        }

        return nodes;
    }

    /// <summary>The unit a student is on: the one holding the open node (or the last one when everything is done).</summary>
    public static int CurrentUnit(IReadOnlyList<PathNode> nodes) =>
        nodes.FirstOrDefault(n => n.State == PathNodeState.Open)?.Unit ?? (nodes.Count == 0 ? 0 : nodes[^1].Unit);

    /// <summary>Gems in a chest: 10–20, the same for the same student and unit (no luck to farm by reloading).</summary>
    public static int ChestGems(string userId, string chestKey)
    {
        ArgumentNullException.ThrowIfNull(userId);
        var hash = 2166136261;
        foreach (var c in userId + "|" + chestKey)
        {
            hash = (hash ^ c) * 16777619;
        }

        return 10 + (int)(hash % 11);
    }

    public static string ChestKey(Area area, Level level, string topic) => $"chest:{area}:{level}:{topic}";

    /// <summary>The attempt topic that marks an adventure as the boss of a unit.</summary>
    public static string BossTopic(string topic) => "unit:" + topic;

    /// <summary>The attempt topic of the legendary challenge of a unit (after its boss is beaten).</summary>
    public static string LegendTopic(string topic) => "legend:" + topic;

    public static bool IsLegend(string? attemptTopic) => attemptTopic?.StartsWith("legend:", StringComparison.Ordinal) == true;

    /// <summary>Any adventure of the path (boss or legendary): straight to the guardian, only the unit's topic, no drills.</summary>
    public static bool IsBoss(string? attemptTopic) => attemptTopic?.StartsWith("unit:", StringComparison.Ordinal) == true || IsLegend(attemptTopic);

    public static string UnitOf(string attemptTopic)
    {
        ArgumentNullException.ThrowIfNull(attemptTopic);
        return IsLegend(attemptTopic) ? attemptTopic[7..] : IsBoss(attemptTopic) ? attemptTopic[5..] : attemptTopic;
    }

    /// <summary>Legendary: one heart only, one more blow to land, and a price in gems (lost if the student fails).</summary>
    public const int LegendHearts = 1;

    public const int LegendExtraBlows = 1;

    public const int LegendPrice = 40;
}

/// <summary>
/// The boss of a unit: the AI only writes the guardian (name, entrance, one taunt per blow, victory and defeat); the
/// questions, the hearts and the blows are decided in code, exactly as in an adventure.
/// </summary>
public static class BossPrompt
{
    public const string ToolName = "unit_boss";

    public const int Lines = 5;

    public const string Schema = """
    {
      "type": "object",
      "properties": {
        "title": { "type": "string" },
        "intro": { "type": "string", "description": "1-2 sentences: the guardian blocks the way and challenges the hero on the topic." },
        "boss": { "type": "string", "description": "Name of a funny, theatrical (never scary) villain." },
        "bossIntro": { "type": "string" },
        "taunts": { "type": "array", "items": { "type": "string" }, "minItems": 5, "maxItems": 5, "description": "One line per question: the villain boasts, then gets more and more worried as it loses." },
        "victory": { "type": "string", "description": "The villain admits defeat with humour." },
        "defeat": { "type": "string", "description": "Encouraging: the villain invites the hero to come back." }
      },
      "required": ["title", "intro", "boss", "bossIntro", "taunts", "victory", "defeat"]
    }
    """;

    public static StructuredPrompt Build(Area area, Level level, string topicName, TeachLanguage language)
    {
        var system = $"""
            You write the villain of a "unit boss" in a learning game, for {Levels.PromptName(level)}.
            The villain guards the topic "{topicName}" of {area}; the hero beats it by answering questions (do not write the questions).
            Theatrical and funny like a pantomime villain: it brags, puns on the topic, and gets dramatically worried as it loses.
            No violence, no fear, no insults, no real people, no brands or known characters. Short sentences (at most 25 words each).
            Write everything in {Lang.Name(language)}. Call the tool {ToolName}.
            """;
        return new StructuredPrompt(system, $"Subject: {area}. Topic: {topicName}.", ToolName, "Returns the villain.", Schema, 1200);
    }

    public static Story Parse(System.Text.Json.JsonElement json)
    {
        var taunts = Json.List(json, "taunts", Lines, 200);
        var story = new Story(Json.Text(json, "title", 80), Json.Text(json, "intro", 300), taunts, Json.Text(json, "boss", 60),
            Json.Text(json, "bossIntro", 300), Json.Text(json, "victory", 300), Json.Text(json, "defeat", 300));
        if (story.Title.Length == 0 || story.Boss.Length == 0 || taunts.Count != Lines || taunts.Any(t => t.Length == 0) || story.Victory.Length == 0 || story.Defeat.Length == 0)
        {
            throw new AiGenerationException("The AI did not return a complete villain.");
        }

        return story;
    }
}

/// <summary>Villains written in advance, for the offline mode: the topic's name is woven into every line.</summary>
public static class DemoBosses
{
    private static readonly (string Name, string Emoji)[] Villains =
    [
        ("Count Forgetful|el Conde Olvidón|le Comte Oublitout|el Comte Oblidó", "🧛"),
        ("Professor Muddle|la Profesora Lío|la Professeure Embrouille|la Professora Embolic", "🧙"),
        ("Baron Blunder|el Barón Patinazo|le Baron Bévue|el Baró Relliscada", "🤡"),
        ("Lady Riddle|Lady Acertijo|Dame Énigme|Dama Endevinalla", "🦹"),
    ];

    public static Story Write(string topicName, string userSeed, TeachLanguage language)
    {
        ArgumentNullException.ThrowIfNull(userSeed);
        string T(string text) => Catalog.Text.Of(text).In(language);
        string F(string text, params object[] args) => string.Format(Levels.Culture(language), T(text), args);
        var villain = Villains[(int)((uint)StableHash(userSeed + topicName) % Villains.Length)];
        var name = T(villain.Name);
        return new Story(
            F("The guardian of {0}|El guardián de {0}|Le gardien de {0}|El guardià de {0}", topicName),
            F("{0} blocks the way: «Nobody passes without mastering {1}!»|{0} bloquea el camino: «¡Nadie pasa sin dominar {1}!»|{0} bloque le chemin : « Personne ne passe sans maîtriser {1} ! »|{0} bloqueja el camí: «Ningú no passa sense dominar {1}!»", name, topicName),
            [
                F("«Ha! {0} is my specialty. You'll never get this one!»|«¡Ja! {0} es mi especialidad. ¡Esta no la sacas!»|« Ha ! {0}, c'est ma spécialité. Celle-là, tu ne l'auras jamais ! »|«Ha! {0} és la meva especialitat. Aquesta no la treus!»", topicName),
                T("«Lucky guess… I'm sure of it. Try this!»|«Ha sido suerte… seguro. ¡Prueba esta!»|« Coup de chance… j'en suis sûr. Essaie celle-ci ! »|«Ha estat sort… segur. Prova aquesta!»"),
                T("«Wait, wait! Who taught you that?»|«¡Espera, espera! ¿Quién te ha enseñado eso?»|« Attends, attends ! Qui t'a appris ça ? »|«Espera, espera! Qui t'ha ensenyat això?»"),
                T("«My cape is trembling… This is my hardest question!»|«Me tiembla la capa… ¡Esta es mi pregunta más difícil!»|« Ma cape tremble… C'est ma question la plus difficile ! »|«Em tremola la capa… Aquesta és la meva pregunta més difícil!»"),
                T("«One last trick up my sleeve… surely!»|«Un último truco en la manga… ¡seguro!»|« Un dernier tour dans ma manche… sûrement ! »|«Un últim truc a la màniga… segur!»"),
            ],
            name,
            F("{0} {1} rubs its hands: «Four questions on {2}. Ready to lose?»|{0} {1} se frota las manos: «Cuatro preguntas de {2}. ¿Listo para perder?»|{0} {1} se frotte les mains : « Quatre questions sur {2}. Prêt à perdre ? »|{0} {1} es frega les mans: «Quatre preguntes de {2}. A punt per perdre?»", villain.Emoji, name, topicName),
            F("«Noooo! You really master {0}. Fine, the path is yours… for now!»|«¡Nooo! De verdad dominas {0}. Vale, el camino es tuyo… ¡por ahora!»|« Nooon ! Tu maîtrises vraiment {0}. Bon, le chemin est à toi… pour l'instant ! »|«Nooo! De debò domines {0}. D'acord, el camí és teu… de moment!»", topicName),
            T("«Ha ha! This time I win. Practise a little and come back: I'll be waiting!»|«¡Ja, ja! Esta vez gano yo. Practica un poco y vuelve: ¡te espero!»|« Ha ha ! Cette fois je gagne. Entraîne-toi un peu et reviens : je t'attends ! »|«Ha, ha! Aquesta vegada guanyo jo. Practica una mica i torna: t'espero!»"));
    }

    private static int StableHash(string text)
    {
        var hash = 17;
        foreach (var c in text)
        {
            hash = unchecked(hash * 31 + c);
        }

        return hash & 0x7fffffff;
    }
}
