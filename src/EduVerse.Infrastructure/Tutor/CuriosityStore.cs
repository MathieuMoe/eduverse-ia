using EduVerse.Core.Ideas;
using EduVerse.Core.Tutor;
using EduVerse.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EduVerse.Infrastructure.Tutor;

public enum CuriosityOutcome
{
    Created,
    TooShort,
    TooLong,
    NotAllowed,
    Limit,
}

/// <summary>A question as the dashboard widget shows it (never who else asked it: this is the student's own list).</summary>
public sealed record CuriosityView(int Id, string Text, DateTimeOffset CreatedAt);

/// <summary>
/// "What I wonder about": a student types a question instead of losing it, and comes back to explore it (through the existing
/// "learn any subject" flow, which already turns any topic into a lesson) whenever they like, or dismisses it. Nothing here
/// grades or measures anything — it is a place to let curiosity wait its turn.
/// </summary>
public sealed class CuriosityStore(EduVerseDb db, TimeProvider clock)
{
    public async Task<CuriosityOutcome> AddAsync(string userId, string? text, CancellationToken ct = default)
    {
        switch (Curiosity.Check(text))
        {
            case IdeaCheck.TooShort: return CuriosityOutcome.TooShort;
            case IdeaCheck.TooLong: return CuriosityOutcome.TooLong;
            case IdeaCheck.NotAllowed: return CuriosityOutcome.NotAllowed;
        }

        var now = clock.GetUtcNow();
        var openCount = await db.CuriosityQuestions.CountAsync(q => q.UserId == userId && q.Status == CuriosityStatus.Open, ct).ConfigureAwait(false);
        var since = now.AddDays(-1);
        var todayCount = await db.CuriosityQuestions.CountAsync(q => q.UserId == userId && q.CreatedAt >= since, ct).ConfigureAwait(false);
        if (openCount >= Curiosity.MaxOpen || todayCount >= Curiosity.DailyLimit)
        {
            return CuriosityOutcome.Limit;
        }

        db.CuriosityQuestions.Add(new CuriosityQuestion { UserId = userId, Text = Curiosity.Clean(text), Status = CuriosityStatus.Open, CreatedAt = now });
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return CuriosityOutcome.Created;
    }

    /// <summary>
    /// Up to <see cref="Curiosity.MaxShown"/> open questions for the dashboard widget: the ones that have waited longest for their
    /// turn first (never shown yet, before shown longest ago), so a student with more questions than fit sees them all in rotation
    /// rather than always the same few. Each one returned is marked as surfaced now.
    /// </summary>
    public async Task<IReadOnlyList<CuriosityView>> ForWidgetAsync(string userId, CancellationToken ct = default)
    {
        var open = await db.CuriosityQuestions.Where(q => q.UserId == userId && q.Status == CuriosityStatus.Open).ToListAsync(ct).ConfigureAwait(false);
        var picked = open.OrderBy(q => q.LastSurfacedAt ?? DateTimeOffset.MinValue).ThenBy(q => q.CreatedAt).Take(Curiosity.MaxShown).ToList();
        if (picked.Count > 0)
        {
            var now = clock.GetUtcNow();
            foreach (var q in picked)
            {
                q.LastSurfacedAt = now;
                q.TimesSurfaced++;
            }

            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        return picked.OrderBy(q => q.CreatedAt).Select(q => new CuriosityView(q.Id, q.Text, q.CreatedAt)).ToList();
    }

    /// <summary>The open question that has waited longest, read-only (unlike <see cref="ForWidgetAsync"/>, this does not mark it
    /// surfaced): used where showing it is incidental to something else, such as the future-self letter.</summary>
    public Task<CuriosityView?> OldestOpenAsync(string userId, CancellationToken ct = default) =>
        db.CuriosityQuestions.Where(q => q.UserId == userId && q.Status == CuriosityStatus.Open)
            .OrderBy(q => q.CreatedAt).Select(q => new CuriosityView(q.Id, q.Text, q.CreatedAt)).FirstOrDefaultAsync(ct);

    /// <summary>Marks the question explored and returns its text, so the caller can send the student straight to exploring it.
    /// Null when the question is not theirs, or already resolved.</summary>
    public async Task<string?> ExploreAsync(string userId, int id, CancellationToken ct = default)
    {
        var q = await OpenAsync(userId, id, ct).ConfigureAwait(false);
        if (q is null)
        {
            return null;
        }

        (q.Status, q.ResolvedAt) = (CuriosityStatus.Explored, clock.GetUtcNow());
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return q.Text;
    }

    public async Task<bool> DismissAsync(string userId, int id, CancellationToken ct = default)
    {
        var q = await OpenAsync(userId, id, ct).ConfigureAwait(false);
        if (q is null)
        {
            return false;
        }

        (q.Status, q.ResolvedAt) = (CuriosityStatus.Dismissed, clock.GetUtcNow());
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return true;
    }

    private Task<CuriosityQuestion?> OpenAsync(string userId, int id, CancellationToken ct) =>
        db.CuriosityQuestions.FirstOrDefaultAsync(q => q.Id == id && q.UserId == userId && q.Status == CuriosityStatus.Open, ct);
}
