using EduVerse.Core.Catalog;
using EduVerse.Core.Common;

namespace EduVerse.Core.Tutor;

/// <summary>One of the fixed interests a student can pick (never free text): the things their word problems are about.</summary>
public sealed record Interest(string Key, string Icon, string En, string Es, string Fr, string Ca, bool EsFem = false, bool CaFem = false);

/// <summary>
/// What the student likes, chosen from a fixed list (up to 3). The AI is told to use them in examples; offline, a share of the word
/// problems of primary mathematics is written around them. The numbers and the answers are always computed in code.
/// </summary>
public static class Interests
{
    public const int Max = 3;

    public static readonly IReadOnlyList<Interest> All =
    [
        new("football", "⚽", "football stickers", "cromos de fútbol", "vignettes de foot", "cromos de futbol"),
        new("games", "🎮", "game coins", "monedas de juego", "pièces de jeu", "monedes de joc", true, true),
        new("animals", "🐾", "animal cards", "cartas de animales", "cartes d'animaux", "cartes d'animals", true, true),
        new("music", "🎵", "songs", "canciones", "chansons", "cançons", true, true),
        new("space", "🚀", "star stickers", "pegatinas de estrellas", "autocollants d'étoiles", "adhesius d'estrelles", true),
        new("cooking", "🍪", "cookies", "galletas", "biscuits", "galetes", true, true),
        new("drawing", "🎨", "coloured pencils", "lápices de colores", "crayons de couleur", "llapis de colors"),
        new("cars", "🚗", "toy cars", "coches de juguete", "voitures miniatures", "cotxes de joguina"),
        new("dance", "💃", "dance medals", "medallas de baile", "médailles de danse", "medalles de ball", true, true),
        new("robots", "🤖", "robot parts", "piezas de robot", "pièces de robot", "peces de robot", true, true),
    ];

    public static Interest? Find(string? key) => All.FirstOrDefault(i => i.Key == key);

    /// <summary>The valid keys of a comma-separated list, without repeats, at most <see cref="Max"/>.</summary>
    public static IReadOnlyList<string> Parse(string? keys) =>
        (keys ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Where(k => Find(k) is not null).Distinct(StringComparer.Ordinal).Take(Max).ToList();

    public static string Join(IEnumerable<string> keys) => string.Join(',', Parse(string.Join(',', keys)));

    public static string Name(Interest interest, TeachLanguage language) => Lang.T(language, interest.En, interest.Es, interest.Fr, interest.Ca);

    /// <summary>For the AI prompts: the interests as words, with the rule to use them with care.</summary>
    public static string ForPrompt(IReadOnlyList<string> keys)
    {
        ArgumentNullException.ThrowIfNull(keys);
        var words = keys.Select(Find).Where(i => i is not null).Select(i => i!.En).ToList();
        return words.Count == 0 ? "" : $"The student likes: {string.Join(", ", words)}. When an example or a word problem helps, set it in one of these worlds. Never force it, never let it change the maths.";
    }
}

/// <summary>Word problems of primary mathematics about the interests of the student, solved in code.</summary>
public static class InterestProblems
{
    private static readonly int[] Percents = [10, 20, 25, 50, 75];
    private static readonly int[] Denominators = [2, 3, 4, 5];

    public static readonly IReadOnlyList<string> Topics = ["math.add", "math.subtract", "math.multiply", "math.divide", "math.percent", "math.fraction"];

    /// <summary>Whether the topic of this level has a word-problem version.</summary>
    public static bool Supports(Area area, Level level, string? topic = null) =>
        area == Area.Mathematics && level is Level.Primary1 or Level.Primary2 or Level.Primary3 && (topic is null || Topics.Contains(topic));

