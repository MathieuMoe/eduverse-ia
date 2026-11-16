using EduVerse.Core.Catalog;
using EduVerse.Core.Gamification;
using EduVerse.Core.Tutor;
using EduVerse.Infrastructure.Data;
using EduVerse.Infrastructure.Gamification;
using EduVerse.Infrastructure.Quizzes;
using Microsoft.EntityFrameworkCore;

namespace EduVerse.Infrastructure.Tutor;

/// <summary>Names in the language of the student (the resources live in the web project).</summary>
public sealed record CompanionNames(Func<string, string> Topic, Func<Area, string> Area, Func<string, string> Rank, Func<QuizMode, string> Mode);

/// <summary>What Evo knows of a student (tests, topics, reviews, streak, explanations, interviews) and the conversation with Evo.</summary>
public sealed class CompanionStore(EduVerseDb db, QuizStore quizzes, GamificationStore game, TimeProvider clock)
{
    public const int Kept = 200;

    public async Task<StudentSnapshot> SnapshotAsync(string userId, string name, string page, string title, CompanionNames names, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(names);
        var profile = await game.ProfileAsync(userId, ct).ConfigureAwait(false);
        var summary = await game.SummaryAsync(userId, ct).ConfigureAwait(false);
        var due = await quizzes.DueCountAsync(userId, ct).ConfigureAwait(false);
        var misconceptions = (await quizzes.MistakesAsync(userId, ct).ConfigureAwait(false)).Count;
        var weak = await db.TopicSkills.AsNoTracking().Where(s => s.UserId == userId && s.Attempts >= 2 && s.PKnown < 0.6)
            .OrderBy(s => s.PKnown).Take(3).ToListAsync(ct).ConfigureAwait(false);
        var recent = await db.QuizAttempts.AsNoTracking().Where(a => a.UserId == userId && a.FinishedAt != null)
            .OrderByDescending(a => a.FinishedAt).Take(5).ToListAsync(ct).ConfigureAwait(false);
        var tests = await db.QuizAttempts.CountAsync(a => a.UserId == userId && a.FinishedAt != null, ct).ConfigureAwait(false);
        var interviews = await db.QuizAttempts.CountAsync(a => a.UserId == userId && a.Mode == QuizMode.Interview && a.FinishedAt != null, ct).ConfigureAwait(false);
        var taught = await db.TeachSessions.AsNoTracking().Where(t => t.UserId == userId && t.FinishedAt != null).Select(t => t.Concept).Distinct().ToListAsync(ct).ConfigureAwait(false);
        // The first name (the nickname is for the public rankings)
        var displayName = name.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? profile.Nickname;
        return new StudentSnapshot(
            displayName, profile.Level, summary.Level, names.Rank(summary.Rank), summary.Streak, summary.PlayedToday, due, misconceptions, tests,
            weak.Select(w => new WeakTopic(w.Area, w.Level, w.Topic, names.Topic(w.Topic), names.Area(w.Area), (int)Math.Round(100 * w.PKnown))).ToList(),
            recent.Select(r => new RecentTest(names.Area(r.Area), names.Mode(r.Mode), r.Correct, r.Length, DateOnly.FromDateTime(r.FinishedAt!.Value.UtcDateTime))).ToList(),
            taught, interviews, page ?? "", title ?? "");
    }

    public async Task<IReadOnlyList<CompanionMessage>> HistoryAsync(string userId, int take = 30, CancellationToken ct = default)
    {
        var last = await db.CompanionMessages.AsNoTracking().Where(m => m.UserId == userId).OrderByDescending(m => m.Id).Take(take).ToListAsync(ct).ConfigureAwait(false);
        last.Reverse();
        return last;
    }

    public async Task SaveAsync(string userId, string question, CompanionReply reply, string page, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(reply);
        var now = clock.GetUtcNow();
        db.CompanionMessages.Add(new CompanionMessage { UserId = userId, FromStudent = true, Text = question, Page = page, At = now });
        db.CompanionMessages.Add(new CompanionMessage { UserId = userId, FromStudent = false, Text = reply.Text, Actions = string.Join(',', reply.Actions), Page = page, At = now.AddMilliseconds(1) });
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        // Only the last messages are kept.
        var old = await db.CompanionMessages.Where(m => m.UserId == userId).OrderByDescending(m => m.Id).Skip(Kept).ToListAsync(ct).ConfigureAwait(false);
        if (old.Count > 0)
        {
            db.CompanionMessages.RemoveRange(old);
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    public async Task<int> ClearAsync(string userId, CancellationToken ct = default) =>
        await db.CompanionMessages.Where(m => m.UserId == userId).ExecuteDeleteAsync(ct).ConfigureAwait(false);
}
