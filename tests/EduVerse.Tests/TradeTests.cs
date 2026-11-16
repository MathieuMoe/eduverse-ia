using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EduVerse.Core.Common;
using EduVerse.Core.Curriculum;
using EduVerse.Core.Learning;
using EduVerse.Infrastructure.Learning;
using Microsoft.Extensions.DependencyInjection;

namespace EduVerse.Tests;

public class TradeRuleTests
{
    [Fact]
    public void Every_trade_has_a_valid_map_and_three_checked_questions_per_concept_in_every_language()
    {
        Assert.Equal(16, Trades.All.Count);
        Assert.Equal(10, Trades.OfKind(TradeKind.Trade).Count);
        Assert.Equal(6, Trades.OfKind(TradeKind.Hobby).Count);
        Assert.Equal(Trades.All.Count, Trades.All.Select(t => t.Key).Distinct().Count());
        foreach (var trade in Trades.All)
        {
            Assert.StartsWith(Trades.Prefix, trade.Key, StringComparison.Ordinal);
            Assert.Equal(5, trade.Concepts.Count);
            foreach (var language in Enum.GetValues<TeachLanguage>())
            {
                var subject = Trades.Subject(trade, language);
                Assert.True(subject.BuiltIn);
                Assert.Empty(ConceptMapValidator.Check(subject.Concepts));
                foreach (var concept in trade.Concepts)
                {
                    var questions = trade.Questions.Where(q => q.Concept == concept.Key).ToList();
                    Assert.Equal(3, questions.Count);
                    Assert.Equal([1, 2, 3], questions.Select(q => q.Difficulty).Order());
                    foreach (var q in questions)
                    {
                        var options = q.Wrong.Select(w => w.In(language)).Prepend(q.Answer.In(language)).ToList();
                        Assert.True(options.Distinct().Count() == 4, $"{trade.Key}/{concept.Key}/{language}: {q.Question.In(language)}");
                        Assert.All(options.Append(q.Question.In(language)).Append(q.Explanation.In(language)), o => Assert.False(string.IsNullOrWhiteSpace(o)));
                    }
                }
            }
        }
    }

    [Fact]
    public void Hobbies_are_a_separate_kind_with_their_own_badge()
    {
        Assert.All(Trades.OfKind(TradeKind.Hobby), h => Assert.Equal("hobbies", Trades.TabOf(h)));
        Assert.Equal("trades", Trades.TabOf(Trades.Find("trade-solar")!));
        Assert.True(Trades.IsTrade("trade-dinosaurs"));
        var stats = new EduVerse.Core.Gamification.BadgeStats(0, 0, 0, 0, 0, 0, 0, 0, 0, false, Certificates: 1, Hobbies: 1);
        var earned = EduVerse.Core.Gamification.Badges.Earned(stats).Select(b => b.Key).ToList();
        Assert.Contains("Hobbyist", earned);
        Assert.Contains("Apprentice", earned);
        Assert.Null(Trades.Find("trade-firstaid")!.Safety is null ? "missing safety note" : null);
    }

    [Fact]
    public void An_exercise_of_a_trade_points_to_the_right_answer_and_names_the_science()
    {
        var trade = Trades.Find("trade-electricity")!;
        var random = new Random(5);
        for (var i = 0; i < 30; i++)
        {
            var e = Trades.Exercise(trade, "ohm", 1 + i % 3, random, TeachLanguage.Spanish);
            var q = trade.Questions.Single(x => x.Question.Es == e.Question);
            Assert.Equal(q.Answer.Es, e.Options[e.CorrectIndex]);
            Assert.Contains("ley de Ohm", e.Explanation, StringComparison.Ordinal);
        }

        Assert.True(Trades.IsTrade("trade-garden"));
        Assert.False(Trades.IsTrade("trade-unknown"));
        Assert.False(Trades.IsTrade(BuiltInSubjects.FractionsKey));
    }

    [Fact]
    public async Task The_offline_tutor_explains_and_practises_a_trade()
    {
        var demo = new DemoLearningAi();
        var subject = Trades.Subject(Trades.Find("trade-plumbing")!, TeachLanguage.French);
        var student = new StudentContext(Stage.Secondary, TeachLanguage.French, [], 0.3, []);
        var exercises = await demo.ExercisesAsync(subject, subject.Find("limescale")!, 2, 3, student);
        Assert.Equal(3, exercises.Count);
        var explanation = await demo.ExplainAsync(subject, subject.Find("pressure")!, ExplanationMethod.WorkedExample, student);
        Assert.Contains("bar", explanation.Body + explanation.Example, StringComparison.Ordinal);
    }
}

