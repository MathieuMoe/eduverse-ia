using EduVerse.Core.Common;
using EduVerse.Core.Tutor;
using EduVerse.Infrastructure.Data;
using EduVerse.Infrastructure.Gamification;
using Microsoft.EntityFrameworkCore;

namespace EduVerse.Infrastructure.Tutor;

/// <summary>One round of "explain it to Evo": the ideas found so far (whole explanation), the first missing idea, the end.</summary>
public sealed record TeachStep(TeachSession Session, TeachConcept Concept, IReadOnlyList<string> Covered, Idea? Missing, bool Done, Reward Reward);

/// <summary>
/// Sessions of "explain it to Evo". The rubric of <see cref="TeachConcepts"/> decides which ideas are present (and the XP);
/// the AI, when there is one, only writes Evo's words.
/// </summary>
public sealed class TeachStore(EduVerseDb db, GamificationStore game, TimeProvider clock)
{
    public DateTimeOffset Now => clock.GetUtcNow();

    public async Task<TeachStep?> ExplainAsync(string userId, string conceptKey, string? sessionId, string text, TeachLanguage language, CancellationToken ct = default)
    {
        var concept = TeachConcepts.Find(conceptKey);
        var clean = (text ?? "").Trim();
        if (concept is null || clean.Length < 2)
        {
            return null;
        }

        clean = clean.Length > TeachConcepts.MaxLength ? clean[..TeachConcepts.MaxLength] : clean;
        var session = sessionId is null ? null
            : await db.TeachSessions.FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId && s.Concept == concept.Key && s.FinishedAt == null, ct).ConfigureAwait(false);
        if (session is null)
        {
            session = new TeachSession { Id = Guid.NewGuid().ToString("N"), UserId = userId, Concept = concept.Key, StartedAt = Now };
            db.TeachSessions.Add(session);
        }

        session.Text = session.Text.Length == 0 ? clean : session.Text + "\n" + clean;
        if (session.Text.Length > TeachConcepts.MaxLength * TeachConcepts.MaxRounds)
        {
            session.Text = session.Text[..(TeachConcepts.MaxLength * TeachConcepts.MaxRounds)];
        }

        session.Rounds++;
        var covered = TeachConcepts.Covered(concept, session.Text, language);
        session.Covered = string.Join(',', covered);
        var missing = concept.Ideas.FirstOrDefault(i => !covered.Contains(i.Key));
        var done = missing is null || session.Rounds >= TeachConcepts.MaxRounds;
        var reward = Reward.None;
        if (done)
        {
            session.FinishedAt = Now;
            session.Xp = TeachConcepts.XpFor(covered.Count, concept.Ideas.Count);
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
            await game.AwardAsync(userId, session.Xp, "teach", covered.Count == concept.Ideas.Count, ct).ConfigureAwait(false);
            reward = new Reward(session.Xp, [], []).Plus(await game.CheckAsync(userId, ct).ConfigureAwait(false));
        }
        else
        {
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        return new TeachStep(session, concept, covered, missing, done, reward);
    }

    /// <summary>Best finished session per concept (ideas found).</summary>
    public async Task<IReadOnlyDictionary<string, int>> BestAsync(string userId, CancellationToken ct = default)
    {
        var sessions = await db.TeachSessions.AsNoTracking().Where(s => s.UserId == userId && s.FinishedAt != null).Select(s => new { s.Concept, s.Covered }).ToListAsync(ct).ConfigureAwait(false);
        return sessions.GroupBy(s => s.Concept).ToDictionary(g => g.Key, g => g.Max(s => s.Covered.Length == 0 ? 0 : s.Covered.Split(',').Length));
    }
}
