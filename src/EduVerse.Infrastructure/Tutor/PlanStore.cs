using EduVerse.Core.Catalog;
using EduVerse.Core.Curriculum;
using EduVerse.Core.Ideas;
using EduVerse.Core.Tutor;
using EduVerse.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EduVerse.Infrastructure.Tutor;

public enum GoalOutcome
{
    Saved,
    BadDate,
    NotAvailable,
    BadTitle,
}

public sealed record PlanTask(string Topic, int Known, bool Done);

public sealed record GoalView(StudyGoal Goal, StudyPlanResult Plan, IReadOnlyList<PlanTask> Today, bool MockToday, bool MockDone, IReadOnlyList<PlanTopic> Topics)
{
    public DateOnly ExamOn => DateOnly.FromDateTime(Goal.ExamOn);
}

/// <summary>
/// Goal of the student (an exam on a date) and its plan. The plan is not stored: it is built again from what the student
/// knows today, so it adjusts itself after every session.
/// </summary>
public sealed class PlanStore(EduVerseDb db, TimeProvider clock)
{
    public const int MaxTitle = 60;

    public DateOnly Today => DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);

    public async Task<GoalOutcome> SetAsync(string userId, Area area, Level level, string? title, DateOnly examOn, int minutes, CancellationToken ct = default)
    {
        var clean = (title ?? "").Trim();
        if (clean.Length > MaxTitle || (clean.Length > 0 && !IdeaRules.Safe(clean)))
        {
            return GoalOutcome.BadTitle;
        }

        if (!Catalog.Available(area, level))
        {
            return GoalOutcome.NotAvailable;
        }

        var days = examOn.DayNumber - Today.DayNumber;
        if (days < 1 || days > StudyPlan.MaxDays)
        {
            return GoalOutcome.BadDate;
        }

        var goal = await db.StudyGoals.FirstOrDefaultAsync(g => g.UserId == userId, ct).ConfigureAwait(false);
        if (goal is null)
        {
            goal = new StudyGoal { UserId = userId };
            db.StudyGoals.Add(goal);
        }

        (goal.Area, goal.Level, goal.Title, goal.ExamOn, goal.Minutes, goal.CreatedAt) = (area, level, clean, examOn.ToDateTime(TimeOnly.MinValue), StudyPlan.Clamp(minutes), clock.GetUtcNow());
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return GoalOutcome.Saved;
    }

    public async Task ClearAsync(string userId, CancellationToken ct = default) =>
        await db.StudyGoals.Where(g => g.UserId == userId).ExecuteDeleteAsync(ct).ConfigureAwait(false);

    public async Task<GoalView?> GetAsync(string userId, CancellationToken ct = default)
    {
        var goal = await db.StudyGoals.AsNoTracking().FirstOrDefaultAsync(g => g.UserId == userId, ct).ConfigureAwait(false);
        if (goal is null)
        {
            return null;
        }

        var skills = await db.TopicSkills.AsNoTracking().Where(s => s.UserId == userId && s.Area == goal.Area && s.Level == goal.Level)
            .ToDictionaryAsync(s => s.Topic, s => s, ct).ConfigureAwait(false);
        var topics = AreaSubjects.Topics(goal.Area, goal.Level)
            .Select(t => new PlanTopic(t, skills.TryGetValue(t, out var s) && s.Attempts > 0 ? (int)Math.Round(100 * s.PKnown) : StudyPlan.NeutralKnown)).ToList();
        var today = Today;
        var plan = StudyPlan.Build(today, DateOnly.FromDateTime(goal.ExamOn), goal.Minutes, topics);
        var day = plan.Days.FirstOrDefault(d => d.Date == today);

        var start = new DateTimeOffset(today.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var counts = await db.QuizAnswers.AsNoTracking().Where(a => a.UserId == userId && a.Area == goal.Area && a.Level == goal.Level && a.At >= start)
            .GroupBy(a => a.Topic).Select(g => new { Topic = g.Key, Count = g.Count() }).ToDictionaryAsync(g => g.Topic, g => g.Count, ct).ConfigureAwait(false);
        var tasks = (day?.Topics ?? []).Select(t => new PlanTask(t, topics.First(x => x.Topic == t).Known, counts.GetValueOrDefault(t) >= StudyPlan.DoneAnswers)).ToList();
        var mockDone = day is { Mock: true } && await db.QuizAttempts.AnyAsync(a => a.UserId == userId && a.Area == goal.Area && a.Level == goal.Level && a.Mode == QuizMode.Practice && a.FinishedAt >= start, ct).ConfigureAwait(false);
        return new GoalView(goal, plan, tasks, day?.Mock ?? false, mockDone, topics);
    }
}