public class CertificateTests(Database database) : IClassFixture<Database>
{
    [Fact]
    public async Task The_certificate_is_issued_once_when_every_concept_is_mastered()
    {
        using var scope = database.Services.CreateScope();
        var learning = scope.ServiceProvider.GetRequiredService<LearningStore>();
        var certificates = scope.ServiceProvider.GetRequiredService<CertificateStore>();
        var leo = await database.UserIdAsync("leo@eduverse.demo");
        var trade = Trades.Find("trade-carpentry")!;

        Assert.Null(await certificates.IssueIfEarnedAsync(leo, trade.Key));
        foreach (var concept in trade.Concepts.SkipLast(1))
        {
            await learning.SaveStateAsync(leo, trade.Key, MasteryState.New(concept.Key) with { PKnown = 0.97, Attempts = 6, Correct = 6, Status = MasteryStatus.Mastered });
        }

        Assert.Null(await certificates.IssueIfEarnedAsync(leo, trade.Key));
        await learning.SaveStateAsync(leo, trade.Key, MasteryState.New(trade.Concepts[^1].Key) with { PKnown = 0.97, Attempts = 6, Correct = 6, Status = MasteryStatus.Mastered });
        var issued = await certificates.IssueIfEarnedAsync(leo, trade.Key);
        Assert.NotNull(issued);
        Assert.Matches("^EV-[A-Z2-9]{8}$", issued.Value.Certificate.Code);
        Assert.True(issued.Value.Reward.Xp >= Trades.CertificateXp);
        Assert.Contains("Apprentice", issued.Value.Reward.Badges);
        Assert.Null(await certificates.IssueIfEarnedAsync(leo, trade.Key));

        var progress = (await certificates.ProgressAsync(leo)).Single(p => p.Trade.Key == trade.Key);
        Assert.Equal((5, 5), (progress.Mastered, progress.Total));
        Assert.Equal(issued.Value.Certificate.Code, progress.Certificate!.Code);
    }
}

