using EduVerse.Core.Catalog;
using EduVerse.Core.Common;
using EduVerse.Core.Curriculum;
using EduVerse.Core.Tutor;
using EduVerse.Infrastructure.Data;
using EduVerse.Infrastructure.Quizzes;
using EduVerse.Infrastructure.Tutor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EduVerse.Tests;

public class PaceRuleTests
{
    private static List<PacePoint> Points(params (int Seconds, bool Correct)[] p) => p.Select(x => new PacePoint(x.Seconds, x.Correct)).ToList();

    [Fact]
    public void Nothing_is_said_without_measured_times_or_when_all_goes_well()
    {
        Assert.Equal(PaceSignal.None, Pace.Read([]));
        Assert.Equal(PaceSignal.None, Pace.Read(Points((0, false), (0, false), (0, false))));
        Assert.Equal(PaceSignal.None, Pace.Read(Points((2, true), (2, true), (2, true)))); // fast and right is fine
        Assert.Equal(PaceSignal.None, Pace.Read(Points((20, true), (120, true)))); // slow and right is fine
    }

    [Fact]
    public void Fast_and_wrong_is_rushing_and_slow_and_wrong_is_stuck()
    {
        Assert.Equal(PaceSignal.Rushing, Pace.Read(Points((20, true), (2, false), (3, true), (1, false))));
        Assert.Equal(PaceSignal.None, Pace.Read(Points((2, false), (3, false), (12, false))));
        Assert.Equal(PaceSignal.Stuck, Pace.Read(Points((20, true), (Pace.SlowSeconds, false))));
    }

    [Fact]
    public void A_long_test_that_started_well_and_drops_is_tiredness()
    {
        var tired = Points((10, true), (10, true), (10, true), (10, true), (10, true), (10, false), (10, false), (10, false), (10, false));
        Assert.Equal(PaceSignal.Tired, Pace.Read(tired));
        Assert.Equal(PaceSignal.None, Pace.Read(tired.Take(Pace.TiredAfter - 1).ToList()));
        var alwaysHard = Points((10, false), (10, false), (10, false), (10, true), (10, false), (10, false), (10, false), (10, false));
        Assert.Equal(PaceSignal.None, Pace.Read(alwaysHard)); // it was hard from the start: not tiredness
    }

    [Fact]
    public void Every_signal_has_advice_in_four_languages_and_seconds_are_clamped()
    {
        foreach (var language in Enum.GetValues<TeachLanguage>())
        {
            Assert.Equal("", Pace.Advice(PaceSignal.None, language));
            Assert.All(new[] { PaceSignal.Rushing, PaceSignal.Stuck, PaceSignal.Tired }, s => Assert.InRange(Pace.Advice(s, language).Length, 30, 200));
        }

        Assert.Equal(0, Pace.Clean(null));
        Assert.Equal(0, Pace.Clean(-5));
        Assert.Equal(Pace.MaxSeconds, Pace.Clean(100_000));
    }
}

public class PrerequisiteRuleTests
{
    [Fact]
    public void Every_step_of_the_map_is_a_real_topic_of_its_level_and_points_down()
    {
        foreach (var (topic, below) in Prerequisites.All)
        {
            Assert.Contains(topic.Topic, AreaSubjects.Topics(Area.Mathematics, topic.Level));
            Assert.Contains(below.Topic, AreaSubjects.Topics(Area.Mathematics, below.Level));
            Assert.True(below.Level <= topic.Level);
            Assert.NotEqual(topic, below);
            var chain = Prerequisites.Chain(Area.Mathematics, topic.Level, topic.Topic);
            Assert.Equal(chain.Count, chain.Distinct().Count()); // no loops
            Assert.DoesNotContain(topic, chain);
        }
    }

    [Fact]
    public void The_chain_goes_down_step_by_step_and_other_areas_have_no_map()
    {
        var chain = Prerequisites.Chain(Area.Mathematics, Level.Primary3, "math.percent");
        Assert.Equal([new TopicRef(Level.Primary3, "math.fraction"), new TopicRef(Level.Primary2, "math.divide"), new TopicRef(Level.Primary2, "math.multiply"), new TopicRef(Level.Primary2, "math.add")], chain);
        Assert.Null(Prerequisites.Of(Area.Physics, Level.Primary3, "math.percent"));
        Assert.Null(Prerequisites.Of(Area.Mathematics, Level.Primary1, "math.add"));
    }

