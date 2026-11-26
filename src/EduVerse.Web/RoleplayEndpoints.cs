using EduVerse.Core.Common;
using EduVerse.Core.Gamification;
using EduVerse.Infrastructure.Engines;
using EduVerse.Infrastructure.Gamification;
using Microsoft.Extensions.Localization;

namespace EduVerse.Web;

public sealed record RoleplayStartRequest(string? Scenario);

public sealed record RoleplaySayRequest(string? Text);

/// <summary>
/// Roleplays (phase 6 of the Duolingo pass): a conversation with a character. The goals are ticked in code on the
/// student's words; the AI only plays the character (offline, the character follows a script).
/// </summary>
public static class RoleplayEndpoints
{
    public static IEndpointRouteBuilder MapRoleplay(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api/roleplay").RequireAuthorization(CurrentUser.StudentPolicy).RequireRateLimiting("tutor");

        api.MapPost("/start", async (RoleplayStartRequest? request, HttpContext context, RoleplayStore store, IStringLocalizer<SharedResource> l) =>
        {
            var view = await store.StartAsync(context.User.Id(), request?.Scenario ?? "", Localization.LanguageForCurrentCulture(), context.RequestAborted);
            return view is null ? Results.BadRequest() : Results.Ok(Payload(view, Reward.None, l));
        });

        api.MapPost("/{id}/say", async (string id, RoleplaySayRequest? request, HttpContext context, RoleplayStore store, GamificationStore game, AiEngines engines, ILogger<RoleplaySayRequest> logger, IStringLocalizer<SharedResource> l) =>
        {
            var userId = context.User.Id();
            if (string.IsNullOrWhiteSpace(request?.Text))
            {
                return Results.Json(new { code = "roleplay_empty" }, statusCode: StatusCodes.Status400BadRequest);
            }

            var profile = await game.ProfileAsync(userId, context.RequestAborted);
            string? fallback = null;
            var engine = "demo";
            var result = await store.TurnAsync(userId, id, request.Text, async (during, left) =>
            {
                var run = await AiRunner.RunAsync(context, engines, logger,
                    async ai => Roleplays.Parse(await ai.GenerateAsync(Roleplays.Build(during.Roleplay, during.Speaks, profile.Level,
                        during.History.Select(h => (h.Student, h.Text)).ToList(), left), context.RequestAborted)),
                    () => Task.FromResult(Roleplays.Offline(during.Roleplay, during.Turns - 1, during.Speaks, Roleplays.Complete(during.Roleplay, during.Met))));
                (fallback, engine) = (run.Fallback, run.Engine);
                return run.Result;
            }, context.RequestAborted);
            return result is { } r
                ? Results.Ok(new { roleplay = Payload(r.View, r.Reward, l), engine, fallback })
                : Results.Json(new { code = "roleplay_closed" }, statusCode: StatusCodes.Status409Conflict);
        });

        return app;
    }

    private static string Code(TeachLanguage language) => language switch
    {
        TeachLanguage.Spanish => "es",
        TeachLanguage.French => "fr",
        TeachLanguage.Catalan => "ca",
        _ => "en",
    };

    public static object Payload(RoleplayView view, Reward reward, IStringLocalizer<SharedResource> l)
    {
        ArgumentNullException.ThrowIfNull(view);
        var language = Localization.LanguageForCurrentCulture();
        return new
        {
            id = view.Id,
            scenario = view.Roleplay.Key,
            emoji = view.Roleplay.Emoji,
            title = view.Roleplay.Title.In(language),
            setting = view.Roleplay.Setting.In(language),
            character = view.Roleplay.Character.In(language),
            speaks = Code(view.Speaks),
            history = view.History.Select(h => new { student = h.Student, text = h.Text }),
            goals = view.Roleplay.Goals.Select((g, i) => new { label = g.Label.In(language), met = view.Met.Contains(i) }),
            turnsLeft = view.TurnsLeft,
            finished = view.Finished,
            completed = view.Completed,
            reward = QuizEndpoints.RewardView(reward, l),
        };
    }
}
