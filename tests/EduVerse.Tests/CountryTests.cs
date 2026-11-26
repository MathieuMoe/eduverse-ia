using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EduVerse.Core.Common;
using EduVerse.Core.Gamification;
using EduVerse.Core.Tutor;
using EduVerse.Infrastructure.Data;
using EduVerse.Infrastructure.Gamification;
using Microsoft.Extensions.DependencyInjection;

namespace EduVerse.Tests;

public class CountryContentTests
{
    [Fact]
    public void Every_country_is_a_timeline_in_order_written_in_every_language()
    {
        Assert.True(Countries.All.Count >= 8);
        Assert.Equal(Countries.All.Count, Countries.All.Select(c => c.Key).Distinct().Count());
        foreach (var c in Countries.All)
        {
            Assert.InRange(c.Eras.Count, CountryHistoryPrompt.MinEras, CountryHistoryPrompt.MaxEras);
            Assert.Equal(c.Eras.Count, c.Eras.Select(e => e.Key).Distinct().Count());
            Assert.Equal(c.Eras.OrderBy(e => e.From).Select(e => e.Key), c.Eras.Select(e => e.Key));
            Assert.Equal(Countries.Today, c.Eras[^1].To);
            foreach (var e in c.Eras)
            {
                Assert.True(e.From < e.To, $"{c.Key}/{e.Key}");
                Assert.InRange(e.People.Count, 1, 3);
                foreach (var language in Enum.GetValues<TeachLanguage>())
                {
                    Assert.All([c.Name, e.Title, e.Story, e.Oddity, .. e.People.Select(p => p.Name), .. e.People.Select(p => p.Role)],
                        text => Assert.False(string.IsNullOrWhiteSpace(text.In(language)), $"{c.Key}/{e.Key}/{language}"));
                }

                Assert.All(e.People.Where(p => p.Figure is not null), p => Assert.NotNull(Interviews.Find(p.Figure)));
            }
        }
    }

    [Fact]
    public void A_check_has_three_different_questions_with_four_options_and_one_right_answer_and_changes_every_time()
    {
        foreach (var c in Countries.All)
        {
            for (var era = 0; era < c.Eras.Count; era++)
            {
                var prompts = new HashSet<string>();
                foreach (var language in Enum.GetValues<TeachLanguage>())
                {
                    for (var seed = 0; seed < 12; seed++)
                    {
                        var check = CountryQuiz.Check(c, era, seed, language);
                        Assert.Equal(Countries.CheckLength, check.Count);
                        Assert.Equal(check.Count, check.Select(q => q.Prompt).Distinct().Count());
                        foreach (var q in check)
                        {
                            Assert.Equal(4, q.Options.Count);
                            Assert.Equal(4, q.Options.Distinct().Count());
                            Assert.InRange(q.Correct, 0, 3);
                            Assert.False(string.IsNullOrWhiteSpace(q.Explanation));
                            prompts.Add(q.Prompt);
                        }
                    }
                }

                Assert.True(prompts.Count > Countries.CheckLength * 4, $"{c.Key}/{era} always asks the same");
            }
        }
    }

    [Fact]
    public void The_right_answer_of_a_check_is_the_fact_of_the_era()
    {
        var spain = Countries.Find("spain")!;
        var era = spain.Eras.Select((e, i) => (e, i)).First(x => x.e.Key == "catholicmonarchs");
        for (var seed = 0; seed < 30; seed++)
        {
            foreach (var q in CountryQuiz.Check(spain, era.i, seed, TeachLanguage.English))
            {
                var right = q.Options[q.Correct];
                if (q.Prompt.StartsWith("When", StringComparison.Ordinal))
                {
                    Assert.Equal("1469 – 1516", right);
                }
                else if (q.Prompt.StartsWith("Which strange", StringComparison.Ordinal))
                {
                    Assert.Equal(era.e.Oddity.En, right);
                }
                else if (q.Prompt.StartsWith("Who", StringComparison.Ordinal))
                {
                    Assert.Contains(right, era.e.People.Select(p => p.Name.En));
                }
            }
        }

        Assert.Equal(2, CountryQuiz.Score(CountryQuiz.Check(spain, 0, 1, TeachLanguage.Spanish), [.. CountryQuiz.Check(spain, 0, 1, TeachLanguage.Spanish).Select((q, i) => i == 0 ? (q.Correct + 1) % 4 : q.Correct)]));
        Assert.Equal("218 a. C.", Countries.Year(-218, TeachLanguage.Spanish));
        Assert.Equal("today", Countries.Year(Countries.Today, TeachLanguage.English));
    }

    [Fact]
    public void The_timeline_challenge_starts_shuffled_and_only_the_right_order_wins()
    {
        foreach (var c in Countries.All)
        {
            for (var seed = 0; seed < 20; seed++)
            {
                var shuffled = CountryOrder.Shuffle(c, seed);
                Assert.Equal(c.Eras.Select(e => e.Key).Order(), shuffled.Select(e => e.Key).Order());
                Assert.NotEqual(c.Eras.Select(e => e.Key), shuffled.Select(e => e.Key));
                Assert.True(CountryOrder.RightPlaces(c, [.. shuffled.Select(e => e.Key)]) < c.Eras.Count);
            }

            Assert.Equal(c.Eras.Count, CountryOrder.RightPlaces(c, [.. c.Eras.Select(e => e.Key)]));
        }

        Assert.Contains(Badges.All, b => b.Key == "Globetrotter");
    }

