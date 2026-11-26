using EduVerse.Core.Catalog;
using EduVerse.Core.Learning;
using EduVerse.Infrastructure.Engines;
using EduVerse.Infrastructure.Gamification;
using EduVerse.Infrastructure.Quizzes;
using Microsoft.Extensions.Localization;

namespace EduVerse.Web;

public sealed record QuizStartRequest(Area Area, Level Level, QuizMode Mode, string? Topic, int? DuelId, string? Figure = null);

public sealed record QuizAnswerRequest(int Choice, int? Confidence = null, int? Seconds = null);

public sealed record QuizTrueFalseRequest(bool Value, int? Confidence = null, int? Seconds = null);

public sealed record QuizFillBlankRequest(string Value, int? Confidence = null, int? Seconds = null);

public sealed record QuizReorderRequest(IReadOnlyList<string> Order, int? Confidence = null, int? Seconds = null);

public sealed record QuizMatchRequest(IReadOnlyDictionary<string, string> Pairs, int? Confidence = null, int? Seconds = null);

public sealed record QuizMultiSelectRequest(IReadOnlyList<int> Indices, int? Confidence = null, int? Seconds = null);

public sealed record QuizMatchPairRequest(string Left, string Right);

public sealed record AdventureRequest(Area Area, Level Level, Theme Theme);

