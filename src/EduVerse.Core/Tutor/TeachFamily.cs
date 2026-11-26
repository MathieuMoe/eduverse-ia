using EduVerse.Core.Common;

namespace EduVerse.Core.Tutor;

/// <summary>Something a parent or sibling can ask the student, and the answer to check it against (already in the
/// student's language, taken from a real exercise of the topic — not invented).</summary>
public sealed record CheckQuestion(string Question, string Answer);

/// <summary>What the student just mastered, enough to teach it (names and explanation already in the student's
/// language; <see cref="Explanation"/> and <see cref="Checks"/> come straight from the exercise bank, nothing invented).</summary>
public sealed record MilestoneFacts(string FirstName, string Topic, string AreaName, string Explanation, IReadOnlyList<CheckQuestion> Checks);

public sealed record FamilyScript(string Title, string Intro, string Explain, IReadOnlyList<CheckQuestion> Checks, string Closing);

/// <summary>
/// The mirror of <see cref="WeeklyLetter"/>: instead of a report sent TO the family, a short script the student can
/// use to teach a parent or sibling what they just mastered, with a couple of check-questions anyone can ask without
/// knowing the subject. Built on the "protégé effect" — explaining something well is one of the best proofs, and one
/// of the best ways, of really knowing it. Written entirely by code from measured facts and real exercises (no AI,
/// nothing leaves the platform), so it works offline too.
/// </summary>
public static class TeachFamily
{
    /// <summary>At least this many check-questions are needed for the script to be worth showing.</summary>
    public const int MinChecks = 2;

    public static bool HasContent(MilestoneFacts f) => f is { Explanation.Length: > 0 } && f.Checks.Count >= MinChecks;

    public static FamilyScript Build(MilestoneFacts f, TeachLanguage language)
    {
        ArgumentNullException.ThrowIfNull(f);
        var name = string.IsNullOrWhiteSpace(f.FirstName) ? "" : f.FirstName;
        var title = Lang.T(language,
            $"Teach it to your family: {f.Topic}",
            $"Enséñaselo a tu familia: {f.Topic}",
            $"Explique-le à ta famille : {f.Topic}",
            $"Explica-ho a la teva família: {f.Topic}");
        var intro = Lang.T(language,
            $"{name}, you just mastered {f.Topic} ({f.AreaName})! Explaining something out loud is one of the best ways to make sure you really understand it. Find someone at home and try this:",
            $"{name}, ¡acabas de dominar {f.Topic} ({f.AreaName})! Explicar algo en voz alta es una de las mejores formas de comprobar que de verdad lo entiendes. Busca a alguien en casa y prueba esto:",
            $"{name}, tu viens de maîtriser {f.Topic} ({f.AreaName}) ! Expliquer quelque chose à voix haute est l'une des meilleures façons de vérifier qu'on le comprend vraiment. Trouve quelqu'un à la maison et essaie ceci :",
            $"{name}, acabes de dominar {f.Topic} ({f.AreaName})! Explicar una cosa en veu alta és una de les millors maneres de comprovar que realment ho entens. Busca algú a casa i prova això:");
        var closing = Lang.T(language,
            "If they get a check question wrong, that's not a problem: it just shows which part to go over again together, calmly.",
            "Si falla alguna pregunta de control, no pasa nada: solo indica qué parte repasar otra vez juntos, con calma.",
            "Si la personne se trompe à une question de contrôle, ce n'est pas grave : cela montre juste quelle partie revoir ensemble, calmement.",
            "Si falla alguna pregunta de control, no passa res: només indica quina part repassar una altra vegada junts, amb calma.");
        return new FamilyScript(title, intro, f.Explanation, f.Checks, closing);
    }
}