public class TradeWebTests(App app) : IClassFixture<App>
{
    [Fact]
    public async Task Trades_are_learned_offline_and_the_certificate_page_needs_the_certificate()
    {
        var client = await app.DemoAsync("Student");
        var page = await client.GetStringAsync("/Student/Learn?tab=trades");
        Assert.Contains("data-testid=\"trade-trade-electricity\"", page, StringComparison.Ordinal);
        Assert.Contains("data-testid=\"learn-tab-trades\"", page, StringComparison.Ordinal);
        Assert.DoesNotContain("href=\"/Student/Trades\"", page, StringComparison.Ordinal); // not in the main menu any more
        var old = await client.GetAsync("/Student/Trades");
        Assert.Equal(HttpStatusCode.MovedPermanently, old.StatusCode);
        Assert.Equal("/Student/Learn?tab=trades", old.Headers.Location!.OriginalString);
        var state = await client.GetFromJsonAsync<JsonElement>("/api/learn/trade-garden");
        Assert.True(state.GetProperty("trade").GetBoolean());
        Assert.Equal(5, state.GetProperty("total").GetInt32());
        var exercise = await (await client.PostAsync("/api/learn/trade-garden/exercise/needs", null)).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("code", exercise.GetProperty("engine").GetString());
        Assert.Equal(4, exercise.GetProperty("exercise").GetProperty("options").GetArrayLength());
        Assert.Equal(HttpStatusCode.Redirect, (await client.GetAsync("/Student/Certificate?subject=trade-plumbing")).StatusCode);
        Assert.Contains("EV-GARDEN26", await client.GetStringAsync("/Student/Certificate?subject=trade-garden"), StringComparison.Ordinal);
        Assert.Contains("optgroup", await client.GetStringAsync("/Student/Learn?subject=trade-electricity"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Hobbies_have_their_own_tab_and_are_learned_like_trades()
    {
        var client = await app.DemoAsync("Student");
        var trades = await client.GetStringAsync("/Student/Learn?tab=trades");
        Assert.Contains("data-testid=\"trade-trade-solar\"", trades, StringComparison.Ordinal);
        Assert.DoesNotContain("data-testid=\"trade-trade-chess\"", trades, StringComparison.Ordinal);
        var hobbies = await client.GetStringAsync("/Student/Learn?tab=hobbies");
        Assert.Contains("data-testid=\"learn-tab-hobbies\"", hobbies, StringComparison.Ordinal);
        foreach (var key in new[] { "astronomy", "music", "magic", "survival", "chess", "dinosaurs" })
        {
            Assert.Contains($"data-testid=\"trade-trade-{key}\"", hobbies, StringComparison.Ordinal);
        }

        Assert.DoesNotContain("data-testid=\"trade-trade-garden\"", hobbies, StringComparison.Ordinal);
        var state = await client.GetFromJsonAsync<JsonElement>("/api/learn/trade-chess");
        Assert.True(state.GetProperty("trade").GetBoolean());
        var exercise = await (await client.PostAsync("/api/learn/trade-chess/exercise/board", null)).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("code", exercise.GetProperty("engine").GetString());
        var learn = await client.GetStringAsync("/Student/Learn?subject=trade-dinosaurs");
        Assert.Contains("href=\"/Student/Learn?tab=hobbies\"", learn, StringComparison.Ordinal);
        Assert.Equal("/Student/Learn?tab=hobbies", (await client.GetAsync("/Student/Certificate?subject=trade-magic")).Headers.Location!.OriginalString);
    }
}

public class AreaSubjectTests
{
    [Fact]
    public void Every_subject_and_level_of_the_catalog_is_a_tutor_subject_with_topics()
    {
        foreach (var info in EduVerse.Core.Catalog.Catalog.Areas)
        {
            foreach (var level in Enum.GetValues<EduVerse.Core.Catalog.Level>().Where(l => EduVerse.Core.Catalog.Catalog.Available(info.Area, l)))
            {
                var key = AreaSubjects.Key(info.Area, level);
                Assert.True(AreaSubjects.TryParse(key, out var area, out var parsed));
                Assert.Equal((info.Area, level), (area, parsed));
                var subject = AreaSubjects.Subject(info.Area, level, new KeyCatalogNames(), TeachLanguage.Spanish);
                Assert.True(subject.Concepts.Count >= 1, key);
                Assert.Empty(ConceptMapValidator.Check(subject.Concepts));
                Assert.All(subject.Concepts, c => Assert.False(string.IsNullOrWhiteSpace(c.Description)));
            }
        }

        Assert.False(AreaSubjects.TryParse("area-Physics-EarlyYears", out _, out _));
        Assert.False(AreaSubjects.TryParse("area-Nope-Primary1", out _, out _));
    }

    [Fact]
    public void Exercises_of_a_catalog_subject_are_of_the_topic_and_keep_their_picture()
    {
        var random = new Random(3);
        var e = AreaSubjects.Exercise(EduVerse.Core.Catalog.Area.Mathematics, EduVerse.Core.Catalog.Level.Primary1, "math.clock", 2, random, TeachLanguage.English);
        Assert.Equal("math.clock", e.ConceptKey);
        Assert.StartsWith("dg:clock:", e.Visual, StringComparison.Ordinal);
        Assert.InRange(e.CorrectIndex, 0, e.Options.Count - 1);
    }
}

public class LearnCatalogWebTests(App app) : IClassFixture<App>
{
    [Fact]
    public async Task Learn_starts_with_a_catalog_and_any_subject_is_learned_offline()
    {
        var client = await app.DemoAsync("Student");
        var page = await client.GetStringAsync("/Student/Learn");
        Assert.Contains("data-testid=\"learn-subjects\"", page, StringComparison.Ordinal);
        Assert.Contains("learn-area-History-", page, StringComparison.Ordinal);
        Assert.Contains("learn-trade-electricity", page, StringComparison.Ordinal);
        var physics = "area-Physics-" + (page.Contains("learn-area-Physics-Primary3", StringComparison.Ordinal) ? "Primary3" : "Secondary1");
        var state = await client.GetFromJsonAsync<JsonElement>("/api/learn/" + physics);
        var concept = state.GetProperty("concepts")[0].GetProperty("key").GetString();
        var explain = await client.PostAsync($"/api/learn/{physics}/explain/{concept}", null);
        Assert.Equal(HttpStatusCode.OK, explain.StatusCode);
        var exercise = await (await client.PostAsync($"/api/learn/{physics}/exercise/{concept}", null)).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("code", exercise.GetProperty("engine").GetString());
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/Student/Learn?subject=" + physics)).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await client.GetAsync("/Student/Learn?subject=area-Physics-Nope")).StatusCode);
    }
}