/// <summary>
/// API of the tests of the catalog: start (practice, placement, duel or AI topic), current question, answer.
/// The answer key stays on the server; the AI only writes questions for free topics.
/// </summary>
public static class QuizEndpoints
{
    public static IEndpointRouteBuilder MapQuiz(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api/quiz").RequireAuthorization(CurrentUser.StudentPolicy);

        api.MapPost("/start", async (QuizStartRequest request, HttpContext context, QuizStore store, GamificationStore game, IStringLocalizer<SharedResource> l) =>
        {
            if (request is null || !Enum.IsDefined(request.Area) || !Enum.IsDefined(request.Level) || request.Mode is not (QuizMode.Practice or QuizMode.Placement or QuizMode.Duel or QuizMode.Evo or QuizMode.Notebook or QuizMode.Review or QuizMode.Interview or QuizMode.Focus))
            {
                return Results.BadRequest();
            }

            if (request.Mode == QuizMode.Focus && (string.IsNullOrWhiteSpace(request.Topic) || !Core.Curriculum.AreaSubjects.Topics(request.Area, request.Level).Contains(request.Topic)))
            {
                return Results.BadRequest();
            }

            if (request.Mode == QuizMode.Duel && await game.DuelForAsync(context.User.Id(), request.DuelId ?? 0, context.RequestAborted) is null)
            {
                return Results.Json(new { code = "duel_closed" }, statusCode: StatusCodes.Status409Conflict);
            }

            if (request.Mode == QuizMode.Interview)
            {
                var figure = Core.Tutor.Interviews.Find(request.Figure);
                if (figure is null)
                {
                    return Results.BadRequest();
                }

                var language = Localization.LanguageForCurrentCulture();
                var check = await store.StartAsync(context.User.Id(), figure.Area, request.Level, QuizMode.Interview, language,
                    pool: Core.Tutor.Interviews.Check(figure, Random.Shared.Next(), language), topic: figure.Key, engine: "bank", ct: context.RequestAborted);
                return Results.Ok(new { engine = "bank", question = QuestionPayload(check, l) });
            }

            if (request.Mode == QuizMode.Review && (await store.ReviewPlanAsync(context.User.Id(), context.RequestAborted)).Count == 0)
            {
                return Results.Json(new { code = "nothing_to_review" }, statusCode: StatusCodes.Status409Conflict);
            }

            var question = await store.StartAsync(context.User.Id(), request.Area, request.Level, request.Mode, Localization.LanguageForCurrentCulture(), request.DuelId, topic: request.Mode == QuizMode.Focus ? request.Topic : null, ct: context.RequestAborted);
            return Results.Ok(new { engine = "bank", question = QuestionPayload(question, l) });
        })
        .RequireRateLimiting("quiz");

        api.MapPost("/topic", async (QuizStartRequest request, HttpContext context, QuizStore store, AiEngines engines, ILogger<QuizStartRequest> logger, IStringLocalizer<SharedResource> l) =>
        {
            var topic = request?.Topic?.Trim() ?? "";
            if (request is null || !Enum.IsDefined(request.Area) || !Enum.IsDefined(request.Level) || topic.Length is < 2 or > TopicQuizPrompt.MaxTopicLength)
            {
                return Results.BadRequest();
            }

            var language = Localization.LanguageForCurrentCulture();
            return await AiRunner.GuardAsync(async () =>
            {
                var run = await AiRunner.RunAsync<IReadOnlyList<QuizQuestion>>(context, engines, logger,
                    async ai => TopicQuizPrompt.Parse(await ai.GenerateAsync(TopicQuizPrompt.Build(request.Area, request.Level, topic, language), context.RequestAborted), topic),
                    () => throw new NeedsAiException());
                var question = await store.StartAsync(context.User.Id(), request.Area, request.Level, QuizMode.Topic, language, pool: run.Result, topic: topic, engine: run.Engine, ct: context.RequestAborted);
                return Results.Ok(new { engine = run.Engine, fallback = run.Fallback, question = QuestionPayload(question, l) });
            });
        })
        .RequireRateLimiting("ai");

        api.MapPost("/adventure", async (AdventureRequest request, HttpContext context, QuizStore store, AiEngines engines, ILogger<AdventureRequest> logger, IStringLocalizer<SharedResource> l) =>
        {
            if (request is null || !Enum.IsDefined(request.Area) || !Enum.IsDefined(request.Level) || !Enum.IsDefined(request.Theme))
            {
                return Results.BadRequest();
            }

            var language = Localization.LanguageForCurrentCulture();
            var level = Catalog.Available(request.Area, request.Level) ? request.Level : Catalog.Info(request.Area).MinLevel;
            return await AiRunner.GuardAsync(async () =>
            {
                // The AI only tells the story (the questions come from the checked banks); offline, a story written in advance
                var run = await AiRunner.RunAsync(context, engines, logger,
                    async ai => StoryPrompt.Parse(await ai.GenerateAsync(StoryPrompt.Build(request.Theme, request.Area, level, language), context.RequestAborted)),
                    () => Task.FromResult(DemoStories.Write(request.Theme, request.Area, language)));
                var question = await store.StartAsync(context.User.Id(), request.Area, level, QuizMode.Adventure, language, engine: run.Engine,
                    story: run.Result, theme: request.Theme, ct: context.RequestAborted);
                return Results.Ok(new { engine = run.Engine, fallback = run.Fallback, question = QuestionPayload(question, l) });
            });
        })
        .RequireRateLimiting("ai");

        api.MapPost("/{attemptId}/hint", async (string attemptId, HttpContext context, QuizStore store) =>
            await store.HintAsync(context.User.Id(), attemptId, Localization.LanguageForCurrentCulture(), context.RequestAborted) is { } h
                ? Results.Ok(new { level = h.Level, max = Hints.Max, removed = h.Removed, text = h.Text })
                : Results.NotFound())
        .RequireRateLimiting("quiz");

        api.MapGet("/{attemptId}", async (string attemptId, HttpContext context, QuizStore store, IStringLocalizer<SharedResource> l) =>
            await store.CurrentAsync(context.User.Id(), attemptId, context.RequestAborted) is { } q ? Results.Ok(new { question = QuestionPayload(q, l) }) : Results.NotFound());

        api.MapPost("/{attemptId}/answer", async (string attemptId, QuizAnswerRequest request, HttpContext context, QuizStore store, IStringLocalizer<SharedResource> l) =>
        {
            var result = request is null ? null : await store.AnswerAsync(context.User.Id(), attemptId, request.Choice, request.Confidence ?? 1, request.Seconds ?? 0, Localization.LanguageForCurrentCulture(), context.RequestAborted);
            return result is null ? Results.Json(new { code = "unknown_question" }, statusCode: StatusCodes.Status400BadRequest) : Results.Ok(AnswerPayload(result, l));
        })
        .RequireRateLimiting("quiz");

        api.MapPost("/{attemptId}/truefalse", async (string attemptId, QuizTrueFalseRequest request, HttpContext context, QuizStore store, IStringLocalizer<SharedResource> l) =>
        {
            var result = request is null ? null : await store.AnswerTrueFalseAsync(context.User.Id(), attemptId, request.Value, request.Confidence ?? 1, request.Seconds ?? 0, Localization.LanguageForCurrentCulture(), context.RequestAborted);
            return result is null ? Results.Json(new { code = "unknown_question" }, statusCode: StatusCodes.Status400BadRequest) : Results.Ok(AnswerPayload(result, l));
        })
        .RequireRateLimiting("quiz");

        api.MapPost("/{attemptId}/fillblank", async (string attemptId, QuizFillBlankRequest request, HttpContext context, QuizStore store, IStringLocalizer<SharedResource> l) =>
        {
            var result = request is null || string.IsNullOrWhiteSpace(request.Value) ? null : await store.AnswerFillBlankAsync(context.User.Id(), attemptId, request.Value, request.Confidence ?? 1, request.Seconds ?? 0, Localization.LanguageForCurrentCulture(), context.RequestAborted);
            return result is null ? Results.Json(new { code = "unknown_question" }, statusCode: StatusCodes.Status400BadRequest) : Results.Ok(AnswerPayload(result, l));
        })
        .RequireRateLimiting("quiz");

        api.MapPost("/{attemptId}/reorder", async (string attemptId, QuizReorderRequest request, HttpContext context, QuizStore store, IStringLocalizer<SharedResource> l) =>
        {
            var result = request is null || request.Order is null or { Count: 0 } ? null : await store.AnswerReorderAsync(context.User.Id(), attemptId, request.Order, request.Confidence ?? 1, request.Seconds ?? 0, Localization.LanguageForCurrentCulture(), context.RequestAborted);
            return result is null ? Results.Json(new { code = "unknown_question" }, statusCode: StatusCodes.Status400BadRequest) : Results.Ok(AnswerPayload(result, l));
        })
        .RequireRateLimiting("quiz");

        api.MapPost("/{attemptId}/match", async (string attemptId, QuizMatchRequest request, HttpContext context, QuizStore store, IStringLocalizer<SharedResource> l) =>
        {
            var result = request is null || request.Pairs is null or { Count: 0 } ? null : await store.AnswerMatchAsync(context.User.Id(), attemptId, request.Pairs, request.Confidence ?? 1, request.Seconds ?? 0, Localization.LanguageForCurrentCulture(), context.RequestAborted);
            return result is null ? Results.Json(new { code = "unknown_question" }, statusCode: StatusCodes.Status400BadRequest) : Results.Ok(AnswerPayload(result, l));
        })
        .RequireRateLimiting("quiz");

        api.MapPost("/{attemptId}/multiselect", async (string attemptId, QuizMultiSelectRequest request, HttpContext context, QuizStore store, IStringLocalizer<SharedResource> l) =>
        {
            var result = request is null || request.Indices is null or { Count: 0 } ? null : await store.AnswerMultiSelectAsync(context.User.Id(), attemptId, request.Indices, request.Confidence ?? 1, request.Seconds ?? 0, Localization.LanguageForCurrentCulture(), context.RequestAborted);
            return result is null ? Results.Json(new { code = "unknown_question" }, statusCode: StatusCodes.Status400BadRequest) : Results.Ok(AnswerPayload(result, l));
        })
        .RequireRateLimiting("quiz");

        // "Listen and answer" (task #114): same request/response shape as the classic /answer endpoint — an option
        // index — since the exercise reuses the regular question generator instead of a curated bank.
        api.MapPost("/{attemptId}/listen", async (string attemptId, QuizAnswerRequest request, HttpContext context, QuizStore store, IStringLocalizer<SharedResource> l) =>
        {
            var result = request is null ? null : await store.AnswerListenAsync(context.User.Id(), attemptId, request.Choice, request.Confidence ?? 1, request.Seconds ?? 0, Localization.LanguageForCurrentCulture(), context.RequestAborted);
            return result is null ? Results.Json(new { code = "unknown_question" }, statusCode: StatusCodes.Status400BadRequest) : Results.Ok(AnswerPayload(result, l));
        })
        .RequireRateLimiting("quiz");

        // Live per-pair feedback for the Match drill (Duolingo-style green/red border as the student taps), read-only:
        // doesn't score, finish or advance the attempt — the /match call above still does that once every pair is right.
        api.MapPost("/{attemptId}/match/pair", async (string attemptId, QuizMatchPairRequest request, HttpContext context, QuizStore store) =>
        {
            if (request is null || string.IsNullOrEmpty(request.Left) || string.IsNullOrEmpty(request.Right))
            {
                return Results.BadRequest();
            }

            var correct = await store.CheckMatchPairAsync(context.User.Id(), attemptId, request.Left, request.Right, context.RequestAborted);
            return correct is null ? Results.Json(new { code = "unknown_question" }, statusCode: StatusCodes.Status400BadRequest) : Results.Ok(new { correct = correct.Value });
        })
        .RequireRateLimiting("quiz");

        return app;
    }

