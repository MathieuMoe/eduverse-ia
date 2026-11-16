using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EduVerse.Core.Catalog;
using EduVerse.Core.Common;
using EduVerse.Core.Ideas;
using EduVerse.Infrastructure.Gamification;
using EduVerse.Infrastructure.Ideas;
using EduVerse.Infrastructure.Tutor;
using Microsoft.Extensions.DependencyInjection;

namespace EduVerse.Tests;

public class IdeaRuleTests
{
    [Theory]
    [InlineData("  Napoleón   Bonaparte ", "napoleon bonaparte")]
    [InlineData("NAPOLEON-BONAPARTE!", "napoleon bonaparte")]
    [InlineData("Frida Kahlo", "frida kahlo")]
    [InlineData("¿¡!?", "")]
    public void The_same_idea_written_differently_has_one_key(string text, string key) => Assert.Equal(key, IdeaRules.Normalize(text));

    [Theory]
    [InlineData("Frida Kahlo", null, IdeaCheck.Ok)]
    [InlineData("a", null, IdeaCheck.TooShort)]
    [InlineData("!!!", null, IdeaCheck.TooShort)]
    [InlineData("Write to me at kid@example.com", null, IdeaCheck.NotAllowed)]
    [InlineData("Pirates", "see http://example.com", IdeaCheck.NotAllowed)]
    [InlineData("Call 612345678", null, IdeaCheck.NotAllowed)]
    [InlineData("Eres idiota", null, IdeaCheck.NotAllowed)]
    public void Proposals_are_short_and_carry_no_contact_details_or_rude_words(string text, string? reason, IdeaCheck expected) =>
        Assert.Equal(expected, IdeaRules.Check(text, reason));

    [Fact]
    public void Long_texts_are_refused_and_what_already_exists_is_recognised()
    {
        Assert.Equal(IdeaCheck.TooLong, IdeaRules.Check(new string('a', IdeaRules.MaxLength + 1)));
        Assert.Equal(IdeaCheck.TooLong, IdeaRules.Check("Pirates", new string('a', IdeaRules.MaxReason + 1)));
        Assert.Equal("Marie Curie", IdeaRules.Existing(IdeaKind.Figure, "marie curie", TeachLanguage.Spanish));
        Assert.Equal("Charles Darwin", IdeaRules.Existing(IdeaKind.Figure, "Darwin", TeachLanguage.Spanish));
        Assert.Null(IdeaRules.Existing(IdeaKind.Figure, "Frida Kahlo", TeachLanguage.Spanish));
        Assert.NotNull(IdeaRules.Existing(IdeaKind.Adventure, "El espacio", TeachLanguage.Spanish));
        Assert.Null(IdeaRules.Existing(IdeaKind.Adventure, "Piratas", TeachLanguage.Spanish));
        Assert.Contains("Ajedrez", IdeaRules.Existing(IdeaKind.Hobby, "ajedrez", TeachLanguage.Spanish), StringComparison.Ordinal);
        Assert.Null(IdeaRules.Existing(IdeaKind.Trade, "Robótica", TeachLanguage.Spanish));
        Assert.True(IdeaRules.IsPublic(IdeaStatus.Approved));
        Assert.False(IdeaRules.IsPublic(IdeaStatus.New));
        Assert.False(IdeaRules.IsPublic(IdeaStatus.Rejected));
    }
}

public class IdeaStoreTests(Database database) : IClassFixture<Database>
{
    private static readonly CompanionNames Names = new(t => t, a => a.ToString(), r => r, m => m.ToString());

