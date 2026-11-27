using EduVerse.Core.Gamification;
using EduVerse.Core.Learning;
using EduVerse.Infrastructure.Engines;
using EduVerse.Infrastructure.Gamification;
using Microsoft.Extensions.Localization;

namespace EduVerse.Web;

public sealed record CountryCheckRequest(IReadOnlyList<int>? Answers);

public sealed record CountryCreateRequest(string? Name);

public sealed record CountryOrderRequest(IReadOnlyList<string>? Order);

public sealed record CountryAskRequest(string? Question);

/// <summary>
/// Trips through the history of a country. The checks of the eras are written and marked by the code; the AI only
/// writes the timeline of a country that is not in the catalog (offline that needs a real engine).
/// </summary>
public static class CountryEndpoints
{
    public static IEndpointRouteBuilder MapCountries(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api/countries").RequireAuthorization(CurrentUser.StudentPolicy);

        api.MapPost("/{key}/eras/{era:int}/check", async (string key, int era, HttpContext context, CountryStore store) =>
        {
            var check = await store.StartCheckAsync(context.User.Id(), key, era, Localization.LanguageForCurrentCulture(), context.RequestAborted);
            return check is null
                ? Results.Json(new { code = "era_locked" }, statusCode: StatusCodes.Status409Conflict)
                : Results.Ok(new { id = check.Id, questions = check.Questions.Select(q => new { prompt = q.Prompt, options = q.Options }) });
        })
        .RequireRateLimiting("quiz");

        api.MapPost("/checks/{id}", async (string id, CountryCheckRequest? request, HttpContext context, CountryStore store, IStringLocalizer<SharedResource> l) =>
        {
            if (request?.Answers is not { Count: Countries.CheckLength } answers)
            {
                return Results.BadRequest();
            }

            var result = await store.AnswerCheckAsync(context.User.Id(), id, answers, context.RequestAborted);
            return result is null
                ? Results.NotFound()
                : Results.Ok(new
                {
                    score = result.Score,
                    passed = result.Passed,
                    stamp = result.Stamp,
                    results = result.Questions.Select((q, i) => new { right = i < answers.Count && answers[i] == q.Correct, correct = q.Correct, explanation = q.Explanation }),
                    reward = QuizEndpoints.RewardView(result.Reward, l),
                });
        })
        .RequireRateLimiting("quiz");

        // The time guide: the AI answers a question about an open era from its facts; offline, the facts answer
        api.MapPost("/{key}/eras/{era:int}/ask", async (string key, int era, CountryAskRequest? request, HttpContext context, CountryStore store, AiEngines engines, ILogger<CountryAskRequest> logger) =>
        {
            var question = request?.Question?.Trim() ?? "";
            if (question.Length is 0 or > TimeGuide.MaxQuestion)
            {
                return Results.BadRequest();
            }

            var userId = context.User.Id();
            var country = await store.FindAsync(userId, key, context.RequestAborted);
            if (country is null || era < 0 || era >= country.Eras.Count || (await store.ViewAsync(userId, country, context.RequestAborted)).Eras[era] == EraState.Locked)
            {
                return Results.Json(new { code = "era_locked" }, statusCode: StatusCodes.Status409Conflict);
            }

            var language = Localization.LanguageForCurrentCulture();
            var stop = country.Eras[era];
            return await AiRunner.GuardAsync(async () =>
            {
                var run = await AiRunner.RunAsync(context, engines, logger,
                    async ai => TimeGuide.Parse(await ai.GenerateAsync(TimeGuide.Build(country, stop, question, language), context.RequestAborted)),
                    () => Task.FromResult(TimeGuide.Offline(stop, question, language)));
                return Results.Ok(new { answer = run.Result, engine = run.Engine, fallback = run.Fallback });
            });
        })
        .RequireRateLimiting("tutor");

        api.MapPost("/{key}/order", async (string key, CountryOrderRequest? request, HttpContext context, CountryStore store) =>
        {
            if (request?.Order is not { Count: > 0 and <= CountryHistoryPrompt.MaxEras } order)
            {
                return Results.BadRequest();
            }

            var result = await store.OrderAsync(context.User.Id(), key, order, context.RequestAborted);
            return result is null
                ? Results.Json(new { code = "order_locked" }, statusCode: StatusCodes.Status409Conflict)
                : Results.Ok(new { right = result.Right, total = result.Total, won = result.Won, gems = result.Gems });
        })
        .RequireRateLimiting("quiz");

        api.MapPost("/", async (CountryCreateRequest? request, HttpContext context, CountryStore store, AiEngines engines, ILogger<CountryCreateRequest> logger) =>
        {
            var name = request?.Name?.Trim() ?? "";
            if (name.Length is < 2 or > CountryHistoryPrompt.MaxName)
            {
                return Results.BadRequest();
            }

            var language = Localization.LanguageForCurrentCulture();
            return await AiRunner.GuardAsync(async () =>
            {
                var run = await AiRunner.RunAsync<CountryHistory>(context, engines, logger,
                    async ai => CountryHistoryPrompt.Parse(await ai.GenerateAsync(CountryHistoryPrompt.Build(name, language), context.RequestAborted), ""),
                    () => throw new NeedsAiException());
                var key = await store.SaveCustomAsync(context.User.Id(), k => run.Result with { Key = k }, context.RequestAborted);
                return Results.Ok(new { key, engine = run.Engine });
            });
        })
        .RequireRateLimiting("ai");

        return app;
    }
}
