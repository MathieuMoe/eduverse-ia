using EduVerse.Core.Common;
using EduVerse.Core.Curriculum;
using EduVerse.Core.Learning;
using EduVerse.Infrastructure.Engines;
using EduVerse.Infrastructure.Learning;

namespace EduVerse.Web;

public sealed record AnswerRequest(string ExerciseId, int Choice);

public sealed record ChatRequest(string Message);

/// <summary>
/// API of the student space: the learning loop (placement test → explain → practise → feedback per misconception) and the Socratic chat.
/// Decisions come from Core (planner, mastery engine, diagnostic); the AI only writes, with the offline tutor as fallback.
/// </summary>
public static class LearningEndpoints
{
    /// <summary>Exercises asked to the AI at once for a generated subject (the extra ones are queued).</summary>
    public const int AiBatch = 3;

    public static IEndpointRouteBuilder MapLearning(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api/learn").RequireAuthorization(CurrentUser.StudentPolicy);

        api.MapGet("/{subjectId}", async (string subjectId, HttpContext context, LearningStore store) =>
        {
            var (subject, _) = await LoadAsync(context, store, subjectId);
            return subject is null ? Results.NotFound() : Results.Ok(await StatePayloadAsync(context, store, subject));
        });

        api.MapPost("/{subjectId}/explain/{conceptKey}", async (string subjectId, string conceptKey, HttpContext context, LearningStore store, AiEngines engines, DemoLearningAi demo, ILogger<AnswerRequest> logger) =>
        {
            var (subject, profile) = await LoadAsync(context, store, subjectId);
            var concept = subject?.Find(conceptKey);
            if (subject is null || concept is null || profile is null)
            {
                return Results.NotFound();
            }

            var state = await store.StateAsync(context.User.Id(), subject.Key, concept.Key, context.RequestAborted);
            var student = LearningStore.Context(profile, state, Localization.LanguageForCurrentCulture());
            return await AiRunner.GuardAsync(async () =>
            {
                var run = await AiRunner.RunAsync(context, engines, logger,
                    ai => new AiLearning(ai).ExplainAsync(subject, concept, state.Method, student, context.RequestAborted),
                    () => demo.ExplainAsync(subject, concept, state.Method, student, context.RequestAborted));
                return Results.Ok(new { engine = run.Engine, fallback = run.Fallback, explanation = run.Result, methodsTried = state.MethodsTried });
            });
        })
        .RequireRateLimiting("ai");

        api.MapPost("/{subjectId}/explained/{conceptKey}", async (string subjectId, string conceptKey, HttpContext context, LearningStore store) =>
        {
            var (subject, _) = await LoadAsync(context, store, subjectId);
            if (subject?.Find(conceptKey) is null)
            {
                return Results.NotFound();
            }

            var userId = context.User.Id();
            var state = MasteryEngine.Explained(await store.StateAsync(userId, subject.Key, conceptKey, context.RequestAborted));
            await store.SaveStateAsync(userId, subject.Key, state, context.RequestAborted);
            return Results.Ok(await StatePayloadAsync(context, store, subject));
        });

        api.MapPost("/{subjectId}/exercise/{conceptKey}", async (string subjectId, string conceptKey, HttpContext context, LearningStore store, AiEngines engines, DemoLearningAi demo, ILogger<AnswerRequest> logger) =>
        {
            var (subject, profile) = await LoadAsync(context, store, subjectId);
            var concept = subject?.Find(conceptKey);
            if (subject is null || concept is null || profile is null)
            {
                return Results.NotFound();
            }

            var userId = context.User.Id();
            var state = await store.StateAsync(userId, subject.Key, concept.Key, context.RequestAborted);
            var difficulty = MasteryEngine.NextDifficulty(state);
            var queued = await store.TakeQueuedAsync(userId, subject.Key, concept.Key, difficulty, context.RequestAborted);
            if (queued is not null)
            {
                return Results.Ok(new { engine = "queue", fallback = (string?)null, exercise = Public(queued) });
            }

            return await AiRunner.GuardAsync(async () =>
            {
                var (exercises, engine, fallback) = await GenerateAsync(context, engines, demo, logger, subject, concept, difficulty,
                    LearningStore.Context(profile, state, Localization.LanguageForCurrentCulture()), subject.BuiltIn ? 1 : AiBatch);
                var first = await store.AddPendingAsync(userId, subject.Key, exercises[0], diagnostic: false, served: true, context.RequestAborted);
                foreach (var extra in exercises.Skip(1))
                {
                    await store.AddPendingAsync(userId, subject.Key, extra, diagnostic: false, served: false, context.RequestAborted);
                }

                return Results.Ok(new { engine, fallback, exercise = Public(exercises[0] with { Id = first }) });
            });
        })
        .RequireRateLimiting("learn");

        api.MapPost("/{subjectId}/diagnostic", async (string subjectId, HttpContext context, LearningStore store, AiEngines engines, DemoLearningAi demo, ILogger<AnswerRequest> logger) =>
        {
            var (subject, profile) = await LoadAsync(context, store, subjectId);
            if (subject is null || profile is null)
            {
                return Results.NotFound();
            }

            var userId = context.User.Id();
            var state = await store.DiagnosticAsync(userId, subject.Key, context.RequestAborted) ?? Diagnostic.Start(subject);
            if (state.Finished)
            {
                var start = await store.FinishDiagnosticAsync(userId, subject, state, context.RequestAborted);
                return Results.Ok(new { finished = true, start, state = await StatePayloadAsync(context, store, subject) });
            }

            await store.SaveDiagnosticAsync(userId, subject.Key, state, context.RequestAborted);
            var concept = subject.Find(Diagnostic.Next(state)!)!;
            return await AiRunner.GuardAsync(async () =>
            {
                var student = LearningStore.Context(profile, MasteryState.New(concept.Key), Localization.LanguageForCurrentCulture());
                var (exercises, engine, fallback) = await GenerateAsync(context, engines, demo, logger, subject, concept, 2, student, 1);
                var id = await store.AddPendingAsync(userId, subject.Key, exercises[0], diagnostic: true, served: true, context.RequestAborted);
                return Results.Ok(new
                {
                    finished = false,
                    engine,
                    fallback,
                    exercise = Public(exercises[0] with { Id = id }),
                    progress = new { answered = state.Answers.Count, max = Diagnostic.MaxQuestions(state.Order.Count) },
                });
            });
        })
        .RequireRateLimiting("learn");

        api.MapPost("/answer", async (AnswerRequest request, HttpContext context, LearningStore store, CertificateStore certificates, Infrastructure.Gamification.GamificationStore game, Microsoft.Extensions.Localization.IStringLocalizer<SharedResource> l) =>
        {
            var userId = context.User.Id();
            var outcome = request is null ? null : await store.AnswerAsync(userId, request.ExerciseId, request.Choice, context.RequestAborted);
            if (outcome is null)
            {
                return Results.Json(new { code = "unknown_exercise" }, statusCode: StatusCodes.Status400BadRequest);
            }

            var language = Localization.LanguageForCurrentCulture();
            var subjectKey = outcome.SubjectId;
            var subject = await store.FindSubjectAsync(subjectKey, language, context.RequestAborted);
            object? diagnostic = null;
            if (outcome.Diagnostic && subject is not null)
            {
                var state = Diagnostic.Answer(await store.DiagnosticAsync(userId, subject.Key, context.RequestAborted) ?? Diagnostic.Start(subject), outcome.Exercise.ConceptKey, outcome.Correct);
                await store.SaveDiagnosticAsync(userId, subject.Key, state, context.RequestAborted);
                diagnostic = new { finished = state.Finished, answered = state.Answers.Count, max = Diagnostic.MaxQuestions(state.Order.Count) };
            }

            var reward = await game.AnswerAsync(userId, outcome.Correct, outcome.Exercise.Difficulty, outcome.State.Streak, "tutor", outcome.Hints, false, context.RequestAborted);
            object? certificate = null;
            if (!outcome.Diagnostic && await certificates.IssueIfEarnedAsync(userId, subjectKey, context.RequestAborted) is { } issued)
            {
                reward = reward.Plus(issued.Reward);
                certificate = new { code = issued.Certificate.Code, url = "/Student/Certificate?subject=" + Uri.EscapeDataString(subjectKey) };
            }

            var events = outcome.Events.Select(e => new
            {
                kind = e.Kind.ToString(),
                e.Detail,
                text = e.Kind == LearningEventKind.RecurringMisconception && e.Detail is { } code ? Misconceptions.Describe(code, subjectKey, language) : null,
            });
            return Results.Ok(new
            {
                correct = outcome.Correct,
                correctIndex = outcome.Exercise.CorrectIndex,
                explanation = outcome.Exercise.Explanation,
                misconception = outcome.Misconception is { } m ? new { code = m, text = Misconceptions.Describe(m, subjectKey, language) } : null,
                events,
                state = outcome.Diagnostic ? null : StateView(outcome.State),
                diagnostic,
                reward = QuizEndpoints.RewardView(reward, l),
                certificate,
            });
        })
        .RequireRateLimiting("learn");

        api.MapPost("/hint", async (AnswerRequest request, HttpContext context, LearningStore store) =>
            request is not null && await store.HintAsync(context.User.Id(), request.ExerciseId, context.RequestAborted) is { } h
                ? Results.Ok(new { level = h.Level, max = 3, text = h.Text, removed = h.Removed })
                : Results.NotFound())
        .RequireRateLimiting("quiz");

        api.MapPost("/{subjectId}/reset", async (string subjectId, HttpContext context, LearningStore store) =>
        {
            var (subject, _) = await LoadAsync(context, store, subjectId);
            if (subject is null)
            {
                return Results.NotFound();
            }

            await store.ResetAsync(context.User.Id(), subject.Key, context.RequestAborted);
            return Results.Ok(await StatePayloadAsync(context, store, subject));
        });

        api.MapGet("/{subjectId}/chat/{conceptKey}", async (string subjectId, string conceptKey, HttpContext context, LearningStore store) =>
        {
            var (subject, _) = await LoadAsync(context, store, subjectId);
            if (subject?.Find(conceptKey) is null)
            {
                return Results.NotFound();
            }

            var messages = await store.ChatAsync(context.User.Id(), subject.Key, conceptKey, context.RequestAborted);
            return Results.Ok(messages.Select(m => new { fromStudent = m.FromStudent, text = m.Text }));
        });

        api.MapPost("/{subjectId}/chat/{conceptKey}", async (string subjectId, string conceptKey, ChatRequest request, HttpContext context, LearningStore store, AiEngines engines, DemoLearningAi demo, ILogger<ChatRequest> logger) =>
        {
            var (subject, profile) = await LoadAsync(context, store, subjectId);
            var concept = subject?.Find(conceptKey);
            if (subject is null || concept is null || profile is null)
            {
                return Results.NotFound();
            }

            var message = (request?.Message ?? "").Trim();
            if (message.Length == 0 || message.Length > LearningPrompts.MaxMessageLength)
            {
                return Results.Json(new { code = "message_length" }, statusCode: StatusCodes.Status400BadRequest);
            }

            var userId = context.User.Id();
            await store.AddChatAsync(userId, subject.Key, concept.Key, true, message, context.RequestAborted);
            var history = (await store.ChatAsync(userId, subject.Key, concept.Key, context.RequestAborted)).Select(m => new ChatTurn(m.FromStudent, m.Text)).ToList();
            var state = await store.StateAsync(userId, subject.Key, concept.Key, context.RequestAborted);
            var student = LearningStore.Context(profile, state, Localization.LanguageForCurrentCulture());
            return await AiRunner.GuardAsync(async () =>
            {
                var run = await AiRunner.RunAsync(context, engines, logger,
                    ai => new AiLearning(ai).ChatAsync(subject, concept, history, student, context.RequestAborted),
                    () => demo.ChatAsync(subject, concept, history, student, context.RequestAborted));
                var text = run.Result.Question is { } q ? run.Result.Reply + "\n\n" + q : run.Result.Reply;
                await store.AddChatAsync(userId, subject.Key, concept.Key, false, text, context.RequestAborted);
                return Results.Ok(new { engine = run.Engine, fallback = run.Fallback, text });
            });
        })
        .RequireRateLimiting("ai");

        return app;
    }

