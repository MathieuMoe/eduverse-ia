using EduVerse.Core.Ideas;
using EduVerse.Infrastructure.Ideas;

namespace EduVerse.Web;

public sealed record IdeaRequest(string? Text, string? Reason);

public sealed record RatingRequest(int Stars, Felt Felt, bool Fun);

/// <summary>Ideas of the students (propose, vote) and the rating at the end of an adventure.</summary>
public static class IdeaEndpoints
{
    public static IEndpointRouteBuilder MapIdeas(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api").RequireAuthorization(CurrentUser.StudentPolicy).RequireRateLimiting("ideas");

        api.MapGet("/ideas/{kind}", async (string kind, HttpContext context, IdeaStore store) =>
            Enum.TryParse<IdeaKind>(kind, true, out var k)
                ? Results.Ok(new { items = (await store.ListAsync(context.User.Id(), k, ct: context.RequestAborted)).Select(Public) })
                : Results.NotFound());

        api.MapPost("/ideas/{kind}", async (string kind, IdeaRequest request, HttpContext context, IdeaStore store) =>
        {
            if (!Enum.TryParse<IdeaKind>(kind, true, out var k) || request is null)
            {
                return Results.NotFound();
            }

            var result = await store.ProposeAsync(context.User.Id(), k, request.Text, request.Reason, Localization.LanguageForCurrentCulture(), context.RequestAborted);
            return Results.Ok(new { outcome = result.Outcome.ToString(), existing = result.Existing, item = result.Idea is null ? null : Public(result.Idea) });
        });

        api.MapPost("/ideas/vote/{id:int}", async (int id, HttpContext context, IdeaStore store) =>
            await store.VoteAsync(context.User.Id(), id, context.RequestAborted) is { } idea ? Results.Ok(Public(idea)) : Results.BadRequest());

        api.MapPost("/adventure-rating/{attemptId}", async (string attemptId, RatingRequest request, HttpContext context, IdeaStore store) =>
            request is not null && await store.RateAdventureAsync(context.User.Id(), attemptId, request.Stars, request.Felt, request.Fun, context.RequestAborted)
                ? Results.Ok(new { saved = true })
                : Results.BadRequest());

        return app;
    }

    private static object Public(IdeaView i) => new
    {
        id = i.Id,
        text = i.Text,
        status = i.Status.ToString(),
        votes = i.Votes,
        mine = i.Mine,
        voted = i.Voted,
        canVote = i.Status is IdeaStatus.Approved or IdeaStatus.InProgress,
    };
}
