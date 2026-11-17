using System.Text;
using EduVerse.Core.Common;
using EduVerse.Core.Curriculum;

namespace EduVerse.Core.Learning;

public enum MisconceptionStatus
{
    /// <summary>Recurring, and either never rechecked since it last happened, or rechecked and still happening.</summary>
    Watching,

    /// <summary>A camouflaged recheck, after the mistake's last occurrence, confirmed it is gone — not just absent from today's lucky streak.</summary>
    Overcome,
}

/// <summary>
/// One of the student's own recurring mistakes, and the real memory of it: not the raw count, but whether a later,
/// camouflaged recheck (weeks after, mixed into ordinary practice, never labelled as a test) confirms it is truly gone.
/// </summary>
public sealed record MisconceptionMemory(string Code, int Occurrences, DateTimeOffset LastOccurrence, DateTimeOffset? LastCheckedAt, bool? LastCheckCorrect)
{
    public MisconceptionStatus Status => LastCheckedAt is { } checkedAt && checkedAt > LastOccurrence && LastCheckCorrect == true
        ? MisconceptionStatus.Overcome
        : MisconceptionStatus.Watching;
}

/// <summary>Human-readable text for a misconception code, for the student's feedback and the teacher's report.</summary>
public static class Misconceptions
{
    /// <summary>
    /// Fractions codes have a written warning in 4 languages; codes invented by the AI for other subjects ("AddsDenominators")
    /// are turned into words ("Adds denominators").
    /// </summary>
    public static string Describe(string code, string subjectKey, TeachLanguage language)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return "";
        }

        if (subjectKey == BuiltInSubjects.FractionsKey)
        {
            var warning = DemoLearningAi.Warning(code, language);
            if (warning != code)
            {
                return char.ToUpperInvariant(warning[0]) + warning[1..];
            }
        }

        return Humanize(code);
    }

    public static string Humanize(string code)
    {
        ArgumentNullException.ThrowIfNull(code);
        var b = new StringBuilder();
        foreach (var c in code.Trim())
        {
            if (char.IsUpper(c) && b.Length > 0 && b[^1] != ' ')
            {
                b.Append(' ').Append(char.ToLowerInvariant(c));
            }
            else
            {
                b.Append(c is '_' or '-' ? ' ' : c);
            }
        }

        var text = b.ToString().Trim();
        return text.Length == 0 ? text : char.ToUpperInvariant(text[0]) + text[1..];
    }
}