    private static async Task<(Subject? Subject, Infrastructure.Data.StudentProfile? Profile)> LoadAsync(HttpContext context, LearningStore store, string subjectId)
    {
        var userId = context.User.Id();
        if (!await store.CanStudyAsync(userId, subjectId, context.RequestAborted))
        {
            return (null, null);
        }

        var subject = await store.FindSubjectAsync(subjectId, Localization.LanguageForCurrentCulture(), context.RequestAborted);
        return (subject, subject is null ? null : await store.ProfileAsync(userId, context.RequestAborted));
    }

    /// <summary>Exercises for a concept: computed in code for the built-in subject, written by the AI (validated) for the others.</summary>
    private static async Task<(IReadOnlyList<Exercise> Exercises, string Engine, string? Fallback)> GenerateAsync(
        HttpContext context, AiEngines engines, DemoLearningAi demo, ILogger logger, Subject subject, Concept concept, int difficulty, StudentContext student, int count)
    {
        if (subject.BuiltIn)
        {
            return (await demo.ExercisesAsync(subject, concept, difficulty, count, student, context.RequestAborted), "code", null);
        }

        var run = await AiRunner.RunAsync(context, engines, logger,
            ai => new AiLearning(ai).ExercisesAsync(subject, concept, difficulty, count, student, context.RequestAborted),
            () => demo.ExercisesAsync(subject, concept, difficulty, count, student, context.RequestAborted));
        return (run.Result, run.Engine, run.Fallback);
    }

