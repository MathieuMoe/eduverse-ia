using System.Text.Json;
using EduVerse.Core.Catalog;
using EduVerse.Core.Common;
using EduVerse.Core.Ideas;
using EduVerse.Infrastructure.Data;
using EduVerse.Infrastructure.Gamification;
using Microsoft.EntityFrameworkCore;

namespace EduVerse.Infrastructure.Ideas;

/// <summary>An idea as a student sees it: never who proposed it, only how many support it.</summary>
public sealed record IdeaView(int Id, IdeaKind Kind, string Text, IdeaStatus Status, int Votes, bool Mine, bool Voted);

public enum ProposeOutcome
{
    Created,
    Joined,
    Exists,
    TooShort,
    TooLong,
    NotAllowed,
    Limit,
}

public sealed record ProposeResult(ProposeOutcome Outcome, IdeaView? Idea = null, string? Existing = null);

/// <summary>An idea for the adults who moderate: votes, how many proposed it and some of their reasons (anonymous).</summary>
public sealed record IdeaRow(int Id, IdeaKind Kind, string Text, IdeaStatus Status, int Votes, int Proposers, IReadOnlyList<string> Reasons, DateTimeOffset CreatedAt);

/// <summary>Ratings of the adventures of one world.</summary>
public sealed record ThemeRating(Theme Theme, int Count, double Stars, int Fun, int TooEasy, int JustRight, int TooHard);

