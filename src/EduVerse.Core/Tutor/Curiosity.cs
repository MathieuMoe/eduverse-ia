using EduVerse.Core.Ideas;

namespace EduVerse.Core.Tutor;

/// <summary>
/// "What I wonder about": a student's own question, kept for later instead of answered on the spot or lost. The same text rules as
/// the students' ideas (<see cref="IdeaRules"/>) — short, no links or rude words — but a question can run a little longer than a
/// one-line idea, so it gets its own limits.
/// </summary>
public static class Curiosity
{
    public const int MinLength = 6;
    public const int MaxLength = 160;

    /// <summary>New questions a student can add per day.</summary>
    public const int DailyLimit = 3;

    /// <summary>Open questions a student can have waiting at once (after this, they must explore or dismiss one first).</summary>
    public const int MaxOpen = 20;

    /// <summary>How many open questions the dashboard widget shows at a time.</summary>
    public const int MaxShown = 3;

    public static IdeaCheck Check(string? text)
    {
        var t = (text ?? "").Trim();
        if (IdeaRules.Normalize(t).Length < MinLength)
        {
            return IdeaCheck.TooShort;
        }

        if (t.Length > MaxLength)
        {
            return IdeaCheck.TooLong;
        }

        return IdeaRules.Safe(t) ? IdeaCheck.Ok : IdeaCheck.NotAllowed;
    }

    public static string Clean(string? text) => IdeaRules.Clean(text);
}
