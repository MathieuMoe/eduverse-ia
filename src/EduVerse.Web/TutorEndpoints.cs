using EduVerse.Core.Catalog;
using EduVerse.Core.Tutor;
using EduVerse.Infrastructure.Engines;
using EduVerse.Infrastructure.Learning;
using EduVerse.Infrastructure.Tutor;
using Microsoft.Extensions.Localization;

namespace EduVerse.Web;

public sealed record TeachRequest(string Text, string? SessionId);

public sealed record InterviewRequest(string Question, IReadOnlyList<InterviewTurn>? History);

public sealed record InterviewRiddleCheckRequest(int CheckIndex, string Answer);

public sealed record InterviewMythCheckRequest(int Index, bool Answer);

public sealed record CompanionRequest(string Message, string? Page, string? Title, string? Style = null);

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

        // The figure poses one checked fact back as a riddle once the student has discovered everything; the correct
        // option is picked server-side only (see Interviews.Riddle / RiddleCorrect), so the answer key never reaches the client.
        api.MapGet("/interview/{figure}/riddle", (string figure) =>
        {
            var f = Interviews.Find(figure);
            if (f is null)
            {
                return Results.BadRequest();
            }

            var language = Localization.LanguageForCurrentCulture();
            var (checkIndex, question, options) = Interviews.Riddle(f, language);
            return Results.Ok(new { checkIndex, question = question.In(language), options });
        })
        .RequireRateLimiting("tutor");

        api.MapPost("/interview/{figure}/riddle/check", async (string figure, InterviewRiddleCheckRequest request, HttpContext context, FigureDiscoveryStore discoveries) =>
        {
            var f = Interviews.Find(figure);
            if (f is null || request is null)
            {
                return Results.BadRequest();
            }

            var language = Localization.LanguageForCurrentCulture();
            var correct = Interviews.RiddleCorrect(f, request.CheckIndex, request.Answer, language);
            var answer = request.CheckIndex >= 0 && request.CheckIndex < f.Checks.Count ? f.Checks[request.CheckIndex].Answer.In(language) : "";
            // Reaching the riddle already means every suggested question was discovered (the client only poses it
            // once the curiosity meter hits 100%), and there is no retry once it is answered either way: that is
            // what "completing" a figure means for the album, the timeline and the connections bonus, right or wrong.
            await discoveries.MarkCompletedAsync(context.User.Id(), f.Key, context.RequestAborted).ConfigureAwait(false);
            return Results.Ok(new { correct, answer });
        })
        .RequireRateLimiting("tutor");

        // The myth-busting round shown after the postcard: a couple of true-or-false claims about the figure. The
        // answer key (IsTrue) never reaches the client until /myths/check is called (see Interviews.Myths).
        api.MapGet("/interview/{figure}/myths", (string figure) =>
        {
            var f = Interviews.Find(figure);
            if (f is null)
            {
                return Results.BadRequest();
            }

            var language = Localization.LanguageForCurrentCulture();
            var items = Interviews.Myths(f).Select((m, i) => new { index = i, statement = m.Statement.In(language) });
            return Results.Ok(new { items });
        })
        .RequireRateLimiting("tutor");

        api.MapPost("/interview/{figure}/myths/check", (string figure, InterviewMythCheckRequest request) =>
        {
            var f = Interviews.Find(figure);
            var myths = f is null ? [] : Interviews.Myths(f);
            if (f is null || request is null || request.Index < 0 || request.Index >= myths.Count)
            {
                return Results.BadRequest();
            }

            var language = Localization.LanguageForCurrentCulture();
            var myth = myths[request.Index];
            var correct = request.Answer == myth.IsTrue;
            return Results.Ok(new { correct, isTrue = myth.IsTrue, explanation = myth.Explanation.In(language) });
        })
        .RequireRateLimiting("tutor");

        // Evo on every page: knows the progress of the student and the conversation; the actions are decided in code.
        api.MapGet("/companion", async (string? page, string? title, HttpContext context, CompanionStore store, IStringLocalizer<SharedResource> l) =>
        {
            var language = Localization.LanguageForCurrentCulture();
            var snapshot = await store.SnapshotAsync(context.User.Id(), context.User.DisplayName(), Clip(page, 120), Clip(title, 120), Names(l), language, context.RequestAborted);
            var actions = Companion.Suggest(snapshot, language);
            var history = await store.HistoryAsync(context.User.Id(), ct: context.RequestAborted);
            return Results.Ok(new
            {
                greeting = Companion.Greeting(snapshot, language),
                actions = actions.Select(ActionView),
                messages = history.Select(m => new { fromUser = m.FromStudent, text = m.Text, at = m.At }),
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
            var snapshot = await store.SnapshotAsync(context.User.Id(), context.User.DisplayName(), page, Clip(request.Title, 120), Names(l), language, context.RequestAborted);
            var actions = Companion.Suggest(snapshot, language);
            var history = (await store.HistoryAsync(context.User.Id(), Companion.HistoryForAi, context.RequestAborted)).Select(m => new CompanionTurn(m.FromStudent, m.Text)).ToList();
            var style = Companion.ParseStyle(request.Style);
            return await AiRunner.GuardAsync(async () =>
            {
                var run = await AiRunner.RunAsync(context, engines, logger,
                    async ai => CompanionPrompt.Parse(await ai.GenerateAsync(CompanionPrompt.Build(snapshot, actions, history, message, language, style), context.RequestAborted), actions),
                    () => Task.FromResult(Companion.Demo(snapshot, actions, message, language, style)));
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
