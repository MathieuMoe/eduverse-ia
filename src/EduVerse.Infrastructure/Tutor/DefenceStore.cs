using EduVerse.Core.Catalog;
using EduVerse.Core.Common;
using EduVerse.Core.Tutor;
using EduVerse.Infrastructure.Data;
using EduVerse.Infrastructure.Gamification;
using EduVerse.Infrastructure.Quizzes;
using Microsoft.EntityFrameworkCore;

namespace EduVerse.Infrastructure.Tutor;

/// <summary>A question of the defence as the student sees it (the correct option stays on the server).</summary>
public sealed record DefenceQuestion(int Id, string Topic, Area Area, Level Level, string Prompt, IReadOnlyList<string> Options, string? Visual, string? Code, int Done, int Total);

public sealed record DefenceOutcome(bool Transfer, bool Reasoned, bool Verified, string Answer, string Explanation, Reward Reward);

/// <summary>Understanding checks after a test: the same topics asked again with other numbers, plus an explanation.</summary>
public sealed class DefenceStore(EduVerseDb db, GamificationStore game, TimeProvider clock)
{
    private static readonly QuizMode[] Modes = [QuizMode.Practice, QuizMode.Review, QuizMode.Focus, QuizMode.Adventure];

    /// <summary>Creates (or finds) the defences of a finished test: up to <see cref="Defence.Max"/> topics answered right, one question each.</summary>
    public async Task<IReadOnlyList<DefenceResult>> StartAsync(string userId, string attemptId, TeachLanguage language, CancellationToken ct = default)
    {
        var attempt = await db.QuizAttempts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == attemptId && a.UserId == userId && a.FinishedAt != null, ct).ConfigureAwait(false);
        if (attempt is null || !Modes.Contains(attempt.Mode))
        {
            return [];
        }

        var existing = await db.DefenceResults.Where(d => d.UserId == userId && d.AttemptId == attemptId).OrderBy(d => d.Id).ToListAsync(ct).ConfigureAwait(false);
        if (existing.Count > 0)
        {
            return existing;
        }

        // Every topic answered right is a candidate (not only the first few): a topic the bank can't ask again at that
        // difficulty (e.g. a word problem about the student's interests) is tried at the other difficulties, then skipped
        var right = (await db.QuizAnswers.AsNoTracking().Where(a => a.AttemptId == attemptId && a.UserId == userId && a.Correct).OrderBy(a => a.Number).ToListAsync(ct).ConfigureAwait(false))
            .GroupBy(a => a.Topic).Select(g => g.OrderByDescending(a => a.Difficulty).First()).OrderByDescending(a => a.Hints > 0 || a.Confidence <= 1).ThenBy(a => a.Number)
            .ToList();
        var created = new List<DefenceResult>();
        foreach (var answer in right)
        {
            if (created.Count >= Defence.Max)
            {
                break;
            }

            foreach (var difficulty in new[] { answer.Difficulty, 1, 2, 3 }.Distinct())
            {
                var seed = Random.Shared.Next(1, int.MaxValue);
                var q = QuestionBanks.ForTopic(answer.Area, answer.Level, answer.Topic, difficulty, seed, language);
                for (var retry = 1; retry < 12 && (q.Prompt == answer.Prompt || q.Topic != answer.Topic); retry++)
                {
                    seed = Random.Shared.Next(1, int.MaxValue);
                    q = QuestionBanks.ForTopic(answer.Area, answer.Level, answer.Topic, difficulty, seed, language);
                }

                if (q.Topic != answer.Topic)
                {
                    continue;
                }

                created.Add(new DefenceResult
                {
                    UserId = userId, AttemptId = attemptId, Area = answer.Area, Level = answer.Level, Topic = answer.Topic, Difficulty = difficulty,
                    Seed = seed, CorrectIndex = q.CorrectIndex, Language = language, At = clock.GetUtcNow(),
                });
                break;
            }
        }

