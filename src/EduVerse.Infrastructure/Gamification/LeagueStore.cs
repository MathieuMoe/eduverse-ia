using EduVerse.Core.Catalog;
using EduVerse.Core.Gamification;
using EduVerse.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EduVerse.Infrastructure.Gamification;

public sealed record LeagueRow(string UserId, string Nickname, string Avatar, string Frame, int Xp, int Position, bool Me, LeagueResult Zone);

public sealed record LeagueView(Division Division, DateTimeOffset EndsAt, IReadOnlyList<LeagueRow> Rows, int ZoneSize, LeagueResult? LastResult, int? LastPosition, bool ShowResult)
{
    public LeagueRow? Me => Rows.FirstOrDefault(r => r.Me);
}

/// <summary>
/// Weekly leagues. A student joins the week's league the first time they show up that week; last week's place is
/// settled at that moment from the XP log of that week (so it never depends on when anyone looks). The rules live in
/// <see cref="Leagues"/>; this store only groups students and counts XP.
/// </summary>
public sealed class LeagueStore(EduVerseDb db, GamificationStore game)
{
    private int WeekOf(DateTimeOffset weekStart) => game.DayOf(weekStart).DayNumber;

    public int CurrentWeek => WeekOf(game.WeekStart);

    private DateTimeOffset StartOfWeek(int week) => game.StartOf(DateOnly.FromDayNumber(week));

    /// <summary>This week's membership (created when needed), or null for a student who doesn't compete or is a young child.</summary>
    public async Task<LeagueMember?> JoinAsync(string userId, CancellationToken ct = default)
    {
        var profile = await game.ProfileAsync(userId, ct).ConfigureAwait(false);
        if (!profile.Compete || Levels.IsKids(profile.Level))
        {
            return null;
        }

        var week = CurrentWeek;
        var existing = await db.LeagueMembers.FirstOrDefaultAsync(m => m.UserId == userId && m.Week == week, ct).ConfigureAwait(false);
        if (existing is not null)
        {
            return existing;
        }

        var previous = await db.LeagueMembers.Where(m => m.UserId == userId && m.Week < week).OrderByDescending(m => m.Week).FirstOrDefaultAsync(ct).ConfigureAwait(false);
        var member = new LeagueMember { UserId = userId, Week = week, Division = Division.Bronze };
        if (previous is not null)
        {
            member.Division = previous.Division;
            if (previous.Week == week - 7)
            {
                var rows = await RankAsync(previous.Week, previous.Group, previous.Division, userId, ct).ConfigureAwait(false);
                if (rows.FirstOrDefault(r => r.Me) is { } me)
                {
                    member.PreviousResult = Leagues.ResultOf(me.Position, rows.Count, me.Xp, previous.Division);
                    member.PreviousPosition = me.Position;
                    member.Division = Leagues.Next(previous.Division, member.PreviousResult.Value);
                }
            }
        }

        var open = await db.LeagueMembers.Where(m => m.Week == week && m.Division == member.Division).GroupBy(m => m.Group)
            .Select(g => new { g.Key, Count = g.Count() }).Where(g => g.Count < Leagues.GroupSize).OrderBy(g => g.Key).FirstOrDefaultAsync(ct).ConfigureAwait(false);
        member.Group = open?.Key ?? (await db.LeagueMembers.Select(m => (int?)m.Group).MaxAsync(ct).ConfigureAwait(false) ?? 0) + 1;
        member.ResultSeen = member.PreviousResult is null;
        db.LeagueMembers.Add(member);
        try
        {
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (DbUpdateException)
        {
            // Two requests of the same student at once: the other one created the row first
            db.Entry(member).State = EntityState.Detached;
            return await db.LeagueMembers.FirstAsync(m => m.UserId == userId && m.Week == week, ct).ConfigureAwait(false);
        }

        return member;
    }

    /// <summary>The group's table for a week, ranked on the XP of that week (ties share a position).</summary>
    private async Task<IReadOnlyList<LeagueRow>> RankAsync(int week, int group, Division division, string userId, CancellationToken ct)
    {
        var members = await db.LeagueMembers.Where(m => m.Week == week && m.Group == group).Select(m => m.UserId).ToListAsync(ct).ConfigureAwait(false);
        var profiles = await db.StudentProfiles.Where(p => members.Contains(p.UserId)).ToDictionaryAsync(p => p.UserId, ct).ConfigureAwait(false);
        var (from, to) = (StartOfWeek(week), StartOfWeek(week).AddDays(7));
        var xp = await db.XpEvents.Where(x => members.Contains(x.UserId) && x.At >= from && x.At < to).GroupBy(x => x.UserId)
            .Select(g => new { g.Key, Sum = g.Sum(x => x.Amount) }).ToDictionaryAsync(g => g.Key, g => g.Sum, ct).ConfigureAwait(false);
        int Xp(string id) => xp.TryGetValue(id, out var v) ? v : 0;
        return Leaderboard.Rank(members, Xp).Select(r =>
        {
            var p = profiles.GetValueOrDefault(r.Item);
            return new LeagueRow(r.Item, p?.Nickname ?? "?", p?.Avatar ?? "🦊", p?.Frame ?? "", Xp(r.Item), r.Position, r.Item == userId,
                Leagues.ZoneOf(r.Position, members.Count, Xp(r.Item), division));
        }).ToList();
    }

    /// <summary>The student's league this week, or null if they don't compete.</summary>
    public async Task<LeagueView?> ViewAsync(string userId, CancellationToken ct = default)
    {
        var member = await JoinAsync(userId, ct).ConfigureAwait(false);
        if (member is null)
        {
            return null;
        }

        var rows = await RankAsync(member.Week, member.Group, member.Division, userId, ct).ConfigureAwait(false);
        return new LeagueView(member.Division, StartOfWeek(member.Week).AddDays(7), rows, Leagues.ZoneSize(rows.Count), member.PreviousResult, member.PreviousPosition, !member.ResultSeen);
    }

    /// <summary>The banner about last week is shown once.</summary>
    public async Task MarkResultSeenAsync(string userId, CancellationToken ct = default)
    {
        var week = CurrentWeek;
        var member = await db.LeagueMembers.FirstOrDefaultAsync(m => m.UserId == userId && m.Week == week, ct).ConfigureAwait(false);
        if (member is { ResultSeen: false })
        {
            member.ResultSeen = true;
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }
}
