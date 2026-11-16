using EduVerse.Core.Abstractions;
using EduVerse.Core.Tutor;
using EduVerse.Infrastructure.Engines;
using EduVerse.Infrastructure.Gamification;
using EduVerse.Infrastructure.Tutor;
using Microsoft.Extensions.Localization;

namespace EduVerse.Web;

public sealed record DefenceAnswerRequest(int Choice, string? Why);

/// <summary>
/// "Defend your answer": the same topics of a finished test asked again with other numbers, and "why?". The server keeps the right option;
/// the explanation is judged by code (offline) or by the AI, and never stored.
/// </summary>
public static class DefenceEndpoints
{
    public static IEndpointRouteBuilder MapDefence(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api/defence").RequireAuthorization(CurrentUser.StudentPolicy).RequireRateLimiting("tutor");

        api.MapPost("/{attemptId}/start", async (string attemptId, HttpContext context, DefenceStore store, IStringLocalizer<SharedResource> l) =>
        {
            var created = await store.StartAsync(context.User.Id(), attemptId, Localization.LanguageForCurrentCulture(), context.RequestAborted);
            return created.Count == 0 ? Results.Json(new { code = "no_defence" }, statusCode: StatusCodes.Status400BadRequest) : Results.Ok(await Next(context, store, attemptId, l));
        });

        api.MapGet("/{attemptId}/next", async (string attemptId, HttpContext context, DefenceStore store, IStringLocalizer<SharedResource> l) => Results.Ok(await Next(context, store, attemptId, l)));

        api.MapPost("/{id:int}/answer", async (int id, DefenceAnswerRequest? request, HttpContext context, DefenceStore store, GamificationStore game, AiEngines engines, ILogger<DefenceAnswerRequest> logger, IStringLocalizer<SharedResource> l) =>
        {
            var why = request?.Why?.Trim() ?? "";
            if (request is null || why.Length < 3 || request.Choice is < 0 or >= QuizQuestionChoices)
            {
                return Results.Json(new { code = "defence_why" }, statusCode: StatusCodes.Status400BadRequest);
            }

            var pending = await store.PendingAsync(context.User.Id(), id, context.RequestAborted);
            if (pending is not { } found)
            {
                return Results.Json(new { code = "no_defence" }, statusCode: StatusCodes.Status400BadRequest);
            }

            var language = Localization.LanguageForCurrentCulture();
            var level = (await game.ProfileAsync(context.User.Id(), context.RequestAborted)).Level;
            return await AiRunner.GuardAsync(async () =>
            {
                var run = await AiRunner.RunAsync<(bool Reasoned, string? Feedback)>(context, engines, logger,
                    async ai =>
                    {
                        var (understood, feedback) = DefencePrompt.Parse(await ai.GenerateAsync(DefencePrompt.Build(found.Question, QuizEndpoints.TopicName(found.Defence.Topic, l), why, level, language), context.RequestAborted));
                        return (understood, feedback);
                    },
                    () => Task.FromResult((Defence.Reasoned(why, found.Question), (string?)null)));
                var (reasoned, aiFeedback) = run.Result;
                var outcome = await store.CompleteAsync(context.User.Id(), id, request.Choice, reasoned, context.RequestAborted);
                if (outcome is null)
                {
                    return Results.Json(new { code = "no_defence" }, statusCode: StatusCodes.Status400BadRequest);
                }

                return Results.Ok(new
                {
                    correct = outcome.Transfer,
                    reasoned = outcome.Reasoned,
                    verified = outcome.Verified,
                    answer = outcome.Answer,
                    explanation = outcome.Explanation,
                    feedback = string.IsNullOrWhiteSpace(aiFeedback) ? Defence.Feedback(outcome.Transfer, outcome.Reasoned, language) : aiFeedback,
                    reward = QuizEndpoints.RewardView(outcome.Reward, l),
                    engine = run.Engine,
                    fallback = run.Fallback,
                });
            });
        });

        return app;
    }

    private const int QuizQuestionChoices = 4;

    private static async Task<object> Next(HttpContext context, DefenceStore store, string attemptId, IStringLocalizer<SharedResource> l)
    {
        var next = await store.NextAsync(context.User.Id(), attemptId, context.RequestAborted);
        if (next is not null)
        {
            return new { done = false, question = new { id = next.Id, prompt = next.Prompt, options = next.Options, visual = next.Visual, code = next.Code, topic = QuizEndpoints.TopicName(next.Topic, l), number = next.Done + 1, total = next.Total } };
        }

        var all = await store.ForAttemptAsync(context.User.Id(), attemptId, context.RequestAborted);
        return new { done = true, verified = all.Count(d => d.Verified), total = all.Count };
    }
}