    [Fact]
    public void The_verdict_needs_enough_answers_and_says_where_the_root_is()
    {
        Assert.Equal(RootVerdict.Check, Prerequisites.Verdict(null));
        Assert.Equal(RootVerdict.Check, Prerequisites.Verdict(new TopicEvidence(Prerequisites.MinAnswers - 1, 0)));
        Assert.Equal(RootVerdict.Root, Prerequisites.Verdict(new TopicEvidence(10, 4)));
        Assert.Equal(RootVerdict.Here, Prerequisites.Verdict(new TopicEvidence(10, 8)));
        Assert.Equal(RootVerdict.Check, Prerequisites.Verdict(new TopicEvidence(10, 7))); // in between: keep checking
        Assert.True(Prerequisites.Weak(new TopicEvidence(5, 2)));
        Assert.False(Prerequisites.Weak(new TopicEvidence(3, 0)));
        foreach (var language in Enum.GetValues<TeachLanguage>())
        {
            Assert.All(Enum.GetValues<RootVerdict>(), v =>
            {
                var text = Prerequisites.Say(v, "Fractions", "Division", language);
                Assert.Contains("Fractions", text, StringComparison.Ordinal);
                Assert.Contains("Division", text, StringComparison.Ordinal);
            });
        }
    }
}

public class FadingRuleTests
{
    [Fact]
    public void Every_problem_is_solved_right_and_the_help_fades_stage_by_stage()
    {
        foreach (var kind in Enum.GetValues<FadingKind>())
        {
            for (var seed = 1; seed <= 200; seed++)
            {
                var p = Fading.Generate(kind, seed, TeachLanguage.English);
                Assert.InRange(p.Steps.Count, 2, 3);
                Assert.All(p.Steps, s => Assert.True(s.Result > 0));
                Assert.All(Fading.Lines(p, 0), l => Assert.True(l.Work is not null && l.Result is not null));
                var one = Fading.Lines(p, 1);
                Assert.Null(one[^1].Result);
                Assert.All(one.Take(one.Count - 1), l => Assert.NotNull(l.Result));
                var two = Fading.Lines(p, 2);
                Assert.All(two, l => Assert.Null(l.Result));
                Assert.NotNull(two[0].Work);
                Assert.All(two.Skip(1), l => Assert.Null(l.Work)); // the work of a later step would reveal the result before
                Assert.Empty(Fading.Lines(p, 3));
                Assert.Equal([p.Answer], Fading.Expected(p, 1));
                Assert.Equal(p.Steps.Select(s => s.Result), Fading.Expected(p, 2));
                Assert.Equal(Fading.Expected(p, 2).Count, Fading.Blanks(p, 2));
                Assert.True(Fading.Check(p, 3, [$" {p.Answer} "]));
                Assert.False(Fading.Check(p, 3, [$"{p.Answer + 1}"]));
                Assert.False(Fading.Check(p, 2, [$"{p.Answer}"]));
                Assert.False(Fading.Check(p, 1, null));
            }
        }
    }

