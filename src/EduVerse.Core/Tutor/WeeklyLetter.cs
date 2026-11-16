using EduVerse.Core.Common;

namespace EduVerse.Core.Tutor;

/// <summary>What the student did in the last 7 days (measured by the platform; topic names already in the student's language).</summary>
public sealed record WeekFacts(string FirstName, int DaysActive, int Answers, int Correct, int Understood, string? BestTopic, string? WorkTopic, string? ExamTitle, int? ExamDays)
{
    /// <summary>A topic answered "sure" and wrong at least twice this week: believes they know it, not yet.</summary>
    public string? SureWrongTopic { get; init; }

    /// <summary>A topic answered "not sure" and right at least twice this week: knows more than they think.</summary>
    public string? UnsureRightTopic { get; init; }
}

public sealed record LetterPart(string Icon, string Text);

public sealed record Letter(string Title, string Greeting, IReadOnlyList<LetterPart> Parts, string Closing);

/// <summary>
/// A letter for the family about the week, written entirely by code from measured facts (no AI, nothing leaves the platform):
/// it celebrates effort first, is honest about what is still hard, and says what would help at home.
/// </summary>
public static class WeeklyLetter
{
    public static bool HasContent(WeekFacts facts) => facts is { Answers: > 0 };

    public static Letter Build(WeekFacts f, TeachLanguage language)
    {
        ArgumentNullException.ThrowIfNull(f);
        var name = string.IsNullOrWhiteSpace(f.FirstName) ? "" : f.FirstName;
        var parts = new List<LetterPart>();
        var rate = f.Answers == 0 ? 0 : 100 * f.Correct / f.Answers;
        parts.Add(new("📅", Lang.T(language,
            $"This week {name} studied on {f.DaysActive} day(s) and answered {f.Answers} questions, {f.Correct} of them right ({rate} %).",
            $"Esta semana {name} estudió {f.DaysActive} día(s) y respondió {f.Answers} preguntas, {f.Correct} bien ({rate} %).",
            $"Cette semaine, {name} a étudié {f.DaysActive} jour(s) et répondu à {f.Answers} questions, dont {f.Correct} justes ({rate} %).",
            $"Aquesta setmana {name} ha estudiat {f.DaysActive} dia(es) i ha respost {f.Answers} preguntes, {f.Correct} ben fetes ({rate} %).")));
        if (f.BestTopic is { Length: > 0 })
        {
            parts.Add(new("🌟", Lang.T(language,
                $"What went best: {f.BestTopic}.", $"Lo que mejor salió: {f.BestTopic}.", $"Ce qui s'est le mieux passé : {f.BestTopic}.", $"El que ha anat millor: {f.BestTopic}.")));
        }

        if (f.Understood > 0)
        {
            parts.Add(new("🛡️", Lang.T(language,
                $"{name} also explained {f.Understood} answer(s) in their own words and showed they understood them.",
                $"{name} también explicó {f.Understood} respuesta(s) con sus palabras y demostró que las entiende.",
                $"{name} a aussi expliqué {f.Understood} réponse(s) avec ses mots et montré qu'il/elle les comprend.",
                $"{name} també ha explicat {f.Understood} resposta(s) amb les seves paraules i ha demostrat que les entén.")));
        }

        if (f.WorkTopic is { Length: > 0 })
        {
            parts.Add(new("🧩", Lang.T(language,
                $"Still tricky: {f.WorkTopic}. It is normal: a few short sessions are better than one long one.",
                $"Todavía cuesta: {f.WorkTopic}. Es normal: mejor varias sesiones cortas que una larga.",
                $"Encore difficile : {f.WorkTopic}. C'est normal : plusieurs séances courtes valent mieux qu'une longue.",
                $"Encara costa: {f.WorkTopic}. És normal: millor diverses sessions curtes que una de llarga.")));
        }

        if (f.UnsureRightTopic is { Length: > 0 })
        {
            parts.Add(new("💪", Lang.T(language,
                $"{name} knows more than they think: in {f.UnsureRightTopic} the answers were right even when {name} was not sure. A word of confidence helps.",
                $"{name} sabe más de lo que cree: en {f.UnsureRightTopic} acertó incluso sin estar seguro/a. Unas palabras de confianza ayudan.",
                $"{name} en sait plus qu'il/elle ne le croit : en {f.UnsureRightTopic}, les réponses étaient justes même sans être sûr(e). Un mot de confiance aide.",
                $"{name} sap més del que es pensa: a {f.UnsureRightTopic} ha encertat fins i tot sense estar-ne segur/a. Unes paraules de confiança ajuden.")));
        }

        if (f.SureWrongTopic is { Length: > 0 })
        {
            parts.Add(new("🔁", Lang.T(language,
                $"In {f.SureWrongTopic}, {name} felt sure but the answers were wrong: an idea to look at again calmly, not a lack of effort.",
                $"En {f.SureWrongTopic}, {name} se sentía seguro/a pero falló: es una idea para repasar con calma, no falta de esfuerzo.",
                $"En {f.SureWrongTopic}, {name} se sentait sûr(e) mais s'est trompé(e) : une idée à revoir calmement, pas un manque d'effort.",
                $"A {f.SureWrongTopic}, {name} se sentia segur/a però ha fallat: és una idea per repassar amb calma, no manca d'esforç.")));
        }

        if (f.ExamTitle is { Length: > 0 } && f.ExamDays is { } d and >= 0)
        {
            parts.Add(new("🎯", Lang.T(language,
                $"“{f.ExamTitle}” is in {d} days. Ten calm minutes a day with the plan is enough.",
                $"«{f.ExamTitle}» es dentro de {d} días. Diez minutos tranquilos al día con el plan bastan.",
                $"« {f.ExamTitle} » est dans {d} jours. Dix minutes calmes par jour avec le plan suffisent.",
                $"«{f.ExamTitle}» és d'aquí {d} dies. Deu minuts tranquils al dia amb el pla són suficients.")));
        }

        parts.Add(new("💬", Lang.T(language,
            "How you can help at home: ask them to explain one thing they learned this week. Explaining is the best way to learn.",
            "Cómo ayudar en casa: pídele que te explique algo que ha aprendido esta semana. Explicar es la mejor forma de aprender.",
            "Comment aider à la maison : demandez-lui d'expliquer une chose apprise cette semaine. Expliquer est la meilleure façon d'apprendre.",
            "Com ajudar a casa: demana-li que t'expliqui una cosa que ha après aquesta setmana. Explicar és la millor manera d'aprendre.")));
        return new Letter(
            Lang.T(language, $"{name}'s week with the Tutor", $"La semana de {name} con el Tutor", $"La semaine de {name} avec le Tuteur", $"La setmana de {name} amb el Tutor"),
            Lang.T(language, "Dear family,", "Queridas familias:", "Chers parents,", "Benvolgudes famílies:"),
            parts,
            Lang.T(language, "Made by the Tutor from what was measured, nothing else.", "Hecho por el Tutor con lo medido, nada más.", "Fait par le Tuteur à partir de ce qui a été mesuré, rien d'autre.", "Fet pel Tutor amb el que s'ha mesurat, res més."));
    }
}
