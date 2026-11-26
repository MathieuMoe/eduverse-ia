using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EduVerse.Core.Catalog;
using EduVerse.Core.Common;
using EduVerse.Core.Gamification;
using EduVerse.Infrastructure.Data;
using EduVerse.Infrastructure.Gamification;
using EduVerse.Infrastructure.Quizzes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EduVerse.Tests;

public class PersonalityRulesTests
{
    private static LessonEnd End(int correct, int length, bool goalJustReached = false) =>
        new(30, correct, length, 90, 3, false, 20, 30, goalJustReached, goalJustReached, false);

    [Fact]
    public void The_mood_comes_from_the_results_not_from_the_ai()
    {
        Assert.Equal(MascotMood.Goal, Mascot.MoodOf(End(2, 10, goalJustReached: true)));
        Assert.Equal(MascotMood.Perfect, Mascot.MoodOf(End(10, 10)));
        Assert.Equal(MascotMood.Great, Mascot.MoodOf(End(7, 10)));
        Assert.Equal(MascotMood.Good, Mascot.MoodOf(End(4, 10)));
        Assert.Equal(MascotMood.Tough, Mascot.MoodOf(End(1, 10)));
    }

    [Fact]
    public void Every_mood_has_offline_lines_in_every_language()
    {
        foreach (var mood in Enum.GetValues<MascotMood>())
        {
            foreach (var language in Enum.GetValues<TeachLanguage>())
            {
                Assert.False(string.IsNullOrWhiteSpace(Mascot.Offline(mood, language, 7)));
                Assert.False(string.IsNullOrWhiteSpace(Mascot.Emoji(mood)));
            }
        }
    }

    [Fact]
    public void The_mascot_prompt_only_carries_numbers()
    {
        var prompt = Mascot.Build(MascotMood.Great, End(7, 10), Level.Secondary1, TeachLanguage.Spanish);
        Assert.Contains("7/10", prompt.User, StringComparison.Ordinal);
        Assert.Equal("Great", Mascot.MoodOf(End(7, 10)).ToString());
    }

    [Theory]
    [InlineData("history.dates")]
    [InlineData("math.percent")]
    [InlineData("foreign.vocabulary")]
    [InlineData("biology.cells")]
    public void An_offline_trick_uses_the_real_question_and_answer(string topic)
    {
        foreach (var language in Enum.GetValues<TeachLanguage>())
        {
            var trick = Tricks.Offline(topic, "In which year: the Berlin Wall falls?", "1989", language);
            Assert.Contains("1989", trick, StringComparison.Ordinal);
            Assert.DoesNotContain("{0}", trick, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void The_offline_story_weaves_every_topic_and_answer_in()
    {
        var items = new List<(string, string, string)> { ("Dates", "Year of the Moon landing?", "1969"), ("Capitals", "Capital of Peru?", "Lima") };
        foreach (var language in Enum.GetValues<TeachLanguage>())
        {
            var (title, story) = MistakeStory.Offline(items, language);
            Assert.False(string.IsNullOrWhiteSpace(title));
            Assert.Contains("1969", story, StringComparison.Ordinal);
            Assert.Contains("Lima", story, StringComparison.Ordinal);
        }

        var json = JsonDocument.Parse("""{"title":"T","story":"short"}""").RootElement;
        Assert.Throws<EduVerse.Core.Abstractions.AiGenerationException>(() => MistakeStory.Parse(json)); // too short to be a story
    }
}

public class PersonalityStoreTests(Database database) : IClassFixture<Database>
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task The_weeks_mistakes_are_grouped_by_topic_with_the_right_answer()
    {
        using var scope = database.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EduVerseDb>();
        var quizzes = scope.ServiceProvider.GetRequiredService<QuizStore>();
        var game = scope.ServiceProvider.GetRequiredService<GamificationStore>();
        var student = await QuizDriver.NewStudentAsync(db, TimeProvider.System);
        Assert.Empty(await game.MistakesOfWeekAsync(student));

        var q = await quizzes.StartAsync(student, Area.History, Level.Secondary1, QuizMode.Focus, TeachLanguage.English, topic: "history.dates");
        var current = JsonSerializer.Deserialize<QuizQuestion>((await db.QuizAttempts.AsNoTracking().SingleAsync(a => a.Id == q.AttemptId)).CurrentJson, Web)!;
        await quizzes.AnswerAsync(student, q.AttemptId, (current.CorrectIndex + 1) % 4, TeachLanguage.English);

        var mistakes = await game.MistakesOfWeekAsync(student);
        var mistake = Assert.Single(mistakes);
        Assert.Equal(("history.dates", current.Prompt, current.Answer), (mistake.Topic, mistake.Prompt, mistake.Answer));
        var answered = await game.AnsweredAsync(student, q.AttemptId, 1);
        Assert.NotNull(answered);
        Assert.Null(await game.AnsweredAsync("someone-else", q.AttemptId, 1));
    }
}

public class PersonalityWebTests(App app) : IClassFixture<App>
{
    [Fact]
    public async Task A_trick_and_the_story_work_offline_and_never_leak_unanswered_questions()
    {
        var client = await app.DemoAsync("Student");
        var start = await client.PostAsJsonAsync("/api/quiz/start", new { area = "History", level = "Secondary1", mode = "Focus", topic = "history.dates" });
        var question = (await start.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("question");
        var id = question.GetProperty("id").GetString();

        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync($"/api/quiz/{id}/trick/1", null)).StatusCode); // not answered yet
        await client.PostAsJsonAsync($"/api/quiz/{id}/answer", new { choice = 0 });
        var trick = await client.PostAsync($"/api/quiz/{id}/trick/1", null);
        Assert.Equal(HttpStatusCode.OK, trick.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace((await trick.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("trick").GetString()));

        var story = await client.PostAsJsonAsync("/api/game/mistake-story", new { });
        Assert.True(story.StatusCode is HttpStatusCode.OK or HttpStatusCode.Conflict); // the demo student may or may not have mistakes this week
        if (story.StatusCode == HttpStatusCode.OK)
        {
            var body = await story.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(body.GetProperty("story").GetString()!.Length > 80);
            Assert.StartsWith("/Student/Quiz?", body.GetProperty("practice").GetString(), StringComparison.Ordinal);
        }

        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync("/api/game/mascot/not-an-attempt", null)).StatusCode);
    }
}
