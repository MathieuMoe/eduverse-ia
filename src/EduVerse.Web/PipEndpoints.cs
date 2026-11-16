using EduVerse.Core.Tutor;
using EduVerse.Infrastructure.Engines;
using EduVerse.Infrastructure.Gamification;
using EduVerse.Infrastructure.Tutor;
using Microsoft.Extensions.Localization;

namespace EduVerse.Web;

public sealed record PipAnswerRequest(int Line, int Reason);

public sealed record PipStartRequest(string? Subject);

/// <summary>"Correct Pip": the student finds the mistake of an AI student. The problem and the mistake are generated in code; the server keeps only the seed.</summary>
public static class PipEndpoints
{
    public static IEndpointRouteBuilder MapPip(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api/pip").RequireAuthorization(CurrentUser.StudentPolicy).RequireRateLimiting("tutor");

        api.MapPost("/start", async (PipStartRequest? request, HttpContext context, PipStore store, GamificationStore game, IStringLocalizer<SharedResource> l) =>
        {
            var level = (await game.ProfileAsync(context.User.Id(), context.RequestAborted)).Level;
            var view = await store.StartAsync(context.User.Id(), level, Localization.LanguageForCurrentCulture(),
                string.Equals(request?.Subject, "language", StringComparison.OrdinalIgnoreCase) ? PipSubject.Language : PipSubject.Maths, context.RequestAborted);
            return view is null ? Results.Json(new { code = "pip_level" }, statusCode: StatusCodes.Status400BadRequest)
                : Results.Ok(new { id = view.Id, problem = view.Problem, lines = view.Lines, reasons = view.Reasons, from = view.FromTopic is null ? null : QuizEndpoints.TopicName(view.FromTopic, l) });
        });

        api.MapPost("/{id}/answer", async (string id, PipAnswerRequest? request, HttpContext context, PipStore store, IStringLocalizer<SharedResource> l) =>
        {
            if (request is null || request.Line < 0 || request.Line >= 10 || request.Reason < 0 || request.Reason >= Pip.Reasons)
            {
                return Results.Json(new { code = "pip_answer" }, statusCode: StatusCodes.Status400BadRequest);
            }

            var outcome = await store.AnswerAsync(context.User.Id(), id, request.Line, request.Reason, context.RequestAborted);
            return outcome is null ? Results.Json(new { code = "no_pip" }, statusCode: StatusCodes.Status400BadRequest)
                : Results.Ok(new { lineOk = outcome.LineOk, reasonOk = outcome.ReasonOk, wrongStep = outcome.WrongStep, reasonIndex = outcome.ReasonIndex, fix = outcome.Fix, right = outcome.Right, reward = QuizEndpoints.RewardView(outcome.Reward, l) });
        });

        // Pip's reply: the AI writes it (in character, from the facts); offline it is a code-written line.
        api.MapPost("/{id}/voice", async (string id, HttpContext context, PipStore store, AiEngines engines, ILogger<PipAnswerRequest> logger) =>
        {
            var facts = await store.FactsAsync(context.User.Id(), id, context.RequestAborted);
            if (facts is null)
            {
                return Results.Json(new { code = "no_pip" }, statusCode: StatusCodes.Status400BadRequest);
            }

            var language = Localization.LanguageForCurrentCulture();
            return await AiRunner.GuardAsync(async () =>
            {
                var run = await AiRunner.RunAsync(context, engines, logger,
                    async ai => PipVoice.Parse(await ai.GenerateAsync(PipVoice.Build(facts.Problem, facts.LineOk, facts.ReasonOk, facts.Level, language), context.RequestAborted)),
                    () => Task.FromResult(PipVoice.Offline(facts.LineOk, facts.ReasonOk, facts.Problem.Mistake, language)));
                return Results.Ok(new { reply = run.Result, engine = run.Engine, fallback = run.Fallback });
            });
        });

        return app;
    }
}
