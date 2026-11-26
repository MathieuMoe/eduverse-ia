using EduVerse.Core.Common;

namespace EduVerse.Core.Gamification;

/// <summary>Experience points: effort counts a little, right answers more, harder questions and runs of right answers more.</summary>
public static class Xp
{
    public const int Wrong = 1;
    public const int TestFinished = 20;
    public const int PerfectTest = 30;
    public const int PlacementFinished = 25;
    public const int DuelWin = 40;
    public const int DuelDraw = 20;
    public const int Badge = 25;
    public const int AdventureWon = 50;

    public const int SureBonus = 3;

    /// <summary>
    /// Each hint takes away a quarter of the points of the question (and the run bonus); a right answer given as "sure" earns a small bonus.
    /// </summary>
    public static int ForAnswer(bool correct, int difficulty, int runOfCorrect, int hints = 0, bool sure = false)
    {
        if (!correct)
        {
            return Wrong;
        }

        hints = Math.Clamp(hints, 0, 3);
        var points = (5 + 5 * Math.Clamp(difficulty, 1, 3)) * (4 - hints) / 4;
        var run = hints == 0 ? Math.Min(Math.Max(runOfCorrect - 1, 0), 5) : 0;
        return Math.Max(Wrong + 1, points + run + (sure ? SureBonus : 0));
    }

    /// <summary>Total XP needed to reach a level: 0, 100, 300, 600, 1000… (50 · n · (n − 1)).</summary>
    public static int NeededFor(int level) => 50 * level * (level - 1);

    public static int LevelOf(int xp)
    {
        var level = 1;
        while (NeededFor(level + 1) <= xp)
        {
            level++;
        }

        return level;
    }

    /// <summary>Level, XP inside the level and XP of the whole level (for the progress ring).</summary>
    public static (int Level, int Into, int Size) Progress(int xp)
    {
        var level = LevelOf(Math.Max(0, xp));
        return (level, xp - NeededFor(level), NeededFor(level + 1) - NeededFor(level));
    }

    /// <summary>Title shown with the level.</summary>
    public static string Rank(int level) => level switch
    {
        <= 2 => "Explorer",
        <= 4 => "Apprentice",
        <= 7 => "Scholar",
        <= 10 => "Expert",
        <= 14 => "Master",
        _ => "Sage",
    };
}

/// <summary>
/// The daily XP goal the student picks (Duolingo-style "casual / regular / serious / intense"). Every XP of the day counts
/// (answers, finished tests, quests, badges), so the goal is reachable in many ways.
/// </summary>
public static class DailyGoals
{
    public const int Default = 20;

    public static readonly IReadOnlyList<int> Options = [10, 20, 30, 50];

    /// <summary>A value outside <see cref="Options"/> (old data, a tampered form) falls back to <see cref="Default"/>.</summary>
    public static int Normalize(int goal) => Options.Contains(goal) ? goal : Default;

    /// <summary>0–100, for the progress ring.</summary>
    public static int Percent(int todayXp, int goal) => Math.Clamp(100 * Math.Max(0, todayXp) / Math.Max(1, goal), 0, 100);

    public static bool Reached(int todayXp, int goal) => todayXp >= goal;

    /// <summary>True only for the lesson that crossed the line (to celebrate it once).</summary>
    public static bool JustReached(int xpBefore, int xpAfter, int goal) => xpBefore < goal && xpAfter >= goal;
}

/// <summary>
/// "Happy hour": a daily half hour, chosen by the student, in which every answer gives double XP. Off by default:
/// the student opts in, so the XP of an answer stays predictable unless they asked for the bonus.
/// </summary>
public static class HappyHour
{
    public const int Off = -1;

    public const int Minutes = 30;

    public const int Multiplier = 2;

    /// <summary>The hours offered in the picker (local time).</summary>
    public static readonly IReadOnlyList<int> Hours = [7, 13, 16, 17, 18, 19, 20, 21];

    public static int Normalize(int hour) => Hours.Contains(hour) ? hour : Off;

    public static bool IsActive(int hour, TimeOnly localTime) =>
        hour is >= 0 and <= 23 && localTime >= new TimeOnly(hour, 0) && localTime < new TimeOnly(hour, 0).AddMinutes(Minutes);