    [Fact]
    public void The_steps_really_solve_the_problem()
    {
        for (var seed = 1; seed <= 100; seed++)
        {
            var order = Fading.Generate(FadingKind.Order, seed, TeachLanguage.English);
            var n = order.Problem.Split([' ', '+', '×'], StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToArray();
            Assert.Equal(n[0] + (n[1] * n[2]), order.Answer);
            var equation = Fading.Generate(FadingKind.Equation, seed, TeachLanguage.English);
            var e = equation.Problem.Split(["x + ", " = "], StringSplitOptions.None).Select(int.Parse).ToArray();
            Assert.Equal(e[2], (e[0] * equation.Answer) + e[1]);
            var discount = Fading.Generate(FadingKind.Discount, seed, TeachLanguage.English);
            Assert.Equal(discount.Steps[0].Result - discount.Steps[1].Result, discount.Answer);
            var mean = Fading.Generate(FadingKind.Mean, seed, TeachLanguage.English);
            Assert.Equal(mean.Steps[0].Result, mean.Answer * 3);
        }
    }

    [Fact]
    public void A_new_try_or_a_new_stage_brings_other_numbers_and_only_some_levels_have_it()
    {
        Assert.NotEqual(Fading.Generate(FadingKind.Order, 5, 1, 0, TeachLanguage.English).Problem + Fading.Generate(FadingKind.Order, 5, 2, 0, TeachLanguage.English).Problem,
            Fading.Generate(FadingKind.Order, 5, 1, 1, TeachLanguage.English).Problem + Fading.Generate(FadingKind.Order, 5, 2, 1, TeachLanguage.English).Problem);
        Assert.False(Fading.Available(Level.Primary2));
        Assert.True(Fading.Available(Level.Primary3));
        Assert.DoesNotContain(FadingKind.Equation, Fading.Kinds(Level.Primary3));
        Assert.Contains(FadingKind.Equation, Fading.Kinds(Level.Secondary1));
        foreach (var language in Enum.GetValues<TeachLanguage>())
        {
            Assert.All(Enum.GetValues<FadingKind>(), k => Assert.NotEmpty(Fading.Name(k, language)));
        }
    }
}

public class TutorInsightStoreTests(Database database) : IClassFixture<Database>
{
    private static readonly System.Text.Json.JsonSerializerOptions Web = new(System.Text.Json.JsonSerializerDefaults.Web);

    private static void Answers(EduVerseDb db, string userId, Level level, string topic, int right, int wrong, int confidence = 1)
    {
        for (var i = 0; i < right + wrong; i++)
        {
            db.QuizAnswers.Add(new QuizAnswer { AttemptId = "seeded", UserId = userId, Area = Area.Mathematics, Level = level, Topic = topic, Correct = i < right, Confidence = confidence, At = DateTimeOffset.UtcNow, Number = i + 1, Key = $"{topic}-{confidence}-{i}" });
        }
    }

    [Fact]
    public async Task The_detective_finds_the_root_one_step_below_and_the_false_certainties()
    {
        using var scope = database.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EduVerseDb>();
        var detective = scope.ServiceProvider.GetRequiredService<DetectiveStore>();
        var leo = await database.UserIdAsync("leo@eduverse.demo");
        db.QuizAnswers.RemoveRange(db.QuizAnswers.Where(a => a.UserId == leo));
        Answers(db, leo, Level.Primary3, "math.fraction", 1, 5);
        await db.SaveChangesAsync();

        var first = Assert.Single(await detective.CasesAsync(leo));
        Assert.Equal(("math.fraction", new TopicRef(Level.Primary2, "math.divide"), RootVerdict.Check), (first.Topic, first.Below, first.Verdict));

        Answers(db, leo, Level.Primary2, "math.divide", 1, 4);
        await db.SaveChangesAsync();
        var cases = await detective.CasesAsync(leo);
        Assert.Equal(RootVerdict.Root, cases.First(c => c.Topic == "math.fraction").Verdict);
        Assert.Contains(cases, c => c.Topic == "math.divide" && c.Below.Topic == "math.multiply"); // the weak step has its own step below

        Answers(db, leo, Level.Primary2, "math.divide", 20, 0);
        await db.SaveChangesAsync();
        Assert.Equal(RootVerdict.Here, Assert.Single(await detective.CasesAsync(leo)).Verdict);

        Assert.Empty(await detective.FalseCertaintiesAsync(leo));
        Answers(db, leo, Level.Primary3, "math.percent", 1, 3, confidence: 2);
        Answers(db, leo, Level.Primary3, "math.area", 5, 1, confidence: 2); // sure and almost always right: fine
        await db.SaveChangesAsync();
        var certain = Assert.Single(await detective.FalseCertaintiesAsync(leo));
        Assert.Equal(("math.percent", 3, 4), (certain.Topic, certain.SureWrong, certain.Sure));

        var bets = await detective.BetsAsync(leo); // only "sure" / "not sure" answers count
        Assert.Equal(10, bets.Count);
        Assert.All(bets, b => Assert.True(b.Sure));
        Assert.Equal(["math.percent"], Calibration.Map(bets)[1].Topics);
    }

