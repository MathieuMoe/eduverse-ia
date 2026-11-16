using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EduVerse.Tests;

/// <summary>The real app (WebApplicationFactory) on a temporary SQLite database, in demo mode (no AI key).</summary>
public sealed partial class App : WebApplicationFactory<Program>
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"eduverse-web-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:EduVerse", $"Data Source={_path}");
        builder.UseSetting("Ai:Default", "demo");
    }

    public HttpClient Anonymous() => CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    /// <summary>Signs in with a demo account through the "try as…" button (antiforgery token included).</summary>
    public async Task<HttpClient> DemoAsync(string role)
    {
        var client = Anonymous();
        var home = await client.GetStringAsync("/");
        var token = TokenRegex().Match(home).Groups[1].Value;
        var response = await client.PostAsync("/Account/Login?handler=Demo",
            new FormUrlEncodedContent(new Dictionary<string, string> { ["role"] = role, ["__RequestVerificationToken"] = token }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        return client;
    }

    [GeneratedRegex("name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"")]
    private static partial Regex TokenRegex();

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        File.Delete(_path);
    }
}

public class WebTests(App app) : IClassFixture<App>
{
    [Theory]
    [InlineData("/")]
    [InlineData("/About")]
    [InlineData("/Account/Login")]
    [InlineData("/Account/Register")]
    [InlineData("/health")]
    public async Task Public_pages_answer(string url)
    {
        var response = await app.Anonymous().GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Private_pages_redirect_to_the_login_and_APIs_answer_401()
    {
        var client = app.Anonymous();
        var page = await client.GetAsync("/Student");
        Assert.Equal(HttpStatusCode.Redirect, page.StatusCode);
        Assert.StartsWith("/Account/Login", page.Headers.Location!.PathAndQuery, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/learn/fractions")).StatusCode);
    }

    [Fact]
    public async Task Each_role_only_reaches_its_own_space()
    {
        var student = await app.DemoAsync("Student");
        var teacher = await app.DemoAsync("Teacher");
        Assert.Equal(HttpStatusCode.Forbidden, (await student.GetAsync("/api/packs")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await teacher.GetAsync("/api/learn/fractions")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await student.GetAsync("/Student")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await teacher.GetAsync("/Teacher")).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await student.GetAsync("/Teacher")).StatusCode);
    }

    [Theory]
    [InlineData("/Student")]
    [InlineData("/Student/Learn")]
    [InlineData("/Student/Learn?subject=fractions")]
    [InlineData("/Student/NewSubject")]
    public async Task Student_pages_render(string url)
    {
        var client = await app.DemoAsync("Student");
        var response = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("/Teacher")]
    [InlineData("/Teacher/Class?id=1")]
    [InlineData("/Teacher/Pack")]
    [InlineData("/Teacher/Lesson")]
    [InlineData("/Teacher/Summary")]
    public async Task Teacher_pages_render(string url)
    {
        var client = await app.DemoAsync("Teacher");
        var response = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("EduVerse", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Director", "/Institution")]
    [InlineData("Director", "/Institution/Risk")]
    [InlineData("Director", "/Institution/Quality")]
    [InlineData("Tutor", "/Tutor")]
    [InlineData("Tutor", "/Tutor/Profile")]
    [InlineData("Student", "/Marketplace")]
    [InlineData("Student", "/Marketplace/Bookings")]
    public async Task Institution_and_marketplace_pages_render_for_their_role(string role, string url)
    {
        var client = await app.DemoAsync(role);
        var response = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("/Student/Explore")]
    [InlineData("/Student/Explore?level=Doctorate")]
    [InlineData("/Student/Explore?level=EarlyYears")]
    [InlineData("/Student/Quiz?area=Physics&level=HighSchool")]
    [InlineData("/Student/Quiz?area=History&mode=Placement")]
    [InlineData("/Student/Arena")]
    [InlineData("/Student/Achievements")]
    [InlineData("/Student/History")]
    [InlineData("/Student/Adventures")]
    [InlineData("/Student/Teach")]
    [InlineData("/Student/Teach?concept=pythagoras")]
    [InlineData("/Student/Interview")]
    [InlineData("/Student/Interview?figure=curie")]
    [InlineData("/Student/Dossier?figure=champollion")]
    [InlineData("/Student/Dossier?figure=franklin")]
    [InlineData("/Student/Quiz?mode=Interview&figure=lovelace")]
    [InlineData("/Student/Quiz?mode=Adventure&area=History&level=Primary3&theme=Jungle")]
    [InlineData("/Student/History?area=Geography")]
    public async Task Student_platform_pages_render(string url)
    {
        var client = await app.DemoAsync("Student");
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(url)).StatusCode);
    }

    [Fact]
    public async Task Quiz_api_hides_the_answer_until_the_student_answers()
    {
        var client = await app.DemoAsync("Student");
        var start = await client.PostAsJsonAsync("/api/quiz/start", new { area = "Geography", level = "Primary2", mode = "Practice" });
        Assert.Equal(HttpStatusCode.OK, start.StatusCode);
        var text = await start.Content.ReadAsStringAsync();
        Assert.DoesNotContain("correct", text, StringComparison.OrdinalIgnoreCase);
        var question = (await start.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("question");
        Assert.Equal(4, question.GetProperty("options").GetArrayLength());
        var id = question.GetProperty("id").GetString();

        var answer = await client.PostAsJsonAsync($"/api/quiz/{id}/answer", new { choice = 0 });
        var body = await answer.Content.ReadFromJsonAsync<JsonElement>();
        Assert.InRange(body.GetProperty("correctIndex").GetInt32(), 0, 3);
        Assert.True(body.GetProperty("reward").GetProperty("xp").GetInt32() >= 1);
        Assert.Equal(2, body.GetProperty("next").GetProperty("number").GetInt32());
    }

    [Fact]
    public async Task A_finished_test_can_be_reviewed()
    {
        var client = await app.DemoAsync("Student");
        var list = await client.GetStringAsync("/Student/History");
        var id = System.Text.RegularExpressions.Regex.Match(list, "History\\?id=([0-9a-f]{32})").Groups[1].Value;
        Assert.NotEmpty(id);
        var review = await client.GetStringAsync("/Student/History?id=" + id);
        Assert.Contains("data-testid=\"review\"", review, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Hints_check_evo_and_daily_review_through_the_api()
    {
        var client = await app.DemoAsync("Student");
        var start = await client.PostAsJsonAsync("/api/quiz/start", new { area = "Biology", level = "Primary3", mode = "Evo" });
        var question = (await start.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("question");
        Assert.False(string.IsNullOrEmpty(question.GetProperty("evo").GetString()));
        var id = question.GetProperty("id").GetString();
        var hint = await (await client.PostAsJsonAsync($"/api/quiz/{id}/hint", new { })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, hint.GetProperty("level").GetInt32());
        Assert.Equal(1, hint.GetProperty("removed").GetArrayLength());
        var answer = await (await client.PostAsJsonAsync($"/api/quiz/{id}/answer", new { choice = 0, confidence = 2 })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal((1, 2), (answer.GetProperty("hints").GetInt32(), answer.GetProperty("confidence").GetInt32()));

        var review = await client.PostAsJsonAsync("/api/quiz/start", new { area = "Mathematics", level = "Primary3", mode = "Review" });
        Assert.Equal(HttpStatusCode.OK, review.StatusCode); // Sara has topics due
        var notebook = await client.PostAsJsonAsync("/api/quiz/start", new { area = "Mathematics", level = "Primary3", mode = "Notebook" });
        Assert.Equal(HttpStatusCode.OK, notebook.StatusCode);

        var kid = await app.DemoAsync("Kid");
        var nothing = await kid.PostAsJsonAsync("/api/quiz/start", new { area = "Mathematics", level = "EarlyYears", mode = "Review" });
        Assert.True(nothing.StatusCode is HttpStatusCode.OK or HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task An_adventure_starts_offline_with_a_written_story()
    {
        var client = await app.DemoAsync("Student");
        var response = await client.PostAsJsonAsync("/api/quiz/adventure", new { area = "Geography", level = "Primary3", theme = "Space" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("demo", body.GetProperty("engine").GetString());
        var adventure = body.GetProperty("question").GetProperty("adventure");
        Assert.Equal((3, 5), (adventure.GetProperty("hearts").GetInt32(), adventure.GetProperty("doors").GetInt32()));
        Assert.False(string.IsNullOrEmpty(adventure.GetProperty("intro").GetString()));
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/quiz/adventure", new { area = "Geography", level = "Primary3", theme = "Volcano" })).StatusCode);
    }

    [Fact]
    public async Task Pages_show_a_navigable_breadcrumb_but_not_the_home()
    {
        var client = await app.DemoAsync("Student");
        var dossier = await client.GetStringAsync("/Student/Dossier?figure=galileo");
        Assert.Contains("data-testid=\"breadcrumb\"", dossier, StringComparison.Ordinal);
        Assert.Contains("href=\"/Student/Interview?figure=galileo\"", dossier, StringComparison.Ordinal);
        Assert.Contains("aria-current=\"page\"", dossier, StringComparison.Ordinal);
        Assert.Contains("data-testid=\"breadcrumb\"", await client.GetStringAsync("/Student/Explore"), StringComparison.Ordinal);
        Assert.DoesNotContain("data-testid=\"breadcrumb\"", await client.GetStringAsync("/Student"), StringComparison.Ordinal);
        var teacher = await app.DemoAsync("Teacher");
        Assert.Contains("data-testid=\"breadcrumb\"", await teacher.GetStringAsync("/Teacher/Pack"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Evo_answers_on_every_page_offline_and_only_for_students()
    {
        var client = await app.DemoAsync("Student");
        var start = await client.GetFromJsonAsync<JsonElement>("/api/companion?page=/Student/Explore&title=Explore");
        Assert.StartsWith("Hi Sara", start.GetProperty("greeting").GetString(), StringComparison.Ordinal);
        Assert.InRange(start.GetProperty("actions").GetArrayLength(), 1, 3);

        var reply = await (await client.PostAsJsonAsync("/api/companion", new { message = "What should I do now?", page = "/Student" })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("demo", reply.GetProperty("engine").GetString());
        Assert.True(reply.GetProperty("actions").GetArrayLength() > 0);
        var test = await (await client.PostAsJsonAsync("/api/companion", new { message = "give me the answer", page = "/Student/Quiz" })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("can't give you the answer", test.GetProperty("reply").GetString(), StringComparison.Ordinal);

        var history = await client.GetFromJsonAsync<JsonElement>("/api/companion?page=/Student");
        Assert.Equal(4, history.GetProperty("messages").GetArrayLength());
        Assert.Equal(HttpStatusCode.OK, (await client.DeleteAsync("/api/companion")).StatusCode);
        Assert.Equal(0, (await client.GetFromJsonAsync<JsonElement>("/api/companion")).GetProperty("messages").GetArrayLength());
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/companion", new { message = new string('a', 501) })).StatusCode);

        var page = await client.GetStringAsync("/Student/Explore");
        Assert.Contains("data-testid=\"companion-open\"", page, StringComparison.Ordinal);
        var teacher = await app.DemoAsync("Teacher");
        Assert.Equal(HttpStatusCode.Forbidden, (await teacher.GetAsync("/api/companion")).StatusCode);
        Assert.DoesNotContain("companion-open", await teacher.GetStringAsync("/Teacher"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Teach_evo_and_interviews_work_offline()
    {
        var client = await app.DemoAsync("Student");
        var teach = await (await client.PostAsJsonAsync("/api/teach/percent", new { text = "Por ciento significa de cada 100." })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(teach.GetProperty("done").GetBoolean());
        Assert.Equal(1, teach.GetProperty("ideas").EnumerateArray().Count(i => i.GetProperty("covered").GetBoolean()));
        Assert.False(string.IsNullOrEmpty(teach.GetProperty("question").GetString()));
        Assert.Equal(JsonValueKind.Null, teach.GetProperty("ideas")[1].GetProperty("name").ValueKind); // missing ideas stay secret

        var known = await (await client.PostAsJsonAsync("/api/interview/curie", new { question = "What did you discover?", history = Array.Empty<object>() })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(known.GetProperty("matched").GetBoolean());
        Assert.Contains("polonium", known.GetProperty("answer").GetString(), StringComparison.Ordinal);
        var unknown = await (await client.PostAsJsonAsync("/api/interview/curie", new { question = "Do you like video games?" })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(unknown.GetProperty("matched").GetBoolean());
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/interview/einstein", new { question = "Hello?" })).StatusCode);

        var check = await client.PostAsJsonAsync("/api/quiz/start", new { area = "History", level = "Primary3", mode = "Interview", figure = "leonardo" });
        var question = (await check.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("question");
        Assert.Equal((3, "Leonardo da Vinci"), (question.GetProperty("length").GetInt32(), question.GetProperty("topic").GetString()));
    }

    [Fact]
    public async Task Free_topic_tests_need_an_ai_engine_in_demo_mode()
    {
        var client = await app.DemoAsync("Student");
        var response = await client.PostAsJsonAsync("/api/quiz/topic", new { area = "History", level = "Primary3", mode = "Practice", topic = "Volcanoes" });
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("needs_ai", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/quiz/start", new { area = "History", level = "Primary3", mode = "Topic" })).StatusCode);
    }

    [Fact]
    public async Task Teachers_cannot_use_the_student_quiz_api()
    {
        var client = await app.DemoAsync("Teacher");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/quiz/start", new { area = "Geography", level = "Primary2", mode = "Practice" })).StatusCode);
    }

    [Fact]
    public async Task The_marketplace_can_be_browsed_without_an_account_but_spaces_are_protected()
    {
        var anonymous = app.Anonymous();
        Assert.Equal(HttpStatusCode.OK, (await anonymous.GetAsync("/Marketplace")).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await anonymous.GetAsync("/Marketplace/Bookings")).StatusCode);
        var student = await app.DemoAsync("Student");
        Assert.Equal(HttpStatusCode.Redirect, (await student.GetAsync("/Institution")).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await student.GetAsync("/Tutor")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await student.PostAsJsonAsync("/api/institution/family", new { studentId = "x", tone = "Warm", language = "English" })).StatusCode);
    }

    [Fact]
    public async Task Director_gets_a_family_message_offline()
    {
        var client = await app.DemoAsync("Director");
        var page = await client.GetStringAsync("/Institution/Risk");
        var studentId = System.Text.RegularExpressions.Regex.Match(page, "data-family=\"([^\"]+)\"").Groups[1].Value;
        var response = await client.PostAsJsonAsync("/api/institution/family", new { studentId, tone = "Formal", language = "French", note = (string?)null });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.StartsWith("Chère famille,", body.GetProperty("message").GetProperty("body").GetString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Learning_loop_never_sends_the_answer_before_the_student_answers()
    {
        var client = await app.DemoAsync("Student");
        var state = await client.GetFromJsonAsync<JsonElement>("/api/learn/fractions");
        var next = state.GetProperty("next");
        Assert.Equal("Practice", next.GetProperty("kind").GetString());
        var concept = next.GetProperty("conceptKey").GetString();

        var response = await client.PostAsync($"/api/learn/fractions/exercise/{concept}", null);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("correctIndex", body, StringComparison.Ordinal);
        Assert.DoesNotContain("misconceptions", body, StringComparison.Ordinal);
        var exercise = JsonDocument.Parse(body).RootElement.GetProperty("exercise");

        var answer = await client.PostAsJsonAsync("/api/learn/answer", new { exerciseId = exercise.GetProperty("id").GetString(), choice = 0 });
        var result = await answer.Content.ReadFromJsonAsync<JsonElement>();
        Assert.InRange(result.GetProperty("correctIndex").GetInt32(), 0, 3);
        Assert.True(result.TryGetProperty("state", out _));
    }

    [Fact]
    public async Task Subject_from_a_topic_needs_an_AI_engine_in_demo_mode()
    {
        var client = await app.DemoAsync("Student");
        var response = await client.PostAsJsonAsync("/api/subjects", new { topic = "The water cycle", stage = "Secondary" });
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("needs_ai", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Teacher_generates_saves_and_reopens_a_lesson_pack_offline()
    {
        var client = await app.DemoAsync("Teacher");
        var sample = await client.GetFromJsonAsync<JsonElement>("/api/samples/pack");
        var response = await client.PostAsJsonAsync("/api/packs", new
        {
            title = sample.GetProperty("title").GetString(),
            subject = sample.GetProperty("subject").GetString(),
            stage = "Secondary",
            text = sample.GetProperty("text").GetString(),
            language = "English",
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("demo", created.GetProperty("engine").GetString());
        var id = created.GetProperty("id").GetInt32();

        var reopened = await client.GetFromJsonAsync<JsonElement>($"/api/packs/{id}");
        Assert.Equal(created.GetProperty("pack").GetProperty("title").GetString(), reopened.GetProperty("pack").GetProperty("title").GetString());
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/packs/{id}")).StatusCode);
    }

    [Fact]
    public async Task Socratic_chat_answers_and_keeps_the_history()
    {
        var client = await app.DemoAsync("Student");
        var reply = await client.PostAsJsonAsync("/api/learn/fractions/chat/add-different", new { message = "Is 1/2 + 1/3 = 2/5?" });
        Assert.Equal(HttpStatusCode.OK, reply.StatusCode);
        var history = await client.GetFromJsonAsync<JsonElement>("/api/learn/fractions/chat/add-different");
        Assert.True(history.GetArrayLength() >= 2);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/learn/fractions/chat/add-different", new { message = "" })).StatusCode);
    }

    [Fact]
    public async Task Interface_and_explanations_switch_language()
    {
        var client = await app.DemoAsync("Student");
        await client.GetAsync("/culture/es?returnUrl=/Student");
        var page = await client.GetStringAsync("/Student");
        Assert.Contains("Fracciones", page, StringComparison.Ordinal);
        var explanation = await client.PostAsync("/api/learn/fractions/explain/add-different", null);
        Assert.Contains("denominador", await explanation.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }
}
