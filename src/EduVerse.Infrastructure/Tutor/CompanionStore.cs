using EduVerse.Core.Catalog;
using EduVerse.Core.Common;
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
public sealed class CompanionStore(EduVerseDb db, QuizStore quizzes, GamificationStore game, Ideas.IdeaStore ideas, LearnerStore learner, PlanStore plans, TimeProvider clock)
{
    public const int Kept = 200;

    public async Task<StudentSnapshot> SnapshotAsync(string userId, string name, string page, string title, CompanionNames names, TeachLanguage language, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(names);
        var profile = await game.ProfileAsync(userId, ct).ConfigureAwait(false);
        var summary = await game.SummaryAsync(userId, ct).ConfigureAwait(false);
        var due = await quizzes.DueCountAsync(userId, ct).ConfigureAwait(false);
        var misconceptions = (await quizzes.MistakesAsync(userId, ct).ConfigureAwait(false)).Count;
        var overcome = (await quizzes.MisconceptionMemoryAsync(userId, ct).ConfigureAwait(false))
            .Where(m => m.Status == Core.Learning.MisconceptionStatus.Overcome)
            .Select(m => Core.Learning.Misconceptions.Describe(m.Code, Core.Curriculum.BuiltInSubjects.FractionsKey, language)).ToList();
        var guessed = (await quizzes.GuessedTopicsAsync(userId, ct).ConfigureAwait(false))
            .Select(g => new GuessedTopic(g.Area, g.Level, g.Topic, names.Topic(g.Topic), names.Area(g.Area), g.GuessPercent)).ToList();
        var weak = await db.TopicSkills.AsNoTracking().Where(s => s.UserId == userId && s.Attempts >= 2 && s.PKnown < 0.6)
            .OrderBy(s => s.PKnown).Take(3).ToListAsync(ct).ConfigureAwait(false);
        var recent = await db.QuizAttempts.AsNoTracking().Where(a => a.UserId == userId && a.FinishedAt != null)
            .OrderByDescending(a => a.FinishedAt).Take(5).ToListAsync(ct).ConfigureAwait(false);
        var tests = await db.QuizAttempts.CountAsync(a => a.UserId == userId && a.FinishedAt != null, ct).ConfigureAwait(false);
        var interviews = await db.QuizAttempts.CountAsync(a => a.UserId == userId && a.Mode == QuizMode.Interview && a.FinishedAt != null, ct).ConfigureAwait(false);
        var taught = await db.TeachSessions.AsNoTracking().Where(t => t.UserId == userId && t.FinishedAt != null).Select(t => t.Concept).Distinct().ToListAsync(ct).ConfigureAwait(false);
        // The first name (the nickname is for the public rankings)
        var now = clock.GetUtcNow();
        var (weekStart, before) = (now.AddDays(-7), now.AddDays(-14));
        var answers = await db.QuizAnswers.AsNoTracking().Where(a => a.UserId == userId && a.At >= before)
            .Select(a => new { a.Area, a.Correct, Recent = a.At >= weekStart }).ToListAsync(ct).ConfigureAwait(false);
        var testsWeek = await db.QuizAttempts.Where(a => a.UserId == userId && a.FinishedAt >= before)
            .Select(a => a.FinishedAt >= weekStart).ToListAsync(ct).ConfigureAwait(false);
        var xpWeek = await db.XpEvents.Where(x => x.UserId == userId && x.At >= before)
            .Select(x => new { x.Amount, Recent = x.At >= weekStart }).ToListAsync(ct).ConfigureAwait(false);
        static int Rate(int right, int total) => total == 0 ? 0 : (int)Math.Round(100.0 * right / total);
        var improving = answers.GroupBy(a => a.Area).Where(g =>
        {
            var (now7, prev7) = (g.Where(a => a.Recent).ToList(), g.Where(a => !a.Recent).ToList());
            return now7.Count >= 5 && prev7.Count >= 5 && Rate(now7.Count(a => a.Correct), now7.Count) - Rate(prev7.Count(a => a.Correct), prev7.Count) >= 10;
        }).Select(g => names.Area(g.Key)).ToList();
        var (thisWeek, lastWeek) = (answers.Where(a => a.Recent).ToList(), answers.Where(a => !a.Recent).ToList());
        var evolution = new Evolution(
            thisWeek.Count, Rate(thisWeek.Count(a => a.Correct), thisWeek.Count), lastWeek.Count, Rate(lastWeek.Count(a => a.Correct), lastWeek.Count),
            testsWeek.Count(r => r), testsWeek.Count(r => !r), xpWeek.Where(x => x.Recent).Sum(x => x.Amount), xpWeek.Where(x => !x.Recent).Sum(x => x.Amount), improving);
        var displayName = name.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? profile.Nickname;
        return new StudentSnapshot(
            displayName, profile.Level, summary.Level, names.Rank(summary.Rank), summary.Streak, summary.PlayedToday, due, misconceptions, tests,
            weak.Select(w => new WeakTopic(w.Area, w.Level, w.Topic, names.Topic(w.Topic), names.Area(w.Area), (int)Math.Round(100 * w.PKnown))).ToList(),
            recent.Select(r => new RecentTest(names.Area(r.Area), names.Mode(r.Mode), r.Correct, r.Length, DateOnly.FromDateTime(r.FinishedAt!.Value.UtcDateTime), r.Topic is { } rt ? names.Topic(rt) : null)).ToList(),
            taught, interviews, page ?? "", title ?? "", evolution, await ideas.NewsAsync(userId, ct).ConfigureAwait(false), await learner.StatsAsync(userId, ct).ConfigureAwait(false), await PlanBriefAsync(userId, names, ct).ConfigureAwait(false), overcome, guessed);
    }

    private async Task<PlanBrief?> PlanBriefAsync(string userId, CompanionNames names, CancellationToken ct)
    {
        var view = await plans.GetAsync(userId, ct).ConfigureAwait(false);
        if (view is null)
        {
            return null;
        }

        var left = view.Today.Count(t => !t.Done) + (view.MockToday && !view.MockDone ? 1 : 0);
        var title = string.IsNullOrWhiteSpace(view.Goal.Title) ? names.Area(view.Goal.Area) : view.Goal.Title;
        return new PlanBrief(title, view.Plan, left);
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
