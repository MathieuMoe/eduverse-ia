using EduVerse.Core.Catalog;
using EduVerse.Core.Common;
using EduVerse.Core.Tutor;
using EduVerse.Infrastructure.Data;
using EduVerse.Infrastructure.Gamification;
using Microsoft.EntityFrameworkCore;

namespace EduVerse.Infrastructure.Tutor;

/// <summary><see cref="FromTopic"/>: the topic of the student's own tests this problem comes from (null = chosen at random).</summary>
public sealed record PipView(string Id, string Problem, IReadOnlyList<string> Lines, IReadOnlyList<string> Reasons, string? FromTopic = null);

public sealed record PipOutcome(bool LineOk, bool ReasonOk, int WrongStep, int ReasonIndex, string Fix, IReadOnlyList<string> Right, Reward Reward);

/// <summary>Challenges of "Correct Pip". The server rebuilds the problem from the seed to check the answer; nothing about the solution is stored.</summary>
public sealed record PipStat(PipMistake Mistake, int Found, int Total);

public sealed record PipFacts(PipProblem Problem, bool LineOk, bool ReasonOk, Level Level);

public sealed class PipStore(EduVerseDb db, GamificationStore game, TimeProvider clock)
{
    /// <summary>How far back the student's own wrong answers count.</summary>
    public const int OwnDays = 30;

    public async Task<PipView?> StartAsync(string userId, Level level, TeachLanguage language, PipSubject subject = PipSubject.Maths, CancellationToken ct = default)
    {
        if (!Pip.Available(level))
        {
            return null;
        }

        var (forced, from) = subject == PipSubject.Maths ? await OwnMistakeAsync(userId, level, ct).ConfigureAwait(false) : (null, null);
        var challenge = new PipChallenge
        {
            Id = Guid.NewGuid().ToString("N"), UserId = userId, Level = level, Language = language, Subject = subject, Forced = forced,
            Seed = Random.Shared.Next(1, int.MaxValue), At = clock.GetUtcNow(),
        };
        db.PipChallenges.Add(challenge);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        var p = Pip.Generate(level, challenge.Seed, language, subject, forced);
        return new PipView(challenge.Id, p.Problem, p.Shown, p.Reasons, from);
    }

    /// <summary>
    /// "Pip makes your mistakes": two times out of three, Pip stumbles on a topic the student got wrong lately in their own tests
    /// (the most failed first), or on a kind of mistake they did not spot last time. Otherwise the seed decides.
    /// </summary>
    private async Task<(PipMistake? Kind, string? Topic)> OwnMistakeAsync(string userId, Level level, CancellationToken ct)
    {
        if (await db.PipChallenges.CountAsync(c => c.UserId == userId, ct).ConfigureAwait(false) % 3 == 2)
        {
            return (null, null);
        }

        var allowed = Pip.Kinds(level);
        var since = clock.GetUtcNow().AddDays(-OwnDays);
        var wrong = await db.QuizAnswers.AsNoTracking().Where(a => a.UserId == userId && !a.Correct && a.At >= since).Select(a => a.Topic).ToListAsync(ct).ConfigureAwait(false);
        var own = wrong.GroupBy(t => t).OrderByDescending(g => g.Count()).ThenBy(g => g.Key, StringComparer.Ordinal)
            .Select(g => (Kind: Pip.ForTopic(g.Key), Topic: g.Key)).FirstOrDefault(x => x.Kind is { } k && allowed.Contains(k));
        if (own.Kind is not null)
        {
            return (own.Kind, own.Topic);
        }

        var missed = await db.PipChallenges.AsNoTracking().Where(c => c.UserId == userId && c.Done && c.Subject == PipSubject.Maths && !(c.FoundLine && c.FoundReason))
            .OrderByDescending(c => c.At).FirstOrDefaultAsync(ct).ConfigureAwait(false);
        var again = missed is null ? (PipMistake?)null : Pip.Generate(missed.Level, missed.Seed, missed.Language, missed.Subject, missed.Forced).Mistake;
        return (again is { } m && allowed.Contains(m) ? again : null, null);
    }

    /// <summary>Checks the line and the reason once; null when it does not exist, is not theirs or was already answered.</summary>
    public async Task<PipOutcome?> AnswerAsync(string userId, string id, int line, int reason, CancellationToken ct = default)
    {
        var c = await db.PipChallenges.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId && !x.Done, ct).ConfigureAwait(false);
        if (c is null)
        {
            return null;
        }

        var p = Pip.Generate(c.Level, c.Seed, c.Language, c.Subject, c.Forced);
        (c.Done, c.FoundLine, c.FoundReason) = (true, line == p.WrongStep, reason == p.ReasonIndex);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        var reward = new Reward(0, [], []);
        if (c.FoundLine && c.FoundReason)
        {
            var since = clock.GetUtcNow().AddDays(-1);
            if (await db.PipChallenges.CountAsync(x => x.UserId == userId && x.FoundLine && x.FoundReason && x.At >= since, ct).ConfigureAwait(false) <= Pip.RewardedPerDay)
            {
                await game.AwardAsync(userId, Pip.Xp, "pip", false, ct).ConfigureAwait(false);
                reward = new Reward(Pip.Xp, [], []);
            }
        }

        return new PipOutcome(c.FoundLine, c.FoundReason, p.WrongStep, p.ReasonIndex, p.Fix, p.Right, reward.Plus(await game.CheckAsync(userId, ct).ConfigureAwait(false)));
    }

    /// <summary>Challenges the student solved completely (line and reason).</summary>
    public async Task<int> SolvedAsync(string userId, CancellationToken ct = default) =>
        await db.PipChallenges.CountAsync(x => x.UserId == userId && x.FoundLine && x.FoundReason, ct).ConfigureAwait(false);

    /// <summary>The facts of an answered challenge, for Pip's reply (null if it is not theirs or not answered yet).</summary>
    public async Task<PipFacts?> FactsAsync(string userId, string id, CancellationToken ct = default)
    {
        var c = await db.PipChallenges.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId && x.Done, ct).ConfigureAwait(false);
        return c is null ? null : new PipFacts(Pip.Generate(c.Level, c.Seed, c.Language, c.Subject, c.Forced), c.FoundLine, c.FoundReason, c.Level);
    }

    /// <summary>How often the student finds each kind of mistake (the last answered challenges). The kind is rebuilt from the seed, never stored.</summary>
    public async Task<IReadOnlyList<PipStat>> StatsAsync(string userId, CancellationToken ct = default)
    {
        var done = await db.PipChallenges.AsNoTracking().Where(x => x.UserId == userId && x.Done).OrderByDescending(x => x.At).Take(60).ToListAsync(ct).ConfigureAwait(false);
        return done.GroupBy(c => Pip.Generate(c.Level, c.Seed, c.Language, c.Subject, c.Forced).Mistake)
            .Select(g => new PipStat(g.Key, g.Count(c => c.FoundLine && c.FoundReason), g.Count())).OrderBy(s => s.Mistake).ToList();
    }
}
