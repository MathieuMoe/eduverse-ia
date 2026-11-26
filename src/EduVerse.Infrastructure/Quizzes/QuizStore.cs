using System.Text.Json;
using EduVerse.Core.Catalog;
using EduVerse.Core.Tutor;
using EduVerse.Core.Common;
using EduVerse.Core.Gamification;
using EduVerse.Core.Learning;
using EduVerse.Infrastructure.Data;
using EduVerse.Infrastructure.Gamification;
using Microsoft.EntityFrameworkCore;

namespace EduVerse.Infrastructure.Quizzes;

/// <summary>A question as the student sees it: no answer key.</summary>
public sealed record QuestionView(string AttemptId, int Number, int Length, string Prompt, IReadOnlyList<string> Options, string? Visual, string? Code,
    int Difficulty, string Topic, Level Level, Area Area, QuizMode Mode, bool Kids, string? Evo = null, string? EvoTask = null, int HintsUsed = 0)
{
    public AdventureView? Adventure { get; init; }

    /// <summary>Null for a classic multiple-choice question; set when this is a Duolingo-style drill (true/false, etc.) instead.</summary>
    public ExerciseKind? ExerciseKind { get; init; }

    /// <summary>Reorder drill only: the words to tap, shown already shuffled.</summary>
    public IReadOnlyList<string>? Tokens { get; init; }

    /// <summary>Match drill only: the left-column items, in a stable order.</summary>
    public IReadOnlyList<string>? MatchLeft { get; init; }

    /// <summary>Match drill only: the right-column items, shuffled per view (the student answers by pairing text, not position).</summary>
    public IReadOnlyList<string>? MatchRight { get; init; }
}

/// <summary>What the adventure screen shows with a question: the map, the hearts, the boss and the line of the story.</summary>
public sealed record AdventureView(Theme Theme, string Title, string? Intro, string Line, int Passed, int Doors, int Hearts, int BossHp, int BossMax, string Boss, bool InBoss);

/// <summary>A topic to review today.</summary>
public sealed record ReviewItem(Area Area, Level Level, string Topic);

public sealed record TopicResult(string Topic, int Correct, int Total);

public sealed record QuizSummary(int Correct, int Length, int Stars, int Xp, Level? Placed, IReadOnlyList<TopicResult> Topics, int? DuelOutcome)
{
    /// <summary>Adventure: won or not, hearts left and the ending of the story.</summary>
    public bool? Won { get; init; }

    public int Hearts { get; init; }

    public string? Ending { get; init; }
}

public sealed record AnswerResult(bool Correct, int CorrectIndex, string Explanation, int Xp, int Run, QuestionView? Next, QuizSummary? Summary, Reward Reward)
{
    public int Hints { get; init; }

    public int Confidence { get; init; } = 1;

    public int Rungs { get; init; }

    /// <summary>What the rhythm of the test says after this answer (advice only).</summary>
    public EduVerse.Core.Tutor.PaceSignal Pace { get; init; }
}

/// <summary>Where the student is on the ladder of the current question (the question stays on the server).</summary>
public sealed record LadderState(int Rung, bool Done, QuizQuestion Question, Level Level);

public sealed record SubjectProgress(Area Area, Level Level, bool Placed, int Answers, int Accuracy, IReadOnlyList<TopicSkill> Weak, int Tests = 0);

