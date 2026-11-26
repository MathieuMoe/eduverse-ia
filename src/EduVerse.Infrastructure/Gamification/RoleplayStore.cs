using System.Text.Json;
using EduVerse.Core.Common;
using EduVerse.Core.Gamification;
using EduVerse.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EduVerse.Infrastructure.Gamification;

public sealed record RoleplayLine(bool Student, string Text);

public sealed record RoleplayView(string Id, Roleplay Roleplay, TeachLanguage Speaks, IReadOnlyList<RoleplayLine> History, IReadOnlySet<int> Met, int Turns, bool Finished, bool Completed)
{
    public int TurnsLeft => Math.Max(0, Roleplays.MaxTurns - Turns);
}

/// <summary>
/// Roleplay conversations (phase 6). The store keeps the dialogue and ticks the goals with <see cref="Roleplays"/> (code);
/// the character's next line is written by the caller (the AI, or the scenario's script offline).
/// </summary>
public sealed class RoleplayStore(EduVerseDb db, GamificationStore game)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static RoleplayView View(RoleplaySession s, Roleplay r) => new(s.Id, r, s.Speaks,
        JsonSerializer.Deserialize<List<RoleplayLine>>(s.HistoryJson, JsonOptions) ?? [],
        (JsonSerializer.Deserialize<List<int>>(s.MetJson, JsonOptions) ?? []).ToHashSet(), s.Turns, s.FinishedAt is not null, s.Completed);

    public async Task<RoleplayView?> StartAsync(string userId, string key, TeachLanguage interfaceLanguage, CancellationToken ct = default)
    {
        if (Roleplays.Find(key) is not { } roleplay)
        {
            return null;
        }

        var speaks = Roleplays.SpeaksIn(roleplay, interfaceLanguage);
        var session = new RoleplaySession
        {
            Id = Guid.NewGuid().ToString("N"), UserId = userId, Scenario = key, Speaks = speaks, StartedAt = game.Now,
            HistoryJson = JsonSerializer.Serialize(new List<RoleplayLine> { new(false, roleplay.Opening.In(speaks)) }, JsonOptions),
        };
        db.RoleplaySessions.Add(session);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return View(session, roleplay);
    }

    public async Task<RoleplayView?> FindAsync(string userId, string id, CancellationToken ct = default)
    {
        var session = await db.RoleplaySessions.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId, ct).ConfigureAwait(false);
        return session is not null && Roleplays.Find(session.Scenario) is { } r ? View(session, r) : null;
    }

    /// <summary>
    /// One turn: the student's message ticks goals (code), then <paramref name="character"/> writes the reply from the
    /// dialogue and the goals still open. Completing every goal ends the roleplay with XP and gems; so does running out of turns (without them).
    /// </summary>
    public async Task<(RoleplayView View, Reward Reward)?> TurnAsync(string userId, string id, string message,
        Func<RoleplayView, IReadOnlyList<string>, Task<string>> character, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(character);
        var text = (message ?? "").Trim();
        var session = await db.RoleplaySessions.FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId, ct).ConfigureAwait(false);
        if (session is null || session.FinishedAt is not null || text.Length == 0 || Roleplays.Find(session.Scenario) is not { } roleplay)
        {
            return null;
        }

        text = text[..Math.Min(text.Length, Roleplays.MaxMessage)];
        var before = View(session, roleplay);
        var met = Roleplays.Progress(roleplay, before.Met, text);
        var history = before.History.Append(new RoleplayLine(true, text)).ToList();
        var during = before with { History = history, Met = met, Turns = before.Turns + 1 };
        var left = roleplay.Goals.Select((g, i) => (g, i)).Where(x => !met.Contains(x.i)).Select(x => x.g.Label.In(session.Speaks)).ToList();
        history.Add(new RoleplayLine(false, await character(during, left).ConfigureAwait(false)));

        session.HistoryJson = JsonSerializer.Serialize(history, JsonOptions);
        session.MetJson = JsonSerializer.Serialize(met.Order().ToList(), JsonOptions);
        session.Turns++;
        var reward = Reward.None;
        if (Roleplays.Complete(roleplay, met))
        {
            (session.Completed, session.FinishedAt) = (true, game.Now);
            await game.AwardAsync(userId, Roleplays.Xp, "roleplay", ct: ct).ConfigureAwait(false);
            db.GemEvents.Add(new GemEvent { UserId = userId, Amount = Roleplays.GemsReward, Reason = "roleplay:" + roleplay.Key, At = game.Now });
            reward = new Reward(Roleplays.Xp, [], []) { Gems = Roleplays.GemsReward };
        }
        else if (session.Turns >= Roleplays.MaxTurns)
        {
            session.FinishedAt = game.Now;
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        reward = reward.Plus(await game.CheckAsync(userId, ct).ConfigureAwait(false));
        return (View(session, roleplay), reward);
    }

    /// <summary>The scenarios a student has completed at least once.</summary>
    public async Task<IReadOnlySet<string>> CompletedAsync(string userId, CancellationToken ct = default) =>
        (await db.RoleplaySessions.Where(s => s.UserId == userId && s.Completed).Select(s => s.Scenario).Distinct().ToListAsync(ct).ConfigureAwait(false)).ToHashSet();
}
