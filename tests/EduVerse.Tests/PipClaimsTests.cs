using EduVerse.Core.Catalog;
using EduVerse.Core.Common;
using EduVerse.Core.Tutor;
using EduVerse.Infrastructure.Data;
using EduVerse.Infrastructure.Gamification;
using EduVerse.Infrastructure.Tutor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EduVerse.Tests;

public class PipClaimsRuleTests
{
    [Fact]
    public void Every_claim_has_a_unique_id_and_text_in_all_four_languages()
    {
        var bank = PipClaims.All();
        Assert.NotEmpty(bank);
        Assert.Equal(bank.Count, bank.Select(c => c.Id).Distinct().Count());
        foreach (var claim in bank)
        {
            foreach (var language in Enum.GetValues<TeachLanguage>())
            {
                Assert.False(string.IsNullOrWhiteSpace(claim.Statement.In(language)));
                Assert.False(string.IsNullOrWhiteSpace(claim.Explanation.In(language)));
            }
        }
    }

    [Fact]
    public void The_bank_mixes_true_and_false_claims()
    {
        var bank = PipClaims.All();
        Assert.Contains(bank, c => c.IsTrue);
        Assert.Contains(bank, c => !c.IsTrue);
    }

    [Fact]
    public void Find_returns_the_claim_by_id_or_null_when_unknown()
    {
        var first = PipClaims.All()[0];
        Assert.Equal(first, PipClaims.Find(first.Id));
        Assert.Null(PipClaims.Find("not-a-real-id"));
    }

    [Fact]
    public void The_same_seed_gives_the_same_round()
    {
        var one = PipClaims.Round(7, 5);
        var two = PipClaims.Round(7, 5);
        Assert.Equal(one.Select(c => c.Id), two.Select(c => c.Id));
    }

    [Fact]
    public void A_round_has_no_duplicate_claims_and_respects_the_requested_size()
    {
        for (var seed = 1; seed <= 50; seed++)
        {
            var round = PipClaims.Round(seed, 5);
            Assert.Equal(5, round.Count);
            Assert.Equal(round.Count, round.Select(c => c.Id).Distinct().Count());
        }
    }

    [Fact]
    public void Filtering_by_area_stays_within_that_area_when_enough_claims_exist()
    {
        var round = PipClaims.Round(3, 2, Area.Mathematics);
        Assert.All(round, c => Assert.Equal(Area.Mathematics, c.Area));
    }

    [Fact]
    public void Filtering_by_an_area_too_small_falls_back_to_the_whole_bank()
    {
        // No single area has anywhere near 50 claims, so asking for 50 must fall back to the full bank.
        var round = PipClaims.Round(1, 50, Area.Mathematics);
        Assert.Equal(PipClaims.All().Count, round.Count);
    }
}

public class PipClaimsStoreTests(Database database) : IClassFixture<Database>
{
    [Fact]
    public async Task Round_returns_localized_statements_without_revealing_the_answer()
    {
        using var scope = database.Services.CreateScope();
        var round = PipClaimsStore.Round(TeachLanguage.Spanish);
        Assert.Equal(PipClaimsStore.RoundSize, round.Count);
        Assert.All(round, c => Assert.False(string.IsNullOrWhiteSpace(c.Statement)));
    }

    [Fact]
    public async Task Answering_correctly_rewards_xp_and_reports_the_explanation()
    {
        using var scope = database.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EduVerseDb>();
        var store = scope.ServiceProvider.GetRequiredService<PipClaimsStore>();
        var userId = await database.UserIdAsync("sara@eduverse.demo");
        db.XpEvents.RemoveRange(db.XpEvents.Where(x => x.UserId == userId && x.Reason == PipClaimsStore.RewardReason));
        await db.SaveChangesAsync();

        var claim = PipClaims.Find("density-2")!; // a true claim
        var outcome = await store.AnswerAsync(userId, claim.Id, saidTrue: true, TeachLanguage.English);

        Assert.NotNull(outcome);
        Assert.True(outcome!.Correct);
        Assert.True(outcome.WasTrue);
        Assert.Equal(claim.Explanation.In(TeachLanguage.English), outcome.Explanation);
        Assert.True(outcome.Reward.Xp >= PipClaims.Xp); // at least the claim's own XP; quests/badges may add more
    }

    [Fact]
    public async Task Answering_incorrectly_gives_no_reward_but_still_explains()
    {
        using var scope = database.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<PipClaimsStore>();
        var userId = await database.UserIdAsync("leo@eduverse.demo");

        var claim = PipClaims.Find("prime-1")!; // a false claim
        var outcome = await store.AnswerAsync(userId, claim.Id, saidTrue: true, TeachLanguage.English);

        Assert.NotNull(outcome);
        Assert.False(outcome!.Correct);
        Assert.False(outcome.WasTrue);
        Assert.Equal(0, outcome.Reward.Xp);
    }

    [Fact]
    public async Task An_unknown_claim_id_returns_null()
    {
        using var scope = database.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<PipClaimsStore>();
        var userId = await database.UserIdAsync("nora@eduverse.demo");

        Assert.Null(await store.AnswerAsync(userId, "not-a-real-id", saidTrue: true, TeachLanguage.English));
    }

    [Fact]
    public async Task The_daily_reward_is_capped()
    {
        using var scope = database.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EduVerseDb>();
        var game = scope.ServiceProvider.GetRequiredService<GamificationStore>();
        var store = scope.ServiceProvider.GetRequiredService<PipClaimsStore>();
        var userId = await database.UserIdAsync("alex@eduverse.demo");
        db.XpEvents.RemoveRange(db.XpEvents.Where(x => x.UserId == userId && x.Reason == PipClaimsStore.RewardReason));
        await db.SaveChangesAsync();

        // Pre-fill today's cap by awarding through the same reason the store uses.
        for (var i = 0; i < PipClaims.RewardedPerDay; i++)
        {
            await game.AwardAsync(userId, PipClaims.Xp, PipClaimsStore.RewardReason, true);
        }

        var claim = PipClaims.All().First(c => c.IsTrue);
        var outcome = await store.AnswerAsync(userId, claim.Id, saidTrue: true, TeachLanguage.English);

        Assert.NotNull(outcome);
        Assert.True(outcome!.Correct);
        // Correct, but the daily cap is already reached: no new pipclaims XP event was recorded.
        var since = DateTimeOffset.UtcNow.AddDays(-1);
        var rewardedToday = await db.XpEvents.CountAsync(x => x.UserId == userId && x.Reason == PipClaimsStore.RewardReason && x.At >= since);
        Assert.Equal(PipClaims.RewardedPerDay, rewardedToday);
    }
}
