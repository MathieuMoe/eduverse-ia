using EduVerse.Core.Gamification;
using EduVerse.Core.Ideas;
using EduVerse.Core.Learning;
using EduVerse.Core.Tutor;
using EduVerse.Infrastructure.Data;
using EduVerse.Infrastructure.Gamification;
using Microsoft.EntityFrameworkCore;

namespace EduVerse.Infrastructure.Tutor;

public enum NoteOutcome
{
    Saved,
    TooShort,
    TooLong,
    NotAllowed,
    Full,
}

/// <summary>
/// "How I learn": the kind of mistake behind wrong answers (from the student's reasoning, which is not stored), the numbers of
/// the learner card (method that works best, autonomy, confidence) and what the student tells or corrects about themselves.
/// </summary>
public sealed class LearnerStore(EduVerseDb db, GamificationStore game, TimeProvider clock)
{
    /// <summary>Answers looked at for the card: the most recent ones.</summary>
    public const int RecentAnswers = 200;

    public static readonly IReadOnlyList<string> InsightKeys = ["method", "autonomy", "overconfident", "underconfident", "error", "reflective"];

    /// <summary>The wrong answer of the student that has no reflection yet; null otherwise.</summary>
    public async Task<QuizAnswer?> WrongAnswerAsync(string userId, string attemptId, int number, CancellationToken ct = default)
    {
        var answer = await db.QuizAnswers.AsNoTracking().FirstOrDefaultAsync(a => a.UserId == userId && a.AttemptId == attemptId && a.Number == number, ct).ConfigureAwait(false);
        if (answer is null || answer.Correct || await db.ThinkingNotes.AnyAsync(t => t.AttemptId == attemptId && t.Number == number, ct).ConfigureAwait(false))
        {
            return null;
        }

        return answer;
    }

