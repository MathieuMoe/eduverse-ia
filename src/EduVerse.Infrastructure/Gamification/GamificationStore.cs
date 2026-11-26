using EduVerse.Core.Catalog;
using EduVerse.Core.Common;
using EduVerse.Core.Curriculum;
using EduVerse.Core.Gamification;
using EduVerse.Core.Learning;
using EduVerse.Infrastructure.Data;
using EduVerse.Infrastructure.Marketplace;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EduVerse.Infrastructure.Gamification;

/// <summary>What the student just won: XP, new badges and finished quests (shown as a toast).</summary>
public sealed record Reward(int Xp, IReadOnlyList<string> Badges, IReadOnlyList<string> Quests)
{
    public static readonly Reward None = new(0, [], []);

    /// <summary>The answer XP was doubled (happy hour or a double-XP boost from the shop).</summary>
    public bool Boosted { get; init; }

    /// <summary>Gems won with this reward (quests, badges, daily goal, streak milestone).</summary>
    public int Gems { get; init; }

    /// <summary>The streak milestone (7, 30, 100… days) this answer reached, or 0.</summary>
    public int Milestone { get; init; }

    public Reward Plus(Reward other) => new(Xp + other.Xp, [.. Badges, .. other.Badges], [.. Quests, .. other.Quests])
    {
        Boosted = Boosted || other.Boosted, Gems = Gems + other.Gems, Milestone = Math.Max(Milestone, other.Milestone),
    };
}

/// <summary>The shop as one student sees it: balance, what they own and what is running.</summary>
public sealed record ShopView(int Gems, int Freezes, int DoubleXpMinutesLeft, IReadOnlySet<string> Frames, string Frame);

/// <summary>The student's daily goal and happy hour, as the dashboard and the test header show them.</summary>
public sealed record DailyGoalView(int Goal, int TodayXp, int Percent, bool Reached, int HappyHour, bool HappyHourActive, int HappyHourMinutesLeft, bool Sounds);

public sealed record QuestView(Quest Quest, int Progress, bool Done);

public sealed record BadgeView(Badge Badge, int Progress, DateTimeOffset? EarnedAt);

public sealed record RankEntry(string UserId, string Nickname, string Avatar, Team Team, int Xp, int Position, bool Me)
{
    /// <summary>The avatar frame the student bought and equipped ("" for none).</summary>
    public string Frame { get; init; } = "";
}

public sealed record TeamEntry(Team Team, int Score, int Members, int Position);

public sealed record ClassBoard(int ClassroomId, string Name, IReadOnlyList<RankEntry> Students, IReadOnlyList<TeamEntry> Teams, int ChallengeDone, int ChallengeTarget);

public sealed record GameSummary(
    StudentProfile Profile, int Xp, int Level, int Into, int Size, string Rank, int Streak, int BestStreak, int WeekXp, bool PlayedToday,
    IReadOnlyList<QuestView> Quests, IReadOnlyList<BadgeView> Badges, IReadOnlyList<(DateOnly Day, int Answers)> Calendar, DateOnly Today)
{
    /// <summary>Answers given as "sure", and how many were right (calibration of confidence).</summary>
    public int Sure { get; init; }

    public int SureRight { get; init; }

    public int Unsure { get; init; }

    public int UnsureRight { get; init; }

    /// <summary>Gem balance (shop currency).</summary>
    public int Gems { get; init; }

    /// <summary>The avatar frame equipped ("" for none).</summary>
    public string Frame => Profile.Frame;
}

public sealed record DuelView(Duel Duel, string Rival, string RivalAvatar, bool IChallenged, int? MyScore, int? RivalScore, bool CanPlay);

/// <summary>
/// Experience, levels, streaks, daily quests, badges, class rankings, teams and duels.
/// Every rule lives in Core (<see cref="Xp"/>, <see cref="Quests"/>, <see cref="Badges"/>…); this store only counts what happened.
/// </summary>
public sealed class GamificationStore(EduVerseDb db, TimeProvider clock, IOptions<MarketplaceOptions> options)
{
    /// <summary>Correct answers a class aims for each week, per student.</summary>
    public const int WeeklyGoalPerStudent = 40;

    public DateTimeOffset Now => clock.GetUtcNow();

    public TimeZoneInfo Zone { get; } = TimeZoneInfo.FindSystemTimeZoneById(options?.Value.TimeZone ?? "Europe/Madrid");

