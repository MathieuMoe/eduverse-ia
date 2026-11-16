using EduVerse.Core.Common;
using EduVerse.Core.Curriculum;
using EduVerse.Core.Learning;

namespace EduVerse.Tests;

public class BktTests
{
    [Fact]
    public void A_right_answer_raises_the_probability_and_a_wrong_one_lowers_it()
    {
        var p = BktParameters.Default;
        Assert.True(Bkt.Update(0.5, true, p) > 0.5);
        Assert.True(Bkt.Update(0.5, false, p) < 0.5);
    }

    [Fact]
    public void Probability_stays_strictly_between_0_and_1()
    {
        var p = 0.5;
        for (var i = 0; i < 50; i++)
        {
            p = Bkt.Update(p, true, BktParameters.Default);
        }

        Assert.InRange(p, 0.9, 0.999);
        for (var i = 0; i < 50; i++)
        {
            p = Bkt.Update(p, false, BktParameters.Default);
        }

        Assert.InRange(p, 0.001, 0.5);
    }

    [Fact]
    public void A_right_answer_to_a_hard_exercise_says_more_than_to_an_easy_one()
    {
        var easy = Bkt.Update(0.3, true, BktParameters.ForDifficulty(1));
        var hard = Bkt.Update(0.3, true, BktParameters.ForDifficulty(3));
        Assert.True(hard > easy);
    }

    [Fact]
    public void A_few_right_answers_in_a_row_reach_mastery_from_the_default_prior()
    {
        var p = BktParameters.DefaultPrior;
        var answers = 0;
        while (p < BktParameters.MasteredThreshold && answers < 20)
        {
            p = Bkt.Update(p, true, BktParameters.Default);
            answers++;
        }

        Assert.InRange(answers, 3, 6);
    }
}

public class MasteryEngineTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 1, 10, 0, 0, TimeSpan.Zero);

    private static (MasteryState State, List<LearningEvent> Events) Play(MasteryState state, params (bool Correct, string? Code)[] answers)
    {
        var events = new List<LearningEvent>();
        foreach (var (correct, code) in answers)
        {
            (state, var e) = MasteryEngine.Record(state, correct, 2, code, Now);
            events.AddRange(e);
        }

        return (state, events);
    }

    [Fact]
    public void Two_wrong_answers_in_a_row_switch_to_a_method_not_tried_yet()
    {
        var (state, events) = Play(MasteryState.New("c"), (false, null), (false, null));
        Assert.Equal(ExplanationMethod.Visual, state.Method);
        Assert.Equal([ExplanationMethod.StepByStep, ExplanationMethod.Visual], state.MethodsTried);
        Assert.True(state.NeedsExplanation);
        Assert.Equal(0, state.FailuresWithMethod);
        Assert.Contains(events, e => e.Kind == LearningEventKind.SwitchMethod && e.Detail == "Visual");
    }

    [Fact]
    public void A_right_answer_between_two_mistakes_keeps_the_method()
    {
        var (state, events) = Play(MasteryEngine.Explained(MasteryState.New("c")), (false, null), (true, null), (false, null));
        Assert.Equal(ExplanationMethod.StepByStep, state.Method);
        Assert.DoesNotContain(events, e => e.Kind == LearningEventKind.SwitchMethod);
    }

    [Fact]
    public void The_same_misconception_twice_is_recurring_and_reported_once()
    {
        var (state, events) = Play(MasteryState.New("c"), (false, "AddAcross"), (true, null), (false, "AddAcross"), (true, null), (false, "AddAcross"));
        Assert.Equal(3, state.Misconceptions["AddAcross"]);
        Assert.Single(events, e => e.Kind == LearningEventKind.RecurringMisconception && e.Detail == "AddAcross");
    }

    [Fact]
    public void The_teacher_is_alerted_once_after_three_methods_failed()
    {
        var wrong = Enumerable.Repeat<(bool, string?)>((false, null), 10).ToArray();
        var (state, events) = Play(MasteryState.New("c"), wrong);
        var alert = Assert.Single(events, e => e.Kind == LearningEventKind.NeedsTeacher);
        Assert.Equal("StepByStep,Visual,RealLife", alert.Detail);
        Assert.True(state.TeacherAlerted);
        Assert.True(state.Attempts >= MasteryEngine.MinAttemptsForTeacher);
    }

    [Fact]
    public void No_teacher_alert_while_the_student_progresses()
    {
        var answers = Enumerable.Range(0, 12).Select(i => (i % 3 != 0, (string?)null)).ToArray();
        var (_, events) = Play(MasteryState.New("c"), answers);
        Assert.DoesNotContain(events, e => e.Kind == LearningEventKind.NeedsTeacher);
    }

    [Fact]
    public void Mastered_is_reported_once_when_crossing_the_threshold()
    {
        var right = Enumerable.Repeat<(bool, string?)>((true, null), 10).ToArray();
        var (state, events) = Play(MasteryState.New("c"), right);
        Assert.Equal(MasteryStatus.Mastered, state.Status);
        Assert.Single(events, e => e.Kind == LearningEventKind.Mastered);
        Assert.Equal(10, state.Streak);
    }

    [Theory]
    [InlineData(0, 0, 0.15, 1)]
    [InlineData(3, 0, 0.30, 1)]
    [InlineData(3, 1, 0.50, 2)]
    [InlineData(5, 3, 0.60, 3)]
    [InlineData(5, 1, 0.80, 3)]
    public void Difficulty_starts_easy_goes_up_with_a_streak_and_down_after_mistakes(int attempts, int streak, double p, int expected)
    {
        var state = MasteryState.New("c") with { Attempts = attempts, Streak = streak, PKnown = p };
        Assert.Equal(expected, MasteryEngine.NextDifficulty(state));
    }
}