    /// <summary>
    /// Keeps the kind of mistake (not the student's words), applies the remedy that needs data (a forgotten topic goes to
    /// today's review) and rewards the first reflections of the day.
    /// </summary>
    public async Task<(Remedy Remedy, Reward Reward)> SaveThinkingAsync(string userId, QuizAnswer answer, ThinkingError error, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(answer);
        var now = clock.GetUtcNow();
        var since = now.AddDays(-1);
        var rewarded = await db.ThinkingNotes.CountAsync(t => t.UserId == userId && t.At >= since, ct).ConfigureAwait(false) < Thinking.RewardedPerDay;
        db.ThinkingNotes.Add(new ThinkingNote { UserId = userId, AttemptId = answer.AttemptId, Number = answer.Number, Area = answer.Area, Level = answer.Level, Topic = answer.Topic, Error = error, At = now });
        var remedy = Thinking.RemedyFor(error);
        if (remedy == Remedy.Review)
        {
            var skill = await db.TopicSkills.FirstOrDefaultAsync(s => s.UserId == userId && s.Area == answer.Area && s.Level == answer.Level && s.Topic == answer.Topic, ct).ConfigureAwait(false);
            if (skill is not null && skill.DueAt > now)
            {
                skill.DueAt = now;
            }
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        var reward = new Reward(0, [], []);
        if (rewarded)
        {
            await game.AwardAsync(userId, Thinking.Xp, "thinking", false, ct).ConfigureAwait(false);
            reward = new Reward(Thinking.Xp, [], []);
        }

        return (remedy, reward.Plus(await game.CheckAsync(userId, ct).ConfigureAwait(false)));
    }

    public async Task<LearnerStats> StatsAsync(string userId, CancellationToken ct = default)
    {
        var methods = (await db.Attempts.AsNoTracking().Where(a => a.UserId == userId && !a.Diagnostic).Select(a => new { a.Method, a.Correct }).ToListAsync(ct).ConfigureAwait(false))
            .GroupBy(a => a.Method).Select(g => new MethodStat(g.Key, g.Count(), g.Count(a => a.Correct))).OrderBy(m => m.Method).ToList();
        var answers = await db.QuizAnswers.AsNoTracking().Where(a => a.UserId == userId).OrderByDescending(a => a.Id).Take(RecentAnswers)
            .Select(a => new { a.Correct, a.Hints, a.Rungs, a.Confidence }).ToListAsync(ct).ConfigureAwait(false);
        var errors = (await db.ThinkingNotes.AsNoTracking().Where(t => t.UserId == userId).Select(t => t.Error).ToListAsync(ct).ConfigureAwait(false))
            .GroupBy(e => e).ToDictionary(g => g.Key, g => g.Count());
        var notes = await db.LearnerNotes.AsNoTracking().Where(n => n.UserId == userId).OrderBy(n => n.Id).Select(n => n.Text).ToListAsync(ct).ConfigureAwait(false);
        var hidden = await db.LearnerHidden.AsNoTracking().Where(h => h.UserId == userId).Select(h => h.Key).ToListAsync(ct).ConfigureAwait(false);
        return new LearnerStats(methods, answers.Count, answers.Count(a => a.Correct), answers.Count(a => a.Correct && a.Hints == 0 && a.Rungs == 0),
            answers.Count(a => a.Confidence >= 2), answers.Count(a => a.Confidence >= 2 && !a.Correct),
            answers.Count(a => a.Confidence <= 0), answers.Count(a => a.Confidence <= 0 && a.Correct),
            errors, notes, hidden.ToHashSet(StringComparer.Ordinal));
    }

    /// <summary>The method to start a new concept with: the one that works best for this student, unless they hid that observation.</summary>
    public async Task<ExplanationMethod?> PreferredMethodAsync(string userId, CancellationToken ct = default)
    {
        var stats = await StatsAsync(userId, ct).ConfigureAwait(false);
        return stats.Hidden.Contains("method") ? null : LearnerProfile.BestMethod(stats);
    }

    public async Task<IReadOnlyList<LearnerNote>> NotesAsync(string userId, CancellationToken ct = default) =>
        await db.LearnerNotes.AsNoTracking().Where(n => n.UserId == userId).OrderBy(n => n.Id).ToListAsync(ct).ConfigureAwait(false);

    public async Task<NoteOutcome> AddNoteAsync(string userId, string? text, CancellationToken ct = default)
    {
        var clean = IdeaRules.Clean(text);
        if (IdeaRules.Normalize(clean).Length < 3)
        {
            return NoteOutcome.TooShort;
        }

        if (clean.Length > LearnerProfile.MaxNote)
        {
            return NoteOutcome.TooLong;
        }

        if (!IdeaRules.Safe(clean) || clean.Contains('<', StringComparison.Ordinal) || clean.Contains('>', StringComparison.Ordinal))
        {
            return NoteOutcome.NotAllowed;
        }

        if (await db.LearnerNotes.CountAsync(n => n.UserId == userId, ct).ConfigureAwait(false) >= LearnerProfile.MaxNotes)
        {
            return NoteOutcome.Full;
        }

        db.LearnerNotes.Add(new LearnerNote { UserId = userId, Text = clean, At = clock.GetUtcNow() });
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return NoteOutcome.Saved;
    }

    public async Task<bool> RemoveNoteAsync(string userId, int id, CancellationToken ct = default)
    {
        var note = await db.LearnerNotes.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId, ct).ConfigureAwait(false);
        if (note is null)
        {
            return false;
        }

        db.LearnerNotes.Remove(note);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return true;
    }

    /// <summary>"That is not me": hides an observation (the tutor stops using it) or shows it again.</summary>
    public async Task<bool> SetHiddenAsync(string userId, string key, bool hidden, CancellationToken ct = default)
    {
        if (!InsightKeys.Contains(key))
        {
            return false;
        }

        var row = await db.LearnerHidden.FirstOrDefaultAsync(h => h.UserId == userId && h.Key == key, ct).ConfigureAwait(false);
        if (hidden && row is null)
        {
            db.LearnerHidden.Add(new LearnerHidden { UserId = userId, Key = key });
        }
        else if (!hidden && row is not null)
        {
            db.LearnerHidden.Remove(row);
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return true;
    }
}
