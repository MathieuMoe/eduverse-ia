using EduVerse.Infrastructure.Tutor;
using Microsoft.Extensions.Localization;

namespace EduVerse.Web;

public sealed record ClaimAnswerRequest(string Id, bool True);

/// <summary>"Don't believe everything an AI tells you": a round of short claims to judge true or false, entirely
/// decided in code (see <see cref="EduVerse.Core.Tutor.PipClaims"/>) — explicitly a lesson in not trusting an AI's
/// words blindly, so nothing here is written by an AI.</summary>
public static class PipClaimsEndpoints
{
    public static IEndpointRouteBuilder MapPipClaims(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api/pipclaims").RequireAuthorization(CurrentUser.StudentPolicy).RequireRateLimiting("tutor");

        api.MapPost("/round", (HttpContext context) =>
        {
            var round = PipClaimsStore.Round(Localization.LanguageForCurrentCulture());
            return Results.Ok(new { claims = round.Select(c => new { id = c.Id, statement = c.Statement }) });
        });

        api.MapPost("/answer", async (ClaimAnswerRequest? request, HttpContext context, PipClaimsStore store, IStringLocalizer<SharedResource> l) =>
        {
            if (string.IsNullOrWhiteSpace(request?.Id))
            {
                return Results.Json(new { code = "pipclaims_answer" }, statusCode: StatusCodes.Status400BadRequest);
            }

            var outcome = await store.AnswerAsync(context.User.Id(), request.Id, request.True, Localization.LanguageForCurrentCulture(), context.RequestAborted);
            return outcome is null ? Results.Json(new { code = "no_claim" }, statusCode: StatusCodes.Status400BadRequest)
                : Results.Ok(new { correct = outcome.Correct, wasTrue = outcome.WasTrue, explanation = outcome.Explanation, reward = QuizEndpoints.RewardView(outcome.Reward, l) });
        });

        return app;
    }
}