    /// <summary>A word problem for the level (and the topic, if given) about the interest. The same seed gives the same problem.</summary>
    public static QuizQuestion Generate(Interest interest, Level level, string? topic, int difficulty, int seed, TeachLanguage language)
    {
        ArgumentNullException.ThrowIfNull(interest);
        var q = new Mc(new Random(seed), language, level, Math.Clamp(difficulty, 1, 3));
        var item = Name(interest, language);
        var how = (q.Language, interest) switch
        {
            (TeachLanguage.Spanish, { EsFem: true }) => "Cuántas",
            (TeachLanguage.Spanish, _) => "Cuántos",
            (TeachLanguage.Catalan, { CaFem: true }) => "Quantes",
            (TeachLanguage.Catalan, _) => "Quants",
            _ => "",
        };
        var kinds = level switch
        {
            Level.Primary1 => new[] { "math.add", "math.subtract" },
            Level.Primary2 => ["math.add", "math.subtract", "math.multiply", "math.divide"],
            _ => ["math.multiply", "math.divide", "math.percent", "math.fraction"],
        };
        var kind = topic is not null && kinds.Contains(topic) ? topic : q.Pick(kinds);
        var max = q.Difficulty switch { 1 => 20, 2 => 50, _ => 100 };
        switch (kind)
        {
            case "math.add":
            {
                var a = q.Between(2, max / 2);
                var b = q.Between(2, max - a);
                return q.Number(kind, Lang.T(language,
                    $"You have {a} {item} and you get {b} more. What is the total?",
                    $"Tienes {a} {item} y consigues {b} más. ¿Cuál es el total?",
                    $"Tu as {a} {item} et tu en gagnes {b} de plus. Quel est le total ?",
                    $"Tens {a} {item} i en aconsegueixes {b} més. Quin és el total?"),
                    a + b, [a + b + 1, a + b - 1, a + b + 10, a - b],
                    q.F("Add what you have and what you get: {0} + {1} = {2}.|Suma lo que tienes y lo que consigues: {0} + {1} = {2}.|Additionne ce que tu as et ce que tu gagnes : {0} + {1} = {2}.|Suma el que tens i el que aconsegueixes: {0} + {1} = {2}.", a, b, a + b));
            }

            case "math.subtract":
            {
                var a = q.Between(5, max);
                var b = q.Between(1, a - 1);
                return q.Number(kind, Lang.T(language,
                    $"You have {a} {item} and you give {b} to a friend. How many are left?",
                    $"Tienes {a} {item} y das {b} a un amigo. ¿{how} te quedan?",
                    $"Tu as {a} {item} et tu en donnes {b} à un ami. Combien t'en reste-t-il ?",
                    $"Tens {a} {item} i en dones {b} a un amic. {how} te'n queden?"),
                    a - b, [a + b, a - b + 1, a - b - 1, a - b + 10],
                    q.F("Giving away is taking away: {0} − {1} = {2}. Check it: {2} + {1} = {0}.|Dar es quitar: {0} − {1} = {2}. Compruébalo: {2} + {1} = {0}.|Donner, c'est enlever : {0} − {1} = {2}. Vérifie : {2} + {1} = {0}.|Donar és treure: {0} − {1} = {2}. Comprova-ho: {2} + {1} = {0}.", a, b, a - b));
            }

            case "math.multiply":
            {
                var (lo, hi) = q.Difficulty switch { 1 => (2, 5), 2 => (2, 9), _ => (6, 12) };
                var a = q.Between(lo, hi);
                var b = q.Between(2, hi);
                return q.Number(kind, Lang.T(language,
                    $"There are {a} boxes with {b} {item} in each one. What is the total?",
                    $"Hay {a} cajas con {b} {item} en cada una. ¿Cuál es el total?",
                    $"Il y a {a} boîtes avec {b} {item} dans chacune. Quel est le total ?",
                    $"Hi ha {a} caixes amb {b} {item} a cadascuna. Quin és el total?"),
                    a * b, [a * (b + 1), a + b, a * b - a, a * b + 1],
                    q.F("{0} boxes of {1} make {0} × {1} = {2}.|{0} cajas de {1} son {0} × {1} = {2}.|{0} boîtes de {1} font {0} × {1} = {2}.|{0} caixes de {1} són {0} × {1} = {2}.", a, b, a * b));
            }

            case "math.divide":
            {
                var a = q.Between(2, q.Difficulty == 3 ? 12 : 9);
                var b = q.Between(2, 10);
                return q.Number(kind, Lang.T(language,
                    $"You share {a * b} {item} equally between {a} friends. How many does each friend get?",
                    $"Repartes {a * b} {item} a partes iguales entre {a} amigos. ¿{how} le tocan a cada amigo?",
                    $"Tu partages {a * b} {item} en parts égales entre {a} amis. Combien en reçoit chaque ami ?",
                    $"Reparteixes {a * b} {item} a parts iguals entre {a} amics. {how} en toquen a cada amic?"),
                    b, [b + 1, b - 1, a * b - a, a],
                    q.F("Sharing equally is dividing: {2} ÷ {0} = {1}, because {0} × {1} = {2}.|Repartir a partes iguales es dividir: {2} ÷ {0} = {1}, porque {0} × {1} = {2}.|Partager en parts égales, c'est diviser : {2} ÷ {0} = {1}, car {0} × {1} = {2}.|Repartir a parts iguals és dividir: {2} ÷ {0} = {1}, perquè {0} × {1} = {2}.", a, b, a * b));
            }

            case "math.percent":
            {
                var p = q.Pick(Percents);
                var n = p == 25 || p == 75 ? q.Between(1, 5) * 4 * (q.Difficulty + 1) : q.Between(1, 10) * 10 * q.Difficulty;
                var answer = n * p / 100.0;
                return q.Number(kind, Lang.T(language,
                    $"You collected {n} {item} and you give {p} % of them to a friend. How many do you give?",
                    $"Has reunido {n} {item} y das el {p} % a un amigo. ¿{how} das?",
                    $"Tu as réuni {n} {item} et tu en donnes {p} % à un ami. Combien en donnes-tu ?",
                    $"Has reunit {n} {item} i en dones el {p} % a un amic. {how} en dones?"),
                    answer, [n * (100 - p) / 100.0, p, n - p, answer * 2],
                    q.F("{0} % of {1} is {1} × {0} ÷ 100 = {2}.|El {0} % de {1} es {1} × {0} ÷ 100 = {2}.|{0} % de {1}, c'est {1} × {0} ÷ 100 = {2}.|El {0} % de {1} és {1} × {0} ÷ 100 = {2}.", p, n, q.N(answer)));
            }

            default:
            {
                var d = q.Pick(Denominators);
                var num = q.Between(1, d - 1);
                var total = d * q.Between(2, q.Difficulty * 3 + 3);
                var answer = total / d * num;
                return q.Number("math.fraction", Lang.T(language,
                    $"You have {total} {item} and you share {num}/{d} of them. How many do you share?",
                    $"Tienes {total} {item} y compartes {num}/{d} de ellas. ¿{how} compartes?",
                    $"Tu as {total} {item} et tu en partages {num}/{d}. Combien en partages-tu ?",
                    $"Tens {total} {item} i en comparteixes {num}/{d}. {how} en comparteixes?"),
                    answer, [total / d, total - answer, num * d, answer + num],
                    q.F("Divide by {1} first: {2} ÷ {1} = {3}; then multiply by {0}: {3} × {0} = {4}.|Divide primero entre {1}: {2} ÷ {1} = {3}; luego multiplica por {0}: {3} × {0} = {4}.|Divise d'abord par {1} : {2} ÷ {1} = {3} ; puis multiplie par {0} : {3} × {0} = {4}.|Divideix primer entre {1}: {2} ÷ {1} = {3}; després multiplica per {0}: {3} × {0} = {4}.", num, d, total, total / d, answer));
            }
        }
    }

    private static string Name(Interest interest, TeachLanguage language) => Interests.Name(interest, language);
}
