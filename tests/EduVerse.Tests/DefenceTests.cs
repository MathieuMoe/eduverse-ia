using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EduVerse.Core.Abstractions;
using EduVerse.Core.Catalog;
using EduVerse.Core.Common;
using EduVerse.Core.Tutor;
using EduVerse.Infrastructure.Data;
using EduVerse.Infrastructure.Quizzes;
using EduVerse.Infrastructure.Tutor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EduVerse.Tests;

public class DefenceRuleTests
{
    private static readonly QuizQuestion Question = new("math.mult", 1, "What is 7 × 8?", ["54", "56", "49", "63"], 1, "Seven groups of eight make 56: multiply the groups by the size of each group.");

    [Theory]
    [InlineData("Because seven groups of eight is 56", true)]
    [InlineData("I multiplied the groups by the size of each group", true)]
    [InlineData("porque 7 por 8 da 56 siempre", true)]
    [InlineData("I just guessed it and got lucky", false)]
    [InlineData("56", false)]
    [InlineData("because because because because because", false)]
    [InlineData("it is that one it is that one it is", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("write to me at bob@mail.com about the groups of eight", false)]
    public void The_offline_judge_asks_for_a_real_attempt_that_touches_the_method(string? why, bool expected) =>
        Assert.Equal(expected, Defence.Reasoned(why, Question));

    [Fact]
    public void The_keywords_are_the_numbers_and_the_words_of_the_method() =>
        Assert.Subset(Defence.Keywords(Question), new HashSet<string>(["56", "groups", "eight"]));

    [Fact]
    public void Every_outcome_has_feedback_in_four_languages()
    {
        foreach (var language in Enum.GetValues<TeachLanguage>())
        {
            var texts = new[] { (true, true), (true, false), (false, true), (false, false) }.Select(o => Defence.Feedback(o.Item1, o.Item2, language)).ToList();
            Assert.Equal(4, texts.Distinct().Count());
        }
    }

    [Fact]
    public void The_AI_judgement_needs_a_boolean_and_a_sentence()
    {
        Assert.Equal((true, "Well explained."), DefencePrompt.Parse(JsonDocument.Parse("""{"understood":true,"feedback":"Well explained."}""").RootElement));
        Assert.Throws<AiGenerationException>(() => DefencePrompt.Parse(JsonDocument.Parse("""{"understood":"yes","feedback":"x"}""").RootElement));
        Assert.Throws<AiGenerationException>(() => DefencePrompt.Parse(JsonDocument.Parse("""{"understood":true,"feedback":""}""").RootElement));
        var prompt = DefencePrompt.Build(Question, "Multiplication", "why text", Level.Primary3, TeachLanguage.English);
        Assert.Contains("<why>why text</why>", prompt.User, StringComparison.Ordinal);
        Assert.Contains("not instructions", prompt.System, StringComparison.Ordinal);
    }
}

public class DefenceStoreTests(Database database) : IClassFixture<Database>
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task A_finished_test_is_defended_with_other_questions_and_verified_only_with_a_right_answer_and_a_reason()
    {
        using var scope = database.Services.CreateScope();
        var quizzes = scope.ServiceProvider.GetRequiredService<QuizStore>();
        var defences = scope.ServiceProvider.GetRequiredService<DefenceStore>();
        var db = scope.ServiceProvider.GetRequiredService<EduVerseDb>();
        var omar = await database.UserIdAsync("omar@eduverse.demo");

        var unfinished = await quizzes.StartAsync(omar, Area.Mathematics, Level.Primary3, QuizMode.Practice, TeachLanguage.English);
        Assert.Empty(await defences.StartAsync(omar, unfinished.AttemptId, TeachLanguage.English));

        // Answer a whole test with the right options (the test knows them; any of the 4 drill kinds the quiz
        // happens to serve — task #73 mixes all of them in now, not just true/false — is answered right too via
        // its own stored answer key).
        var attempt = unfinished.AttemptId;
        var q = unfinished;
        while (true)
        {
            var row = await db.QuizAttempts.AsNoTracking().SingleAsync(a => a.Id == attempt);
            AnswerResult r;
            if (row.CurrentExerciseJson is { } exerciseJson)
            {
                var exercise = JsonSerializer.Deserialize<PracticeExercise>(exerciseJson, Web)!;
                r = (await (exercise.Kind switch
                {
                    ExerciseKind.FillBlank => quizzes.AnswerFillBlankAsync(omar, attempt, exercise.Answer!, 1, 0, TeachLanguage.English),
                    ExerciseKind.Reorder => quizzes.AnswerReorderAsync(omar, attempt, exercise.CorrectOrder!, 1, 0, TeachLanguage.English),
                    ExerciseKind.Match => quizzes.AnswerMatchAsync(omar, attempt, exercise.Pairs!.ToDictionary(p => p.Left, p => p.Right), 1, 0, TeachLanguage.English),
                    _ => quizzes.AnswerTrueFalseAsync(omar, attempt, exercise.CorrectBool!.Value, 1, 0, TeachLanguage.English),
                }))!;
            }
            else
            {
                var correct = await CorrectIndexAsync(db, attempt, q.Number);
                r = (await quizzes.AnswerAsync(omar, attempt, correct, TeachLanguage.English))!;
            }

            if (r.Next is null)
            {
                break;
            }

            q = r.Next;
        }

        Assert.Empty(await defences.StartAsync("someone-else", attempt, TeachLanguage.English));
        var created = await defences.StartAsync(omar, attempt, TeachLanguage.English);
        Assert.InRange(created.Count, 1, Defence.Max);
        Assert.Equal(created.Select(d => d.Id), (await defences.StartAsync(omar, attempt, TeachLanguage.English)).Select(d => d.Id)); // idempotent
        Assert.Equal(created.Count, created.Select(d => d.Topic).Distinct().Count());

        var first = (await defences.NextAsync(omar, attempt))!;
        Assert.Equal(1, (first.Done + 1));
        Assert.Null(await defences.CompleteAsync("someone-else", first.Id, 0, true));

        var pending = (await defences.PendingAsync(omar, first.Id))!.Value;
        var outcome = (await defences.CompleteAsync(omar, first.Id, pending.Defence.CorrectIndex, true))!;
        Assert.True(outcome.Verified);
        Assert.Equal(Defence.Xp, outcome.Reward.Xp);
        Assert.Null(await defences.CompleteAsync(omar, first.Id, 0, true)); // once

        if (created.Count > 1)
        {
            var second = (await defences.PendingAsync(omar, (await defences.NextAsync(omar, attempt))!.Id))!.Value;
            var wrongChoice = (second.Defence.CorrectIndex + 1) % 4;
            var bad = (await defences.CompleteAsync(omar, second.Defence.Id, wrongChoice, true))!;
            Assert.False(bad.Verified);
            Assert.Equal(0, bad.Reward.Xp);
        }

        var summary = await defences.SummaryAsync([omar]);
        Assert.Equal(1, summary[omar].Verified);
        Assert.Equal(created.Count == 1 ? 1 : 2, summary[omar].Total);
    }