    private static object AnswerPayload(AnswerResult result, IStringLocalizer<SharedResource> l) => new
    {
        correct = result.Correct,
        correctIndex = result.CorrectIndex,
        hints = result.Hints,
        rungs = result.Rungs,
        confidence = result.Confidence,
        pace = result.Pace == EduVerse.Core.Tutor.PaceSignal.None ? null : new { kind = result.Pace.ToString(), text = EduVerse.Core.Tutor.Pace.Advice(result.Pace, Localization.LanguageForCurrentCulture()) },
        explanation = result.Explanation,
        run = result.Run,
        next = result.Next is { } n ? QuestionPayload(n, l) : null,
        summary = result.Summary is { } s ? new
        {
            s.Correct,
            s.Length,
            s.Stars,
            s.Xp,
            placed = s.Placed is { } p ? new { level = p.ToString(), name = l[$"Level_{p}"].Value } : null,
            topics = s.Topics.Select(t => new { name = TopicName(t.Topic, l), t.Correct, t.Total }),
            duel = s.DuelOutcome,
            won = s.Won,
            hearts = s.Hearts,
            ending = s.Ending,
        } : null,
        reward = RewardView(result.Reward, l),
    };

    public static string TopicName(string topic, IStringLocalizer l)
    {
        ArgumentNullException.ThrowIfNull(topic);
        ArgumentNullException.ThrowIfNull(l);
        if (topic.StartsWith("topic:", StringComparison.Ordinal))
        {
            return topic[6..];
        }

        if (topic.StartsWith("interview:", StringComparison.Ordinal))
        {
            return Core.Tutor.Interviews.Find(topic[10..])?.Name ?? topic;
        }

        var name = l[$"Topic_{topic.Replace('.', '_')}"];
        return name.ResourceNotFound ? topic : name.Value;
    }

