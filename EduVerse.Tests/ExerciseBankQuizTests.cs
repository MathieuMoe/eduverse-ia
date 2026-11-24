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
    [Fact]
    public void No_content_yet_for_a_subject_other_than_mathematics()
    {
        Assert.Null(ExerciseBank.TrueFalse(Area.Physics, Level.Primary3, TeachLanguage.English, 1));
    }

    [Theory]
    [InlineData(Level.EarlyYears)]
    [InlineData(Level.Primary3)]
    [InlineData(Level.HighSchool)]
    public void Mathematics_has_a_true_false_drill_at_every_level_bucket(Level level)
    {
        var exercise = ExerciseBank.TrueFalse(Area.Mathematics, level, TeachLanguage.Spanish, 7);
        Assert.NotNull(exercise);
        Assert.Equal(ExerciseKind.TrueFalse, exercise!.Kind);
        Assert.NotNull(exercise.CorrectBool);
        Assert.False(string.IsNullOrWhiteSpace(exercise.Prompt));
        Assert.False(string.IsNullOrWhiteSpace(exercise.Explanation));
    }

    [Fact]
    public void The_same_seed_always_gives_the_same_drill()
    {
        var a = ExerciseBank.TrueFalse(Area.Mathematics, Level.Primary3, TeachLanguage.French, 42);
        var b = ExerciseBank.TrueFalse(Area.Mathematics, Level.Primary3, TeachLanguage.French, 42);
        Assert.Equal(a, b);
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
}
