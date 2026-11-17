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

    public Reward Plus(Reward other) => new(Xp + other.Xp, [.. Badges, .. other.Badges], [.. Quests, .. other.Quests]);
}

public sealed record QuestView(Quest Quest, int Progress, bool Done);

public sealed record BadgeView(Badge Badge, int Progress, DateTimeOffset? EarnedAt);

public sealed record RankEntry(string UserId, string Nickname, string Avatar, Team Team, int Xp, int Position, bool Me);

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

    private DateTimeOffset StartOf(DateOnly day)
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
        var xp = Xp.ForAnswer(correct, difficulty, run, hints, sure);
        await AwardAsync(userId, xp, source, correct, ct).ConfigureAwait(false);
        return new Reward(xp, [], []).Plus(await CheckAsync(userId, ct).ConfigureAwait(false));
    }

    /// <summary>Claims finished quests of today and new badges (each gives XP once).</summary>
    public async Task<Reward> CheckAsync(string userId, CancellationToken ct = default)
    {
        var today = Today;
        var stats = await DayStatsAsync(userId, today, ct).ConfigureAwait(false);
        var claimed = await db.QuestClaims.Where(q => q.UserId == userId && q.Day == today.DayNumber).Select(q => q.Key).ToListAsync(ct).ConfigureAwait(false);
        var quests = new List<string>();
        var xp = 0;
        foreach (var quest in Quests.ForDay(userId, today).Where(q => Quests.Done(q, stats) && !claimed.Contains(q.Key)))
        {
            db.QuestClaims.Add(new QuestClaim { UserId = userId, Day = today.DayNumber, Key = quest.Key });
            db.XpEvents.Add(new XpEvent { UserId = userId, Amount = quest.Reward, Reason = "quest:" + quest.Key, At = Now });
            quests.Add(quest.Key);
            xp += quest.Reward;
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        var badgeStats = await BadgeStatsAsync(userId, ct).ConfigureAwait(false);
        var owned = await db.BadgeAwards.Where(b => b.UserId == userId).Select(b => b.Key).ToListAsync(ct).ConfigureAwait(false);
        var badges = new List<string>();
        foreach (var badge in Badges.Earned(badgeStats).Where(b => !owned.Contains(b.Key)))
        {
            db.BadgeAwards.Add(new BadgeAward { UserId = userId, Key = badge.Key, EarnedAt = Now });
            db.XpEvents.Add(new XpEvent { UserId = userId, Amount = Xp.Badge, Reason = "badge:" + badge.Key, At = Now });
            badges.Add(badge.Key);
            xp += Xp.Badge;
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return new Reward(xp, badges, quests);
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
            .Select(a => new { a.Mode, a.Correct, a.Length, a.Won }).ToListAsync(ct).ConfigureAwait(false);
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
            await db.ThinkingNotes.CountAsync(t => t.UserId == userId, ct).ConfigureAwait(false));
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
        return new GameSummary(profile, xp, level, into, size, Xp.Rank(level), Streaks.Current(days, today), Streaks.Best(days), weekXp,
            days.Contains(today), quests, badges, calendar, today)
        {
            Sure = sure?.Total ?? 0, SureRight = sure?.Right ?? 0, Unsure = unsure?.Total ?? 0, UnsureRight = unsure?.Right ?? 0,
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
                    xp.TryGetValue(r.Item, out var v) ? v : 0, r.Position, r.Item == userId)).ToList();
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
