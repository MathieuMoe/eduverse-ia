using EduVerse.Core.Tutor;
using EduVerse.Infrastructure.Data;
using EduVerse.Infrastructure.Quizzes;
using Microsoft.EntityFrameworkCore;

namespace EduVerse.Infrastructure.Tutor;

/// <summary>Gathers what the tutor remembers of the last session. Topic names come back as keys: the page puts them in the student's language.</summary>
public sealed class OpeningStore(EduVerseDb db, QuizStore quizzes, PlanStore plans, TimeProvider clock)
{
    public async Task<OpeningFacts> FactsAsync(string userId, CancellationToken ct = default)
    {
        var lastAt = await db.QuizAnswers.AsNoTracking().Where(a => a.UserId == userId).OrderByDescending(a => a.At).Select(a => (DateTimeOffset?)a.At).FirstOrDefaultAsync(ct).ConfigureAwait(false);
        if (lastAt is null)
        {
            return new OpeningFacts(null, null, 0, 0, null, 0, null, null, 0);
        }

        var now = clock.GetUtcNow();
        var attemptId = await db.QuizAnswers.AsNoTracking().Where(a => a.UserId == userId && a.At == lastAt).Select(a => a.AttemptId).FirstAsync(ct).ConfigureAwait(false);
        var answers = await db.QuizAnswers.AsNoTracking().Where(a => a.UserId == userId && a.AttemptId == attemptId).Select(a => new { a.Topic, a.Correct }).ToListAsync(ct).ConfigureAwait(false);
        var topic = answers.GroupBy(a => a.Topic).OrderByDescending(g => g.Count()).First().Key;
        var hard = await db.TopicSkills.AsNoTracking().Where(s => s.UserId == userId && s.Attempts >= 3 && s.PKnown < 0.6).OrderBy(s => s.PKnown).FirstOrDefaultAsync(ct).ConfigureAwait(false);
        var goal = await plans.GetAsync(userId, ct).ConfigureAwait(false);
        var today = plans.Today;
        var days = (int)(now.UtcDateTime.Date - lastAt.Value.UtcDateTime.Date).TotalDays;
        return new OpeningFacts(Math.Max(0, days), topic, answers.Count(a => a.Correct), answers.Count, hard?.Topic, hard is null ? 0 : (int)Math.Round(hard.PKnown * 100),
            goal?.Goal.Title, goal is null ? null : goal.ExamOn.DayNumber - today.DayNumber, await quizzes.DueCountAsync(userId, ct).ConfigureAwait(false));
    }
}
