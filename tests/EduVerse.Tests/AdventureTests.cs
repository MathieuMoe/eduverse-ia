using System.Text.Json;
using EduVerse.Core.Catalog;
using EduVerse.Core.Common;
using EduVerse.Core.Gamification;
using EduVerse.Core.Parsing;
using EduVerse.Infrastructure.Data;
using EduVerse.Infrastructure.Quizzes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EduVerse.Tests;

public class AdventureRuleTests
{
    [Fact]
    public void Doors_then_the_boss_with_three_hearts()
    {
        var s = Adventure.Start(Theme.Jungle);
        s = Adventure.Answer(s, false);
        Assert.Equal((0, 2), (s.Passed, s.Hearts)); // the door asks again
        for (var i = 0; i < Adventure.Doors; i++)
        {
            s = Adventure.Answer(s, true);
        }

        Assert.True(s.InBoss);
        Assert.Equal(Adventure.BossHp, s.BossHp);
        for (var i = 0; i < Adventure.BossHp; i++)
        {
            Assert.False(s.Finished);
            s = Adventure.Answer(s, true);
        }

        Assert.True(s.Won);
        Assert.Equal(2, Adventure.Stars(s));
        Assert.Same(s, Adventure.Answer(s, false)); // nothing changes once finished
    }

    [Fact]
    public void Three_wrong_answers_end_the_adventure()
    {
        var s = Adventure.Start(Theme.Space);
        s = Adventure.Answer(Adventure.Answer(Adventure.Answer(s, false), true), false);
        Assert.False(s.Finished);
        s = Adventure.Answer(s, false);
        Assert.True(s.Lost);
        Assert.Equal(0, Adventure.Stars(s));
    }

    [Fact]
    public void Offline_stories_are_complete_in_every_theme_and_language()
    {
        foreach (var theme in Enum.GetValues<Theme>())
        {
            foreach (var language in Enum.GetValues<TeachLanguage>())
            {
                var story = DemoStories.Write(theme, Area.History, language);
                Assert.Equal(Adventure.Doors, story.Doors.Count);
                var all = string.Join(" ", new[] { story.Title, story.Intro, story.Boss, story.BossIntro, story.Victory, story.Defeat }.Concat(story.Doors));
                Assert.DoesNotContain("{0}", all, StringComparison.Ordinal);
                Assert.DoesNotContain("|", all, StringComparison.Ordinal);
                Assert.All(story.Doors, d => Assert.False(string.IsNullOrWhiteSpace(d)));
            }
        }

        Assert.Contains("la historia", DemoStories.Write(Theme.Jungle, Area.History, TeachLanguage.Spanish).Title, StringComparison.Ordinal);
    }

    [Fact]
    public void Ai_stories_are_checked_and_only_get_the_theme()
    {
        var prompt = StoryPrompt.Build(Theme.Ocean, Area.Biology, Level.Primary1, TeachLanguage.French);
        Assert.Contains("French", prompt.System, StringComparison.Ordinal);
        Assert.Contains("no violence", prompt.System, StringComparison.Ordinal);
        var good = JsonText.Parse("""{"title":"T","intro":"I","doors":["1","2","3","4","5"],"boss":"B","bossIntro":"BI","victory":"V","defeat":"D"}""");
        Assert.Equal("B", StoryPrompt.Parse(good).Boss);
        var missing = JsonText.Parse("""{"title":"T","intro":"I","doors":["1","2"],"boss":"B","bossIntro":"BI","victory":"V","defeat":"D"}""");
        Assert.Throws<EduVerse.Core.Abstractions.AiGenerationException>(() => StoryPrompt.Parse(missing));
    }
}

