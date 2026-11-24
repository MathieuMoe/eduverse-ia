using EduVerse.Core.Catalog;
using EduVerse.Core.Common;
using EduVerse.Core.Gamification;
using EduVerse.Infrastructure.Data;
using EduVerse.Infrastructure.Gamification;
using EduVerse.Infrastructure.Learning;
using EduVerse.Infrastructure.Quizzes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EduVerse.Tests;

public class StudentPlatformPersistenceTests(Database database) : IClassFixture<Database>
{
    private static readonly System.Text.Json.JsonSerializerOptions Web = new(System.Text.Json.JsonSerializerDefaults.Web);

    /// <summary>Builds a Match drill answer: the real pairs if <paramref name="right"/>, or every left matched to
    /// the next pair's right otherwise (always wrong for 2+ pairs, since CheckMatch requires every pair to line up).</summary>
    private static Dictionary<string, string> MatchAnswer(IReadOnlyList<MatchPair> pairs, bool right)
    {
        if (right)
        {
            return pairs.ToDictionary(p => p.Left, p => p.Right);
        }

        var ordered = pairs.ToList();
        return ordered.ToDictionary(p => p.Left, p => ordered[(ordered.IndexOf(p) + 1) % ordered.Count].Right);
    }

    [Fact]
    public async Task A_practice_test_grades_on_the_server_gives_xp_and_tracks_topics()
    {
        using var scope = database.Services.CreateScope();
        var quizzes = scope.ServiceProvider.GetRequiredService<QuizStore>();
        var game = scope.ServiceProvider.GetRequiredService<GamificationStore>();
        var db = scope.ServiceProvider.GetRequiredService<EduVerseDb>();
        var nora = await database.UserIdAsync("nora@eduverse.demo");
        var before = await game.TotalXpAsync(nora);

        var q = await quizzes.StartAsync(nora, Area.Chemistry, Level.HighSchool, QuizMode.Practice, TeachLanguage.Spanish);
        Assert.Equal(1, q.Number);
        Assert.Equal(AdaptiveQuiz.Length, q.Length);
        AnswerResult? last = null;
        var prompts = new HashSet<string>();
        for (var i = 0; i < AdaptiveQuiz.Length; i++)
        {
            var attempt = await db.QuizAttempts.AsNoTracking().SingleAsync(a => a.Id == q.AttemptId);
            // Chemistry now mixes in all 4 drill kinds (task #72's True/False, plus task #73's fill-blank/reorder/
            // match): every 4th Practice question can be one instead of a classic question, so detect the kind and
            // always answer it right, via its own stored answer key, to keep this test's "perfect score" asserts.
            // Drills aren't deduplicated within an attempt the way classic questions are (no "already seen" tracking
            // yet), so — unlike the classic branch below — a drill's prompt isn't checked for repeats here.
            if (attempt.CurrentExerciseJson is { } exerciseJson)
            {
                var exercise = System.Text.Json.JsonSerializer.Deserialize<PracticeExercise>(exerciseJson, Web)!;
                last = await (exercise.Kind switch
                {
                    ExerciseKind.FillBlank => quizzes.AnswerFillBlankAsync(nora, q.AttemptId, exercise.Answer!, 1, 0, TeachLanguage.Spanish),
                    ExerciseKind.Reorder => quizzes.AnswerReorderAsync(nora, q.AttemptId, exercise.CorrectOrder!, 1, 0, TeachLanguage.Spanish),
                    ExerciseKind.Match => quizzes.AnswerMatchAsync(nora, q.AttemptId, exercise.Pairs!.ToDictionary(p => p.Left, p => p.Right), 1, 0, TeachLanguage.Spanish),
                    _ => quizzes.AnswerTrueFalseAsync(nora, q.AttemptId, exercise.CorrectBool!.Value, TeachLanguage.Spanish),
                });
            }
            else
            {
                var current = System.Text.Json.JsonSerializer.Deserialize<QuizQuestion>(attempt.CurrentJson, Web)!;
                Assert.True(prompts.Add(current.Prompt + string.Concat(current.Options)), "no repeated question in a test");
                last = await quizzes.AnswerAsync(nora, q.AttemptId, current.CorrectIndex, TeachLanguage.Spanish);
            }

            Assert.NotNull(last);
            Assert.True(last.Correct);
            if (last.Next is { } next)
            {
                Assert.Equal(i + 2, next.Number);
                Assert.True(next.Difficulty >= 2); // right answers make it harder, never easier
            }
        }

        Assert.NotNull(last!.Summary);
        Assert.Equal((10, 10, 3), (last.Summary.Correct, last.Summary.Length, last.Summary.Stars));
        Assert.Null(await quizzes.AnswerAsync(nora, q.AttemptId, 0, TeachLanguage.Spanish)); // finished
        Assert.True(await game.TotalXpAsync(nora) - before >= 10 * 10 + Xp.TestFinished + Xp.PerfectTest);
        Assert.True(await db.TopicSkills.AnyAsync(s => s.UserId == nora && s.Area == Area.Chemistry && s.PKnown > 0.5));
        Assert.Contains("Perfect", await db.BadgeAwards.Where(b => b.UserId == nora).Select(b => b.Key).ToListAsync());
        var level = await db.SubjectLevels.SingleAsync(s => s.UserId == nora && s.Area == Area.Chemistry);
        Assert.Equal((Level.HighSchool, false), (level.Level, level.Placed));
    }

