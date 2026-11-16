using EduVerse.Core.Catalog;
using EduVerse.Core.Tutor;
using EduVerse.Infrastructure.Engines;
using EduVerse.Infrastructure.Learning;
using EduVerse.Infrastructure.Tutor;
using Microsoft.Extensions.Localization;

namespace EduVerse.Web;

public sealed record TeachRequest(string Text, string? SessionId);

public sealed record InterviewRequest(string Question, IReadOnlyList<InterviewTurn>? History);

public sealed record CompanionRequest(string Message, string? Page, string? Title);

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
            // Documents the teacher attached to this figure: the AI answers from them; offline, the piece that matches the question is quoted
            var excerpts = await context.RequestServices.GetRequiredService<Infrastructure.Tutor.MaterialStore>().ExcerptsAsync(context.User.Id(), MaterialTarget.Figure, f.Key, question, context.RequestAborted);
            return await AiRunner.GuardAsync(async () =>
            {
                var run = await AiRunner.RunAsync(context, engines, logger,
                    async ai => (Answer: InterviewPrompt.Parse(await ai.GenerateAsync(InterviewPrompt.Build(f, level, question, history, language, Materials.ForPrompt(excerpts)), context.RequestAborted)), Matched: true),
                    () =>
                    {
                        var match = Interviews.Match(f, question, language);
                        if (match is null && Materials.Quote(excerpts.Where(e => Materials.Score(e.Text, question) > 0).ToList(), language) is { } quote)
                        {
                            return Task.FromResult((Answer: quote, Matched: true));
                        }

                        return Task.FromResult((Answer: match?.Answer.In(language) ?? string.Format(System.Globalization.CultureInfo.CurrentCulture, l["Interview_DemoOnly"].Value, f.Name), Matched: match is not null));
                    });
                return Results.Ok(new { answer = run.Result.Answer, matched = run.Result.Matched, engine = run.Engine, fallback = run.Fallback });
            });
        })
        .RequireRateLimiting("tutor");

        // Evo on every page: knows the progress of the student and the conversation; the actions are decided in code.
        api.MapGet("/companion", async (string? page, string? title, HttpContext context, CompanionStore store, IStringLocalizer<SharedResource> l) =>
        {
            var language = Localization.LanguageForCurrentCulture();
            var snapshot = await store.SnapshotAsync(context.User.Id(), context.User.DisplayName(), Clip(page, 120), Clip(title, 120), Names(l), context.RequestAborted);
            var actions = Companion.Suggest(snapshot, language);
            var history = await store.HistoryAsync(context.User.Id(), ct: context.RequestAborted);
            return Results.Ok(new
            {
                greeting = Companion.Greeting(snapshot, language),
                actions = actions.Select(ActionView),
                messages = history.Select(m => new { fromStudent = m.FromStudent, text = m.Text, at = m.At }),
            });
        });

        api.MapPost("/companion", async (CompanionRequest request, HttpContext context, CompanionStore store, AiEngines engines, ILogger<CompanionRequest> logger, IStringLocalizer<SharedResource> l) =>
        {
            var message = request?.Message?.Trim() ?? "";
            if (message.Length is < 1 or > Companion.MaxMessage)
            {
                return Results.BadRequest();
            }

            var language = Localization.LanguageForCurrentCulture();
            var page = Clip(request!.Page, 120);
            var snapshot = await store.SnapshotAsync(context.User.Id(), context.User.DisplayName(), page, Clip(request.Title, 120), Names(l), context.RequestAborted);
            var actions = Companion.Suggest(snapshot, language);
            var history = (await store.HistoryAsync(context.User.Id(), Companion.HistoryForAi, context.RequestAborted)).Select(m => new CompanionTurn(m.FromStudent, m.Text)).ToList();
            return await AiRunner.GuardAsync(async () =>
            {
                var run = await AiRunner.RunAsync(context, engines, logger,
                    async ai => CompanionPrompt.Parse(await ai.GenerateAsync(CompanionPrompt.Build(snapshot, actions, history, message, language), context.RequestAborted), actions),
                    () => Task.FromResult(Companion.Demo(snapshot, actions, message, language)));
                await store.SaveAsync(context.User.Id(), message, run.Result, page, context.RequestAborted);
                return Results.Ok(new
                {
                    reply = run.Result.Text,
                    actions = actions.Where(a => run.Result.Actions.Contains(a.Id)).Select(ActionView),
                    engine = run.Engine,
                    fallback = run.Fallback,
                });
            });
        })
        .RequireRateLimiting("tutor");

        api.MapDelete("/companion", async (HttpContext context, CompanionStore store) =>
            Results.Ok(new { deleted = await store.ClearAsync(context.User.Id(), context.RequestAborted) }));

        return app;
    }

    private static string Clip(string? text, int max) => text is null ? "" : text.Length > max ? text[..max] : text;

    private static object ActionView(CompanionAction a) => new { id = a.Id, url = a.Url, label = a.Label, icon = a.Icon };

    private static CompanionNames Names(IStringLocalizer l) => new(
        topic => QuizEndpoints.TopicName(topic, l),
        area => l[$"Area_{area}"].Value,
        rank => l[$"Rank_{rank}"].Value,
        mode => l[$"QuizMode_{mode}"].Value);
}
