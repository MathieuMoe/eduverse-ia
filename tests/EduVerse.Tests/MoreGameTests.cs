using EduVerse.Core.Catalog;
using EduVerse.Core.Common;
using EduVerse.Core.Curriculum;
using EduVerse.Core.Gamification;
using EduVerse.Infrastructure.Data;
using EduVerse.Infrastructure.Gamification;
using EduVerse.Infrastructure.Marketplace;
using EduVerse.Infrastructure.Quizzes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EduVerse.Tests;

public class StreakReminderRulesTests
{
    [Fact]
    public void A_streak_is_at_risk_only_in_the_evening_and_before_practising()
    {
        Assert.True(Streaks.AtRisk(5, false, new TimeOnly(18, 0)));
        Assert.False(Streaks.AtRisk(5, false, new TimeOnly(17, 59)));
        Assert.False(Streaks.AtRisk(5, true, new TimeOnly(21, 0)));
        Assert.False(Streaks.AtRisk(0, false, new TimeOnly(21, 0))); // no streak, nothing to lose
        Assert.Equal(6, Streaks.HoursLeft(new TimeOnly(18, 30)));
    }

    [Fact]
    public void Milestones_pay_more_the_longer_the_streak()
    {
        Assert.True(Streaks.IsMilestone(7) && Streaks.IsMilestone(100));
        Assert.False(Streaks.IsMilestone(8));
        Assert.True(Streaks.MilestoneGems(7) < Streaks.MilestoneGems(30) && Streaks.MilestoneGems(30) < Streaks.MilestoneGems(100));
        Assert.True(new LessonEnd(10, 5, 5, 60, 7, true, 20, 10, false, false, false).Milestone);
        Assert.False(new LessonEnd(10, 5, 5, 60, 7, false, 20, 10, false, false, false).Milestone); // not reached by this lesson
    }

    [Fact]
    public void The_automatic_partner_is_the_closest_in_xp()
    {
        var picked = FriendQuests.Pick(new[] { ("Ana", 10), ("Bo", 95), ("Cy", 140) }, 100, c => c.Item2, c => c.Item1);
        Assert.Equal("Bo", picked.Item1);
        var monday = new DateOnly(2026, 11, 23);
        Assert.False(FriendQuests.AutoPairDue(monday, monday));
        Assert.True(FriendQuests.AutoPairDue(monday.AddDays(1), monday));
    }

    [Fact]
    public void Every_roleplay_goal_can_be_reached_by_a_model_answer()
    {
        var answers = new Dictionary<string, string>
        {
            ["airport"] = "Hello! Here is my passport and my ticket, one suitcase. Where is the gate? Thank you",
            ["cafe"] = "Hi! A sandwich and a juice, please. How much is it? Thanks",
            ["market"] = "30? Can I have a discount? Thanks",
            ["sciencefair"] = "It needs light, water and carbon dioxide, and it makes sugar and oxygen. Can I ask you something?",
            ["directions"] = "Excuse me, where is the museum? Straight on and left at the corner. Thank you!",
            ["doctor"] = "Good morning. I have a headache and a cough since 3 days. Thank you",
            ["rocket"] = "Gravity pulls, thrust pushes: action and reaction. a = 20000 / 2000 = 10 m/s². Are we ready?",
            ["rubberduck"] = "The loop uses range(10) so it starts at 0. Use range(1, 11) and run a test. Do you agree?",
        };
        Assert.Equal(Roleplays.All.Select(r => r.Key).Order(), answers.Keys.Order());
        foreach (var r in Roleplays.All)
        {
            var met = Roleplays.Progress(r, new HashSet<int>(), answers[r.Key]);
            Assert.True(Roleplays.Complete(r, met), $"{r.Key}: missing goals {string.Join(", ", Enumerable.Range(0, r.Goals.Count).Where(i => !met.Contains(i)))}");
        }
    }
}

public class MoreGameStoreTests(Database database) : IClassFixture<Database>
{
    private static GamificationStore At(IServiceProvider services, DateTimeOffset now) =>
        new(services.GetRequiredService<EduVerseDb>(), new FixedClock(now), services.GetRequiredService<IOptions<MarketplaceOptions>>());