/// <summary>
/// Ideas of the students: they propose (a figure, an adventure world, a trade, a hobby), the same idea written differently becomes
/// a vote, an adult approves what others can see and marks what becomes available; then the Tutor tells the students who asked
/// for it and the proposers win XP and a badge. Also the short rating at the end of an adventure.
/// </summary>
public sealed class IdeaStore(EduVerseDb db, GamificationStore game, TimeProvider clock)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>The public ideas of a kind (most voted first) and the student's own ideas still waiting for an adult.</summary>
    public async Task<IReadOnlyList<IdeaView>> ListAsync(string userId, IdeaKind kind, int max = 8, CancellationToken ct = default)
    {
        var ideas = await db.Ideas.AsNoTracking().Include(i => i.Votes).Where(i => i.Kind == kind && i.Status != IdeaStatus.Rejected).ToListAsync(ct).ConfigureAwait(false);
        return ideas
            .Where(i => IdeaRules.IsPublic(i.Status) || i.Votes.Any(v => v.UserId == userId && v.Proposed))
            .OrderBy(i => i.Status == IdeaStatus.Available ? 1 : 0).ThenByDescending(i => i.Votes.Count).ThenBy(i => i.Text, StringComparer.CurrentCulture)
            .Take(max)
            .Select(i => View(i, userId))
            .ToList();
    }

    private static IdeaView View(StudentIdea i, string userId) =>
        new(i.Id, i.Kind, i.Text, i.Status, i.Votes.Count, i.Votes.Any(v => v.UserId == userId && v.Proposed), i.Votes.Any(v => v.UserId == userId));

    public async Task<ProposeResult> ProposeAsync(string userId, IdeaKind kind, string? text, string? reason, TeachLanguage language, CancellationToken ct = default)
    {
        switch (IdeaRules.Check(text, reason))
        {
            case IdeaCheck.TooShort: return new(ProposeOutcome.TooShort);
            case IdeaCheck.TooLong: return new(ProposeOutcome.TooLong);
            case IdeaCheck.NotAllowed: return new(ProposeOutcome.NotAllowed);
        }

        var clean = IdeaRules.Clean(text);
        if (IdeaRules.Existing(kind, clean, language) is { } existing)
        {
            return new(ProposeOutcome.Exists, Existing: existing);
        }

        var key = IdeaRules.Normalize(clean);
        var now = clock.GetUtcNow();
        var idea = await db.Ideas.Include(i => i.Votes).FirstOrDefaultAsync(i => i.Kind == kind && i.Key == key, ct).ConfigureAwait(false);
        if (idea is not null && idea.Votes.Any(v => v.UserId == userId))
        {
            return new(ProposeOutcome.Joined, View(idea, userId));
        }

        var since = now.AddDays(-1);
        if (await db.IdeaVotes.CountAsync(v => v.UserId == userId && v.Proposed && v.At >= since, ct).ConfigureAwait(false) >= IdeaRules.DailyLimit)
        {
            return new(ProposeOutcome.Limit);
        }

        var vote = new IdeaVote { UserId = userId, Proposed = true, Reason = string.IsNullOrWhiteSpace(reason) ? null : IdeaRules.Clean(reason), At = now };
        var created = idea is null;
        if (idea is null)
        {
            idea = new StudentIdea { Kind = kind, Key = key, Text = clean, Status = IdeaStatus.New, CreatedAt = now };
            db.Ideas.Add(idea);
        }

        idea.Votes.Add(vote);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return new(created ? ProposeOutcome.Created : ProposeOutcome.Joined, View(idea, userId));
    }

    /// <summary>Supports a public idea (once per student). Null when the idea cannot be voted.</summary>
    public async Task<IdeaView?> VoteAsync(string userId, int id, CancellationToken ct = default)
    {
        var idea = await db.Ideas.Include(i => i.Votes).FirstOrDefaultAsync(i => i.Id == id, ct).ConfigureAwait(false);
        if (idea is null || idea.Status is not (IdeaStatus.Approved or IdeaStatus.InProgress))
        {
            return null;
        }

        if (!idea.Votes.Any(v => v.UserId == userId))
        {
            idea.Votes.Add(new IdeaVote { UserId = userId, At = clock.GetUtcNow() });
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        return View(idea, userId);
    }

    /// <summary>Every idea for the adults (optionally of one kind and status), most voted first.</summary>
    public async Task<IReadOnlyList<IdeaRow>> AllAsync(IdeaKind? kind = null, IdeaStatus? status = null, CancellationToken ct = default)
    {
        var query = db.Ideas.AsNoTracking().Include(i => i.Votes).AsQueryable();
        if (kind is { } k)
        {
            query = query.Where(i => i.Kind == k);
        }

        if (status is { } s)
        {
            query = query.Where(i => i.Status == s);
        }

        var ideas = await query.ToListAsync(ct).ConfigureAwait(false);
        return ideas.OrderBy(i => i.Status).ThenByDescending(i => i.Votes.Count).ThenBy(i => i.CreatedAt)
            .Select(i => new IdeaRow(i.Id, i.Kind, i.Text, i.Status, i.Votes.Count, i.Votes.Count(v => v.Proposed),
                i.Votes.Where(v => v.Reason is not null).OrderByDescending(v => v.At).Select(v => v.Reason!).Take(3).ToList(), i.CreatedAt))
            .ToList();
    }

    /// <summary>Changes the status of an idea; when it becomes available its proposers win XP (and the badge, through the stats).</summary>
    public async Task<bool> SetStatusAsync(int id, IdeaStatus status, CancellationToken ct = default)
    {
        var idea = await db.Ideas.Include(i => i.Votes).FirstOrDefaultAsync(i => i.Id == id, ct).ConfigureAwait(false);
        if (idea is null)
        {
            return false;
        }

        var becomesAvailable = status == IdeaStatus.Available && idea.Status != IdeaStatus.Available;
        idea.Status = status;
        idea.UpdatedAt = clock.GetUtcNow();
        if (becomesAvailable)
        {
            foreach (var v in idea.Votes)
            {
                v.SeenAt = null;
            }
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        if (becomesAvailable)
        {
            foreach (var proposer in idea.Votes.Where(v => v.Proposed).Select(v => v.UserId).Distinct())
            {
                await game.AwardAsync(proposer, IdeaRules.AvailableXp, "idea", true, ct).ConfigureAwait(false);
                await game.CheckAsync(proposer, ct).ConfigureAwait(false);
            }
        }

        return true;
    }

    /// <summary>The ideas the student asked for that are now available and not seen yet (the Tutor announces them).</summary>
    public async Task<IReadOnlyList<IdeaNews>> NewsAsync(string userId, CancellationToken ct = default) =>
        await db.Ideas.AsNoTracking()
            .Where(i => i.Status == IdeaStatus.Available && i.Votes.Any(v => v.UserId == userId && v.SeenAt == null))
            .OrderByDescending(i => i.UpdatedAt)
            .Select(i => new IdeaNews(i.Text, i.Kind))
            .ToListAsync(ct).ConfigureAwait(false);

    /// <summary>The student opened the page of this kind: the news of that kind are seen.</summary>
    public async Task MarkSeenAsync(string userId, IdeaKind kind, CancellationToken ct = default)
    {
        var votes = await db.IdeaVotes.Where(v => v.UserId == userId && v.SeenAt == null
            && db.Ideas.Any(i => i.Id == v.IdeaId && i.Kind == kind && i.Status == IdeaStatus.Available)).ToListAsync(ct).ConfigureAwait(false);
        if (votes.Count == 0)
        {
            return;
        }

        var now = clock.GetUtcNow();
        foreach (var v in votes)
        {
            v.SeenAt = now;
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Ideas proposed by the student that are now available (for the badge).</summary>
    public Task<int> AvailableProposalsAsync(string userId, CancellationToken ct = default) =>
        db.IdeaVotes.CountAsync(v => v.UserId == userId && v.Proposed && db.Ideas.Any(i => i.Id == v.IdeaId && i.Status == IdeaStatus.Available), ct);

    /// <summary>Rates a finished adventure of the student (once; a second rating replaces the first).</summary>
    public async Task<bool> RateAdventureAsync(string userId, string attemptId, int stars, Felt felt, bool fun, CancellationToken ct = default)
    {
        if (stars is < 1 or > 5 || !Enum.IsDefined(felt))
        {
            return false;
        }

        var attempt = await db.QuizAttempts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == attemptId && a.UserId == userId, ct).ConfigureAwait(false);
        if (attempt is not { Mode: QuizMode.Adventure, FinishedAt: not null, AdventureJson: not null })
        {
            return false;
        }

        var theme = JsonSerializer.Deserialize<AdventureState>(attempt.AdventureJson, JsonOptions)!.Theme;
        var rating = await db.AdventureRatings.FirstOrDefaultAsync(r => r.AttemptId == attemptId, ct).ConfigureAwait(false);
        if (rating is null)
        {
            rating = new AdventureRating { AttemptId = attemptId, UserId = userId };
            db.AdventureRatings.Add(rating);
        }

        (rating.Theme, rating.Stars, rating.Felt, rating.Fun, rating.At) = (theme, stars, felt, fun, clock.GetUtcNow());
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return true;
    }

    public async Task<IReadOnlyList<ThemeRating>> AdventureRatingsAsync(CancellationToken ct = default)
    {
        var all = await db.AdventureRatings.AsNoTracking().ToListAsync(ct).ConfigureAwait(false);
        return Enum.GetValues<Theme>().Select(t =>
        {
            var r = all.Where(x => x.Theme == t).ToList();
            return new ThemeRating(t, r.Count, r.Count == 0 ? 0 : Math.Round(r.Average(x => x.Stars), 1), r.Count(x => x.Fun),
                r.Count(x => x.Felt == Felt.TooEasy), r.Count(x => x.Felt == Felt.JustRight), r.Count(x => x.Felt == Felt.TooHard));
        }).ToList();
    }
}
