using System.Text.Json;
using EduVerse.Core.Abstractions;
using EduVerse.Core.Catalog;
using EduVerse.Core.Common;
using EduVerse.Core.Parsing;

namespace EduVerse.Core.Tutor;

/// <summary>The typical mistakes Pip (the student-AI) can make. Each one is injected by code, so the code always knows which line is wrong and why.</summary>
public enum PipMistake
{
    Carry,
    Borrow,
    Order,
    FractionAdd,
    Percent,
    Area,
    Mean,
    Units,
    Speed,
    Agreement,
    Past,
    Capital,
}

/// <summary>Which kind of problems Pip brings.</summary>
public enum PipSubject
{
    Maths,
    Language,
}

/// <summary>
/// A problem solved by Pip with one mistake: <see cref="Shown"/> are the lines the student sees, <see cref="Right"/> the correct ones,
/// <see cref="WrongStep"/> the first line that is wrong. The student must find that line and say what went wrong (one of <see cref="Reasons"/>).
/// </summary>
public sealed record PipProblem(string Problem, IReadOnlyList<string> Shown, IReadOnlyList<string> Right, int WrongStep, PipMistake Mistake, IReadOnlyList<string> Reasons, int ReasonIndex, string Fix);

/// <summary>"Correct Pip": the student teaches by finding the mistake of an AI student. Everything is decided in code (offline too).</summary>
public static class Pip
{
    public const int Xp = 10;

    public const int RewardedPerDay = 6;

    public const int Reasons = 3;

    /// <summary>Whether the level has problems (from the second year of primary).</summary>
    public static bool Available(Level level) => level >= Level.Primary2 && level <= Level.HighSchool;

    public static IReadOnlyList<PipMistake> Kinds(Level level, PipSubject subject = PipSubject.Maths) => subject == PipSubject.Language
        ? [PipMistake.Agreement, PipMistake.Past, PipMistake.Capital]
        : MathKinds(level);

    private static IReadOnlyList<PipMistake> MathKinds(Level level) => level switch
    {
        <= Level.Primary2 => [PipMistake.Carry, PipMistake.Borrow],
        Level.Primary3 => [PipMistake.Carry, PipMistake.Borrow, PipMistake.Order, PipMistake.Percent, PipMistake.Area, PipMistake.Mean],
        _ => [PipMistake.Order, PipMistake.FractionAdd, PipMistake.Percent, PipMistake.Mean, PipMistake.Units, PipMistake.Speed],
    };

    public static string Describe(PipMistake mistake, TeachLanguage language) => mistake switch
    {
        PipMistake.Carry => Lang.T(language, "Forgot to add the carry", "Olvidó sumar la llevada", "A oublié d'ajouter la retenue", "Ha oblidat sumar la que porta"),
        PipMistake.Borrow => Lang.T(language, "Subtracted the smaller digit from the bigger one instead of borrowing", "Restó la cifra menor de la mayor en vez de pedir prestado", "A soustrait le plus petit chiffre du plus grand au lieu d'emprunter", "Ha restat la xifra menor de la major en lloc de demanar emprestat"),
        PipMistake.Order => Lang.T(language, "Added before multiplying", "Sumó antes de multiplicar", "A additionné avant de multiplier", "Ha sumat abans de multiplicar"),
        PipMistake.FractionAdd => Lang.T(language, "Added numerators and denominators separately", "Sumó numeradores y denominadores por separado", "A additionné numérateurs et dénominateurs séparément", "Ha sumat numeradors i denominadors per separat"),
        PipMistake.Agreement => Lang.T(language, "The verb does not agree with the subject", "El verbo no concuerda con el sujeto", "Le verbe ne s'accorde pas avec le sujet", "El verb no concorda amb el subjecte"),
        PipMistake.Past => Lang.T(language, "Used the present for something that already happened", "Usó el presente para algo que ya pasó", "A utilisé le présent pour quelque chose de déjà passé", "Ha usat el present per a una cosa que ja ha passat"),
        PipMistake.Capital => Lang.T(language, "Forgot the capital letter at the start of the sentence", "Olvidó la mayúscula al empezar la frase", "A oublié la majuscule au début de la phrase", "Ha oblidat la majúscula a l'inici de la frase"),
        PipMistake.Area => Lang.T(language, "Added the sides (perimeter) instead of multiplying them (area)", "Sumó los lados (perímetro) en vez de multiplicarlos (área)", "A additionné les côtés (périmètre) au lieu de les multiplier (aire)", "Ha sumat els costats (perímetre) en lloc de multiplicar-los (àrea)"),
        PipMistake.Mean => Lang.T(language, "Forgot to divide by how many numbers there are", "Olvidó dividir entre cuántos números hay", "A oublié de diviser par le nombre de valeurs", "Ha oblidat dividir entre quants nombres hi ha"),
        PipMistake.Units => Lang.T(language, "Divided instead of multiplying when changing to a smaller unit", "Dividió en vez de multiplicar al pasar a una unidad menor", "A divisé au lieu de multiplier en passant à une unité plus petite", "Ha dividit en lloc de multiplicar en passar a una unitat menor"),
        PipMistake.Speed => Lang.T(language, "Multiplied distance by time instead of dividing", "Multiplicó distancia por tiempo en vez de dividir", "A multiplié la distance par le temps au lieu de diviser", "Ha multiplicat distància per temps en lloc de dividir"),
        _ => Lang.T(language, "Forgot to divide by 100", "Olvidó dividir entre 100", "A oublié de diviser par 100", "Ha oblidat dividir entre 100"),
    };

