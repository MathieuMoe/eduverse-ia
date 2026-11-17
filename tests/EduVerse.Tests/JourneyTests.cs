using System.Net;
using EduVerse.Core.Catalog;
using EduVerse.Core.Common;
using EduVerse.Core.Tutor;
using EduVerse.Infrastructure.Data;
using EduVerse.Infrastructure.Tutor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EduVerse.Tests;

public class JourneyRuleTests
{
    private static TopicMonth Month(int year, int month, int answers, int correct, string topic = "math.fractions") =>
        new(Area.Mathematics, Level.Primary3, topic, year, month, answers, correct);

    [Fact]
    public void A_topic_that_turns_from_weak_to_solid_is_reported_with_both_percentages()
    {
        var months = new[] { Month(2026, 1, 10, 2), Month(2026, 2, 10, 9) }; // 20% then 90%
        var found = Assert.Single(Journey.Turnarounds(months));
        Assert.Equal((20, 90, new DateOnly(2026, 1, 1), new DateOnly(2026, 2, 1)), (found.FromPercent, found.ToPercent, found.FromMonth, found.ToMonth));
    }

    [Fact]
    public void A_month_with_too_few_answers_never_starts_or_ends_a_turnaround()
    {
        // weak month has only 3 answers: below MinMonthAnswers, so it cannot anchor a turnaround
        var thin = new[] { Month(2026, 1, 3, 0), Month(2026, 2, 10, 9) };
        Assert.Empty(Journey.Turnarounds(thin));

        // weak month qualifies but the later "solid" month is too thin to count as the recovery
        var thinLater = new[] { Month(2026, 1, 10, 2), Month(2026, 2, 3, 3) };
        Assert.Empty(Journey.Turnarounds(thinLater));
    }

    [Fact]
    public void A_topic_that_was_never_weak_or_never_became_solid_again_is_not_a_turnaround()
    {
        Assert.Empty(Journey.Turnarounds([Month(2026, 1, 10, 8), Month(2026, 2, 10, 9)])); // always fine, nothing to recover from
        Assert.Empty(Journey.Turnarounds([Month(2026, 1, 10, 2), Month(2026, 2, 10, 5)])); // still shaky (50%), not solid yet
    }

    [Fact]
    public void Only_the_best_few_turnarounds_are_kept_most_improved_and_most_recent_first()
    {
        var months = Enumerable.Range(0, Journey.MaxTurnarounds + 2)
            .SelectMany(i => new[] { Month(2026, 1, 10, 2, $"topic-{i}"), Month(2026, 2 + i, 10, 9, $"topic-{i}") })
            .ToList();
        var found = Journey.Turnarounds(months);
        Assert.Equal(Journey.MaxTurnarounds, found.Count);
        Assert.Equal(found.OrderByDescending(t => t.ToMonth).Select(t => t.Topic), found.Select(t => t.Topic));
    }

    [Fact]
    public void The_longest_run_of_consecutive_days_is_found_with_its_last_day()
    {
        Assert.Equal((0, null), Journey.BestStreak([]));
        var d = new DateOnly(2026, 3, 1);
        var days = new[] { d, d.AddDays(1), d.AddDays(2), d.AddDays(10), d.AddDays(11), d.AddDays(1) /* duplicate, ignored */ };
        Assert.Equal((3, d.AddDays(2)), Journey.BestStreak(days));
    }

    [Fact]
    public void Every_sentence_is_available_in_all_four_languages_and_names_the_real_numbers()
    {
        var t = new TopicTurnaround(Area.Mathematics, Level.Primary3, "math.fractions", 20, new DateOnly(2026, 1, 1), 90, new DateOnly(2026, 2, 1));
        foreach (var language in Enum.GetValues<TeachLanguage>())
        {
            Assert.Contains("20", Journey.Say(t, "Fractions", language), StringComparison.Ordinal);
            Assert.Contains("90", Journey.Say(t, "Fractions", language), StringComparison.Ordinal);
            Assert.Contains("Fractions", Journey.SayDefence("Fractions", language), StringComparison.Ordinal);
            Assert.Contains("Fractions", Journey.SayTaught("Fractions", language), StringComparison.Ordinal);
            Assert.Contains("5", Journey.SayStreak(5, language), StringComparison.Ordinal);
        }
    }
}

