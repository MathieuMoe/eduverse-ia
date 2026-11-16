using EduVerse.Core.Catalog;
using EduVerse.Core.Tutor;
using EduVerse.Infrastructure.Engines;
using EduVerse.Infrastructure.Learning;
using EduVerse.Infrastructure.Tutor;
using Microsoft.Extensions.Localization;

namespace EduVerse.Web;

public sealed record TeachRequest(string Text, string? SessionId);

public sealed record InterviewRequest(string Question, IReadOnlyList<InterviewTurn>? History);

/// <summary>
/// "Explain it to Evo" (the rubric decides, the AI only writes Evo's words) and interviews with historical figures
/// (a simulation from documented facts; offline, prepared answers).
/// </summary>
public static class TutorEndpoints
{
    public static IEndpointRouteBuilder MapTutor(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api").RequireAuthorization(CurrentUser.StudentPolicy);

        api.MapPost("/teach/{concept}", async (string concept, TeachRequest request, HttpContext context, TeachStore store, LearningStore learning, AiEngines engines, ILogger<TeachRequest> logger, IStringLocalizer<SharedResource> l) =>
        {
            var language = Localization.LanguageForCurrentCulture();
            var step = request is null ? null : await store.ExplainAsync(context.User.Id(), concept, request.SessionId, request.Text, language, context.RequestAborted);
            if (step is null)
            {
                return Results.BadRequest();
            }

            var level = (await learning.ProfileAsync(context.User.Id(), context.RequestAborted)).Level;
            return await AiRunner.GuardAsync(async () =>
            {
                var run = await AiRunner.RunAsync(context, engines, logger,
                    async ai => TeachPrompt.Parse(await ai.GenerateAsync(TeachPrompt.Build(step.Concept, step.Session.Text, step.Done ? null : step.Missing, level, language), context.RequestAborted)),
                    () => Task.FromResult(TeachPrompt.Demo(step.Concept, step.Covered.Count, step.Done ? null : step.Missing, language)));
                var (reaction, question) = run.Result;
                return Results.Ok(new
                {
                    sessionId = step.Session.Id,
                    engine = run.Engine,
                    round = step.Session.Rounds,
                    maxRounds = TeachConcepts.MaxRounds,
                    ideas = step.Concept.Ideas.Select(i => new { key = i.Key, covered = step.Covered.Contains(i.Key), name = step.Covered.Contains(i.Key) || step.Done ? i.Name.In(language) : null }),
                    reaction,
                    question = step.Done ? null : question,
                    done = step.Done,
                    model = step.Done ? step.Concept.Model.In(language) : null,
                    reward = QuizEndpoints.RewardView(step.Reward, l),
                });
            });
        })
        .RequireRateLimiting("tutor");

        api.MapPost("/interview/{figure}", async (string figure, InterviewRequest request, HttpContext context, LearningStore learning, AiEngines engines, ILogger<InterviewRequest> logger, IStringLocalizer<SharedResource> l) =>
        {
            var f = Interviews.Find(figure);
            var question = request?.Question?.Trim() ?? "";
            if (f is null || question.Length is < 2 or > Interviews.MaxQuestion)
            {
                return Results.BadRequest();
            }

            var language = Localization.LanguageForCurrentCulture();
            var level = (await learning.ProfileAsync(context.User.Id(), context.RequestAborted)).Level;
            var history = (request!.History ?? []).TakeLast(Interviews.MaxTurns).ToList();
            return await AiRunner.GuardAsync(async () =>
            {
                var run = await AiRunner.RunAsync(context, engines, logger,
                    async ai => (Answer: InterviewPrompt.Parse(await ai.GenerateAsync(InterviewPrompt.Build(f, level, question, history, language), context.RequestAborted)), Matched: true),
                    () =>
                    {
                        var match = Interviews.Match(f, question, language);
                        return Task.FromResult((Answer: match?.Answer.In(language) ?? string.Format(System.Globalization.CultureInfo.CurrentCulture, l["Interview_DemoOnly"].Value, f.Name), Matched: match is not null));
                    });
                return Results.Ok(new { answer = run.Result.Answer, matched = run.Result.Matched, engine = run.Engine, fallback = run.Fallback });
            });
        })
        .RequireRateLimiting("tutor");

        return app;
    }
}
