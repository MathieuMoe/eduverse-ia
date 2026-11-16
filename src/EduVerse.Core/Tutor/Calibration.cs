using EduVerse.Core.Common;

namespace EduVerse.Core.Tutor;

/// <summary>Where an answer falls when what the student believed meets what happened.</summary>
public enum Quadrant
{
    /// <summary>Sure and right: knows it, and knows that they know it.</summary>
    Solid,

    /// <summary>Sure and wrong: an idea learned the wrong way. The most useful to review.</summary>
    FalseCertainty,

    /// <summary>Not sure and right: knows more than they think.</summary>
    HiddenStrength,

    /// <summary>Not sure and wrong: still to learn, and they know it (that is fine).</summary>
    ToLearn,
}

/// <summary>An answer where the student said how sure they were ("I think so" answers are left out). Oldest first in lists.</summary>
public sealed record Bet(string Topic, bool Sure, bool Correct);

public sealed record QuadrantCell(Quadrant Quadrant, int Count, IReadOnlyList<string> Topics);

public enum CalibrationTrend
{
    Unknown,
    Better,
    Same,
    Worse,
}

/// <summary>
/// The confidence map: every answer given as "sure" or "not sure" lands in one of four boxes. It turns the bet the student makes before
/// each answer into something they can read: what is solid, what they wrongly believe they know, and what they know without believing it.
/// </summary>
public static class Calibration
{
    /// <summary>Bets needed before saying anything about how well the student judges themselves.</summary>
    public const int MinBets = 8;

    /// <summary>Size of each of the two windows compared for the trend.</summary>
    public const int Window = 15;

    public const int TrendStep = 10;

    public const int TopicsPerCell = 3;

    public static Quadrant Of(Bet bet)
    {
        ArgumentNullException.ThrowIfNull(bet);
        return (bet.Sure, bet.Correct) switch
        {
            (true, true) => Quadrant.Solid,
            (true, false) => Quadrant.FalseCertainty,
            (false, true) => Quadrant.HiddenStrength,
            _ => Quadrant.ToLearn,
        };
    }

    /// <summary>The four boxes, always in the same order, each with its count and the topics that fall there most (at least twice).</summary>
    public static IReadOnlyList<QuadrantCell> Map(IReadOnlyList<Bet> bets)
    {
        ArgumentNullException.ThrowIfNull(bets);
        return Enum.GetValues<Quadrant>().Select(q =>
        {
            var inside = bets.Where(b => Of(b) == q).ToList();
            var topics = inside.GroupBy(b => b.Topic).Where(g => g.Count() >= 2).OrderByDescending(g => g.Count()).ThenBy(g => g.Key, StringComparer.Ordinal).Take(TopicsPerCell).Select(g => g.Key).ToList();
            return new QuadrantCell(q, inside.Count, topics);
        }).ToList();
    }

    /// <summary>How well belief matches result, 0–100: "sure" should be right and "not sure" is honest when wrong. Null with too few bets.</summary>
    public static int? Score(IReadOnlyList<Bet> bets)
    {
        ArgumentNullException.ThrowIfNull(bets);
        return bets.Count < MinBets ? null : 100 * bets.Count(b => b.Sure == b.Correct) / bets.Count;
    }

    /// <summary>Compares the last <see cref="Window"/> bets with the ones just before.</summary>
    public static CalibrationTrend Trend(IReadOnlyList<Bet> bets)
    {
        ArgumentNullException.ThrowIfNull(bets);
        if (bets.Count < 2 * Window)
        {
            return CalibrationTrend.Unknown;
        }

        var now = Score(bets.TakeLast(Window).ToList())!.Value;
        var before = Score(bets.SkipLast(Window).TakeLast(Window).ToList())!.Value;
        return now - before >= TrendStep ? CalibrationTrend.Better : before - now >= TrendStep ? CalibrationTrend.Worse : CalibrationTrend.Same;
    }

    public static string Name(Quadrant quadrant, TeachLanguage language) => quadrant switch
    {
        Quadrant.Solid => Lang.T(language, "You know it, and you know that you know it", "Lo sabes, y sabes que lo sabes", "Tu le sais, et tu sais que tu le sais", "Ho saps, i saps que ho saps"),
        Quadrant.FalseCertainty => Lang.T(language, "Sure… but not yet", "Seguro… pero todavía no", "Sûr… mais pas encore", "Segur… però encara no"),
        Quadrant.HiddenStrength => Lang.T(language, "You know more than you think", "Sabes más de lo que crees", "Tu en sais plus que tu ne crois", "Saps més del que et penses"),
        _ => Lang.T(language, "Still to learn, and you know it", "Por aprender, y lo sabes", "Encore à apprendre, et tu le sais", "Per aprendre, i ho saps"),
    };

