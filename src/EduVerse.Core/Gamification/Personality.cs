using System.Text.Json;
using EduVerse.Core.Abstractions;
using EduVerse.Core.Catalog;
using EduVerse.Core.Common;
using EduVerse.Core.Parsing;

namespace EduVerse.Core.Gamification;

/// <summary>How the mascot reacts to a finished lesson. The mood is decided in code; the AI only finds the words.</summary>
public enum MascotMood
{
    Goal,
    Perfect,
    Great,
    Good,
    Tough,
}

/// <summary>
/// The Tutor's reaction at the end of a lesson (phase 5 of the Duolingo pass): code picks the mood from the results,
/// the AI writes one short line in the Tutor's voice, and lines written in advance cover the offline mode.
/// </summary>
public static class Mascot
{
    public static MascotMood MoodOf(LessonEnd end)
    {
        ArgumentNullException.ThrowIfNull(end);
        return end.GoalJustReached ? MascotMood.Goal
            : end.Accuracy == 100 ? MascotMood.Perfect
            : end.Accuracy >= 70 ? MascotMood.Great
            : end.Accuracy >= 40 ? MascotMood.Good
            : MascotMood.Tough;
    }

    public static string Emoji(MascotMood mood) => mood switch
    {
        MascotMood.Goal => "🥳",
        MascotMood.Perfect => "🤩",
        MascotMood.Great => "😄",
        MascotMood.Good => "🙂",
        _ => "🤗",
    };

    private static readonly Dictionary<MascotMood, string[]> Lines = new()
    {
        [MascotMood.Goal] =
        [
            "Daily goal: done! Hoo-hoo! I'm doing my happy owl dance.|¡Meta diaria cumplida! ¡Uh-uh! Estoy haciendo mi baile de búho feliz.|Objectif du jour atteint ! Hou-hou ! Je fais ma danse de hibou heureux.|Objectiu diari assolit! Uh-uh! Faig el meu ball de mussol feliç.",
            "You kept your promise to yourself today. That's the real superpower.|Hoy has cumplido la promesa que te hiciste. Ese es el verdadero superpoder.|Aujourd'hui tu as tenu la promesse que tu t'étais faite. C'est ça, le vrai super-pouvoir.|Avui has complert la promesa que et vas fer. Aquest és el veritable superpoder.",
        ],
        [MascotMood.Perfect] =
        [
            "Not a single mistake! Are you secretly a teacher?|¡Ni un solo fallo! ¿Eres profe en secreto?|Pas une seule erreur ! Tu serais pas prof en secret ?|Ni una sola errada! Ets profe en secret?",
            "Perfect score. I'm writing your name in my golden notebook.|Puntuación perfecta. Apunto tu nombre en mi cuaderno dorado.|Score parfait. J'écris ton nom dans mon carnet doré.|Puntuació perfecta. Apunto el teu nom al meu quadern daurat.",
        ],
        [MascotMood.Great] =
        [
            "Great lesson! A couple more like this and that topic is yours.|¡Gran lección! Un par más así y ese tema es tuyo.|Super leçon ! Encore deux comme ça et ce sujet est à toi.|Gran lliçó! Un parell més així i aquest tema és teu.",
            "Your brain is warming up nicely. Keep going!|Tu cerebro está entrando en calor. ¡Sigue así!|Ton cerveau chauffe bien. Continue !|El teu cervell s'està escalfant. Continua així!",
        ],
        [MascotMood.Good] =
        [
            "Good work! The mistakes are where the learning happens.|¡Buen trabajo! Donde fallas es justo donde aprendes.|Bon travail ! C'est dans les erreurs qu'on apprend.|Bona feina! On t'equivoques és just on aprens.",
            "Halfway there and climbing. One more round?|Vas por la mitad y subiendo. ¿Otra ronda?|À mi-chemin et ça monte. Encore un tour ?|Vas per la meitat i pujant. Una altra ronda?",
        ],
        [MascotMood.Tough] =
        [
            "That was a tough one, and you finished it. I'm proud of you.|Ha sido difícil y aun así la has terminado. Estoy orgulloso de ti.|C'était dur, et tu l'as finie. Je suis fier de toi.|Ha estat difícil i tot i així l'has acabat. N'estic orgullós.",
            "Every expert started exactly here. Shall we review the tricky bits?|Todos los expertos empezaron justo aquí. ¿Repasamos lo difícil?|Tous les experts ont commencé exactement ici. On revoit les points difficiles ?|Tots els experts van començar just aquí. Repassem el que costa?",
        ],
    };