    /// <summary>Whole minutes left in the window (0 when it is not active).</summary>
    public static int MinutesLeft(int hour, TimeOnly localTime) =>
        IsActive(hour, localTime) ? (int)Math.Ceiling((new TimeOnly(hour, 0).AddMinutes(Minutes) - localTime).TotalMinutes) : 0;

    public static int Apply(int xp, bool active) => active ? xp * Multiplier : xp;
}

/// <summary>What the end-of-lesson screens show: computed from the attempt and the XP log, never guessed by the browser.</summary>
public sealed record LessonEnd(
    int Xp, int Correct, int Length, int Seconds, int Streak, bool StreakExtended, int Goal, int TodayXp, bool GoalReached, bool GoalJustReached, bool HappyHour)
{
    public int Accuracy => Length == 0 ? 0 : (int)Math.Round(100.0 * Correct / Length);

    public int GoalPercent => DailyGoals.Percent(TodayXp, Goal);

    /// <summary>Gems won during the lesson (quests, badges, daily goal).</summary>
    public int Gems { get; init; }
}

/// <summary>Gems: the currency of the shop, earned by keeping promises (quests, badges, the daily goal), never bought.</summary>
public static class Gems
{
    public const int Quest = 5;

    public const int Badge = 10;

    public const int DailyGoal = 10;
}

public enum ShopItem
{
    StreakFreeze,
    DoubleXp,
    Potion,
    FrameGold,
    FrameRainbow,
    FrameFire,
}

public enum BuyOutcome
{
    Bought,
    NotEnoughGems,
    Limit,
    AlreadyOwned,
    NotNow,
}

/// <summary>What the shop sells and the rules of each item (prices in gems).</summary>
public static class Shop
{
    /// <summary>At most two streak freezes waiting at once, as in Duolingo.</summary>
    public const int MaxFreezes = 2;

    public const int DoubleXpMinutes = 15;

    public static readonly IReadOnlyList<ShopItem> Items = Enum.GetValues<ShopItem>();

    public static int Price(ShopItem item) => item switch
    {
        ShopItem.StreakFreeze => 50,
        ShopItem.DoubleXp => 40,
        ShopItem.Potion => 30,
        ShopItem.FrameGold => 120,
        ShopItem.FrameRainbow => 200,
        _ => 300,
    };

    public static string Icon(ShopItem item) => item switch
    {
        ShopItem.StreakFreeze => "🧊",
        ShopItem.DoubleXp => "⚡",
        ShopItem.Potion => "🧪",
        ShopItem.FrameGold => "🥇",
        ShopItem.FrameRainbow => "🌈",
        _ => "🔥",
    };

    public static bool IsFrame(ShopItem item) => item is ShopItem.FrameGold or ShopItem.FrameRainbow or ShopItem.FrameFire;

    /// <summary>The CSS name of a frame ("gold", "rainbow", "fire"), stored on the profile when equipped.</summary>
    public static string FrameKey(ShopItem item) => item switch
    {
        ShopItem.FrameGold => "gold",
        ShopItem.FrameRainbow => "rainbow",
        ShopItem.FrameFire => "fire",
        _ => "",
    };

    public static bool IsFrameKey(string? key) => key is "" or "gold" or "rainbow" or "fire";

    /// <summary>Double XP stays on for <see cref="DoubleXpMinutes"/> after the last purchase.</summary>
    public static bool DoubleXpActive(DateTimeOffset? lastBought, DateTimeOffset now) =>
        lastBought is { } at && now >= at && now < at.AddMinutes(DoubleXpMinutes);

    public static int DoubleXpMinutesLeft(DateTimeOffset? lastBought, DateTimeOffset now) =>
        DoubleXpActive(lastBought, now) ? (int)Math.Ceiling((lastBought!.Value.AddMinutes(DoubleXpMinutes) - now).TotalMinutes) : 0;
}

/// <summary>Days in a row with at least one answer. The streak is still alive today until midnight if yesterday counted.</summary>
public static class Streaks
{
    public static int Current(IEnumerable<DateOnly> activeDays, DateOnly today) => Current(activeDays, today, []);

    /// <summary>
    /// Days covered by a streak freeze keep the streak alive without adding to it (Duolingo-style): the count walks back
    /// through active and frozen days, and only active days count.
    /// </summary>
    public static int Current(IEnumerable<DateOnly> activeDays, DateOnly today, IEnumerable<DateOnly> frozenDays)
    {
        var days = activeDays.ToHashSet();
        var frozen = frozenDays.ToHashSet();
        var day = days.Contains(today) || frozen.Contains(today) ? today : today.AddDays(-1);
        var count = 0;
        while (days.Contains(day) || frozen.Contains(day))
        {
            count += days.Contains(day) ? 1 : 0;
            day = day.AddDays(-1);
        }

        return count;
    }