public class AdventurePersistenceTests(Database database) : IClassFixture<Database>
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    private static async Task<QuizQuestion> CurrentAsync(EduVerseDb db, string id) =>
        JsonSerializer.Deserialize<QuizQuestion>((await db.QuizAttempts.AsNoTracking().SingleAsync(a => a.Id == id)).CurrentJson, Web)!;

    [Fact]
    public async Task An_adventure_is_won_after_the_doors_and_the_boss_on_weak_topics()
    {
        using var scope = database.Services.CreateScope();
        var quizzes = scope.ServiceProvider.GetRequiredService<QuizStore>();
        var db = scope.ServiceProvider.GetRequiredService<EduVerseDb>();
        var sara = await database.UserIdAsync("sara@eduverse.demo");
        var story = DemoStories.Write(Theme.Castle, Area.Physics, TeachLanguage.English);
        var q = await quizzes.StartAsync(sara, Area.Physics, Level.Primary3, QuizMode.Adventure, TeachLanguage.English, story: story, theme: Theme.Castle);
        Assert.NotNull(q.Adventure);
        Assert.Equal((story.Intro, story.Doors[0], 3), (q.Adventure.Intro, q.Adventure.Line, q.Adventure.Hearts));

        var weak = JsonSerializer.Deserialize<List<string>>((await db.QuizAttempts.AsNoTracking().SingleAsync(a => a.Id == q.AttemptId)).PlanJson!, Web)!;
        Assert.NotEmpty(weak); // Sara's weak physics topics

        AnswerResult? r = null;
        var bossTopics = new List<string>();
        for (var i = 0; i < Adventure.Doors + Adventure.BossHp; i++)
        {
            // Adventure now mixes in Duolingo-style drills too (task #74), every 4th answer, so a door or boss hit
            // can be a drill instead of a classic question; detect which and answer right via its own key either way.
            var attempt = await db.QuizAttempts.AsNoTracking().SingleAsync(a => a.Id == q.AttemptId);
            string topic;
            int difficulty;
            if (attempt.CurrentExerciseJson is { } exerciseJson)
            {
                var exercise = JsonSerializer.Deserialize<PracticeExercise>(exerciseJson, Web)!;
                (topic, difficulty) = (exercise.Topic, exercise.Difficulty);
                if (i >= Adventure.Doors)
                {
                    Assert.Equal(3, difficulty);
                    bossTopics.Add(topic);
                }

                r = await (exercise.Kind switch
                {
                    ExerciseKind.FillBlank => quizzes.AnswerFillBlankAsync(sara, q.AttemptId, exercise.Answer!, 1, 0, TeachLanguage.English),
                    ExerciseKind.Reorder => quizzes.AnswerReorderAsync(sara, q.AttemptId, exercise.CorrectOrder!, 1, 0, TeachLanguage.English),
                    ExerciseKind.Match => quizzes.AnswerMatchAsync(sara, q.AttemptId, exercise.Pairs!.ToDictionary(p => p.Left, p => p.Right), 1, 0, TeachLanguage.English),
                    _ => quizzes.AnswerTrueFalseAsync(sara, q.AttemptId, exercise.CorrectBool!.Value, TeachLanguage.English),
                });
            }
            else
            {
                var current = JsonSerializer.Deserialize<QuizQuestion>(attempt.CurrentJson, Web)!;
                (topic, difficulty) = (current.Topic, current.Difficulty);
                if (i >= Adventure.Doors)
                {
                    Assert.Equal(3, difficulty);
                    bossTopics.Add(topic);
                }

                r = await quizzes.AnswerAsync(sara, q.AttemptId, current.CorrectIndex, 1, TeachLanguage.English);
            }

            if (i == Adventure.Doors - 1)
            {
                Assert.True(r!.Next!.Adventure!.InBoss);
                Assert.Equal(story.BossIntro, r.Next.Adventure.Line);
            }
        }

        Assert.True(bossTopics.Count(weak.Contains) >= 2);
        Assert.Equal((true, 3, story.Victory), (r!.Summary!.Won, r.Summary.Hearts, r.Summary.Ending));
        Assert.Equal(3, r.Summary.Stars);
        Assert.True(await db.XpEvents.AnyAsync(x => x.UserId == sara && x.Reason == "adventure" && x.Amount == Xp.AdventureWon));
        Assert.True(await db.BadgeAwards.AnyAsync(b => b.UserId == sara && b.Key == "BossSlayer"));
        Assert.True((await db.QuizAttempts.AsNoTracking().SingleAsync(a => a.Id == q.AttemptId)).Won);
    }

    [Fact]
    public async Task Losing_all_hearts_ends_with_an_encouraging_ending()
    {
        using var scope = database.Services.CreateScope();
        var quizzes = scope.ServiceProvider.GetRequiredService<QuizStore>();
        var db = scope.ServiceProvider.GetRequiredService<EduVerseDb>();
        var mia = await database.UserIdAsync(EduVerse.Infrastructure.Data.DemoSeeder.KidEmail);
        var story = DemoStories.Write(Theme.Ocean, Area.Mathematics, TeachLanguage.Spanish);
        var q = await quizzes.StartAsync(mia, Area.Mathematics, Level.EarlyYears, QuizMode.Adventure, TeachLanguage.Spanish, story: story, theme: Theme.Ocean);
        Assert.True(q.Kids);
        AnswerResult? r = null;
        for (var i = 0; i < Adventure.Hearts; i++)
        {
            var current = await CurrentAsync(db, q.AttemptId);
            r = await quizzes.AnswerAsync(mia, q.AttemptId, (current.CorrectIndex + 1) % 4, 1, TeachLanguage.Spanish);
            if (i < Adventure.Hearts - 1)
            {
                Assert.Equal(Adventure.Hearts - 1 - i, r!.Next!.Adventure!.Hearts);
                Assert.Equal(story.Doors[0], r.Next.Adventure.Line); // still the first door
            }
        }

        Assert.Equal((false, story.Defeat), (r!.Summary!.Won, r.Summary.Ending));
    }
}
