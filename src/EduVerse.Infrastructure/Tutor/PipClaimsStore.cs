using EduVerse.Core.Catalog;
using EduVerse.Core.Common;
using EduVerse.Core.Tutor;
using EduVerse.Infrastructure.Data;
using EduVerse.Infrastructure.Gamification;
using Microsoft.EntityFrameworkCore;

namespace EduVerse.Infrastructure.Tutor;

/// <summary>A claim as the student sees it before answering: the statement only, never whether it is true.</summary>
public sealed record ClaimView(string Id, string Statement);

public sealed record ClaimOutcome(bool Correct, bool WasTrue, string Explanation, Reward Reward);

/// <summary>
/// "Don't believe everything an AI tells you" (<see cref="PipClaims"/>): nothing about a round is stored — the claim
/// bank is fixed and public, so the server only needs the claim's id to check an answer and award XP, the same way
/// <see cref="QuizStore"/> rewards do, capped per day via the existing XP log (no new table needed).
/// </summary>
public sealed class PipClaimsStore(EduVerseDb db, GamificationStore game, TimeProvider clock)
{
    public const string RewardReason = "pipclaims";

    public const int RoundSize = 5;

    /// <summary>A fresh round of claims, nothing stored: the student answers them by id, which is enough to check and
    /// reward each one (see <see cref="AnswerAsync"/>).</summary>
    public static IReadOnlyList<ClaimView> Round(TeachLanguage language, Area? area = null) =>
        PipClaims.Round(Random.Shared.Next(), RoundSize, area).Select(c => new ClaimView(c.Id, c.Statement.In(language))).ToList();

    public async Task<ClaimOutcome?> AnswerAsync(string userId, string claimId, bool saidTrue, TeachLanguage language, CancellationToken ct = default)
    {
        var claim = PipClaims.Find(claimId);
        if (claim is null)
        {
            return null;
        }

        var correct = saidTrue == claim.IsTrue;
        var reward = new Reward(0, [], []);
        if (correct)
        {
            var since = clock.GetUtcNow().AddDays(-1);
            var today = await db.XpEvents.CountAsync(x => x.UserId == userId && x.Reason == RewardReason && x.At >= since, ct).ConfigureAwait(false);
            if (today < PipClaims.RewardedPerDay)
            {
                await game.AwardAsync(userId, PipClaims.Xp, RewardReason, true, ct).ConfigureAwait(false);
                reward = new Reward(PipClaims.Xp, [], []);
            }
        }

        return new ClaimOutcome(correct, claim.IsTrue, claim.Explanation.In(language), reward.Plus(await game.CheckAsync(userId, ct).ConfigureAwait(false)));
    }
}
