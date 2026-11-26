using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EduVerse.Core.Catalog;
using EduVerse.Core.Common;
using EduVerse.Core.Gamification;
using EduVerse.Infrastructure.Data;
using EduVerse.Infrastructure.Gamification;
using EduVerse.Infrastructure.Marketplace;
using EduVerse.Infrastructure.Quizzes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EduVerse.Tests;

/// <summary>A clock frozen at a given instant (happy hour depends on the time of day).</summary>
internal sealed class FixedClock(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}

/// <summary>Plays a whole test answering every item right, drills included, through the real store.</summary>
internal static class QuizDriver
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    public static async Task<AnswerResult> AnswerAllRightAsync(QuizStore quizzes, EduVerseDb db, string userId, string attemptId, TeachLanguage language = TeachLanguage.Spanish)
    {
        AnswerResult? last = null;
        for (var i = 0; i < 40 && last?.Summary is null; i++)
        {
            var attempt = await db.QuizAttempts.AsNoTracking().SingleAsync(a => a.Id == attemptId);
            if (attempt.CurrentExerciseJson is { } exerciseJson)
            {
                var exercise = JsonSerializer.Deserialize<PracticeExercise>(exerciseJson, Web)!;
                last = await (exercise.Kind switch
                {
                    ExerciseKind.FillBlank => quizzes.AnswerFillBlankAsync(userId, attemptId, exercise.Answer!, 1, 0, language),
                    ExerciseKind.Reorder => quizzes.AnswerReorderAsync(userId, attemptId, exercise.CorrectOrder!, 1, 0, language),
                    ExerciseKind.Match => quizzes.AnswerMatchAsync(userId, attemptId, exercise.Pairs!.ToDictionary(p => p.Left, p => p.Right), 1, 0, language),
                    ExerciseKind.MultiSelect => quizzes.AnswerMultiSelectAsync(userId, attemptId, exercise.CorrectIndices!, 1, 0, language),
                    ExerciseKind.Listen => quizzes.AnswerListenAsync(userId, attemptId, exercise.CorrectIndex!.Value, 1, 0, language),
                    _ => quizzes.AnswerTrueFalseAsync(userId, attemptId, exercise.CorrectBool!.Value, language),
                });
            }
            else
            {
                var current = JsonSerializer.Deserialize<QuizQuestion>(attempt.CurrentJson, Web)!;
                last = await quizzes.AnswerAsync(userId, attemptId, current.CorrectIndex, language);
            }

            Assert.NotNull(last);
        }

        Assert.NotNull(last!.Summary);
        return last;
    }

    /// <summary>A brand-new student account (no XP, no streak), so "first lesson of the day" is deterministic.</summary>
    public static async Task<string> NewStudentAsync(EduVerseDb db, TimeProvider clock)
    {
        var id = Guid.NewGuid().ToString("N");
        db.Users.Add(new AppUser { Id = id, UserName = $"{id}@test.local", Email = $"{id}@test.local", DisplayName = "Test", CreatedAt = clock.GetUtcNow() });
        await db.SaveChangesAsync();
        return id;
    }
}

public class DailyGoalRulesTests
{
    [Theory]
    [InlineData(10, 10)]
    [InlineData(50, 50)]
    [InlineData(7, DailyGoals.Default)]
    [InlineData(-1, DailyGoals.Default)]
    public void A_goal_outside_the_options_falls_back_to_the_default(int asked, int kept) => Assert.Equal(kept, DailyGoals.Normalize(asked));

    [Fact]
    public void Goal_progress_is_capped_and_celebrated_only_once()
    {
        Assert.Equal((0, 50, 100), (DailyGoals.Percent(0, 20), DailyGoals.Percent(10, 20), DailyGoals.Percent(45, 20)));
        Assert.True(DailyGoals.JustReached(15, 22, 20));
        Assert.False(DailyGoals.JustReached(20, 35, 20)); // already reached before this lesson
        Assert.False(DailyGoals.JustReached(5, 15, 20));
    }

    [Fact]
    public void Happy_hour_lasts_half_an_hour_and_doubles_xp()
    {
        Assert.False(HappyHour.IsActive(HappyHour.Off, new TimeOnly(18, 10)));
        Assert.False(HappyHour.IsActive(18, new TimeOnly(17, 59)));
        Assert.True(HappyHour.IsActive(18, new TimeOnly(18, 0)));
        Assert.True(HappyHour.IsActive(18, new TimeOnly(18, 29, 59)));
        Assert.False(HappyHour.IsActive(18, new TimeOnly(18, 30)));
        Assert.Equal(20, HappyHour.MinutesLeft(18, new TimeOnly(18, 10)));
        Assert.Equal(0, HappyHour.MinutesLeft(18, new TimeOnly(19, 10)));
        Assert.Equal((24, 12), (HappyHour.Apply(12, true), HappyHour.Apply(12, false)));
        Assert.Equal(HappyHour.Off, HappyHour.Normalize(3)); // not an offered hour
        Assert.Equal(19, HappyHour.Normalize(19));
    }

    [Fact]
    public void Lesson_accuracy_is_rounded_and_safe_on_an_empty_test()
    {
        Assert.Equal(67, new LessonEnd(10, 2, 3, 60, 1, true, 20, 10, false, false, false).Accuracy);
        Assert.Equal(0, new LessonEnd(0, 0, 0, 0, 0, false, 20, 0, false, false, false).Accuracy);
    }
}

