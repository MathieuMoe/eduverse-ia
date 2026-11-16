using EduVerse.Core.Common;
using EduVerse.Core.Institutions;
using EduVerse.Infrastructure.Engines;
using EduVerse.Infrastructure.Institutions;

namespace EduVerse.Web;

public sealed record FamilyMessageRequest(string StudentId, FamilyTone Tone, TeachLanguage Language, string? Note);

/// <summary>API of the institution space: the message to a student's family (facts in code, text by the AI or the offline template).</summary>
public static class InstitutionEndpoints
{
    public static IEndpointRouteBuilder MapInstitution(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/institution/family", async (FamilyMessageRequest request, HttpContext context, InstitutionStore store, AiEngines engines, DemoFamilyWriter demo, ILogger<FamilyMessageRequest> logger) =>
        {
            var institution = await store.ForUserAsync(context.User.Id(), context.RequestAborted);
            if (institution is null || request is null)
            {
                return Results.NotFound();
            }

            if ((request.Note ?? "").Length > FamilyRequest.MaxNoteLength)
            {
                return Results.Json(new { code = "note_too_long" }, statusCode: StatusCodes.Status400BadRequest);
            }

            var facts = await store.FamilyFactsAsync(institution, request.StudentId, Localization.LanguageFor(Localization.CultureFor(request.Language)), context.RequestAborted);
            if (facts is null)
            {
                return Results.NotFound();
            }

            var family = new FamilyRequest(facts, request.Tone, request.Language, request.Note?.Trim(), institution.Name);
            var run = await AiRunner.RunAsync(context, engines, logger,
                ai => new AiFamilyWriter(ai).WriteAsync(family, context.RequestAborted), () => demo.WriteAsync(family, context.RequestAborted));
            return Results.Ok(new { engine = run.Engine, fallback = run.Fallback, facts, message = run.Result });
        })
        .RequireAuthorization(CurrentUser.DirectorPolicy)
        .RequireRateLimiting("ai");

        return app;
    }
}