    /// <summary>The mistake Pip can make on a topic of the tests (so Pip can stumble where the student stumbled), or null.</summary>
    public static PipMistake? ForTopic(string? topic) => topic switch
    {
        "math.add" => PipMistake.Carry,
        "math.subtract" => PipMistake.Borrow,
        "math.multiply" or "math.integers" or "math.equations" => PipMistake.Order,
        "math.fraction" or "math.proportion" => PipMistake.FractionAdd,
        "math.percent" => PipMistake.Percent,
        "math.area" or "math.perimeter" => PipMistake.Area,
        "math.statistics" => PipMistake.Mean,
        _ => null,
    };

    /// <summary>The same seed, level and language always give the same problem (so the server can check the answer without storing it).</summary>
    public static PipProblem Generate(Level level, int seed, TeachLanguage language, PipSubject subject = PipSubject.Maths, PipMistake? forced = null)
    {
        var rng = new Random(seed);
        var kinds = Kinds(level, subject);
        var kind = kinds[rng.Next(kinds.Count)];
        if (forced is { } wanted && kinds.Contains(wanted))
        {
            kind = wanted;
        }

        var big = level >= Level.Secondary1;
        var (problem, shown, right, wrong) = kind switch
        {
            PipMistake.Carry => Carry(rng, language),
            PipMistake.Borrow => Borrow(rng, language),
            PipMistake.Order => Order(rng, big ? 12 : 9),
            PipMistake.FractionAdd => FractionAdd(rng),
            PipMistake.Agreement => Agreement(rng, language),
            PipMistake.Past => Past(rng, language),
            PipMistake.Capital => Capital(rng, language),
            PipMistake.Area => Area(rng, language),
            PipMistake.Mean => Mean(rng, language),
            PipMistake.Units => Units(rng, language),
            PipMistake.Speed => Speed(rng, language),
            _ => Percent(rng, big ? 15 : 9, language),
        };
        var pool = Kinds(level, subject);
        var others = (subject == PipSubject.Language ? pool : Enum.GetValues<PipMistake>().Where(m => !Kinds(level, PipSubject.Language).Contains(m))).Where(m => m != kind).OrderBy(_ => rng.Next()).Take(Reasons - 1).Append(kind).OrderBy(_ => rng.Next()).ToList();
        return new PipProblem(problem, shown, right, wrong, kind, others.Select(m => Describe(m, language)).ToList(), others.IndexOf(kind), Fix(kind, shown, right, wrong, language));
    }

    private static string Fix(PipMistake kind, IReadOnlyList<string> shown, IReadOnlyList<string> right, int wrong, TeachLanguage language) =>
        Lang.T(language,
            $"{Describe(kind, language)}. Pip wrote “{shown[wrong]}”, but it should be “{right[wrong]}”. The answer is {right[^1].Split('=')[^1].Trim()}.",
            $"{Describe(kind, language)}. Pip escribió «{shown[wrong]}», pero debía ser «{right[wrong]}». La respuesta es {right[^1].Split('=')[^1].Trim()}.",
            $"{Describe(kind, language)}. Pip a écrit « {shown[wrong]} », mais il fallait « {right[wrong]} ». La réponse est {right[^1].Split('=')[^1].Trim()}.",
            $"{Describe(kind, language)}. Pip ha escrit «{shown[wrong]}», però havia de ser «{right[wrong]}». La resposta és {right[^1].Split('=')[^1].Trim()}.");

