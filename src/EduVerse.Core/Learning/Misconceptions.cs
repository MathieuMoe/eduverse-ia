using System.Text;
using EduVerse.Core.Common;
using EduVerse.Core.Curriculum;

namespace EduVerse.Core.Learning;

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
