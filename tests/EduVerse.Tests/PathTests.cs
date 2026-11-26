using System.Net;
using System.Net.Http.Json;
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

public class LearningPathRulesTests
{
    private static readonly string[] Topics = ["a", "b"];

    [Fact]
    public void A_unit_is_lesson_lesson_chest_lesson_boss_and_only_the_first_unfinished_step_is_open()
    {
        var fresh = LearningPath.Build(Topics, _ => new UnitProgress(0, false, false));
        Assert.Equal(10, fresh.Count);
        Assert.Equal([PathNodeKind.Lesson, PathNodeKind.Lesson, PathNodeKind.Chest, PathNodeKind.Lesson, PathNodeKind.Boss], fresh.Take(5).Select(n => n.Kind));
        Assert.Equal(PathNodeState.Open, fresh[0].State);
        Assert.All(fresh.Skip(1), n => Assert.Equal(PathNodeState.Locked, n.State));

        var chest = LearningPath.Build(Topics, t => t == "a" ? new UnitProgress(2, false, false) : new UnitProgress(0, false, false));
        Assert.Equal((PathNodeKind.Chest, PathNodeState.Open), (chest[2].Kind, chest[2].State));
        Assert.Equal(0, LearningPath.CurrentUnit(chest));

        var nextUnit = LearningPath.Build(Topics, t => t == "a" ? new UnitProgress(3, true, true) : new UnitProgress(0, false, false));
        Assert.All(nextUnit.Take(5), n => Assert.Equal(PathNodeState.Done, n.State));
        Assert.Equal((1, PathNodeState.Open), (nextUnit[5].Unit, nextUnit[5].State));
        Assert.Equal(1, LearningPath.CurrentUnit(nextUnit));

        // Work done out of order (a lesson of unit b passed through the plan) stays locked until the path gets there
        var ahead = LearningPath.Build(Topics, t => t == "b" ? new UnitProgress(3, false, false) : new UnitProgress(0, false, false));
        Assert.Equal(PathNodeState.Locked, ahead[5].State);
    }

    [Fact]
    public void A_lesson_needs_three_right_answers_of_five()
    {
        Assert.True(LearningPath.LessonPassed(3, 5));
        Assert.False(LearningPath.LessonPassed(2, 5));
        Assert.False(LearningPath.LessonPassed(0, 0));
    }

    [Fact]
    public void A_chest_gives_the_same_ten_to_twenty_gems_every_time()
    {
        var key = LearningPath.ChestKey(Area.History, Level.Primary3, "history.dates");
        var gems = LearningPath.ChestGems("user-1", key);
        Assert.InRange(gems, 10, 20);
        Assert.Equal(gems, LearningPath.ChestGems("user-1", key));
        Assert.True(LearningPath.IsBoss(LearningPath.BossTopic("history.dates")));
        Assert.Equal("history.dates", LearningPath.UnitOf(LearningPath.BossTopic("history.dates")));
        Assert.False(LearningPath.IsBoss("history.dates"));
    }

