using EduVerse.Core.Common;
using EduVerse.Core.Gamification;

namespace EduVerse.Tests;

public class GamificationTests
{
    [Fact]
    public void Xp_rewards_right_answers_difficulty_and_runs()
    {
        Assert.Equal(Xp.Wrong, Xp.ForAnswer(false, 3, 0));
        Assert.Equal(10, Xp.ForAnswer(true, 1, 1));
        Assert.Equal(20, Xp.ForAnswer(true, 3, 1));
        Assert.Equal(25, Xp.ForAnswer(true, 3, 9)); // run bonus capped at 5
    }

    [Fact]
    public void Levels_need_more_xp_each_time()
    {
        Assert.Equal(1, Xp.LevelOf(0));
        Assert.Equal(1, Xp.LevelOf(99));
        Assert.Equal(2, Xp.LevelOf(100));
        Assert.Equal(3, Xp.LevelOf(300));
        Assert.Equal((4, 50, 400), Xp.Progress(650));
        Assert.Equal("Explorer", Xp.Rank(1));
        Assert.Equal("Scholar", Xp.Rank(5));
        Assert.Equal("Sage", Xp.Rank(20));
    }

    [Fact]
    public void Streak_counts_days_in_a_row_and_survives_until_midnight()
    {
        var today = new DateOnly(2026, 11, 15);
        DateOnly D(int ago) => today.AddDays(-ago);
        Assert.Equal(3, Streaks.Current([D(0), D(1), D(2), D(4)], today));
        Assert.Equal(2, Streaks.Current([D(1), D(2), D(5)], today)); // not played yet today
        Assert.Equal(0, Streaks.Current([D(2), D(3)], today));
        Assert.Equal(4, Streaks.Best([D(10), D(9), D(8), D(7), D(1), D(0)]));
    }

    [Fact]
    public void Daily_quests_are_stable_and_different_from_day_to_day()
    {
        var day = new DateOnly(2026, 11, 15);
        var a = Quests.ForDay("user-1", day);
        Assert.Equal(a, Quests.ForDay("user-1", day));
        Assert.Equal(3, a.Count);
        Assert.Equal(3, a.Select(q => q.Kind).Distinct().Count());
        Assert.Equal(QuestKind.Answer, a[0].Kind);
        var week = Enumerable.Range(0, 14).Select(i => string.Join(",", Quests.ForDay("user-1", day.AddDays(i)).Select(q => q.Kind))).Distinct().Count();
        Assert.True(week > 3);

        var stats = new DayStats(12, 9, 6, 1, 0, 2, 0, 0, 0);
        Assert.True(Quests.Done(new Quest(QuestKind.Answer, 10, 20), stats));
        Assert.Equal(2, Quests.Progress(new Quest(QuestKind.ThreeSubjects, 3, 40), stats));
        Assert.True(Quests.Done(new Quest(QuestKind.CorrectInARow, 5, 30), stats));
    }

    [Fact]
    public void Badges_are_earned_from_what_the_student_did()
    {
        var stats = new BadgeStats(Correct: 120, BestStreak: 7, Tests: 4, PerfectTests: 1, Areas: 3, Placements: 0, DuelsWon: 0, Mastered: 2, Xp: 900, AdvancedLevel: false);
        var earned = Badges.Earned(stats).Select(b => b.Key).ToList();
        Assert.Contains("Hundred", earned);
        Assert.Contains("Streak7", earned);
        Assert.Contains("Curious", earned);
        Assert.DoesNotContain("Thousand", earned);
        Assert.DoesNotContain("Scholar", earned); // level 5 needs 1000 XP
        Assert.Equal(120, Badges.All.Single(b => b.Key == "Thousand").Progress(stats));
        Assert.Equal(Badges.All.Count, Badges.All.Select(b => b.Key).Distinct().Count());
    }

    [Fact]
    public void Nicknames_never_use_the_real_name_and_are_stable()
    {
        var (en, emoji) = Nicknames.Generate(42, TeachLanguage.English);
        Assert.Equal(en, Nicknames.Generate(42, TeachLanguage.English).Name);
        Assert.Equal(2, en.Split(' ').Length);
        Assert.False(string.IsNullOrEmpty(emoji));
        Assert.NotEqual(en, Nicknames.Generate(42, TeachLanguage.Spanish).Name);
    }

    [Fact]
    public void Teams_score_by_average_and_new_members_balance_them()
    {
        Assert.Equal(0, Teams.Score([]));
        Assert.Equal(150, Teams.Score([100, 200]));
        Assert.Equal(Team.Dolphins, Teams.Assign(new Dictionary<Team, int> { [Team.Owls] = 3, [Team.Foxes] = 2, [Team.Bears] = 1 }));
    }

    [Fact]
    public void Leaderboard_shares_positions_on_ties()
    {
        var ranked = Leaderboard.Rank([("a", 50), ("b", 80), ("c", 50), ("d", 10)], x => x.Item2);
        Assert.Equal(["b", "a", "c", "d"], ranked.Select(r => r.Item.Item1));
        Assert.Equal([1, 2, 2, 4], ranked.Select(r => r.Position));
    }

    [Fact]
    public void Duels_have_a_winner_or_a_draw_and_expire()
    {
        Assert.Equal(1, Duels.Outcome(4, 3));
        Assert.Equal(-1, Duels.Outcome(2, 5));
        Assert.Equal(0, Duels.Outcome(3, 3));
        var t = new DateTimeOffset(2026, 11, 1, 10, 0, 0, TimeSpan.Zero);
        Assert.False(Duels.Expired(t, t.AddDays(6)));
        Assert.True(Duels.Expired(t, t.AddDays(8)));
    }
}