    public static string Offline(MascotMood mood, TeachLanguage language, int seed)
    {
        var lines = Lines[mood];
        return Catalog.Text.Of(lines[Math.Abs(seed % lines.Length)]).In(language);
    }

    public const string ToolName = "mascot_line";

    public const string Schema = """
    { "type": "object", "properties": { "line": { "type": "string", "description": "One or two short sentences, at most 30 words." } }, "required": ["line"] }
    """;

    /// <summary>Only numbers and the mood are sent: nothing the student wrote.</summary>
    public static StructuredPrompt Build(MascotMood mood, LessonEnd end, Level level, TeachLanguage language)
    {
        ArgumentNullException.ThrowIfNull(end);
        var system = $"""
            You are the Tutor, the friendly owl mascot of a learning game, talking to a student ({Levels.PromptName(level)}).
            Write ONE short reaction to the lesson they just finished (at most 30 words): warm, playful, specific to the facts given.
            Mood to convey: {mood}. Never mock, never pressure, no guilt. No emojis. Write in {Lang.Name(language)}. Call the tool {ToolName}.
            """;
        var facts = $"Right answers: {end.Correct}/{end.Length}. XP: {end.Xp}. Streak: {end.Streak} days (kept alive by this lesson: {end.StreakExtended}). Daily goal: {end.TodayXp}/{end.Goal} XP (just reached: {end.GoalJustReached}).";
        return new StructuredPrompt(system, facts, ToolName, "Returns the line.", Schema, 300);
    }

    public static string Parse(JsonElement json)
    {
        var line = Json.Text(json, "line", 240);
        return line.Length == 0 ? throw new AiGenerationException("The AI did not return a line.") : line;
    }
}

/// <summary>
/// "Give me a trick": a memory hook for a question the student just answered. The question and its right answer come
/// from the checked banks (never from the student); offline, a technique matched to the kind of topic.
/// </summary>
public static class Tricks
{
    public const string ToolName = "memory_trick";

    public const string Schema = """
    { "type": "object", "properties": { "trick": { "type": "string", "description": "A mnemonic, image, rhyme or rule of thumb, at most 45 words." } }, "required": ["trick"] }
    """;

    public static StructuredPrompt Build(string prompt, string answer, string topicName, Level level, TeachLanguage language)
    {
        var system = $"""
            You help a student ({Levels.PromptName(level)}) remember an answer with a memory trick: a vivid image, a rhyme,
            an acronym, a story or a rule of thumb. At most 45 words, fun, correct, age-appropriate. Do not just repeat the answer.
            Write in {Lang.Name(language)}. Call the tool {ToolName}.
            """;
        return new StructuredPrompt(system, $"Topic: {topicName}\nQuestion: {prompt}\nRight answer: {answer}", ToolName, "Returns the trick.", Schema, 400);
    }

    public static string Parse(JsonElement json)
    {
        var trick = Json.Text(json, "trick", 360);
        return trick.Length == 0 ? throw new AiGenerationException("The AI did not return a trick.") : trick;
    }

