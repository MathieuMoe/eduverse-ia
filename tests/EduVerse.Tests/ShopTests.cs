using System.Net;
using System.Text.Json;
using EduVerse.Core.Catalog;
using EduVerse.Core.Common;
using EduVerse.Core.Gamification;
using EduVerse.Infrastructure.Data;
using EduVerse.Infrastructure.Gamification;
using EduVerse.Infrastructure.Quizzes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EduVerse.Tests;

public class ShopRulesTests
{
    private static readonly DateOnly Today = new(2026, 11, 26);

    [Fact]
    public void A_frozen_day_keeps_the_streak_without_adding_to_it()
    {
        var active = new[] { Today.AddDays(-3), Today.AddDays(-2), Today };
        Assert.Equal(1, Streaks.Current(active, Today)); // yesterday missed: the streak restarts
        Assert.Equal(3, Streaks.Current(active, Today, [Today.AddDays(-1)]));
        Assert.Equal(2, Streaks.Current(active[..2], Today, [Today.AddDays(-1)])); // nothing yet today: still alive
    }

    [Fact]
    public void Freezes_cover_every_missed_day_or_none()
    {
        var active = new[] { Today.AddDays(-4), Today.AddDays(-3) };
        Assert.Equal([Today.AddDays(-2), Today.AddDays(-1)], Streaks.DaysToFreeze(active, [], Today, 2));
        Assert.Empty(Streaks.DaysToFreeze(active, [], Today, 1)); // two days missed, one freeze: the streak is lost
        Assert.Empty(Streaks.DaysToFreeze([Today.AddDays(-1)], [], Today, 2)); // nothing missed
        Assert.Empty(Streaks.DaysToFreeze([], [], Today, 2)); // no streak to save
        Assert.Empty(Streaks.DaysToFreeze(active, [], Today, 0));
        Assert.Equal([Today.AddDays(-1)], Streaks.DaysToFreeze(active, [Today.AddDays(-2)], Today, 1)); // one already frozen
    }

    [Fact]
    public void Double_xp_lasts_fifteen_minutes_after_buying_it()
    {
        var bought = new DateTimeOffset(2026, 11, 26, 10, 0, 0, TimeSpan.Zero);
        Assert.True(Shop.DoubleXpActive(bought, bought.AddMinutes(14)));
        Assert.False(Shop.DoubleXpActive(bought, bought.AddMinutes(15)));
        Assert.False(Shop.DoubleXpActive(null, bought));
        Assert.Equal(5, Shop.DoubleXpMinutesLeft(bought, bought.AddMinutes(10)));
    }

    [Fact]
    public void Every_item_has_a_price_and_an_icon_and_frames_have_a_key()
    {
        Assert.All(Shop.Items, i => Assert.True(Shop.Price(i) > 0 && Shop.Icon(i).Length > 0));
        Assert.All(Shop.Items.Where(Shop.IsFrame), i => Assert.True(Shop.IsFrameKey(Shop.FrameKey(i)) && Shop.FrameKey(i).Length > 0));
        Assert.False(Shop.IsFrameKey("diamond"));
    }
}

