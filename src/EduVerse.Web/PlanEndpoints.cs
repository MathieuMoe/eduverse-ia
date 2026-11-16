using System.Globalization;
using EduVerse.Core.Catalog;
using EduVerse.Infrastructure.Tutor;

namespace EduVerse.Web;

public sealed record GoalRequest(Area Area, Level Level, string? Title, string? ExamOn, int Minutes);

/// <summary>The goal of the student (an exam on a date). The plan itself is computed in code every time it is shown.</summary>
public static class PlanEndpoints
{
    public static IEndpointRouteBuilder MapPlan(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api/plan").RequireAuthorization(CurrentUser.StudentPolicy).RequireRateLimiting("tutor");

        api.MapPost("", async (GoalRequest? request, HttpContext context, PlanStore plans) =>
        {
            if (request is null || !Enum.IsDefined(request.Area) || !Enum.IsDefined(request.Level)
                || !DateOnly.TryParseExact(request.ExamOn, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                return Results.Json(new { code = "bad_date" }, statusCode: StatusCodes.Status400BadRequest);
            }

            var outcome = await plans.SetAsync(context.User.Id(), request.Area, request.Level, request.Title, date, request.Minutes, context.RequestAborted);
            return outcome == GoalOutcome.Saved
                ? Results.Ok(new { saved = true })
                : Results.Json(new { code = outcome switch { GoalOutcome.BadDate => "bad_date", GoalOutcome.BadTitle => "bad_title", _ => "not_available" } }, statusCode: StatusCodes.Status400BadRequest);
        });

        api.MapDelete("", async (HttpContext context, PlanStore plans) =>
        {
            await plans.ClearAsync(context.User.Id(), context.RequestAborted);
            return Results.Ok(new { removed = true });
        });
        return app;
    }
}