public class DiagnosticTests
{
    private static readonly Subject Fractions = BuiltInSubjects.Fractions(TeachLanguage.English);

    private static DiagnosticState Run(Func<string, bool> knows)
    {
        var state = Diagnostic.Start(Fractions);
        while (Diagnostic.Next(state) is { } key)
        {
            state = Diagnostic.Answer(state, key, knows(key));
        }

        return state;
    }

    [Fact]
    public void Asks_about_log2_n_plus_one_questions_at_most()
    {
        Assert.Equal(4, Diagnostic.MaxQuestions(7));
        var state = Run(_ => true);
        Assert.True(state.Answers.Count <= Diagnostic.MaxQuestions(Fractions.Concepts.Count));
        Assert.True(state.Finished);
    }

    [Fact]
    public void A_student_who_knows_nothing_starts_at_the_first_concept()
    {
        var (priors, start) = Diagnostic.Result(Run(_ => false));
        Assert.Equal("meaning", start);
        Assert.All(priors.Values, p => Assert.True(p < Diagnostic.KnownPrior));
    }

    [Fact]
    public void A_student_who_knows_the_first_half_starts_after_it()
    {
        var order = Fractions.Ordered().Select(c => c.Key).ToList();
        var known = order.Take(4).ToHashSet();
        var (priors, start) = Diagnostic.Result(Run(known.Contains));
        Assert.Equal(order[4], start);
        Assert.All(order.Take(4), k => Assert.True(priors[k] >= Diagnostic.KnownPrior));
    }

    [Fact]
    public void An_unknown_concept_does_not_change_the_state()
    {
        var state = Diagnostic.Start(Fractions);
        Assert.Same(state, Diagnostic.Answer(state, "nope", true));
    }
}

public class PlannerTests
{
    private static readonly Subject Fractions = BuiltInSubjects.Fractions(TeachLanguage.English);
    private static readonly DateTimeOffset Now = new(2026, 11, 15, 10, 0, 0, TimeSpan.Zero);

    private static MasteryState Mastered(string key, int daysAgo = 1) =>
        MasteryState.New(key, 0.99) with { Attempts = 5, Status = MasteryStatus.Mastered, NeedsExplanation = false, LastAttemptAt = Now.AddDays(-daysAgo) };

    [Fact]
    public void First_time_is_the_placement_test()
    {
        Assert.Equal(StepKind.Diagnostic, NextStepPlanner.Plan(Fractions, new Dictionary<string, MasteryState>(), Now).Kind);
    }

