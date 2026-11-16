namespace EduVerse.Core.Catalog;

/// <summary>A checked fact turned into a question; the wrong options are the answers of other facts of the same group.</summary>
internal sealed record Fact(Level Level, string Topic, string Group, Text Question, Text Answer, Text? Note = null, string? Visual = null)
{
    public static Fact Of(Level level, string topic, string group, string question, string answer, string? note = null, string? visual = null) =>
        new(level, topic, group, Text.Of(question), Text.Of(answer), note is null ? null : Text.Of(note), visual);
}

internal static class Facts
{
    /// <summary>Asks a fact of the level (or of the two levels below); higher difficulty prefers the level itself.</summary>
    public static QuizQuestion Ask(Mc q, IReadOnlyList<Fact> facts)
    {
        var near = facts.Where(f => f.Level <= q.Level && f.Level >= q.Level - (q.Difficulty >= 3 ? 0 : 2)).ToList();
        if (near.Count == 0)
        {
            near = facts.Where(f => f.Level <= q.Level).ToList();
        }

        if (near.Count == 0)
        {
            near = facts.OrderBy(f => f.Level).Take(3).ToList();
        }

        var fact = q.Pick(near);
        var answer = fact.Answer.In(q.Language);
        var distractors = facts.Where(f => f.Group == fact.Group).Select(f => f.Answer.In(q.Language)).Where(a => a != answer).Distinct();
        var explanation = fact.Note?.In(q.Language)
            ?? q.F("The right answer is: {0}.|La respuesta correcta es: {0}.|La bonne réponse est : {0}.|La resposta correcta és: {0}.", answer);
        return q.Choice(fact.Topic, fact.Question.In(q.Language), answer, distractors, explanation, fact.Visual);
    }

    public static bool Covers(IReadOnlyList<Fact> facts, Level level) => facts.Any(f => f.Level <= level);
}
