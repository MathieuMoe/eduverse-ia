using EduVerse.Core.Catalog;
using EduVerse.Core.Common;
using EduVerse.Core.Tutor;
using EduVerse.Infrastructure.Data;
using EduVerse.Infrastructure.Quizzes;
using EduVerse.Infrastructure.Tutor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EduVerse.Tests;

public class InterestRuleTests
{
    [Fact]
    public void Only_known_interests_are_kept_without_repeats_and_at_most_three() =>
        Assert.Equal(["football", "music", "space"], Interests.Parse("football, nonsense,music,football,space,cars"));

    [Fact]
    public void The_prompt_line_names_the_interests_and_keeps_the_maths_untouched()
    {
        Assert.Equal("", Interests.ForPrompt([]));
        var line = Interests.ForPrompt(["football", "robots"]);
        Assert.Contains("football stickers", line, StringComparison.Ordinal);
        Assert.Contains("never let it change the maths", line, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(Level.Primary1)]
    [InlineData(Level.Primary2)]
    [InlineData(Level.Primary3)]
    public void Every_word_problem_has_one_right_option_the_computed_one_in_four_languages(Level level)
    {
        foreach (var language in Enum.GetValues<TeachLanguage>())
        {
            foreach (var interest in Interests.All)
            {
                for (var seed = 1; seed <= 40; seed++)
                {
                    var q = InterestProblems.Generate(interest, level, null, 1 + seed % 3, seed, language);
                    Assert.Equal(q.Options.Count, q.Options.Distinct().Count());
                    Assert.InRange(q.CorrectIndex, 0, q.Options.Count - 1);
                    Assert.Contains(Interests.Name(interest, language), q.Prompt, StringComparison.Ordinal);
                    Assert.DoesNotContain("{", q.Prompt, StringComparison.Ordinal);
                    Assert.DoesNotContain("{", q.Explanation, StringComparison.Ordinal);
                }
            }
        }
    }

    [Fact]
    public void The_same_seed_gives_the_same_problem_and_a_topic_is_respected()
    {
        var a = InterestProblems.Generate(Interests.All[0], Level.Primary3, "math.percent", 2, 7, TeachLanguage.English);
        var b = InterestProblems.Generate(Interests.All[0], Level.Primary3, "math.percent", 2, 7, TeachLanguage.English);
        Assert.Equal(a.Prompt, b.Prompt);
        Assert.Equal("math.percent", a.Topic);
        Assert.Contains("%", a.Prompt, StringComparison.Ordinal);
    }

    [Fact]
    public void Only_primary_mathematics_has_word_problems()
    {
        Assert.True(InterestProblems.Supports(Area.Mathematics, Level.Primary2));
        Assert.False(InterestProblems.Supports(Area.Mathematics, Level.Secondary1));
        Assert.False(InterestProblems.Supports(Area.Mathematics, Level.Primary2, "math.shapes"));
    }
}

public class OpeningRuleTests
{
    private static OpeningFacts Facts(int? days = 1, string? exam = null, int? examDays = null, int due = 0, string? hard = null) =>
        new(days, "Fractions", 4, 5, hard, 40, exam, examDays, due);

    [Fact]
    public void A_new_student_has_no_opening() =>
        Assert.Null(Opening.Build(new OpeningFacts(null, null, 0, 0, null, 0, null, null, 0), TeachLanguage.English));

    [Fact]
    public void The_exam_comes_first_then_the_reviews_then_the_hard_topic()
    {
        Assert.Equal("/Student/Plan", Opening.Build(Facts(exam: "Maths test", examDays: 5, due: 3, hard: "Decimals"), TeachLanguage.English)!.Url);
        Assert.Contains("/Student/Quiz?mode=Review", Opening.Build(Facts(due: 3, hard: "Decimals"), TeachLanguage.English)!.Url, StringComparison.Ordinal);
        Assert.Contains("Decimals", Opening.Build(Facts(hard: "Decimals"), TeachLanguage.English)!.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void It_remembers_the_last_session_in_every_language()
    {
        foreach (var language in Enum.GetValues<TeachLanguage>())
        {
            var note = Opening.Build(Facts(), language)!;
            Assert.Contains("4", note.Text, StringComparison.Ordinal);
            Assert.Contains("Fractions", note.Text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void A_long_absence_gets_a_warm_welcome() =>
        Assert.Contains("Welcome back", Opening.Build(Facts(days: 12), TeachLanguage.English)!.Text, StringComparison.Ordinal);
}

public class InterestStoreTests(Database database) : IClassFixture<Database>
{
    private static readonly System.Text.Json.JsonSerializerOptions Web = new(System.Text.Json.JsonSerializerDefaults.Web);

    [Fact]
    public async Task A_practice_test_swaps_in_word_problems_about_what_the_student_likes_and_the_opening_remembers_it()
    {
        using var scope = database.Services.CreateScope();
        var quizzes = scope.ServiceProvider.GetRequiredService<QuizStore>();
        var openings = scope.ServiceProvider.GetRequiredService<OpeningStore>();
        var db = scope.ServiceProvider.GetRequiredService<EduVerseDb>();
        var omar = await database.UserIdAsync("omar@eduverse.demo");
        var profile = await db.StudentProfiles.FirstAsync(p => p.UserId == omar);
        profile.Interests = "football";
        await db.SaveChangesAsync();

        var q = await quizzes.StartAsync(omar, Area.Mathematics, Level.Primary2, QuizMode.Practice, TeachLanguage.English);
        var prompts = new List<string> { q.Prompt };
        var attempt = q.AttemptId;
        for (var i = 0; i < 2; i++)
        {
            var current = System.Text.Json.JsonSerializer.Deserialize<QuizQuestion>((await db.QuizAttempts.AsNoTracking().FirstAsync(a => a.Id == attempt)).CurrentJson, Web)!;
            var r = await quizzes.AnswerAsync(omar, attempt, current.CorrectIndex, TeachLanguage.English);
            prompts.Add(r!.Next!.Prompt);
        }

        Assert.Contains(prompts, p => p.Contains("football stickers", StringComparison.Ordinal));
        var facts = await openings.FactsAsync(omar);
        Assert.Equal(2, facts.LastTotal);
        Assert.Equal(2, facts.LastCorrect);
        Assert.Equal(0, facts.DaysAway);
    }
}
