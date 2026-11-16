using EduVerse.Core.Common;

namespace EduVerse.Core.Tutor;

/// <summary>A topic of the subject and what the student knows of it (0–100).</summary>
public sealed record PlanTopic(string Topic, int Known);

/// <summary>One day of the plan: the topics to practise (5 minutes each) and whether it is the mock test.</summary>
public sealed record PlanDay(DateOnly Date, IReadOnlyList<string> Topics, bool Mock);

public enum PlanFeasibility
{
    /// <summary>Plenty of time: the plan fits half of the time the student gave.</summary>
    Comfortable,

    OnTrack,

    /// <summary>It fits, but with no spare day.</summary>
    Tight,

    /// <summary>It does not fit in the minutes per day: the code says how many more are needed.</summary>
    NotEnough,
}

public sealed record StudyPlanResult(
    IReadOnlyList<PlanDay> Days, int DaysLeft, int SessionsNeeded, int SessionsAvailable, PlanFeasibility Feasibility, int Readiness, int MinutesNeeded)
{
    public bool Over => DaysLeft <= 0;
}

/// <summary>
/// Plan towards an exam, decided in code: the weakest topics first, spaced (a topic never twice on the same day),
/// a mock test the day before. Built again from what the student knows today, so it adjusts itself every day.
/// </summary>
public static class StudyPlan
{
    public const int MinutesPerSession = 5;
    public const int MinMinutes = 10;
    public const int MaxMinutes = 60;
    public const int MaxDays = 120;
    public const int MinMockDays = 3;
    public const int MockSessions = 2;
    public const int DoneAnswers = 3;
    public const int NeutralKnown = 25;

    public static int Clamp(int minutes) => Math.Clamp(minutes, MinMinutes, MaxMinutes);

    /// <summary>Practice sessions a topic needs by how much the student knows it (a known topic still gets one review).</summary>
    public static int SessionsFor(int known) => known switch { < 40 => 4, < 60 => 3, < 80 => 2, _ => 1 };

    public static StudyPlanResult Build(DateOnly today, DateOnly examOn, int minutesPerDay, IReadOnlyList<PlanTopic> topics)
    {
        ArgumentNullException.ThrowIfNull(topics);
        var capacity = Clamp(minutesPerDay) / MinutesPerSession;
        var daysLeft = Math.Clamp(examOn.DayNumber - today.DayNumber, 0, MaxDays);
        var readiness = topics.Count == 0 ? 100 : (int)Math.Round(topics.Average(t => (double)Math.Clamp(t.Known, 0, 100)));
        var needed = topics.Sum(t => SessionsFor(t.Known));
        if (daysLeft == 0)
        {
            return new StudyPlanResult([], 0, needed, 0, PlanFeasibility.NotEnough, readiness, 0);
        }

        var mockDay = daysLeft >= MinMockDays ? daysLeft - 1 : -1;
        var days = Enumerable.Range(0, daysLeft).Select(_ => new List<string>()).ToList();
        var room = Enumerable.Range(0, daysLeft).Select(i => i == mockDay ? Math.Max(0, capacity - MockSessions) : capacity).ToArray();
        var available = room.Sum();
        var ratio = available == 0 ? 2.0 : (double)needed / available;
        var feasibility = ratio switch { <= 0.5 => PlanFeasibility.Comfortable, <= 0.85 => PlanFeasibility.OnTrack, <= 1 => PlanFeasibility.Tight, _ => PlanFeasibility.NotEnough };
        var studyDays = Math.Max(1, room.Count(r => r > 0));
        var target = Math.Max(1, (int)Math.Ceiling((double)needed / studyDays));

        // Weakest first; one session of every topic per round, so the same topic is spaced out
        var order = topics.OrderBy(t => t.Known).ThenBy(t => t.Topic, StringComparer.Ordinal).ToList();
        var queue = new List<string>();
        for (var round = 0; round < 4; round++)
        {
            queue.AddRange(order.Where(t => SessionsFor(t.Known) > round).Select(t => t.Topic));
        }

        var cursor = 0;
        foreach (var topic in queue)
        {
            var placed = false;
            for (var pass = 0; pass < 2 && !placed; pass++)
            {
                for (var step = 0; step < daysLeft && !placed; step++)
                {
                    var d = (cursor + step) % daysLeft;
                    var limit = pass == 0 ? Math.Min(target, room[d]) : room[d];
                    if (days[d].Count < limit && !days[d].Contains(topic))
                    {
                        days[d].Add(topic);
                        cursor = (d + 1) % daysLeft;
                        placed = true;
                    }
                }
            }
        }

        var plan = days.Select((topicsOfDay, i) => new PlanDay(today.AddDays(i), topicsOfDay, i == mockDay)).ToList();
        var perDay = (int)Math.Ceiling((double)(needed + (mockDay >= 0 ? MockSessions : 0)) / daysLeft);
        var minutesNeeded = Math.Max(0, perDay * MinutesPerSession);
        return new StudyPlanResult(plan, daysLeft, needed, available, feasibility, readiness, feasibility == PlanFeasibility.NotEnough ? minutesNeeded : 0);
    }