    // The exercise sent to the browser: no correct index, no misconception tags, no explanation (they come with the answer)
    private static object Public(Exercise e) => new { e.Id, e.ConceptKey, e.Difficulty, e.Question, e.Options, e.Hint, e.Visual, e.Code };

    private static object StateView(MasteryState s) => new
    {
        s.ConceptKey,
        pKnown = Math.Round(s.PKnown, 3),
        s.Attempts,
        s.Correct,
        s.Streak,
        status = s.Status.ToString(),
        method = s.Method.ToString(),
        s.NeedsExplanation,
        s.TeacherAlerted,
    };

    private static async Task<object> StatePayloadAsync(HttpContext context, LearningStore store, Subject subject)
    {
        var userId = context.User.Id();
        var states = await store.StatesAsync(userId, subject.Key, context.RequestAborted);
        var diagnostic = await store.DiagnosticAsync(userId, subject.Key, context.RequestAborted);
        var next = diagnostic is not null && states.Count == 0
            ? new NextStep(StepKind.Diagnostic, null, "Resume", ExplanationMethod.StepByStep, 2)
            : NextStepPlanner.Plan(subject, states, store.Now);
        var ordered = subject.Ordered();
        return new
        {
            subject = new { subject.Key, subject.Name, subject.Area, subject.BuiltIn },
            concepts = ordered.Select(c => new { c.Key, c.Name, c.Description, c.Prerequisites }),
            states = ordered.Where(c => states.ContainsKey(c.Key)).ToDictionary(c => c.Key, c => StateView(states[c.Key])),
            next = new { kind = next.Kind.ToString(), next.ConceptKey, next.Reason, method = next.Method.ToString(), next.Difficulty },
            mastered = states.Values.Count(s => s.Status == MasteryStatus.Mastered),
            trade = Trades.IsTrade(subject.Key),
            certificate = Trades.IsTrade(subject.Key) && await context.RequestServices.GetRequiredService<CertificateStore>().FindAsync(userId, subject.Key, context.RequestAborted) is { } cert
                ? new { code = cert.Code, url = "/Student/Certificate?subject=" + Uri.EscapeDataString(subject.Key) }
                : null,
            
            total = ordered.Count,
        };
    }
}
