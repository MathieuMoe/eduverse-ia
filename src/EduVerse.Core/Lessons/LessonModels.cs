using EduVerse.Core.Common;
using EduVerse.Core.Text;

namespace EduVerse.Core.Lessons;

public enum LessonLevel
{
    Basic,
    Standard,
    Advanced,
}

public sealed record LessonRequest(string Title, string Subject, Stage Stage, string Text, TeachLanguage Language, int Questions = 4)
{
    public const int MinTextLength = 150;
    public const int MaxTextLength = 15000;
    public const int MaxTitleLength = 150;
}

public sealed record GlossaryEntry(string Term, string Definition);

public sealed record QuestionAnswer(string Question, string Answer);

/// <summary>One version of the lesson for a level, with its measured readability.</summary>
public sealed record LevelVersion(
    LessonLevel Level,
    string Text,
    IReadOnlyList<GlossaryEntry> Glossary,
    IReadOnlyList<QuestionAnswer> Questions,
    string Activity,
    TextStats Stats);

/// <summary>The same lesson in 3 levels for a mixed-ability class, plus objectives and tips for the teacher.</summary>
public sealed record LessonResult(
    IReadOnlyList<string> Objectives,
    IReadOnlyList<LevelVersion> Versions,
    IReadOnlyList<string> TeacherTips,
    TextStats Original,
    IReadOnlyList<string> Warnings);

public interface ILessonAdapter
{
    Task<LessonResult> AdaptAsync(LessonRequest request, CancellationToken cancellationToken = default);
}

public static class LessonValidator
{
    public static IReadOnlyList<string> Check(LessonRequest? r)
    {
        if (r is null)
        {
            return ["invalid"];
        }

        var errors = new List<string>();
        var length = (r.Text ?? "").Trim().Length;
        if (length < LessonRequest.MinTextLength)
        {
            errors.Add("text_too_short");
        }
        else if (length > LessonRequest.MaxTextLength)
        {
            errors.Add("text_too_long");
        }

        if ((r.Title ?? "").Length > LessonRequest.MaxTitleLength)
        {
            errors.Add("title_too_long");
        }

        if (r.Questions is < 2 or > 8)
        {
            errors.Add("questions_out_of_range");
        }

        return errors;
    }

    /// <summary>
    /// The levels must really differ: the basic text must read easier than the standard one, which must read easier
    /// than or as easily as the advanced one. Measured in code; returns warning codes for the interface.
    /// </summary>
    public static IReadOnlyList<string> CheckLevels(IReadOnlyList<LevelVersion> versions)
    {
        ArgumentNullException.ThrowIfNull(versions);
        var warnings = new List<string>();
        var basic = versions.FirstOrDefault(v => v.Level == LessonLevel.Basic);
        var standard = versions.FirstOrDefault(v => v.Level == LessonLevel.Standard);
        var advanced = versions.FirstOrDefault(v => v.Level == LessonLevel.Advanced);
        if (basic is null || standard is null || advanced is null)
        {
            return ["missing_level"];
        }

        if (basic.Stats.Score < standard.Stats.Score + 3 && basic.Stats.WordsPerSentence >= standard.Stats.WordsPerSentence)
        {
            warnings.Add("basic_not_easier");
        }

        if (advanced.Stats.Words < standard.Stats.Words * 0.8)
        {
            warnings.Add("advanced_shorter");
        }

        return warnings;
    }
}
