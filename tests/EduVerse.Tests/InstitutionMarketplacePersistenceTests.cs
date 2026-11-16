using EduVerse.Core.Common;
using EduVerse.Core.Institutions;
using EduVerse.Core.Marketplace;
using EduVerse.Infrastructure;
using EduVerse.Infrastructure.Data;
using EduVerse.Infrastructure.Institutions;
using EduVerse.Infrastructure.Marketplace;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EduVerse.Tests;

public class InstitutionPersistenceTests(Database database) : IClassFixture<Database>
{
    [Fact]
    public async Task The_director_sees_every_class_of_the_institution_with_the_dropout_signals()
    {
        using var scope = database.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<InstitutionStore>();
        var institution = await store.ForUserAsync(await database.UserIdAsync(DemoSeeder.DirectorEmail));
        Assert.NotNull(institution);

        var d = await store.DashboardAsync(institution, TeachLanguage.English);

        Assert.Equal(["Maths 2B", "Maths 3A"], d.Classes.Select(c => c.Name));
        Assert.Equal(9, d.Students);
        Assert.Subset(d.Teachers.Select(t => t.Name).ToHashSet(), new HashSet<string> { "Laura", "Pablo" });
        var omar = d.Risks.Single(r => r.Student.Name == "Omar");
        Assert.Equal(RiskLevel.High, omar.Level);
        Assert.Contains(omar.Factors, f => f.Code == "Inactive" && f.Value >= 14);
        Assert.Contains(d.Risks.Single(r => r.Student.Name == "Hugo").Factors, f => f.Code == "NeverStarted");
        Assert.Contains(d.Risks.Single(r => r.Student.Name == "Elena").Factors, f => f.Code == "ActivityDrop");
        Assert.Equal(Plans.Academy.MonthlyBase, d.Bill.Total);
        Assert.True(d.Quality.AlertsResolved >= 1);
        Assert.NotNull(d.Quality.Best);
    }

    [Fact]
    public async Task Family_facts_hold_only_what_the_platform_measured_and_stay_inside_the_institution()
    {
        using var scope = database.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<InstitutionStore>();
        var institution = (await store.ForUserAsync(await database.UserIdAsync(DemoSeeder.DirectorEmail)))!;

        var facts = await store.FamilyFactsAsync(institution, await database.UserIdAsync("leo@eduverse.demo"), TeachLanguage.Spanish);

        Assert.NotNull(facts);
        Assert.Equal(("Leo", "Fracciones", 7), (facts.FirstName, facts.Subject, facts.Total));
        Assert.Contains("Comparar fracciones", facts.ToWorkOn);
        Assert.NotEmpty(facts.Mistakes);
        Assert.Null(await store.FamilyFactsAsync(institution, await database.UserIdAsync("tutor@eduverse.demo"), TeachLanguage.Spanish));
    }

    [Fact]
    public async Task A_teacher_joins_one_institution_with_its_code()
    {
        using var scope = database.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<InstitutionStore>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var teacher = new AppUser { UserName = "new-teacher@test.local", Email = "new-teacher@test.local", DisplayName = "Rosa" };
        Assert.True((await users.CreateAsync(teacher, "password1")).Succeeded);

        Assert.Null(await store.JoinAsTeacherAsync(teacher.Id, "ZZZZZZ"));
        Assert.NotNull(await store.JoinAsTeacherAsync(teacher.Id, "hrzn-24"));
        Assert.Null(await store.JoinAsTeacherAsync(teacher.Id, DemoSeeder.InstitutionCode)); // already a member
        var created = await store.CreateAsync("New Academy", (await database.UserIdAsync("alex@eduverse.demo")));
        Assert.True(Core.Teaching.JoinCode.IsValid(created.Code));
    }
}

public class MarketplacePersistenceTests(Database database) : IClassFixture<Database>
{
    [Fact]
    public async Task Published_teachers_have_their_rating_and_the_student_gets_maths_teachers_first()
    {
        using var scope = database.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<MarketplaceStore>();

        var tutors = await store.TutorsAsync();
        Assert.Equal(6, tutors.Count);
        Assert.All(tutors, t => Assert.NotNull(t.Rating));
        Assert.Single(await store.TutorsAsync(new TutorFilter("french", null, null, null)));
        Assert.Equal(2, (await store.TutorsAsync(new TutorFilter(null, TeachLanguage.Catalan, null, null))).Count);

        var recommended = await store.RecommendAsync(await database.UserIdAsync("sara@eduverse.demo"), TeachLanguage.Spanish);
        Assert.Contains("Subject", recommended[0].Reasons);
        Assert.Contains("Mathematics", recommended[0].Tutor.Subjects);
    }