public class JourneyStoreTests(Database database) : IClassFixture<Database>
{
    private const string StudentId = "journey-student";

    private async Task<(IServiceScope Scope, EduVerseDb Db, JourneyStore Store)> SetupAsync()
    {
        var scope = database.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EduVerseDb>();
        db.QuizAnswers.RemoveRange(db.QuizAnswers.Where(a => a.UserId == StudentId));
        db.DefenceResults.RemoveRange(db.DefenceResults.Where(d => d.UserId == StudentId));
        db.TeachSessions.RemoveRange(db.TeachSessions.Where(s => s.UserId == StudentId));
        db.XpEvents.RemoveRange(db.XpEvents.Where(x => x.UserId == StudentId));
        await db.SaveChangesAsync();
        return (scope, db, scope.ServiceProvider.GetRequiredService<JourneyStore>());
    }

    private static DateTimeOffset Noon(int year, int month, int day) => new(year, month, day, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task A_student_with_no_history_at_all_has_no_candidates()
    {
        var (scope, _, store) = await SetupAsync();
        using var _ = scope;
        var candidates = await store.CandidatesAsync(StudentId);
        Assert.Empty(candidates.Turnarounds);
        Assert.Null(candidates.FirstDefence);
        Assert.Null(candidates.FirstTaught);
        Assert.Equal(0, candidates.BestStreakDays);
    }

    [Fact]
    public async Task A_weak_to_solid_topic_is_found_across_two_calendar_months_of_real_answers()
    {
        var (scope, db, store) = await SetupAsync();
        using var _ = scope;
        void Add(DateTimeOffset at, bool correct) => db.QuizAnswers.Add(new QuizAnswer
        {
            AttemptId = "seeded", UserId = StudentId, Area = Area.Mathematics, Level = Level.Primary3,
            Topic = "math.fractions", Correct = correct, At = at, Number = 1, Key = Guid.NewGuid().ToString(),
        });
        for (var i = 0; i < 10; i++) { Add(Noon(2026, 1, 5), i < 2); } // January: 20% right
        for (var i = 0; i < 10; i++) { Add(Noon(2026, 2, 5), i < 9); } // February: 90% right
        await db.SaveChangesAsync();

        var candidates = await store.CandidatesAsync(StudentId);
        var found = Assert.Single(candidates.Turnarounds);
        Assert.Equal(("math.fractions", 20, 90), (found.Topic, found.FromPercent, found.ToPercent));
    }

    [Fact]
    public async Task Material_quiz_topics_are_never_counted_towards_a_turnaround()
    {
        var (scope, db, store) = await SetupAsync();
        using var _ = scope;
        for (var i = 0; i < 10; i++)
        {
            db.QuizAnswers.Add(new QuizAnswer { AttemptId = "seeded", UserId = StudentId, Area = Area.Mathematics, Level = Level.Primary3, Topic = "topic:Volcanoes", Correct = i < 2, At = Noon(2026, 1, 5), Number = 1, Key = Guid.NewGuid().ToString() });
        }
        await db.SaveChangesAsync();
        Assert.Empty((await store.CandidatesAsync(StudentId)).Turnarounds);
    }

    [Fact]
    public async Task Only_a_truly_verified_defence_counts_as_the_first_one()
    {
        var (scope, db, store) = await SetupAsync();
        using var _ = scope;
        // not reasoned: not a real defence
        db.DefenceResults.Add(new DefenceResult { UserId = StudentId, AttemptId = "a1", Area = Area.Mathematics, Level = Level.Primary3, Topic = "math.fractions", State = DefenceState.Done, Transfer = true, Reasoned = false, At = Noon(2026, 1, 1) });
        await db.SaveChangesAsync();
        Assert.Null((await store.CandidatesAsync(StudentId)).FirstDefence);

        // the real, earlier, verified defence is the one reported, not a later one
        db.DefenceResults.Add(new DefenceResult { UserId = StudentId, AttemptId = "a2", Area = Area.Mathematics, Level = Level.Primary3, Topic = "math.fractions", State = DefenceState.Done, Transfer = true, Reasoned = true, At = Noon(2026, 2, 1) });
        db.DefenceResults.Add(new DefenceResult { UserId = StudentId, AttemptId = "a3", Area = Area.Mathematics, Level = Level.Primary3, Topic = "math.percent", State = DefenceState.Done, Transfer = true, Reasoned = true, At = Noon(2026, 3, 1) });
        await db.SaveChangesAsync();
        var defence = (await store.CandidatesAsync(StudentId)).FirstDefence;
        Assert.Equal(("math.fractions", new DateOnly(2026, 2, 1)), (defence?.Topic, defence?.Date));
    }

    [Fact]
    public async Task Teaching_a_concept_only_counts_once_every_key_idea_was_really_covered()
    {
        var (scope, db, store) = await SetupAsync();
        using var _ = scope;
        var concept = TeachConcepts.All.First(c => c.Ideas.Count > 1);

        // finished (ran out of rounds) but missing an idea: not a real "taught it" milestone
        db.TeachSessions.Add(new TeachSession { Id = "s1", UserId = StudentId, Concept = concept.Key, Covered = concept.Ideas[0].Key, Rounds = TeachConcepts.MaxRounds, FinishedAt = Noon(2026, 1, 1) });
        await db.SaveChangesAsync();
        Assert.Null((await store.CandidatesAsync(StudentId)).FirstTaught);

        // every idea covered: a genuine milestone
        db.TeachSessions.Add(new TeachSession { Id = "s2", UserId = StudentId, Concept = concept.Key, Covered = string.Join(',', concept.Ideas.Select(i => i.Key)), Rounds = 1, FinishedAt = Noon(2026, 2, 1) });
        await db.SaveChangesAsync();
        var taught = (await store.CandidatesAsync(StudentId)).FirstTaught;
        Assert.Equal((concept.Key, new DateOnly(2026, 2, 1)), (taught?.ConceptKey, taught?.Date));
    }

    [Fact]
    public async Task The_best_streak_of_active_days_is_built_from_xp_events_alone()
    {
        var (scope, db, store) = await SetupAsync();
        using var _ = scope;
        void Active(int day, string reason = "answer") => db.XpEvents.Add(new XpEvent { UserId = StudentId, Amount = 5, Reason = reason, At = Noon(2026, 4, day) });
        Active(1); Active(2); Active(3); // a 3-day run
        Active(10); // a lone day later
        Active(20, reason: "badge"); // not an activity day: ignored
        await db.SaveChangesAsync();

        var candidates = await store.CandidatesAsync(StudentId);
        Assert.Equal((3, new DateOnly(2026, 4, 3)), (candidates.BestStreakDays, candidates.BestStreakEnd));
    }
}

public class JourneyWebTests(App app) : IClassFixture<App>
{
    [Fact]
    public async Task The_page_renders_for_a_demo_student_whatever_their_history_holds()
    {
        var client = await app.DemoAsync("Student");
        var response = await client.GetAsync("/Student/Journey");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadAsStringAsync();
        Assert.Contains("data-testid=\"journey", page, StringComparison.Ordinal); // either the list or the empty state
        Assert.DoesNotContain("Journey_Subtitle", page, StringComparison.Ordinal); // the localized text resolved, not the raw key
        Assert.DoesNotContain("Journey_Empty", page, StringComparison.Ordinal);
    }
}