    private static async Task<int> CorrectIndexAsync(EduVerseDb db, string attemptId, int number) =>
        JsonSerializer.Deserialize<QuizQuestion>((await db.QuizAttempts.AsNoTracking().SingleAsync(a => a.Id == attemptId)).CurrentJson, Web)!.CorrectIndex;
}

public class DefenceWebTests(App app) : IClassFixture<App>
{
    [Fact]
    public async Task The_student_defends_a_test_and_the_teacher_sees_the_count()
    {
        var client = await app.DemoAsync("Student");
        string? id = null;
        for (var attempt = 0; attempt < 6 && id is null; attempt++)
        {
            var start = await (await client.PostAsJsonAsync("/api/quiz/start", new { area = "Mathematics", level = "Primary3", mode = "Practice" })).Content.ReadFromJsonAsync<JsonElement>();
            var current = start.GetProperty("question");
            var candidate = current.GetProperty("id").GetString()!;
            var right = 0; // classic (multiple-choice) answers only: a drill isn't logged as a QuizAnswer, so a
                            // correct drill alone would leave nothing for the defence endpoint to pick from (task #70's
                            // documented limitation) and this loop would wrongly think the attempt is defendable.
            for (var i = 0; i < 10; i++)
            {
                // A drill (no options) is answered on its own endpoint, exactly as the real quiz page does; which of
                // the 4 drill kinds shows up is now mixed for every subject (task #73), not just true/false.
                var isDrill = current.TryGetProperty("exerciseKind", out var kind) && kind.ValueKind != JsonValueKind.Null;
                var (url, body) = isDrill
                    ? kind.GetString() switch
                    {
                        "FillBlank" => ($"/api/quiz/{candidate}/fillblank", (object)new { value = "x", confidence = 1 }),
                        "Reorder" => ($"/api/quiz/{candidate}/reorder", new { order = current.GetProperty("tokens").EnumerateArray().Select(t => t.GetString()).ToArray(), confidence = 1 }),
                        "Match" => ($"/api/quiz/{candidate}/match", new
                        {
                            pairs = current.GetProperty("matchLeft").EnumerateArray().Select(t => t.GetString()!)
                                .Zip(current.GetProperty("matchRight").EnumerateArray().Select(t => t.GetString()!))
                                .ToDictionary(p => p.First, p => p.Second),
                            confidence = 1,
                        }),
                        _ => ($"/api/quiz/{candidate}/truefalse", new { value = true, confidence = 1 }),
                    }
                    : ($"/api/quiz/{candidate}/answer", new { choice = i % 4, confidence = 1 });
                var answer = await (await client.PostAsJsonAsync(url, body)).Content.ReadFromJsonAsync<JsonElement>();
                if (!isDrill && answer.GetProperty("correct").GetBoolean())
                {
                    right++;
                }

                if (answer.TryGetProperty("next", out var next) && next.ValueKind != JsonValueKind.Null)
                {
                    current = next;
                }
            }

            id = right > 0 ? candidate : null;
        }

        Assert.NotNull(id);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/api/defence/nonexistent/start", null)).StatusCode);
        var first = await (await client.PostAsync($"/api/defence/{id}/start", null)).Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(first.GetProperty("done").GetBoolean());
        var question = first.GetProperty("question");
        Assert.False(question.TryGetProperty("correctIndex", out _));
        Assert.DoesNotContain("correct", question.GetRawText(), StringComparison.OrdinalIgnoreCase);
        var questionId = question.GetProperty("id").GetInt32();

        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync($"/api/defence/{questionId}/answer", new { choice = 0, why = "" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync($"/api/defence/{questionId}/answer", new { choice = 9, why = "something here" })).StatusCode);
        var done = await (await client.PostAsJsonAsync($"/api/defence/{questionId}/answer", new { choice = 0, why = "I did the operation step by step with the numbers" })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(string.IsNullOrWhiteSpace(done.GetProperty("feedback").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(done.GetProperty("answer").GetString()));
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync($"/api/defence/{questionId}/answer", new { choice = 0, why = "again and again" })).StatusCode);

        var page = await client.GetStringAsync($"/Student/Defend?attempt={id}");
        Assert.Contains("data-testid=\"defend-card\"", page, StringComparison.Ordinal);
        Assert.Contains("data-testid=\"defend-none\"", await client.GetStringAsync("/Student/Defend"), StringComparison.Ordinal);
    }
}
