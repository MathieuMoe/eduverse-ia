using EduVerse.Core.Tutor;
using EduVerse.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EduVerse.Infrastructure.Tutor;

/// <summary>The facts of the last 7 days for the family letter. Topic names come back as keys.</summary>
public sealed class LetterStore(EduVerseDb db, PlanStore plans, TimeProvider clock)
{
    public const int MinAnswersForTopic = 3;

    public async Task<WeekFacts> FactsAsync(string userId, string firstName, CancellationToken ct = default)
    {
        var since = clock.GetUtcNow().AddDays(-7);
        var answers = await db.QuizAnswers.AsNoTracking().Where(a => a.UserId == userId && a.At >= since).Select(a => new { a.Topic, a.Correct, a.At, a.Confidence }).ToListAsync(ct).ConfigureAwait(false);
        var understood = await db.DefenceResults.CountAsync(d => d.UserId == userId && d.At >= since && d.State == DefenceState.Done && d.Transfer && d.Reasoned, ct).ConfigureAwait(false);
        var topics = answers.GroupBy(a => a.Topic).Where(g => g.Count() >= MinAnswersForTopic)
            .Select(g => (Topic: g.Key, Rate: g.Count(a => a.Correct) / (double)g.Count())).ToList();
        var best = topics.Where(t => t.Rate >= 0.7).OrderByDescending(t => t.Rate).Select(t => t.Topic).FirstOrDefault();
        var work = topics.Where(t => t.Rate < 0.6).OrderBy(t => t.Rate).Select(t => t.Topic).FirstOrDefault();
        var goal = await plans.GetAsync(userId, ct).ConfigureAwait(false);
        return new WeekFacts(firstName, answers.Select(a => a.At.UtcDateTime.Date).Distinct().Count(), answers.Count, answers.Count(a => a.Correct), understood, best, work,
            goal?.Goal.Title, goal is null ? null : goal.ExamOn.DayNumber - plans.Today.DayNumber)
        {
            SureWrongTopic = answers.Where(a => a.Confidence >= 2 && !a.Correct).GroupBy(a => a.Topic).Where(g => g.Count() >= 2).OrderByDescending(g => g.Count()).Select(g => g.Key).FirstOrDefault(),
            UnsureRightTopic = answers.Where(a => a.Confidence <= 0).GroupBy(a => a.Topic).Where(g => g.Count() >= 2 && g.All(a => a.Correct)).OrderByDescending(g => g.Count()).Select(g => g.Key).FirstOrDefault(),
        };
    }
}
