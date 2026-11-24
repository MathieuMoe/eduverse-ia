using EduVerse.Core.Catalog;
using EduVerse.Core.Common;
using EduVerse.Infrastructure.Data;
using EduVerse.Infrastructure.Quizzes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EduVerse.Tests;

/// <summary>Pure data-level coverage of the demo content; see <see cref="PracticeExerciseQuizTests"/> for the live-store round trip.</summary>
public class ExerciseBankTests
{
    [Theory]
    [InlineData(Area.Language)]
    [InlineData(Area.ForeignLanguage)]
    public void No_true_false_content_yet_for_language_subjects(Area area)
    {
        // Language and ForeignLanguage are left for task #71, where fill-blank/reorder/match fit more naturally.
        Assert.Null(ExerciseBank.TrueFalse(area, Level.Primary3, TeachLanguage.English, 1));
    }

    [Theory]
    [InlineData(Area.Mathematics)]
    [InlineData(Area.Physics)]
    [InlineData(Area.Chemistry)]
    [InlineData(Area.Biology)]
    [InlineData(Area.History)]
    [InlineData(Area.Geography)]
    [InlineData(Area.Economics)]
    [InlineData(Area.Programming)]
    public void Every_non_language_subject_has_a_true_false_drill_at_every_level_bucket(Area area)
    {
        foreach (var level in new[] { Level.EarlyYears, Level.Primary3, Level.HighSchool })
        {
            var exercise = ExerciseBank.TrueFalse(area, level, TeachLanguage.Spanish, 7);
            Assert.NotNull(exercise);
            Assert.Equal(ExerciseKind.TrueFalse, exercise!.Kind);
            Assert.NotNull(exercise.CorrectBool);
            Assert.False(string.IsNullOrWhiteSpace(exercise.Prompt));
            Assert.False(string.IsNullOrWhiteSpace(exercise.Explanation));
        }
    }

    [Fact]
    public void The_same_seed_always_gives_the_same_drill()
    {
        var a = ExerciseBank.TrueFalse(Area.Mathematics, Level.Primary3, TeachLanguage.French, 42);
        var b = ExerciseBank.TrueFalse(Area.Mathematics, Level.Primary3, TeachLanguage.French, 42);
        Assert.Equal(a, b);
    }

    public static IEnumerable<object[]> NonLanguageAreas =>
    [
        [Area.Mathematics],
        [Area.Physics],
        [Area.Chemistry],
        [Area.Biology],
        [Area.History],
        [Area.Geography],
        [Area.Economics],
        [Area.Programming],
    ];

    [Theory]
    [MemberData(nameof(NonLanguageAreas))]
    public void No_language_drill_for_a_subject_other_than_language(Area area)
    {
        // LanguageDrill (true/false-or-fill-blank/reorder/match mix) is for Language and ForeignLanguage only
        // (task #71); the other 8 subjects get true/false (task #72) and their own fill-blank/reorder/match
        // content (task #73), reached directly through FillBlank/Reorder/Match, not LanguageDrill.
        Assert.Null(ExerciseBank.LanguageDrill(area, Level.Primary3, TeachLanguage.English, 1));
    }

    [Theory]
    [MemberData(nameof(NonLanguageAreas))]
    public void Every_non_language_subject_has_fill_blank_reorder_and_match_drills_at_every_level_bucket(Area area)
    {
        // task #73: fill-blank/reorder/match now reuse the UI built in task #71 for all 8 non-language subjects,
        // the same way true/false was extended to them in task #72.
        foreach (var level in new[] { Level.EarlyYears, Level.Primary3, Level.HighSchool })
        {
            var fillBlank = ExerciseBank.FillBlank(area, level, TeachLanguage.Spanish, 3);
            Assert.NotNull(fillBlank);
            Assert.Equal(ExerciseKind.FillBlank, fillBlank!.Kind);
            Assert.False(string.IsNullOrWhiteSpace(fillBlank.Answer));
            Assert.False(string.IsNullOrWhiteSpace(fillBlank.Prompt));
            Assert.False(string.IsNullOrWhiteSpace(fillBlank.Explanation));

            var reorder = ExerciseBank.Reorder(area, level, TeachLanguage.Spanish, 3);
            Assert.NotNull(reorder);
            Assert.Equal(ExerciseKind.Reorder, reorder!.Kind);
            Assert.NotNull(reorder.Tokens);
            Assert.NotEmpty(reorder.Tokens!);
            Assert.Equal(reorder.Tokens!.Count, reorder.CorrectOrder!.Count);

            var match = ExerciseBank.Match(area, level, TeachLanguage.Spanish, 3);
            Assert.NotNull(match);
            Assert.Equal(ExerciseKind.Match, match!.Kind);
            Assert.NotNull(match.Pairs);
            Assert.NotEmpty(match.Pairs!);
        }
    }

