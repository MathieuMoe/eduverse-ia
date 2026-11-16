using EduVerse.Core.Tutor;
using EduVerse.Infrastructure.Data;
using EduVerse.Infrastructure.Gamification;
using Microsoft.EntityFrameworkCore;

namespace EduVerse.Infrastructure.Tutor;

/// <summary>Open answers: keeps the measures of each version (never the text) so the next version can be compared with it.</summary>
public sealed class WritingStore(EduVerseDb db, GamificationStore game, TimeProvider clock)
{
    /// <summary>Saves the measures of a version and returns the version before it (to show the improvement) and the reward.</summary>
    public async Task<(WritingSubmission? Before, Reward Reward)> SaveAsync(string userId, WritingTask task, WritingReview review, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(review);
        var before = await db.WritingSubmissions.AsNoTracking().Where(w => w.UserId == userId && w.TaskKey == task.Key).OrderByDescending(w => w.Id).FirstOrDefaultAsync(ct).ConfigureAwait(false);
        var since = clock.GetUtcNow().AddDays(-1);
        var rewarded = review.AllPassed && await db.WritingSubmissions.CountAsync(w => w.UserId == userId && w.At >= since && w.Passed == w.Checks, ct).ConfigureAwait(false) < Writing.RewardedPerDay;
        db.WritingSubmissions.Add(new WritingSubmission
        {
            UserId = userId, TaskKey = task.Key, Spoken = task.Spoken, Words = review.Stats.Words, Passed = review.Passed, Checks = review.Checks.Count,
            RubricTotal = review.RubricTotal, At = clock.GetUtcNow(),
        });
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        var reward = new Reward(0, [], []);
        if (rewarded)
        {
            await game.AwardAsync(userId, Writing.Xp, "writing", false, ct).ConfigureAwait(false);
            reward = new Reward(Writing.Xp, [], []).Plus(await game.CheckAsync(userId, ct).ConfigureAwait(false));
        }

        return (before, reward);
    }

    /// <summary>The best version of each task of the student (most criteria met).</summary>
    public async Task<IReadOnlyDictionary<string, WritingSubmission>> BestAsync(string userId, CancellationToken ct = default) =>
        (await db.WritingSubmissions.AsNoTracking().Where(w => w.UserId == userId).ToListAsync(ct).ConfigureAwait(false))
        .GroupBy(w => w.TaskKey).ToDictionary(g => g.Key, g => g.OrderByDescending(w => w.Passed).ThenByDescending(w => w.RubricTotal ?? -1).First());
}
