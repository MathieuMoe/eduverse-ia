using EduVerse.Core.Gamification;
using EduVerse.Core.Learning;
using EduVerse.Infrastructure.Engines;
using EduVerse.Infrastructure.Gamification;
using Microsoft.Extensions.Localization;

namespace EduVerse.Web;

public sealed record CountryCheckRequest(IReadOnlyList<int>? Answers);

public sealed record CountryCreateRequest(string? Name);

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
