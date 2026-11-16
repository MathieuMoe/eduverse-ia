using EduVerse.Core.Curriculum;
using EduVerse.Core.Gamification;
using EduVerse.Core.Learning;
using EduVerse.Infrastructure.Data;
using EduVerse.Infrastructure.Gamification;
using Microsoft.EntityFrameworkCore;

namespace EduVerse.Infrastructure.Learning;

/// <summary>Progress of a trade: concepts mastered and the certificate, if earned.</summary>
public sealed record TradeProgress(Trade Trade, int Mastered, int Total, Certificate? Certificate);

/// <summary>
/// Internal certificates of the trades: issued by the code when every concept of a trade is mastered (knowledge tracing),
/// with a short code, XP and badges. They prove theory learned on the platform, not a professional qualification.
/// </summary>
public sealed class CertificateStore(EduVerseDb db, LearningStore learning, GamificationStore game, TimeProvider clock)
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public async Task<Certificate?> FindAsync(string userId, string subjectKey, CancellationToken ct = default) =>
        await db.Certificates.AsNoTracking().FirstOrDefaultAsync(c => c.UserId == userId && c.SubjectKey == subjectKey, ct).ConfigureAwait(false);

    public async Task<Certificate?> ByCodeAsync(string code, CancellationToken ct = default) =>
        await db.Certificates.AsNoTracking().FirstOrDefaultAsync(c => c.Code == code, ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<TradeProgress>> ProgressAsync(string userId, CancellationToken ct = default)
    {
        var certificates = await db.Certificates.AsNoTracking().Where(c => c.UserId == userId).ToDictionaryAsync(c => c.SubjectKey, ct).ConfigureAwait(false);
        var list = new List<TradeProgress>();
        foreach (var trade in Trades.All)
        {
            var states = await learning.StatesAsync(userId, trade.Key, ct).ConfigureAwait(false);
            list.Add(new TradeProgress(trade, trade.Concepts.Count(c => states.TryGetValue(c.Key, out var s) && s.Status == MasteryStatus.Mastered),
                trade.Concepts.Count, certificates.GetValueOrDefault(trade.Key)));
        }

        return list;
    }

    /// <summary>Issues the certificate when every concept of the trade is mastered; returns it and the reward only the first time.</summary>
    public async Task<(Certificate Certificate, Reward Reward)?> IssueIfEarnedAsync(string userId, string subjectKey, CancellationToken ct = default)
    {
        var trade = Trades.Find(subjectKey);
        if (trade is null || await db.Certificates.AnyAsync(c => c.UserId == userId && c.SubjectKey == subjectKey, ct).ConfigureAwait(false))
        {
            return null;
        }

        var states = await learning.StatesAsync(userId, subjectKey, ct).ConfigureAwait(false);
        if (!trade.Concepts.All(c => states.TryGetValue(c.Key, out var s) && s.Status == MasteryStatus.Mastered))
        {
            return null;
        }

        string code;
        do
        {
            code = "EV-" + string.Concat(Enumerable.Range(0, 8).Select(_ => Alphabet[System.Security.Cryptography.RandomNumberGenerator.GetInt32(Alphabet.Length)]));
        }
        while (await db.Certificates.AnyAsync(c => c.Code == code, ct).ConfigureAwait(false));

        var certificate = new Certificate { UserId = userId, SubjectKey = subjectKey, Code = code, IssuedAt = clock.GetUtcNow() };
        db.Certificates.Add(certificate);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        await game.AwardAsync(userId, Trades.CertificateXp, "certificate", true, ct).ConfigureAwait(false);
        var reward = new Reward(Trades.CertificateXp, [], []).Plus(await game.CheckAsync(userId, ct).ConfigureAwait(false));
        return (certificate, reward);
    }
}