    /// <summary>A technique chosen by the kind of topic, filled with the real question and answer.</summary>
    public static string Offline(string topic, string prompt, string answer, TeachLanguage language)
    {
        ArgumentNullException.ThrowIfNull(topic);
        var kind = topic switch
        {
            _ when topic.Contains("date", StringComparison.Ordinal) || topic.Contains("chronology", StringComparison.Ordinal) => "date",
            _ when topic.StartsWith("math.", StringComparison.Ordinal) || topic.StartsWith("physics.", StringComparison.Ordinal) || topic.Contains("percent", StringComparison.Ordinal) => "steps",
            _ when topic.Contains("vocabulary", StringComparison.Ordinal) || topic.StartsWith("foreign.", StringComparison.Ordinal) => "image",
            _ => "link",
        };
        var text = kind switch
        {
            "date" => "Picture the answer as a big scoreboard: «{1}». Say it out loud three times while imagining the scene of the question: «{0}».|Imagina la respuesta en un gran marcador: «{1}». Dila en voz alta tres veces mientras imaginas la escena de la pregunta: «{0}».|Imagine la réponse sur un grand tableau d'affichage : « {1} ». Dis-la trois fois à voix haute en imaginant la scène de la question : « {0} ».|Imagina la resposta en un gran marcador: «{1}». Digues-la tres vegades en veu alta mentre imagines l'escena de la pregunta: «{0}».",
            "steps" => "Turn it into a recipe: read the question («{0}»), write down the data, do one step per line, and check that you get «{1}». Next time, follow the same recipe.|Conviértelo en una receta: lee la pregunta («{0}»), apunta los datos, haz un paso por línea y comprueba que llegas a «{1}». La próxima vez, sigue la misma receta.|Transforme-le en recette : lis la question (« {0} »), note les données, fais une étape par ligne et vérifie que tu arrives à « {1} ». La prochaine fois, suis la même recette.|Converteix-ho en una recepta: llegeix la pregunta («{0}»), apunta les dades, fes un pas per línia i comprova que arribes a «{1}». La pròxima vegada, segueix la mateixa recepta.",
            "image" => "Draw a tiny, silly picture of «{1}» in the margin and label it. A funny image sticks far better than a list.|Dibuja en el margen un dibujito gracioso de «{1}» y ponle el nombre. Una imagen divertida se recuerda mucho mejor que una lista.|Dessine dans la marge un petit dessin rigolo de « {1} » et écris son nom. Une image drôle se retient bien mieux qu'une liste.|Dibuixa al marge un dibuixet divertit de «{1}» i posa-hi el nom. Una imatge divertida es recorda molt millor que una llista.",
            _ => "Link it to something you know: make up a short sentence that connects «{0}» with «{1}», the sillier the better, and say it out loud.|Únelo a algo que conozcas: inventa una frase corta que conecte «{0}» con «{1}» (cuanto más absurda, mejor) y dila en voz alta.|Relie-le à quelque chose que tu connais : invente une petite phrase qui relie « {0} » à « {1} » (plus c'est absurde, mieux c'est) et dis-la à voix haute.|Lliga-ho amb alguna cosa que coneguis: inventa una frase curta que connecti «{0}» amb «{1}» (com més absurda, millor) i digues-la en veu alta.",
        };
        var shortPrompt = prompt.Length > 90 ? prompt[..87] + "…" : prompt;
        return Catalog.Text.Of(text).Format(language, shortPrompt, answer);
    }
}

/// <summary>
/// "The story of your mistakes": once a week, a short story in which the hero meets exactly the topics the student
/// got wrong most. The topics are picked in code from the answer history; the AI (or a template offline) tells it.
/// </summary>
public static class MistakeStory
{
    public const string ToolName = "mistake_story";

    public const int MaxTopics = 3;

    public const string Schema = """
    {
      "type": "object",
      "properties": {
        "title": { "type": "string" },
        "story": { "type": "string", "description": "120-180 words. Each topic appears as a challenge the hero solves, with the right idea explained naturally." }
      },
      "required": ["title", "story"]
    }
    """;

    public static StructuredPrompt Build(IReadOnlyList<(string TopicName, string Question, string Answer)> items, Level level, TeachLanguage language)
    {
        ArgumentNullException.ThrowIfNull(items);
        var system = $"""
            You write a short adventure story (120-180 words) for a student ({Levels.PromptName(level)}).
            The hero solves one challenge per topic given, and each challenge explains the right idea clearly (use the right answers given).
            Kind, funny, age-appropriate: no violence, no real people, no brands or known characters. Address the reader as the hero ("you").
            Write in {Lang.Name(language)}. Call the tool {ToolName}.
            """;
        var facts = string.Join("\n", items.Select((x, i) => $"{i + 1}. Topic: {x.TopicName}. Example question: {x.Question} Right answer: {x.Answer}"));
        return new StructuredPrompt(system, facts, ToolName, "Returns the story.", Schema, 900);
    }