    public static object QuestionPayload(QuestionView q, IStringLocalizer l) => new
    {
        id = q.AttemptId,
        number = q.Number,
        length = q.Length,
        prompt = q.Prompt,
        options = q.Options,
        visual = q.Visual,
        code = q.Code,
        difficulty = q.Difficulty,
        topic = TopicName(q.Topic, l),
        level = q.Level.ToString(),
        levelName = l[$"Level_{q.Level}"].Value,
        area = q.Area.ToString(),
        areaName = l[$"Area_{q.Area}"].Value,
        mode = q.Mode.ToString(),
        kids = q.Kids,
        exerciseKind = q.ExerciseKind?.ToString(),
        tokens = q.Tokens,
        matchLeft = q.MatchLeft,
        matchRight = q.MatchRight,
        evo = q.Evo,
        evoTask = q.EvoTask,
        hintsUsed = q.HintsUsed,
        adventure = q.Adventure is { } a ? new
        {
            theme = a.Theme.ToString(),
            emoji = Adventure.Emoji(a.Theme),
            title = a.Title,
            intro = a.Intro,
            line = a.Line,
            passed = a.Passed,
            doors = a.Doors,
            hearts = a.Hearts,
            maxHearts = Adventure.Hearts,
            bossHp = a.BossHp,
            bossMax = a.BossMax,
            boss = a.Boss,
            inBoss = a.InBoss,
        } : null,
    };

    public static object RewardView(Reward reward, IStringLocalizer l)
    {
        ArgumentNullException.ThrowIfNull(reward);
        ArgumentNullException.ThrowIfNull(l);
        return new
        {
            xp = reward.Xp,
            badges = reward.Badges.Select(b => new { key = b, icon = Core.Gamification.Badges.All.First(x => x.Key == b).Icon, name = l[$"Badge_{b}"].Value }),
            quests = reward.Quests.Select(q => new { key = q, name = l[$"Quest_{q}"].Value }),
        };
    }
}
