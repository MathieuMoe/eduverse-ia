using EduVerse.Core.Tutor;
using EduVerse.Infrastructure.Gamification;
using EduVerse.Infrastructure.Tutor;
using Microsoft.Extensions.Localization;

namespace EduVerse.Web;

public sealed record FadingStartRequest(FadingKind Kind);

public sealed record FadingAnswerRequest(List<string?>? Answers);

/// <summary>Fading examples: worked problems where the help disappears step by step. Problems and checks are computed in code.</summary>
public static class FadingEndpoints
{
    public static IEndpointRouteBuilder MapFading(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api/fading").RequireAuthorization(CurrentUser.StudentPolicy).RequireRateLimiting("tutor");

        api.MapPost("/start", async (FadingStartRequest? request, HttpContext context, FadingStore store, GamificationStore game) =>
        {
            var level = (await game.ProfileAsync(context.User.Id(), context.RequestAborted)).Level;
            var view = request is null || !Enum.IsDefined(request.Kind) ? null : await store.StartAsync(context.User.Id(), level, request.Kind, Localization.LanguageForCurrentCulture(), context.RequestAborted);
            return view is null ? Results.Json(new { code = "fading_kind" }, statusCode: StatusCodes.Status400BadRequest) : Results.Ok(Payload(view));
        });

        api.MapPost("/{id}/answer", async (string id, FadingAnswerRequest? request, HttpContext context, FadingStore store, IStringLocalizer<SharedResource> l) =>
        {
            if (request?.Answers is { Count: > 6 })
            {
                return Results.Json(new { code = "fading_answer" }, statusCode: StatusCodes.Status400BadRequest);
            }

            var outcome = await store.AnswerAsync(context.User.Id(), id, request?.Answers, context.RequestAborted);
            return outcome is null ? Results.Json(new { code = "no_fading" }, statusCode: StatusCodes.Status400BadRequest)
                : Results.Ok(new { correct = outcome.Correct, solution = outcome.Solution.Select(s => new { work = s.Work, result = s.Result }), next = Payload(outcome.Next), reward = QuizEndpoints.RewardView(outcome.Reward, l) });
        });

        return app;
    }

    private static object Payload(FadingView v) => new
    {
        id = v.Id, kind = v.Kind.ToString(), stage = v.Stage, stages = Fading.Stages, problem = v.Problem, blanks = v.Blanks, done = v.Done,
        lines = v.Lines.Select(line => new { work = line.Work, result = line.Result }),
    };
}
