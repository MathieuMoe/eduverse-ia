using EduVerse.Core.Catalog;
using EduVerse.Core.Tutor;
using EduVerse.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EduVerse.Infrastructure.Tutor;

/// <summary>A weak topic with the step below it and what the detective concludes.</summary>
public sealed record DetectiveCase(Area Area, Level Level, string Topic, int Percent, TopicRef Below, TopicEvidence? BelowEvidence, RootVerdict Verdict);

/// <summary>A topic where the student was sure and wrong several times.</summary>
public sealed record FalseCertainty(Area Area, Level Level, string Topic, int SureWrong, int Sure);

/// <summary>Reads the student's answers to find the root of a weak topic and the topics they wrongly believe they master.</summary>
public sealed class DetectiveStore(EduVerseDb db)
{
    public const int Recent = 300;

    public const int MaxCases = 3;

    public const int MinSureWrong = 2;

    private async Task<List<(Area Area, Level Level, string Topic, bool Correct, int Confidence)>> RecentAsync(string userId, CancellationToken ct) =>
        (await db.QuizAnswers.AsNoTracking().Where(a => a.UserId == userId).OrderByDescending(a => a.Id).Take(Recent)
            .Select(a => new { a.Area, a.Level, a.Topic, a.Correct, a.Confidence }).ToListAsync(ct).ConfigureAwait(false))
        .Select(a => (a.Area, a.Level, a.Topic, a.Correct, a.Confidence)).ToList();

    /// <summary>The weak topics that have a step below, weakest first.</summary>
    public async Task<IReadOnlyList<DetectiveCase>> CasesAsync(string userId, CancellationToken ct = default)
    {
        var answers = await RecentAsync(userId, ct).ConfigureAwait(false);
        var evidence = answers.GroupBy(a => (a.Area, a.Level, a.Topic)).ToDictionary(g => g.Key, g => new TopicEvidence(g.Count(), g.Count(a => a.Correct)));
        var cases = new List<DetectiveCase>();
        foreach (var (key, e) in evidence.Where(p => Prerequisites.Weak(p.Value)).OrderBy(p => p.Value.Correct / (double)p.Value.Answers))
        {
            if (Prerequisites.Of(key.Area, key.Level, key.Topic) is not { } below)
            {
                continue;
            }

            var belowEvidence = evidence.GetValueOrDefault((key.Area, below.Level, below.Topic));
            cases.Add(new DetectiveCase(key.Area, key.Level, key.Topic, 100 * e.Correct / e.Answers, below, belowEvidence, Prerequisites.Verdict(belowEvidence)));
            if (cases.Count == MaxCases)
            {
                break;
            }
        }

        return cases;
    }

    /// <summary>The answers where the student said "sure" or "not sure" (not "I think so"), oldest first, with the topic as a key.</summary>
    public async Task<IReadOnlyList<Bet>> BetsAsync(string userId, CancellationToken ct = default) =>
        (await RecentAsync(userId, ct).ConfigureAwait(false)).Where(a => a.Confidence != 1 && !a.Topic.Contains(':', StringComparison.Ordinal))
            .Reverse().Select(a => new Bet(a.Topic, a.Confidence >= 2, a.Correct)).ToList();

    /// <summary>Topics answered "sure" and wrong at least twice, and wrong at least half of the times they were sure.</summary>
    public async Task<IReadOnlyList<FalseCertainty>> FalseCertaintiesAsync(string userId, CancellationToken ct = default) =>
        (await RecentAsync(userId, ct).ConfigureAwait(false)).Where(a => a.Confidence >= 2 && !a.Topic.Contains(':', StringComparison.Ordinal))
            .GroupBy(a => (a.Area, a.Level, a.Topic))
            .Select(g => new FalseCertainty(g.Key.Area, g.Key.Level, g.Key.Topic, g.Count(a => !a.Correct), g.Count()))
            .Where(f => f.SureWrong >= MinSureWrong && 2 * f.SureWrong >= f.Sure)
            .OrderByDescending(f => f.SureWrong).Take(MaxCases).ToList();
}
