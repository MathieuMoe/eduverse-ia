using EduVerse.Core.Catalog;
using EduVerse.Core.Learning;
using EduVerse.Core.Tutor;
using EduVerse.Infrastructure.Engines;
using EduVerse.Infrastructure.Gamification;
using EduVerse.Infrastructure.Quizzes;
using EduVerse.Infrastructure.Tutor;
using Microsoft.Extensions.Localization;

namespace EduVerse.Web;

public sealed record WritingRequest(string? Task, string? Text);

/// <summary>What students do with the teacher's documents (a test about one) and open answers (write it or say it).</summary>
public static class MaterialEndpoints
{
    public static IEndpointRouteBuilder MapMaterials(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api").RequireAuthorization(CurrentUser.StudentPolicy);

        // A test about a document of one of the student's classes: written by the AI only from the document; offline, fill-the-gap
        // questions built in code from its own sentences. A document too poor for that needs an AI.
        api.MapPost("/materials/{id:int}/quiz", async (int id, HttpContext context, MaterialStore materials, QuizStore quizzes, GamificationStore game, AiEngines engines, ILogger<WritingRequest> logger, IStringLocalizer<SharedResource> l) =>
        {
            var userId = context.User.Id();
            var found = await materials.FindForStudentAsync(userId, id, context.RequestAborted);
            if (found is null)
            {
                return Results.NotFound();
            }

            var language = Localization.LanguageForCurrentCulture();
            var level = (await game.ProfileAsync(userId, context.RequestAborted)).Level;
            var material = found.Material;
            return await AiRunner.GuardAsync(async () =>
            {
                var run = await AiRunner.RunAsync<IReadOnlyList<QuizQuestion>>(context, engines, logger,
                    async ai => TopicQuizPrompt.Parse(await ai.GenerateAsync(MaterialQuiz.Build(material.Title, material.Text, level, language), context.RequestAborted), material.Title),
                    () => Task.FromResult(MaterialQuiz.Offline(material.Title, material.Text, Random.Shared.Next(1, int.MaxValue), language) ?? throw new NeedsAiException()));
                var question = await quizzes.StartAsync(userId, Area.Language, level, QuizMode.Topic, language, pool: run.Result, topic: material.Title, engine: run.Engine, ct: context.RequestAborted);
                await materials.MarkAttemptAsync(userId, question.AttemptId, material.Id, context.RequestAborted);
                return Results.Ok(new { engine = run.Engine, fallback = run.Fallback, question = QuizEndpoints.QuestionPayload(question, l) });
            });
        })
        .RequireRateLimiting("ai");

        // An open answer: the code measures it; the AI adds four criteria (each with a fragment of the student's own text, verified).
        // Only the measures are stored, never the text.
        api.MapPost("/writing/check", async (WritingRequest? request, HttpContext context, WritingStore store, GamificationStore game, AiEngines engines, ILogger<WritingRequest> logger, IStringLocalizer<SharedResource> l) =>
        {
            var language = Localization.LanguageForCurrentCulture();
            var task = Writing.Find(request?.Task, language);
            var text = (request?.Text ?? "").Trim();
            if (task is null || text.Length is < 2 or > Writing.MaxChars)
            {
                return Results.Json(new { code = text.Length > Writing.MaxChars ? "writing_long" : "writing_empty" }, statusCode: StatusCodes.Status400BadRequest);
            }

            var userId = context.User.Id();
            var level = (await game.ProfileAsync(userId, context.RequestAborted)).Level;
            if (!Writing.Available(level))
            {
                return Results.Json(new { code = "not_available" }, statusCode: StatusCodes.Status400BadRequest);
            }

            var measured = Writing.Offline(task, text, language);
            return await AiRunner.GuardAsync(async () =>
            {
                var run = await AiRunner.RunAsync(context, engines, logger,
                    async ai =>
                    {
                        var (rubric, next) = WritingPrompt.Parse(await ai.GenerateAsync(WritingPrompt.Build(task, text, level, language), context.RequestAborted));
                        return Writing.Merge(measured, text, rubric, next);
                    },
                    () => Task.FromResult(measured));
                var review = run.Result;
                var (before, reward) = await store.SaveAsync(userId, task, review, context.RequestAborted);
                return Results.Ok(new
                {
                    engine = run.Engine,
                    fallback = run.Fallback,
                    words = review.Stats.Words,
                    passed = review.Passed,
                    total = review.Checks.Count,
                    checks = review.Checks.Select(c => new { key = c.Key, ok = c.Ok, text = c.Text }),
                    rubric = review.Rubric.Select(r => new { name = Writing.RubricName(r.Key, language), score = r.Score, quote = r.Quote, advice = r.Advice }),
                    rubricTotal = review.RubricTotal,
                    next = review.Next,
                    before = before is null ? null : new { passed = before.Passed, total = before.Checks, rubricTotal = before.RubricTotal, words = before.Words },
                    reward = QuizEndpoints.RewardView(reward, l),
                });
            });
        })
        .RequireRateLimiting("ai");

        return app;
    }
}