    [Fact]
    public async Task Booking_paying_and_cancelling_follow_the_rules()
    {
        using var scope = database.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<MarketplaceStore>();
        var db = scope.ServiceProvider.GetRequiredService<EduVerseDb>();
        var tutor = await database.UserIdAsync("claire@eduverse.demo");
        var student = await database.UserIdAsync("nora@eduverse.demo");
        var other = await database.UserIdAsync("alex@eduverse.demo");

        var slots = await store.SlotsAsync(tutor);
        Assert.NotEmpty(slots);
        Assert.All(slots, s => Assert.True(s - store.Now >= Availability.MinNotice));

        var (result, booking) = await store.BookAsync(student, tutor, slots[^1], "French", "DELF B1");
        Assert.Equal(BookResult.Booked, result);
        Assert.Equal(BookingStatus.Pending, booking!.Status);
        Assert.DoesNotContain(slots[^1], await store.SlotsAsync(tutor)); // held while waiting for the payment
        Assert.Equal(BookResult.SlotTaken, (await store.BookAsync(other, tutor, slots[^1], "French", null)).Result);

        Assert.False(await store.PayAsync(other, booking.Id)); // not my booking
        Assert.True(await store.PayAsync(student, booking.Id));
        var paid = await db.Bookings.AsNoTracking().SingleAsync(b => b.Id == booking.Id);
        Assert.Equal(BookingStatus.Confirmed, paid.Status);
        Assert.StartsWith("SIM-", paid.PaymentReference, StringComparison.Ordinal);
        Assert.Equal(Commission.Split(paid.Price, paid.CommissionRate).Commission, paid.Commission);

        Assert.True(await store.CancelAsync(student, booking.Id));
        var cancelled = await db.Bookings.AsNoTracking().SingleAsync(b => b.Id == booking.Id);
        Assert.Equal(BookingStatus.Cancelled, cancelled.Status);
        Assert.Equal(cancelled.Start - store.Now >= BookingRules.FreeCancellation, cancelled.Refunded);
        Assert.Contains(slots[^1], await store.SlotsAsync(tutor)); // free again
    }

    [Fact]
    public async Task Earnings_are_net_of_commission()
    {
        using var scope = database.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<MarketplaceStore>();
        var e = await store.EarningsAsync(await database.UserIdAsync(DemoSeeder.TutorEmail));
        Assert.Equal(e.Gross - e.Commission, e.Net);
        Assert.Equal(6, e.Months.Count);
        Assert.Contains(e.CurrentRate, new[] { Commission.Standard, Commission.Medium, Commission.Low });
    }

    [Fact]
    public async Task Only_a_finished_lesson_can_be_rated_once()
    {
        using var scope = database.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<MarketplaceStore>();
        var db = scope.ServiceProvider.GetRequiredService<EduVerseDb>();
        var sara = await database.UserIdAsync("sara@eduverse.demo");
        var upcoming = await db.Bookings.FirstAsync(b => b.StudentId == sara && b.Status == BookingStatus.Confirmed);
        Assert.False(await store.RateAsync(sara, upcoming.Id, 5, "early"));

        var done = await db.Bookings.FirstAsync(b => b.StudentId == sara && b.Status == BookingStatus.Completed);
        done.Rating = null;
        await db.SaveChangesAsync();
        Assert.True(await store.RateAsync(sara, done.Id, 4, "Good"));
        Assert.False(await store.RateAsync(sara, done.Id, 5, "again"));
    }
}

public class SchemaVersionTests
{
    [Fact]
    public async Task An_old_database_is_recreated_in_development_and_refused_otherwise()
    {
        var path = Path.Combine(Path.GetTempPath(), $"eduverse-old-{Guid.NewGuid():N}.db");
        using (var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={path}"))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "CREATE TABLE Old (Id INTEGER); PRAGMA user_version = 1;";
            command.ExecuteNonQuery();
        }

        var configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:EduVerse"] = $"Data Source={path}" }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(TimeProvider.System);
        services.AddEduVerse(configuration);
        services.AddEduVerseData(configuration);
        await using var provider = services.BuildServiceProvider();
        try
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => provider.InitializeDatabaseAsync(seedDemo: false, allowReset: false));
            await provider.InitializeDatabaseAsync(seedDemo: false, allowReset: true);
            using var scope = provider.CreateScope();
            Assert.Equal(0, await scope.ServiceProvider.GetRequiredService<EduVerseDb>().Bookings.CountAsync());
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            File.Delete(path);
        }
    }
}
