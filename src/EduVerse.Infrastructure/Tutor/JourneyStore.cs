using EduVerse.Core.Catalog;
using EduVerse.Core.Tutor;
using EduVerse.Infrastructure.Data;
using EduVerse.Infrastructure.Gamification;
using Microsoft.EntityFrameworkCore;

namespace EduVerse.Infrastructure.Tutor;

/// <summary>The first time the student truly defended an answer about a topic (code-verified: a new question right, and reasoned).</summary>
public sealed record DefenceMilestone(Area Area, Level Level, string Topic, DateOnly Date);

/// <summary>The first concept the student explained completely to Evo (every key idea found, not just "ran out of rounds").</summary>
public sealed record TaughtMilestone(string ConceptKey, DateOnly Date);

/// <summary>Raw candidates for "my path": real, dated evidence only. Names are resolved and the timeline is ordered by the caller.</summary>
public sealed record JourneyCandidates(IReadOnlyList<TopicTurnaround> Turnarounds, DefenceMilestone? FirstDefence, TaughtMilestone? FirstTaught, int BestStreakDays, DateOnly? BestStreakEnd);

/// <summary>
/// Builds "my path" from the student's whole history. Nothing new is tracked here: the same tables the detective, the defence and
/// "explain it to Evo" already use are just read across months instead of inside a short window.
/// </summary>
public sealed class JourneyStore(EduVerseDb db, GamificationStore game)
{
    public async Task<JourneyCandidates> CandidatesAsync(string userId, CancellationToken ct = default)
    {
        var answers = await db.QuizAnswers.AsNoTracking().Where(a => a.UserId == userId && !a.Topic.StartsWith("topic:"))
            .Select(a => new { a.Area, a.Level, a.Topic, a.Correct, a.At }).ToListAsync(ct).ConfigureAwait(false);
        var months = answers.Select(a => (a.Area, a.Level, a.Topic, Day: game.DayOf(a.At), a.Correct))
            .GroupBy(a => (a.Area, a.Level, a.Topic, a.Day.Year, a.Day.Month))
            .Select(g => new TopicMonth(g.Key.Area, g.Key.Level, g.Key.Topic, g.Key.Year, g.Key.Month, g.Count(), g.Count(x => x.Correct)))
            .ToList();
        var turnarounds = Journey.Turnarounds(months);

        var defence = await db.DefenceResults.AsNoTracking().Where(d => d.UserId == userId && d.State == DefenceState.Done && d.Transfer && d.Reasoned)
            .OrderBy(d => d.At).Select(d => new { d.Area, d.Level, d.Topic, d.At }).FirstOrDefaultAsync(ct).ConfigureAwait(false);
        var firstDefence = defence is null ? null : new DefenceMilestone(defence.Area, defence.Level, defence.Topic, game.DayOf(defence.At));

        var sessions = await db.TeachSessions.AsNoTracking().Where(s => s.UserId == userId && s.FinishedAt != null)
            .OrderBy(s => s.FinishedAt).Select(s => new { s.Concept, s.Covered, FinishedAt = s.FinishedAt!.Value }).ToListAsync(ct).ConfigureAwait(false);
        TaughtMilestone? firstTaught = null;
        foreach (var s in sessions)
        {
            var concept = TeachConcepts.Find(s.Concept);
            if (concept is not null && s.Covered.Split(',', StringSplitOptions.RemoveEmptyEntries).Length >= concept.Ideas.Count)
            {
                firstTaught = new TaughtMilestone(s.Concept, game.DayOf(s.FinishedAt));
                break;
            }
        }

        var xpDays = await db.XpEvents.AsNoTracking().Where(x => x.UserId == userId && (x.Reason == "answer" || x.Reason == "tutor"))
            .Select(x => x.At).ToListAsync(ct).ConfigureAwait(false);
        var (bestDays, bestEnd) = Journey.BestStreak(xpDays.Select(game.DayOf).ToList());

        return new JourneyCandidates(turnarounds, firstDefence, firstTaught, bestDays, bestEnd);
    }
}