        db.DefenceResults.AddRange(created);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return created;
    }

    private static QuizQuestion Rebuild(DefenceResult d) => QuestionBanks.ForTopic(d.Area, d.Level, d.Topic, d.Difficulty, d.Seed, d.Language);

    /// <summary>The next question still pending of a test, or null when every one is done.</summary>
    public async Task<DefenceQuestion?> NextAsync(string userId, string attemptId, CancellationToken ct = default)
    {
        var all = await db.DefenceResults.AsNoTracking().Where(d => d.UserId == userId && d.AttemptId == attemptId).OrderBy(d => d.Id).ToListAsync(ct).ConfigureAwait(false);
        var next = all.FirstOrDefault(d => d.State == DefenceState.Pending);
        if (next is null)
        {
            return null;
        }

        var q = Rebuild(next);
        return new DefenceQuestion(next.Id, next.Topic, next.Area, next.Level, q.Prompt, q.Options, q.Visual, q.Code, all.Count(d => d.State == DefenceState.Done), all.Count);
    }

    public async Task<IReadOnlyList<DefenceResult>> ForAttemptAsync(string userId, string attemptId, CancellationToken ct = default) =>
        await db.DefenceResults.AsNoTracking().Where(d => d.UserId == userId && d.AttemptId == attemptId).OrderBy(d => d.Id).ToListAsync(ct).ConfigureAwait(false);

    /// <summary>The pending defence and its question (to judge the explanation), or null when it does not exist or is done.</summary>
    public async Task<(DefenceResult Defence, QuizQuestion Question)?> PendingAsync(string userId, int id, CancellationToken ct = default)
    {
        var d = await db.DefenceResults.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId && x.State == DefenceState.Pending, ct).ConfigureAwait(false);
        return d is null ? null : (d, Rebuild(d));
    }

    /// <summary>Saves the result: the answer is checked here (the server knows the right option); whether the explanation shows understanding comes from the caller.</summary>
    public async Task<DefenceOutcome?> CompleteAsync(string userId, int id, int choice, bool reasoned, CancellationToken ct = default)
    {
        var d = await db.DefenceResults.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId && x.State == DefenceState.Pending, ct).ConfigureAwait(false);
        if (d is null)
        {
            return null;
        }

        var q = Rebuild(d);
        (d.State, d.Transfer, d.Reasoned, d.At) = (DefenceState.Done, choice == d.CorrectIndex, reasoned, clock.GetUtcNow());
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        var reward = new Reward(0, [], []);
        if (d.Verified)
        {
            var since = clock.GetUtcNow().AddDays(-1);
            if (await db.DefenceResults.CountAsync(x => x.UserId == userId && x.State == DefenceState.Done && x.Transfer && x.Reasoned && x.At >= since, ct).ConfigureAwait(false) <= Defence.RewardedPerDay)
            {
                await game.AwardAsync(userId, Defence.Xp, "defence", false, ct).ConfigureAwait(false);
                reward = new Reward(Defence.Xp, [], []);
            }
        }

        return new DefenceOutcome(d.Transfer, d.Reasoned, d.Verified, q.Answer, q.Explanation, reward.Plus(await game.CheckAsync(userId, ct).ConfigureAwait(false)));
    }

    /// <summary>For the teacher: how many topics each student has verified out of how many they were asked.</summary>
    public async Task<IReadOnlyDictionary<string, (int Verified, int Total)>> SummaryAsync(IReadOnlyCollection<string> userIds, CancellationToken ct = default)
    {
        var rows = await db.DefenceResults.AsNoTracking().Where(d => userIds.Contains(d.UserId) && d.State == DefenceState.Done)
            .Select(d => new { d.UserId, d.Transfer, d.Reasoned }).ToListAsync(ct).ConfigureAwait(false);
        return rows.GroupBy(r => r.UserId).ToDictionary(g => g.Key, g => (g.Count(r => r.Transfer && r.Reasoned), g.Count()));
    }
}
