using EduVerse.Core.Teaching;
using EduVerse.Infrastructure.Engines;
using EduVerse.Infrastructure.Teaching;
using EduVerse.Infrastructure.Tutor;
using Microsoft.Extensions.Localization;

namespace EduVerse.Web;

public sealed record TeacherCompanionRequest(string Message, string? Page, string? Title);

/// <summary>
/// The assistant of every page of the teacher: the same facts already shown on <c>/Teacher/Class</c> (heatmap, open
/// alerts, hardest concept), read across every one of the teacher's classes and narrated by the AI or, offline, in
/// code. Reuses the student Tutor's own conversation storage (<see cref="CompanionStore"/>) — same shape (one row per
/// message, keyed by UserId), so no new table is needed for a second role's conversation.
/// </summary>
public static class TeacherCompanionEndpoints
{
    public static IEndpointRouteBuilder MapTeacherCompanion(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api").RequireAuthorization(CurrentUser.TeacherPolicy);

        api.MapGet("/teacher-companion", async (string? page, string? title, HttpContext context, TeacherCompanionStore store, CompanionStore messages, IStringLocalizer<SharedResource> l) =>
        {
            var language = Localization.LanguageForCurrentCulture();
            var snapshot = await store.SnapshotAsync(context.User.Id(), context.User.DisplayName(), Clip(page, 120), Clip(title, 120), language, context.RequestAborted);
            var actions = TeacherCompanion.Suggest(snapshot, language);
            var history = await messages.HistoryAsync(context.User.Id(), ct: context.RequestAborted);
            return Results.Ok(new
            {
                greeting = TeacherCompanion.Greeting(snapshot, language),
                actions = actions.Select(ActionView),
                messages = history.Select(m => new { fromUser = m.FromStudent, text = m.Text, at = m.At }),
            });
        });

        api.MapPost("/teacher-companion", async (TeacherCompanionRequest request, HttpContext context, TeacherCompanionStore store, CompanionStore messages, AiEngines engines, ILogger<TeacherCompanionRequest> logger) =>
        {
            var message = request?.Message?.Trim() ?? "";
            if (message.Length is < 1 or > TeacherCompanion.MaxMessage)
            {
                return Results.BadRequest();
            }

            var language = Localization.LanguageForCurrentCulture();
            var page = Clip(request!.Page, 120);
            var snapshot = await store.SnapshotAsync(context.User.Id(), context.User.DisplayName(), page, Clip(request.Title, 120), language, context.RequestAborted);
            var actions = TeacherCompanion.Suggest(snapshot, language);
            var history = (await messages.HistoryAsync(context.User.Id(), TeacherCompanion.HistoryForAi, context.RequestAborted)).Select(m => new TeacherCompanionTurn(m.FromStudent, m.Text)).ToList();
            return await AiRunner.GuardAsync(async () =>
            {
                var run = await AiRunner.RunAsync(context, engines, logger,
                    async ai => TeacherCompanionPrompt.Parse(await ai.GenerateAsync(TeacherCompanionPrompt.Build(snapshot, actions, history, message, language), context.RequestAborted), actions),
                    () => Task.FromResult(TeacherCompanion.Demo(snapshot, actions, message, language)));
                await messages.SaveAsync(context.User.Id(), message, new EduVerse.Core.Tutor.CompanionReply(run.Result.Text, run.Result.Actions), page, context.RequestAborted);
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

        api.MapDelete("/teacher-companion", async (HttpContext context, CompanionStore messages) =>
            Results.Ok(new { deleted = await messages.ClearAsync(context.User.Id(), context.RequestAborted) }));

        // The "nudge" action: a short teacher note (the existing, student-facing channel) to every struggling student
        // of one class, naming its hardest concept. Not an Assignment: a class is never on one of the Area/Level
        // catalog subjects (only Fractions, a teacher's own AI subject, or a trade/hobby), so that system cannot
        // target a class's own concept anyway.
        api.MapPost("/teacher-companion/practice-note", async (int classId, HttpContext context, TeacherCompanionStore store, TeacherNoteStore notes) =>
        {
            var language = Localization.LanguageForCurrentCulture();
            var snapshot = await store.SnapshotAsync(context.User.Id(), context.User.DisplayName(), "", "", language, context.RequestAborted);
            var target = snapshot.Classes.FirstOrDefault(c => c.ClassId == classId);
            if (target is not { HardestConceptName: { } concept } || target.AtRisk.Count == 0)
            {
                return Results.BadRequest();
            }

            var text = TeacherCompanion.PracticeNoteText(concept, language);
            int saved = 0, full = 0;
            foreach (var student in target.AtRisk)
            {
                switch (await notes.AddAsync(context.User.Id(), student.StudentId, text, context.RequestAborted))
                {
                    case NoteOutcome.Saved: saved++; break;
                    case NoteOutcome.Full: full++; break;
                }
            }

            return Results.Ok(new { saved, full, concept });
        })
        .RequireRateLimiting("tutor");

        return app;
    }

    private static string Clip(string? text, int max) => text is null ? "" : text.Length > max ? text[..max] : text;

    private static object ActionView(TeacherCompanionAction a) => new { id = a.Id, kind = a.Kind.ToString(), url = a.Url, label = a.Label, icon = a.Icon };
}
