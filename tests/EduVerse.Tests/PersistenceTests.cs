using EduVerse.Core.Common;
using EduVerse.Core.Curriculum;
using EduVerse.Core.Learning;
using EduVerse.Infrastructure;
using EduVerse.Infrastructure.Data;
using EduVerse.Infrastructure.Learning;
using EduVerse.Infrastructure.Teaching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EduVerse.Tests;

/// <summary>A SQLite database in a temporary file, created and seeded like the app does on start.</summary>
public sealed class Database : IAsyncLifetime
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"eduverse-test-{Guid.NewGuid():N}.db");

    public ServiceProvider Services { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:EduVerse"] = $"Data Source={_path}" }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(TimeProvider.System);
        services.AddEduVerse(configuration);
        services.AddEduVerseData(configuration);
        Services = services.BuildServiceProvider();
        await Services.InitializeDatabaseAsync(seedDemo: true);
    }

    public async Task DisposeAsync()
    {
        await Services.DisposeAsync();
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        File.Delete(_path);
    }

    public async Task<string> UserIdAsync(string email)
    {
        using var scope = Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<EduVerseDb>().Users.Where(u => u.Email == email).Select(u => u.Id).SingleAsync();
    }
}

public class PersistenceTests(Database database) : IClassFixture<Database>
{
    private const string Fractions = BuiltInSubjects.FractionsKey;

    [Fact]
    public async Task Demo_seed_creates_four_different_learning_histories()
    {
        using var scope = database.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<LearningStore>();
        var db = scope.ServiceProvider.GetRequiredService<EduVerseDb>();

        var alex = await store.StatesAsync(await database.UserIdAsync("alex@eduverse.demo"), Fractions);
        Assert.True(alex.Values.Count(s => s.Status == MasteryStatus.Mastered) >= 6);

        var saraId = await database.UserIdAsync("sara@eduverse.demo");
        Assert.True(await db.Alerts.AnyAsync(a => a.StudentId == saraId && a.Kind == AlertKind.RecurringMisconception && a.Detail == FractionExercises.AddAcross));

        var leoId = await database.UserIdAsync("leo@eduverse.demo");
        var leo = await store.StatesAsync(leoId, Fractions);
        Assert.True(leo["compare"].TeacherAlerted);
        Assert.True(await db.Alerts.AnyAsync(a => a.StudentId == leoId && a.Kind == AlertKind.NeedsTeacher));

        var nora = await store.StatesAsync(await database.UserIdAsync("nora@eduverse.demo"), Fractions);
        Assert.Empty(nora);
        Assert.Equal(StepKind.Diagnostic, NextStepPlanner.Plan(BuiltInSubjects.Fractions(TeachLanguage.English), nora, store.Now).Kind);
    }

    [Fact]
    public async Task An_answer_is_checked_once_on_the_server_and_updates_the_mastery()
    {
        using var scope = database.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<LearningStore>();
        var userId = await database.UserIdAsync("alex@eduverse.demo");
        var exercise = FractionExercises.Create("multiply", 2, new Random(5), TeachLanguage.English);
        var before = await store.StateAsync(userId, Fractions, "multiply");

        var id = await store.AddPendingAsync(userId, Fractions, exercise, diagnostic: false, served: true);
        var outcome = await store.AnswerAsync(userId, id, exercise.CorrectIndex);

        Assert.NotNull(outcome);
        Assert.True(outcome.Correct);
        Assert.Equal(before.Attempts + 1, outcome.State.Attempts);
        Assert.True(outcome.State.PKnown > before.PKnown);
        Assert.Null(await store.AnswerAsync(userId, id, exercise.CorrectIndex)); // already answered
    }

    [Fact]
    public async Task Another_student_cannot_answer_my_exercise()
    {
        using var scope = database.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<LearningStore>();
        var alex = await database.UserIdAsync("alex@eduverse.demo");
        var leo = await database.UserIdAsync("leo@eduverse.demo");
        var id = await store.AddPendingAsync(alex, Fractions, FractionExercises.Create("meaning", 1, new Random(1), TeachLanguage.English), false, true);
        Assert.Null(await store.AnswerAsync(leo, id, 0));
    }

    [Fact]
    public async Task Recurring_mistakes_become_alerts_for_the_teacher()
    {
        using var scope = database.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<LearningStore>();
        var db = scope.ServiceProvider.GetRequiredService<EduVerseDb>();
        var userId = await database.UserIdAsync("nora@eduverse.demo");
        var random = new Random(11);
        for (var i = 0; i < 2; i++)
        {
            var e = FractionExercises.Create("compare", 1, random, TeachLanguage.English);
            var wrong = e.Misconceptions.ToList().IndexOf(FractionExercises.BiggerDenominatorBigger);
            var id = await store.AddPendingAsync(userId, Fractions, e, false, true);
            await store.AnswerAsync(userId, id, wrong);
        }

        Assert.Single(await db.Alerts.Where(a => a.StudentId == userId && a.ConceptKey == "compare").ToListAsync());
        Assert.Equal(2, await db.Attempts.CountAsync(a => a.UserId == userId && a.Misconception == FractionExercises.BiggerDenominatorBigger));
    }