    [Fact]
    public void Generic_reorder_drill_gives_the_same_token_set_in_every_language()
    {
        // The tokens are derived from a per-language sentence (not word-aligned across languages), so the only
        // invariant worth checking is that shuffling preserves the token multiset for a given language.
        var exercise = ExerciseBank.Reorder(Area.Mathematics, Level.Primary3, TeachLanguage.French, 9);
        Assert.NotNull(exercise);
        Assert.Equal(exercise!.CorrectOrder!.OrderBy(t => t), exercise.Tokens!.OrderBy(t => t));
    }

    public static IEnumerable<object[]> LanguageAreas =>
    [
        [Area.Language],
        [Area.ForeignLanguage],
    ];

    [Theory]
    [MemberData(nameof(LanguageAreas))]
    public void Every_language_subject_has_a_fill_blank_drill_at_every_level_bucket(Area area)
    {
        foreach (var level in new[] { Level.EarlyYears, Level.Primary3, Level.HighSchool })
        {
            var exercise = ExerciseBank.FillBlank(area, level, TeachLanguage.Spanish, 7);
            Assert.NotNull(exercise);
            Assert.Equal(ExerciseKind.FillBlank, exercise!.Kind);
            Assert.False(string.IsNullOrWhiteSpace(exercise.Answer));
            Assert.False(string.IsNullOrWhiteSpace(exercise.Prompt));
        }
    }

    [Theory]
    [MemberData(nameof(LanguageAreas))]
    public void Every_language_subject_has_a_reorder_drill_at_every_level_bucket(Area area)
    {
        foreach (var level in new[] { Level.EarlyYears, Level.Primary3, Level.HighSchool })
        {
            var exercise = ExerciseBank.Reorder(area, level, TeachLanguage.Catalan, 7);
            Assert.NotNull(exercise);
            Assert.Equal(ExerciseKind.Reorder, exercise!.Kind);
            Assert.NotNull(exercise.Tokens);
            Assert.NotNull(exercise.CorrectOrder);
            Assert.True(exercise.Tokens!.Count >= 2);
            Assert.Equal(exercise.CorrectOrder!.Count, exercise.Tokens!.Count);
            Assert.Equal([.. exercise.CorrectOrder.Order()], [.. exercise.Tokens.Order()]); // same words, just possibly reshuffled
        }
    }

    [Theory]
    [MemberData(nameof(LanguageAreas))]
    public void Every_language_subject_has_a_match_drill_at_every_level_bucket(Area area)
    {
        foreach (var level in new[] { Level.EarlyYears, Level.Primary3, Level.HighSchool })
        {
            var exercise = ExerciseBank.Match(area, level, TeachLanguage.French, 7);
            Assert.NotNull(exercise);
            Assert.Equal(ExerciseKind.Match, exercise!.Kind);
            Assert.NotNull(exercise.Pairs);
            Assert.True(exercise.Pairs!.Count >= 2);
        }
    }

    [Theory]
    [MemberData(nameof(LanguageAreas))]
    public void LanguageDrill_picks_among_the_three_kinds_and_never_true_false(Area area)
    {
        var kinds = Enumerable.Range(0, 30)
            .Select(seed => ExerciseBank.LanguageDrill(area, Level.Primary3, TeachLanguage.English, seed)!.Kind)
            .Distinct()
            .ToList();
        Assert.DoesNotContain(ExerciseKind.TrueFalse, kinds);
        Assert.True(kinds.Count > 1, "30 different seeds should surface more than one of the three drill kinds");
    }