    /// <summary>
    /// The missed days (between the last active or frozen day and today) that the student's freezes would cover:
    /// all of them if there are enough freezes, none otherwise (a freeze can't half-save a streak), and none when
    /// there was no streak to save.
    /// </summary>
    public static IReadOnlyList<DateOnly> DaysToFreeze(IEnumerable<DateOnly> activeDays, IEnumerable<DateOnly> frozenDays, DateOnly today, int freezesOwned)
    {
        var covered = activeDays.Concat(frozenDays).Where(d => d < today).ToHashSet();
        if (covered.Count == 0 || freezesOwned <= 0)
        {
            return [];
        }

        var last = covered.Max();
        var missed = new List<DateOnly>();
        for (var d = last.AddDays(1); d < today; d = d.AddDays(1))
        {
            missed.Add(d);
        }

        return missed.Count <= freezesOwned ? missed : [];
    }

    public static int Best(IEnumerable<DateOnly> activeDays)
    {
        var best = 0;
        var run = 0;
        DateOnly? previous = null;
        foreach (var day in activeDays.Distinct().Order())
        {
            run = previous is { } p && p.AddDays(1) == day ? run + 1 : 1;
            best = Math.Max(best, run);
            previous = day;
        }

        return best;
    }
}

/// <summary>What a student did on one day, for the daily quests.</summary>
public sealed record DayStats(int Answers, int Correct, int BestRun, int Tests, int PerfectTests, int Areas, int Duels, int Placements, int TutorAnswers, int Reviews = 0, int EvoChecks = 0, int Teach = 0);

public enum QuestKind
{
    Answer,
    CorrectInARow,
    FinishTest,
    PerfectTest,
    ThreeSubjects,
    PlayDuel,
    Placement,
    Tutor,
    Review,
    CheckEvo,
    TeachEvo,
}

public sealed record Quest(QuestKind Kind, int Target, int Reward)
{
    public string Key => Kind.ToString();
}

/// <summary>Three quests per day: always "answer 10 questions", plus two others chosen in a stable way for the student and the day.</summary>
public static class Quests
{
    private static readonly Quest[] Pool =
    [
        new(QuestKind.CorrectInARow, 5, 30), new(QuestKind.FinishTest, 1, 25), new(QuestKind.PerfectTest, 1, 50), new(QuestKind.ThreeSubjects, 3, 40),
        new(QuestKind.PlayDuel, 1, 30), new(QuestKind.Placement, 1, 30), new(QuestKind.Tutor, 5, 30), new(QuestKind.Review, 1, 30), new(QuestKind.CheckEvo, 1, 30), new(QuestKind.TeachEvo, 1, 30),
    ];

    public static IReadOnlyList<Quest> ForDay(string userId, DateOnly day)
    {
        ArgumentNullException.ThrowIfNull(userId);
        var hash = Fnv($"{userId}|{day.DayNumber}");
        var first = (int)(hash % (uint)Pool.Length);
        var second = (first + 1 + (int)(hash / (uint)Pool.Length % (uint)(Pool.Length - 1))) % Pool.Length;
        return [new(QuestKind.Answer, 10, 20), Pool[first], Pool[second]];
    }

    public static int Progress(Quest quest, DayStats stats)
    {
        ArgumentNullException.ThrowIfNull(quest);
        ArgumentNullException.ThrowIfNull(stats);
        var value = quest.Kind switch
        {
            QuestKind.Answer => stats.Answers,
            QuestKind.CorrectInARow => stats.BestRun,
            QuestKind.FinishTest => stats.Tests,
            QuestKind.PerfectTest => stats.PerfectTests,
            QuestKind.ThreeSubjects => stats.Areas,
            QuestKind.PlayDuel => stats.Duels,
            QuestKind.Placement => stats.Placements,
            QuestKind.Review => stats.Reviews,
            QuestKind.CheckEvo => stats.EvoChecks,
            QuestKind.TeachEvo => stats.Teach,
            _ => stats.TutorAnswers,
        };
        return Math.Min(value, quest.Target);
    }

