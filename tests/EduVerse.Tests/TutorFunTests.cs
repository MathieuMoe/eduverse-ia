using System.Text.Json;
using EduVerse.Core.Catalog;
using EduVerse.Core.Common;
using EduVerse.Core.Gamification;
using EduVerse.Core.Learning;
using EduVerse.Infrastructure.Data;
using EduVerse.Infrastructure.Learning;
using EduVerse.Infrastructure.Quizzes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EduVerse.Tests;

public class TutorFunRuleTests
{
    [Fact]
    public void Hints_remove_wrong_options_then_show_the_method_without_the_answer()
    {
        for (var seed = 0; seed < 200; seed++)
        {
            var q = QuestionBanks.Generate(Area.Physics, Level.HighSchool, 2, seed, TeachLanguage.Spanish);
            var one = Hints.For(q, 1, seed, TeachLanguage.Spanish);
            var two = Hints.For(q, 2, seed, TeachLanguage.Spanish);
            var three = Hints.For(q, 3, seed, TeachLanguage.Spanish);
            Assert.Single(one.Removed);
            Assert.Equal(2, two.Removed.Count);
            Assert.Equal(one.Removed[0], two.Removed[0]); // the ladder keeps what was removed
            Assert.DoesNotContain(q.CorrectIndex, two.Removed);
            Assert.Null(two.Text);
            Assert.NotNull(three.Text);
            Assert.DoesNotContain(q.Answer, three.Text, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void A_fact_hint_gives_only_the_first_letter()
    {
        var q = new QuizQuestion("t", 1, "Capital of Peru?", ["Lima", "Quito", "Bogotá", "Caracas"], 0, "The right answer is: Lima.");
        Assert.Equal("Empieza por «L…».", Hints.Method(q, TeachLanguage.Spanish));
        var method = new QuizQuestion("t", 1, "3 × 4?", ["12", "7", "34", "1"], 0, "Multiply: three groups of four objects make 12 objects in total.");
        Assert.Equal("Multiply: three groups of four objects make ▢ objects in total.", Hints.Method(method, TeachLanguage.English));
    }

    [Fact]
    public void Checking_evo_keeps_one_right_option_and_evo_is_right_sometimes()
    {
        var right = 0;
        for (var seed = 0; seed < 300; seed++)
        {
            var original = QuestionBanks.Generate(Area.Mathematics, Level.Primary2, 2, seed, TeachLanguage.English);
            var q = EvoQuestions.Check(original, new Random(seed), TeachLanguage.English);
            Assert.Equal(4, q.Options.Distinct().Count());
            Assert.NotNull(q.Evo);
            if (q.Evo == original.Answer)
            {
                right++;
                Assert.Equal(EvoQuestions.EvoIsRight.En, q.Answer);
            }
            else
            {
                Assert.Equal(original.Answer, q.Answer);
                Assert.DoesNotContain(q.Evo, q.Options); // his wrong answer is not an option: the student must pick the right one
            }
        }

        Assert.InRange(right, 60, 140);
    }

    [Fact]
    public void Evo_notebook_makes_the_students_own_mistakes()
    {
        var mine = 0;
        for (var seed = 0; seed < 200; seed++)
        {
            var q = EvoNotebook.Create([FractionExercises.AddAcross], new Random(seed), TeachLanguage.Spanish);
            Assert.Equal(4, q.Options.Distinct().Count());
            Assert.Equal("math.fraction", q.Topic);
            Assert.NotNull(q.Evo);
            mine += q.Answer == EvoNotebook.Describe(FractionExercises.AddAcross, TeachLanguage.Spanish) ? 1 : 0;
        }

        Assert.True(mine > 80, $"Evo made the student's mistake {mine} times out of 200");
        Assert.Equal(13, EvoNotebook.Codes.Count);
    }

    [Fact]
    public void Spaced_repetition_and_confidence()
    {
        var now = new DateTimeOffset(2026, 11, 16, 10, 0, 0, TimeSpan.Zero);
        Assert.Equal((1, now.AddDays(1)), Spaced.Next(0, true, 1, now));
        Assert.Equal((4, now.AddDays(7)), Spaced.Next(3, true, 1, now));
        Assert.Equal((6, now.AddDays(30)), Spaced.Next(6, true, 2, now));
        Assert.Equal((0, now.AddDays(1)), Spaced.Next(4, false, 1, now));
        Assert.Equal((0, now), Spaced.Next(4, false, Spaced.Sure, now)); // sure but wrong: today
        Assert.Null(Spaced.Calibration(3, 3));
        Assert.Equal(80, Spaced.Calibration(10, 8));
    }

    [Fact]
    public void Hints_lower_the_xp_and_sure_right_answers_earn_a_bonus()
    {
        Assert.Equal(15, Xp.ForAnswer(true, 2, 1));
        Assert.Equal(11, Xp.ForAnswer(true, 2, 1, hints: 1));
        Assert.Equal(3, Xp.ForAnswer(true, 2, 1, hints: 3));
        Assert.Equal(15 + Xp.SureBonus, Xp.ForAnswer(true, 2, 1, sure: true));
        Assert.Equal(Xp.Wrong, Xp.ForAnswer(false, 3, 0, 0, sure: true));
        Assert.Equal(11, Xp.ForAnswer(true, 2, 5, hints: 1)); // no run bonus with hints
        Assert.True(Quests.Done(new Quest(QuestKind.Review, 1, 30), new DayStats(0, 0, 0, 0, 0, 0, 0, 0, 0, Reviews: 1)));
    }
}

public class TutorFunPersistenceTests(Database database) : IClassFixture<Database>
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    private static async Task<QuizQuestion> CurrentAsync(EduVerseDb db, string id) =>
        JsonSerializer.Deserialize<QuizQuestion>((await db.QuizAttempts.AsNoTracking().SingleAsync(a => a.Id == id)).CurrentJson, Web)!;

    [Fact]
    public async Task Hints_are_counted_on_the_server_and_lower_the_xp()
    {
        using var scope = database.Services.CreateScope();
        var quizzes = scope.ServiceProvider.GetRequiredService<QuizStore>();
        var db = scope.ServiceProvider.GetRequiredService<EduVerseDb>();
        var omar = await database.UserIdAsync("omar@eduverse.demo");
        var q = await quizzes.StartAsync(omar, Area.Chemistry, Level.Secondary2, QuizMode.Practice, TeachLanguage.English);
        var h1 = await quizzes.HintAsync(omar, q.AttemptId, TeachLanguage.English);
        var h2 = await quizzes.HintAsync(omar, q.AttemptId, TeachLanguage.English);
        Assert.Equal((1, 2), (h1!.Level, h2!.Level));
        Assert.Null(await quizzes.HintAsync(await database.UserIdAsync("alex@eduverse.demo"), q.AttemptId, TeachLanguage.English));

        var current = await CurrentAsync(db, q.AttemptId);
        var result = await quizzes.AnswerAsync(omar, q.AttemptId, current.CorrectIndex, 1, TeachLanguage.English);
        Assert.Equal(2, result!.Hints);
        var xp = await db.XpEvents.AsNoTracking().Where(x => x.UserId == omar && x.Reason == "answer").OrderByDescending(x => x.Id).FirstAsync();
        Assert.Equal(Xp.ForAnswer(true, current.Difficulty, 1, 2), xp.Amount);
        Assert.Equal(0, result.Next!.HintsUsed); // the ladder starts again on the next question
        var saved = await db.QuizAnswers.AsNoTracking().Where(a => a.AttemptId == q.AttemptId).SingleAsync();
        Assert.Equal(2, saved.Hints);
    }

    [Fact]
    public async Task Sure_but_wrong_puts_the_topic_in_todays_review()
    {
        using var scope = database.Services.CreateScope();
        var quizzes = scope.ServiceProvider.GetRequiredService<QuizStore>();
        var db = scope.ServiceProvider.GetRequiredService<EduVerseDb>();
        var noa = await database.UserIdAsync("noa@eduverse.demo");
        Assert.Empty(await quizzes.ReviewPlanAsync(noa)); // nothing practised in the catalog yet

        var q = await quizzes.StartAsync(noa, Area.Geography, Level.Primary3, QuizMode.Practice, TeachLanguage.Spanish);
        var current = await CurrentAsync(db, q.AttemptId);
        var result = await quizzes.AnswerAsync(noa, q.AttemptId, (current.CorrectIndex + 1) % 4, Spaced.Sure, TeachLanguage.Spanish);
        Assert.Equal(Spaced.Sure, result!.Confidence);
        var plan = await quizzes.ReviewPlanAsync(noa);
        Assert.Contains(plan, p => p.Topic == current.Topic && p.Area == Area.Geography);

        // The review asks that topic, then sends it to a later box when it is right
        var review = await quizzes.StartAsync(noa, Area.Mathematics, Level.EarlyYears, QuizMode.Review, TeachLanguage.Spanish);
        Assert.Equal(QuizMode.Review, review.Mode);
        Assert.Equal(plan.Count, review.Length);
        Assert.Equal(plan[0].Area, review.Area);
        var asked = await CurrentAsync(db, review.AttemptId);
        Assert.Equal(plan[0].Topic, asked.Topic);
        await quizzes.AnswerAsync(noa, review.AttemptId, asked.CorrectIndex, 1, TeachLanguage.Spanish);
        var skill = await db.TopicSkills.AsNoTracking().SingleAsync(s => s.UserId == noa && s.Area == plan[0].Area && s.Level == plan[0].Level && s.Topic == plan[0].Topic);
        Assert.True(skill.DueAt > quizzes.Now);
        Assert.Equal(1, skill.Box);
    }

    [Fact]
    public async Task Evo_notebook_uses_the_mistakes_the_student_makes_with_the_tutor()
    {
        using var scope = database.Services.CreateScope();
        var quizzes = scope.ServiceProvider.GetRequiredService<QuizStore>();
        var db = scope.ServiceProvider.GetRequiredService<EduVerseDb>();
        var sara = await database.UserIdAsync("sara@eduverse.demo");
        var mistakes = await quizzes.MistakesAsync(sara);
        Assert.Equal(FractionExercises.AddAcross, mistakes[0]);

        var q = await quizzes.StartAsync(sara, Area.History, Level.Doctorate, QuizMode.Notebook, TeachLanguage.Spanish);
        Assert.Equal((Area.Mathematics, 6), (q.Area, q.Length));
        Assert.NotNull(q.Evo);
        Assert.Equal("¿Qué ha hecho mal Evo?", q.EvoTask);
        for (var i = 0; i < 6; i++)
        {
            var current = await CurrentAsync(db, q.AttemptId);
            await quizzes.AnswerAsync(sara, q.AttemptId, current.CorrectIndex, 1, TeachLanguage.Spanish);
        }

        Assert.Equal(6, await db.QuizAnswers.CountAsync(a => a.AttemptId == q.AttemptId && a.Correct && a.Evo != null));
    }

    [Fact]
    public async Task Check_evo_mode_works_in_every_subject()
    {
        using var scope = database.Services.CreateScope();
        var quizzes = scope.ServiceProvider.GetRequiredService<QuizStore>();
        var db = scope.ServiceProvider.GetRequiredService<EduVerseDb>();
        var hugo = await database.UserIdAsync("hugo@eduverse.demo");
        foreach (var area in Enum.GetValues<Area>())
        {
            var q = await quizzes.StartAsync(hugo, area, Level.HighSchool, QuizMode.Evo, TeachLanguage.French);
            Assert.NotNull(q.Evo);
            var current = await CurrentAsync(db, q.AttemptId);
            Assert.Contains(current.Options, o => o == EvoQuestions.EvoIsRight.Fr);
        }
    }

    [Fact]
    public async Task The_tutor_has_a_hint_ladder_too()
    {
        using var scope = database.Services.CreateScope();
        var learning = scope.ServiceProvider.GetRequiredService<LearningStore>();
        var db = scope.ServiceProvider.GetRequiredService<EduVerseDb>();
        var leo = await database.UserIdAsync("leo@eduverse.demo");
        var exercise = FractionExercises.Create("compare", 2, new Random(3), TeachLanguage.English);
        var id = await learning.AddPendingAsync(leo, Core.Curriculum.BuiltInSubjects.FractionsKey, exercise, diagnostic: false, served: true);

        var h1 = await learning.HintAsync(leo, id);
        var h2 = await learning.HintAsync(leo, id);
        Assert.Equal((1, exercise.Hint), (h1!.Level, h1.Text));
        Assert.Empty(h1.Removed);
        Assert.Single(h2!.Removed);
        Assert.DoesNotContain(exercise.CorrectIndex, h2.Removed);
        var outcome = await learning.AnswerAsync(leo, id, exercise.CorrectIndex);
        Assert.Equal(2, outcome!.Hints);
    }
}