    [Fact]
    public async Task Pip_stumbles_where_the_student_stumbled()
    {
        using var scope = database.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EduVerseDb>();
        var pips = scope.ServiceProvider.GetRequiredService<PipStore>();
        var nora = await database.UserIdAsync("nora@eduverse.demo");
        db.QuizAnswers.RemoveRange(db.QuizAnswers.Where(a => a.UserId == nora));
        Answers(db, nora, Level.Primary3, "math.percent", 0, 3);
        Answers(db, nora, Level.Primary3, "math.shapes", 0, 9); // no Pip mistake for this topic: skipped
        await db.SaveChangesAsync();

        var views = new List<PipView>();
        for (var i = 0; i < 3; i++)
        {
            views.Add((await pips.StartAsync(nora, Level.Primary3, TeachLanguage.English))!);
        }

        Assert.Equal(["math.percent", "math.percent", null], views.Select(v => v.FromTopic)); // two of three are theirs, one is free
        Assert.All(views.Take(2), v => Assert.Contains("%", v.Problem, StringComparison.Ordinal));
        var outcome = (await pips.AnswerAsync(nora, views[0].Id, 9, 9))!;
        Assert.Equal(PipMistake.Percent, (await pips.FactsAsync(nora, views[0].Id))!.Problem.Mistake);
        Assert.Equal(Pip.Describe(PipMistake.Percent, TeachLanguage.English), views[0].Reasons[outcome.ReasonIndex]);
        Assert.Null((await pips.StartAsync(nora, Level.Primary3, TeachLanguage.English, PipSubject.Language))!.FromTopic);
        Assert.Equal(PipMistake.Carry, Pip.ForTopic("math.add"));
        Assert.Null(Pip.ForTopic("math.shapes"));
    }

    [Fact]
    public async Task The_seconds_are_kept_and_rushing_gets_a_gentle_warning()
    {
        using var scope = database.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EduVerseDb>();
        var quizzes = scope.ServiceProvider.GetRequiredService<QuizStore>();
        var omar = await database.UserIdAsync("omar@eduverse.demo");
        var q = await quizzes.StartAsync(omar, Area.Mathematics, Level.Primary2, QuizMode.Practice, TeachLanguage.English);
        var signals = new List<PaceSignal>();
        for (var i = 0; i < 3; i++)
        {
            var current = System.Text.Json.JsonSerializer.Deserialize<QuizQuestion>((await db.QuizAttempts.AsNoTracking().FirstAsync(a => a.Id == q.AttemptId)).CurrentJson, Web)!;
            signals.Add((await quizzes.AnswerAsync(omar, q.AttemptId, (current.CorrectIndex + 1) % QuizQuestion.Choices, 1, 2, TeachLanguage.English))!.Pace);
        }

        Assert.Equal([PaceSignal.None, PaceSignal.None, PaceSignal.Rushing], signals);
        Assert.All(await db.QuizAnswers.AsNoTracking().Where(a => a.AttemptId == q.AttemptId).ToListAsync(), a => Assert.Equal(2, a.Seconds));
    }