    [Fact]
    public void A_timeline_written_by_the_ai_is_checked_before_it_is_walked()
    {
        static string Era(int from, int to, string person) =>
            $$"""{"title":"Era {{from}}","emoji":"🏺","from":{{from}},"to":{{to}},"story":"Things happened.","oddity":"Something odd.","people":[{"name":"{{person}}","emoji":"👤","role":"Did things"}]}""";
        var good = JsonDocument.Parse($$"""{"country":"Italy","flag":"🇮🇹","eras":[{{Era(-753, -27, "A")}},{{Era(-27, 476, "B")}},{{Era(476, 1400, "C")}},{{Era(1400, 1861, "D")}},{{Era(1861, 9999, "E")}}]}""").RootElement;
        var italy = CountryHistoryPrompt.Parse(good, "ai-1");
        Assert.True(italy.Custom);
        Assert.Equal(5, italy.Eras.Count);
        Assert.Equal("Italy", italy.Name.In(TeachLanguage.French));
        Assert.Equal(3, CountryQuiz.Check(italy, 2, 7, TeachLanguage.English).Count);

        // Out of order, too short, or with too few people: refused
        var unordered = JsonDocument.Parse($$"""{"country":"X","flag":"🏳️","eras":[{{Era(1000, 1100, "A")}},{{Era(-500, 0, "B")}},{{Era(1100, 1200, "C")}},{{Era(1200, 1300, "D")}},{{Era(1300, 1400, "E")}}]}""").RootElement;
        Assert.Throws<EduVerse.Core.Abstractions.AiGenerationException>(() => CountryHistoryPrompt.Parse(unordered, "ai-2"));
        var samePeople = JsonDocument.Parse($$"""{"country":"X","flag":"🏳️","eras":[{{Era(1, 2, "A")}},{{Era(2, 3, "A")}},{{Era(3, 4, "A")}},{{Era(4, 5, "B")}},{{Era(5, 6, "B")}}]}""").RootElement;
        Assert.Throws<EduVerse.Core.Abstractions.AiGenerationException>(() => CountryHistoryPrompt.Parse(samePeople, "ai-3"));
        Assert.Contains("<country>Italy</country>", CountryHistoryPrompt.Build("Italy", TeachLanguage.English).User, StringComparison.Ordinal);
    }
}