    [Fact]
    public async Task An_idea_is_proposed_grouped_moderated_voted_and_announced_when_available()
    {
        using var scope = database.Services.CreateScope();
        var ideas = scope.ServiceProvider.GetRequiredService<IdeaStore>();
        var game = scope.ServiceProvider.GetRequiredService<GamificationStore>();
        var companion = scope.ServiceProvider.GetRequiredService<CompanionStore>();
        var leo = await database.UserIdAsync("leo@eduverse.demo");
        var alex = await database.UserIdAsync("alex@eduverse.demo");
        var nora = await database.UserIdAsync("nora@eduverse.demo");

        Assert.Equal(ProposeOutcome.Exists, (await ideas.ProposeAsync(leo, IdeaKind.Figure, "Marie Curie", null, TeachLanguage.Spanish)).Outcome);
        Assert.Equal(ProposeOutcome.NotAllowed, (await ideas.ProposeAsync(leo, IdeaKind.Figure, "www.example.com", null, TeachLanguage.Spanish)).Outcome);

        var created = await ideas.ProposeAsync(leo, IdeaKind.Figure, "cleopatra", "Fue reina de Egipto", TeachLanguage.Spanish);
        Assert.Equal(ProposeOutcome.Created, created.Outcome);
        Assert.Equal("Cleopatra", created.Idea!.Text);
        var id = created.Idea.Id;

        // The same idea written differently is a vote, and proposing it twice changes nothing
        var joined = await ideas.ProposeAsync(alex, IdeaKind.Figure, "  CLEOPATRA! ", null, TeachLanguage.Spanish);
        Assert.Equal((ProposeOutcome.Joined, id, 2), (joined.Outcome, joined.Idea!.Id, joined.Idea.Votes));
        Assert.Equal(2, (await ideas.ProposeAsync(alex, IdeaKind.Figure, "Cleopatra", null, TeachLanguage.Spanish)).Idea!.Votes);

        // Until an adult approves it only its proposers see it, and nobody can vote
        Assert.Contains(await ideas.ListAsync(leo, IdeaKind.Figure, 50), i => i.Id == id && i.Mine);
        Assert.DoesNotContain(await ideas.ListAsync(nora, IdeaKind.Figure, 50), i => i.Id == id);
        Assert.Null(await ideas.VoteAsync(nora, id));

        Assert.True(await ideas.SetStatusAsync(id, IdeaStatus.Approved));
        Assert.Equal(3, (await ideas.VoteAsync(nora, id))!.Votes);
        Assert.Equal(3, (await ideas.VoteAsync(nora, id))!.Votes); // one vote per student
        Assert.Contains(await ideas.ListAsync(nora, IdeaKind.Figure, 50), i => i.Id == id && i.Voted && !i.Mine);
        Assert.DoesNotContain(await ideas.NewsAsync(leo), n => n.Text == "Cleopatra");

        // Available: XP and the badge for the proposers, and the Tutor tells everyone who asked for it
        var before = (await game.BadgeStatsAsync(leo)).Ideas;
        Assert.True(await ideas.SetStatusAsync(id, IdeaStatus.Available));
        Assert.Equal(before + 1, (await game.BadgeStatsAsync(leo)).Ideas);
        Assert.Equal(0, (await game.BadgeStatsAsync(nora)).Ideas);
        Assert.Contains(await ideas.NewsAsync(nora), n => n.Text == "Cleopatra" && n.Url == "/Student/Interview");
        var snapshot = await companion.SnapshotAsync(leo, "Leo", "/Student", "Inicio", Names);
        Assert.Contains("Cleopatra", EduVerse.Core.Tutor.Companion.Greeting(snapshot, TeachLanguage.Spanish), StringComparison.Ordinal);
        Assert.Equal("idea", EduVerse.Core.Tutor.Companion.Suggest(snapshot, TeachLanguage.Spanish)[0].Id);
        await ideas.MarkSeenAsync(leo, IdeaKind.Figure);
        Assert.DoesNotContain(await ideas.NewsAsync(leo), n => n.Text == "Cleopatra");
        Assert.Null(await ideas.VoteAsync(alex, id)); // available ideas are closed

        var row = (await ideas.AllAsync(IdeaKind.Figure)).Single(r => r.Id == id);
        Assert.Equal((3, 2, IdeaStatus.Available), (row.Votes, row.Proposers, row.Status));
        Assert.Contains("Fue reina de Egipto", row.Reasons);
        Assert.False(await ideas.SetStatusAsync(-1, IdeaStatus.Approved));
    }

    [Fact]
    public async Task A_student_cannot_flood_the_ideas_and_rejected_ones_disappear()
    {
        using var scope = database.Services.CreateScope();
        var ideas = scope.ServiceProvider.GetRequiredService<IdeaStore>();
        var iker = await database.UserIdAsync("iker@eduverse.demo");
        ProposeResult? last = null;
        for (var i = 0; i < IdeaRules.DailyLimit + 1; i++)
        {
            last = await ideas.ProposeAsync(iker, IdeaKind.Hobby, "Aficion nueva " + (char)('a' + i), null, TeachLanguage.Spanish);
            Assert.Equal(i < IdeaRules.DailyLimit ? ProposeOutcome.Created : ProposeOutcome.Limit, last.Outcome);
        }

        var mine = (await ideas.ListAsync(iker, IdeaKind.Hobby, 50)).First(i => i.Mine && i.Status == IdeaStatus.New);
        Assert.True(await ideas.SetStatusAsync(mine.Id, IdeaStatus.Rejected));
        Assert.DoesNotContain(await ideas.ListAsync(iker, IdeaKind.Hobby, 50), i => i.Id == mine.Id);
    }