    public static string Hint(Quadrant quadrant, TeachLanguage language) => quadrant switch
    {
        Quadrant.Solid => Lang.T(language, "Said “sure” and it was right. This is solid ground.", "Dijiste «seguro» y acertaste. Esto es terreno firme.", "Tu as dit « sûr » et c'était juste. C'est du solide.", "Vas dir «segur» i vas encertar. Això és terreny ferm."),
        Quadrant.FalseCertainty => Lang.T(language, "Said “sure” and it was wrong. An idea got stuck the wrong way: the most useful thing to review.", "Dijiste «seguro» y fallaste. Hay una idea aprendida al revés: lo más útil de repasar.", "Tu as dit « sûr » et c'était faux. Une idée s'est installée de travers : le plus utile à revoir.", "Vas dir «segur» i vas fallar. Hi ha una idea apresa al revés: el més útil de repassar."),
        Quadrant.HiddenStrength => Lang.T(language, "Said “not sure” and it was right. Trust yourself a little more here.", "Dijiste «no estoy seguro» y acertaste. Confía un poco más en ti aquí.", "Tu as dit « pas sûr » et c'était juste. Fais-toi un peu plus confiance ici.", "Vas dir «no n'estic segur» i vas encertar. Confia una mica més en tu aquí."),
        _ => Lang.T(language, "Said “not sure” and it was wrong. Knowing what you do not know yet is a strength.", "Dijiste «no estoy seguro» y fallaste. Saber lo que aún no sabes es una fortaleza.", "Tu as dit « pas sûr » et c'était faux. Savoir ce que tu ne sais pas encore est une force.", "Vas dir «no n'estic segur» i vas fallar. Saber el que encara no saps és una fortalesa."),
    };

    /// <summary>One sentence about how well the student judges themselves ("" with too few bets).</summary>
    public static string Say(int? score, CalibrationTrend trend, TeachLanguage language)
    {
        if (score is not { } s)
        {
            return "";
        }

        var level = s >= 80
            ? Lang.T(language, $"You judge yourself very well: what you believed matched what happened {s} % of the time.", $"Te conoces muy bien: lo que creías coincidió con lo que pasó el {s} % de las veces.", $"Tu te connais très bien : ce que tu pensais correspondait à ce qui s'est passé {s} % du temps.", $"Et coneixes molt bé: el que creies ha coincidit amb el que ha passat el {s} % de les vegades.")
            : s >= 60
                ? Lang.T(language, $"What you believed matched what happened {s} % of the time. Good, and it can get sharper.", $"Lo que creías coincidió con lo que pasó el {s} % de las veces. Bien, y se puede afinar.", $"Ce que tu pensais correspondait à ce qui s'est passé {s} % du temps. Bien, et ça peut s'affiner.", $"El que creies ha coincidit amb el que ha passat el {s} % de les vegades. Bé, i es pot afinar.")
                : Lang.T(language, $"What you believed matched what happened {s} % of the time. Before choosing “sure”, ask yourself: could I explain why?", $"Lo que creías coincidió con lo que pasó el {s} % de las veces. Antes de elegir «seguro», pregúntate: ¿sabría explicar por qué?", $"Ce que tu pensais correspondait à ce qui s'est passé {s} % du temps. Avant de choisir « sûr », demande-toi : saurais-je expliquer pourquoi ?", $"El que creies ha coincidit amb el que ha passat el {s} % de les vegades. Abans de triar «segur», pregunta't: sabria explicar per què?");
        var change = trend switch
        {
            CalibrationTrend.Better => Lang.T(language, " And it is getting better lately.", " Y últimamente va a mejor.", " Et ça s'améliore ces derniers temps.", " I últimament va a millor."),
            CalibrationTrend.Worse => Lang.T(language, " Lately a bit less: slow down before betting.", " Últimamente algo menos: piénsalo un momento antes de apostar.", " Un peu moins ces derniers temps : prends un instant avant de parier.", " Últimament una mica menys: pensa-t'ho un moment abans d'apostar."),
            _ => "",
        };
        return level + change;
    }
}
