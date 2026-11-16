using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
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

public class StudyPlanTests
{
    private static readonly DateOnly Today = new(2026, 11, 16);

    private static readonly IReadOnlyList<PlanTopic> Topics =
        [new("a", 10), new("b", 30), new("c", 55), new("d", 75), new("e", 95)];

    [Theory]
    [InlineData(0, 4)]
    [InlineData(39, 4)]
    [InlineData(40, 3)]
    [InlineData(59, 3)]
    [InlineData(60, 2)]
    [InlineData(79, 2)]
    [InlineData(80, 1)]
    [InlineData(100, 1)]
    public void The_sessions_a_topic_needs_follow_what_the_student_knows(int known, int sessions) =>
        Assert.Equal(sessions, StudyPlan.SessionsFor(known));

    [Fact]
    public void Every_session_is_planned_the_weakest_first_and_never_twice_a_day()
    {
        var plan = StudyPlan.Build(Today, Today.AddDays(10), 20, Topics);
        Assert.Equal(10, plan.DaysLeft);
        Assert.Equal(Topics.Sum(t => StudyPlan.SessionsFor(t.Known)), plan.Days.SelectMany(d => d.Topics).Count());
        foreach (var day in plan.Days)
        {
            Assert.Equal(day.Topics.Count, day.Topics.Distinct().Count());
            Assert.True(day.Topics.Count <= 20 / StudyPlan.MinutesPerSession);
        }

        Assert.Equal("a", plan.Days[0].Topics[0]);
        Assert.Equal(4, plan.Days.Count(d => d.Topics.Contains("a")));
        Assert.Equal(1, plan.Days.Count(d => d.Topics.Contains("e")));
    }

    [Fact]
    public void The_mock_test_is_the_day_before_the_exam_only_when_there_is_time()
    {
        var plan = StudyPlan.Build(Today, Today.AddDays(6), 30, Topics);
        Assert.Equal([5], plan.Days.Select((d, i) => (d, i)).Where(x => x.d.Mock).Select(x => x.i));
        Assert.True(plan.Days[5].Topics.Count <= 30 / StudyPlan.MinutesPerSession - StudyPlan.MockSessions);
        Assert.DoesNotContain(StudyPlan.Build(Today, Today.AddDays(2), 30, Topics).Days, d => d.Mock);
    }

    [Fact]
    public void The_plan_says_when_it_does_not_fit_and_how_many_minutes_are_needed()
    {
        var plan = StudyPlan.Build(Today, Today.AddDays(3), 10, Topics);
        Assert.Equal(PlanFeasibility.NotEnough, plan.Feasibility);
        Assert.True(plan.MinutesNeeded > 10);
        Assert.Equal(PlanFeasibility.Comfortable, StudyPlan.Build(Today, Today.AddDays(60), 60, Topics).Feasibility);
        Assert.Contains(plan.MinutesNeeded.ToString(System.Globalization.CultureInfo.InvariantCulture), StudyPlan.Advice(plan, "Maths", TeachLanguage.Spanish), StringComparison.Ordinal);
    }

    [Fact]
    public void An_exam_that_has_come_or_a_subject_without_topics_gives_a_safe_plan()
    {
        var over = StudyPlan.Build(Today, Today, 30, Topics);
        Assert.True(over.Over);
        Assert.Empty(over.Days);
        Assert.NotEmpty(StudyPlan.Advice(over, "", TeachLanguage.Catalan));
        var empty = StudyPlan.Build(Today, Today.AddDays(5), 30, []);
        Assert.Equal((100, PlanFeasibility.Comfortable), (empty.Readiness, empty.Feasibility));
        Assert.Equal(10, StudyPlan.Clamp(0));
        Assert.Equal(60, StudyPlan.Clamp(500));
    }

    [Fact]
    public void The_same_inputs_give_the_same_plan() =>
        Assert.Equal(
            StudyPlan.Build(Today, Today.AddDays(9), 25, Topics).Days.Select(d => string.Join(',', d.Topics)),
            StudyPlan.Build(Today, Today.AddDays(9), 25, Topics).Days.Select(d => string.Join(',', d.Topics)));
}