    public static (string Title, string Story) Parse(JsonElement json)
    {
        var (title, story) = (Json.Text(json, "title", 100), Json.Text(json, "story", 1600));
        return title.Length == 0 || story.Length < 80 ? throw new AiGenerationException("The AI did not return a story.") : (title, story);
    }

    public static (string Title, string Story) Offline(IReadOnlyList<(string TopicName, string Question, string Answer)> items, TeachLanguage language)
    {
        ArgumentNullException.ThrowIfNull(items);
        string T(string text) => Catalog.Text.Of(text).In(language);
        string F(string text, params object[] args) => Catalog.Text.Of(text).Format(language, args);
        var parts = new List<string>
        {
            T("You wake up in the Library of Lost Answers, where every book you once got wrong has come to life.|Te despiertas en la Biblioteca de las Respuestas Perdidas, donde cada libro que una vez fallaste ha cobrado vida.|Tu te réveilles dans la Bibliothèque des Réponses Perdues, où chaque livre que tu as raté un jour a pris vie.|Et despertes a la Biblioteca de les Respostes Perdudes, on cada llibre que un dia vas fallar ha pres vida."),
        };
        var doors = new[]
        {
            "The first book, «{0}», blocks the corridor and asks: «{1}» You remember: «{2}». The book bows and lets you pass.|El primer libro, «{0}», bloquea el pasillo y pregunta: «{1}» Lo recuerdas: «{2}». El libro hace una reverencia y te deja pasar.|Le premier livre, « {0} », bloque le couloir et demande : « {1} » Tu te souviens : « {2} ». Le livre s'incline et te laisse passer.|El primer llibre, «{0}», bloqueja el passadís i pregunta: «{1}» Ho recordes: «{2}». El llibre fa una reverència i et deixa passar.",
            "Next, a dusty volume of «{0}» whispers: «{1}» This time you answer «{2}», and its pages turn gold.|Después, un tomo polvoriento de «{0}» susurra: «{1}» Esta vez respondes «{2}» y sus páginas se vuelven doradas.|Ensuite, un vieux volume de « {0} » chuchote : « {1} » Cette fois tu réponds « {2} », et ses pages deviennent dorées.|Després, un volum polsós de «{0}» xiuxiueja: «{1}» Aquesta vegada respons «{2}» i les seves pàgines es tornen daurades.",
            "The last guardian, a giant encyclopedia of «{0}», thunders: «{1}» «{2}!», you shout, and the doors swing open.|El último guardián, una enciclopedia gigante de «{0}», truena: «{1}» «¡{2}!», gritas, y las puertas se abren de par en par.|Le dernier gardien, une encyclopédie géante de « {0} », tonne : « {1} » « {2} ! », cries-tu, et les portes s'ouvrent grand.|L'últim guardià, una enciclopèdia gegant de «{0}», trona: «{1}» «{2}!», crides, i les portes s'obren de bat a bat.",
        };
        for (var i = 0; i < Math.Min(items.Count, doors.Length); i++)
        {
            parts.Add(F(doors[i], items[i].TopicName, items[i].Question, items[i].Answer));
        }

        parts.Add(T("Outside, the sun is shining. The answers you lost are yours again — and this time, they're staying.|Fuera brilla el sol. Las respuestas que perdiste vuelven a ser tuyas, y esta vez se quedan.|Dehors, le soleil brille. Les réponses perdues sont de nouveau à toi — et cette fois, elles restent.|A fora brilla el sol. Les respostes que vas perdre tornen a ser teves, i aquesta vegada s'hi queden."));
        return (T("The Library of Lost Answers|La Biblioteca de las Respuestas Perdidas|La Bibliothèque des Réponses Perdues|La Biblioteca de les Respostes Perdudes"), string.Join(" ", parts));
    }
}
