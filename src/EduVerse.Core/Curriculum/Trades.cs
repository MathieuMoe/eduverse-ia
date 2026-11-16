using EduVerse.Core.Catalog;
using EduVerse.Core.Common;
using EduVerse.Core.Learning;
using Txt = EduVerse.Core.Catalog.Text;

namespace EduVerse.Core.Curriculum;

/// <summary>A concept of a trade and the school science behind it.</summary>
public sealed record TradeConcept(string Key, Txt Name, Txt Description, Area Science, Txt ScienceLink, IReadOnlyList<string> Prerequisites);

/// <summary>A checked multiple-choice question of a trade (written by hand, not by the AI).</summary>
public sealed record TradeQuestion(string Concept, int Difficulty, Txt Question, Txt Answer, Txt Explanation, IReadOnlyList<Txt> Wrong);

/// <summary>A trade (a job skill) or a hobby (for fun): they are shown in two tabs of Learn.</summary>
public enum TradeKind
{
    Trade,
    Hobby,
}

/// <summary>A practical learning path (a trade or a hobby), with an internal certificate when every concept is mastered.</summary>
public sealed record Trade(string Key, string Icon, string Tone, Txt Name, Txt Summary, Txt? Safety, IReadOnlyList<TradeConcept> Concepts, IReadOnlyList<TradeQuestion> Questions, TradeKind Kind = TradeKind.Trade)
{
    public IReadOnlyList<Area> Sciences => Concepts.Select(c => c.Science).Distinct().ToList();
}

/// <summary>
/// Trades and practical skills (electricity at home, vegetable garden, plumbing, carpentry): built-in subjects of the tutor that work
/// without AI, each concept connected to the science behind it. Theory only: the certificate is internal and is not a professional licence.
/// </summary>
public static partial class Trades
{
    public const string Prefix = "trade-";
    public const int CertificateXp = 100;

    private static TradeConcept C(string key, string name, string description, Area science, string link, params string[] prerequisites) =>
        new(key, Txt.Of(name), Txt.Of(description), science, Txt.Of(link), prerequisites);

    private static TradeQuestion Q(string concept, int difficulty, string question, string answer, string explanation, params string[] wrong) =>
        new(concept, difficulty, Txt.Of(question), Txt.Of(answer), Txt.Of(explanation), wrong.Select(Txt.Of).ToList());

    /// <summary>Every trade and hobby (read through a nested class so the order of the partial files does not matter).</summary>
    public static IReadOnlyList<Trade> All => Registry.List;

    public static IReadOnlyList<Trade> OfKind(TradeKind kind) => All.Where(t => t.Kind == kind).ToList();

    private static class Registry
    {
        public static readonly IReadOnlyList<Trade> List = [.. Core, .. MoreTrades.List, .. MoreTrades2.List, .. HobbyList.List, .. HobbyList2.List];
    }

    public static Trade? Find(string? key) => All.FirstOrDefault(t => t.Key == key);

    /// <summary>The tab of the Learn page for a trade or hobby: "trades" or "hobbies".</summary>
    public static string TabOf(Trade trade) => trade.Kind == TradeKind.Hobby ? "hobbies" : "trades";

    public static bool IsTrade(string? key) => key is not null && key.StartsWith(Prefix, StringComparison.Ordinal) && Find(key) is not null;

    /// <summary>The trade as a subject of the tutor (concept map with prerequisites).</summary>
    public static Subject Subject(Trade trade, TeachLanguage language)
    {
        ArgumentNullException.ThrowIfNull(trade);
        return new Subject(trade.Key, trade.Name.In(language), Txt.Of("Trades and practical skills|Oficios y aprendizaje práctico|Métiers et apprentissage pratique|Oficis i aprenentatge pràctic").In(language),
            trade.Concepts.Select(c => new Concept(c.Key, c.Name.In(language), c.Description.In(language) + " (" + c.ScienceLink.In(language) + ")", c.Prerequisites)).ToList(), true);
    }

    /// <summary>A question of the concept, the closest to the difficulty asked, options shuffled.</summary>
    public static Exercise Exercise(Trade trade, string concept, int difficulty, Random random, TeachLanguage language)
    {
        ArgumentNullException.ThrowIfNull(trade);
        ArgumentNullException.ThrowIfNull(random);
        var pool = trade.Questions.Where(q => q.Concept == concept).ToList();
        if (pool.Count == 0)
        {
            throw new ArgumentException($"No questions for {concept}", nameof(concept));
        }

        var best = pool.Min(q => Math.Abs(q.Difficulty - difficulty));
        var candidates = pool.Where(q => Math.Abs(q.Difficulty - difficulty) <= best + (random.Next(3) == 0 ? 1 : 0)).ToList();
        var q = candidates[random.Next(candidates.Count)];
        var options = q.Wrong.Select(w => w.In(language)).Prepend(q.Answer.In(language)).OrderBy(_ => random.Next()).ToList();
        var c = trade.Concepts.First(x => x.Key == concept);
        return new Exercise(Guid.NewGuid().ToString("N"), concept, q.Difficulty, q.Question.In(language), options, options.IndexOf(q.Answer.In(language)),
            options.Select(_ => (string?)null).ToList(), c.Description.In(language), q.Explanation.In(language) + " " + c.ScienceLink.In(language) + ".");
    }
}
