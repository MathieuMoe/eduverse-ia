using System.Net;
using System.Text.RegularExpressions;
using EduVerse.Core.Ideas;
using EduVerse.Core.Tutor;
using EduVerse.Infrastructure.Data;
using EduVerse.Infrastructure.Tutor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EduVerse.Tests;

public class CuriosityRuleTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("why?")] // below MinLength once normalized
    public void A_question_with_too_little_text_is_too_short(string? text) => Assert.Equal(IdeaCheck.TooShort, Curiosity.Check(text));

    [Fact]
    public void A_question_longer_than_the_limit_is_too_long() => Assert.Equal(IdeaCheck.TooLong, Curiosity.Check(new string('a', Curiosity.MaxLength + 1)));

    [Fact]
    public void A_question_with_a_link_is_not_allowed() => Assert.Equal(IdeaCheck.NotAllowed, Curiosity.Check("check this out at http://example.com"));

    [Fact]
    public void An_ordinary_question_of_reasonable_length_is_ok() => Assert.Equal(IdeaCheck.Ok, Curiosity.Check("Why is the sky blue at sunset?"));

    [Fact]
    public void Cleaning_trims_and_normalizes_whitespace() => Assert.Equal("Why is the sky blue?", Curiosity.Clean("  Why   is the sky blue?  "));
}

public class CuriosityStoreTests(Database database) : IClassFixture<Database>
{
    private const string StudentId = "curiosity-student";

    private async Task<(IServiceScope Scope, EduVerseDb Db, CuriosityStore Store)> SetupAsync()
    {
        var scope = database.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EduVerseDb>();
        db.CuriosityQuestions.RemoveRange(db.CuriosityQuestions.Where(q => q.UserId == StudentId));
        await db.SaveChangesAsync();
        return (scope, db, scope.ServiceProvider.GetRequiredService<CuriosityStore>());
    }

    [Fact]
    public async Task A_good_question_is_created_and_starts_open()
    {
        var (scope, db, store) = await SetupAsync();
        using var _d = scope;
        Assert.Equal(CuriosityOutcome.Created, await store.AddAsync(StudentId, "Why is the sky blue at sunset?"));
        var saved = Assert.Single(db.CuriosityQuestions.Where(q => q.UserId == StudentId));
        Assert.Equal(CuriosityStatus.Open, saved.Status);
    }

    [Theory]
    [InlineData("why", CuriosityOutcome.TooShort)]
    [InlineData("this is fine but has http://example.com in it", CuriosityOutcome.NotAllowed)]
    public async Task A_bad_question_is_rejected_and_never_saved(string text, CuriosityOutcome expected)
    {
        var (scope, db, store) = await SetupAsync();
        using var _d = scope;
        Assert.Equal(expected, await store.AddAsync(StudentId, text));
        Assert.Empty(db.CuriosityQuestions.Where(q => q.UserId == StudentId));
    }

    [Fact]
    public async Task Once_the_daily_limit_is_reached_a_further_question_is_refused()
    {
        var (scope, _, store) = await SetupAsync();
        using var _d = scope;
        for (var i = 0; i < Curiosity.DailyLimit; i++)
        {
            Assert.Equal(CuriosityOutcome.Created, await store.AddAsync(StudentId, $"Real question number {i} here?"));
        }

        Assert.Equal(CuriosityOutcome.Limit, await store.AddAsync(StudentId, "One too many questions today?"));
    }

    [Fact]
    public async Task Once_the_open_cap_is_reached_a_further_question_is_refused_even_though_the_daily_limit_was_not_hit_today()
    {
        var (scope, db, store) = await SetupAsync();
        using var _d = scope;
        var longAgo = DateTimeOffset.UtcNow.AddDays(-10); // outside the daily-limit window, so only MaxOpen is in play
        for (var i = 0; i < Curiosity.MaxOpen; i++)
        {
            db.CuriosityQuestions.Add(new CuriosityQuestion { UserId = StudentId, Text = $"Old question {i}", Status = CuriosityStatus.Open, CreatedAt = longAgo });
        }

        await db.SaveChangesAsync();
        Assert.Equal(CuriosityOutcome.Limit, await store.AddAsync(StudentId, "Can I still ask this one?"));
    }

    [Fact]
    public async Task The_widget_shows_the_least_recently_surfaced_questions_first_and_marks_them_shown()
    {
        var (scope, db, store) = await SetupAsync();
        using var _d = scope;
        var now = DateTimeOffset.UtcNow;
        db.CuriosityQuestions.AddRange(
            new CuriosityQuestion { UserId = StudentId, Text = "Shown just now, excluded this round", Status = CuriosityStatus.Open, CreatedAt = now.AddDays(-6), LastSurfacedAt = now.AddMinutes(-1) },
            new CuriosityQuestion { UserId = StudentId, Text = "Shown a while ago", Status = CuriosityStatus.Open, CreatedAt = now.AddDays(-5), LastSurfacedAt = now.AddDays(-1) },
            new CuriosityQuestion { UserId = StudentId, Text = "Never shown, oldest", Status = CuriosityStatus.Open, CreatedAt = now.AddDays(-4) },
            new CuriosityQuestion { UserId = StudentId, Text = "Never shown, newest", Status = CuriosityStatus.Open, CreatedAt = now.AddDays(-3) },
            new CuriosityQuestion { UserId = StudentId, Text = "Dismissed, never shown", Status = CuriosityStatus.Dismissed, CreatedAt = now.AddDays(-2) });
        await db.SaveChangesAsync();

        var widget = await store.ForWidgetAsync(StudentId);
        Assert.Equal(Curiosity.MaxShown, widget.Count);
        // the one surfaced most recently loses its spot to the three waiting longest for a turn; displayed oldest-created first
        Assert.Equal(new[] { "Shown a while ago", "Never shown, oldest", "Never shown, newest" }, widget.Select(q => q.Text));
    }