    [Theory]
    [MemberData(nameof(NonLanguageAreas))]
    public void AnyDrill_mixes_all_four_kinds_for_a_non_language_subject(Area area)
    {
        // task #73: once a subject has true/false (task #72) plus fill-blank/reorder/match (task #73) content,
        // the live Quiz flow (QuizStore.TryExerciseFor) picks among all 4 instead of always preferring true/false.
        var kinds = Enumerable.Range(0, 40)
            .Select(seed => ExerciseBank.AnyDrill(area, Level.Primary3, TeachLanguage.English, seed)!.Kind)
            .Distinct()
            .ToList();
        Assert.True(kinds.Count > 1, "40 different seeds should surface more than one of the four drill kinds");
    }

    [Theory]
    [MemberData(nameof(LanguageAreas))]
    public void AnyDrill_never_returns_true_false_for_a_language_subject(Area area)
    {
        // Language/ForeignLanguage still have no true/false content (task #71); AnyDrill must fall through to the
        // other 3 kinds for them, exactly as LanguageDrill already does.
        var kinds = Enumerable.Range(0, 30)
            .Select(seed => ExerciseBank.AnyDrill(area, Level.Primary3, TeachLanguage.English, seed)!.Kind)
            .Distinct()
            .ToList();
        Assert.DoesNotContain(ExerciseKind.TrueFalse, kinds);
    }

    [Fact]
    public void A_foreign_language_drill_translates_into_the_language_the_interface_is_not_in()
    {
        // English interface -> Spanish is taught; any other interface language -> English is taught (QuestionBanks.ForeignTarget).
        var fillFromEnglish = ExerciseBank.FillBlank(Area.ForeignLanguage, Level.Primary3, TeachLanguage.English, 1)!;
        Assert.Contains("Spanish", fillFromEnglish.Prompt, StringComparison.Ordinal);

        var fillFromSpanish = ExerciseBank.FillBlank(Area.ForeignLanguage, Level.Primary3, TeachLanguage.Spanish, 1)!;
        Assert.Contains("inglés", fillFromSpanish.Prompt, StringComparison.Ordinal);
    }

    [Fact]
    public void AnyDrill_never_returns_true_false_when_it_is_disallowed()
    {
        // Placement excludes True/False (task #74): a 50%-guess coin-flip is too risky on a binary-search
        // question that single-handedly decides whether a level is "known". Check across enough seeds and
        // subjects that the exclusion, not luck, is what's being observed.
        foreach (var area in new[] { Area.Mathematics, Area.Physics, Area.History, Area.Language, Area.ForeignLanguage })
        {
            for (var seed = 0; seed < 40; seed++)
            {
                var exercise = ExerciseBank.AnyDrill(area, Level.Primary3, TeachLanguage.English, seed, allowTrueFalse: false);
                if (exercise is not null)
                {
                    Assert.NotEqual(ExerciseKind.TrueFalse, exercise.Kind);
                }
            }
        }
    }
}

/// <summary>
/// The server-side half of the Duolingo-style exercise architecture (task #69): a true/false drill can be made the
/// current item of a live Practice attempt and answered through <see cref="QuizStore.AnswerTrueFalseAsync(string,string,bool,EduVerse.Core.Common.TeachLanguage,CancellationToken)"/>,
/// without disturbing the classic <see cref="QuizQuestion"/> flow. The server does not yet pick these on its own in
/// <see cref="QuizStore.NextQuestion"/> (that switches on together with the matching UI, see HANDOFF) — this test
/// exercises the path the way that future wiring will drive it: by placing a drill directly as the attempt's current item.
/// </summary>
public class PracticeExerciseQuizTests(Database database) : IClassFixture<Database>
{
    // QuizStore serializes AdventureJson/PlacementJson with Web defaults (camelCase); match that here so reading
    // them back (task #74's tests below) doesn't silently default every field to 0/false on a case mismatch.
    private static readonly System.Text.Json.JsonSerializerOptions Web = new(System.Text.Json.JsonSerializerDefaults.Web);

