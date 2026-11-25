using EduVerse.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EduVerse.Infrastructure.Tutor;

/// <summary>
/// Tracks which interview figures a student has "completed": discovered every suggested question and solved the
/// closing riddle (see <see cref="EduVerse.Core.Tutor.Interviews.RiddleCorrect"/>). This is the shared foundation
/// behind the figure album, the chronological timeline game, and the figure-connections bonus content.
/// </summary>
public sealed class FigureDiscoveryStore(EduVerseDb db, TimeProvider clock)
{
    /// <summary>The figure keys this student has completed, for a quick membership check (the album, the timeline).</summary>
    public async Task<IReadOnlySet<string>> CompletedAsync(string userId, CancellationToken ct = default) =>
        (await db.FigureDiscoveries.AsNoTracking().Where(d => d.UserId == userId).Select(d => d.FigureKey).ToListAsync(ct).ConfigureAwait(false)).ToHashSet(StringComparer.Ordinal);

    /// <summary>Marks a figure as completed. Returns true only the first time, so the caller can celebrate it once.</summary>
    public async Task<bool> MarkCompletedAsync(string userId, string figureKey, CancellationToken ct = default)
    {
        if (await db.FigureDiscoveries.AnyAsync(d => d.UserId == userId && d.FigureKey == figureKey, ct).ConfigureAwait(false))
        {
            return false;
        }

        db.FigureDiscoveries.Add(new() { UserId = userId, FigureKey = figureKey, CompletedAt = clock.GetUtcNow() });
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return true;
    }
}