    [Fact]
    public async Task Wrong_answers_make_the_test_easier_and_other_students_cannot_answer()
    {
        using var scope = database.Services.CreateScope();
        var quizzes = scope.ServiceProvider.GetRequiredService<QuizStore>();
        var leo = await database.UserIdAsync("leo@eduverse.demo");
        var q = await quizzes.StartAsync(leo, Area.Mathematics, Level.Secondary2, QuizMode.Practice, TeachLanguage.English);
        Assert.Null(await quizzes.AnswerAsync(await database.UserIdAsync("alex@eduverse.demo"), q.AttemptId, 0, TeachLanguage.English));
        Assert.Null(await quizzes.AnswerAsync(leo, q.AttemptId, 7, TeachLanguage.English));

        var db = scope.ServiceProvider.GetRequiredService<EduVerseDb>();
        var attempt = await db.QuizAttempts.AsNoTracking().SingleAsync(a => a.Id == q.AttemptId);
        var current = System.Text.Json.JsonSerializer.Deserialize<QuizQuestion>(attempt.CurrentJson, Web)!;
        var result = await quizzes.AnswerAsync(leo, q.AttemptId, (current.CorrectIndex + 1) % 4, TeachLanguage.English);
        Assert.False(result!.Correct);
        Assert.Equal(1, result.Next!.Difficulty);
        Assert.Equal(Xp.Wrong, result.Reward.Xp);
        // A wrong answer now says what the right one was (never shown before) and still carries the question's own reasoning.
        Assert.Contains(current.Answer, result.Explanation, StringComparison.Ordinal);
        Assert.Contains(current.Explanation.TrimEnd('.'), result.Explanation, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task A_right_answer_explains_why_it_is_right_and_a_wrong_one_names_the_right_answer()
    {
        using var scope = database.Services.CreateScope();
        var quizzes = scope.ServiceProvider.GetRequiredService<QuizStore>();
        var db = scope.ServiceProvider.GetRequiredService<EduVerseDb>();
        var nora = await database.UserIdAsync("nora@eduverse.demo");
        var q = await quizzes.StartAsync(nora, Area.Mathematics, Level.Primary2, QuizMode.Practice, TeachLanguage.Spanish);
        var attempt = await db.QuizAttempts.AsNoTracking().SingleAsync(a => a.Id == q.AttemptId);
        var current = System.Text.Json.JsonSerializer.Deserialize<QuizQuestion>(attempt.CurrentJson, Web)!;

        var right = await quizzes.AnswerAsync(nora, q.AttemptId, current.CorrectIndex, TeachLanguage.Spanish);
        Assert.True(right!.Correct);
        Assert.NotEqual(current.Explanation, right.Explanation); // framed, not the bare bank text
        Assert.Contains(current.Explanation.TrimEnd('.'), right.Explanation, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task The_tutor_context_remembers_the_student_s_most_recent_quiz_mistakes()
    {
        using var scope = database.Services.CreateScope();
        var quizzes = scope.ServiceProvider.GetRequiredService<QuizStore>();
        var db = scope.ServiceProvider.GetRequiredService<EduVerseDb>();
        var learning = scope.ServiceProvider.GetRequiredService<LearningStore>();
        var sara = await database.UserIdAsync("sara@eduverse.demo");

        async Task<string> MissOneAsync(Area area, Level level)
        {
            var q = await quizzes.StartAsync(sara, area, level, QuizMode.Practice, TeachLanguage.English);
            var attempt = await db.QuizAttempts.AsNoTracking().SingleAsync(a => a.Id == q.AttemptId);
            var current = System.Text.Json.JsonSerializer.Deserialize<QuizQuestion>(attempt.CurrentJson, Web)!;
            await quizzes.AnswerAsync(sara, q.AttemptId, (current.CorrectIndex + 1) % 4, TeachLanguage.English);
            return current.Topic;
        }

        await MissOneAsync(Area.Geography, Level.Secondary1);
        var lastTopic = await MissOneAsync(Area.History, Level.Secondary1);

        var mistakes = await learning.RecentQuizMistakesAsync(sara);
        Assert.True(mistakes.Count is > 0 and <= LearningStore.ContextMisconceptions);
        Assert.StartsWith(lastTopic + ":", mistakes[0], StringComparison.Ordinal); // most recent first
    }

    [Fact]
    public async Task A_placement_test_saves_the_level_found()
    {
        using var scope = database.Services.CreateScope();
        var quizzes = scope.ServiceProvider.GetRequiredService<QuizStore>();
        var db = scope.ServiceProvider.GetRequiredService<EduVerseDb>();
        var alex = await database.UserIdAsync("alex@eduverse.demo");
        var q = await quizzes.StartAsync(alex, Area.Economics, Level.EarlyYears, QuizMode.Placement, TeachLanguage.French);
        Assert.True(q.Level >= Level.Secondary2);

        AnswerResult? r;
        do
        {
            var attempt = await db.QuizAttempts.AsNoTracking().SingleAsync(a => a.Id == q.AttemptId);
            // knows everything up to the bachelor level
            var right = attempt.Level <= Level.HighSchool;
            // Placement now mixes in drills too (task #74), but never True/False (PerLevel's binary search can't
            // absorb a 50%-guess coin-flip on a decisive question), so only FillBlank/Reorder/Match can appear here.
            if (attempt.CurrentExerciseJson is { } exerciseJson)
            {
                var exercise = System.Text.Json.JsonSerializer.Deserialize<PracticeExercise>(exerciseJson, Web)!;
                Assert.NotEqual(ExerciseKind.TrueFalse, exercise.Kind);
                r = await (exercise.Kind switch
                {
                    ExerciseKind.FillBlank => quizzes.AnswerFillBlankAsync(alex, q.AttemptId, right ? exercise.Answer! : exercise.Answer + "-wrong", 1, 0, TeachLanguage.French),
                    ExerciseKind.Reorder => quizzes.AnswerReorderAsync(alex, q.AttemptId, right ? exercise.CorrectOrder! : exercise.CorrectOrder!.Reverse().ToList(), 1, 0, TeachLanguage.French),
                    _ => quizzes.AnswerMatchAsync(alex, q.AttemptId, MatchAnswer(exercise.Pairs!, right), 1, 0, TeachLanguage.French),
                });
            }
            else
            {
                var current = System.Text.Json.JsonSerializer.Deserialize<QuizQuestion>(attempt.CurrentJson, Web)!;
                r = await quizzes.AnswerAsync(alex, q.AttemptId, right ? current.CorrectIndex : (current.CorrectIndex + 1) % 4, TeachLanguage.French);
            }
        }
        while (r!.Summary is null);

        Assert.Equal(Level.Bachelor, r.Summary.Placed);
        var level = await db.SubjectLevels.SingleAsync(s => s.UserId == alex && s.Area == Area.Economics);
        Assert.Equal((Level.Bachelor, true), (level.Level, level.Placed));
    }

    [Fact]
    public async Task Demo_class_has_rankings_teams_streaks_and_quests()
    {
        using var scope = database.Services.CreateScope();
        var game = scope.ServiceProvider.GetRequiredService<GamificationStore>();
        var sara = await database.UserIdAsync("sara@eduverse.demo");
        var summary = await game.SummaryAsync(sara);
        Assert.True(summary.Streak >= 5);
        Assert.True(summary.Xp > 500);
        Assert.Equal(3, summary.Quests.Count);
        Assert.Equal(35, summary.Calendar.Count);
        Assert.Contains(summary.Badges, b => b.Badge.Key == "Duelist" && b.EarnedAt is not null);
        Assert.False(string.IsNullOrEmpty(summary.Profile.Nickname));
        Assert.DoesNotContain("Sara", summary.Profile.Nickname, StringComparison.Ordinal);

        var board = Assert.Single(await game.BoardsAsync(sara));
        Assert.Equal("Maths 2B", board.Name);
        Assert.Equal(4, board.Students.Count);
        Assert.Single(board.Students, s => s.Me);
        Assert.True(board.Teams.Count >= 2);
        Assert.Equal(board.Students.OrderByDescending(s => s.Xp).Select(s => s.Xp), board.Students.Select(s => s.Xp));
    }

    [Fact]
    public async Task Students_who_turn_competition_off_disappear_from_rankings_and_duels()
    {
        using var scope = database.Services.CreateScope();
        var game = scope.ServiceProvider.GetRequiredService<GamificationStore>();
        var leo = await database.UserIdAsync("leo@eduverse.demo");
        var nora = await database.UserIdAsync("nora@eduverse.demo");
        var leoProfile = await game.ProfileAsync(leo);
        await game.SaveSettingsAsync(leo, Team.Bears, compete: false, newNickname: false);

        var board = (await game.BoardsAsync(nora))[0];
        Assert.DoesNotContain(board.Students, s => s.UserId == leo);
        Assert.DoesNotContain(await game.RivalsAsync(nora), r => r.UserId == leo);
        Assert.Equal(GamificationStore.ChallengeResult.NotAllowed, (await game.ChallengeAsync(nora, leo, Area.Mathematics, Level.Primary3)).Result);
        Assert.Contains((await game.BoardsAsync(leo))[0].Students, s => s.Me); // they still see themselves
        await game.SaveSettingsAsync(leo, leoProfile.Team, compete: true, newNickname: false);
    }

    [Fact]
    public async Task A_student_can_pick_their_own_nickname_and_avatar_from_the_safe_options()
    {
        using var scope = database.Services.CreateScope();
        var game = scope.ServiceProvider.GetRequiredService<GamificationStore>();
        var leo = await database.UserIdAsync("leo@eduverse.demo");
        var before = await game.ProfileAsync(leo);

        await game.SaveSettingsAsync(leo, before.Team, compete: before.Compete, newNickname: false, animal: 2, word: 5);

        var after = await game.ProfileAsync(leo);
        var (expectedName, expectedEmoji) = Nicknames.Build(2, 5, before.Language);
        Assert.Equal(expectedEmoji, after.Avatar);
        Assert.Equal(expectedName, after.Nickname);

        // picking again with the same indexes doesn't drift: it rebuilds the exact same nickname/avatar.
        await game.SaveSettingsAsync(leo, after.Team, compete: after.Compete, newNickname: false, animal: 2, word: 5);
        var again = await game.ProfileAsync(leo);
        Assert.Equal(expectedEmoji, again.Avatar);
        Assert.Equal(expectedName, again.Nickname);

        await game.SaveSettingsAsync(leo, before.Team, compete: before.Compete, newNickname: false, animal: Nicknames.AnimalIndexOf(before.Avatar), word: Nicknames.WordIndexOf(before.Nickname, before.Language));
    }

    [Fact]
    public async Task A_duel_gives_both_students_the_same_questions_and_a_winner()
    {
        using var scope = database.Services.CreateScope();
        var game = scope.ServiceProvider.GetRequiredService<GamificationStore>();
        var quizzes = scope.ServiceProvider.GetRequiredService<QuizStore>();
        var db = scope.ServiceProvider.GetRequiredService<EduVerseDb>();
        var options = Web;
        var nora = await database.UserIdAsync("nora@eduverse.demo");
        var alex = await database.UserIdAsync("alex@eduverse.demo");
        var (result, duel) = await game.ChallengeAsync(nora, alex, Area.Geography, Level.Primary3);
        Assert.Equal(GamificationStore.ChallengeResult.Created, result);

        async Task<List<string>> PlayAsync(string userId, int right)
        {
            var prompts = new List<string>();
            var q = await quizzes.StartAsync(userId, Area.Mathematics, Level.Doctorate, QuizMode.Duel, TeachLanguage.English, duel!.Id);
            Assert.Equal((Area.Geography, Level.Primary3, AdaptiveQuiz.DuelLength), (q.Area, q.Level, q.Length));
            for (var i = 0; i < AdaptiveQuiz.DuelLength; i++)
            {
                var attempt = await db.QuizAttempts.AsNoTracking().SingleAsync(a => a.Id == q.AttemptId);
                var wantsRight = i < right;
                // Duel also mixes in drills now (task #74): same seed for both duelists means the same drill (or
                // classic question) lands at the same index for each, so the fairness check below still holds.
                if (attempt.CurrentExerciseJson is { } exerciseJson)
                {
                    var exercise = System.Text.Json.JsonSerializer.Deserialize<PracticeExercise>(exerciseJson, options)!;
                    prompts.Add(exercise.Prompt);
                    await (exercise.Kind switch
                    {
                        ExerciseKind.FillBlank => quizzes.AnswerFillBlankAsync(userId, q.AttemptId, wantsRight ? exercise.Answer! : exercise.Answer + "-wrong", 1, 0, TeachLanguage.English),
                        ExerciseKind.Reorder => quizzes.AnswerReorderAsync(userId, q.AttemptId, wantsRight ? exercise.CorrectOrder! : exercise.CorrectOrder!.Reverse().ToList(), 1, 0, TeachLanguage.English),
                        ExerciseKind.Match => quizzes.AnswerMatchAsync(userId, q.AttemptId, MatchAnswer(exercise.Pairs!, wantsRight), 1, 0, TeachLanguage.English),
                        _ => quizzes.AnswerTrueFalseAsync(userId, q.AttemptId, wantsRight ? exercise.CorrectBool!.Value : !exercise.CorrectBool!.Value, TeachLanguage.English),
                    });
                }
                else
                {
                    var current = System.Text.Json.JsonSerializer.Deserialize<QuizQuestion>(attempt.CurrentJson, options)!;
                    prompts.Add(current.Prompt);
                    await quizzes.AnswerAsync(userId, q.AttemptId, wantsRight ? current.CorrectIndex : (current.CorrectIndex + 1) % 4, TeachLanguage.English);
                }
            }

            return prompts;
        }

        var noraPrompts = await PlayAsync(nora, 4);
        Assert.Null(await game.DuelForAsync(nora, duel!.Id)); // played already
        var alexXp = await game.TotalXpAsync(alex);
        var alexPrompts = await PlayAsync(alex, 2);
        Assert.Equal(noraPrompts, alexPrompts);
        var finished = await db.Duels.AsNoTracking().SingleAsync(d => d.Id == duel.Id);
        Assert.Equal((DuelStatus.Finished, 4, 2), (finished.Status, finished.ChallengerScore!.Value, finished.OpponentScore!.Value));
        Assert.True(await db.XpEvents.AnyAsync(x => x.UserId == nora && x.Reason == "duel" && x.Amount == Xp.DuelWin));
        Assert.True(await game.TotalXpAsync(alex) > alexXp);
    }

    [Fact]
    public async Task A_new_test_does_not_repeat_the_questions_already_seen_and_is_saved_for_review()
    {
        using var scope = database.Services.CreateScope();
        var quizzes = scope.ServiceProvider.GetRequiredService<QuizStore>();
        var db = scope.ServiceProvider.GetRequiredService<EduVerseDb>();
        var iker = await database.UserIdAsync("iker@eduverse.demo");

        // Physics now mixes in all 4 drill kinds (task #72's True/False, plus task #73's fill-blank/reorder/match):
        // every 4th Practice question can be one instead of a classic question. A drill doesn't log a QuizAnswer
        // history row yet (documented limitation from task #69), so it's skipped here for the "no repeat"/"review"
        // checks below, which are specifically about the classic question bank — the drill is still answered
        // (correctly, via its own stored answer key), so the quiz keeps progressing normally through it.
        async Task<(string Id, List<string> Keys, List<int> Numbers)> PlayAsync()
        {
            var q = await quizzes.StartAsync(iker, Area.Physics, Level.Secondary2, QuizMode.Practice, TeachLanguage.English);
            var keys = new List<string>();
            var numbers = new List<int>();
            for (var i = 0; i < AdaptiveQuiz.Length; i++)
            {
                var attempt = await db.QuizAttempts.AsNoTracking().SingleAsync(a => a.Id == q.AttemptId);
                if (attempt.CurrentExerciseJson is { } exerciseJson)
                {
                    var exercise = System.Text.Json.JsonSerializer.Deserialize<PracticeExercise>(exerciseJson, Web)!;
                    await (exercise.Kind switch
                    {
                        ExerciseKind.FillBlank => quizzes.AnswerFillBlankAsync(iker, q.AttemptId, exercise.Answer!, 1, 0, TeachLanguage.English),
                        ExerciseKind.Reorder => quizzes.AnswerReorderAsync(iker, q.AttemptId, exercise.CorrectOrder!, 1, 0, TeachLanguage.English),
                        ExerciseKind.Match => quizzes.AnswerMatchAsync(iker, q.AttemptId, exercise.Pairs!.ToDictionary(p => p.Left, p => p.Right), 1, 0, TeachLanguage.English),
                        _ => quizzes.AnswerTrueFalseAsync(iker, q.AttemptId, exercise.CorrectBool!.Value, TeachLanguage.English),
                    });
                }
                else
                {
                    var current = System.Text.Json.JsonSerializer.Deserialize<QuizQuestion>(attempt.CurrentJson, Web)!;
                    keys.Add(QuizStore.KeyOf(current));
                    numbers.Add(i + 1); // attempt.Answered counts drills too, so a classic answer's own Number is just its 1-based position
                    await quizzes.AnswerAsync(iker, q.AttemptId, i % 4, TeachLanguage.English);
                }
            }

            return (q.AttemptId, keys, numbers);
        }

        var first = await PlayAsync();
        var second = await PlayAsync();
        Assert.Empty(first.Keys.Intersect(second.Keys));

        var history = await quizzes.HistoryAsync(iker, Area.Physics);
        Assert.Equal([second.Id, first.Id], history.Take(2).Select(h => h.Id));
        var review = await quizzes.ReviewAsync(iker, first.Id);
        Assert.NotNull(review);
        Assert.Equal(first.Numbers.Count, review.Value.Answers.Count);
        Assert.Equal(first.Numbers, review.Value.Answers.Select(a => a.Number));
        Assert.All(review.Value.Answers, a =>
        {
            Assert.Equal(4, QuizStore.Options(a).Count);
            Assert.Equal(a.Choice == a.CorrectIndex, a.Correct);
            Assert.False(string.IsNullOrEmpty(a.Prompt));
        });
        Assert.Null(await quizzes.ReviewAsync(await database.UserIdAsync("sara@eduverse.demo"), first.Id)); // not hers
    }

    [Fact]
    public async Task Changing_the_level_applies_it_to_every_subject()
    {
        using var scope = database.Services.CreateScope();
        var quizzes = scope.ServiceProvider.GetRequiredService<QuizStore>();
        var elena = await database.UserIdAsync("elena@eduverse.demo");
        await quizzes.SetLevelAsync(elena, Area.History, Level.Primary2, placed: true);

        await quizzes.ApplyLevelToAllAsync(elena, Level.Bachelor);
        var progress = await quizzes.ProgressAsync(elena, Level.Bachelor);
        Assert.All(progress, p => Assert.Equal(Level.Bachelor, p.Level));
        Assert.All(progress, p => Assert.False(p.Placed));

        await quizzes.ApplyLevelToAllAsync(elena, Level.Primary2);
        progress = await quizzes.ProgressAsync(elena, Level.Primary2);
        Assert.Equal(Level.Secondary2, progress.Single(p => p.Area == Area.Economics).Level); // starts later
        Assert.Equal(Level.Primary2, progress.Single(p => p.Area == Area.History).Level);
    }

    [Fact]
    public async Task The_kid_account_starts_in_early_years()
    {
        using var scope = database.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EduVerseDb>();
        var mia = await database.UserIdAsync(DemoSeeder.KidEmail);
        var profile = await db.StudentProfiles.SingleAsync(p => p.UserId == mia);
        Assert.Equal(Level.EarlyYears, profile.Level);
        Assert.True(Levels.IsKids(profile.Level));
        var progress = await scope.ServiceProvider.GetRequiredService<QuizStore>().ProgressAsync(mia, profile.Level);
        Assert.Equal(Catalog.Areas.Count, progress.Count);
        Assert.True(progress.Single(p => p.Area == Area.Mathematics).Answers > 0);
    }
}