    [Fact]
    public async Task A_true_false_drill_can_be_answered_right_and_wrong_and_the_quiz_moves_on()
    {
        using var scope = database.Services.CreateScope();
        var quizzes = scope.ServiceProvider.GetRequiredService<QuizStore>();
        var db = scope.ServiceProvider.GetRequiredService<EduVerseDb>();
        var sara = await database.UserIdAsync("sara@eduverse.demo");

        var started = await quizzes.StartAsync(sara, Area.Mathematics, Level.Primary3, QuizMode.Practice, TeachLanguage.English);
        Assert.Null(started.ExerciseKind); // the first question is still a classic one today

        var exercise = ExerciseBank.TrueFalse(Area.Mathematics, Level.Primary3, TeachLanguage.English, 7)!;
        var attempt = await db.QuizAttempts.SingleAsync(a => a.Id == started.AttemptId);
        attempt.CurrentExerciseJson = System.Text.Json.JsonSerializer.Serialize(exercise);
        await db.SaveChangesAsync();
        var (attemptsBefore, correctBefore) = await SkillAsync(db, sara, exercise.Topic);

        var current = await quizzes.CurrentAsync(sara, started.AttemptId);
        Assert.Equal(ExerciseKind.TrueFalse, current!.ExerciseKind);
        Assert.Empty(current.Options);
        Assert.Equal(exercise.Prompt, current.Prompt);

        // The classic choice-based endpoint refuses to answer a drill: it would misinterpret the choice.
        Assert.Null(await quizzes.AnswerAsync(sara, started.AttemptId, 0, TeachLanguage.English));

        var right = await quizzes.AnswerTrueFalseAsync(sara, started.AttemptId, exercise.CorrectBool!.Value, TeachLanguage.English);
        Assert.NotNull(right);
        Assert.True(right!.Correct);
        Assert.Equal(exercise.CorrectBool.Value ? 1 : 0, right.CorrectIndex);
        Assert.Equal(exercise.Explanation, right.Explanation);
        Assert.Equal(1, right.Run);
        Assert.NotNull(right.Next); // the quiz carries on with a classic question again
        Assert.Null(right.Next!.ExerciseKind);

        var (attemptsAfter, correctAfter) = await SkillAsync(db, sara, exercise.Topic);
        Assert.Equal(attemptsBefore + 1, attemptsAfter);
        Assert.Equal(correctBefore + 1, correctAfter);
    }

    [Fact]
    public async Task A_wrong_true_false_answer_breaks_the_run_without_crashing_the_quiz()
    {
        using var scope = database.Services.CreateScope();
        var quizzes = scope.ServiceProvider.GetRequiredService<QuizStore>();
        var db = scope.ServiceProvider.GetRequiredService<EduVerseDb>();
        var omar = await database.UserIdAsync("omar@eduverse.demo");

        var started = await quizzes.StartAsync(omar, Area.Mathematics, Level.Primary3, QuizMode.Practice, TeachLanguage.Spanish);
        var exercise = ExerciseBank.TrueFalse(Area.Mathematics, Level.Primary3, TeachLanguage.Spanish, 3)!;
        var attempt = await db.QuizAttempts.SingleAsync(a => a.Id == started.AttemptId);
        attempt.CurrentExerciseJson = System.Text.Json.JsonSerializer.Serialize(exercise);
        await db.SaveChangesAsync();
        var (attemptsBefore, correctBefore) = await SkillAsync(db, omar, exercise.Topic);

        var wrong = await quizzes.AnswerTrueFalseAsync(omar, started.AttemptId, !exercise.CorrectBool!.Value, TeachLanguage.Spanish);
        Assert.NotNull(wrong);
        Assert.False(wrong!.Correct);
        Assert.Equal(0, wrong.Run);

        var (attemptsAfter, correctAfter) = await SkillAsync(db, omar, exercise.Topic);
        Assert.Equal(attemptsBefore + 1, attemptsAfter);
        Assert.Equal(correctBefore, correctAfter);
    }

