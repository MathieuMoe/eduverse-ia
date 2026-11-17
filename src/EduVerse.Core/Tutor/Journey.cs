using EduVerse.Core.Catalog;
using EduVerse.Core.Common;

namespace EduVerse.Core.Tutor;

/// <summary>What is known of one calendar month for one topic: how many answers, how many right.</summary>
public sealed record TopicMonth(Area Area, Level Level, string Topic, int Year, int Month, int Answers, int Correct);

/// <summary>A topic that was weak in an earlier month and is solid in a later one: a real change over time, not a lucky week.</summary>
public sealed record TopicTurnaround(Area Area, Level Level, string Topic, int FromPercent, DateOnly FromMonth, int ToPercent, DateOnly ToMonth);

/// <summary>The kinds of moment "my path" can tell, each needing its own real, dated evidence.</summary>
public enum MilestoneKind
{
    TopicTurnaround,
    DefenceVerified,
    ConceptTaught,
    Streak,
}

/// <summary>One moment of "my path": a date, an icon, and the sentence about it, already in the student's language.</summary>
public sealed record Milestone(MilestoneKind Kind, DateOnly Date, string Icon, string Text);

/// <summary>
/// "My path": the student's own evolution told as a story, not a dashboard. Built entirely from facts the platform already measured for
/// other features (quiz answers, defended answers, concepts taught to Evo, active days) — nothing new is tracked, the same facts are
/// just read across months instead of inside a 7-day window. Every milestone needs real, dated evidence; nothing here is guessed.
/// </summary>
public static class Journey
{
    /// <summary>Answers a month needs before its rate for a topic counts for anything.</summary>
    public const int MinMonthAnswers = 4;

    public const int WeakPercent = 60;

    public const int SolidPercent = 75;

    /// <summary>At most this many topic turnarounds are told (the biggest jumps first).</summary>
    public const int MaxTurnarounds = 3;

    /// <summary>A streak shorter than this is not worth telling.</summary>
    public const int MinStreakDays = 3;

    /// <summary>
    /// Topics that went from weak to solid across months: the first month a topic was weak, and the latest later month where it was
    /// solid again (so a topic that fell back after looking solid once does not count). Biggest jump first, at most <see cref="MaxTurnarounds"/>.
    /// </summary>
    public static IReadOnlyList<TopicTurnaround> Turnarounds(IReadOnlyList<TopicMonth> months)
    {
        ArgumentNullException.ThrowIfNull(months);
        var result = new List<TopicTurnaround>();
        foreach (var group in months.Where(m => m.Answers >= MinMonthAnswers).GroupBy(m => (m.Area, m.Level, m.Topic)))
        {
            var ordered = group.OrderBy(m => m.Year).ThenBy(m => m.Month).ToList();
            var weakIndex = ordered.FindIndex(m => 100 * m.Correct < WeakPercent * m.Answers);
            if (weakIndex < 0)
            {
                continue;
            }

            var solid = ordered.Skip(weakIndex + 1).LastOrDefault(m => 100 * m.Correct >= SolidPercent * m.Answers);
            if (solid is null)
            {
                continue;
            }

            var weak = ordered[weakIndex];
            result.Add(new TopicTurnaround(group.Key.Area, group.Key.Level, group.Key.Topic,
                100 * weak.Correct / weak.Answers, new DateOnly(weak.Year, weak.Month, 1),
                100 * solid.Correct / solid.Answers, new DateOnly(solid.Year, solid.Month, 1)));
        }

        return result.OrderByDescending(t => t.ToPercent - t.FromPercent).ThenByDescending(t => t.ToMonth).Take(MaxTurnarounds).ToList();
    }

    /// <summary>The longest run of consecutive active days, and the date it ended; (0, null) when there are none.</summary>
    public static (int Days, DateOnly? End) BestStreak(IReadOnlyList<DateOnly> activeDays)
    {
        ArgumentNullException.ThrowIfNull(activeDays);
        int best = 0, run = 0;
        DateOnly? previous = null, bestEnd = null;
        foreach (var day in activeDays.Distinct().Order())
        {
            run = previous is { } p && p.AddDays(1) == day ? run + 1 : 1;
            if (run > best)
            {
                (best, bestEnd) = (run, day);
            }

            previous = day;
        }

        return (best, bestEnd);
    }

    public static string Say(TopicTurnaround t, string topicName, TeachLanguage language)
    {
        ArgumentNullException.ThrowIfNull(t);
        return Lang.T(language,
            $"{topicName} used to be hard for you: {t.FromPercent} % right, back then. Now you get it right {t.ToPercent} % of the time.",
            $"{topicName} te costaba: acertabas el {t.FromPercent} % de las veces, entonces. Ahora lo aciertas el {t.ToPercent} % de las veces.",
            $"{topicName} était difficile pour toi : {t.FromPercent} % de bonnes réponses, à l'époque. Tu réussis maintenant {t.ToPercent} % du temps.",
            $"{topicName} et costava: encertaves el {t.FromPercent} % de les vegades, llavors. Ara ho encertes el {t.ToPercent} % de les vegades.");
    }

    public static string SayDefence(string topicName, TeachLanguage language) => Lang.T(language,
        $"The first time you defended an answer about {topicName} with a new question and a real explanation — not just a lucky guess.",
        $"La primera vez que defendiste una respuesta sobre {topicName} con una pregunta nueva y una explicación de verdad — no un acierto de suerte.",
        $"La première fois que tu as défendu une réponse sur {topicName} avec une nouvelle question et une vraie explication — pas juste un coup de chance.",
        $"La primera vegada que vas defensar una resposta sobre {topicName} amb una pregunta nova i una explicació de debò — no un encert de sort.");

    public static string SayTaught(string conceptName, TeachLanguage language) => Lang.T(language,
        $"You explained {conceptName} so well that there was nothing left to ask. Teaching it well is the surest sign that you understood it.",
        $"Explicaste {conceptName} tan bien que no quedó nada por preguntar. Enseñarlo bien es la señal más segura de que lo entendiste.",
        $"Tu as si bien expliqué {conceptName} qu'il ne restait rien à demander. Bien l'enseigner est le signe le plus sûr que tu l'as compris.",
        $"Vas explicar {conceptName} tan bé que no va quedar res per preguntar. Ensenyar-ho bé és el senyal més segur que ho vas entendre.");

    public static string SayStreak(int days, TeachLanguage language) => Lang.T(language,
        $"Your longest run: {days} days in a row studying, without missing one.",
        $"Tu racha más larga: {days} días seguidos estudiando, sin fallar ni uno.",
        $"Ta plus longue série : {days} jours d'affilée à étudier, sans en manquer un.",
        $"La teva ratxa més llarga: {days} dies seguits estudiant, sense fallar-ne cap.");
}