    public DateOnly DayOf(DateTimeOffset at) => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(at, Zone).DateTime);

    public DateOnly Today => DayOf(Now);

    public DateTimeOffset StartOf(DateOnly day)
    {
        var local = day.ToDateTime(TimeOnly.MinValue);
        return new DateTimeOffset(local, Zone.GetUtcOffset(local));
    }

    public DateTimeOffset WeekStart
    {
        get
        {
            var today = Today;
            return StartOf(today.AddDays(-(((int)today.DayOfWeek + 6) % 7)));
        }
    }

    /// <summary>The profile with a nickname and a team (created the first time).</summary>
    public async Task<StudentProfile> ProfileAsync(string userId, CancellationToken ct = default)
    {
        var profile = await db.StudentProfiles.FirstOrDefaultAsync(p => p.UserId == userId, ct).ConfigureAwait(false);
        if (profile is null)
        {
            profile = new StudentProfile { UserId = userId };
            db.StudentProfiles.Add(profile);
        }

        if (profile.Nickname.Length == 0)
        {
            (profile.Nickname, profile.Avatar) = Nicknames.Generate(StableSeed(userId), profile.Language);
            var counts = await db.StudentProfiles.Where(p => p.Nickname != "").GroupBy(p => p.Team)
                .Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(g => g.Key, g => g.Count, ct).ConfigureAwait(false);
            profile.Team = Teams.Assign(counts);
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        return profile;
    }

    public static int StableSeed(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var hash = 17;
        foreach (var c in text)
        {
            hash = unchecked(hash * 31 + c);
        }

        return hash & 0x7fffffff;
    }

    /// <summary>Saves Arena settings. When <paramref name="animal"/> and <paramref name="word"/> are both given, the
    /// student's own pick builds the nickname and avatar (see <see cref="Nicknames.Build"/>); otherwise, if
    /// <paramref name="newNickname"/> is set, a random one is rolled instead; otherwise both are left unchanged.</summary>
    public async Task SaveSettingsAsync(string userId, Team team, bool compete, bool newNickname, int? animal = null, int? word = null, CancellationToken ct = default)
    {
        var profile = await ProfileAsync(userId, ct).ConfigureAwait(false);
        profile.Team = Enum.IsDefined(team) ? team : profile.Team;
        profile.Compete = compete;
        if (animal is >= 0 && word is >= 0)
        {
            (profile.Nickname, profile.Avatar) = Nicknames.Build(animal.Value, word.Value, profile.Language);
        }
        else if (newNickname)
        {
            (profile.Nickname, profile.Avatar) = Nicknames.Generate(StableSeed(userId + Now.Ticks), profile.Language);
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task AwardAsync(string userId, int amount, string reason, bool correct = false, CancellationToken ct = default)
    {
        db.XpEvents.Add(new XpEvent { UserId = userId, Amount = amount, Reason = reason, Correct = correct, At = Now });
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <summary>XP for one answer (test or tutor), then quests and badges.</summary>
    public async Task<Reward> AnswerAsync(string userId, bool correct, int difficulty, int run, string source, CancellationToken ct = default) =>
        await AnswerAsync(userId, correct, difficulty, run, source, 0, false, ct).ConfigureAwait(false);

    public async Task<Reward> AnswerAsync(string userId, bool correct, int difficulty, int run, string source, int hints, bool sure, CancellationToken ct = default)
    {
        var profile = await db.StudentProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId, ct).ConfigureAwait(false);
        var firstToday = !await ActiveTodayAsync(userId, ct).ConfigureAwait(false);
        if (firstToday)
        {
            // First answer of the day: a missed day in between is covered now by a streak freeze, if the student has enough
            await SettleStreakAsync(userId, ct).ConfigureAwait(false);
        }

        var boosted = (profile is not null && HappyHour.IsActive(profile.HappyHour, LocalTime)) || await DoubleXpActiveAsync(userId, ct).ConfigureAwait(false);
        var xp = HappyHour.Apply(Xp.ForAnswer(correct, difficulty, run, hints, sure), boosted);
        await AwardAsync(userId, xp, source, correct, ct).ConfigureAwait(false);
        var reward = new Reward(xp, [], []) { Boosted = boosted };
        if (firstToday && source is "answer" or "tutor")
        {
            // The first answer of the day is the one that can make the streak reach a milestone: gems, once per milestone
            var streak = await StreakAsync(userId, ct).ConfigureAwait(false);
            var key = "streak:" + streak.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (Streaks.IsMilestone(streak) && !await db.GemEvents.AnyAsync(g => g.UserId == userId && g.Reason == key, ct).ConfigureAwait(false))
            {
                db.GemEvents.Add(new GemEvent { UserId = userId, Amount = Streaks.MilestoneGems(streak), Reason = key, At = Now });
                await db.SaveChangesAsync(ct).ConfigureAwait(false);
                reward = reward with { Milestone = streak, Gems = Streaks.MilestoneGems(streak) };
            }
        }

        return reward.Plus(await CheckAsync(userId, ct).ConfigureAwait(false));
    }

    /// <summary>The evening reminder: the streak is alive but nothing was done today (null when it isn't at risk).</summary>
    public async Task<(int Streak, int HoursLeft, int Freezes)?> StreakRiskAsync(string userId, CancellationToken ct = default)
    {
        if (await ActiveTodayAsync(userId, ct).ConfigureAwait(false))
        {
            return null;
        }

        var streak = await StreakAsync(userId, ct).ConfigureAwait(false);
        var time = LocalTime;
        return Streaks.AtRisk(streak, false, time) ? (streak, Streaks.HoursLeft(time), await FreezesOwnedAsync(userId, ct).ConfigureAwait(false)) : null;
    }

    /// <summary>The time of day where the platform runs (same zone as the streak days).</summary>
    public TimeOnly LocalTime => TimeOnly.FromDateTime(TimeZoneInfo.ConvertTime(Now, Zone).DateTime);

    /// <summary>XP earned today, all sources.</summary>
    public async Task<int> TodayXpAsync(string userId, CancellationToken ct = default)
    {
        var from = StartOf(Today);
        return await db.XpEvents.Where(x => x.UserId == userId && x.At >= from).SumAsync(x => x.Amount, ct).ConfigureAwait(false);
    }

    public async Task<DailyGoalView> GoalAsync(string userId, CancellationToken ct = default)
    {
        var profile = await ProfileAsync(userId, ct).ConfigureAwait(false);
        var goal = DailyGoals.Normalize(profile.DailyGoal);
        var today = await TodayXpAsync(userId, ct).ConfigureAwait(false);
        var time = LocalTime;
        return new DailyGoalView(goal, today, DailyGoals.Percent(today, goal), DailyGoals.Reached(today, goal), profile.HappyHour,
            HappyHour.IsActive(profile.HappyHour, time), HappyHour.MinutesLeft(profile.HappyHour, time), profile.Sounds);
    }

    /// <summary>Saves the daily goal and the happy hour (values outside the offered options are normalized).</summary>
    public async Task SaveGoalAsync(string userId, int goal, int happyHour, CancellationToken ct = default)
    {
        var profile = await ProfileAsync(userId, ct).ConfigureAwait(false);
        profile.DailyGoal = DailyGoals.Normalize(goal);
        profile.HappyHour = HappyHour.Normalize(happyHour);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task SaveSoundsAsync(string userId, bool on, CancellationToken ct = default)
    {
        var profile = await ProfileAsync(userId, ct).ConfigureAwait(false);
        profile.Sounds = on;
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// The end-of-lesson screens of a finished test of this student (null otherwise): XP of the lesson, accuracy, time,
    /// whether this lesson is the one that kept the streak alive today, and the daily goal before/after.
    /// </summary>
    public async Task<LessonEnd?> LessonEndAsync(string userId, string attemptId, CancellationToken ct = default)
    {
        var attempt = await db.QuizAttempts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == attemptId && a.UserId == userId, ct).ConfigureAwait(false);
        if (attempt?.FinishedAt is not { } finished)
        {
            return null;
        }

        var lessonXp = await db.XpEvents.Where(x => x.UserId == userId && x.At >= attempt.StartedAt).SumAsync(x => x.Amount, ct).ConfigureAwait(false);
        var todayXp = await TodayXpAsync(userId, ct).ConfigureAwait(false);
        var todayStart = StartOf(Today);
        var startedToday = attempt.StartedAt >= todayStart;
        var activeBefore = await db.XpEvents.AnyAsync(x => x.UserId == userId && (x.Reason == "answer" || x.Reason == "tutor") && x.At >= todayStart && x.At < attempt.StartedAt, ct).ConfigureAwait(false);
        var days = await ActiveDaysAsync(userId, null, ct).ConfigureAwait(false);
        var profile = await ProfileAsync(userId, ct).ConfigureAwait(false);
        var goal = DailyGoals.Normalize(profile.DailyGoal);
        var before = startedToday ? todayXp - lessonXp : todayXp;
        return new LessonEnd(lessonXp, attempt.Correct, attempt.Length, (int)Math.Max(0, (finished - attempt.StartedAt).TotalSeconds),
            Streaks.Current(days, Today, await FrozenDaysAsync(userId, ct).ConfigureAwait(false)), startedToday && !activeBefore && days.Contains(Today), goal, todayXp,
            DailyGoals.Reached(todayXp, goal), DailyGoals.JustReached(before, todayXp, goal), HappyHour.IsActive(profile.HappyHour, LocalTime))
        {
            Gems = await db.GemEvents.Where(g => g.UserId == userId && g.At >= attempt.StartedAt && g.Amount > 0).SumAsync(g => g.Amount, ct).ConfigureAwait(false),
        };
    }

    /// <summary>Claims finished quests of today and new badges (each gives XP once).</summary>
    public async Task<Reward> CheckAsync(string userId, CancellationToken ct = default)
    {
        var today = Today;
        var stats = await DayStatsAsync(userId, today, ct).ConfigureAwait(false);
        var claimed = await db.QuestClaims.Where(q => q.UserId == userId && q.Day == today.DayNumber).Select(q => q.Key).ToListAsync(ct).ConfigureAwait(false);
        var quests = new List<string>();
        var xp = 0;
        var gems = 0;
        foreach (var quest in Quests.ForDay(userId, today).Where(q => Quests.Done(q, stats) && !claimed.Contains(q.Key)))
        {
            db.QuestClaims.Add(new QuestClaim { UserId = userId, Day = today.DayNumber, Key = quest.Key });
            db.XpEvents.Add(new XpEvent { UserId = userId, Amount = quest.Reward, Reason = "quest:" + quest.Key, At = Now });
            db.GemEvents.Add(new GemEvent { UserId = userId, Amount = Gems.Quest, Reason = "quest:" + quest.Key, At = Now });
            quests.Add(quest.Key);
            xp += quest.Reward;
            gems += Gems.Quest;
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        var badgeStats = await BadgeStatsAsync(userId, ct).ConfigureAwait(false);
        var owned = await db.BadgeAwards.Where(b => b.UserId == userId).Select(b => b.Key).ToListAsync(ct).ConfigureAwait(false);
        var badges = new List<string>();
        foreach (var badge in Badges.Earned(badgeStats).Where(b => !owned.Contains(b.Key)))
        {
            db.BadgeAwards.Add(new BadgeAward { UserId = userId, Key = badge.Key, EarnedAt = Now });
            db.XpEvents.Add(new XpEvent { UserId = userId, Amount = Xp.Badge, Reason = "badge:" + badge.Key, At = Now });
            db.GemEvents.Add(new GemEvent { UserId = userId, Amount = Gems.Badge, Reason = "badge:" + badge.Key, At = Now });
            badges.Add(badge.Key);
            xp += Xp.Badge;
            gems += Gems.Badge;
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        // The daily goal pays its gems once a day, the first time today's XP reaches it
        var goalKey = "goal:" + today.DayNumber.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var profile = await db.StudentProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId, ct).ConfigureAwait(false);
        var goal = DailyGoals.Normalize(profile?.DailyGoal ?? DailyGoals.Default);
        if (DailyGoals.Reached(await TodayXpAsync(userId, ct).ConfigureAwait(false), goal)
            && !await db.GemEvents.AnyAsync(g => g.UserId == userId && g.Reason == goalKey, ct).ConfigureAwait(false))
        {
            db.GemEvents.Add(new GemEvent { UserId = userId, Amount = Gems.DailyGoal, Reason = goalKey, At = Now });
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
            gems += Gems.DailyGoal;
        }

        return new Reward(xp, badges, quests) { Gems = gems };
    }

    // ---------- Personality (phase 5): the week's mistakes, for "the story of your mistakes" ----------

    /// <summary>
    /// The topics the student got wrong most in the last 7 days (at most <see cref="MistakeStory.MaxTopics"/>), each with
    /// its latest wrong question and the right answer (bank content, never something the student wrote).
    /// </summary>
    public async Task<IReadOnlyList<(Area Area, Level Level, string Topic, string Prompt, string Answer)>> MistakesOfWeekAsync(string userId, CancellationToken ct = default)
    {
        var since = Now.AddDays(-7);
        var wrong = await db.QuizAnswers.AsNoTracking().Where(a => a.UserId == userId && !a.Correct && a.At >= since && a.Prompt != "")
            .OrderByDescending(a => a.Id).Take(200).ToListAsync(ct).ConfigureAwait(false);
        return wrong.GroupBy(a => (a.Area, a.Level, a.Topic)).OrderByDescending(g => g.Count()).Take(MistakeStory.MaxTopics)
            .Select(g =>
            {
                var latest = g.First();
                var options = System.Text.Json.JsonSerializer.Deserialize<List<string>>(latest.OptionsJson) ?? [];
                var answer = latest.CorrectIndex >= 0 && latest.CorrectIndex < options.Count ? options[latest.CorrectIndex] : "";
                return (g.Key.Area, g.Key.Level, g.Key.Topic, latest.Prompt, answer);
            })
            .Where(x => x.answer.Length > 0).ToList();
    }

    /// <summary>A classic question this student answered in a test (prompt, options and right answer), for "give me a trick".</summary>
    public async Task<QuizAnswer?> AnsweredAsync(string userId, string attemptId, int number, CancellationToken ct = default) =>
        await db.QuizAnswers.AsNoTracking().FirstOrDefaultAsync(a => a.UserId == userId && a.AttemptId == attemptId && a.Number == number, ct).ConfigureAwait(false);

    // ---------- Gems, shop and streak freezes (phase 2 of the Duolingo pass) ----------

    public async Task<int> GemsAsync(string userId, CancellationToken ct = default) =>
        await db.GemEvents.Where(g => g.UserId == userId).SumAsync(g => g.Amount, ct).ConfigureAwait(false);

    private async Task<bool> ActiveTodayAsync(string userId, CancellationToken ct)
    {
        var from = StartOf(Today);
        return await db.XpEvents.AnyAsync(x => x.UserId == userId && (x.Reason == "answer" || x.Reason == "tutor") && x.At >= from, ct).ConfigureAwait(false);
    }

    private async Task<List<DateOnly>> FrozenDaysAsync(string userId, CancellationToken ct) =>
        (await db.StreakFreezeUses.Where(f => f.UserId == userId).Select(f => f.Day).ToListAsync(ct).ConfigureAwait(false)).Select(DateOnly.FromDayNumber).ToList();

    private async Task<int> FreezesOwnedAsync(string userId, CancellationToken ct)
    {
        var bought = await db.GemEvents.CountAsync(g => g.UserId == userId && g.Reason == "buy:" + nameof(ShopItem.StreakFreeze), ct).ConfigureAwait(false);
        var used = await db.StreakFreezeUses.CountAsync(f => f.UserId == userId, ct).ConfigureAwait(false);
        return Math.Max(0, bought - used);
    }

    /// <summary>Covers the days missed since the last active day with streak freezes, when the student owns enough (all or nothing).</summary>
    public async Task<IReadOnlyList<DateOnly>> SettleStreakAsync(string userId, CancellationToken ct = default)
    {
        var owned = await FreezesOwnedAsync(userId, ct).ConfigureAwait(false);
        if (owned == 0)
        {
            return [];
        }

        var days = Streaks.DaysToFreeze(await ActiveDaysAsync(userId, null, ct).ConfigureAwait(false), await FrozenDaysAsync(userId, ct).ConfigureAwait(false), Today, owned);
        foreach (var day in days)
        {
            db.StreakFreezeUses.Add(new StreakFreezeUse { UserId = userId, Day = day.DayNumber, At = Now });
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return days;
    }

    /// <summary>The current streak, counting the days saved by freezes as kept (not as added).</summary>
    public async Task<int> StreakAsync(string userId, CancellationToken ct = default) =>
        Streaks.Current(await ActiveDaysAsync(userId, null, ct).ConfigureAwait(false), Today, await FrozenDaysAsync(userId, ct).ConfigureAwait(false));

    private async Task<DateTimeOffset?> LastDoubleXpAsync(string userId, CancellationToken ct) =>
        (await db.GemEvents.Where(g => g.UserId == userId && g.Reason == "buy:" + nameof(ShopItem.DoubleXp)).Select(g => g.At).ToListAsync(ct).ConfigureAwait(false))
            .Cast<DateTimeOffset?>().DefaultIfEmpty(null).Max();

    private async Task<bool> DoubleXpActiveAsync(string userId, CancellationToken ct) =>
        Shop.DoubleXpActive(await LastDoubleXpAsync(userId, ct).ConfigureAwait(false), Now);

    public async Task<ShopView> ShopAsync(string userId, CancellationToken ct = default)
    {
        var profile = await ProfileAsync(userId, ct).ConfigureAwait(false);
        await SettleStreakAsync(userId, ct).ConfigureAwait(false);
        var frames = (await db.GemEvents.Where(g => g.UserId == userId && g.Reason.StartsWith("buy:Frame")).Select(g => g.Reason).ToListAsync(ct).ConfigureAwait(false))
            .Select(r => Enum.TryParse<ShopItem>(r[4..], out var item) ? Shop.FrameKey(item) : "").Where(k => k.Length > 0).ToHashSet();
        return new ShopView(await GemsAsync(userId, ct).ConfigureAwait(false), await FreezesOwnedAsync(userId, ct).ConfigureAwait(false),
            Shop.DoubleXpMinutesLeft(await LastDoubleXpAsync(userId, ct).ConfigureAwait(false), Now), frames, profile.Frame);
    }

    /// <summary>Takes gems if the balance allows it (false otherwise; nothing is spent).</summary>
    public async Task<bool> SpendAsync(string userId, int price, string reason, CancellationToken ct = default)
    {
        if (await GemsAsync(userId, ct).ConfigureAwait(false) < price)
        {
            return false;
        }

        db.GemEvents.Add(new GemEvent { UserId = userId, Amount = -price, Reason = reason, At = Now });
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return true;
    }

    /// <summary>Buys an item of the shop. Potions are bought where they are drunk (during an adventure, see QuizStore).</summary>
    public async Task<BuyOutcome> BuyAsync(string userId, ShopItem item, CancellationToken ct = default)
    {
        if (!Enum.IsDefined(item) || item == ShopItem.Potion)
        {
            return BuyOutcome.NotNow;
        }

        var shop = await ShopAsync(userId, ct).ConfigureAwait(false);
        if (item == ShopItem.StreakFreeze && shop.Freezes >= Shop.MaxFreezes)
        {
            return BuyOutcome.Limit;
        }

        if (Shop.IsFrame(item) && shop.Frames.Contains(Shop.FrameKey(item)))
        {
            return BuyOutcome.AlreadyOwned;
        }

        if (!await SpendAsync(userId, Shop.Price(item), "buy:" + item, ct).ConfigureAwait(false))
        {
            return BuyOutcome.NotEnoughGems;
        }

        if (Shop.IsFrame(item))
        {
            var profile = await ProfileAsync(userId, ct).ConfigureAwait(false);
            profile.Frame = Shop.FrameKey(item);
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        return BuyOutcome.Bought;
    }

    /// <summary>Equips an owned frame ("" takes it off). False for a frame the student doesn't own.</summary>
    public async Task<bool> EquipFrameAsync(string userId, string frame, CancellationToken ct = default)
    {
        var shop = await ShopAsync(userId, ct).ConfigureAwait(false);
        if (!Shop.IsFrameKey(frame) || (frame.Length > 0 && !shop.Frames.Contains(frame)))
        {
            return false;
        }

        var profile = await ProfileAsync(userId, ct).ConfigureAwait(false);
        profile.Frame = frame;
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return true;
    }

    public async Task<DayStats> DayStatsAsync(string userId, DateOnly day, CancellationToken ct = default)
    {
        var (from, to) = (StartOf(day), StartOf(day.AddDays(1)));
        var answers = await db.QuizAnswers.Where(a => a.UserId == userId && a.At >= from && a.At < to)
            .Select(a => new { a.Correct, a.Area }).ToListAsync(ct).ConfigureAwait(false);
        var tutor = await db.Attempts.CountAsync(a => a.UserId == userId && a.At >= from && a.At < to, ct).ConfigureAwait(false);
        var tests = await db.QuizAttempts.Where(a => a.UserId == userId && a.StartedAt >= from && a.StartedAt < to)
            .Select(a => new { a.Mode, a.BestRun, a.FinishedAt, a.Correct, a.Length }).ToListAsync(ct).ConfigureAwait(false);
        var finished = tests.Where(t => t.FinishedAt is not null).ToList();
        return new DayStats(
            answers.Count + tutor,
            answers.Count(a => a.Correct),
            tests.Select(t => t.BestRun).DefaultIfEmpty(0).Max(),
            finished.Count(t => t.Mode is QuizMode.Practice or QuizMode.Topic or QuizMode.Evo or QuizMode.Adventure),
            finished.Count(t => t.Mode is QuizMode.Practice or QuizMode.Topic or QuizMode.Evo && t.Correct == t.Length),
            answers.Select(a => a.Area).Distinct().Count(),
            finished.Count(t => t.Mode == QuizMode.Duel),
            finished.Count(t => t.Mode == QuizMode.Placement),
            tutor,
            finished.Count(t => t.Mode == QuizMode.Review),
            finished.Count(t => t.Mode is QuizMode.Evo or QuizMode.Notebook),
            await db.TeachSessions.CountAsync(t => t.UserId == userId && t.FinishedAt >= from && t.FinishedAt < to, ct).ConfigureAwait(false));
    }

    private async Task<List<DateOnly>> ActiveDaysAsync(string userId, DateTimeOffset? since, CancellationToken ct)
    {
        var query = db.XpEvents.Where(x => x.UserId == userId && (x.Reason == "answer" || x.Reason == "tutor"));
        if (since is { } s)
        {
            query = query.Where(x => x.At >= s);
        }

        var times = await query.Select(x => x.At).ToListAsync(ct).ConfigureAwait(false);
        return times.Select(DayOf).ToList();
    }

    public async Task<BadgeStats> BadgeStatsAsync(string userId, CancellationToken ct = default)
    {
        var quizCorrect = await db.QuizAnswers.CountAsync(a => a.UserId == userId && a.Correct, ct).ConfigureAwait(false);
        var tutorCorrect = await db.Attempts.CountAsync(a => a.UserId == userId && a.Correct, ct).ConfigureAwait(false);
        var tests = await db.QuizAttempts.Where(a => a.UserId == userId && a.FinishedAt != null)
            .Select(a => new { a.Mode, a.Correct, a.Length, a.Won, a.Topic }).ToListAsync(ct).ConfigureAwait(false);
        var areas = await db.QuizAnswers.Where(a => a.UserId == userId).Select(a => a.Area).Distinct().CountAsync(ct).ConfigureAwait(false);
        var advanced = await db.QuizAnswers.AnyAsync(a => a.UserId == userId && a.Level >= Level.Master, ct).ConfigureAwait(false);
        var mastered = await db.Mastery.CountAsync(m => m.UserId == userId && m.Status == MasteryStatus.Mastered, ct).ConfigureAwait(false);
        var xp = await TotalXpAsync(userId, ct).ConfigureAwait(false);
        var duels = await db.Duels.Where(d => d.Status == DuelStatus.Finished && (d.ChallengerId == userId || d.OpponentId == userId)).ToListAsync(ct).ConfigureAwait(false);
        var won = duels.Count(d => Duels.Outcome(d.ChallengerScore ?? 0, d.OpponentScore ?? 0) == (d.ChallengerId == userId ? 1 : -1));
        var evoCaught = await (from a in db.QuizAnswers
                               join t in db.QuizAttempts on a.AttemptId equals t.Id
                               where a.UserId == userId && a.Correct && (t.Mode == QuizMode.Evo || t.Mode == QuizMode.Notebook)
                               select a.Id).CountAsync(ct).ConfigureAwait(false);
        var certificates = await db.Certificates.Where(c => c.UserId == userId).Select(c => c.SubjectKey).ToListAsync(ct).ConfigureAwait(false);
        return new BadgeStats(
            quizCorrect + tutorCorrect,
            Streaks.Best(await ActiveDaysAsync(userId, null, ct).ConfigureAwait(false)),
            tests.Count(t => t.Mode is QuizMode.Practice or QuizMode.Topic),
            tests.Count(t => t.Mode is QuizMode.Practice or QuizMode.Topic && t.Correct == t.Length),
            areas,
            tests.Count(t => t.Mode == QuizMode.Placement),
            won,
            mastered,
            xp,
            advanced,
            tests.Count(t => t.Mode == QuizMode.Review),
            evoCaught,
            tests.Count(t => t.Mode == QuizMode.Adventure && t.Won),
            await db.TeachSessions.Where(t => t.UserId == userId && t.FinishedAt != null && t.Xp >= 50).Select(t => t.Concept).Distinct().CountAsync(ct).ConfigureAwait(false),
            tests.Count(t => t.Mode == QuizMode.Interview),
            certificates.Count,
            certificates.Count(k => Trades.Find(k)?.Kind == TradeKind.Hobby),
            await db.IdeaVotes.CountAsync(v => v.UserId == userId && v.Proposed && db.Ideas.Any(i => i.Id == v.IdeaId && i.Status == EduVerse.Core.Ideas.IdeaStatus.Available), ct).ConfigureAwait(false),
            await db.ThinkingNotes.CountAsync(t => t.UserId == userId, ct).ConfigureAwait(false),
            tests.Count(t => t.Mode == QuizMode.Adventure && t.Won && LearningPath.IsLegend(t.Topic)),
            await db.RoleplaySessions.Where(r => r.UserId == userId && r.Completed).Select(r => r.Scenario).Distinct().CountAsync(ct).ConfigureAwait(false));
    }

    public async Task<int> TotalXpAsync(string userId, CancellationToken ct = default) =>
        await db.XpEvents.Where(x => x.UserId == userId).SumAsync(x => x.Amount, ct).ConfigureAwait(false);

    public async Task<GameSummary> SummaryAsync(string userId, CancellationToken ct = default)
    {
        var profile = await ProfileAsync(userId, ct).ConfigureAwait(false);
        var xp = await TotalXpAsync(userId, ct).ConfigureAwait(false);
        var (level, into, size) = Xp.Progress(xp);
        var days = await ActiveDaysAsync(userId, null, ct).ConfigureAwait(false);
        var today = Today;
        var weekStart = WeekStart;
        var weekXp = await db.XpEvents.Where(x => x.UserId == userId && x.At >= weekStart).SumAsync(x => x.Amount, ct).ConfigureAwait(false);
        var stats = await DayStatsAsync(userId, today, ct).ConfigureAwait(false);
        var quests = Quests.ForDay(userId, today).Select(q => new QuestView(q, Quests.Progress(q, stats), Quests.Done(q, stats))).ToList();
        var badgeStats = await BadgeStatsAsync(userId, ct).ConfigureAwait(false);
        var awards = await db.BadgeAwards.Where(b => b.UserId == userId).ToDictionaryAsync(b => b.Key, b => b.EarnedAt, ct).ConfigureAwait(false);
        var badges = Badges.All.Select(b => new BadgeView(b, b.Progress(badgeStats), awards.TryGetValue(b.Key, out var at) ? at : null)).ToList();
        var counts = days.GroupBy(d => d).ToDictionary(g => g.Key, g => g.Count());
        var calendar = Enumerable.Range(0, 35).Select(i => today.AddDays(i - 34)).Select(d => (d, counts.TryGetValue(d, out var n) ? n : 0)).ToList();
        var confidence = await db.QuizAnswers.Where(a => a.UserId == userId && a.Confidence != 1).GroupBy(a => a.Confidence)
            .Select(g => new { g.Key, Total = g.Count(), Right = g.Count(a => a.Correct) }).ToListAsync(ct).ConfigureAwait(false);
        var sure = confidence.FirstOrDefault(c => c.Key == 2);
        var unsure = confidence.FirstOrDefault(c => c.Key == 0);
        await SettleStreakAsync(userId, ct).ConfigureAwait(false);
        var frozen = await FrozenDaysAsync(userId, ct).ConfigureAwait(false);
        return new GameSummary(profile, xp, level, into, size, Xp.Rank(level), Streaks.Current(days, today, frozen), Streaks.Best(days), weekXp,
            days.Contains(today), quests, badges, calendar, today)
        {
            Sure = sure?.Total ?? 0, SureRight = sure?.Right ?? 0, Unsure = unsure?.Total ?? 0, UnsureRight = unsure?.Right ?? 0,
            Gems = await GemsAsync(userId, ct).ConfigureAwait(false),
        };
    }

    /// <summary>Weekly rankings of the classes of the student (students who turned competition off are hidden; the student always sees themself).</summary>
    public async Task<IReadOnlyList<ClassBoard>> BoardsAsync(string userId, CancellationToken ct = default)
    {
        await ProfileAsync(userId, ct).ConfigureAwait(false);
        var classIds = await db.ClassMembers.Where(m => m.UserId == userId).Select(m => m.ClassroomId).ToListAsync(ct).ConfigureAwait(false);
        var boards = new List<ClassBoard>();
        var weekStart = WeekStart;
        foreach (var classroom in await db.Classrooms.Where(c => classIds.Contains(c.Id)).OrderBy(c => c.Name).ToListAsync(ct).ConfigureAwait(false))
        {
            var members = await db.ClassMembers.Where(m => m.ClassroomId == classroom.Id).Select(m => m.UserId).ToListAsync(ct).ConfigureAwait(false);
            var profiles = await db.StudentProfiles.Where(p => members.Contains(p.UserId)).ToDictionaryAsync(p => p.UserId, ct).ConfigureAwait(false);
            var xp = await db.XpEvents.Where(x => members.Contains(x.UserId) && x.At >= weekStart).GroupBy(x => x.UserId)
                .Select(g => new { g.Key, Sum = g.Sum(x => x.Amount) }).ToDictionaryAsync(g => g.Key, g => g.Sum, ct).ConfigureAwait(false);
            var correct = await db.XpEvents.CountAsync(x => members.Contains(x.UserId) && x.At >= weekStart && x.Correct, ct).ConfigureAwait(false);
            var visible = members.Where(m => profiles.TryGetValue(m, out var p) && p.Nickname.Length > 0 && (p.Compete || m == userId)).ToList();
            var ranked = Leaderboard.Rank(visible, m => xp.TryGetValue(m, out var v) ? v : 0)
                .Select(r => new RankEntry(r.Item, profiles[r.Item].Nickname, profiles[r.Item].Avatar, profiles[r.Item].Team,
                    xp.TryGetValue(r.Item, out var v) ? v : 0, r.Position, r.Item == userId) { Frame = profiles[r.Item].Frame }).ToList();
            var teams = Enum.GetValues<Team>().Select(t =>
            {
                var teamMembers = members.Where(m => profiles.TryGetValue(m, out var p) && p.Team == t).ToList();
                return (Team: t, Score: Teams.Score(teamMembers.Select(m => xp.TryGetValue(m, out var v) ? v : 0).ToList()), Members: teamMembers.Count);
            }).Where(t => t.Members > 0).ToList();
            var teamRanks = Leaderboard.Rank(teams, t => t.Score).Select(r => new TeamEntry(r.Item.Team, r.Item.Score, r.Item.Members, r.Position)).ToList();
            boards.Add(new ClassBoard(classroom.Id, classroom.Name, ranked, teamRanks, correct, WeeklyGoalPerStudent * members.Count));
        }

        return boards;
    }

    /// <summary>Position of the student in the first of their classes (for the dashboard).</summary>
    public async Task<(int Position, int Size)?> PositionAsync(string userId, CancellationToken ct = default)
    {
        var boards = await BoardsAsync(userId, ct).ConfigureAwait(false);
        var board = boards.Count > 0 ? boards[0] : null;
        var me = board?.Students.FirstOrDefault(s => s.Me);
        return me is null || board is null ? null : (me.Position, board.Students.Count);
    }

    public async Task<IReadOnlyList<RankEntry>> RivalsAsync(string userId, CancellationToken ct = default)
    {
        var boards = await BoardsAsync(userId, ct).ConfigureAwait(false);
        var me = await ProfileAsync(userId, ct).ConfigureAwait(false);
        if (!me.Compete)
        {
            return [];
        }

        var ids = boards.SelectMany(b => b.Students).Where(s => !s.Me).Select(s => s.UserId).Distinct().ToList();
        var competing = await db.StudentProfiles.Where(p => ids.Contains(p.UserId) && p.Compete).Select(p => p.UserId).ToListAsync(ct).ConfigureAwait(false);
        return boards.SelectMany(b => b.Students).Where(s => competing.Contains(s.UserId)).DistinctBy(s => s.UserId).OrderBy(s => s.Nickname).ToList();
    }

    public enum ChallengeResult
    {
        Created,
        NotAllowed,
        TooMany,
    }

    public async Task<(ChallengeResult Result, Duel? Duel)> ChallengeAsync(string userId, string opponentId, Area area, Level level, CancellationToken ct = default)
    {
        await ExpireAsync(ct).ConfigureAwait(false);
        if (opponentId == userId || !Catalog.Available(area, level))
        {
            return (ChallengeResult.NotAllowed, null);
        }

        var rivals = await RivalsAsync(userId, ct).ConfigureAwait(false);
        if (rivals.All(r => r.UserId != opponentId))
        {
            return (ChallengeResult.NotAllowed, null);
        }

        var open = await db.Duels.CountAsync(d => d.ChallengerId == userId && d.Status == DuelStatus.Waiting, ct).ConfigureAwait(false);
        if (open >= Duels.MaxOpen)
        {
            return (ChallengeResult.TooMany, null);
        }

        var shared = await db.ClassMembers.Where(m => m.UserId == userId).Select(m => m.ClassroomId)
            .Intersect(db.ClassMembers.Where(m => m.UserId == opponentId).Select(m => m.ClassroomId)).FirstAsync(ct).ConfigureAwait(false);
        var duel = new Duel
        {
            ClassroomId = shared, ChallengerId = userId, OpponentId = opponentId, Area = area, Level = level,
            Seed = Random.Shared.Next(1, int.MaxValue), Status = DuelStatus.Waiting, CreatedAt = Now,
        };
        db.Duels.Add(duel);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return (ChallengeResult.Created, duel);
    }

    private async Task ExpireAsync(CancellationToken ct)
    {
        var limit = Now - Duels.Lifetime;
        var old = await db.Duels.Where(d => d.Status == DuelStatus.Waiting && d.CreatedAt < limit).ToListAsync(ct).ConfigureAwait(false);
        foreach (var d in old)
        {
            d.Status = DuelStatus.Expired;
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<Duel?> DuelForAsync(string userId, int duelId, CancellationToken ct = default)
    {
        await ExpireAsync(ct).ConfigureAwait(false);
        var duel = await db.Duels.FirstOrDefaultAsync(d => d.Id == duelId && (d.ChallengerId == userId || d.OpponentId == userId), ct).ConfigureAwait(false);
        var played = duel is not null && (duel.ChallengerId == userId ? duel.ChallengerScore : duel.OpponentScore) is not null;
        return duel is { Status: DuelStatus.Waiting } && !played ? duel : null;
    }

    public async Task<IReadOnlyList<DuelView>> DuelsAsync(string userId, CancellationToken ct = default)
    {
        await ExpireAsync(ct).ConfigureAwait(false);
        var duels = await db.Duels.Where(d => d.ChallengerId == userId || d.OpponentId == userId).OrderByDescending(d => d.CreatedAt).Take(20).ToListAsync(ct).ConfigureAwait(false);
        var ids = duels.SelectMany(d => new[] { d.ChallengerId, d.OpponentId }).Distinct().ToList();
        var profiles = await db.StudentProfiles.Where(p => ids.Contains(p.UserId)).ToDictionaryAsync(p => p.UserId, ct).ConfigureAwait(false);
        return duels.Select(d =>
        {
            var mine = d.ChallengerId == userId;
            var rival = profiles.TryGetValue(mine ? d.OpponentId : d.ChallengerId, out var p) ? p : null;
            var myScore = mine ? d.ChallengerScore : d.OpponentScore;
            return new DuelView(d, rival?.Nickname ?? "?", rival?.Avatar ?? "❔", mine, myScore, mine ? d.OpponentScore : d.ChallengerScore,
                d.Status == DuelStatus.Waiting && myScore is null);
        }).ToList();
    }

    /// <summary>Saves the score of one side; when both played, the duel ends and XP is given (win or draw).</summary>
    public async Task<int?> DuelScoreAsync(string userId, int duelId, int score, CancellationToken ct = default)
    {
        var duel = await db.Duels.FirstOrDefaultAsync(d => d.Id == duelId, ct).ConfigureAwait(false);
        if (duel is null)
        {
            return null;
        }

        if (duel.ChallengerId == userId)
        {
            duel.ChallengerScore ??= score;
        }
        else if (duel.OpponentId == userId)
        {
            duel.OpponentScore ??= score;
        }

        if (duel is { ChallengerScore: { } a, OpponentScore: { } b, Status: DuelStatus.Waiting })
        {
            duel.Status = DuelStatus.Finished;
            duel.FinishedAt = Now;
            var outcome = Duels.Outcome(a, b);
            if (outcome == 0)
            {
                db.XpEvents.Add(new XpEvent { UserId = duel.ChallengerId, Amount = Xp.DuelDraw, Reason = "duel", At = Now });
                db.XpEvents.Add(new XpEvent { UserId = duel.OpponentId, Amount = Xp.DuelDraw, Reason = "duel", At = Now });
            }
            else
            {
                db.XpEvents.Add(new XpEvent { UserId = outcome > 0 ? duel.ChallengerId : duel.OpponentId, Amount = Xp.DuelWin, Reason = "duel", At = Now });
            }
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return duel.Status == DuelStatus.Finished ? Duels.Outcome(duel.ChallengerScore ?? 0, duel.OpponentScore ?? 0) * (duel.ChallengerId == userId ? 1 : -1) : null;
    }
}
