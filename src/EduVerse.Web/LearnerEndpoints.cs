using System.Text.Json;
using EduVerse.Core.Catalog;
using EduVerse.Core.Curriculum;
using EduVerse.Core.Tutor;
using EduVerse.Infrastructure.Engines;
using EduVerse.Infrastructure.Gamification;
using EduVerse.Infrastructure.Quizzes;
using EduVerse.Infrastructure.Tutor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace EduVerse.Web;

public sealed record LadderRequest(string? Reply);

public sealed record ThinkingRequest(string AttemptId, int Number, string? Text, ThinkingError? Reason);

public sealed record NoteRequest(string? Text);

public sealed record HideRequest(bool Hidden);

public sealed record InterestsRequest(List<string>? Keys);

/// <summary>
/// The tutor that understands how the student thinks: the Socratic ladder on a question ("Think with the Tutor"), the diagnosis of
/// the reasoning after a wrong answer ("Tell me how you thought") and the learner card ("How I learn").
/// The rungs, the catalogue of mistakes, the remedies and the numbers are decided in code; the AI only writes.
/// </summary>
public static class LearnerEndpoints
{
    public static IEndpointRouteBuilder MapLearner(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api").RequireAuthorization(CurrentUser.StudentPolicy).RequireRateLimiting("tutor");

        api.MapPost("/quiz/{attemptId}/ladder", async (string attemptId, LadderRequest? request, HttpContext context, QuizStore quizzes, AiEngines engines, ILogger<LadderRequest> logger, IStringLocalizer<SharedResource> l) =>
        {
            var language = Localization.LanguageForCurrentCulture();
            var state = await quizzes.LadderAsync(context.User.Id(), attemptId, context.RequestAborted);
            if (state is null)
            {
                return Results.Json(new { code = "no_ladder" }, statusCode: StatusCodes.Status400BadRequest);
            }

            if (state.Done)
            {
                return Results.Ok(new { rung = state.Rung, max = Ladder.Max, free = Ladder.Free, done = true, reaction = (string?)null, question = Ladder.Done(language), engine = "code" });
            }

            var reply = request?.Reply?.Trim();
            reply = reply is { Length: > Ladder.MaxReply } ? reply[..Ladder.MaxReply] : reply;
            var topic = QuizEndpoints.TopicName(state.Question.Topic, l);
            var written = new LadderStep(state.Rung, Ladder.Reaction(reply, language), Ladder.Question(state.Rung, state.Question, topic, language));
            return await AiRunner.GuardAsync(async () =>
            {
                var run = await AiRunner.RunAsync(context, engines, logger,
                    async ai => Ladder.Safe(LadderPrompt.Parse(await ai.GenerateAsync(LadderPrompt.Build(state.Question, topic, state.Rung, reply, state.Level, language), context.RequestAborted), state.Rung), state.Question, topic, language),
                    () => Task.FromResult(written));
                return Results.Ok(new
                {
                    rung = state.Rung,
                    max = Ladder.Max,
                    free = Ladder.Free,
                    done = false,
                    reaction = run.Result.Reaction,
                    question = run.Result.Question,
                    engine = run.Engine,
                    fallback = run.Fallback,
                });
            });
        });

        api.MapPost("/thinking", async (ThinkingRequest request, HttpContext context, LearnerStore learner, AiEngines engines, ILogger<ThinkingRequest> logger, IStringLocalizer<SharedResource> l) =>
        {
            var language = Localization.LanguageForCurrentCulture();
            var text = request?.Text?.Trim() ?? "";
            var reason = request?.Reason;
            if (request is null || (reason is null && text.Length is < Thinking.MinText or > Thinking.MaxText) || (reason is { } r && !Enum.IsDefined(r)))
            {
                return Results.Json(new { code = "thinking_text" }, statusCode: StatusCodes.Status400BadRequest);
            }

            var answer = await learner.WrongAnswerAsync(context.User.Id(), request.AttemptId, request.Number, context.RequestAborted);
            if (answer is null)
            {
                return Results.Json(new { code = "thinking_answer" }, statusCode: StatusCodes.Status400BadRequest);
            }

            return await AiRunner.GuardAsync(async () =>
            {
                ThinkingReply reply;
                string engine;
                string? fallback = null;
                if (reason is { } chosen)
                {
                    // The student picked the reason: nothing to interpret, the code answers
                    (reply, engine) = (new ThinkingReply(chosen, Thinking.Feedback(chosen, language), null), "code");
                }
                else
                {
                    var options = JsonSerializer.Deserialize<List<string>>(answer.OptionsJson) ?? [];
                    var thinking = new ThinkingContext(answer.Prompt, options, answer.Choice, answer.CorrectIndex, answer.Explanation, QuizEndpoints.TopicName(answer.Topic, l), answer.Confidence, answer.Hints, answer.Level);
                    var run = await AiRunner.RunAsync(context, engines, logger,
                        async ai => ThinkingPrompt.Parse(await ai.GenerateAsync(ThinkingPrompt.Build(thinking, text, language), context.RequestAborted)),
                        () => Task.FromResult(Thinking.Demo(text, answer.Confidence, language)));
                    (reply, engine, fallback) = (run.Result, run.Engine, run.Fallback);
                }

                var (remedy, reward) = await learner.SaveThinkingAsync(context.User.Id(), answer, reply.Error, context.RequestAborted);
                var action = ActionFor(remedy, answer.Area, answer.Level, answer.Topic, l);
                return Results.Ok(new
                {
                    error = reply.Error.ToString(),
                    label = Thinking.Label(reply.Error, language),
                    feedback = reply.Feedback,
                    question = reply.Question,
                    remedy = remedy.ToString(),
                    tip = Thinking.Tip(remedy, language),
                    action = action is { } a ? new { url = a.Url, label = a.Label } : null,
                    reward = QuizEndpoints.RewardView(reward, l),
                    engine,
                    fallback,
                });
            });
        });

        api.MapPost("/learner/interests", async (InterestsRequest request, HttpContext context, EduVerse.Infrastructure.Data.EduVerseDb db) =>
        {
            var userId = context.User.Id();
            var profile = await db.StudentProfiles.FirstOrDefaultAsync(p => p.UserId == userId, context.RequestAborted);
            if (profile is null)
            {
                return Results.NotFound();
            }

            profile.Interests = Interests.Join(request?.Keys ?? []);
            await db.SaveChangesAsync(context.RequestAborted);
            return Results.Ok(new { saved = true, keys = Interests.Parse(profile.Interests) });
        });

        api.MapPost("/learner/notes", async (NoteRequest request, HttpContext context, LearnerStore learner) =>
        {
            var outcome = await learner.AddNoteAsync(context.User.Id(), request?.Text, context.RequestAborted);
            return outcome == NoteOutcome.Saved ? Results.Ok(new { saved = true }) : Results.Json(new { code = "note_" + outcome }, statusCode: StatusCodes.Status400BadRequest);
        });

        api.MapDelete("/learner/notes/{id:int}", async (int id, HttpContext context, LearnerStore learner) =>
            await learner.RemoveNoteAsync(context.User.Id(), id, context.RequestAborted) ? Results.Ok(new { deleted = true }) : Results.NotFound());

        api.MapPost("/learner/insight/{key}", async (string key, HideRequest request, HttpContext context, LearnerStore learner) =>
            request is not null && await learner.SetHiddenAsync(context.User.Id(), key, request.Hidden, context.RequestAborted) ? Results.Ok(new { hidden = request.Hidden }) : Results.NotFound());

        api.MapPost("/learner/summary", async (HttpContext context, LearnerStore learner, GamificationStore game, AiEngines engines, ILogger<NoteRequest> logger) =>
        {
            var language = Localization.LanguageForCurrentCulture();
            var stats = await learner.StatsAsync(context.User.Id(), context.RequestAborted);
            var level = (await game.ProfileAsync(context.User.Id(), context.RequestAborted)).Level;
            var name = context.User.DisplayName().Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
            return await AiRunner.GuardAsync(async () =>
            {
                var run = await AiRunner.RunAsync(context, engines, logger,
                    async ai => LearnerPrompt.Parse(await ai.GenerateAsync(LearnerPrompt.Build(stats, name, level, language), context.RequestAborted)),
                    () => Task.FromResult(LearnerProfile.Summary(stats, language)));
                return Results.Ok(new { summary = run.Result, engine = run.Engine, fallback = run.Fallback });
            });
        });

        return app;
    }

    /// <summary>Where the remedy sends the student (decided in code): the tutor of the topic, more practice or today's review.</summary>
    private static (string Url, string Label)? ActionFor(Remedy remedy, Area area, Level level, string topic, IStringLocalizer l)
    {
        var bank = !topic.Contains(':', StringComparison.Ordinal) && Catalog.Available(area, level);
        return remedy switch
        {
            Remedy.Review => ("/Student/Quiz?mode=Review", l["Thinking_ActionReview"].Value),
            Remedy.Explain or Remedy.StepByStep when bank => ("/Student/Learn?subject=" + AreaSubjects.Key(area, level), l["Thinking_ActionLearn"].Value),
            Remedy.Practise or Remedy.ReadAgain or Remedy.SlowDown when bank => ($"/Student/Quiz?area={area}&level={level}", l["Thinking_ActionPractise"].Value),
            _ => null,
        };
    }
}