    [Fact]
    public async Task Only_a_finished_adventure_of_the_student_can_be_rated()
    {
        using var scope = database.Services.CreateScope();
        var ideas = scope.ServiceProvider.GetRequiredService<IdeaStore>();
        var leo = await database.UserIdAsync("leo@eduverse.demo");
        Assert.False(await ideas.RateAdventureAsync(leo, "nope", 5, Felt.JustRight, true));
        Assert.False(await ideas.RateAdventureAsync(leo, "demo-rating-0", 9, Felt.JustRight, true));
        var space = (await ideas.AdventureRatingsAsync()).Single(r => r.Theme == Theme.Space);
        Assert.True(space.Count >= 5);
        Assert.InRange(space.Stars, 1, 5);
        Assert.Equal(space.Count, space.TooEasy + space.JustRight + space.TooHard);
    }
}

public class IdeaWebTests(App app) : IClassFixture<App>
{
    [Fact]
    public async Task Students_propose_and_vote_and_only_adults_moderate()
    {
        var student = await app.DemoAsync("Student");
        foreach (var url in new[] { "/Student/Interview", "/Student/Adventures", "/Student/Learn?tab=trades", "/Student/Learn?tab=hobbies" })
        {
            Assert.Contains("data-ideas=", await student.GetStringAsync(url), StringComparison.Ordinal);
        }

        var list = await student.GetFromJsonAsync<JsonElement>("/api/ideas/figure");
        var items = list.GetProperty("items").EnumerateArray().ToList();
        Assert.Contains(items, i => i.GetProperty("text").GetString() == "Frida Kahlo" && i.GetProperty("canVote").GetBoolean());
        Assert.DoesNotContain(items, i => i.GetProperty("text").GetString() == "Napoleón Bonaparte"); // not approved yet
        Assert.DoesNotContain("userId", list.GetRawText(), StringComparison.OrdinalIgnoreCase);

        var created = await (await student.PostAsJsonAsync("/api/ideas/figure", new { text = "Hipatia de Alejandría", reason = "Era matemática" })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Created", created.GetProperty("outcome").GetString());
        Assert.True(created.GetProperty("item").GetProperty("mine").GetBoolean());
        var exists = await (await student.PostAsJsonAsync("/api/ideas/figure", new { text = "Newton" })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Exists", exists.GetProperty("outcome").GetString());
        Assert.Equal("Isaac Newton", exists.GetProperty("existing").GetString());

        var frida = items.First(i => i.GetProperty("text").GetString() == "Frida Kahlo");
        var voted = await (await student.PostAsync($"/api/ideas/vote/{frida.GetProperty("id").GetInt32()}", null)).Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(voted.GetProperty("voted").GetBoolean());
        Assert.Equal(frida.GetProperty("votes").GetInt32() + 1, voted.GetProperty("votes").GetInt32());
        Assert.Equal(HttpStatusCode.BadRequest, (await student.PostAsync("/api/ideas/vote/999999", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await student.GetAsync("/api/ideas/nothing")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await student.PostAsJsonAsync("/api/adventure-rating/nope", new { stars = 5, felt = "JustRight", fun = true })).StatusCode);

        Assert.Equal(HttpStatusCode.Redirect, (await student.GetAsync("/Moderation/Ideas")).StatusCode);
        var teacher = await app.DemoAsync("Teacher");
        var page = await teacher.GetStringAsync("/Moderation/Ideas");
        Assert.Contains("Napoleón Bonaparte", page, StringComparison.Ordinal);
        Assert.Contains("Hipatia de Alejandría", page, StringComparison.Ordinal);
        Assert.Contains("data-testid=\"mod-ratings\"", page, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.Forbidden, (await teacher.GetAsync("/api/ideas/figure")).StatusCode);
    }
}