/// <summary>
/// Tests of the catalog: adaptive practice, placement, duels and AI topics. Questions are generated on the server and
/// the answer key never leaves it; knowledge per topic is tracked with Bayesian Knowledge Tracing.
/// </summary>
public sealed class QuizStore(EduVerseDb db, GamificationStore game, TimeProvider clock)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly int[] AllDifficulties = [1, 2, 3];

    public DateTimeOffset Now => clock.GetUtcNow();

    public async Task<QuestionView> StartAsync(string userId, Area area, Level level, QuizMode mode, TeachLanguage language,
        int? duelId = null, IReadOnlyList<QuizQuestion>? pool = null, string? topic = null, string engine = "demo",
        Story? story = null, Theme theme = Theme.Space, CancellationToken ct = default)
    {
        if (!Catalog.Available(area, level))
        {
            level = Catalog.Info(area).MinLevel;
        }

        var attempt = new QuizAttempt
        {
            Id = Guid.NewGuid().ToString("N"), UserId = userId, Area = area, Level = level, Mode = mode, Topic = topic, Engine = engine,
            Seed = Random.Shared.Next(1, int.MaxValue), Difficulty = AdaptiveQuiz.StartDifficulty, Length = AdaptiveQuiz.LengthOf(mode), StartedAt = Now,
        };
        if (mode == QuizMode.Duel)
        {
            var duel = await game.DuelForAsync(userId, duelId ?? 0, ct).ConfigureAwait(false) ?? throw new InvalidOperationException("duel");
            (attempt.Area, attempt.Level, attempt.Seed, attempt.DuelId) = (duel.Area, duel.Level, duel.Seed, duel.Id);
        }
        else if (mode == QuizMode.Placement)
        {
            var state = Placement.Start(area);
            attempt.PlacementJson = JsonSerializer.Serialize(state, JsonOptions);
            attempt.Level = Placement.LevelToAsk(state);
        }
        else if (mode is QuizMode.Topic or QuizMode.Interview)
        {
            ArgumentNullException.ThrowIfNull(pool);
            attempt.PoolJson = JsonSerializer.Serialize(pool, JsonOptions);
            attempt.Length = Math.Min(AdaptiveQuiz.Length, pool.Count);
        }
        else if (mode == QuizMode.Review)
        {
            var plan = await ReviewPlanAsync(userId, ct).ConfigureAwait(false);
            if (plan.Count == 0)
            {
                throw new InvalidOperationException("nothing_to_review");
            }

            attempt.PlanJson = JsonSerializer.Serialize(plan, JsonOptions);
            attempt.Length = plan.Count;
            (attempt.Area, attempt.Level) = (plan[0].Area, plan[0].Level);
        }
        else if (mode == QuizMode.Adventure)
        {
            ArgumentNullException.ThrowIfNull(story);
            attempt.AdventureJson = JsonSerializer.Serialize(Adventure.Start(theme), JsonOptions);
            attempt.StoryJson = JsonSerializer.Serialize(story, JsonOptions);
            // The boss asks the weakest topics of the student in this subject and level
            var weak = await db.TopicSkills.Where(t => t.UserId == userId && t.Area == attempt.Area && t.Level == attempt.Level && t.Attempts > 0)
                .OrderBy(t => t.PKnown).Select(t => t.Topic).Take(Adventure.BossHp).ToListAsync(ct).ConfigureAwait(false);
            attempt.PlanJson = JsonSerializer.Serialize(weak, JsonOptions);
        }
        else if (mode == QuizMode.Notebook)
        {
            (attempt.Area, attempt.Level) = (Area.Mathematics, Level.Primary3);
            attempt.PlanJson = JsonSerializer.Serialize(await MistakesAsync(userId, ct).ConfigureAwait(false), JsonOptions);
        }

        if (mode is QuizMode.Practice or QuizMode.Placement or QuizMode.Evo or QuizMode.Adventure)
        {
            // A new test never starts with questions the student has already seen (as long as the bank has new ones)
            attempt.UsedJson = JsonSerializer.Serialize(await SeenAsync(userId, attempt.Area, mode == QuizMode.Placement ? null : attempt.Level, ct).ConfigureAwait(false), JsonOptions);
        }

        var question = NextQuestion(attempt, language, await InterestsOfAsync(userId, ct).ConfigureAwait(false));
        attempt.CurrentJson = JsonSerializer.Serialize(question, JsonOptions);
        db.QuizAttempts.Add(attempt);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return View(attempt, question);
    }

    public async Task<QuestionView?> CurrentAsync(string userId, string attemptId, CancellationToken ct = default)
    {
        var attempt = await db.QuizAttempts.FirstOrDefaultAsync(a => a.Id == attemptId && a.UserId == userId, ct).ConfigureAwait(false);
        if (attempt is null || attempt.FinishedAt is not null)
        {
            return null;
        }

        return attempt.CurrentExerciseJson is not null ? ViewOfExercise(attempt, CurrentExercise(attempt)) : View(attempt, Current(attempt));
    }

    /// <summary>Same question whatever the order of its options.</summary>
    public static string KeyOf(QuizQuestion q)
    {
        ArgumentNullException.ThrowIfNull(q);
        return q.Prompt + "|" + string.Join("|", q.Options.Order(StringComparer.Ordinal));
    }

    /// <summary>Questions this student already answered in this subject and level (most recent first).</summary>
    private async Task<List<string>> SeenAsync(string userId, Area area, Level? level, CancellationToken ct) =>
        await db.QuizAnswers.Where(a => a.UserId == userId && a.Area == area && (level == null || a.Level == level) && a.Key != "")
            .OrderByDescending(a => a.Id).Select(a => a.Key).Take(MemoryOfQuestions).ToListAsync(ct).ConfigureAwait(false);

    /// <summary>How many past questions a new test tries not to repeat.</summary>
    public const int MemoryOfQuestions = 300;

    private static QuizQuestion Current(QuizAttempt attempt) => JsonSerializer.Deserialize<QuizQuestion>(attempt.CurrentJson, JsonOptions)!;

    private static PracticeExercise CurrentExercise(QuizAttempt attempt) => JsonSerializer.Deserialize<PracticeExercise>(attempt.CurrentExerciseJson!, JsonOptions)!;

    private static QuestionView View(QuizAttempt a, QuizQuestion q) =>
        new(a.Id, a.Answered + 1, a.Length, q.Prompt, q.Options, q.Visual, q.Code, q.Difficulty, q.Topic, a.Level, a.Area, a.Mode, Levels.IsKids(a.Level), q.Evo, q.EvoTask, a.CurrentHints)
        {
            Adventure = AdventureOf(a),
        };

    /// <summary>The view of a Duolingo-style drill: usually no options to list (the kind tells the client how to render
    /// it), no hints or evo. MultiSelect and Listen are the exceptions — both reuse the classic Options list (MultiSelect
    /// to offer several checkboxes, Listen to offer the same 4 buttons a classic question would, just spoken instead of
    /// shown), so both need it sent through instead of the usual empty array.
    /// Carries the Adventure map/hearts/story line exactly like a classic question's view does (task #74), so a drill
    /// landing mid-adventure doesn't make that chrome disappear for one question.</summary>
    private static QuestionView ViewOfExercise(QuizAttempt a, PracticeExercise e) =>
        new(a.Id, a.Answered + 1, a.Length, e.Prompt, e.Kind is ExerciseKind.MultiSelect or ExerciseKind.Listen ? e.Options! : [], e.Visual, null, e.Difficulty, e.Topic, a.Level, a.Area, a.Mode, Levels.IsKids(a.Level))
        {
            ExerciseKind = e.Kind,
            Tokens = e.Kind == ExerciseKind.Reorder ? e.Tokens : null,
            MatchLeft = e.Kind == ExerciseKind.Match ? e.Pairs!.Select(p => p.Left).ToList() : null,
            MatchRight = e.Kind == ExerciseKind.Match ? e.Pairs!.Select(p => p.Right).OrderBy(_ => Random.Shared.Next()).ToList() : null,
            Adventure = AdventureOf(a),
        };

    /// <summary>The key a drill is remembered under in <see cref="QuizAttempt.UsedJson"/> — the same list the classic
    /// path already stores question keys in (task #95), so one no-repeat mechanism covers both. Distinct in shape
    /// from a classic key (a full prompt/options string), so a collision between the two is not a practical concern.</summary>
    private static string KeyOf(PracticeExercise e) => $"{e.Kind}|{e.Topic}";

    /// <summary>The modes a Duolingo-style drill can appear in: Practice (task #71–73), Adventure, Duel (Arena) and
    /// Placement (task #74) — every 3rd answered item (task #95 tightened this from every 4th: Mathieu's
    /// "visiblemente no aparece mucho" was partly a raw-frequency problem), when the bank has content for this
    /// subject and level. Review and Focus were considered too (task #95) but dropped: both pin every question to
    /// one specific topic (the item being reviewed, or the plan's chosen study topic) and a drill from
    /// <see cref="ExerciseBank.AnyDrill"/> can land on any topic in the subject, so inserting one there would
    /// silently break out of the single-topic session those modes exist for. True/false is excluded from Placement
    /// specifically: its 50/50 guess rate would add noise to the binary-search level calibration (2 decisive
    /// questions per level), where the other kinds (free-text/reorder/match/multi-select, each harder to guess right
    /// than even a classic 4-option question) don't carry that risk. The residue (0 mod 3, not 2) is chosen to miss
    /// Practice's own interest-word-problem swap, which already claims Answered % 3 == 2 just below (the "due" check
    /// in <see cref="NextQuestion"/>) — landing on the same residue would mean a drill always wins that slot and the
    /// swap would never fire.
    /// Evo/Topic/Interview/Notebook are deliberately left out — a drill has no Evo-specific adaptation, and no
    /// meaning for a free-topic, fixed-pool or interview-script set.</summary>
    private static PracticeExercise? TryExerciseFor(QuizAttempt attempt, TeachLanguage language)
    {
        if (attempt.Mode is not (QuizMode.Practice or QuizMode.Adventure or QuizMode.Duel or QuizMode.Placement) || attempt.Answered % 3 != 0)
        {
            return null;
        }

        var seed = QuestionBanks.Seed(attempt.Seed, attempt.Answered, 5);
        var exclude = JsonSerializer.Deserialize<List<string>>(attempt.UsedJson, JsonOptions) is { Count: > 0 } used ? used.ToHashSet() : null;
        var exercise = ExerciseBank.AnyDrill(attempt.Area, attempt.Level, language, seed, allowTrueFalse: attempt.Mode != QuizMode.Placement, exclude: exclude);
        if (exercise is not null && attempt.Mode == QuizMode.Adventure && JsonSerializer.Deserialize<AdventureState>(attempt.AdventureJson!, JsonOptions)!.InBoss)
        {
            // Boss fights force the hardest difficulty for classic questions too (see NextQuestion); keep a drill
            // landing on a boss turn consistent, so skill tracking and the XP reward treat it the same way.
            exercise = exercise with { Difficulty = 3 };
        }

        if (exercise is not null)
        {
            // Remembered right away, not only once answered: a drill the student never finishes (navigates away,
            // or it's the attempt's last item) still shouldn't come back up again the moment they resume.
            var updated = exclude?.ToList() ?? [];
            updated.Add(KeyOf(exercise));
            attempt.UsedJson = JsonSerializer.Serialize(updated, JsonOptions);
        }

        return exercise;
    }

    /// <summary>Applies the mode-specific progression for one answered item — Adventure's door/boss state, Placement's
    /// binary-search level state, or Practice/Evo/Topic's adaptive difficulty — and returns whether the attempt is now
    /// finished. Shared by the classic answer path and every drill-answer path (task #74), so a drill answered during
    /// Adventure or Placement advances that mode's own state exactly like a classic answer does, instead of silently
    /// leaving it frozen.</summary>
    private static bool ApplyModeProgression(QuizAttempt attempt, bool correct)
    {
        var finished = attempt.Answered >= attempt.Length;
        if (attempt.Mode == QuizMode.Adventure)
        {
            var state = Adventure.Answer(JsonSerializer.Deserialize<AdventureState>(attempt.AdventureJson!, JsonOptions)!, correct);
            attempt.AdventureJson = JsonSerializer.Serialize(state, JsonOptions);
            finished = state.Finished;
            attempt.Won = state.Won;
            attempt.Length = finished ? attempt.Answered : Math.Max(attempt.Length, attempt.Answered + (Adventure.Doors - state.Passed) + state.BossHp);
            if (!finished && !state.InBoss)
            {
                attempt.Difficulty = AdaptiveQuiz.NextDifficulty(attempt.Difficulty, correct, attempt.Run);
            }
        }
        else if (attempt.Mode == QuizMode.Placement)
        {
            var state = Placement.Answer(JsonSerializer.Deserialize<PlacementState>(attempt.PlacementJson!, JsonOptions)!, correct);
            attempt.PlacementJson = JsonSerializer.Serialize(state, JsonOptions);
            finished = state.Finished;
            if (!finished)
            {
                attempt.Level = Placement.LevelToAsk(state);
            }
            else
            {
                attempt.Result = Placement.Result(state, attempt.Area);
                attempt.Length = attempt.Answered;
            }
        }
        else if (attempt.Mode is QuizMode.Practice or QuizMode.Evo or QuizMode.Topic)
        {
            attempt.Difficulty = AdaptiveQuiz.NextDifficulty(attempt.Difficulty, correct, attempt.Run);
        }

        return finished;
    }

    private static AdventureView? AdventureOf(QuizAttempt a)
    {
        if (a.AdventureJson is null || a.StoryJson is null)
        {
            return null;
        }

        var state = JsonSerializer.Deserialize<AdventureState>(a.AdventureJson, JsonOptions)!;
        var story = JsonSerializer.Deserialize<Story>(a.StoryJson, JsonOptions)!;
        var line = state.InBoss ? story.BossIntro : story.Doors[Math.Min(state.Passed, story.Doors.Count - 1)];
        return new AdventureView(state.Theme, story.Title, a.Answered == 0 ? story.Intro : null, line, state.Passed, Adventure.Doors, state.Hearts,
            state.BossHp, Adventure.BossHp, story.Boss, state.InBoss);
    }

    /// <summary>A new question, different from the ones already asked in this test (the same sequence for both sides of a duel).</summary>
    private async Task<IReadOnlyList<string>> InterestsOfAsync(string userId, CancellationToken ct) =>
        Interests.Parse(await db.StudentProfiles.Where(p => p.UserId == userId).Select(p => p.Interests).FirstOrDefaultAsync(ct).ConfigureAwait(false));

    /// <summary>Every few questions of primary maths, a word problem about what the student likes (the numbers are computed in code).</summary>
    private static QuizQuestion WithInterest(QuizAttempt attempt, QuizQuestion question, IReadOnlyList<string> interests, TeachLanguage language, List<string> used)
    {
        var due = attempt.Mode == QuizMode.Focus ? attempt.Answered % 2 == 1 : attempt.Mode == QuizMode.Practice && attempt.Answered % 3 == 2;
        if (!due || interests.Count == 0 || !InterestProblems.Supports(attempt.Area, attempt.Level, attempt.Mode == QuizMode.Focus ? attempt.Topic : null))
        {
            return question;
        }

        var interest = Interests.Find(interests[attempt.Answered % interests.Count])!;
        var topic = attempt.Mode == QuizMode.Focus ? attempt.Topic : null;
        var swapped = question;
        for (var retry = 0; retry < 20; retry++)
        {
            swapped = InterestProblems.Generate(interest, attempt.Level, topic, attempt.Difficulty, QuestionBanks.Seed(attempt.Seed + retry * 101, attempt.Answered, 7), language);
            if (!used.Contains(KeyOf(swapped)))
            {
                break;
            }
        }

        return topic is not null && swapped.Topic != topic ? question : swapped;
    }

    private static QuizQuestion NextQuestion(QuizAttempt attempt, TeachLanguage language, IReadOnlyList<string> interests)
    {
        var used = JsonSerializer.Deserialize<List<string>>(attempt.UsedJson, JsonOptions) ?? [];
        QuizQuestion question;
        if (attempt.Mode is QuizMode.Topic or QuizMode.Interview)
        {
            var pool = JsonSerializer.Deserialize<List<QuizQuestion>>(attempt.PoolJson!, JsonOptions)!;
            var index = TopicQuizPrompt.Pick(pool, used.Select(int.Parse).ToList(), attempt.Difficulty);
            question = pool[Math.Max(0, index)];
            used.Add(Math.Max(0, index).ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
        else if (attempt.Mode == QuizMode.Notebook)
        {
            var mistakes = JsonSerializer.Deserialize<List<string>>(attempt.PlanJson ?? "[]", JsonOptions) ?? [];
            question = EvoNotebook.Create(mistakes, new Random(QuestionBanks.Seed(attempt.Seed, attempt.Answered, 1)), language);
            for (var retry = 1; retry < 10 && used.Contains(KeyOf(question)); retry++)
            {
                question = EvoNotebook.Create(mistakes, new Random(QuestionBanks.Seed(attempt.Seed + retry * 101, attempt.Answered, 1)), language);
            }

            used.Add(KeyOf(question));
        }
        else if (attempt.Mode == QuizMode.Adventure && JsonSerializer.Deserialize<AdventureState>(attempt.AdventureJson!, JsonOptions)!.InBoss)
        {
            // Boss: the hardest difficulty, on the weakest topics first
            var weak = JsonSerializer.Deserialize<List<string>>(attempt.PlanJson ?? "[]", JsonOptions) ?? [];
            var seed = QuestionBanks.Seed(attempt.Seed, attempt.Answered, 3);
            question = weak.Count > 0
                ? QuestionBanks.ForTopic(attempt.Area, attempt.Level, weak[attempt.Answered % weak.Count], 3, seed, language)
                : QuestionBanks.Generate(attempt.Area, attempt.Level, 3, seed, language);
            for (var retry = 1; retry < 20 && used.Contains(KeyOf(question)); retry++)
            {
                question = QuestionBanks.Generate(attempt.Area, attempt.Level, 3, QuestionBanks.Seed(attempt.Seed + retry * 101, attempt.Answered, 3), language);
            }

            used.Add(KeyOf(question));
        }
        else if (attempt.Mode == QuizMode.Focus)
        {
            // Study session of the plan: one topic, adaptive difficulty, no repeated question
            var seed = QuestionBanks.Seed(attempt.Seed, attempt.Answered, attempt.Difficulty);
            question = QuestionBanks.ForTopic(attempt.Area, attempt.Level, attempt.Topic ?? "", attempt.Difficulty, seed, language);
            for (var retry = 1; retry < 20 && used.Contains(KeyOf(question)); retry++)
            {
                question = QuestionBanks.ForTopic(attempt.Area, attempt.Level, attempt.Topic ?? "", attempt.Difficulty, QuestionBanks.Seed(attempt.Seed + retry * 101, attempt.Answered, attempt.Difficulty), language);
            }

            question = WithInterest(attempt, question, interests, language, used);
            used.Add(KeyOf(question));
        }
        else if (attempt.Mode == QuizMode.Review)
        {
            var plan = JsonSerializer.Deserialize<List<ReviewItem>>(attempt.PlanJson!, JsonOptions)!;
            var item = plan[Math.Min(attempt.Answered, plan.Count - 1)];
            (attempt.Area, attempt.Level) = (item.Area, item.Level);
            question = QuestionBanks.ForTopic(item.Area, item.Level, item.Topic, AdaptiveQuiz.StartDifficulty, QuestionBanks.Seed(attempt.Seed, attempt.Answered, 2), language);
            used.Add(KeyOf(question));
        }
        else
        {
            var difficulty = attempt.Mode is QuizMode.Duel or QuizMode.Placement ? AdaptiveQuiz.StartDifficulty : attempt.Difficulty;
            question = QuestionBanks.Generate(attempt.Area, attempt.Level, difficulty, QuestionBanks.Seed(attempt.Seed, attempt.Answered, difficulty), language);
            var tries = attempt.Mode == QuizMode.Duel ? 8 : 40;
            for (var retry = 1; retry < tries && used.Contains(KeyOf(question)); retry++)
            {
                question = QuestionBanks.Generate(attempt.Area, attempt.Level, difficulty, QuestionBanks.Seed(attempt.Seed + retry * 101, attempt.Answered, difficulty), language);
            }

            // Some subjects (fixed-fact banks like History or Geography, as opposed to Maths' procedurally
            // generated numbers) only have a handful of distinct questions per difficulty tier, and a student's last
            // 300 answered questions (MemoryOfQuestions) are already excluded — so the loop above can exhaust that
            // tier's whole pool and still be stuck on a duplicate. Before accepting one, widen to the other two
            // difficulties: their pools are largely disjoint, so this often turns up something genuinely unseen
            // without needing more authored content. Keeps the requested difficulty if nothing else is free.
            if (used.Contains(KeyOf(question)))
            {
                foreach (var otherDifficulty in AllDifficulties.Where(d => d != difficulty))
                {
                    var alt = question;
                    for (var retry = 0; retry < 15 && used.Contains(KeyOf(alt)); retry++)
                    {
                        alt = QuestionBanks.Generate(attempt.Area, attempt.Level, otherDifficulty, QuestionBanks.Seed(attempt.Seed + retry * 101, attempt.Answered, otherDifficulty), language);
                    }

                    if (!used.Contains(KeyOf(alt)))
                    {
                        question = alt;
                        break;
                    }
                }
            }

            question = WithInterest(attempt, question, interests, language, used);
            used.Add(KeyOf(question));
            if (attempt.Mode == QuizMode.Evo)
            {
                question = EvoQuestions.Check(question, new Random(QuestionBanks.Seed(attempt.Seed, attempt.Answered, 3)), language);
            }
        }

        attempt.UsedJson = JsonSerializer.Serialize(used, JsonOptions);
        return question;
    }

    public async Task<AnswerResult?> AnswerAsync(string userId, string attemptId, int choice, TeachLanguage language, CancellationToken ct = default) =>
        await AnswerAsync(userId, attemptId, choice, 1, language, ct).ConfigureAwait(false);

    /// <summary>Records an answer. <paramref name="confidence"/>: 0 not sure, 1 I think so, 2 sure (said before answering).</summary>
    public async Task<AnswerResult?> AnswerAsync(string userId, string attemptId, int choice, int confidence, TeachLanguage language, CancellationToken ct = default) =>
        await AnswerAsync(userId, attemptId, choice, confidence, 0, language, ct).ConfigureAwait(false);

    /// <summary><paramref name="seconds"/>: time the question was on screen, as measured by the page (0 = unknown); only used for rhythm advice.</summary>
    public async Task<AnswerResult?> AnswerAsync(string userId, string attemptId, int choice, int confidence, int seconds, TeachLanguage language, CancellationToken ct = default)
    {
        var attempt = await db.QuizAttempts.FirstOrDefaultAsync(a => a.Id == attemptId && a.UserId == userId, ct).ConfigureAwait(false);
        if (attempt is null || attempt.FinishedAt is not null || attempt.CurrentExerciseJson is not null || choice is < 0 or >= QuizQuestion.Choices)
        {
            return null;
        }

        var question = Current(attempt);
        var correct = choice == question.CorrectIndex;
        var hints = attempt.CurrentHints;
        var rungs = attempt.CurrentRungs;
        confidence = Math.Clamp(confidence, 0, Spaced.Sure);
        attempt.CurrentHints = 0;
        attempt.CurrentRungs = 0;
        attempt.Answered++;
        attempt.Correct += correct ? 1 : 0;
        attempt.Run = correct ? attempt.Run + 1 : 0;
        attempt.BestRun = Math.Max(attempt.BestRun, attempt.Run);
        db.QuizAnswers.Add(new QuizAnswer
        {
            AttemptId = attempt.Id, UserId = userId, Area = attempt.Area, Level = attempt.Level, Topic = question.Topic,
            Difficulty = question.Difficulty, Correct = correct, At = Now, Number = attempt.Answered, Key = KeyOf(question),
            Prompt = question.Prompt, OptionsJson = JsonSerializer.Serialize(question.Options, JsonOptions), Choice = choice,
            CorrectIndex = question.CorrectIndex, Explanation = question.Explanation, Visual = question.Visual, Code = question.Code,
            Evo = question.Evo, TargetMisconception = question.TargetMisconception, Hints = hints, Confidence = confidence, Rungs = rungs, Seconds = EduVerse.Core.Tutor.Pace.Clean(seconds),
        });
        await UpdateSkillAsync(userId, attempt, question, correct, confidence, ct).ConfigureAwait(false);

        var finished = ApplyModeProgression(attempt, correct);

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        var reward = await game.AnswerAsync(userId, correct, question.Difficulty, attempt.Run, "answer", Math.Min(Hints.Max, hints + Ladder.HintCost(rungs)), confidence == Spaced.Sure, ct).ConfigureAwait(false);
        attempt.Xp += reward.Xp;

        QuestionView? next = null;
        QuizSummary? summary = null;
        if (finished)
        {
            (summary, var finishReward) = await FinishAsync(attempt, ct).ConfigureAwait(false);
            reward = reward.Plus(finishReward);
        }
        else
        {
            var exercise = TryExerciseFor(attempt, language);
            if (exercise is not null)
            {
                attempt.CurrentExerciseJson = JsonSerializer.Serialize(exercise, JsonOptions);
                attempt.CurrentJson = "";
                next = ViewOfExercise(attempt, exercise);
            }
            else
            {
                var nextQuestion = NextQuestion(attempt, language, await InterestsOfAsync(userId, ct).ConfigureAwait(false));
                attempt.CurrentExerciseJson = null;
                attempt.CurrentJson = JsonSerializer.Serialize(nextQuestion, JsonOptions);
                next = View(attempt, nextQuestion);
            }
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        var pace = EduVerse.Core.Tutor.PaceSignal.None;
        if (!finished && attempt.Mode is not (QuizMode.Placement or QuizMode.Duel))
        {
            var points = await db.QuizAnswers.AsNoTracking().Where(a => a.AttemptId == attempt.Id).OrderBy(a => a.Number).Select(a => new { a.Seconds, a.Correct }).ToListAsync(ct).ConfigureAwait(false);
            pace = EduVerse.Core.Tutor.Pace.Read(points.Select(a => new EduVerse.Core.Tutor.PacePoint(a.Seconds, a.Correct)).ToList());
        }

        return new AnswerResult(correct, question.CorrectIndex, AnswerExplainer.Explain(question, correct, attempt.Level, language), reward.Xp, attempt.Run, next,
            summary is null ? null : summary with { Xp = attempt.Xp }, reward) { Hints = hints, Confidence = confidence, Rungs = rungs, Pace = pace };
    }

    /// <summary>Answers the current true/false drill. Returns null when there is no such drill current (wrong endpoint for the current item).</summary>
    public async Task<AnswerResult?> AnswerTrueFalseAsync(string userId, string attemptId, bool value, TeachLanguage language, CancellationToken ct = default) =>
        await AnswerTrueFalseAsync(userId, attemptId, value, 1, 0, language, ct).ConfigureAwait(false);

    /// <summary><paramref name="seconds"/>: time the question was on screen, as measured by the page (0 = unknown); only used for rhythm advice.</summary>
    public async Task<AnswerResult?> AnswerTrueFalseAsync(string userId, string attemptId, bool value, int confidence, int seconds, TeachLanguage language, CancellationToken ct = default)
    {
        var attempt = await db.QuizAttempts.FirstOrDefaultAsync(a => a.Id == attemptId && a.UserId == userId, ct).ConfigureAwait(false);
        if (attempt is null || attempt.FinishedAt is not null || attempt.CurrentExerciseJson is null)
        {
            return null;
        }

        var exercise = CurrentExercise(attempt);
        var correct = exercise.CheckTrueFalse(value);
        confidence = Math.Clamp(confidence, 0, Spaced.Sure);
        attempt.Answered++;
        attempt.Correct += correct ? 1 : 0;
        attempt.Run = correct ? attempt.Run + 1 : 0;
        attempt.BestRun = Math.Max(attempt.BestRun, attempt.Run);
        await UpdateSkillAsync(userId, attempt, exercise.Topic, exercise.Difficulty, correct, confidence, ct).ConfigureAwait(false);

        var finished = ApplyModeProgression(attempt, correct);

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        var reward = await game.AnswerAsync(userId, correct, exercise.Difficulty, attempt.Run, "answer", 0, confidence == Spaced.Sure, ct).ConfigureAwait(false);
        attempt.Xp += reward.Xp;

        QuestionView? next = null;
        QuizSummary? summary = null;
        if (finished)
        {
            (summary, var finishReward) = await FinishAsync(attempt, ct).ConfigureAwait(false);
            reward = reward.Plus(finishReward);
        }
        else
        {
            var nextExercise = TryExerciseFor(attempt, language);
            if (nextExercise is not null)
            {
                attempt.CurrentExerciseJson = JsonSerializer.Serialize(nextExercise, JsonOptions);
                attempt.CurrentJson = "";
                next = ViewOfExercise(attempt, nextExercise);
            }
            else
            {
                var nextQuestion = NextQuestion(attempt, language, await InterestsOfAsync(userId, ct).ConfigureAwait(false));
                attempt.CurrentExerciseJson = null;
                attempt.CurrentJson = JsonSerializer.Serialize(nextQuestion, JsonOptions);
                next = View(attempt, nextQuestion);
            }
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return new AnswerResult(correct, exercise.CorrectBool!.Value ? 1 : 0, exercise.Explanation, reward.Xp, attempt.Run, next,
            summary is null ? null : summary with { Xp = attempt.Xp }, reward) { Confidence = confidence };
    }

    /// <summary>Answers the current fill-in-the-blank drill. Returns null when there is no such drill current.</summary>
    public async Task<AnswerResult?> AnswerFillBlankAsync(string userId, string attemptId, string value, int confidence, int seconds, TeachLanguage language, CancellationToken ct = default)
    {
        var attempt = await db.QuizAttempts.FirstOrDefaultAsync(a => a.Id == attemptId && a.UserId == userId, ct).ConfigureAwait(false);
        if (attempt is null || attempt.FinishedAt is not null || attempt.CurrentExerciseJson is null)
        {
            return null;
        }

        var exercise = CurrentExercise(attempt);
        var correct = exercise.CheckFillBlank(value);
        return await FinishExerciseAnswerAsync(userId, attempt, exercise, correct, confidence, language, ct).ConfigureAwait(false);
    }

    /// <summary>Answers the current word-reorder drill. Returns null when there is no such drill current.</summary>
    public async Task<AnswerResult?> AnswerReorderAsync(string userId, string attemptId, IReadOnlyList<string> order, int confidence, int seconds, TeachLanguage language, CancellationToken ct = default)
    {
        var attempt = await db.QuizAttempts.FirstOrDefaultAsync(a => a.Id == attemptId && a.UserId == userId, ct).ConfigureAwait(false);
        if (attempt is null || attempt.FinishedAt is not null || attempt.CurrentExerciseJson is null)
        {
            return null;
        }

        var exercise = CurrentExercise(attempt);
        var correct = exercise.CheckReorder(order);
        return await FinishExerciseAnswerAsync(userId, attempt, exercise, correct, confidence, language, ct).ConfigureAwait(false);
    }

    /// <summary>Answers the current matching-pairs drill. Returns null when there is no such drill current.</summary>
    public async Task<AnswerResult?> AnswerMatchAsync(string userId, string attemptId, IReadOnlyDictionary<string, string> pairs, int confidence, int seconds, TeachLanguage language, CancellationToken ct = default)
    {
        var attempt = await db.QuizAttempts.FirstOrDefaultAsync(a => a.Id == attemptId && a.UserId == userId, ct).ConfigureAwait(false);
        if (attempt is null || attempt.FinishedAt is not null || attempt.CurrentExerciseJson is null)
        {
            return null;
        }

        var exercise = CurrentExercise(attempt);
        var correct = exercise.CheckMatch(pairs);
        return await FinishExerciseAnswerAsync(userId, attempt, exercise, correct, confidence, language, ct).ConfigureAwait(false);
    }

    /// <summary>Answers the current "select all that apply" drill (task #96). Returns null when there is no such drill current.</summary>
    public async Task<AnswerResult?> AnswerMultiSelectAsync(string userId, string attemptId, IReadOnlyList<int> indices, int confidence, int seconds, TeachLanguage language, CancellationToken ct = default)
    {
        var attempt = await db.QuizAttempts.FirstOrDefaultAsync(a => a.Id == attemptId && a.UserId == userId, ct).ConfigureAwait(false);
        if (attempt is null || attempt.FinishedAt is not null || attempt.CurrentExerciseJson is null)
        {
            return null;
        }

        var exercise = CurrentExercise(attempt);
        var correct = exercise.CheckMultiSelect(indices);
        return await FinishExerciseAnswerAsync(userId, attempt, exercise, correct, confidence, language, ct).ConfigureAwait(false);
    }

    /// <summary>Answers the current "listen and answer" drill (task #114): same shape as a classic multiple-choice
    /// answer (an option index), since the exercise reuses the regular question generator instead of a curated bank.
    /// Returns null when there is no such drill current.</summary>
    public async Task<AnswerResult?> AnswerListenAsync(string userId, string attemptId, int choice, int confidence, int seconds, TeachLanguage language, CancellationToken ct = default)
    {
        var attempt = await db.QuizAttempts.FirstOrDefaultAsync(a => a.Id == attemptId && a.UserId == userId, ct).ConfigureAwait(false);
        if (attempt is null || attempt.FinishedAt is not null || attempt.CurrentExerciseJson is null)
        {
            return null;
        }

        var exercise = CurrentExercise(attempt);
        var correct = exercise.CheckListen(choice);
        return await FinishExerciseAnswerAsync(userId, attempt, exercise, correct, confidence, language, ct).ConfigureAwait(false);
    }

    /// <summary>Checks a single left/right pairing of the current Match drill, Duolingo-style — a live green/red
    /// border as the student taps, instead of waiting for the final "Check". Read-only: no attempt state changes,
    /// no XP, no "answered" count, so the student can retry a wrong pairing freely; the final <see cref="AnswerMatchAsync"/>
    /// call (made once every pair has been matched correctly) is still what actually scores and advances the attempt.
    /// Returns null when there is no current Match drill, or <paramref name="left"/> isn't one of its items.</summary>
    public async Task<bool?> CheckMatchPairAsync(string userId, string attemptId, string left, string right, CancellationToken ct = default)
    {
        var attempt = await db.QuizAttempts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == attemptId && a.UserId == userId, ct).ConfigureAwait(false);
        if (attempt is null || attempt.FinishedAt is not null || attempt.CurrentExerciseJson is null)
        {
            return null;
        }

        var exercise = CurrentExercise(attempt);
        if (exercise.Kind != ExerciseKind.Match)
        {
            return null;
        }

        var pair = exercise.Pairs!.FirstOrDefault(p => p.Left == left);
        return pair is null ? null : pair.Right == right;
    }

    /// <summary>Shared tail for the 6 drill-answer methods (true/false, fill-blank, reorder, match, multi-select,
    /// listen): scores the exercise, updates skill/streak/xp, and queues the next item (another drill, a classic
    /// question, or the test summary). Seconds (time on screen) is reserved for the same rhythm-advice path the
    /// classic flow uses.</summary>
    private async Task<AnswerResult> FinishExerciseAnswerAsync(string userId, QuizAttempt attempt, PracticeExercise exercise, bool correct, int confidence, TeachLanguage language, CancellationToken ct)
    {
        confidence = Math.Clamp(confidence, 0, Spaced.Sure);
        attempt.Answered++;
        attempt.Correct += correct ? 1 : 0;
        attempt.Run = correct ? attempt.Run + 1 : 0;
        attempt.BestRun = Math.Max(attempt.BestRun, attempt.Run);
        await UpdateSkillAsync(userId, attempt, exercise.Topic, exercise.Difficulty, correct, confidence, ct).ConfigureAwait(false);

        var finished = ApplyModeProgression(attempt, correct);

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        var reward = await game.AnswerAsync(userId, correct, exercise.Difficulty, attempt.Run, "answer", 0, confidence == Spaced.Sure, ct).ConfigureAwait(false);
        attempt.Xp += reward.Xp;

        QuestionView? next = null;
        QuizSummary? summary = null;
        if (finished)
        {
            (summary, var finishReward) = await FinishAsync(attempt, ct).ConfigureAwait(false);
            reward = reward.Plus(finishReward);
        }
        else
        {
            var nextExercise = TryExerciseFor(attempt, language);
            if (nextExercise is not null)
            {
                attempt.CurrentExerciseJson = JsonSerializer.Serialize(nextExercise, JsonOptions);
                attempt.CurrentJson = "";
                next = ViewOfExercise(attempt, nextExercise);
            }
            else
            {
                var nextQuestion = NextQuestion(attempt, language, await InterestsOfAsync(userId, ct).ConfigureAwait(false));
                attempt.CurrentExerciseJson = null;
                attempt.CurrentJson = JsonSerializer.Serialize(nextQuestion, JsonOptions);
                next = View(attempt, nextQuestion);
            }
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        // CorrectIndex is only meaningful for Listen (it shares the classic MultipleChoice shape, so the client can
        // highlight the right option button); every other drill kind here has no such index, hence the ?? 0.
        return new AnswerResult(correct, exercise.CorrectIndex ?? 0, exercise.Explanation, reward.Xp, attempt.Run, next,
            summary is null ? null : summary with { Xp = attempt.Xp }, reward) { Confidence = confidence };
    }

    private Task UpdateSkillAsync(string userId, QuizAttempt attempt, QuizQuestion question, bool correct, int confidence, CancellationToken ct) =>
        UpdateSkillAsync(userId, attempt, question.Topic, question.Difficulty, correct, confidence, ct);

    private async Task UpdateSkillAsync(string userId, QuizAttempt attempt, string topic, int difficulty, bool correct, int confidence, CancellationToken ct)
    {
        if (attempt.Mode is QuizMode.Topic or QuizMode.Interview)
        {
            return;
        }

        var skill = await db.TopicSkills.FirstOrDefaultAsync(s => s.UserId == userId && s.Area == attempt.Area && s.Level == attempt.Level && s.Topic == topic, ct).ConfigureAwait(false);
        if (skill is null)
        {
            skill = new TopicSkill { UserId = userId, Area = attempt.Area, Level = attempt.Level, Topic = topic, PKnown = BktParameters.DefaultPrior };
            db.TopicSkills.Add(skill);
        }

        skill.PKnown = Bkt.Update(skill.PKnown, correct, BktParameters.ForDifficulty(difficulty));
        skill.Attempts++;
        skill.Correct += correct ? 1 : 0;
        skill.LastAt = Now;
        (skill.Box, skill.DueAt) = Spaced.Next(skill.Box, correct, confidence, Now);
    }

    private async Task<(QuizSummary, Reward)> FinishAsync(QuizAttempt attempt, CancellationToken ct)
    {
        attempt.FinishedAt = Now;
        var reward = Reward.None;
        int? duelOutcome = null;
        switch (attempt.Mode)
        {
            case QuizMode.Placement:
                await SetLevelAsync(attempt.UserId, attempt.Area, attempt.Result!.Value, placed: true, ct).ConfigureAwait(false);
                await game.AwardAsync(attempt.UserId, Xp.PlacementFinished, "placement", ct: ct).ConfigureAwait(false);
                reward = new Reward(Xp.PlacementFinished, [], []);
                break;
            case QuizMode.Duel:
                duelOutcome = await game.DuelScoreAsync(attempt.UserId, attempt.DuelId!.Value, attempt.Correct, ct).ConfigureAwait(false);
                break;
            case QuizMode.Adventure:
                var bonusXp = attempt.Won ? Xp.AdventureWon : Xp.Wrong * 10;
                await game.AwardAsync(attempt.UserId, bonusXp, "adventure", ct: ct).ConfigureAwait(false);
                reward = new Reward(bonusXp, [], []);
                break;
            default:
                var bonus = Xp.TestFinished + (attempt.Correct == attempt.Length ? Xp.PerfectTest : 0);
                await game.AwardAsync(attempt.UserId, bonus, "test", ct: ct).ConfigureAwait(false);
                reward = new Reward(bonus, [], []);
                if (attempt.Mode == QuizMode.Practice && !await db.SubjectLevels.AnyAsync(s => s.UserId == attempt.UserId && s.Area == attempt.Area, ct).ConfigureAwait(false))
                {
                    await SetLevelAsync(attempt.UserId, attempt.Area, attempt.Level, placed: false, ct).ConfigureAwait(false);
                }

                break;
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        reward = reward.Plus(await game.CheckAsync(attempt.UserId, ct).ConfigureAwait(false));
        attempt.Xp += reward.Xp;
        var topics = await db.QuizAnswers.Where(a => a.AttemptId == attempt.Id).GroupBy(a => a.Topic)
            .Select(g => new TopicResult(g.Key, g.Count(a => a.Correct), g.Count())).ToListAsync(ct).ConfigureAwait(false);
        var summary = new QuizSummary(attempt.Correct, attempt.Length, AdaptiveQuiz.Stars(attempt.Correct, attempt.Length), attempt.Xp, attempt.Result, topics, duelOutcome);
        if (attempt.Mode == QuizMode.Adventure)
        {
            var state = JsonSerializer.Deserialize<AdventureState>(attempt.AdventureJson!, JsonOptions)!;
            var story = JsonSerializer.Deserialize<Story>(attempt.StoryJson!, JsonOptions)!;
            summary = summary with { Stars = Adventure.Stars(state) };
            summary = summary with { Won = state.Won, Hearts = state.Hearts, Ending = state.Won ? story.Victory : story.Defeat };
        }

        return (summary, reward);
    }

    public async Task SetLevelAsync(string userId, Area area, Level level, bool placed, CancellationToken ct = default)
    {
        var record = await db.SubjectLevels.FirstOrDefaultAsync(s => s.UserId == userId && s.Area == area, ct).ConfigureAwait(false);
        if (record is null)
        {
            record = new SubjectLevel { UserId = userId, Area = area };
            db.SubjectLevels.Add(record);
        }

        record.Level = Catalog.Available(area, level) ? level : Catalog.Info(area).MinLevel;
        record.Placed = placed || record.Placed;
        record.UpdatedAt = Now;
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Next hint of the ladder for the current question (at most 3). Null when the test is unknown or finished.</summary>
    public async Task<Hint?> HintAsync(string userId, string attemptId, TeachLanguage language, CancellationToken ct = default)
    {
        var attempt = await db.QuizAttempts.FirstOrDefaultAsync(a => a.Id == attemptId && a.UserId == userId, ct).ConfigureAwait(false);
        if (attempt is null || attempt.FinishedAt is not null)
        {
            return null;
        }

        attempt.CurrentHints = Math.Min(Hints.Max, attempt.CurrentHints + 1);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return Hints.For(Current(attempt), attempt.CurrentHints, QuestionBanks.Seed(attempt.Seed, attempt.Answered, 7), language);
    }

    /// <summary>
    /// "Think with the Tutor": climbs one rung of the ladder on the current question (not in a placement test or a duel).
    /// After the last rung nothing more is given.
    /// </summary>
    public async Task<LadderState?> LadderAsync(string userId, string attemptId, CancellationToken ct = default)
    {
        var attempt = await db.QuizAttempts.FirstOrDefaultAsync(a => a.Id == attemptId && a.UserId == userId, ct).ConfigureAwait(false);
        if (attempt is null || attempt.FinishedAt is not null || !Ladder.Allowed(attempt.Mode))
        {
            return null;
        }

        if (attempt.CurrentRungs >= Ladder.Max)
        {
            return new LadderState(Ladder.Max, true, Current(attempt), attempt.Level);
        }

        attempt.CurrentRungs++;
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return new LadderState(attempt.CurrentRungs, false, Current(attempt), attempt.Level);
    }

    /// <summary>
    /// Topics to review today: the ones due by spaced repetition (oldest first), completed with the weakest topics, from every subject.
    /// </summary>
    public async Task<IReadOnlyList<ReviewItem>> ReviewPlanAsync(string userId, CancellationToken ct = default)
    {
        var now = Now;
        var skills = await db.TopicSkills.AsNoTracking().Where(s => s.UserId == userId && !s.Topic.StartsWith("topic:")).ToListAsync(ct).ConfigureAwait(false);
        var due = skills.Where(s => s.DueAt <= now).OrderBy(s => s.DueAt).ThenBy(s => s.PKnown).ToList();
        var weak = skills.Where(s => s.DueAt > now && s.PKnown < 0.6).OrderBy(s => s.PKnown).ToList();
        return due.Concat(weak).Select(s => new ReviewItem(s.Area, s.Level, s.Topic)).Distinct().Take(Spaced.ReviewLength).ToList();
    }

    public async Task<int> DueCountAsync(string userId, CancellationToken ct = default)
    {
        var now = Now;
        return await db.TopicSkills.CountAsync(s => s.UserId == userId && s.DueAt <= now && !s.Topic.StartsWith("topic:"), ct).ConfigureAwait(false);
    }

    /// <summary>The student's fraction mistakes from the tutor, the most frequent first (for Evo's notebook).</summary>
    public async Task<IReadOnlyList<string>> MistakesAsync(string userId, CancellationToken ct = default) =>
        await db.Attempts.Where(a => a.UserId == userId && a.SubjectId == Core.Curriculum.BuiltInSubjects.FractionsKey && a.Misconception != null)
            .GroupBy(a => a.Misconception!).OrderByDescending(g => g.Count()).Select(g => g.Key).ToListAsync(ct).ConfigureAwait(false);

    /// <summary>
    /// The real memory of each recurring mistake: not just whether the student makes it today, but whether a later,
    /// camouflaged recheck (Evo's notebook quietly repeating it weeks after, among ordinary practice) confirms it is
    /// really gone, or whether it is still there. The check only counts if it happened after the mistake's last
    /// occurrence — a pass from before a relapse does not count as "overcome".
    /// </summary>
    public async Task<IReadOnlyList<MisconceptionMemory>> MisconceptionMemoryAsync(string userId, CancellationToken ct = default)
    {
        var occurrences = await db.Attempts.Where(a => a.UserId == userId && a.SubjectId == Core.Curriculum.BuiltInSubjects.FractionsKey && a.Misconception != null)
            .GroupBy(a => a.Misconception!)
            .Select(g => new { Code = g.Key, Count = g.Count(), Last = g.Max(a => a.At) })
            .ToListAsync(ct).ConfigureAwait(false);
        if (occurrences.Count == 0)
        {
            return [];
        }

        // One row per code, most recent check first (materialized client-side: a single student's own checks are few).
        var checks = await db.QuizAnswers.Where(a => a.UserId == userId && a.TargetMisconception != null)
            .OrderByDescending(a => a.At).Select(a => new { a.TargetMisconception, a.At, a.Correct }).ToListAsync(ct).ConfigureAwait(false);
        var lastCheck = new Dictionary<string, (DateTimeOffset At, bool Correct)>(StringComparer.Ordinal);
        foreach (var c in checks)
        {
            lastCheck.TryAdd(c.TargetMisconception!, (c.At, c.Correct));
        }

        return occurrences.Select(o => new MisconceptionMemory(o.Code, o.Count, o.Last,
            lastCheck.TryGetValue(o.Code, out var check) ? check.At : null,
            lastCheck.TryGetValue(o.Code, out var check2) ? check2.Correct : null)).ToList();
    }

    /// <summary>
    /// Topics that look fine by the numbers (knowledge tracing already calls them known) but where most of the correct
    /// answers were the student's own "not sure" — a string of lucky guesses, not confirmed understanding. The Bayesian
    /// tracker (<see cref="Bkt"/>) never sees self-reported confidence at all, so this is additive, not a correction to it.
    /// </summary>
    public async Task<IReadOnlyList<(Area Area, Level Level, string Topic, int GuessPercent)>> GuessedTopicsAsync(string userId, CancellationToken ct = default)
    {
        var byTopic = await db.QuizAnswers.Where(a => a.UserId == userId && a.Correct)
            .GroupBy(a => new { a.Area, a.Level, a.Topic })
            .Select(g => new { g.Key.Area, g.Key.Level, g.Key.Topic, Correct = g.Count(), Guessed = g.Count(a => a.Confidence == 0) })
            .Where(g => g.Correct >= Confidence.MinSample)
            .ToListAsync(ct).ConfigureAwait(false);
        if (byTopic.Count == 0)
        {
            return [];
        }

        var known = (await db.TopicSkills.Where(s => s.UserId == userId && s.PKnown >= 0.6)
                .Select(s => new { s.Area, s.Level, s.Topic }).ToListAsync(ct).ConfigureAwait(false))
            .Select(s => (s.Area, s.Level, s.Topic)).ToHashSet();

        return byTopic.Where(t => known.Contains((t.Area, t.Level, t.Topic)) && Confidence.LooksGuessed(t.Correct, t.Guessed))
            .Select(t => (t.Area, t.Level, t.Topic, (int)Math.Round(100.0 * t.Guessed / t.Correct)))
            .ToList();
    }

    /// <summary>Applies one level of study to every subject (the subjects that start later get their first level).</summary>
    public async Task ApplyLevelToAllAsync(string userId, Level level, CancellationToken ct = default)
    {
        var records = await db.SubjectLevels.Where(s => s.UserId == userId).ToDictionaryAsync(s => s.Area, ct).ConfigureAwait(false);
        foreach (var info in Catalog.Areas)
        {
            if (!records.TryGetValue(info.Area, out var record))
            {
                record = new SubjectLevel { UserId = userId, Area = info.Area };
                db.SubjectLevels.Add(record);
            }

            record.Level = Catalog.Available(info.Area, level) ? level : info.MinLevel;
            record.Placed = false;
            record.UpdatedAt = Now;
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Finished tests of the student, most recent first.</summary>
    public async Task<IReadOnlyList<QuizAttempt>> HistoryAsync(string userId, Area? area = null, int take = 100, CancellationToken ct = default)
    {
        var query = db.QuizAttempts.AsNoTracking().Where(a => a.UserId == userId && a.FinishedAt != null);
        if (area is { } a)
        {
            query = query.Where(x => x.Area == a);
        }

        return await query.OrderByDescending(x => x.FinishedAt).Take(take).ToListAsync(ct).ConfigureAwait(false);
    }

    /// <summary>A finished test with every question, the answer given and the right one (for the review).</summary>
    public async Task<(QuizAttempt Attempt, IReadOnlyList<QuizAnswer> Answers)?> ReviewAsync(string userId, string attemptId, CancellationToken ct = default)
    {
        var attempt = await db.QuizAttempts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == attemptId && a.UserId == userId && a.FinishedAt != null, ct).ConfigureAwait(false);
        if (attempt is null)
        {
            return null;
        }

        var answers = await db.QuizAnswers.AsNoTracking().Where(a => a.AttemptId == attemptId).OrderBy(a => a.Number).ThenBy(a => a.Id).ToListAsync(ct).ConfigureAwait(false);
        return (attempt, answers);
    }

    public static IReadOnlyList<string> Options(QuizAnswer answer) =>
        JsonSerializer.Deserialize<List<string>>(answer!.OptionsJson, JsonOptions) ?? [];

    /// <summary>Level, answers, accuracy and weakest topics of the student in every subject of the catalog.</summary>
    public async Task<IReadOnlyList<SubjectProgress>> ProgressAsync(string userId, Level defaultLevel, CancellationToken ct = default)
    {
        var levels = await db.SubjectLevels.Where(s => s.UserId == userId).ToDictionaryAsync(s => s.Area, ct).ConfigureAwait(false);
        var answers = await db.QuizAnswers.Where(a => a.UserId == userId).GroupBy(a => a.Area)
            .Select(g => new { g.Key, Total = g.Count(), Right = g.Count(a => a.Correct) }).ToDictionaryAsync(g => g.Key, ct).ConfigureAwait(false);
        var skills = await db.TopicSkills.Where(s => s.UserId == userId).ToListAsync(ct).ConfigureAwait(false);
        var tests = await db.QuizAttempts.Where(a => a.UserId == userId && a.FinishedAt != null).GroupBy(a => a.Area)
            .Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(g => g.Key, g => g.Count, ct).ConfigureAwait(false);
        return Catalog.Areas.Select(info =>
        {
            var level = levels.TryGetValue(info.Area, out var l) ? l.Level : (Catalog.Available(info.Area, defaultLevel) ? defaultLevel : info.MinLevel);
            var a = answers.TryGetValue(info.Area, out var x) ? x : null;
            var weak = skills.Where(s => s.Area == info.Area && s.Attempts >= 2 && s.PKnown < 0.6).OrderBy(s => s.PKnown).Take(3).ToList();
            return new SubjectProgress(info.Area, level, l?.Placed ?? false, a?.Total ?? 0, a is null || a.Total == 0 ? 0 : 100 * a.Right / a.Total, weak,
                tests.TryGetValue(info.Area, out var t) ? t : 0);
        }).ToList();
    }
}
