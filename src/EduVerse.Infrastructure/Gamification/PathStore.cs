using EduVerse.Core.Catalog;
using EduVerse.Core.Curriculum;
using EduVerse.Core.Gamification;
using EduVerse.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EduVerse.Infrastructure.Gamification;

public sealed record PathView(Area Area, Level Level, IReadOnlyList<PathNode> Nodes, int CurrentUnit)
{
    public int Done => Nodes.Count(n => n.State == PathNodeState.Done);

    /// <summary>Units whose legendary challenge is won (shown gold with a crown).</summary>
    public IReadOnlySet<string> Legendary { get; init; } = new HashSet<string>();

    /// <summary>A unit whose boss is beaten can be played as a legendary challenge.</summary>
    public bool BossBeaten(string topic) => Nodes.Any(n => n.Topic == topic && n.Kind == PathNodeKind.Boss && n.State == PathNodeState.Done);
}

/// <summary>
/// The learning path of a subject and level. Its state is read from what the student already did (finished Focus
/// lessons, opened chests, beaten unit bosses): nothing is stored twice, and the rules live in <see cref="LearningPath"/>.
/// </summary>
public sealed class PathStore(EduVerseDb db, GamificationStore game)
{
    public async Task<PathView> PathAsync(string userId, Area area, Level level, CancellationToken ct = default)
    {
        if (!Catalog.Available(area, level))
        {
            level = Catalog.Info(area).MinLevel;
        }

        var topics = AreaSubjects.Topics(area, level);
        var attempts = await db.QuizAttempts.AsNoTracking()
            .Where(a => a.UserId == userId && a.Area == area && a.Level == level && a.FinishedAt != null && (a.Mode == QuizMode.Focus || a.Mode == QuizMode.Adventure) && a.Topic != null)
            .Select(a => new { a.Mode, a.Topic, a.Correct, a.Length, a.Won }).ToListAsync(ct).ConfigureAwait(false);
        var prefix = $"chest:{area}:{level}:";
        var chests = (await db.GemEvents.Where(g => g.UserId == userId && g.Reason.StartsWith(prefix)).Select(g => g.Reason).ToListAsync(ct).ConfigureAwait(false))
            .Select(r => r[prefix.Length..]).ToHashSet();
        var nodes = LearningPath.Build(topics, topic => new UnitProgress(
            attempts.Count(a => a.Mode == QuizMode.Focus && a.Topic == topic && LearningPath.LessonPassed(a.Correct, a.Length)),
            chests.Contains(topic),
            attempts.Any(a => a.Mode == QuizMode.Adventure && a.Topic == LearningPath.BossTopic(topic) && a.Won),
            attempts.Any(a => a.Mode == QuizMode.Adventure && a.Topic == LearningPath.LegendTopic(topic) && a.Won)));
        var legendary = topics.Where(t => attempts.Any(a => a.Mode == QuizMode.Adventure && a.Topic == LearningPath.LegendTopic(t) && a.Won)).ToHashSet();
        return new PathView(area, level, nodes, LearningPath.CurrentUnit(nodes)) { Legendary = legendary };
    }

    /// <summary>Lessons of a unit already passed: the stage of the next one (its difficulty).</summary>
    public async Task<int> LessonsPassedAsync(string userId, Area area, Level level, string topic, CancellationToken ct = default) =>
        (await db.QuizAttempts.AsNoTracking()
            .Where(a => a.UserId == userId && a.Area == area && a.Level == level && a.FinishedAt != null && a.Mode == QuizMode.Focus && a.Topic == topic)
            .Select(a => new { a.Correct, a.Length }).ToListAsync(ct).ConfigureAwait(false))
            .Count(a => LearningPath.LessonPassed(a.Correct, a.Length));

    /// <summary>Whether the boss of a unit is open right now (the path is followed in order).</summary>
    public async Task<bool> BossOpenAsync(string userId, Area area, Level level, string topic, CancellationToken ct = default)
    {
        var path = await PathAsync(userId, area, level, ct).ConfigureAwait(false);
        return path.Nodes.Any(n => n.Topic == topic && n.Kind == PathNodeKind.Boss && n.State != PathNodeState.Locked);
    }

    /// <summary>The legendary challenge of a unit opens once its boss is beaten (it can be replayed after a failure).</summary>
    public async Task<bool> LegendOpenAsync(string userId, Area area, Level level, string topic, CancellationToken ct = default)
    {
        var path = await PathAsync(userId, area, level, ct).ConfigureAwait(false);
        return path.BossBeaten(topic) && !path.Legendary.Contains(topic);
    }

    /// <summary>Opens the chest of a unit when it is the open node: its gems (null when it is locked or already open).</summary>
    public async Task<int?> OpenChestAsync(string userId, Area area, Level level, string topic, CancellationToken ct = default)
    {
        var path = await PathAsync(userId, area, level, ct).ConfigureAwait(false);
        if (!path.Nodes.Any(n => n.Topic == topic && n.Kind == PathNodeKind.Chest && n.State == PathNodeState.Open))
        {
            return null;
        }

        var key = LearningPath.ChestKey(path.Area, path.Level, topic);
        var gems = LearningPath.ChestGems(userId, key);
        db.GemEvents.Add(new GemEvent { UserId = userId, Amount = gems, Reason = key, At = game.Now });
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return gems;
    }
}