    private static (string, IReadOnlyList<string>, IReadOnlyList<string>, int) Carry(Random rng, TeachLanguage language)
    {
        var (t1, t2, u1) = (rng.Next(1, 5), rng.Next(1, 5), rng.Next(5, 10));
        var u2 = rng.Next(10 - u1, 10);
        var (a, b, s) = (t1 * 10 + u1, t2 * 10 + u2, u1 + u2);
        var write = Lang.T(language, $"write {s % 10} and carry 1", $"escribo {s % 10} y llevo 1", $"j'écris {s % 10} et je retiens 1", $"escric {s % 10} i porto 1");
        var step1 = $"{u1} + {u2} = {s}: {write}";
        return ($"{a} + {b}", [step1, $"{t1} + {t2} = {t1 + t2}", $"{a} + {b} = {((t1 + t2) * 10) + (s % 10)}"], [step1, $"{t1} + {t2} + 1 = {t1 + t2 + 1}", $"{a} + {b} = {a + b}"], 1);
    }

    private static (string, IReadOnlyList<string>, IReadOnlyList<string>, int) Borrow(Random rng, TeachLanguage language)
    {
        var (ta, tb) = (rng.Next(3, 10), 0);
        tb = rng.Next(1, ta);
        var ua = rng.Next(0, 9);
        var ub = rng.Next(ua + 1, 10);
        var (a, b) = (ta * 10 + ua, tb * 10 + ub);
        var (units, tens, borrow) = (Lang.T(language, "units", "unidades", "unités", "unitats"), Lang.T(language, "tens", "decenas", "dizaines", "desenes"),
            Lang.T(language, "borrow 1", "pido 1 prestado", "j'emprunte 1", "demano 1 emprestat"));
        return ($"{a} − {b}",
            [$"{units}: {ub} − {ua} = {ub - ua}", $"{tens}: {ta} − {tb} = {ta - tb}", $"{a} − {b} = {(10 * (ta - tb)) + (ub - ua)}"],
            [$"{units}: {10 + ua} − {ub} = {10 + ua - ub} ({borrow})", $"{tens}: {ta} − 1 − {tb} = {ta - 1 - tb}", $"{a} − {b} = {a - b}"], 0);
    }

    private static (string, IReadOnlyList<string>, IReadOnlyList<string>, int) Order(Random rng, int max)
    {
        var (a, b, c) = (rng.Next(2, max + 1), rng.Next(2, max + 1), rng.Next(2, max + 1));
        return ($"{a} + {b} × {c}",
            [$"{a} + {b} = {a + b}", $"{a + b} × {c} = {(a + b) * c}", $"{a} + {b} × {c} = {(a + b) * c}"],
            [$"{b} × {c} = {b * c}", $"{a} + {b * c} = {a + (b * c)}", $"{a} + {b} × {c} = {a + (b * c)}"], 0);
    }

    private static (string, IReadOnlyList<string>, IReadOnlyList<string>, int) FractionAdd(Random rng)
    {
        int b, d;
        do
        {
            (b, d) = (rng.Next(2, 7), rng.Next(2, 7));
        }
        while (b == d);
        var (a, c) = (rng.Next(1, b), rng.Next(1, d));
        var m = Lcm(b, d);
        var (x, y) = (a * (m / b), c * (m / d));
        return ($"{a}/{b} + {c}/{d}",
            [$"{a}/{b} + {c}/{d} = ({a} + {c})/({b} + {d})", $"{a}/{b} + {c}/{d} = {a + c}/{b + d}"],
            [$"{a}/{b} + {c}/{d} = {x}/{m} + {y}/{m}", $"{a}/{b} + {c}/{d} = {x + y}/{m}"], 0);
    }

    private static (string, IReadOnlyList<string>, IReadOnlyList<string>, int) Percent(Random rng, int max, TeachLanguage language)
    {
        var p = new[] { 10, 20, 25, 50 }[rng.Next(4)];
        var n = 20 * rng.Next(2, max + 1);
        var means = Lang.T(language, $"{p} % means {p} out of 100", $"{p} % significa {p} de cada 100", $"{p} % veut dire {p} pour 100", $"{p} % vol dir {p} de cada 100");
        var answer = Lang.T(language, "answer", "respuesta", "réponse", "resposta");
        return ($"{p} % × {n}",
            [means, $"{n} × {p} = {n * p}", $"{answer} = {n * p}"],
            [means, $"{n} × {p} ÷ 100 = {n * p / 100}", $"{answer} = {n * p / 100}"], 1);
    }


