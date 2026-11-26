using System.Text.Json;
using EduVerse.Core.Catalog;
using EduVerse.Core.Common;
using EduVerse.Core.Curriculum;
using EduVerse.Core.Gamification;
using EduVerse.Infrastructure.Data;
using EduVerse.Infrastructure.Gamification;
using EduVerse.Infrastructure.Quizzes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EduVerse.Tests;

public class FreshLessonRulesTests
{
    [Fact]
    public void The_stage_of_a_path_lesson_sets_its_difficulty()
    {
        Assert.Equal([1, 2, 3, 3], new[] { 0, 1, 2, 5 }.Select(PathLesson.Difficulty));
    }

    [Fact]
    public void The_prompt_of_a_question_comes_back_from_its_stored_key()
    {
        var q = new QuizQuestion("t", 1, "Is 1|2 a fraction?", ["a", "b", "c", "d"], 0, "");
        Assert.Equal("Is 1|2 a fraction?", PathLesson.PromptOfKey(QuizStore.KeyOf(q)));
        Assert.Equal(PathLesson.Normalize("What is 2+2?"), PathLesson.Normalize("what is 2 + 2"));
    }

    [Fact]
    public void The_ai_lesson_lists_what_to_avoid_and_its_questions_are_checked_tagged_and_new()
    {
        var examples = new[] { new QuizQuestion("geo", 1, "Example question", ["a", "b", "c", "d"], 0, "") };
        var prompt = PathLesson.Build(Area.Geography, Level.Primary3, "geo", 2, PathLesson.AngleFor(3), ["Old question one"], examples, TeachLanguage.French);
        Assert.Contains("Old question one", prompt.User, StringComparison.Ordinal);
        Assert.Contains("Example question", prompt.User, StringComparison.Ordinal);
        Assert.Contains(PathLesson.AngleFor(3), prompt.System, StringComparison.Ordinal);
        Assert.Contains("lesson 2 of 3", prompt.System, StringComparison.Ordinal);

        var json = JsonDocument.Parse("""
        {"questions":[
          {"question":"Old question one!","options":["a","b","c","d"],"correct":0,"difficulty":2,"explanation":"x"},
          {"question":"New A","options":["a","b","c","d"],"correct":1,"difficulty":2,"explanation":"x"},
          {"question":"New A","options":["a","b","c","e"],"correct":1,"difficulty":2,"explanation":"x"},
          {"question":"New B","options":["a","a","c","d"],"correct":1,"difficulty":2,"explanation":"x"},
          {"question":"New C","options":["a","b","c","d"],"correct":2,"difficulty":3,"explanation":"x"},
          {"question":"New D","options":["a","b","c","d"],"correct":3,"difficulty":1,"explanation":"x"}
        ]}
        """).RootElement;
        var fresh = PathLesson.Parse(json, "geo", ["Old question one"]);
        Assert.Equal(["New A", "New C", "New D"], fresh.Select(q => q.Prompt));
        Assert.All(fresh, q => Assert.Equal("geo", q.Topic));

        Assert.Throws<EduVerse.Core.Abstractions.AiGenerationException>(() => PathLesson.Parse(json, "geo", ["New A", "New C"]));
    }
}

public class FreshLessonStoreTests(Database database) : IClassFixture<Database>
{
    [Fact]
    public async Task A_second_lesson_on_a_topic_does_not_repeat_the_questions_of_the_first()
    {
        using var scope = database.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EduVerseDb>();
        var quizzes = scope.ServiceProvider.GetRequiredService<QuizStore>();
        var student = await QuizDriver.NewStudentAsync(db, TimeProvider.System);
        const Area area = Area.Geography;
        const Level level = Level.Primary3;
        var topic = AreaSubjects.Topics(area, level)[0];
        var bank = Enumerable.Range(0, 600).Select(i => QuizStore.KeyOf(QuestionBanks.ForTopic(area, level, topic, 1 + i % 3, i * 7919 + 1, TeachLanguage.Spanish))).Distinct().Count();

        var keys = new List<List<string>>();
        for (var lesson = 0; lesson < 2; lesson++)
        {
            var start = await quizzes.StartAsync(student, area, level, QuizMode.Focus, TeachLanguage.Spanish, topic: topic);
            await QuizDriver.AnswerAllRightAsync(quizzes, db, student, start.AttemptId);
            keys.Add(await db.QuizAnswers.Where(a => a.AttemptId == start.AttemptId && a.Key != "").Select(a => a.Key).ToListAsync());
        }

        var repeated = keys[1].Intersect(keys[0]).Count();
        Assert.True(repeated <= Math.Max(0, keys[0].Count + keys[1].Count - bank), $"{repeated} repeated with {bank} questions in the bank");
    }

    [Fact]
    public async Task Fresh_questions_from_the_ai_come_first_then_the_banks_and_the_stage_sets_the_difficulty()
    {
        using var scope = database.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EduVerseDb>();
        var quizzes = scope.ServiceProvider.GetRequiredService<QuizStore>();
        var paths = scope.ServiceProvider.GetRequiredService<PathStore>();
        var student = await QuizDriver.NewStudentAsync(db, TimeProvider.System);
        const Area area = Area.Geography;
        const Level level = Level.Primary3;
        var topic = AreaSubjects.Topics(area, level)[0];
        Assert.Equal(0, await paths.LessonsPassedAsync(student, area, level, topic));

        QuizQuestion[] pool =
        [
            new(topic, 3, "Fresh question A", ["a", "b", "c", "d"], 0, "because"),
            new(topic, 3, "Fresh question B", ["a", "b", "c", "d"], 1, "because"),
        ];
        var start = await quizzes.StartAsync(student, area, level, QuizMode.Focus, TeachLanguage.Spanish, pool: pool, topic: topic, difficulty: 3);
        Assert.Equal("Fresh question A", start.Prompt);
        Assert.Equal(3, (await db.QuizAttempts.SingleAsync(a => a.Id == start.AttemptId)).Difficulty);
        var end = await QuizDriver.AnswerAllRightAsync(quizzes, db, student, start.AttemptId);
        Assert.Equal(AdaptiveQuiz.LengthOf(QuizMode.Focus), end.Summary!.Length);
        var prompts = await db.QuizAnswers.Where(a => a.AttemptId == start.AttemptId).OrderBy(a => a.Id).Select(a => a.Key).ToListAsync();
        Assert.StartsWith("Fresh question A", prompts[0], StringComparison.Ordinal);
        Assert.StartsWith("Fresh question B", prompts[1], StringComparison.Ordinal);
        Assert.All(await db.QuizAnswers.Where(a => a.AttemptId == start.AttemptId).Select(a => a.Topic).ToListAsync(), t => Assert.Equal(topic, t));

        Assert.Equal(1, await paths.LessonsPassedAsync(student, area, level, topic));
        Assert.Contains("Fresh question A", await quizzes.SeenPromptsAsync(student, area, level, topic));
    }
}