    [Fact]
    public void The_offline_villain_has_a_line_for_every_blow_in_every_language()
    {
        foreach (var language in Enum.GetValues<TeachLanguage>())
        {
            var story = DemoBosses.Write("Dates", "user-1", language);
            Assert.Equal(BossPrompt.Lines, story.Doors.Count);
            Assert.All([story.Title, story.Intro, story.Boss, story.BossIntro, story.Victory, story.Defeat, .. story.Doors], line => Assert.False(string.IsNullOrWhiteSpace(line)));
            Assert.Contains("Dates", story.Title, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void The_ai_villain_is_checked_before_it_is_used()
    {
        var good = JsonDocument.Parse("""{"title":"T","intro":"I","boss":"B","bossIntro":"BI","taunts":["1","2","3","4","5"],"victory":"V","defeat":"D"}""").RootElement;
        Assert.Equal("B", BossPrompt.Parse(good).Boss);
        var missing = JsonDocument.Parse("""{"title":"T","intro":"I","boss":"B","bossIntro":"BI","taunts":["1","2"],"victory":"V","defeat":"D"}""").RootElement;
        Assert.Throws<EduVerse.Core.Abstractions.AiGenerationException>(() => BossPrompt.Parse(missing));
    }
}

public class PathStoreTests(Database database) : IClassFixture<Database>
{
    [Fact]
    public async Task Lessons_a_chest_and_the_boss_complete_a_unit_and_open_the_next()
    {
        using var scope = database.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EduVerseDb>();
        var quizzes = scope.ServiceProvider.GetRequiredService<QuizStore>();
        var paths = scope.ServiceProvider.GetRequiredService<PathStore>();
        var game = scope.ServiceProvider.GetRequiredService<GamificationStore>();
        var student = await QuizDriver.NewStudentAsync(db, TimeProvider.System);
        const Area area = Area.Geography;
        const Level level = Level.Primary3;
        var topic = AreaSubjects.Topics(area, level)[0];

        var path = await paths.PathAsync(student, area, level);
        Assert.Equal(5 * AreaSubjects.Topics(area, level).Count, path.Nodes.Count);
        Assert.Equal((PathNodeKind.Lesson, PathNodeState.Open, topic), (path.Nodes[0].Kind, path.Nodes[0].State, path.Nodes[0].Topic));
        Assert.Null(await paths.OpenChestAsync(student, area, level, topic)); // locked
        Assert.False(await paths.BossOpenAsync(student, area, level, topic));

        async Task LessonAsync()
        {
            var lesson = await quizzes.StartAsync(student, area, level, QuizMode.Focus, TeachLanguage.Spanish, topic: topic);
            var end = await QuizDriver.AnswerAllRightAsync(quizzes, db, student, lesson.AttemptId);
            Assert.Equal(AdaptiveQuiz.LengthOf(QuizMode.Focus), end.Summary!.Length);
        }

        await LessonAsync();
        await LessonAsync();
        path = await paths.PathAsync(student, area, level);
        Assert.Equal((PathNodeKind.Chest, PathNodeState.Open), (path.Nodes[2].Kind, path.Nodes[2].State));

        var gems = await paths.OpenChestAsync(student, area, level, topic);
        Assert.NotNull(gems);
        Assert.InRange(gems.Value, 10, 20);
        Assert.Null(await paths.OpenChestAsync(student, area, level, topic)); // once
        Assert.True(await game.GemsAsync(student) >= gems);

        await LessonAsync();
        Assert.True(await paths.BossOpenAsync(student, area, level, topic));
        var story = DemoBosses.Write("Topic", student, TeachLanguage.Spanish);
        var boss = await quizzes.StartAsync(student, area, level, QuizMode.Adventure, TeachLanguage.Spanish, story: story, theme: Theme.Castle, topic: LearningPath.BossTopic(topic));
        Assert.True(boss.Adventure!.UnitBoss && boss.Adventure.InBoss);
        Assert.Equal(story.BossIntro, boss.Adventure.Line);
        var won = await QuizDriver.AnswerAllRightAsync(quizzes, db, student, boss.AttemptId);
        Assert.True(won.Summary!.Won);
        Assert.All(await db.QuizAnswers.Where(a => a.AttemptId == boss.AttemptId).Select(a => a.Topic).ToListAsync(), t => Assert.Equal(topic, t)); // every blow on the unit's topic

        path = await paths.PathAsync(student, area, level);
        Assert.All(path.Nodes.Take(5), n => Assert.Equal(PathNodeState.Done, n.State));
        Assert.Equal((1, PathNodeState.Open), (path.Nodes[5].Unit, path.Nodes[5].State));
        Assert.Equal(1, path.CurrentUnit);
    }
}

public class PathWebTests(App app) : IClassFixture<App>
{
    [Fact]
    public async Task The_path_page_shows_the_units_and_a_locked_boss_cannot_be_started()
    {
        var client = await app.DemoAsync("Student");
        var page = await client.GetStringAsync("/Student/Path?area=History&level=Primary3");
        Assert.Contains("data-testid=\"path-unit\"", page, StringComparison.Ordinal);
        Assert.Contains("data-state=\"Open\"", page, StringComparison.Ordinal);

        var topic = AreaSubjects.Topics(Area.History, Level.Primary3)[^1];
        var boss = await client.PostAsJsonAsync("/api/quiz/boss", new { area = "History", level = "Primary3", topic });
        Assert.Equal(HttpStatusCode.Conflict, boss.StatusCode);
        var bad = await client.PostAsJsonAsync("/api/quiz/boss", new { area = "History", level = "Primary3", topic = "not.a.topic" });
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
    }
}
