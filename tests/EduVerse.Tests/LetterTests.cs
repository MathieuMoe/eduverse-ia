using EduVerse.Core.Common;
using EduVerse.Core.Tutor;
using EduVerse.Infrastructure.Tutor;
using Microsoft.Extensions.DependencyInjection;

namespace EduVerse.Tests;

public class WeeklyLetterTests
{
    private static WeekFacts Facts(int answers = 20, string? best = "Fractions", string? work = "Decimals", int understood = 2, string? exam = "Maths test", int? days = 6) =>
        new("Sara", 4, answers, answers / 2, understood, best, work, exam, days);

    [Fact]
    public void A_week_without_answers_has_no_letter() => Assert.False(WeeklyLetter.HasContent(Facts(answers: 0)));

    [Fact]
    public void The_letter_is_made_of_the_measured_facts_in_four_languages()
    {
        foreach (var language in Enum.GetValues<TeachLanguage>())
        {
            var letter = WeeklyLetter.Build(Facts(), language);
            var text = string.Join(' ', letter.Parts.Select(p => p.Text));
            Assert.Contains("Sara", letter.Title, StringComparison.Ordinal);
            Assert.Contains("20", text, StringComparison.Ordinal);
            Assert.Contains("50", text, StringComparison.Ordinal);
            Assert.Contains("Fractions", text, StringComparison.Ordinal);
            Assert.Contains("Decimals", text, StringComparison.Ordinal);
            Assert.Contains("Maths test", text, StringComparison.Ordinal);
            Assert.DoesNotContain("{", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Confidence_adds_a_line_only_when_it_was_measured()
    {
        var plain = string.Join(' ', WeeklyLetter.Build(Facts(), TeachLanguage.English).Parts.Select(p => p.Text));
        Assert.DoesNotContain("felt sure", plain, StringComparison.Ordinal);
        Assert.DoesNotContain("knows more", plain, StringComparison.Ordinal);
        foreach (var language in Enum.GetValues<TeachLanguage>())
        {
            var text = string.Join(' ', WeeklyLetter.Build(Facts() with { SureWrongTopic = "Percentages", UnsureRightTopic = "Areas" }, language).Parts.Select(p => p.Text));
            Assert.Contains("Percentages", text, StringComparison.Ordinal);
            Assert.Contains("Areas", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Missing_facts_leave_their_lines_out()
    {
        var text = string.Join(' ', WeeklyLetter.Build(Facts(best: null, work: null, understood: 0, exam: null, days: null), TeachLanguage.English).Parts.Select(p => p.Text));
        Assert.DoesNotContain("Still tricky", text, StringComparison.Ordinal);
        Assert.DoesNotContain("went best", text, StringComparison.Ordinal);
        Assert.DoesNotContain("explained", text, StringComparison.Ordinal);
    }
}

public class LetterStoreTests(Database database) : IClassFixture<Database>
{
    [Fact]
    public async Task The_facts_count_only_this_week_of_the_student()
    {
        using var scope = database.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<LetterStore>();
        var sara = await database.UserIdAsync("sara@eduverse.demo");
        var facts = await store.FactsAsync(sara, "Sara");
        Assert.Equal("Sara", facts.FirstName);
        Assert.True(facts.Answers >= facts.Correct);
        Assert.Equal(0, (await store.FactsAsync("nobody", "X")).Answers);
    }
}
