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

/// <summary>Days in a row with at least one answer. The streak is still alive today until midnight if yesterday counted.</summary>
public static class Streaks
{
    public static int Current(IEnumerable<DateOnly> activeDays, DateOnly today)
    {
        var days = activeDays.ToHashSet();
        var day = days.Contains(today) ? today : today.AddDays(-1);
        var count = 0;
        while (days.Contains(day))
        {
            count++;
            day = day.AddDays(-1);
        }

        return count;
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
public sealed record BadgeStats(int Correct, int BestStreak, int Tests, int PerfectTests, int Areas, int Placements, int DuelsWon, int Mastered, int Xp, bool AdvancedLevel, int Reviews = 0, int EvoCaught = 0, int Adventures = 0, int Taught = 0, int Interviews = 0);

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
        var a = Animals[rng.Next(Animals.Length)];
        var w = Words[rng.Next(Words.Length)];
        var name = language switch
        {
            TeachLanguage.Spanish => $"{a.Es} {w.Es}",
            TeachLanguage.French => $"{a.Fr} {w.Fr}",
            TeachLanguage.Catalan => $"{a.Ca} {w.Ca}",
            _ => $"{w.En} {a.En}",
        };
        return (name, a.Emoji);
    }
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