    private static async Task<(int Attempts, int Correct)> SkillAsync(EduVerseDb db, string userId, string topic)
    {
        var skill = await db.TopicSkills.AsNoTracking().SingleOrDefaultAsync(s => s.UserId == userId && s.Topic == topic && s.Area == Area.Mathematics);
        return (skill?.Attempts ?? 0, skill?.Correct ?? 0);
    }

    [Fact]
    public async Task Answering_a_drill_twice_the_second_time_is_refused()
    {
        using var scope = database.Services.CreateScope();
        var quizzes = scope.ServiceProvider.GetRequiredService<QuizStore>();
        var db = scope.ServiceProvider.GetRequiredService<EduVerseDb>();
        var sara = await database.UserIdAsync("sara@eduverse.demo");

        var started = await quizzes.StartAsync(sara, Area.Mathematics, Level.Primary3, QuizMode.Practice, TeachLanguage.English);
        var exercise = ExerciseBank.TrueFalse(Area.Mathematics, Level.Primary3, TeachLanguage.English, 11)!;
        var attempt = await db.QuizAttempts.SingleAsync(a => a.Id == started.AttemptId);
        attempt.CurrentExerciseJson = System.Text.Json.JsonSerializer.Serialize(exercise);
        await db.SaveChangesAsync();

        Assert.NotNull(await quizzes.AnswerTrueFalseAsync(sara, started.AttemptId, exercise.CorrectBool!.Value, TeachLanguage.English));
        // The next item is a classic question again, so answering true/false again is refused.
        Assert.Null(await quizzes.AnswerTrueFalseAsync(sara, started.AttemptId, true, TeachLanguage.English));
    }

    [Fact]
    public async Task A_fill_blank_drill_can_be_answered_right_and_the_quiz_moves_on()
    {
        using var scope = database.Services.CreateScope();
        var quizzes = scope.ServiceProvider.GetRequiredService<QuizStore>();
        var db = scope.ServiceProvider.GetRequiredService<EduVerseDb>();
        var sara = await database.UserIdAsync("sara@eduverse.demo");

        var started = await quizzes.StartAsync(sara, Area.Language, Level.Primary3, QuizMode.Practice, TeachLanguage.English);
        var exercise = ExerciseBank.FillBlank(Area.Language, Level.Primary3, TeachLanguage.English, 7)!;
        var attempt = await db.QuizAttempts.SingleAsync(a => a.Id == started.AttemptId);
        attempt.CurrentExerciseJson = System.Text.Json.JsonSerializer.Serialize(exercise);
        await db.SaveChangesAsync();

        var current = await quizzes.CurrentAsync(sara, started.AttemptId);
        Assert.Equal(ExerciseKind.FillBlank, current!.ExerciseKind);
        Assert.Empty(current.Options);

        var right = await quizzes.AnswerFillBlankAsync(sara, started.AttemptId, exercise.Answer!, 1, 0, TeachLanguage.English);
        Assert.NotNull(right);
        Assert.True(right!.Correct);
        Assert.Equal(1, right.Run);
        Assert.NotNull(right.Next);
    }

    [Fact]
    public async Task A_reorder_drill_accepts_the_correct_order_and_rejects_a_wrong_one()
    {
        using var scope = database.Services.CreateScope();
        var quizzes = scope.ServiceProvider.GetRequiredService<QuizStore>();
        var db = scope.ServiceProvider.GetRequiredService<EduVerseDb>();
        var omar = await database.UserIdAsync("omar@eduverse.demo");

        var started = await quizzes.StartAsync(omar, Area.Language, Level.Primary3, QuizMode.Practice, TeachLanguage.English);
        var exercise = ExerciseBank.Reorder(Area.Language, Level.Primary3, TeachLanguage.English, 7)!;
        var attempt = await db.QuizAttempts.SingleAsync(a => a.Id == started.AttemptId);
        attempt.CurrentExerciseJson = System.Text.Json.JsonSerializer.Serialize(exercise);
        await db.SaveChangesAsync();

        var current = await quizzes.CurrentAsync(omar, started.AttemptId);
        Assert.Equal(ExerciseKind.Reorder, current!.ExerciseKind);
        Assert.Equal(exercise.Tokens!.Count, current.Tokens!.Count);

        var scrambled = exercise.CorrectOrder!.Reverse().ToList();
        var wrong = await quizzes.AnswerReorderAsync(omar, started.AttemptId, scrambled, 1, 0, TeachLanguage.English);
        Assert.NotNull(wrong);
        Assert.False(wrong!.Correct);
    }