public class ShopStoreTests(Database database) : IClassFixture<Database>
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    private static async Task GiveGemsAsync(EduVerseDb db, string userId, int amount)
    {
        db.GemEvents.Add(new GemEvent { UserId = userId, Amount = amount, Reason = "test", At = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Quests_and_the_daily_goal_pay_gems_once()
    {
        using var scope = database.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EduVerseDb>();
        var quizzes = scope.ServiceProvider.GetRequiredService<QuizStore>();
        var game = scope.ServiceProvider.GetRequiredService<GamificationStore>();
        var student = await QuizDriver.NewStudentAsync(db, TimeProvider.System);
        await game.SaveGoalAsync(student, 10, HappyHour.Off);

        var q = await quizzes.StartAsync(student, Area.Geography, Level.Primary3, QuizMode.Practice, TeachLanguage.Spanish);
        await QuizDriver.AnswerAllRightAsync(quizzes, db, student, q.AttemptId);

        var reasons = await db.GemEvents.Where(g => g.UserId == student).Select(g => g.Reason).ToListAsync();
        var claimed = await db.QuestClaims.Where(c => c.UserId == student).Select(c => c.Key).ToListAsync();
        Assert.All(claimed, k => Assert.Contains("quest:" + k, reasons)); // every finished quest paid its gems
        Assert.Single(reasons, r => r.StartsWith("goal:", StringComparison.Ordinal));
        var badges = await db.BadgeAwards.CountAsync(b => b.UserId == student);
        Assert.Equal(badges, reasons.Count(r => r.StartsWith("badge:", StringComparison.Ordinal))); // every badge paid its gems
        var expected = Gems.DailyGoal + Gems.Quest * claimed.Count + Gems.Badge * badges;
        Assert.Equal(expected, await game.GemsAsync(student));
        Assert.Equal(expected, (await game.LessonEndAsync(student, q.AttemptId))!.Gems);

        await game.CheckAsync(student); // checking again pays nothing twice
        Assert.Single(await db.GemEvents.Where(g => g.UserId == student && g.Reason.StartsWith("goal:")).ToListAsync());
    }

    [Fact]
    public async Task The_shop_checks_the_balance_the_limits_and_what_is_owned()
    {
        using var scope = database.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EduVerseDb>();
        var game = scope.ServiceProvider.GetRequiredService<GamificationStore>();
        var student = await QuizDriver.NewStudentAsync(db, TimeProvider.System);

        Assert.Equal(BuyOutcome.NotEnoughGems, await game.BuyAsync(student, ShopItem.StreakFreeze));
        await GiveGemsAsync(db, student, 1000);
        Assert.Equal(BuyOutcome.Bought, await game.BuyAsync(student, ShopItem.StreakFreeze));
        Assert.Equal(BuyOutcome.Bought, await game.BuyAsync(student, ShopItem.StreakFreeze));
        Assert.Equal(BuyOutcome.Limit, await game.BuyAsync(student, ShopItem.StreakFreeze));
        Assert.Equal(BuyOutcome.NotNow, await game.BuyAsync(student, ShopItem.Potion)); // only during an adventure

        Assert.False(await game.EquipFrameAsync(student, "gold")); // not owned yet
        Assert.Equal(BuyOutcome.Bought, await game.BuyAsync(student, ShopItem.FrameGold));
        Assert.Equal(BuyOutcome.AlreadyOwned, await game.BuyAsync(student, ShopItem.FrameGold));
        var shop = await game.ShopAsync(student);
        Assert.Equal((2, "gold"), (shop.Freezes, shop.Frame)); // a new frame is worn at once
        Assert.Equal(1000 - 2 * Shop.Price(ShopItem.StreakFreeze) - Shop.Price(ShopItem.FrameGold), shop.Gems);
        Assert.True(await game.EquipFrameAsync(student, ""));
        Assert.False(await game.EquipFrameAsync(student, "diamond"));

        Assert.Equal(BuyOutcome.Bought, await game.BuyAsync(student, ShopItem.DoubleXp));
        Assert.True((await game.ShopAsync(student)).DoubleXpMinutesLeft > 0);
        var reward = await game.AnswerAsync(student, true, 2, 1, "answer");
        Assert.True(reward.Boosted);
        Assert.Equal(Xp.ForAnswer(true, 2, 1) * 2, await db.XpEvents.Where(x => x.UserId == student && x.Reason == "answer").SumAsync(x => x.Amount));
    }

    [Fact]
    public async Task A_freeze_saves_a_missed_day_but_not_two()
    {
        using var scope = database.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EduVerseDb>();
        var game = scope.ServiceProvider.GetRequiredService<GamificationStore>();
        var saved = await QuizDriver.NewStudentAsync(db, TimeProvider.System);
        var lost = await QuizDriver.NewStudentAsync(db, TimeProvider.System);
        var now = game.Now;
        foreach (var (user, daysAgo) in new[] { (saved, 3), (saved, 2), (lost, 4), (lost, 3) })
        {
            db.XpEvents.Add(new XpEvent { UserId = user, Amount = 10, Reason = "answer", Correct = true, At = now.AddDays(-daysAgo) });
        }

        await db.SaveChangesAsync();
        foreach (var user in new[] { saved, lost })
        {
            await GiveGemsAsync(db, user, Shop.Price(ShopItem.StreakFreeze));
            Assert.Equal(BuyOutcome.Bought, await game.BuyAsync(user, ShopItem.StreakFreeze));
        }

        Assert.Equal([game.Today.AddDays(-1)], await game.SettleStreakAsync(saved));
        Assert.Equal(2, await game.StreakAsync(saved));
        Assert.Equal(0, (await game.ShopAsync(saved)).Freezes); // used

        Assert.Empty(await game.SettleStreakAsync(lost)); // two missed days, one freeze
        Assert.Equal(0, await game.StreakAsync(lost));
        Assert.Equal(1, (await game.ShopAsync(lost)).Freezes); // kept for next time
    }

    [Fact]
    public async Task A_potion_gives_a_heart_back_during_an_adventure_only()
    {
        using var scope = database.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EduVerseDb>();
        var quizzes = scope.ServiceProvider.GetRequiredService<QuizStore>();
        var game = scope.ServiceProvider.GetRequiredService<GamificationStore>();
        var student = await QuizDriver.NewStudentAsync(db, TimeProvider.System);
        var story = DemoStories.Write(Theme.Space, Area.Geography, TeachLanguage.English);
        var q = await quizzes.StartAsync(student, Area.Geography, Level.Primary3, QuizMode.Adventure, TeachLanguage.English, story: story, theme: Theme.Space);

        Assert.Equal(BuyOutcome.Limit, (await quizzes.UsePotionAsync(student, q.AttemptId)).Outcome); // hearts are full
        var first = JsonSerializer.Deserialize<QuizQuestion>((await db.QuizAttempts.AsNoTracking().SingleAsync(a => a.Id == q.AttemptId)).CurrentJson, Web)!;
        var wrong = await quizzes.AnswerAsync(student, q.AttemptId, (first.CorrectIndex + 1) % 4, TeachLanguage.English);
        Assert.Equal(Adventure.Hearts - 1, wrong!.Next!.Adventure!.Hearts);

        Assert.Equal(BuyOutcome.NotEnoughGems, (await quizzes.UsePotionAsync(student, q.AttemptId)).Outcome);
        await GiveGemsAsync(db, student, Shop.Price(ShopItem.Potion));
        var (outcome, hearts) = await quizzes.UsePotionAsync(student, q.AttemptId);
        Assert.Equal((BuyOutcome.Bought, Adventure.Hearts), (outcome, hearts));
        Assert.Equal(0, await game.GemsAsync(student));
        Assert.Equal(BuyOutcome.NotNow, (await quizzes.UsePotionAsync("someone-else", q.AttemptId)).Outcome);

        var practice = await quizzes.StartAsync(student, Area.Geography, Level.Primary3, QuizMode.Practice, TeachLanguage.English);
        Assert.Equal(BuyOutcome.NotNow, (await quizzes.UsePotionAsync(student, practice.AttemptId)).Outcome);
    }
}

public class ShopWebTests(App app) : IClassFixture<App>
{
    [Fact]
    public async Task The_shop_page_lists_the_items_and_a_potion_outside_an_adventure_is_refused()
    {
        var client = await app.DemoAsync("Student");
        var page = await client.GetStringAsync("/Student/Shop");
        foreach (var item in Shop.Items)
        {
            Assert.Contains($"data-testid=\"shop-item-{item}\"", page, StringComparison.Ordinal);
        }

        var potion = await client.PostAsync("/api/quiz/not-an-attempt/potion", new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.Conflict, potion.StatusCode);
        Assert.Contains("data-testid=\"dashboard-gems\"", await client.GetStringAsync("/Student"), StringComparison.Ordinal);
    }
}