    [Fact]
    public void A_new_concept_is_explained_before_practice()
    {
        var states = new Dictionary<string, MasteryState> { ["meaning"] = Mastered("meaning") };
        var next = NextStepPlanner.Plan(Fractions, states, Now);
        Assert.Equal((StepKind.Learn, "equivalent", "NewConcept"), (next.Kind, next.ConceptKey, next.Reason));
    }

    [Fact]
    public void After_the_explanation_comes_practice()
    {
        var states = new Dictionary<string, MasteryState>
        {
            ["meaning"] = Mastered("meaning"),
            ["equivalent"] = MasteryEngine.Explained(MasteryState.New("equivalent")),
        };
        Assert.Equal(StepKind.Practice, NextStepPlanner.Plan(Fractions, states, Now).Kind);
    }

    [Fact]
    public void A_mastered_concept_not_practised_for_a_week_is_reviewed_first()
    {
        var states = new Dictionary<string, MasteryState> { ["meaning"] = Mastered("meaning", daysAgo: 8) };
        var next = NextStepPlanner.Plan(Fractions, states, Now);
        Assert.Equal((StepKind.Review, "meaning", "SpacedReview"), (next.Kind, next.ConceptKey, next.Reason));
    }

    [Fact]
    public void A_prerequisite_is_revisited_once_a_day_while_a_concept_resists()
    {
        var subject = new Subject("s", "S", "A", [new("a", "A", "", []), new("b", "B", "", ["a"])], false);
        var states = new Dictionary<string, MasteryState>
        {
            ["a"] = Mastered("a", daysAgo: 3),
            ["b"] = MasteryState.New("b", 0.2) with { Attempts = 4, Status = MasteryStatus.Learning, NeedsExplanation = false, LastAttemptAt = Now },
        };
        var next = NextStepPlanner.Plan(subject, states, Now);
        Assert.Equal((StepKind.Review, "a", "ReviewPrerequisite"), (next.Kind, next.ConceptKey, next.Reason));

        // Revisited an hour ago: back to the concept that resists
        states["a"] = Mastered("a") with { LastAttemptAt = Now.AddHours(-1) };
        next = NextStepPlanner.Plan(subject, states, Now);
        Assert.Equal((StepKind.Practice, "b"), (next.Kind, next.ConceptKey));
    }

    [Fact]
    public void Everything_mastered_recently_is_done()
    {
        var states = Fractions.Concepts.ToDictionary(c => c.Key, c => Mastered(c.Key));
        Assert.Equal(StepKind.Done, NextStepPlanner.Plan(Fractions, states, Now).Kind);
    }
}

public class CurriculumTests
{
    [Theory]
    [InlineData(TeachLanguage.English, "Fractions")]
    [InlineData(TeachLanguage.Spanish, "Fracciones")]
    [InlineData(TeachLanguage.French, "Les fractions")]
    [InlineData(TeachLanguage.Catalan, "Fraccions")]
    public void Built_in_subject_is_valid_in_the_4_languages(TeachLanguage language, string name)
    {
        var subject = BuiltInSubjects.Fractions(language);
        Assert.Equal(name, subject.Name);
        Assert.Empty(ConceptMapValidator.Check(subject.Concepts));
        Assert.Equal(BuiltInSubjects.FractionConcepts, subject.Concepts.Select(c => c.Key));
    }

    [Fact]
    public void Ordered_puts_prerequisites_first()
    {
        var subject = new Subject("s", "S", "A", [new("c", "C", "", ["b"]), new("b", "B", "", ["a"]), new("a", "A", "", [])], false);
        Assert.Equal(["a", "b", "c"], subject.Ordered().Select(c => c.Key));
    }

    [Fact]
    public void Validator_finds_duplicates_unknown_prerequisites_and_cycles()
    {
        Assert.Contains("duplicate_key", ConceptMapValidator.Check([new("a", "A", "", []), new("a", "A", "", [])]));
        Assert.Contains("unknown_prerequisite", ConceptMapValidator.Check([new("a", "A", "", ["x"])]));
        Assert.Contains("cycle", ConceptMapValidator.Check([new("a", "A", "", ["b"]), new("b", "B", "", ["a"])]));
        Assert.Contains("concept_count", ConceptMapValidator.Check([]));
    }
}