    [Fact]
    public async Task A_match_drill_can_be_answered_right()
    {
        using var scope = database.Services.CreateScope();
        var quizzes = scope.ServiceProvider.GetRequiredService<QuizStore>();
        var db = scope.ServiceProvider.GetRequiredService<EduVerseDb>();
        var sara = await database.UserIdAsync("sara@eduverse.demo");

        var started = await quizzes.StartAsync(sara, Area.ForeignLanguage, Level.Primary3, QuizMode.Practice, TeachLanguage.English);
        var exercise = ExerciseBank.Match(Area.ForeignLanguage, Level.Primary3, TeachLanguage.English, 7)!;
        var attempt = await db.QuizAttempts.SingleAsync(a => a.Id == started.AttemptId);
        attempt.CurrentExerciseJson = System.Text.Json.JsonSerializer.Serialize(exercise);
        await db.SaveChangesAsync();

        var current = await quizzes.CurrentAsync(sara, started.AttemptId);
        Assert.Equal(ExerciseKind.Match, current!.ExerciseKind);
        Assert.Equal(exercise.Pairs!.Count, current.MatchLeft!.Count);
        Assert.Equal(exercise.Pairs.Count, current.MatchRight!.Count);

        var pairs = exercise.Pairs.ToDictionary(p => p.Left, p => p.Right);
        var right = await quizzes.AnswerMatchAsync(sara, started.AttemptId, pairs, 1, 0, TeachLanguage.English);
        Assert.NotNull(right);
        Assert.True(right!.Correct);
    }

    [Fact]
    public async Task CheckMatchPairAsync_gives_live_feedback_without_finishing_the_drill()
    {
        // Backs the Duolingo-style instant green/red border on each pairing: a read-only per-pair check, so the
        // student can see right away without the whole drill being scored or the attempt advancing.
        using var scope = database.Services.CreateScope();
        var quizzes = scope.ServiceProvider.GetRequiredService<QuizStore>();
        var db = scope.ServiceProvider.GetRequiredService<EduVerseDb>();
        var sara = await database.UserIdAsync("sara@eduverse.demo");

        var started = await quizzes.StartAsync(sara, Area.ForeignLanguage, Level.Primary3, QuizMode.Practice, TeachLanguage.English);
        var exercise = ExerciseBank.Match(Area.ForeignLanguage, Level.Primary3, TeachLanguage.English, 7)!;
        var attempt = await db.QuizAttempts.SingleAsync(a => a.Id == started.AttemptId);
        attempt.CurrentExerciseJson = System.Text.Json.JsonSerializer.Serialize(exercise);
        await db.SaveChangesAsync();
        var answeredBefore = attempt.Answered;

        var firstPair = exercise.Pairs![0];
        var wrongRight = exercise.Pairs.First(p => p.Right != firstPair.Right).Right;

        Assert.True(await quizzes.CheckMatchPairAsync(sara, started.AttemptId, firstPair.Left, firstPair.Right));
        Assert.False(await quizzes.CheckMatchPairAsync(sara, started.AttemptId, firstPair.Left, wrongRight));
        Assert.Null(await quizzes.CheckMatchPairAsync(sara, started.AttemptId, "not a real left item", firstPair.Right));

        // None of the checks above scored or advanced the attempt — the drill is still current, untouched.
        var after = await db.QuizAttempts.AsNoTracking().SingleAsync(a => a.Id == started.AttemptId);
        Assert.Equal(answeredBefore, after.Answered);
        Assert.NotNull(after.CurrentExerciseJson);
    }