public class PlanStoreTests(Database database) : IClassFixture<Database>
{
    [Fact]
    public async Task A_goal_is_checked_saved_and_removed()
    {
        using var scope = database.Services.CreateScope();
        var plans = scope.ServiceProvider.GetRequiredService<PlanStore>();
        var iker = await database.UserIdAsync("iker@eduverse.demo");
        var today = plans.Today;

        Assert.Null(await plans.GetAsync(iker));
        Assert.Equal(GoalOutcome.BadDate, await plans.SetAsync(iker, Area.Mathematics, Level.Primary3, "Test", today, 15));
        Assert.Equal(GoalOutcome.BadDate, await plans.SetAsync(iker, Area.Mathematics, Level.Primary3, "Test", today.AddDays(StudyPlan.MaxDays + 1), 15));
        Assert.Equal(GoalOutcome.BadTitle, await plans.SetAsync(iker, Area.Mathematics, Level.Primary3, "write to me at bob@mail.com", today.AddDays(5), 15));
        Assert.Equal(GoalOutcome.BadTitle, await plans.SetAsync(iker, Area.Mathematics, Level.Primary3, new string('x', PlanStore.MaxTitle + 1), today.AddDays(5), 15));
        Assert.Equal(GoalOutcome.NotAvailable, await plans.SetAsync(iker, Area.Programming, Level.EarlyYears, "Test", today.AddDays(5), 15));

        Assert.Equal(GoalOutcome.Saved, await plans.SetAsync(iker, Area.Mathematics, Level.Primary3, "Fractions", today.AddDays(8), 500));
        var view = (await plans.GetAsync(iker))!;
        Assert.Equal((Area.Mathematics, StudyPlan.MaxMinutes, 8), (view.Goal.Area, view.Goal.Minutes, view.Plan.DaysLeft));
        Assert.Equal(AreaSubjects.Topics(Area.Mathematics, Level.Primary3).Count, view.Topics.Count);

        // One goal at a time: a new one replaces the old one
        Assert.Equal(GoalOutcome.Saved, await plans.SetAsync(iker, Area.Physics, Level.Secondary1, "", today.AddDays(20), 20));
        Assert.Equal(Area.Physics, (await plans.GetAsync(iker))!.Goal.Area);
        await plans.ClearAsync(iker);
        Assert.Null(await plans.GetAsync(iker));
    }

    [Fact]
    public async Task A_study_session_of_a_topic_asks_only_that_topic_and_ticks_the_task_of_today()
    {
        using var scope = database.Services.CreateScope();
        var plans = scope.ServiceProvider.GetRequiredService<PlanStore>();
        var quizzes = scope.ServiceProvider.GetRequiredService<QuizStore>();
        var elena = await database.UserIdAsync("elena@eduverse.demo");
        Assert.Equal(GoalOutcome.Saved, await plans.SetAsync(elena, Area.Mathematics, Level.Primary3, "", plans.Today.AddDays(3), 60));
        var task = (await plans.GetAsync(elena))!.Today.First(t => !t.Done);

        var q = await quizzes.StartAsync(elena, Area.Mathematics, Level.Primary3, QuizMode.Focus, TeachLanguage.English, topic: task.Topic);
        Assert.Equal(5, q.Length);
        for (var i = 0; i < StudyPlan.DoneAnswers; i++)
        {
            Assert.Equal(task.Topic, q.Topic);
            var r = await quizzes.AnswerAsync(elena, q.AttemptId, 0, TeachLanguage.English);
            q = r!.Next ?? q;
        }

        Assert.True((await plans.GetAsync(elena))!.Today.First(t => t.Topic == task.Topic).Done);
    }
}

public class PlanWebTests(App app) : IClassFixture<App>
{
    [Fact]
    public async Task The_student_sets_a_goal_sees_the_plan_and_starts_a_session()
    {
        var client = await app.DemoAsync("Student");
        var date = DateTime.UtcNow.AddDays(10).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/plan", new { area = "Mathematics", level = "Primary3", title = "", examOn = "mañana", minutes = 15 })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/plan", new { area = "Mathematics", level = "Primary3", title = "http://x.com", examOn = date, minutes = 15 })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/plan", new { area = "Mathematics", level = "Primary3", title = "Fractions test", examOn = date, minutes = 20 })).StatusCode);

        var page = await client.GetStringAsync("/Student/Plan");
        Assert.Contains("data-testid=\"plan-summary\"", page, StringComparison.Ordinal);
        Assert.Contains("Fractions test", page, StringComparison.Ordinal);
        Assert.Contains("data-testid=\"plan-calendar\"", page, StringComparison.Ordinal);

        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/quiz/start", new { area = "Mathematics", level = "Primary3", mode = "Focus", topic = "not.a.topic" })).StatusCode);
        var topic = AreaSubjects.Topics(Area.Mathematics, Level.Primary3)[0];
        var start = await (await client.PostAsJsonAsync("/api/quiz/start", new { area = "Mathematics", level = "Primary3", mode = "Focus", topic })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(string.IsNullOrWhiteSpace(start.GetProperty("question").GetProperty("topic").GetString()));
        Assert.Contains("mode=Focus", await client.GetStringAsync("/Student/Plan"), StringComparison.Ordinal);

        Assert.Equal(HttpStatusCode.OK, (await client.DeleteAsync("/api/plan")).StatusCode);
        Assert.Contains("data-testid=\"plan-empty\"", await client.GetStringAsync("/Student/Plan"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_page_of_the_plan_is_only_for_students()
    {
        var teacher = await app.DemoAsync("Teacher");
        Assert.NotEqual(HttpStatusCode.OK, (await teacher.GetAsync("/Student/Plan")).StatusCode);
        Assert.NotEqual(HttpStatusCode.OK, (await teacher.PostAsJsonAsync("/api/plan", new { area = "Mathematics", level = "Primary3", examOn = "2030-01-01", minutes = 10 })).StatusCode);
    }
}