    private static DateTimeOffset MadridToday(int hour)
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid");
        var local = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, zone).Date.AddHours(hour);
        return new DateTimeOffset(local, zone.GetUtcOffset(local));
    }

    [Fact]
    public async Task The_first_answer_of_the_seventh_day_pays_the_milestone_once_and_the_evening_reminder_follows_the_clock()
    {
        using var scope = database.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EduVerseDb>();
        var evening = At(scope.ServiceProvider, MadridToday(20));
        var morning = At(scope.ServiceProvider, MadridToday(9));
        var student = await QuizDriver.NewStudentAsync(db, TimeProvider.System);
        for (var d = 1; d <= 6; d++)
        {
            db.XpEvents.Add(new XpEvent { UserId = student, Amount = 10, Reason = "answer", Correct = true, At = MadridToday(12).AddDays(-d) });
        }

        await db.SaveChangesAsync();
        Assert.Equal(6, (await evening.StreakRiskAsync(student))!.Value.Streak);
        Assert.Null(await morning.StreakRiskAsync(student)); // too early to worry

        var first = await evening.AnswerAsync(student, true, 2, 1, "answer");
        Assert.Equal(7, first.Milestone);
        Assert.True(first.Gems >= Streaks.MilestoneGems(7));
        Assert.Null(await evening.StreakRiskAsync(student)); // practised today
        var second = await evening.AnswerAsync(student, true, 2, 1, "answer");
        Assert.Equal(0, second.Milestone);
        Assert.Single(await db.GemEvents.Where(g => g.UserId == student && g.Reason == "streak:7").ToListAsync());
    }

    [Fact]
    public async Task A_legendary_unit_needs_the_boss_beaten_costs_gems_and_has_one_heart()
    {
        using var scope = database.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EduVerseDb>();
        var quizzes = scope.ServiceProvider.GetRequiredService<QuizStore>();
        var paths = scope.ServiceProvider.GetRequiredService<PathStore>();
        var student = await QuizDriver.NewStudentAsync(db, TimeProvider.System);
        const Area area = Area.Geography;
        const Level level = Level.Primary3;
        var topic = AreaSubjects.Topics(area, level)[0];
        Assert.False(await paths.LegendOpenAsync(student, area, level, topic));

        for (var i = 0; i < LearningPath.LessonsPerUnit; i++)
        {
            var lesson = await quizzes.StartAsync(student, area, level, QuizMode.Focus, TeachLanguage.Spanish, topic: topic);
            await QuizDriver.AnswerAllRightAsync(quizzes, db, student, lesson.AttemptId);
        }

        Assert.NotNull(await paths.OpenChestAsync(student, area, level, topic));
        var story = DemoBosses.Write("Topic", student, TeachLanguage.Spanish);
        var boss = await quizzes.StartAsync(student, area, level, QuizMode.Adventure, TeachLanguage.Spanish, story: story, theme: Theme.Castle, topic: LearningPath.BossTopic(topic));
        await QuizDriver.AnswerAllRightAsync(quizzes, db, student, boss.AttemptId);
        Assert.True(await paths.LegendOpenAsync(student, area, level, topic));

        var legend = await quizzes.StartAsync(student, area, level, QuizMode.Adventure, TeachLanguage.Spanish, story: story, theme: Theme.Castle, topic: LearningPath.LegendTopic(topic));
        Assert.True(legend.Adventure!.Legendary && legend.Adventure.UnitBoss);
        Assert.Equal((LearningPath.LegendHearts, Adventure.BossHp + LearningPath.LegendExtraBlows), (legend.Adventure.Hearts, legend.Adventure.BossHp));
        Assert.Equal(BuyOutcome.Limit, (await quizzes.UsePotionAsync(student, legend.AttemptId)).Outcome); // no potions in a legendary
        var won = await QuizDriver.AnswerAllRightAsync(quizzes, db, student, legend.AttemptId);
        Assert.True(won.Summary!.Won);

        var path = await paths.PathAsync(student, area, level);
        Assert.Contains(topic, path.Legendary);
        Assert.False(await paths.LegendOpenAsync(student, area, level, topic)); // done once
        Assert.Contains("Legend", await db.BadgeAwards.Where(b => b.UserId == student).Select(b => b.Key).ToListAsync());
    }

    [Fact]
    public async Task From_tuesday_a_student_without_a_partner_is_paired_with_a_free_classmate()
    {
        using var scope = database.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EduVerseDb>();
        var services = scope.ServiceProvider;
        // A fixed week far from the demo data: Monday 2032-03-01 and Wednesday 2032-03-03
        GamificationStore Game(DateTimeOffset now) => At(services, now);
        FriendQuestStore Friends(GamificationStore g) => new(db, g, new LeagueStore(db, g));
        var monday = Friends(Game(new DateTimeOffset(2032, 3, 1, 12, 0, 0, TimeSpan.Zero)));
        var wednesday = Friends(Game(new DateTimeOffset(2032, 3, 3, 12, 0, 0, TimeSpan.Zero)));
        var alex = await database.UserIdAsync("alex@eduverse.demo");

        Assert.Null(await monday.AutoPairAsync(alex)); // Monday stays free to choose
        var quest = await wednesday.AutoPairAsync(alex);
        Assert.NotNull(quest);
        Assert.NotEqual(await database.UserIdAsync("mia@eduverse.demo"), quest.Friend.UserId);
        Assert.Equal(quest.Id, (await wednesday.AutoPairAsync(alex))!.Id); // already paired: the same quest
    }
}