    [Fact]
    public async Task A_fading_run_goes_from_reading_to_solving_alone_and_a_wrong_try_changes_the_problem()
    {
        using var scope = database.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<FadingStore>();
        var db = scope.ServiceProvider.GetRequiredService<EduVerseDb>();
        var sara = await database.UserIdAsync("sara@eduverse.demo");
        Assert.Null(await store.StartAsync(sara, Level.Primary1, FadingKind.Order, TeachLanguage.English));
        Assert.Null(await store.StartAsync(sara, Level.Primary3, FadingKind.Equation, TeachLanguage.English));

        var view = (await store.StartAsync(sara, Level.Primary3, FadingKind.Order, TeachLanguage.English))!;
        Assert.Equal((0, 0), (view.Stage, view.Blanks));
        Assert.Null(await store.AnswerAsync("someone-else", view.Id, null));
        view = (await store.AnswerAsync(sara, view.Id, null))!.Next; // read
        Assert.Equal((1, 1), (view.Stage, view.Blanks));

        var wrong = (await store.AnswerAsync(sara, view.Id, ["-1"]))!;
        Assert.False(wrong.Correct);
        Assert.Equal(1, wrong.Next.Stage);
        Assert.NotEqual(view.Problem, wrong.Next.Problem);
        view = wrong.Next;

        for (var stage = 1; stage < Fading.Stages; stage++)
        {
            var run = await db.FadingRuns.AsNoTracking().FirstAsync(r => r.Id == view.Id);
            var problem = Fading.Generate(run.Kind, run.Seed, run.Stage, run.Tries, run.Language);
            Assert.Equal(problem.Problem, view.Problem);
            var outcome = (await store.AnswerAsync(sara, view.Id, Fading.Expected(problem, stage).Select(x => (string?)x.ToString(System.Globalization.CultureInfo.InvariantCulture)).ToList()))!;
            Assert.True(outcome.Correct);
            view = outcome.Next;
        }

        Assert.True(view.Done);
        Assert.Equal(1, await store.DoneAsync(sara));
        Assert.Null(await store.AnswerAsync(sara, view.Id, ["1"])); // finished
    }
}

public class CalibrationRuleTests
{
    private static List<Bet> Bets(params (string Topic, bool Sure, bool Correct, int Times)[] b) => b.SelectMany(x => Enumerable.Repeat(new Bet(x.Topic, x.Sure, x.Correct), x.Times)).ToList();

    [Fact]
    public void Every_bet_lands_in_one_of_four_boxes_with_its_most_frequent_topics()
    {
        var map = Calibration.Map(Bets(("a", true, true, 3), ("b", true, false, 2), ("c", true, false, 1), ("d", false, true, 2), ("e", false, false, 4)));
        Assert.Equal(Enum.GetValues<Quadrant>(), map.Select(c => c.Quadrant));
        Assert.Equal([3, 3, 2, 4], map.Select(c => c.Count));
        Assert.Equal(["b"], map[1].Topics); // "c" fell there only once: not a pattern yet
        Assert.Equal(["d"], map[2].Topics);
        Assert.All(Calibration.Map([]), c => Assert.Equal(0, c.Count));
    }

    [Fact]
    public void The_score_counts_beliefs_that_matched_and_needs_enough_bets()
    {
        Assert.Null(Calibration.Score(Bets(("a", true, true, Calibration.MinBets - 1))));
        Assert.Equal(100, Calibration.Score(Bets(("a", true, true, 5), ("b", false, false, 5))));
        Assert.Equal(50, Calibration.Score(Bets(("a", true, true, 5), ("b", true, false, 5))));
        Assert.Equal(0, Calibration.Score(Bets(("a", true, false, 5), ("b", false, true, 5))));
    }

    [Fact]
    public void The_trend_compares_the_last_bets_with_the_ones_before()
    {
        var bad = Bets(("a", true, false, Calibration.Window));
        var good = Bets(("a", true, true, Calibration.Window));
        Assert.Equal(CalibrationTrend.Unknown, Calibration.Trend(good));
        Assert.Equal(CalibrationTrend.Better, Calibration.Trend([.. bad, .. good]));
        Assert.Equal(CalibrationTrend.Worse, Calibration.Trend([.. good, .. bad]));
        Assert.Equal(CalibrationTrend.Same, Calibration.Trend([.. good, .. good]));
    }

    [Fact]
    public void Everything_is_said_in_four_languages()
    {
        foreach (var language in Enum.GetValues<TeachLanguage>())
        {
            Assert.Equal("", Calibration.Say(null, CalibrationTrend.Unknown, language));
            Assert.All(new[] { 90, 70, 40 }, s => Assert.Contains($"{s} %", Calibration.Say(s, CalibrationTrend.Same, language), StringComparison.Ordinal));
            Assert.True(Calibration.Say(70, CalibrationTrend.Better, language).Length > Calibration.Say(70, CalibrationTrend.Same, language).Length);
            Assert.Equal(4, Enum.GetValues<Quadrant>().Select(q => Calibration.Name(q, language)).Distinct().Count());
            Assert.All(Enum.GetValues<Quadrant>(), q => Assert.NotEmpty(Calibration.Hint(q, language)));
        }
    }
}