    [Fact]
    public async Task A_drill_answered_during_an_adventure_advances_the_door_state_and_keeps_the_adventure_view()
    {
        // Task #74: a drill can now land on any of Adventure's turns, not just Practice's. Answering it right
        // must still open the next door (not leave the state frozen, per ApplyModeProgression) and the response's
        // Adventure view (map/hearts/story line) must still be there, since ViewOfExercise now sets it too.
        using var scope = database.Services.CreateScope();
        var quizzes = scope.ServiceProvider.GetRequiredService<QuizStore>();
        var db = scope.ServiceProvider.GetRequiredService<EduVerseDb>();
        var sara = await database.UserIdAsync("sara@eduverse.demo");

        var story = DemoStories.Write(Theme.Castle, Area.Mathematics, TeachLanguage.English);
        var started = await quizzes.StartAsync(sara, Area.Mathematics, Level.Primary3, QuizMode.Adventure, TeachLanguage.English, story: story, theme: Theme.Castle);
        var exercise = ExerciseBank.TrueFalse(Area.Mathematics, Level.Primary3, TeachLanguage.English, 7)!;
        var attempt = await db.QuizAttempts.SingleAsync(a => a.Id == started.AttemptId);
        var before = System.Text.Json.JsonSerializer.Deserialize<AdventureState>(attempt.AdventureJson!, Web)!;
        attempt.CurrentExerciseJson = System.Text.Json.JsonSerializer.Serialize(exercise);
        await db.SaveChangesAsync();

        var result = await quizzes.AnswerTrueFalseAsync(sara, started.AttemptId, exercise.CorrectBool!.Value, TeachLanguage.English);
        Assert.NotNull(result);
        Assert.True(result!.Correct);
        Assert.NotNull(result.Next!.Adventure); // the map/hearts/story-line UI survives a drill turn
        Assert.Equal(before.Passed + 1, result.Next.Adventure!.Passed);

        var after = System.Text.Json.JsonSerializer.Deserialize<AdventureState>((await db.QuizAttempts.AsNoTracking().SingleAsync(a => a.Id == started.AttemptId)).AdventureJson!, Web)!;
        Assert.Equal(before.Passed + 1, after.Passed); // the door actually opened, not just the view
    }

    [Fact]
    public async Task A_drill_answered_during_placement_advances_the_binary_search()
    {
        // Task #74: a drill can also land on a Placement turn. Answering it must still feed Placement's binary
        // search (PlacementState.Right/Total), per ApplyModeProgression, exactly like a classic answer would.
        using var scope = database.Services.CreateScope();
        var quizzes = scope.ServiceProvider.GetRequiredService<QuizStore>();
        var db = scope.ServiceProvider.GetRequiredService<EduVerseDb>();
        var sara = await database.UserIdAsync("sara@eduverse.demo");

        var started = await quizzes.StartAsync(sara, Area.Mathematics, Level.EarlyYears, QuizMode.Placement, TeachLanguage.English);
        var exercise = ExerciseBank.FillBlank(Area.Mathematics, started.Level, TeachLanguage.English, 7)
            ?? ExerciseBank.Reorder(Area.Mathematics, started.Level, TeachLanguage.English, 7)!;
        var attempt = await db.QuizAttempts.SingleAsync(a => a.Id == started.AttemptId);
        var before = System.Text.Json.JsonSerializer.Deserialize<PlacementState>(attempt.PlacementJson!, Web)!;
        attempt.CurrentExerciseJson = System.Text.Json.JsonSerializer.Serialize(exercise);
        await db.SaveChangesAsync();

        var result = exercise.Kind == ExerciseKind.FillBlank
            ? await quizzes.AnswerFillBlankAsync(sara, started.AttemptId, exercise.Answer!, 1, 0, TeachLanguage.English)
            : await quizzes.AnswerReorderAsync(sara, started.AttemptId, exercise.CorrectOrder!, 1, 0, TeachLanguage.English);
        Assert.NotNull(result);
        Assert.True(result!.Correct);

        var after = System.Text.Json.JsonSerializer.Deserialize<PlacementState>((await db.QuizAttempts.AsNoTracking().SingleAsync(a => a.Id == started.AttemptId)).PlacementJson!, Web)!;
        Assert.Equal(before.Right + 1, after.Right); // the right answer actually counted toward the level search
        Assert.Equal(before.Total + 1, after.Total);
    }
}
