using System.Text.Json;
using EduVerse.Core.Common;
using EduVerse.Core.Content;
using EduVerse.Core.Learning;
using EduVerse.Infrastructure.Data;
using EduVerse.Infrastructure.Engines;
using EduVerse.Infrastructure.Learning;
using Microsoft.EntityFrameworkCore;

namespace EduVerse.Web;

public sealed record SubjectRequest(string Topic, Stage Stage);

/// <summary>API: subjects generated from a topic (students and teachers) and lesson packs (teachers).</summary>
public static class ContentEndpoints
{
    public const int MinTopicLength = 3;
    public const int MaxTopicLength = 200;
    public const int MaxSubjectsPerUser = 20;

    public static IEndpointRouteBuilder MapContent(this IEndpointRouteBuilder app)
    {
        // A subject from any topic: the AI proposes the concept map, the code checks it (unique keys, known prerequisites, no cycle)
        app.MapPost("/api/subjects", async (SubjectRequest request, HttpContext context, AiEngines engines, DemoLearningAi demo, LearningStore store, EduVerseDb db, ILogger<SubjectRequest> logger) =>
        {
            var topic = (request?.Topic ?? "").Trim();
            if (topic.Length is < MinTopicLength or > MaxTopicLength)
            {
                return Results.Json(new { code = "topic_length" }, statusCode: StatusCodes.Status400BadRequest);
            }

            var userId = context.User.Id();
            if (await db.Subjects.CountAsync(s => s.OwnerId == userId, context.RequestAborted) >= MaxSubjectsPerUser)
            {
                return Results.Json(new { code = "too_many_subjects" }, statusCode: StatusCodes.Status400BadRequest);
            }

            var language = Localization.LanguageForCurrentCulture();
            return await AiRunner.GuardAsync(async () =>
            {
                var run = await AiRunner.RunAsync(context, engines, logger,
                    ai => new AiLearning(ai).ConceptMapAsync(topic, request!.Stage, language, context.RequestAborted),
                    () => demo.ConceptMapAsync(topic, request!.Stage, language, context.RequestAborted));
                var record = await store.SaveSubjectAsync(run.Result, userId, language, context.RequestAborted);
                return Results.Ok(new { engine = run.Engine, id = record.Id, name = record.Name, concepts = run.Result.Ordered().Select(c => c.Name) });
            });
        })
        .RequireAuthorization()
        .RequireRateLimiting("ai");

        var packs = app.MapGroup("/api/packs").RequireAuthorization(CurrentUser.TeacherPolicy);

        packs.MapPost("", async (PackRequest request, HttpContext context, AiEngines engines, DemoPackGenerator demo, EduVerseDb db, TimeProvider clock, ILogger<PackRequest> logger) =>
        {
            var length = request?.Text?.Trim().Length ?? 0;
            if (request is null || length < PackRequest.MinTextLength)
            {
                return Results.Json(new { code = "text_too_short" }, statusCode: StatusCodes.Status400BadRequest);
            }

            if (length > PackRequest.MaxTextLength)
            {
                return Results.Json(new { code = "text_too_long" }, statusCode: StatusCodes.Status400BadRequest);
            }

            if ((request.Title ?? "").Length > 150)
            {
                return Results.Json(new { code = "title_too_long" }, statusCode: StatusCodes.Status400BadRequest);
            }

            var clean = request with { Title = (request.Title ?? "").Trim(), Subject = (request.Subject ?? "").Trim(), Text = request.Text!.Trim() };
            if (clean.Title.Length == 0)
            {
                clean = clean with { Title = Lang.T(clean.Language, "Lesson", "Lección", "Leçon", "Lliçó") };
            }

            var run = await AiRunner.RunAsync(context, engines, logger,
                ai => new AiPackGenerator(ai).GenerateAsync(clean, context.RequestAborted),
                () => demo.GenerateAsync(clean, context.RequestAborted));
            var record = new LessonPackRecord
            {
                TeacherId = context.User.Id(),
                Title = run.Result.Title.Length > 200 ? run.Result.Title[..200] : run.Result.Title,
                Engine = run.Engine,
                Json = JsonSerializer.Serialize(run.Result, LearningStore.JsonOptions),
                CreatedAt = clock.GetUtcNow(),
            };
            db.LessonPacks.Add(record);
            await db.SaveChangesAsync(context.RequestAborted);
            return Results.Ok(new { id = record.Id, engine = run.Engine, fallback = run.Fallback, pack = run.Result });
        })
        .RequireRateLimiting("ai");

        packs.MapGet("", async (HttpContext context, EduVerseDb db) =>
        {
            var userId = context.User.Id();
            var list = await db.LessonPacks.AsNoTracking().Where(p => p.TeacherId == userId).OrderByDescending(p => p.CreatedAt).Take(30)
                .Select(p => new { p.Id, p.Title, p.Engine, p.CreatedAt }).ToListAsync(context.RequestAborted);
            return Results.Ok(list);
        });

        packs.MapGet("/{id:int}", async (int id, HttpContext context, EduVerseDb db) =>
        {
            var userId = context.User.Id();
            var record = await db.LessonPacks.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id && p.TeacherId == userId, context.RequestAborted);
            return record is null
                ? Results.NotFound()
                : Results.Ok(new { id = record.Id, engine = record.Engine, fallback = (string?)null, pack = JsonSerializer.Deserialize<LessonPack>(record.Json, LearningStore.JsonOptions) });
        });

        packs.MapDelete("/{id:int}", async (int id, HttpContext context, EduVerseDb db) =>
        {
            var userId = context.User.Id();
            var deleted = await db.LessonPacks.Where(p => p.Id == id && p.TeacherId == userId).ExecuteDeleteAsync(context.RequestAborted);
            return deleted == 0 ? Results.NotFound() : Results.NoContent();
        });

        app.MapGet("/api/samples/pack", () =>
        {
            var (title, subject, text) = Core.Lessons.LessonSamples.Get(Localization.LanguageForCurrentCulture());
            return Results.Ok(new { title, subject, text });
        }).RequireAuthorization(CurrentUser.TeacherPolicy);

        return app;
    }
}