public class CountryStoreTests(Database database) : IClassFixture<Database>
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task Eras_open_in_order_the_first_pass_pays_and_the_last_era_stamps_the_passport()
    {
        using var scope = database.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EduVerseDb>();
        var store = scope.ServiceProvider.GetRequiredService<CountryStore>();
        var game = scope.ServiceProvider.GetRequiredService<GamificationStore>();
        var student = await QuizDriver.NewStudentAsync(db, TimeProvider.System);
        var japan = Countries.Find("japan")!;

        var view = await store.ViewAsync(student, japan);
        Assert.Equal([EraState.Open, .. Enumerable.Repeat(EraState.Locked, japan.Eras.Count - 1)], view.Eras);
        Assert.Null(await store.StartCheckAsync(student, "japan", 1, TeachLanguage.English)); // locked
        Assert.Null(await store.StartCheckAsync(student, "nowhere", 0, TeachLanguage.English));

        // A failed check pays nothing and keeps the era open
        var failed = await store.StartCheckAsync(student, "japan", 0, TeachLanguage.English);
        var wrong = await store.AnswerCheckAsync(student, failed!.Id, [-1, -1, -1]);
        Assert.False(wrong!.Passed);
        Assert.Equal(0, wrong.Reward.Xp);
        Assert.Null(await store.AnswerCheckAsync(student, failed.Id, [0, 0, 0])); // answered once

        Assert.Null(await store.OrderAsync(student, "japan", [.. japan.Eras.Select(e => e.Key)])); // not stamped yet
        var gemsBefore = await game.GemsAsync(student);
        for (var era = 0; era < japan.Eras.Count; era++)
        {
            var result = await PassAsync(db, store, student, era);
            Assert.True(result.Passed);
            Assert.Equal(Countries.EraXp, result.Reward.Xp);
            Assert.Equal(era == japan.Eras.Count - 1, result.Stamp);
        }

        Assert.True(await game.GemsAsync(student) >= gemsBefore + japan.Eras.Count * Countries.EraGems + Countries.StampGems);
        view = await store.ViewAsync(student, japan);
        Assert.True(view.Stamped);
        Assert.Equal(japan.Eras.Count, (await store.ProgressAsync(student))["japan"]);

        Assert.Equal(1, (await game.BadgeStatsAsync(student)).Stamps);

        // The timeline challenge: a wrong order is only a try, the right one turns the stamp gold once
        var keys = japan.Eras.Select(e => e.Key).ToList();
        var miss = await store.OrderAsync(student, "japan", [.. keys.AsEnumerable().Reverse()]);
        Assert.False(miss!.Won);
        Assert.False((await store.ViewAsync(student, japan)).Gold);
        var win = await store.OrderAsync(student, "japan", keys);
        Assert.True(win!.Won);
        Assert.Equal(CountryOrder.GoldGems, win.Gems);
        Assert.Equal(0, (await store.OrderAsync(student, "japan", keys))!.Gems);
        Assert.True((await store.ViewAsync(student, japan)).Gold);
        Assert.Contains("japan", await store.GoldAsync(student));
        Assert.Equal(japan.Eras.Count, (await store.ProgressAsync(student))["japan"]); // the challenge is not an era

        // Passing an era again is allowed but pays nothing
        var again = await PassAsync(db, store, student, 0);
        Assert.True(again.Passed);
        Assert.Equal(0, again.Reward.Xp);
        Assert.False(again.Stamp);
    }

    [Fact]
    public async Task A_timeline_written_by_the_ai_belongs_to_its_student()
    {
        using var scope = database.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EduVerseDb>();
        var store = scope.ServiceProvider.GetRequiredService<CountryStore>();
        var owner = await QuizDriver.NewStudentAsync(db, TimeProvider.System);
        var other = await QuizDriver.NewStudentAsync(db, TimeProvider.System);
        var key = await store.SaveCustomAsync(owner, k => Countries.Find("mexico")! with { Key = k, Custom = true });

        Assert.StartsWith(CountryStore.CustomPrefix, key, StringComparison.Ordinal);
        var mine = await store.FindAsync(owner, key);
        Assert.Equal(Countries.Find("mexico")!.Eras.Count, mine!.Eras.Count);
        Assert.Equal("México", mine.Name.In(TeachLanguage.Spanish));
        Assert.Single(await store.CustomAsync(owner));
        Assert.Null(await store.FindAsync(other, key));
        Assert.NotNull(await store.StartCheckAsync(owner, key, 0, TeachLanguage.Catalan));
        Assert.Null(await store.StartCheckAsync(other, key, 0, TeachLanguage.Catalan));
    }

    private static async Task<CheckResult> PassAsync(EduVerseDb db, CountryStore store, string student, int era)
    {
        var check = await store.StartCheckAsync(student, "japan", era, TeachLanguage.Spanish);
        Assert.NotNull(check);
        var questions = JsonSerializer.Deserialize<List<HistoryQuestion>>(db.CountryChecks.Single(c => c.Id == check.Id).QuestionsJson, Web)!;
        return (await store.AnswerCheckAsync(student, check.Id, [.. questions.Select(q => q.Correct)]))!;
    }
}

public class CountryWebTests(App app) : IClassFixture<App>
{
    [Fact]
    public async Task The_trips_page_lists_the_countries_and_a_new_country_needs_an_ai_engine()
    {
        var client = await app.DemoAsync("Student");
        var list = await client.GetStringAsync("/Student/Countries");
        Assert.Contains("data-testid=\"country-spain\"", list, StringComparison.Ordinal);
        Assert.Contains("data-testid=\"passport\"", list, StringComparison.Ordinal);

        var timeline = await client.GetStringAsync("/Student/Countries?country=france");
        Assert.Contains("data-testid=\"country-timeline\"", timeline, StringComparison.Ordinal);
        Assert.Contains("data-state=\"Locked\"", timeline, StringComparison.Ordinal);

        var locked = await client.PostAsync("/api/countries/france/eras/5/check", null);
        Assert.Equal(HttpStatusCode.Conflict, locked.StatusCode);
        var open = await client.PostAsync("/api/countries/france/eras/0/check", null);
        open.EnsureSuccessStatusCode();
        var check = await open.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(Countries.CheckLength, check.GetProperty("questions").GetArrayLength());
        Assert.False(check.GetProperty("questions")[0].TryGetProperty("correct", out _)); // the key stays on the server
        var bad = await client.PostAsJsonAsync($"/api/countries/checks/{check.GetProperty("id").GetString()}", new { answers = new[] { 0 } });
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
        var marked = await client.PostAsJsonAsync($"/api/countries/checks/{check.GetProperty("id").GetString()}", new { answers = new[] { 0, 1, 2 } });
        marked.EnsureSuccessStatusCode();
        Assert.Equal(3, (await marked.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("results").GetArrayLength());

        var ai = await client.PostAsJsonAsync("/api/countries", new { name = "Italy" });
        Assert.Equal(HttpStatusCode.Conflict, ai.StatusCode);
        Assert.Equal("needs_ai", (await ai.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());

        // A lesson of the learning path still works offline (the banks answer, the stage sets the difficulty)
        var topic = EduVerse.Core.Curriculum.AreaSubjects.Topics(EduVerse.Core.Catalog.Area.History, EduVerse.Core.Catalog.Level.Primary3)[0];
        var lesson = await client.PostAsJsonAsync("/api/quiz/start", new { area = "History", level = "Primary3", mode = "Focus", topic, path = true });
        lesson.EnsureSuccessStatusCode();
        Assert.Equal("bank", (await lesson.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("engine").GetString());
    }
}
