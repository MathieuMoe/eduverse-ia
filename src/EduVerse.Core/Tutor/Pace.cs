using EduVerse.Core.Common;

namespace EduVerse.Core.Tutor;

/// <summary>What the rhythm of a test says (decided in code from the seconds per question and the results).</summary>
public enum PaceSignal
{
    None,

    /// <summary>Answering very fast and failing: read again.</summary>
    Rushing,

    /// <summary>A long time on one question that ended wrong: ask for a hint earlier.</summary>
    Stuck,

    /// <summary>A long test that started well and is going down: time for a break.</summary>
    Tired,
}

/// <summary>One answer of the test: seconds on screen (0 = not measured) and whether it was right.</summary>
public sealed record PacePoint(int Seconds, bool Correct);

/// <summary>
/// Rhythm and tiredness: a good teacher notices when a student rushes, gets stuck or runs out of energy. Here the code notices, from the
/// time on each question. It only advises (never punishes, never changes the score) and says one thing at a time.
/// </summary>
public static class Pace
{
    public const int MaxSeconds = 600;

    /// <summary>Under this, a question was hardly read.</summary>
    public const int FastSeconds = 4;

    public const int SlowSeconds = 90;

    public const int TiredAfter = 8;

    public static int Clean(int? seconds) => Math.Clamp(seconds ?? 0, 0, MaxSeconds);

    /// <summary>The signal after the last answer of a test (oldest first). Each signal needs measured times.</summary>
    public static PaceSignal Read(IReadOnlyList<PacePoint> answers)
    {
        ArgumentNullException.ThrowIfNull(answers);
        if (answers.Count == 0 || answers[^1].Seconds <= 0)
        {
            return PaceSignal.None;
        }

        var last3 = answers.TakeLast(3).ToList();
        if (last3.Count == 3 && last3.All(a => a.Seconds is > 0 and < FastSeconds) && last3.Count(a => !a.Correct) >= 2)
        {
            return PaceSignal.Rushing;
        }

        if (answers.Count >= TiredAfter)
        {
            var half = answers.Count / 2;
            var early = answers.Take(half).Count(a => a.Correct) / (double)half;
            var late = answers.TakeLast(4).Count(a => a.Correct);
            if (early >= 0.6 && late <= 1)
            {
                return PaceSignal.Tired;
            }
        }

        return answers[^1] is { Seconds: >= SlowSeconds, Correct: false } ? PaceSignal.Stuck : PaceSignal.None;
    }

    public static string Advice(PaceSignal signal, TeachLanguage language) => signal switch
    {
        PaceSignal.Rushing => Lang.T(language,
            "You are going very fast. Read the next question twice before choosing: there is no clock here.",
            "Vas muy rápido. Lee la siguiente pregunta dos veces antes de elegir: aquí no hay reloj.",
            "Tu vas très vite. Lis la prochaine question deux fois avant de choisir : ici, pas de chrono.",
            "Vas molt de pressa. Llegeix la pregunta següent dues vegades abans de triar: aquí no hi ha rellotge."),
        PaceSignal.Stuck => Lang.T(language,
            "That one took a long time. Next time, ask for a hint or think with the Tutor before you get stuck: it is not cheating.",
            "Esa te ha llevado mucho rato. La próxima vez pide una pista o piensa con el Tutor antes de atascarte: no es hacer trampa.",
            "Celle-ci t'a pris longtemps. La prochaine fois, demande un indice ou réfléchis avec le Tuteur avant de bloquer : ce n'est pas tricher.",
            "Aquesta t'ha portat molta estona. La propera vegada demana una pista o pensa amb el Tutor abans d'encallar-te: no és fer trampa."),
        PaceSignal.Tired => Lang.T(language,
            "You started strong and now it costs more: that is tiredness, not you. Finish this one and take a five-minute break.",
            "Empezaste fuerte y ahora cuesta más: eso es cansancio, no eres tú. Termina esta y descansa cinco minutos.",
            "Tu as bien commencé et maintenant c'est plus dur : c'est la fatigue, pas toi. Termine celle-ci et fais une pause de cinq minutes.",
            "Has començat fort i ara costa més: això és cansament, no ets tu. Acaba aquesta i descansa cinc minuts."),
        _ => "",
    };
}