    private static (string, IReadOnlyList<string>, IReadOnlyList<string>, int) Area(Random rng, TeachLanguage language)
    {
        int a, b;
        do
        {
            (a, b) = (rng.Next(3, 10), rng.Next(3, 10));
        }
        while (2 * (a + b) == a * b);
        var area = Lang.T(language, "area", "área", "aire", "àrea");
        var rect = Lang.T(language, $"rectangle {a} by {b}", $"rectángulo de {a} por {b}", $"rectangle de {a} sur {b}", $"rectangle de {a} per {b}");
        return (rect, [$"{area} = 2 × ({a} + {b}) = {2 * (a + b)}", $"{area} = {2 * (a + b)}"], [$"{area} = {a} × {b} = {a * b}", $"{area} = {a * b}"], 0);
    }

    private static (string, IReadOnlyList<string>, IReadOnlyList<string>, int) Mean(Random rng, TeachLanguage language)
    {
        var n = rng.Next(3, 5);
        var mean = rng.Next(4, 15);
        var values = new List<int>();
        for (var i = 0; i < n - 1; i++)
        {
            values.Add(Math.Max(1, mean + rng.Next(-3, 4)));
        }

        values.Add((mean * n) - values.Sum());
        if (values[^1] < 1)
        {
            values = [.. Enumerable.Repeat(mean, n)];
        }

        var (word, sum) = (Lang.T(language, "mean", "media", "moyenne", "mitjana"), values.Sum());
        var sumLine = $"{string.Join(" + ", values)} = {sum}";
        return (string.Join(", ", values), [sumLine, $"{word} = {sum}", $"{word} = {sum}"], [sumLine, $"{word} = {sum} ÷ {n} = {sum / n}", $"{word} = {sum / n}"], 1);
    }

    private static (string, IReadOnlyList<string>, IReadOnlyList<string>, int) Units(Random rng, TeachLanguage language)
    {
        var k = rng.Next(2, 10);
        var answer = Lang.T(language, "answer", "respuesta", "réponse", "resposta");
        return ($"{k} km → m", ["1 km = 1000 m", $"{k} ÷ 1000 = {k / 1000.0:0.###}", $"{answer} = {k / 1000.0:0.###} m"], ["1 km = 1000 m", $"{k} × 1000 = {k * 1000}", $"{answer} = {k * 1000} m"], 1);
    }

    private static (string, IReadOnlyList<string>, IReadOnlyList<string>, int) Speed(Random rng, TeachLanguage language)
    {
        var (v, t) = (rng.Next(3, 13) * 10, rng.Next(2, 6));
        var d = v * t;
        var formula = Lang.T(language, "speed = distance ÷ time", "velocidad = distancia ÷ tiempo", "vitesse = distance ÷ temps", "velocitat = distància ÷ temps");
        var speed = Lang.T(language, "speed", "velocidad", "vitesse", "velocitat");
        return (Lang.T(language, $"{d} km in {t} h", $"{d} km en {t} h", $"{d} km en {t} h", $"{d} km en {t} h"),
            [formula, $"{d} × {t} = {d * t}", $"{speed} = {d * t} km/h"], [formula, $"{d} ÷ {t} = {v}", $"{speed} = {v} km/h"], 1);
    }

    // ----- Language: a small controlled lexicon per language, so the code always knows the right form -----

    private sealed record Verb(string Inf, string Sing, string Plur, string Past);

    private static readonly string[] SubjectsEn = ["The children", "My friends", "The dogs"];
    private static readonly string[] SubjectsEs = ["Los niños", "Mis amigos", "Los perros"];
    private static readonly string[] SubjectsFr = ["Les enfants", "Mes amis", "Les chiens"];
    private static readonly string[] SubjectsCa = ["Els nens", "Els meus amics", "Els gossos"];