public class DailyGoalStoreTests(Database database) : IClassFixture<Database>
{
    /// <summary>A Madrid local time today, as a UTC instant (the store's zone is Europe/Madrid by default).</summary>
    private static DateTimeOffset MadridToday(int hour, int minute)
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid");
        var local = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, zone).Date.AddHours(hour).AddMinutes(minute);
        return new DateTimeOffset(local, zone.GetUtcOffset(local));
    }

    private static GamificationStore StoreAt(IServiceProvider services, DateTimeOffset now) =>
        new(services.GetRequiredService<EduVerseDb>(), new FixedClock(now), services.GetRequiredService<IOptions<MarketplaceOptions>>());

    [Fact]
    public async Task Saving_the_goal_normalizes_values_outside_the_options()
    {
        using var scope = database.Services.CreateScope();
        var game = scope.ServiceProvider.GetRequiredService<GamificationStore>();
        var nora = await database.UserIdAsync("nora@eduverse.demo");

        await game.SaveGoalAsync(nora, 30, 19);
        var saved = await game.GoalAsync(nora);
        Assert.Equal((30, 19), (saved.Goal, saved.HappyHour));

        await game.SaveGoalAsync(nora, 999, 4);
        var normalized = await game.GoalAsync(nora);
        Assert.Equal((DailyGoals.Default, HappyHour.Off), (normalized.Goal, normalized.HappyHour));
    }

    [Fact]
    public async Task Answers_give_double_xp_only_during_the_students_happy_hour()
    {
        using var scope = database.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EduVerseDb>();
        var during = StoreAt(scope.ServiceProvider, MadridToday(18, 10));
        var after = StoreAt(scope.ServiceProvider, MadridToday(18, 45));
        var student = await QuizDriver.NewStudentAsync(db, TimeProvider.System);
        await during.SaveGoalAsync(student, 20, 18);

        var boosted = await during.AnswerAsync(student, true, 2, 1, "answer");
        var normal = await after.AnswerAsync(student, true, 2, 1, "answer");

        Assert.True(boosted.Boosted);
        Assert.False(normal.Boosted);
        var xpEvents = await db.XpEvents.Where(x => x.UserId == student && x.Reason == "answer").OrderBy(x => x.Id).Select(x => x.Amount).ToListAsync();
        Assert.Equal([Xp.ForAnswer(true, 2, 1) * HappyHour.Multiplier, Xp.ForAnswer(true, 2, 1)], xpEvents);
        Assert.True((await during.GoalAsync(student)).HappyHourActive);
        Assert.False((await after.GoalAsync(student)).HappyHourActive);
    }

    [Fact]
    public async Task The_first_lesson_of_the_day_extends_the_streak_and_can_reach_the_goal()
    {
        using var scope = database.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EduVerseDb>();
        var quizzes = scope.ServiceProvider.GetRequiredService<QuizStore>();
        var game = scope.ServiceProvider.GetRequiredService<GamificationStore>();
        var student = await QuizDriver.NewStudentAsync(db, TimeProvider.System);
        await game.SaveGoalAsync(student, 10, HappyHour.Off);

        var q = await quizzes.StartAsync(student, Area.Geography, Level.Primary3, QuizMode.Practice, TeachLanguage.Spanish);
        Assert.Null(await game.LessonEndAsync(student, q.AttemptId)); // not finished yet
        await QuizDriver.AnswerAllRightAsync(quizzes, db, student, q.AttemptId);

        var end = await game.LessonEndAsync(student, q.AttemptId);
        Assert.NotNull(end);
        Assert.Equal((10, 10, 100), (end.Correct, end.Length, end.Accuracy));
        Assert.True(end.Xp > 0);
        Assert.Equal(end.Xp, end.TodayXp); // a new student: every XP of today comes from this lesson
        Assert.True(end.StreakExtended);
        Assert.Equal(1, end.Streak);
        Assert.True(end.GoalReached && end.GoalJustReached);
        Assert.Null(await game.LessonEndAsync("someone-else", q.AttemptId));

        // A second lesson the same day neither extends the streak nor reaches the goal again
        var second = await quizzes.StartAsync(student, Area.Geography, Level.Primary3, QuizMode.Practice, TeachLanguage.Spanish);
        await QuizDriver.AnswerAllRightAsync(quizzes, db, student, second.AttemptId);
        var again = await game.LessonEndAsync(student, second.AttemptId);
        Assert.False(again!.StreakExtended);
        Assert.False(again.GoalJustReached);
        Assert.True(again.GoalReached);
    }
}

public class DailyGoalWebTests(App app) : IClassFixture<App>
{
    [Fact]
    public async Task The_goal_api_saves_and_the_quiz_page_offers_the_sound_toggle()
    {
        var client = await app.DemoAsync("Student");
        var saved = await client.PostAsJsonAsync("/api/game/goal", new { goal = 50, happyHour = 20 });
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        var body = await saved.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal((50, 20), (body.GetProperty("goal").GetInt32(), body.GetProperty("happyHour").GetInt32()));

        var sounds = await client.PostAsJsonAsync("/api/game/sounds", new { on = false });
        Assert.Equal(HttpStatusCode.OK, sounds.StatusCode);
        var page = await client.GetStringAsync("/Student/Quiz?area=Geography&level=Primary3");
        Assert.Contains("data-testid=\"sound-toggle\"", page, StringComparison.Ordinal);
        Assert.Contains("data-sounds=\"0\"", page, StringComparison.Ordinal);

        var home = await client.GetStringAsync("/Student");
        Assert.Contains("data-testid=\"daily-goal\"", home, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/game/lesson-end/not-a-real-attempt")).StatusCode);
    }

    [Fact]
    public async Task The_goal_api_is_for_students_only()
    {
        var anonymous = app.Anonymous();
        var response = await anonymous.GetAsync("/api/game/goal");
        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
    }
}
