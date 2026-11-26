using EduVerse.Core.Catalog;
using EduVerse.Core.Tutor;
using EduVerse.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EduVerse.Infrastructure.Tutor;

/// <summary>A topic the student just crossed into solid mastery, raw (area/level/topic are keys; display names and
/// the explanation/check-questions are resolved by the caller, which knows the student's language).</summary>
public sealed record MilestoneTopic(Area Area, Level Level, string Topic);

/// <summary>Finds a topic the student has recently mastered, for the "teach your family" script (the mirror of the
/// weekly letter, see <see cref="LetterStore"/>): a topic freshly past the knowledge-tracing threshold, not one
/// mastered long ago (so the script stays tied to something worth celebrating now, not a stale old topic).</summary>
public sealed class FamilyStore(EduVerseDb db)
{
    /// <summary>Past this many attempts since reaching solid mastery, the milestone is no longer "recent".</summary>
    public const int MaxAttemptsSinceMastery = 8;

    public async Task<MilestoneTopic?> RecentMilestoneAsync(string userId, CancellationToken ct = default)
    {
        var skill = await db.TopicSkills.AsNoTracking()
            .Where(s => s.UserId == userId && s.Attempts >= 2 && s.Attempts <= MaxAttemptsSinceMastery && s.PKnown >= CrossSubjectBridges.KnownThreshold)
            .OrderBy(s => s.Attempts)
            .FirstOrDefaultAsync(ct).ConfigureAwait(false);
        return skill is null ? null : new MilestoneTopic(skill.Area, skill.Level, skill.Topic);
    }
}