    private static readonly Verb[] VerbsEn = [new("play", "plays", "play", "played"), new("walk", "walks", "walk", "walked"), new("cook", "cooks", "cook", "cooked"), new("jump", "jumps", "jump", "jumped"), new("paint", "paints", "paint", "painted")];
    private static readonly Verb[] VerbsEs = [new("caminar", "camina", "caminan", "caminó"), new("cocinar", "cocina", "cocinan", "cocinó"), new("saltar", "salta", "saltan", "saltó"), new("pintar", "pinta", "pintan", "pintó"), new("cantar", "canta", "cantan", "cantó")];
    private static readonly Verb[] VerbsFr = [new("marcher", "marche", "marchent", "a marché"), new("cuisiner", "cuisine", "cuisinent", "a cuisiné"), new("sauter", "saute", "sautent", "a sauté"), new("chanter", "chante", "chantent", "a chanté"), new("danser", "danse", "dansent", "a dansé")];
    private static readonly Verb[] VerbsCa = [new("caminar", "camina", "caminen", "va caminar"), new("cuinar", "cuina", "cuinen", "va cuinar"), new("saltar", "salta", "salten", "va saltar"), new("pintar", "pinta", "pinten", "va pintar"), new("cantar", "canta", "canten", "va cantar")];

    private static (string[] Subjects, Verb[] Verbs, string Place, string Yesterday, string She) Lexicon(TeachLanguage language) => language switch
    {
        TeachLanguage.Spanish => (SubjectsEs, VerbsEs, "en el parque", "Ayer", "ella"),
        TeachLanguage.French => (SubjectsFr, VerbsFr, "dans le parc", "Hier", "elle"),
        TeachLanguage.Catalan => (SubjectsCa, VerbsCa, "al parc", "Ahir", "ella"),
        _ => (SubjectsEn, VerbsEn, "in the park", "Yesterday", "she"),
    };

    private static (string, IReadOnlyList<string>, IReadOnlyList<string>, int) Agreement(Random rng, TeachLanguage language)
    {
        var (subjects, verbs, place, _, _) = Lexicon(language);
        var (subject, verb) = (subjects[rng.Next(subjects.Length)], verbs[rng.Next(verbs.Length)]);
        var (word, verbWord, sentence) = (Lang.T(language, "subject", "sujeto", "sujet", "subjecte"), Lang.T(language, "verb", "verbo", "verbe", "verb"), Lang.T(language, "sentence", "frase", "phrase", "frase"));
        var plural = Lang.T(language, "plural", "plural", "pluriel", "plural");
        var line0 = $"{word}: {subject} ({plural})";
        return ($"{subject} ___ ({verb.Inf}) {place}.", [line0, $"{verbWord} {verb.Inf}: {verb.Sing}", $"{sentence} = {subject} {verb.Sing} {place}."], [line0, $"{verbWord} {verb.Inf}: {verb.Plur}", $"{sentence} = {subject} {verb.Plur} {place}."], 1);
    }

    private static (string, IReadOnlyList<string>, IReadOnlyList<string>, int) Past(Random rng, TeachLanguage language)
    {
        var (_, verbs, place, yesterday, she) = Lexicon(language);
        var verb = verbs[rng.Next(verbs.Length)];
        var (verbWord, sentence) = (Lang.T(language, "verb", "verbo", "verbe", "verb"), Lang.T(language, "sentence", "frase", "phrase", "frase"));
        var line0 = Lang.T(language, $"{yesterday} = past", $"{yesterday} = pasado", $"{yesterday} = passé", $"{yesterday} = passat");
        return ($"{yesterday} {she} ___ ({verb.Inf}) {place}.", [line0, $"{verbWord} {verb.Inf}: {verb.Sing}", $"{sentence} = {yesterday} {she} {verb.Sing} {place}."], [line0, $"{verbWord} {verb.Inf}: {verb.Past}", $"{sentence} = {yesterday} {she} {verb.Past} {place}."], 1);
    }

    private static (string, IReadOnlyList<string>, IReadOnlyList<string>, int) Capital(Random rng, TeachLanguage language)
    {
        var (subjects, verbs, place, _, _) = Lexicon(language);
        var (subject, verb) = (subjects[rng.Next(subjects.Length)], verbs[rng.Next(verbs.Length)]);
        var right = $"{subject} {verb.Plur} {place}.";
        var wrong = char.ToLowerInvariant(right[0]) + right[1..];
        var (order, sentence) = (Lang.T(language, "order: subject + verb + place", "orden: sujeto + verbo + lugar", "ordre : sujet + verbe + lieu", "ordre: subjecte + verb + lloc"), Lang.T(language, "sentence", "frase", "phrase", "frase"));
        return (Lang.T(language, $"Write a sentence: {subject.ToLowerInvariant()}, {verb.Inf}, {place}", $"Escribe una frase: {subject.ToLowerInvariant()}, {verb.Inf}, {place}", $"Écris une phrase : {subject.ToLowerInvariant()}, {verb.Inf}, {place}", $"Escriu una frase: {subject.ToLowerInvariant()}, {verb.Inf}, {place}"),
            [order, $"{sentence} = {wrong}"], [order, $"{sentence} = {right}"], 1);
    }

