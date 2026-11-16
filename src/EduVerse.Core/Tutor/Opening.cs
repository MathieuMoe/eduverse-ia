using EduVerse.Core.Common;

namespace EduVerse.Core.Tutor;

/// <summary>What the tutor remembers of the last time (names already in the student's language). Nothing here is written by the AI.</summary>
public sealed record OpeningFacts(int? DaysAway, string? LastTopic, int LastCorrect, int LastTotal, string? HardTopic, int HardKnown, string? ExamTitle, int? ExamDays, int Due);

/// <summary>The line the Tutor opens a session with, and where it leads.</summary>
public sealed record OpeningNote(string Text, string Url);

/// <summary>"Last time we…": the tutor greets the student by what they did, not with a generic hello.</summary>
public static class Opening
{
    public const int LongAwayDays = 7;

    /// <summary>The opening, or null when there is nothing to remember yet (a new student).</summary>
    public static OpeningNote? Build(OpeningFacts facts, TeachLanguage language)
    {
        ArgumentNullException.ThrowIfNull(facts);
        if (facts.DaysAway is null || facts.LastTotal <= 0)
        {
            return null;
        }

        var when = facts.DaysAway switch
        {
            0 => Lang.T(language, "Earlier today", "Hoy", "Aujourd'hui", "Avui"),
            1 => Lang.T(language, "Yesterday", "Ayer", "Hier", "Ahir"),
            var d => Lang.T(language, $"{d} days ago", $"Hace {d} días", $"Il y a {d} jours", $"Fa {d} dies"),
        };
        var topic = facts.LastTopic ?? "";
        var last = Lang.T(language,
            $"{when} you answered {facts.LastCorrect} of {facts.LastTotal} on {topic}.",
            $"{when} acertaste {facts.LastCorrect} de {facts.LastTotal} en {topic}.",
            $"{when}, tu as réussi {facts.LastCorrect} sur {facts.LastTotal} en {topic}.",
            $"{when} vas encertar {facts.LastCorrect} de {facts.LastTotal} en {topic}.");
        if (facts.ExamTitle is { Length: > 0 } && facts.ExamDays is { } days and >= 0)
        {
            var exam = Lang.T(language,
                $"“{facts.ExamTitle}” is in {days} days: let's do today's part of the plan.",
                $"«{facts.ExamTitle}» es dentro de {days} días: hagamos la parte de hoy del plan.",
                $"« {facts.ExamTitle} » est dans {days} jours : faisons la partie du jour du plan.",
                $"«{facts.ExamTitle}» és d'aquí {days} dies: fem la part d'avui del pla.");
            return new OpeningNote($"{last} {exam}", "/Student/Plan");
        }

        if (facts.Due > 0)
        {
            var due = Lang.T(language,
                $"{facts.Due} topics are ready to review before you forget them.",
                $"Hay {facts.Due} temas listos para repasar antes de que se olviden.",
                $"{facts.Due} sujets sont prêts à réviser avant de les oublier.",
                $"Hi ha {facts.Due} temes a punt per repassar abans que s'obliden.");
            return new OpeningNote($"{last} {due}", "/Student/Quiz?mode=Review");
        }

        if (facts.HardTopic is { Length: > 0 })
        {
            var hard = Lang.T(language,
                $"{facts.HardTopic} is still the tricky one ({facts.HardKnown} % so far): a short try today?",
                $"{facts.HardTopic} sigue siendo el difícil ({facts.HardKnown} % por ahora): ¿probamos un rato hoy?",
                $"{facts.HardTopic} reste le plus difficile ({facts.HardKnown} % pour l'instant) : un petit essai aujourd'hui ?",
                $"{facts.HardTopic} continua sent el difícil ({facts.HardKnown} % de moment): provem una estona avui?");
            return new OpeningNote($"{last} {hard}", "/Student/Quiz");
        }

        var more = facts.DaysAway >= LongAwayDays
            ? Lang.T(language, "Welcome back! A few easy questions will warm you up.", "¡Qué bien verte de nuevo! Unas preguntas fáciles te calentarán.", "Content de te revoir ! Quelques questions faciles pour t'échauffer.", "Quin goig tornar a veure't! Unes preguntes fàcils t'escalfaran.")
            : Lang.T(language, "Shall we keep going?", "¿Seguimos?", "On continue ?", "Seguim?");
        return new OpeningNote($"{last} {more}", "/Student/Quiz");
    }
}