    public static bool Done(Quest quest, DayStats stats) => Progress(quest, stats) >= quest!.Target;

    /// <summary>FNV-1a: stable between runs (string.GetHashCode is not).</summary>
    private static uint Fnv(string text)
    {
        var hash = 2166136261;
        foreach (var c in text)
        {
            hash = (hash ^ c) * 16777619;
        }

        return hash;
    }
}

/// <summary>Everything a student has done, for the badges.</summary>
public sealed record BadgeStats(int Correct, int BestStreak, int Tests, int PerfectTests, int Areas, int Placements, int DuelsWon, int Mastered, int Xp, bool AdvancedLevel, int Reviews = 0, int EvoCaught = 0, int Adventures = 0, int Taught = 0, int Interviews = 0, int Certificates = 0, int Hobbies = 0, int Ideas = 0, int Reflections = 0);

public sealed record Badge(string Key, string Icon, int Goal, Func<BadgeStats, int> Measure)
{
    public int Progress(BadgeStats stats) => Math.Min(Measure(stats), Goal);

    public bool Earned(BadgeStats stats) => Measure(stats) >= Goal;
}

public static class Badges
{
    public static readonly IReadOnlyList<Badge> All =
    [
        new("FirstSteps", "🌱", 1, s => s.Correct),
        new("Hundred", "💯", 100, s => s.Correct),
        new("Thousand", "🏔️", 1000, s => s.Correct),
        new("Streak3", "🔥", 3, s => s.BestStreak),
        new("Streak7", "⚡", 7, s => s.BestStreak),
        new("Streak30", "☄️", 30, s => s.BestStreak),
        new("Perfect", "🎯", 1, s => s.PerfectTests),
        new("Sharpshooter", "🏹", 5, s => s.PerfectTests),
        new("Curious", "🧭", 3, s => s.Areas),
        new("Polymath", "🦉", 7, s => s.Areas),
        new("Explorer", "🗺️", 1, s => s.Placements),
        new("Cartographer", "🧩", 5, s => s.Placements),
        new("Duelist", "⚔️", 1, s => s.DuelsWon),
        new("Champion", "🏆", 10, s => s.DuelsWon),
        new("Mastery", "🧠", 1, s => s.Mastered),
        new("Scholar", "🎓", Xp.NeededFor(5), s => s.Xp),
        new("Researcher", "🔬", 1, s => s.AdvancedLevel ? 1 : 0),
        new("Reviewer", "🔁", 5, s => s.Reviews),
        new("Detective", "🔎", 10, s => s.EvoCaught),
        new("BossSlayer", "🐉", 1, s => s.Adventures),
        new("Hero", "🛡️", 5, s => s.Adventures),
        new("Teacher", "🧑‍🏫", 3, s => s.Taught),
        new("TimeTraveler", "⏳", 2, s => s.Interviews),
        new("Apprentice", "🛠️", 1, s => s.Certificates),
        new("MasterCraftsman", "🏅", 3, s => s.Certificates),
        new("Hobbyist", "🎨", 1, s => s.Hobbies),
        new("IdeaMaker", "💡", 1, s => s.Ideas),
        new("Thinker", "🧠", 5, s => s.Reflections),
    ];

    public static IReadOnlyList<Badge> Earned(BadgeStats stats) => All.Where(b => b.Earned(stats)).ToList();
}

/// <summary>Public name in rankings: an animal and a word of the sky, never the real name.</summary>
public static class Nicknames
{
    private static readonly (string Emoji, string En, string Es, string Fr, string Ca)[] Animals =
    [
        ("🦊", "Fox", "Zorro", "Renard", "Guineu"), ("🦉", "Owl", "Búho", "Hibou", "Mussol"), ("🐬", "Dolphin", "Delfín", "Dauphin", "Dofí"),
        ("🐼", "Panda", "Panda", "Panda", "Panda"), ("🐯", "Tiger", "Tigre", "Tigre", "Tigre"), ("🐨", "Koala", "Koala", "Koala", "Koala"),
        ("🦅", "Falcon", "Halcón", "Faucon", "Falcó"), ("🦦", "Otter", "Nutria", "Loutre", "Llúdriga"), ("🐢", "Turtle", "Tortuga", "Tortue", "Tortuga"),
        ("🐙", "Octopus", "Pulpo", "Poulpe", "Pop"), ("🦁", "Lion", "León", "Lion", "Lleó"), ("🐧", "Penguin", "Pingüino", "Manchot", "Pingüí"),
    ];