    [Fact]
    public async Task Finishing_the_placement_test_sets_the_starting_mastery()
    {
        using var scope = database.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<LearningStore>();
        var db = scope.ServiceProvider.GetRequiredService<EduVerseDb>();
        var users = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<AppUser>>();
        var user = new AppUser { UserName = "new@test.local", Email = "new@test.local", DisplayName = "New" };
        Assert.True((await users.CreateAsync(user, "password1")).Succeeded);

        var subject = BuiltInSubjects.Fractions(TeachLanguage.English);
        var order = subject.Ordered().Select(c => c.Key).ToList();
        var state = Diagnostic.Start(subject);
        while (Diagnostic.Next(state) is { } key)
        {
            state = Diagnostic.Answer(state, key, order.IndexOf(key) < 3);
        }

        await store.SaveDiagnosticAsync(user.Id, subject.Key, state);
        var start = await store.FinishDiagnosticAsync(user.Id, subject, state);
        var states = await store.StatesAsync(user.Id, subject.Key);

        Assert.Equal(order[3], start);
        Assert.Equal(order.Count, states.Count);
        Assert.False(states[order[0]].NeedsExplanation); // known: straight to a quick practice check
        Assert.True(states[order[5]].NeedsExplanation);
        Assert.False(await db.Diagnostics.AnyAsync(d => d.UserId == user.Id));
    }

    [Fact]
    public async Task Students_join_with_the_code_and_the_teacher_sees_them()
    {
        using var scope = database.Services.CreateScope();
        var classrooms = scope.ServiceProvider.GetRequiredService<ClassroomStore>();
        var store = scope.ServiceProvider.GetRequiredService<LearningStore>();
        var teacher = await database.UserIdAsync(DemoSeeder.TeacherEmail);
        var student = await database.UserIdAsync("nora@eduverse.demo");

        var created = await classrooms.CreateAsync(teacher, "Group C", Fractions);
        Assert.NotNull(created);
        Assert.Null(await classrooms.JoinAsync(student, "ZZZZZZ"));
        Assert.NotNull(await classrooms.JoinAsync(student, created.Code.ToLowerInvariant()));
        Assert.NotNull(await classrooms.JoinAsync(student, created.Code)); // twice: still one membership

        var detail = await classrooms.DetailAsync(teacher, created.Id, TeachLanguage.English);
        Assert.NotNull(detail);
        Assert.Single(detail.Report.Rows);
        Assert.Null(await classrooms.DetailAsync(student, created.Id, TeachLanguage.English)); // not the teacher of this class
        Assert.True(await store.CanStudyAsync(student, Fractions));
    }

    [Fact]
    public async Task A_teacher_resolves_only_alerts_of_their_students()
    {
        using var scope = database.Services.CreateScope();
        var classrooms = scope.ServiceProvider.GetRequiredService<ClassroomStore>();
        var db = scope.ServiceProvider.GetRequiredService<EduVerseDb>();
        var teacher = await database.UserIdAsync(DemoSeeder.TeacherEmail);
        var leo = await database.UserIdAsync("leo@eduverse.demo");
        var alert = await db.Alerts.FirstAsync(a => a.StudentId == leo && a.ResolvedAt == null);

        Assert.False(await classrooms.ResolveAlertAsync(leo, alert.Id));
        Assert.True(await classrooms.ResolveAlertAsync(teacher, alert.Id));
        Assert.False(await classrooms.ResolveAlertAsync(teacher, alert.Id));
    }

    [Fact]
    public async Task Subjects_generated_by_the_AI_are_saved_and_read_back()
    {
        using var scope = database.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<LearningStore>();
        var userId = await database.UserIdAsync("alex@eduverse.demo");
        var subject = new Subject("ai-water-abc123", "Water cycle", "Science", [new("evaporation", "Evaporation", "d", []), new("rain", "Rain", "d", ["evaporation"])], false);

        await store.SaveSubjectAsync(subject, userId, TeachLanguage.English);
        var loaded = await store.FindSubjectAsync(subject.Key, TeachLanguage.French);
        var mine = await store.SubjectsForStudentAsync(userId, TeachLanguage.English);

        Assert.Equal(["evaporation", "rain"], loaded!.Ordered().Select(c => c.Key));
        Assert.Contains(mine, s => s.Key == subject.Key);
        Assert.False(await store.CanStudyAsync(await database.UserIdAsync("leo@eduverse.demo"), subject.Key));
    }
}