    /// <summary>The Tutor's reading of the plan (the numbers come from the code).</summary>
    public static string Advice(StudyPlanResult plan, string title, TeachLanguage language)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var what = string.IsNullOrWhiteSpace(title) ? Lang.T(language, "your exam", "tu examen", "ton examen", "el teu examen") : title;
        if (plan.Over)
        {
            return Lang.T(language,
                $"The day of {what} has come. Tell me how it went, and we can plan the next one.",
                $"Ha llegado el día de {what}. Cuéntame cómo ha ido y planificamos el siguiente.",
                $"Le jour de {what} est arrivé. Raconte-moi comment ça s'est passé et on prépare le suivant.",
                $"Ha arribat el dia de {what}. Explica'm com ha anat i planifiquem el següent.");
        }

        var left = plan.DaysLeft;
        var ready = plan.Readiness;
        return plan.Feasibility switch
        {
            PlanFeasibility.NotEnough => Lang.T(language,
                $"{what} is in {left} days and you are at {ready} %. With your minutes it does not fit: you would need {plan.MinutesNeeded} minutes a day, or we choose the topics that matter most.",
                $"{what} es en {left} días y vas por el {ready} %. Con tus minutos no cabe: harían falta {plan.MinutesNeeded} minutos al día, o elegimos los temas que más importan.",
                $"{what} est dans {left} jours et tu en es à {ready} %. Avec tes minutes ça ne rentre pas : il faudrait {plan.MinutesNeeded} minutes par jour, ou on choisit les thèmes les plus importants.",
                $"{what} és d'aquí {left} dies i vas pel {ready} %. Amb els teus minuts no hi cap: caldrien {plan.MinutesNeeded} minuts al dia, o triem els temes que més importen."),
            PlanFeasibility.Tight => Lang.T(language,
                $"{what} is in {left} days and you are at {ready} %. It fits, but without spare days: do not skip any.",
                $"{what} es en {left} días y vas por el {ready} %. Cabe, pero sin días de sobra: no te saltes ninguno.",
                $"{what} est dans {left} jours et tu en es à {ready} %. Ça rentre, mais sans jour de marge : n'en saute aucun.",
                $"{what} és d'aquí {left} dies i vas pel {ready} %. Hi cap, però sense dies de sobra: no te'n saltis cap."),
            PlanFeasibility.OnTrack => Lang.T(language,
                $"{what} is in {left} days and you are at {ready} %. You are on track: a few minutes a day are enough.",
                $"{what} es en {left} días y vas por el {ready} %. Vas bien: bastan unos minutos al día.",
                $"{what} est dans {left} jours et tu en es à {ready} %. Tu es dans les temps : quelques minutes par jour suffisent.",
                $"{what} és d'aquí {left} dies i vas pel {ready} %. Vas bé: bastaran uns minuts al dia."),
            _ => Lang.T(language,
                $"{what} is in {left} days and you are at {ready} %. You have time to spare: we can go calmly.",
                $"{what} es en {left} días y vas por el {ready} %. Te sobra tiempo: podemos ir con calma.",
                $"{what} est dans {left} jours et tu en es à {ready} %. Tu as du temps : on peut y aller calmement.",
                $"{what} és d'aquí {left} dies i vas pel {ready} %. Et sobra temps: podem anar amb calma."),
        };
    }
}