    private static int Lcm(int a, int b)
    {
        var (x, y) = (a, b);
        while (y != 0)
        {
            (x, y) = (y, x % y);
        }

        return a / x * b;
    }
}

/// <summary>Pip's voice after the student answers: what it says is written by the AI, or by code offline. The facts never change.</summary>
public static class PipVoice
{
    public const string ToolName = "pip_reply";

    public const int MaxChars = 280;

    public const string Schema = """
    {
      "type": "object",
      "properties": { "reply": { "type": "string", "description": "What Pip says, 1 to 3 short sentences, in first person, warm and a little silly." } },
      "required": ["reply"]
    }
    """;

    /// <summary>Pip's reply written in code: it thanks the student and says what it learned, never more than the facts.</summary>
    public static string Offline(bool lineOk, bool reasonOk, PipMistake mistake, TeachLanguage language)
    {
        var what = Pip.Describe(mistake, language).ToLowerInvariant();
        return (lineOk, reasonOk) switch
        {
            (true, true) => Lang.T(language,
                $"Oh! I see: I {what}. Thank you for teaching me, next time I will check that line first!",
                $"¡Ah! Ya lo veo: {what}. ¡Gracias por enseñarme, la próxima vez revisaré esa línea primero!",
                $"Oh ! Je vois : {what}. Merci de m'avoir appris, la prochaine fois je vérifierai cette ligne d'abord !",
                $"Ah! Ja ho veig: {what}. Gràcies per ensenyar-me, la propera vegada revisaré aquesta línia primer!"),
            (true, false) => Lang.T(language,
                $"You found my wrong line, thank you! The reason was: {what}. Can you remember it for next time?",
                $"¡Encontraste mi línea mal, gracias! El motivo era: {what}. ¿Lo recordarás la próxima vez?",
                $"Tu as trouvé ma ligne fausse, merci ! La raison : {what}. Tu t'en souviendras la prochaine fois ?",
                $"Has trobat la meva línia mal feta, gràcies! El motiu era: {what}. El recordaràs la propera vegada?"),
            _ => Lang.T(language,
                "Hmm, I still do not know where I went wrong. Read the right lines and try another one with me!",
                "Mmm, aún no sé dónde me equivoqué. ¡Lee las líneas correctas y probemos otro juntos!",
                "Hmm, je ne sais toujours pas où je me suis trompé. Lis les lignes justes et essayons-en un autre ensemble !",
                "Mmm, encara no sé on m'he equivocat. Llegeix les línies correctes i provem-ne un altre junts!"),
        };
    }

    public static StructuredPrompt Build(PipProblem problem, bool lineOk, bool reasonOk, Level level, TeachLanguage language)
    {
        ArgumentNullException.ThrowIfNull(problem);
        var system = $"""
            You are Pip, a cheerful student-AI who solved a problem with a mistake. A student of {Levels.PromptName(level)} has just tried to find your mistake.
            React in first person, in character: thank them, say what you understood from the facts below, and encourage them. Never invent facts: use only
            the facts given. Do not give new problems. Never scold. One to three short sentences. Write in {Lang.Name(language)}. Call the tool {ToolName}.
            """;
        var data = $"""
            problem: {problem.Problem}
            my mistake: {Pip.Describe(problem.Mistake, TeachLanguage.English)}
            the student found the wrong line: {(lineOk ? "yes" : "no")}
            the student named the reason correctly: {(reasonOk ? "yes" : "no")}
            my right solution: {string.Join(" ; ", problem.Right)}
            """;
        return new StructuredPrompt(system, data, ToolName, "Returns what Pip says.", Schema, 300);
    }

    public static string Parse(JsonElement json)
    {
        var reply = Json.Text(json, "reply", MaxChars);
        return reply.Length == 0 ? throw new AiGenerationException("The AI did not return a reply.") : reply;
    }
}
