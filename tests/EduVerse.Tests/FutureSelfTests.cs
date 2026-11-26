using EduVerse.Core.Catalog;
using EduVerse.Core.Common;
using EduVerse.Core.Tutor;
using EduVerse.Infrastructure.Data;
using EduVerse.Infrastructure.Tutor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EduVerse.Tests;

public class FutureSelfRuleTests
{
    private static FutureSelfFacts Facts(int mastered = 5, IReadOnlyList<string>? overcome = null, string? curiosity = null, string? url = null) =>
        new("Sara", mastered, overcome ?? [], curiosity, url);

    [Theory]
    [InlineData(0, false)]
    [InlineData(4, false)]
    [InlineData(5, true)]
    [InlineData(6, true)]
    [InlineData(7, true)]
    [InlineData(8, false)]
    [InlineData(10, true)]
    public void A_letter_only_shows_right_at_a_round_milestone(int mastered, bool expected) =>
        Assert.Equal(expected, FutureSelf.HasContent(Facts(mastered)));

    [Fact]
    public void The_letter_carries_the_measured_facts_in_four_languages()
    {
        foreach (var language in Enum.GetValues<TeachLanguage>())
        {
            var letter = FutureSelf.Build(Facts(overcome: ["Fractions"]), language);
            Assert.Contains("Sara", letter.Opening, StringComparison.Ordinal);
            Assert.Contains("5", letter.Opening, StringComparison.Ordinal);
            Assert.Contains("Fractions", string.Join(' ', letter.Body), StringComparison.Ordinal);
            Assert.DoesNotContain("{", letter.Title + letter.Opening + letter.NextStep + letter.Closing, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Without_a_curiosity_question_the_next_step_is_generic_and_has_no_link()
    {
        var letter = FutureSelf.Build(Facts(), TeachLanguage.English);
        Assert.Null(letter.NextStepUrl);
        Assert.Contains("curious", letter.NextStep, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void With_a_curiosity_question_the_next_step_names_it_and_links_to_it()
    {
        var letter = FutureSelf.Build(Facts(curiosity: "Why is the sky blue?", url: "/Student/NewSubject?topic=x"), TeachLanguage.English);
        Assert.Equal("/Student/NewSubject?topic=x", letter.NextStepUrl);
        Assert.Contains("Why is the sky blue?", letter.NextStep, StringComparison.Ordinal);
    }
}

public class FutureSelfStoreTests(Database database) : IClassFixture<Database>
{
    [Fact]
    public async Task No_letter_facts_lead_anywhere_for_a_student_with_nothing_mastered()
    {
        using var scope = database.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<FutureSelfStore>();
        var facts = await store.FactsAsync("nobody", "X", TeachLanguage.English);
        Assert.False(FutureSelf.HasContent(facts));
    }

    [Fact]
    public async Task Picks_up_the_oldest_open_curiosity_question_with_its_explore_link()
    {
        using var scope = database.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EduVerseDb>();
        var curiosity = scope.ServiceProvider.GetRequiredService<CuriosityStore>();
        var store = scope.ServiceProvider.GetRequiredService<FutureSelfStore>();
        var userId = await database.UserIdAsync("leo@eduverse.demo");

        foreach (var q in await db.CuriosityQuestions.Where(c => c.UserId == userId).ToListAsync())
        {
            db.CuriosityQuestions.Remove(q);
        }

        await db.SaveChangesAsync();
        Assert.Equal(CuriosityOutcome.Created, await curiosity.AddAsync(userId, "Why is the sky blue today?"));

        var facts = await store.FactsAsync(userId, "Leo", TeachLanguage.English);
        Assert.Equal("Why is the sky blue today?", facts.Curiosity);
        Assert.Equal("/Student/NewSubject?topic=" + Uri.EscapeDataString("Why is the sky blue today?"), facts.CuriosityUrl);
    }
}
