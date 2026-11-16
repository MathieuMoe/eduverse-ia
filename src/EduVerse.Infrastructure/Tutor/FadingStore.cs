using EduVerse.Core.Catalog;
using EduVerse.Core.Common;
using EduVerse.Core.Tutor;
using EduVerse.Infrastructure.Data;
using EduVerse.Infrastructure.Gamification;
using Microsoft.EntityFrameworkCore;

namespace EduVerse.Infrastructure.Tutor;

public sealed record FadingView(string Id, FadingKind Kind, int Stage, string Problem, IReadOnlyList<FadingLine> Lines, int Blanks, bool Done);

/// <summary>The result of a try: whether it was right, the worked solution of that problem, what comes next and the reward.</summary>
public sealed record FadingOutcome(bool Correct, IReadOnlyList<FadingStep> Solution, FadingView Next, Reward Reward);

/// <summary>Runs of fading examples. The server rebuilds each problem from the seed, the stage and the tries; no solution is stored.</summary>
public sealed class FadingStore(EduVerseDb db, GamificationStore game, TimeProvider clock)
{
    public async Task<FadingView?> StartAsync(string userId, Level level, FadingKind kind, TeachLanguage language, CancellationToken ct = default)
    {
        if (!Fading.Available(level) || !Fading.Kinds(level).Contains(kind))
        {
            return null;
        }

        var run = new FadingRun { Id = Guid.NewGuid().ToString("N"), UserId = userId, Level = level, Language = language, Kind = kind, Seed = Random.Shared.Next(1, 1_000_000), At = clock.GetUtcNow() };
        db.FadingRuns.Add(run);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return View(run);
    }

    /// <summary>Stage 0 has nothing to answer: "I have read it" moves on. Later stages check the results typed.</summary>
    public async Task<FadingOutcome?> AnswerAsync(string userId, string id, IReadOnlyList<string?>? answers, CancellationToken ct = default)
    {
        var run = await db.FadingRuns.FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId && !r.Done, ct).ConfigureAwait(false);
        if (run is null)
        {
            return null;
        }

        var problem = Fading.Generate(run.Kind, run.Seed, run.Stage, run.Tries, run.Language);
        var correct = run.Stage == 0 || Fading.Check(problem, run.Stage, answers);
        var reward = new Reward(0, [], []);
        if (!correct)
        {
            run.Tries++;
        }
        else if (run.Stage >= Fading.Stages - 1)
        {
            run.Done = true;
            var since = clock.GetUtcNow().AddDays(-1);
            if (await db.FadingRuns.CountAsync(r => r.UserId == userId && r.Done && r.At >= since, ct).ConfigureAwait(false) < Fading.RewardedPerDay)
            {
                await game.AwardAsync(userId, Fading.Xp, "fading", false, ct).ConfigureAwait(false);
                reward = new Reward(Fading.Xp, [], []);
            }
        }
        else
        {
            run.Stage++;
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return new FadingOutcome(correct, problem.Steps, View(run), run.Done ? reward.Plus(await game.CheckAsync(userId, ct).ConfigureAwait(false)) : reward);
    }

    /// <summary>Runs finished (the student ended alone).</summary>
    public async Task<int> DoneAsync(string userId, CancellationToken ct = default) =>
        await db.FadingRuns.CountAsync(r => r.UserId == userId && r.Done, ct).ConfigureAwait(false);

    private static FadingView View(FadingRun run)
    {
        var problem = Fading.Generate(run.Kind, run.Seed, run.Stage, run.Tries, run.Language);
        return new FadingView(run.Id, run.Kind, run.Stage, problem.Problem, run.Done ? [] : Fading.Lines(problem, run.Stage), run.Done ? 0 : Fading.Blanks(problem, run.Stage), run.Done);
    }
}
