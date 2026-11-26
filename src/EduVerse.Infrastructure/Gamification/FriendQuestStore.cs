using EduVerse.Core.Catalog;
using EduVerse.Core.Gamification;
using EduVerse.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EduVerse.Infrastructure.Gamification;

public sealed record FriendView(string UserId, string Nickname, string Avatar, string Frame, int Xp);

public sealed record FriendQuestView(int Id, FriendView Me, FriendView Friend, int Percent, bool Reached, bool Claimed)
{
    public int Total => Me.Xp + Friend.Xp;
}

public enum FriendQuestOutcome
{
    Started,
    AlreadyPaired,
    NotAvailable,
}

/// <summary>
/// Friend quests (phase 7): a student invites a classmate who competes (never a young child) to a shared weekly XP
/// target. Only nicknames are shown. Each student is in at most one friend quest per week.
/// </summary>
public sealed class FriendQuestStore(EduVerseDb db, GamificationStore game, LeagueStore leagues)
{
    private int Week => leagues.CurrentWeek;

    private async Task<FriendQuest?> CurrentRowAsync(string userId, CancellationToken ct)
    {
        var week = Week;
        return await db.FriendQuests.FirstOrDefaultAsync(f => f.Week == week && (f.UserA == userId || f.UserB == userId), ct).ConfigureAwait(false);
    }

    /// <summary>Classmates the student can team up with this week (they compete, aren't young children and are free).</summary>
    public async Task<IReadOnlyList<FriendView>> CandidatesAsync(string userId, CancellationToken ct = default)
    {
        var week = Week;
        var classes = await db.ClassMembers.Where(m => m.UserId == userId).Select(m => m.ClassroomId).ToListAsync(ct).ConfigureAwait(false);
        var mates = await db.ClassMembers.Where(m => classes.Contains(m.ClassroomId) && m.UserId != userId).Select(m => m.UserId).Distinct().ToListAsync(ct).ConfigureAwait(false);
        var pairs = await db.FriendQuests.Where(f => f.Week == week).Select(f => new { f.UserA, f.UserB }).ToListAsync(ct).ConfigureAwait(false);
        var taken = pairs.SelectMany(p => new[] { p.UserA, p.UserB }).ToHashSet();
        var profiles = await db.StudentProfiles.AsNoTracking().Where(p => mates.Contains(p.UserId) && p.Compete && p.Nickname != "").ToListAsync(ct).ConfigureAwait(false);
        return profiles.Where(p => !Levels.IsKids(p.Level) && !taken.Contains(p.UserId)).OrderBy(p => p.Nickname)
            .Select(p => new FriendView(p.UserId, p.Nickname, p.Avatar, p.Frame, 0)).ToList();
    }

    public async Task<FriendQuestOutcome> StartAsync(string userId, string friendId, CancellationToken ct = default)
    {
        var me = await game.ProfileAsync(userId, ct).ConfigureAwait(false);
        if (!me.Compete || Levels.IsKids(me.Level))
        {
            return FriendQuestOutcome.NotAvailable;
        }

        if (await CurrentRowAsync(userId, ct).ConfigureAwait(false) is not null)
        {
            return FriendQuestOutcome.AlreadyPaired;
        }

        if (!(await CandidatesAsync(userId, ct).ConfigureAwait(false)).Any(c => c.UserId == friendId))
        {
            return FriendQuestOutcome.NotAvailable;
        }

        db.FriendQuests.Add(new FriendQuest { Week = Week, UserA = userId, UserB = friendId });
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return FriendQuestOutcome.Started;
    }

    public async Task<FriendQuestView?> CurrentAsync(string userId, CancellationToken ct = default)
    {
        var row = await CurrentRowAsync(userId, ct).ConfigureAwait(false);
        if (row is null)
        {
            return null;
        }

        var friendId = row.UserA == userId ? row.UserB : row.UserA;
        var from = game.WeekStart;
        var xp = await db.XpEvents.Where(x => (x.UserId == userId || x.UserId == friendId) && x.At >= from).GroupBy(x => x.UserId)
            .Select(g => new { g.Key, Sum = g.Sum(x => x.Amount) }).ToDictionaryAsync(g => g.Key, g => g.Sum, ct).ConfigureAwait(false);
        var profiles = await db.StudentProfiles.AsNoTracking().Where(p => p.UserId == userId || p.UserId == friendId).ToDictionaryAsync(p => p.UserId, ct).ConfigureAwait(false);
        FriendView View(string id) => new(id, profiles.GetValueOrDefault(id)?.Nickname ?? "?", profiles.GetValueOrDefault(id)?.Avatar ?? "🦊",
            profiles.GetValueOrDefault(id)?.Frame ?? "", xp.GetValueOrDefault(id));
        var (me, friend) = (View(userId), View(friendId));
        return new FriendQuestView(row.Id, me, friend, FriendQuests.Percent(me.Xp, friend.Xp), FriendQuests.Reached(me.Xp, friend.Xp),
            row.UserA == userId ? row.ClaimedA : row.ClaimedB);
    }

    /// <summary>Each partner claims the gems once, when the shared target is reached.</summary>
    public async Task<int?> ClaimAsync(string userId, CancellationToken ct = default)
    {
        var view = await CurrentAsync(userId, ct).ConfigureAwait(false);
        if (view is not { Reached: true, Claimed: false })
        {
            return null;
        }

        var row = await db.FriendQuests.FirstAsync(f => f.Id == view.Id, ct).ConfigureAwait(false);
        if (row.UserA == userId)
        {
            row.ClaimedA = true;
        }
        else
        {
            row.ClaimedB = true;
        }

        db.GemEvents.Add(new GemEvent { UserId = userId, Amount = FriendQuests.Gems, Reason = $"friend:{row.Week}", At = game.Now });
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return FriendQuests.Gems;
    }
}
