using EduVerse.Core.Catalog;
using EduVerse.Core.Abstractions;
using EduVerse.Core.Learning;
using EduVerse.Core.Tutor;
using EduVerse.Infrastructure.Engines;
using EduVerse.Infrastructure.Gamification;

namespace EduVerse.Web;

public sealed record HomeworkRequest(string? Steps, string? Image, bool Consent);

/// <summary>
/// "Review my steps": the student types the steps of an exercise or sends a photo of the notebook. The calculations are checked in code;
/// the AI (only with an engine chosen) reads the handwriting and explains. The photo lives in memory during the request and is never stored.
/// </summary>
public static class HomeworkEndpoints
{
    public static IEndpointRouteBuilder MapHomework(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api/homework").RequireAuthorization(CurrentUser.StudentPolicy).RequireRateLimiting("ai");

        api.MapPost("/check", async (HomeworkRequest? request, HttpContext context, GamificationStore game, AiEngines engines, ILogger<HomeworkRequest> logger) =>
        {
            if (request is null)
            {
                return Results.BadRequest();
            }

            var language = Localization.LanguageForCurrentCulture();
            var level = (await game.ProfileAsync(context.User.Id(), context.RequestAborted)).Level;
            PromptImage? photo = null;
            if (!string.IsNullOrWhiteSpace(request.Image))
            {
                if (!request.Consent)
                {
                    return Results.Json(new { code = "photo_consent" }, statusCode: StatusCodes.Status400BadRequest);
                }

                if (Levels.IsKids(level))
                {
                    return Results.Json(new { code = "photo_kids" }, statusCode: StatusCodes.Status400BadRequest);
                }

                photo = Decode(request.Image);
                if (photo is null)
                {
                    return Results.Json(new { code = "photo_bad" }, statusCode: StatusCodes.Status400BadRequest);
                }
            }

            var lines = Homework.Lines(request.Steps);
            if (photo is null && lines.Count == 0)
            {
                return Results.Json(new { code = "homework_empty" }, statusCode: StatusCodes.Status400BadRequest);
            }

            return await AiRunner.GuardAsync(async () =>
            {
                var run = await AiRunner.RunAsync(context, engines, logger,
                    async ai =>
                    {
                        var prompt = HomeworkPrompt.Build(string.Join('\n', lines), photo is not null, level, language) with { Image = photo };
                        var (transcript, line, error, feedback, hint) = HomeworkPrompt.Parse(await ai.GenerateAsync(prompt, context.RequestAborted));
                        return Homework.Merge(transcript.Count > 0 || photo is not null ? transcript : lines, line, error, feedback, hint, language);
                    },
                    () => photo is not null
                        ? throw new NeedsAiException()
                        : Task.FromResult(Homework.Offline(Homework.Check(lines), language)));
                var review = run.Result;
                var tip = review.Error is { } e ? Thinking.Tip(Thinking.RemedyFor(e), language) : null;
                return Results.Ok(new
                {
                    steps = review.Steps.Select(s => new { n = s.Number, text = s.Text, verdict = s.Verdict.ToString(), expected = s.Expected }),
                    firstWrong = review.FirstWrong,
                    error = review.Error?.ToString(),
                    label = review.Error is { } er ? Thinking.Label(er, language) : null,
                    feedback = review.Feedback,
                    hint = review.Hint,
                    tip,
                    photo = photo is not null,
                    engine = run.Engine,
                    fallback = run.Fallback,
                });
            });
        });

        return app;
    }

    /// <summary>A data URL or plain base64 of a real image (checked by its first bytes), or null.</summary>
    internal static PromptImage? Decode(string text)
    {
        var comma = text.IndexOf(',', StringComparison.Ordinal);
        var base64 = text.StartsWith("data:", StringComparison.Ordinal) && comma > 0 ? text[(comma + 1)..] : text;
        if (base64.Length > Homework.MaxImageBytes * 4 / 3 + 8)
        {
            return null;
        }

        var buffer = new byte[base64.Length * 3 / 4 + 3];
        if (!Convert.TryFromBase64String(base64, buffer, out var written))
        {
            return null;
        }

        var data = buffer.AsSpan(0, written).ToArray();
        return Homework.ImageType(data) is { } type ? new PromptImage(type, data) : null;
    }
}
