using EduVerse.Core.Catalog;
using EduVerse.Core.Gamification;
using EduVerse.Infrastructure.Data;
using EduVerse.Infrastructure.Gamification;
using EduVerse.Infrastructure.Marketplace;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EduVerse.Tests;

public class LeagueRulesTests
{
    [Theory]
    [InlineData(1, 0)]
    [InlineData(4, 1)]
    [InlineData(12, 3)]
    [InlineData(20, 5)]
    [InlineData(40, 5)]
    public void The_promotion_and_demotion_zones_are_a_quarter_of_the_group_at_most_five(int size, int zone) =>
        Assert.Equal(zone, Leagues.ZoneSize(size));

    [Fact]
    public void The_top_goes_up_the_bottom_goes_down_and_the_ends_are_safe()
    {
        Assert.Equal(LeagueResult.Promoted, Leagues.ResultOf(1, 20, 300, Division.Gold));
        Assert.Equal(LeagueResult.Promoted, Leagues.ResultOf(5, 20, 10, Division.Gold));
        Assert.Equal(LeagueResult.Stayed, Leagues.ResultOf(6, 20, 300, Division.Gold));
        Assert.Equal(LeagueResult.Demoted, Leagues.ResultOf(16, 20, 50, Division.Gold));
        Assert.Equal(LeagueResult.Stayed, Leagues.ResultOf(20, 20, 0, Division.Bronze)); // nothing below Bronze
        Assert.Equal(LeagueResult.Stayed, Leagues.ResultOf(1, 20, 900, Division.Diamond)); // nothing above Diamond
        Assert.Equal(LeagueResult.Stayed, Leagues.ResultOf(1, 20, 0, Division.Silver)); // no XP, no promotion
        Assert.Equal(LeagueResult.Promoted, Leagues.ResultOf(1, 2, 40, Division.Silver));
        Assert.Equal(LeagueResult.Demoted, Leagues.ResultOf(2, 2, 10, Division.Silver));
        Assert.Equal(LeagueResult.Stayed, Leagues.ResultOf(1, 1, 40, Division.Silver)); // alone: nobody to beat
        Assert.Equal(Division.Silver, Leagues.Next(Division.Bronze, LeagueResult.Promoted));
        Assert.Equal(Division.Bronze, Leagues.Next(Division.Bronze, LeagueResult.Demoted));
        Assert.Equal(Division.Diamond, Leagues.Next(Division.Diamond, LeagueResult.Promoted));
    }
}

public class LeagueStoreTests(Database database) : IClassFixture<Database>
{
    private static (GamificationStore Game, LeagueStore Leagues) At(IServiceProvider services, DateTimeOffset now)
    {
        var game = new GamificationStore(services.GetRequiredService<EduVerseDb>(), new FixedClock(now), services.GetRequiredService<IOptions<MarketplaceOptions>>());
        return (game, new LeagueStore(services.GetRequiredService<EduVerseDb>(), game));
    }

    [Fact]
    public async Task Last_weeks_place_moves_the_student_to_another_division_when_they_come_back()
    {
        using var scope = database.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EduVerseDb>();
        // A fixed week far from the demo data, so these students are alone with each other
        var lastWeek = new DateTimeOffset(2030, 3, 6, 12, 0, 0, TimeSpan.Zero); // a Wednesday
        var thisWeek = lastWeek.AddDays(7);
        var (gameThen, leaguesThen) = At(scope.ServiceProvider, lastWeek);
        var students = new List<string>();
        for (var i = 0; i < 4; i++)
        {
            var id = await QuizDriver.NewStudentAsync(db, TimeProvider.System);
            var profile = await gameThen.ProfileAsync(id);
            profile.Level = Level.Secondary1;
            await db.SaveChangesAsync();
            students.Add(id);
            Assert.NotNull(await leaguesThen.JoinAsync(id));
        }

        // The four are grouped together and ranked on the XP of that week: 40, 30, 20, 0
        var groups = await db.LeagueMembers.Where(m => students.Contains(m.UserId)).Select(m => m.Group).Distinct().ToListAsync();
        Assert.Single(groups);
        for (var i = 0; i < 3; i++)
        {
            db.XpEvents.Add(new XpEvent { UserId = students[i], Amount = 40 - 10 * i, Reason = "answer", Correct = true, At = lastWeek });
        }

        db.XpEvents.Add(new XpEvent { UserId = students[0], Amount = 999, Reason = "answer", Correct = true, At = thisWeek }); // next week's XP doesn't count
        await db.SaveChangesAsync();
        var table = await leaguesThen.ViewAsync(students[1]);
        Assert.Equal(new[] { 40, 30, 20, 0 }, table!.Rows.Select(r => r.Xp));
        Assert.Equal(2, table.Me!.Position);
        Assert.Equal(LeagueResult.Promoted, table.Rows[0].Zone);

        var (_, leaguesNow) = At(scope.ServiceProvider, thisWeek);
        var winner = await leaguesNow.ViewAsync(students[0]);
        Assert.Equal((Division.Silver, LeagueResult.Promoted, 1), (winner!.Division, winner.LastResult, winner.LastPosition));
        Assert.True(winner.ShowResult);
        await leaguesNow.MarkResultSeenAsync(students[0]);
        Assert.False((await leaguesNow.ViewAsync(students[0]))!.ShowResult);

        var middle = await leaguesNow.ViewAsync(students[1]);
        Assert.Equal((Division.Bronze, LeagueResult.Stayed), (middle!.Division, middle.LastResult));
        Assert.NotEqual(winner.Rows.Select(r => r.UserId), middle.Rows.Select(r => r.UserId)); // different divisions, different groups
    }

    [Fact]
    public async Task Young_children_and_students_who_dont_compete_stay_out_of_the_leagues()
    {
        using var scope = database.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EduVerseDb>();
        var leagues = scope.ServiceProvider.GetRequiredService<LeagueStore>();
        var game = scope.ServiceProvider.GetRequiredService<GamificationStore>();
        var mia = await database.UserIdAsync("mia@eduverse.demo");
        Assert.Null(await leagues.ViewAsync(mia));

        var shy = await QuizDriver.NewStudentAsync(db, TimeProvider.System);
        await game.SaveSettingsAsync(shy, Core.Gamification.Team.Owls, compete: false, newNickname: false);
        Assert.Null(await leagues.JoinAsync(shy));

        var sara = await database.UserIdAsync("sara@eduverse.demo");
        var view = await leagues.ViewAsync(sara);
        Assert.NotNull(view);
        Assert.True(view.Rows.Count >= 3); // the demo classmates who compete share Sara's group
        Assert.DoesNotContain(view.Rows, r => r.UserId == mia);
        Assert.Equal(await leagues.ViewAsync(sara) is { } again ? again.Rows.Count : -1, view.Rows.Count); // joining twice changes nothing
    }

    [Fact]
    public async Task A_full_group_opens_a_new_one()
    {
        using var scope = database.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EduVerseDb>();
        var (game, leagues) = At(scope.ServiceProvider, new DateTimeOffset(2031, 5, 7, 12, 0, 0, TimeSpan.Zero));
        var groups = new List<int>();
        for (var i = 0; i < Leagues.GroupSize + 1; i++)
        {
            var id = await QuizDriver.NewStudentAsync(db, TimeProvider.System);
            (await game.ProfileAsync(id)).Level = Level.HighSchool;
            await db.SaveChangesAsync();
            groups.Add((await leagues.JoinAsync(id))!.Group);
        }

        Assert.Equal(Leagues.GroupSize, groups.Count(g => g == groups[0]));
        Assert.NotEqual(groups[0], groups[^1]);
    }
}