    [Fact]
    public async Task Exploring_a_question_marks_it_explored_and_returns_its_text()
    {
        var (scope, db, store) = await SetupAsync();
        using var _d = scope;
        var q = new CuriosityQuestion { UserId = StudentId, Text = "Why does ice float?", Status = CuriosityStatus.Open, CreatedAt = DateTimeOffset.UtcNow };
        db.CuriosityQuestions.Add(q);
        await db.SaveChangesAsync();

        Assert.Equal("Why does ice float?", await store.ExploreAsync(StudentId, q.Id));
        Assert.Equal(CuriosityStatus.Explored, (await db.CuriosityQuestions.FindAsync(q.Id))!.Status);
    }

    [Fact]
    public async Task Dismissing_a_question_marks_it_dismissed()
    {
        var (scope, db, store) = await SetupAsync();
        using var _d = scope;
        var q = new CuriosityQuestion { UserId = StudentId, Text = "Why is grass green?", Status = CuriosityStatus.Open, CreatedAt = DateTimeOffset.UtcNow };
        db.CuriosityQuestions.Add(q);
        await db.SaveChangesAsync();

        Assert.True(await store.DismissAsync(StudentId, q.Id));
        Assert.Equal(CuriosityStatus.Dismissed, (await db.CuriosityQuestions.FindAsync(q.Id))!.Status);
    }

    [Fact]
    public async Task A_question_belonging_to_someone_else_cannot_be_explored_or_dismissed()
    {
        var (scope, db, store) = await SetupAsync();
        using var _d = scope;
        var q = new CuriosityQuestion { UserId = "someone-else", Text = "Not yours", Status = CuriosityStatus.Open, CreatedAt = DateTimeOffset.UtcNow };
        db.CuriosityQuestions.Add(q);
        await db.SaveChangesAsync();

        Assert.Null(await store.ExploreAsync(StudentId, q.Id));
        Assert.False(await store.DismissAsync(StudentId, q.Id));
    }
}

public partial class CuriosityWebTests(App app) : IClassFixture<App>
{
    [GeneratedRegex("name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"")]
    private static partial Regex TokenRegex();

    private static async Task<string> TokenAsync(HttpClient client, string url) => TokenRegex().Match(await client.GetStringAsync(url)).Groups[1].Value;

    [Fact]
    public async Task The_dashboard_renders_the_widget_in_its_empty_state_for_a_fresh_demo_student()
    {
        var client = await app.DemoAsync("Student");
        var response = await client.GetAsync("/Student");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadAsStringAsync();
        Assert.Contains("data-testid=\"curiosity\"", page, StringComparison.Ordinal);
        Assert.DoesNotContain("Curiosity_Title", page, StringComparison.Ordinal); // localized, not the raw key
        Assert.DoesNotContain("Curiosity_Empty", page, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Adding_exploring_and_dismissing_a_question_works_through_the_real_page_handlers()
    {
        var client = await app.DemoAsync("Student");
        var add = await client.PostAsync("/Student?handler=CuriosityAdd",
            new FormUrlEncodedContent(new Dictionary<string, string> { ["text"] = "Why do leaves change color in autumn?", ["__RequestVerificationToken"] = await TokenAsync(client, "/Student") }));
        Assert.Equal(HttpStatusCode.Redirect, add.StatusCode);

        var afterAdd = await client.GetStringAsync("/Student");
        Assert.Contains("Why do leaves change color in autumn?", afterAdd, StringComparison.Ordinal);
        var id = Regex.Match(afterAdd, "name=\"id\" value=\"(\\d+)\"").Groups[1].Value;
        Assert.NotEmpty(id);

        var explore = await client.PostAsync("/Student?handler=CuriosityExplore",
            new FormUrlEncodedContent(new Dictionary<string, string> { ["id"] = id, ["__RequestVerificationToken"] = await TokenAsync(client, "/Student") }));
        Assert.Equal(HttpStatusCode.Redirect, explore.StatusCode);
        Assert.Contains("/Student/NewSubject", explore.Headers.Location!.ToString(), StringComparison.Ordinal);
        Assert.Contains("leaves", explore.Headers.Location!.ToString(), StringComparison.OrdinalIgnoreCase);

        var landing = await client.GetStringAsync(explore.Headers.Location);
        Assert.Contains("Why do leaves change color in autumn?", landing, StringComparison.Ordinal); // prefilled into #sb-topic
    }

    [Fact]
    public async Task A_too_short_question_is_rejected_with_a_notice_and_never_added()
    {
        var client = await app.DemoAsync("Student");
        var add = await client.PostAsync("/Student?handler=CuriosityAdd",
            new FormUrlEncodedContent(new Dictionary<string, string> { ["text"] = "why", ["__RequestVerificationToken"] = await TokenAsync(client, "/Student") }));
        Assert.Equal(HttpStatusCode.Redirect, add.StatusCode);
        var page = await client.GetStringAsync("/Student");
        Assert.DoesNotContain("data-testid=\"curiosity-question\"", page, StringComparison.Ordinal);
    }
}
