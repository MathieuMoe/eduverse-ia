using EduVerse.Core.Common;
using EduVerse.Core.Lessons;
using EduVerse.Core.Summaries;
using EduVerse.Infrastructure.Engines;
using EduVerse.Infrastructure.Teaching;

namespace EduVerse.Web;

/// <summary>API of the teacher tools reused from MatGPT Teacher: lesson in 3 levels and class summary (results kept in the teacher's browser).</summary>
public static class TeachingEndpoints
{
    public static IEndpointRouteBuilder MapTeaching(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/lesson", async (LessonRequest request, AiEngines engines, DemoLessonAdapter demo, HttpContext context, ILogger<LessonRequest> logger) =>
        {
            var errors = LessonValidator.Check(request);
            if (errors.Count > 0)
            {
                return Results.Json(new { code = errors[0] }, statusCode: StatusCodes.Status400BadRequest);
            }

            var clean = request with { Title = request.Title?.Trim() ?? "", Subject = request.Subject?.Trim() ?? "", Text = request.Text.Trim() };
            var run = await AiRunner.RunAsync(context, engines, logger,
                ai => new AiLessonAdapter(ai).AdaptAsync(clean, context.RequestAborted), () => demo.AdaptAsync(clean, context.RequestAborted));
            return Results.Ok(new { engine = run.Engine, fallback = run.Fallback, result = run.Result });
        })
        .RequireAuthorization(CurrentUser.TeacherPolicy)
        .RequireRateLimiting("ai");

        app.MapPost("/api/summary", async (SummaryRequest request, AiEngines engines, DemoClassSummarizer demo, HttpContext context, ILogger<SummaryRequest> logger) =>
        {
            var length = request?.Notes?.Trim().Length ?? 0;
            if (request is null || length < SummaryRequest.MinNotesLength)
            {
                return Results.Json(new { code = "notes_too_short" }, statusCode: StatusCodes.Status400BadRequest);
            }

            if (length > SummaryRequest.MaxNotesLength)
            {
                return Results.Json(new { code = "notes_too_long" }, statusCode: StatusCodes.Status400BadRequest);
            }

            var clean = request with { Subject = request.Subject?.Trim() ?? "", Group = request.Group?.Trim() ?? "", Notes = request.Notes!.Trim() };
            var run = await AiRunner.RunAsync(context, engines, logger,
                ai => new AiClassSummarizer(ai).SummarizeAsync(clean, context.RequestAborted), () => demo.SummarizeAsync(clean, context.RequestAborted));
            return Results.Ok(new { engine = run.Engine, fallback = run.Fallback, summary = run.Result });
        })
        .RequireAuthorization(CurrentUser.TeacherPolicy)
        .RequireRateLimiting("ai");

        // Examples in the interface language
        app.MapGet("/api/samples/lesson", () =>
        {
            var (title, subject, text) = LessonSamples.Get(Localization.LanguageForCurrentCulture());
            return Results.Ok(new { title, subject, text });
        });
        app.MapGet("/api/samples/summary", () => Results.Ok(new { notes = SummaryNotes() }));

        return app;
    }

    private static string SummaryNotes() =>
        string.Join("\n", SummarySamples.Notes(Localization.LanguageForCurrentCulture()).Split('\n').Select(l => l.Trim()));
}
