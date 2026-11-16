using EduVerse.Core.Abstractions;
using EduVerse.Core.Learning;
using EduVerse.Infrastructure.Engines;

namespace EduVerse.Web;

/// <summary>The AI failed and the offline version cannot do this task (a subject generated from a free topic, for instance).</summary>
public sealed class AiUnavailableException : Exception
{
    public AiUnavailableException() { }

    public AiUnavailableException(string message) : base(message) { }

    public AiUnavailableException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>
/// Runs a task with the AI engine chosen in the header. If the AI fails (key, quota, model, timeout, invalid answer),
/// the offline version answers instead and the reason is returned, so the user always gets a result and knows why.
/// </summary>
public static class AiRunner
{
    public sealed record Run<T>(T Result, string Engine, string? Fallback);

    public static async Task<Run<T>> RunAsync<T>(
        HttpContext context, AiEngines engines, ILogger logger, Func<IStructuredAi, Task<T>> withAi, Func<Task<T>> offline)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(withAi);
        ArgumentNullException.ThrowIfNull(offline);
        var engine = EngineSelection.Current(context, engines);
        if (engine.CreateAi is not { } create)
        {
            return new(await offline(), "demo", null);
        }

        try
        {
            return new(await withAi(create()), engine.Name, null);
        }
        catch (Exception ex) when (ex is AiGenerationException or HttpRequestException or TaskCanceledException && !context.RequestAborted.IsCancellationRequested)
        {
            Log.AiFailed(logger, ex);
            var reason = ex switch
            {
                TaskCanceledException => "no answer in time (timeout)",
                HttpRequestException h => "could not connect: " + h.Message,
                _ => ex.Message,
            };
            var fallback = $"{engine.Name} — {reason}";
            try
            {
                return new(await offline(), "demo", fallback);
            }
            catch (NeedsAiException)
            {
                throw new AiUnavailableException(fallback, ex);
            }
        }
    }

    /// <summary>Common error answers: the task needs a real AI engine (demo selected) or the AI failed with no offline version.</summary>
    public static async Task<IResult> GuardAsync(Func<Task<IResult>> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        try
        {
            return await action();
        }
        catch (NeedsAiException)
        {
            return Results.Json(new { code = "needs_ai" }, statusCode: StatusCodes.Status409Conflict);
        }
        catch (AiUnavailableException ex)
        {
            return Results.Json(new { code = "ai_error", detail = ex.Message }, statusCode: StatusCodes.Status502BadGateway);
        }
    }
}