    private static readonly (string En, string Es, string Fr, string Ca)[] Words =
    [
        ("Comet", "Cometa", "Comète", "Cometa"), ("Star", "Estrella", "Étoile", "Estel"), ("Rocket", "Cohete", "Fusée", "Coet"), ("Galaxy", "Galaxia", "Galaxie", "Galàxia"),
        ("Thunder", "Trueno", "Tonnerre", "Tro"), ("Nova", "Nova", "Nova", "Nova"), ("Quasar", "Quásar", "Quasar", "Quàsar"), ("Orbit", "Órbita", "Orbite", "Òrbita"),
    ];

    public static (string Name, string Emoji) Generate(int seed, TeachLanguage language)
    {
        var rng = new Random(seed);
        return Build(rng.Next(Animals.Length), rng.Next(Words.Length), language);
    }

    /// <summary>How many animal options <see cref="Build"/> and <see cref="AnimalLabel"/> accept (valid indexes: 0..AnimalCount-1).</summary>
    public static int AnimalCount => Animals.Length;

    /// <summary>How many word options <see cref="Build"/> and <see cref="WordLabel"/> accept (valid indexes: 0..WordCount-1).</summary>
    public static int WordCount => Words.Length;

    /// <summary>The emoji for an animal option, for rendering a picker.</summary>
    public static string AnimalEmoji(int animal) => Animals[Clamp(animal, Animals.Length)].Emoji;

    /// <summary>The localized label for an animal option, for rendering a picker.</summary>
    public static string AnimalLabel(int animal, TeachLanguage language)
    {
        var a = Animals[Clamp(animal, Animals.Length)];
        return language switch
        {
            TeachLanguage.Spanish => a.Es,
            TeachLanguage.French => a.Fr,
            TeachLanguage.Catalan => a.Ca,
            _ => a.En,
        };
    }

    /// <summary>The localized label for a word option, for rendering a picker.</summary>
    public static string WordLabel(int word, TeachLanguage language)
    {
        var w = Words[Clamp(word, Words.Length)];
        return language switch
        {
            TeachLanguage.Spanish => w.Es,
            TeachLanguage.French => w.Fr,
            TeachLanguage.Catalan => w.Ca,
            _ => w.En,
        };
    }

    /// <summary>Builds a nickname and avatar from an explicit animal/word choice (out-of-range indexes are clamped).</summary>
    public static (string Name, string Emoji) Build(int animal, int word, TeachLanguage language)
    {
        var a = Animals[Clamp(animal, Animals.Length)];
        var w = Words[Clamp(word, Words.Length)];
        var name = language switch
        {
            TeachLanguage.Spanish => $"{a.Es} {w.Es}",
            TeachLanguage.French => $"{a.Fr} {w.Fr}",
            TeachLanguage.Catalan => $"{a.Ca} {w.Ca}",
            _ => $"{w.En} {a.En}",
        };
        return (name, a.Emoji);
    }

    /// <summary>Which animal option matches an existing avatar emoji, or -1 if none does (e.g. legacy data).</summary>
    public static int AnimalIndexOf(string avatar)
    {
        for (var i = 0; i < Animals.Length; i++)
        {
            if (Animals[i].Emoji == avatar) return i;
        }
        return -1;
    }

    /// <summary>Which word option appears in an existing nickname for the given language, or -1 if none does.</summary>
    public static int WordIndexOf(string nickname, TeachLanguage language)
    {
        for (var i = 0; i < Words.Length; i++)
        {
            if (nickname.Contains(WordLabel(i, language), StringComparison.OrdinalIgnoreCase)) return i;
        }
        return -1;
    }

    private static int Clamp(int index, int length) => index < 0 || index >= length ? 0 : index;
}

public enum Team
{
    Owls,
    Foxes,
    Dolphins,
    Bears,
}

public static class Teams
{
    public static string Emoji(Team team) => team switch
    {
        Team.Owls => "🦉",
        Team.Foxes => "🦊",
        Team.Dolphins => "🐬",
        _ => "🐻",
    };

    /// <summary>Team score: average weekly XP of its members, so small teams can win too.</summary>
    public static int Score(IReadOnlyCollection<int> memberXp)
    {
        ArgumentNullException.ThrowIfNull(memberXp);
        return memberXp.Count == 0 ? 0 : (int)Math.Round(memberXp.Average());
    }

