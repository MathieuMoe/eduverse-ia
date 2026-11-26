using EduVerse.Core.Gamification;
using EduVerse.Infrastructure.Engines;
using EduVerse.Infrastructure.Gamification;
using Microsoft.Extensions.Localization;

namespace EduVerse.Web;

public sealed record DailyGoalRequest(int Goal, int HappyHour);

public sealed record SoundsRequest(bool On);

/// <summary>
/// Daily goal, happy hour, sounds and the end-of-lesson screens. Every number comes from the server (XP log and
/// attempt); the browser only animates it.
/// </summary>
public static class GameEndpoints
{
    public static IEndpointRouteBuilder MapGame(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api/game").RequireAuthorization(CurrentUser.StudentPolicy).RequireRateLimiting("quiz");

        api.MapGet("/goal", async (HttpContext context, GamificationStore game) => Results.Ok(GoalPayload(await game.GoalAsync(context.User.Id(), context.RequestAborted))));

        api.MapPost("/goal", async (DailyGoalRequest? request, HttpContext context, GamificationStore game) =>
        {
            if (request is null)
            {
                return Results.BadRequest();
            }

            await game.SaveGoalAsync(context.User.Id(), request.Goal, request.HappyHour, context.RequestAborted);
            return Results.Ok(GoalPayload(await game.GoalAsync(context.User.Id(), context.RequestAborted)));
        });

        api.MapPost("/sounds", async (SoundsRequest? request, HttpContext context, GamificationStore game) =>
        {
            if (request is null)
            {
                return Results.BadRequest();
            }

            await game.SaveSoundsAsync(context.User.Id(), request.On, context.RequestAborted);
            return Results.Ok(new { sounds = request.On });
        });

        api.MapGet("/lesson-end/{attemptId}", async (string attemptId, HttpContext context, GamificationStore game, LeagueStore leagues, IStringLocalizer<SharedResource> l) =>
        {
            var userId = context.User.Id();
            var end = await game.LessonEndAsync(userId, attemptId, context.RequestAborted);
            if (end is null)
            {
                return Results.NotFound();
            }

            var summary = await game.SummaryAsync(userId, context.RequestAborted);
            var league = await leagues.ViewAsync(userId, context.RequestAborted);
            var culture = System.Globalization.CultureInfo.CurrentCulture;
            return Results.Ok(new
            {
                xp = end.Xp,
                correct = end.Correct,
                length = end.Length,
                accuracy = end.Accuracy,
                seconds = end.Seconds,
                streak = end.Streak,
                streakExtended = end.StreakExtended,
                goal = end.Goal,
                todayXp = end.TodayXp,
                goalPercent = end.GoalPercent,
                goalReached = end.GoalReached,
                goalJustReached = end.GoalJustReached,
                happyHour = end.HappyHour,
                gems = end.Gems,
                milestone = end.Milestone,
                milestoneGems = end.Milestone ? Streaks.MilestoneGems(end.Streak) : 0,
                gemBalance = summary.Gems,
                league = league?.Me is { } me ? new
                {
                    emoji = Core.Gamification.Leagues.Emoji(league.Division),
                    name = l[$"Division_{league.Division}"].Value,
                    position = me.Position,
                    size = league.Rows.Count,
                    zone = me.Zone.ToString(),
                } : null,
                quests = summary.Quests.Select(q => new
                {
                    text = string.Format(culture, l[$"QuestText_{q.Quest.Kind}"].Value, q.Quest.Target),
                    progress = q.Progress,
                    target = q.Quest.Target,
                    done = q.Done,
                }),
            });
        });

        // Phase 5: the Tutor reacts to the lesson just finished. Code picks the mood; the AI (or a line written in
        // advance) only finds the words, from the numbers of the lesson — nothing the student typed is sent.
        // One reaction per finished lesson: bounded by the lessons themselves, so the larger "quiz" budget is enough
        api.MapPost("/mascot/{attemptId}", async (string attemptId, HttpContext context, GamificationStore game, AiEngines engines, ILogger<SoundsRequest> logger) =>
        {
            var userId = context.User.Id();
            var end = await game.LessonEndAsync(userId, attemptId, context.RequestAborted);
            if (end is null)
            {
                return Results.NotFound();
            }

            var profile = await game.ProfileAsync(userId, context.RequestAborted);
            var language = Localization.LanguageForCurrentCulture();
            var mood = Mascot.MoodOf(end);
            return await AiRunner.GuardAsync(async () =>
            {
                var run = await AiRunner.RunAsync(context, engines, logger,
                    async a => Mascot.Parse(await a.GenerateAsync(Mascot.Build(mood, end, profile.Level, language), context.RequestAborted)),
                    () => Task.FromResult(Mascot.Offline(mood, language, end.Xp + end.Correct)));
                return Results.Ok(new { mood = mood.ToString(), emoji = Mascot.Emoji(mood), line = run.Result, engine = run.Engine });
            });
        });

        // "The story of your mistakes": the week's most-missed topics (picked in code) told as a short adventure
        var ai = app.MapGroup("/api/game").RequireAuthorization(CurrentUser.StudentPolicy).RequireRateLimiting("ai");
        ai.MapPost("/mistake-story", async (HttpContext context, GamificationStore game, AiEngines engines, ILogger<SoundsRequest> logger, IStringLocalizer<SharedResource> l) =>
        {
            var userId = context.User.Id();
            var mistakes = await game.MistakesOfWeekAsync(userId, context.RequestAborted);
            if (mistakes.Count == 0)
            {
                return Results.Json(new { code = "no_mistakes" }, statusCode: StatusCodes.Status409Conflict);
            }

            var profile = await game.ProfileAsync(userId, context.RequestAborted);
            var language = Localization.LanguageForCurrentCulture();
            var items = mistakes.Select(m => (QuizEndpoints.TopicName(m.Topic, l), m.Prompt, m.Answer)).ToList();
            return await AiRunner.GuardAsync(async () =>
            {
                var run = await AiRunner.RunAsync(context, engines, logger,
                    async a => MistakeStory.Parse(await a.GenerateAsync(MistakeStory.Build(items, profile.Level, language), context.RequestAborted)),
                    () => Task.FromResult(MistakeStory.Offline(items, language)));
                var first = mistakes[0];
                return Results.Ok(new
                {
                    title = run.Result.Title,
                    story = run.Result.Story,
                    topics = items.Select(i => i.Item1),
                    practice = $"/Student/Quiz?area={first.Area}&level={first.Level}&mode=Focus&topic={Uri.EscapeDataString(first.Topic)}",
                    engine = run.Engine,
                    fallback = run.Fallback,
                });
            });
        });

        return app;
    }

    private static object GoalPayload(DailyGoalView g) => new
    {
        goal = g.Goal,
        todayXp = g.TodayXp,
        percent = g.Percent,
        reached = g.Reached,
        happyHour = g.HappyHour,
        happyHourActive = g.HappyHourActive,
        happyHourMinutesLeft = g.HappyHourMinutesLeft,
        sounds = g.Sounds,
    };
}