    /// <summary>A new member goes to the smallest team (first in order on a tie).</summary>
    public static Team Assign(IReadOnlyDictionary<Team, int> members)
    {
        ArgumentNullException.ThrowIfNull(members);
        return Enum.GetValues<Team>().OrderBy(t => members.TryGetValue(t, out var n) ? n : 0).First();
    }
}

public enum Division
{
    Bronze,
    Silver,
    Gold,
    Sapphire,
    Diamond,
}

public enum LeagueResult
{
    Stayed,
    Promoted,
    Demoted,
}

/// <summary>
/// Weekly leagues (Duolingo-style): groups of up to <see cref="GroupSize"/> students of the same division compete on the
/// XP of the week; the top of the group goes up a division, the bottom goes down. Only nicknames are ever shown.
/// </summary>
public static class Leagues
{
    public const int GroupSize = 20;

    /// <summary>A quarter of the group, at most 5, at least 1 when there is someone to compete with.</summary>
    public static int ZoneSize(int groupSize) => groupSize < 2 ? 0 : Math.Clamp(groupSize / 4, 1, 5);

    /// <summary>
    /// Where a position (1-based, ties share a position) ends the week. Promotion needs XP: nobody goes up by doing
    /// nothing. Demotion only touches the bottom zone, never someone already promoted, never below Bronze.
    /// </summary>
    public static LeagueResult ResultOf(int position, int groupSize, int weekXp, Division division)
    {
        var zone = ZoneSize(groupSize);
        if (weekXp > 0 && position <= zone && division < Division.Diamond)
        {
            return LeagueResult.Promoted;
        }

        return position > groupSize - zone && position > zone && division > Division.Bronze ? LeagueResult.Demoted : LeagueResult.Stayed;
    }

    public static Division Next(Division division, LeagueResult result) => result switch
    {
        LeagueResult.Promoted => (Division)Math.Min((int)division + 1, (int)Division.Diamond),
        LeagueResult.Demoted => (Division)Math.Max((int)division - 1, (int)Division.Bronze),
        _ => division,
    };

    public static string Emoji(Division division) => division switch
    {
        Division.Bronze => "🥉",
        Division.Silver => "🥈",
        Division.Gold => "🥇",
        Division.Sapphire => "💠",
        _ => "💎",
    };

    /// <summary>The zone a position is in right now, for the colours of the table.</summary>
    public static LeagueResult ZoneOf(int position, int groupSize, int weekXp, Division division) => ResultOf(position, groupSize, weekXp, division);
}

/// <summary>
/// Friend quests (phase 7): two classmates team up for the week and add their XP towards a shared target; when they
/// reach it, each one claims the same gems. Cooperation, not competition: nobody can lose a friend quest.
/// </summary>
public static class FriendQuests
{
    public const int Target = 300;

    public const int Gems = 25;

    public static int Percent(int xpA, int xpB) => Math.Clamp(100 * (Math.Max(0, xpA) + Math.Max(0, xpB)) / Target, 0, 100);

    public static bool Reached(int xpA, int xpB) => xpA + xpB >= Target;
}

/// <summary>Ranking with ties: equal scores share the same position (1, 2, 2, 4).</summary>
public static class Leaderboard
{
    public static IReadOnlyList<(T Item, int Position)> Rank<T>(IEnumerable<T> items, Func<T, int> score)
    {
        var ordered = items.OrderByDescending(score).ToList();
        var result = new List<(T, int)>();
        for (var i = 0; i < ordered.Count; i++)
        {
            var position = i > 0 && score(ordered[i]) == score(ordered[i - 1]) ? result[i - 1].Item2 : i + 1;
            result.Add((ordered[i], position));
        }

        return result;
    }
}

public enum DuelStatus
{
    Waiting,
    Finished,
    Expired,
}

/// <summary>A duel: two classmates, the same 5 questions. The higher score wins; equal scores are a draw.</summary>
public static class Duels
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(7);
    public const int MaxOpen = 3;

    /// <summary>1 if the challenger wins, −1 if the opponent wins, 0 for a draw.</summary>
    public static int Outcome(int challengerScore, int opponentScore) => Math.Sign(challengerScore - opponentScore);

    public static bool Expired(DateTimeOffset createdAt, DateTimeOffset now) => now - createdAt > Lifetime;
}
