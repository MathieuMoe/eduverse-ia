using System.Globalization;
using EduVerse.Core.Common;

namespace EduVerse.Core.Catalog;

/// <summary>Level of study, from early years (5–6 years old) to a doctorate.</summary>
public enum Level
{
    EarlyYears,
    Primary1,
    Primary2,
    Primary3,
    Secondary1,
    Secondary2,
    HighSchool,
    Bachelor,
    Master,
    Doctorate,
}

/// <summary>Subjects of the catalog of tests.</summary>
public enum Area
{
    Mathematics,
    Physics,
    Chemistry,
    Biology,
    Language,
    ForeignLanguage,
    History,
    Geography,
    Economics,
    Programming,
}

/// <summary>A text in the 4 languages of the platform.</summary>
public sealed record Text(string En, string Es, string Fr, string Ca)
{
    /// <summary>"english|spanish|french|catalan", or one value for all the languages.</summary>
    public static Text Of(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var p = value.Split('|');
        return p.Length switch
        {
            1 => new(p[0], p[0], p[0], p[0]),
            4 => new(p[0], p[1], p[2], p[3]),
            _ => throw new ArgumentException($"Expected 1 or 4 values: {value}", nameof(value)),
        };
    }

    public string In(TeachLanguage language) => Lang.T(language, En, Es, Fr, Ca);

    public string Format(TeachLanguage language, params object[] args) =>
        string.Format(Levels.Culture(language), In(language), args);
}

/// <summary>
/// One multiple-choice question of a test. <see cref="Visual"/> holds emojis for young children, <see cref="Code"/> a code snippet,
/// <see cref="Evo"/> the answer of the mascot when the student has to check it (and <see cref="EvoTask"/> what to do with it).
/// </summary>
public sealed record QuizQuestion(
    string Topic,
    int Difficulty,
    string Prompt,
    IReadOnlyList<string> Options,
    int CorrectIndex,
    string Explanation,
    string? Visual = null,
    string? Code = null,
    string? Evo = null,
    string? EvoTask = null)
{
    public const int Choices = 4;

    public string Answer => Options[CorrectIndex];
}

public sealed record AreaInfo(Area Area, Level MinLevel, string Icon, string Tone);

public static class Catalog
{
    public static readonly IReadOnlyList<AreaInfo> Areas =
    [
        new(Area.Mathematics, Level.EarlyYears, "➗", "lavender"),
        new(Area.Language, Level.EarlyYears, "📖", "pink"),
        new(Area.ForeignLanguage, Level.EarlyYears, "💬", "sky"),
        new(Area.Biology, Level.Primary1, "🌱", "mint"),
        new(Area.Geography, Level.Primary1, "🌍", "sky"),
        new(Area.History, Level.Primary2, "🏛️", "peach"),
        new(Area.Programming, Level.Primary2, "💻", "lavender"),
        new(Area.Physics, Level.Primary3, "🧲", "peach"),
        new(Area.Chemistry, Level.Primary3, "⚗️", "mint"),
        new(Area.Economics, Level.Secondary2, "📈", "pink"),
    ];

    public static AreaInfo Info(Area area) => Areas.First(a => a.Area == area);

    public static bool Available(Area area, Level level) => level >= Info(area).MinLevel;

    public static IReadOnlyList<Level> LevelsOf(Area area) => Enum.GetValues<Level>().Where(l => Available(area, l)).ToList();

    public static bool TryParse(string? value, out Area area) =>
        Enum.TryParse(value, ignoreCase: true, out area) && Enum.IsDefined(area);
}

public static class Levels
{
    /// <summary>Young children: big buttons, pictures, the question read aloud.</summary>
    public static bool IsKids(Level level) => level <= Level.Primary1;

    public static string Ages(Level level) => level switch
    {
        Level.EarlyYears => "5–6",
        Level.Primary1 => "6–8",
        Level.Primary2 => "8–10",
        Level.Primary3 => "10–12",
        Level.Secondary1 => "12–14",
        Level.Secondary2 => "14–16",
        Level.HighSchool => "16–18",
        _ => "18+",
    };

    /// <summary>The broader stage used by the tutor and the AI prompts.</summary>
    public static Stage ToStage(Level level) => level switch
    {
        <= Level.Primary3 => Stage.Primary,
        <= Level.Secondary2 => Stage.Secondary,
        Level.HighSchool => Stage.HighSchool,
        _ => Stage.University,
    };

    public static Level FromStage(Stage stage) => stage switch
    {
        Stage.Primary => Level.Primary3,
        Stage.Secondary => Level.Secondary1,
        Stage.HighSchool => Level.HighSchool,
        _ => Level.Bachelor,
    };

    /// <summary>Description of the level for the AI (in English).</summary>
    public static string PromptName(Level level) => level switch
    {
        Level.EarlyYears => "early years, children of 5-6 years old who are learning to read (very short sentences, simple words)",
        Level.Primary1 => "first years of primary school (6-8 years old)",
        Level.Primary2 => "middle of primary school (8-10 years old)",
        Level.Primary3 => "end of primary school (10-12 years old)",
        Level.Secondary1 => "lower secondary school (12-14 years old)",
        Level.Secondary2 => "upper secondary school (14-16 years old)",
        Level.HighSchool => "high school / pre-university (16-18 years old)",
        Level.Bachelor => "undergraduate university level",
        Level.Master => "master's degree level",
        _ => "doctoral (PhD) level: research-grade, precise terminology",
    };

    public static CultureInfo Culture(TeachLanguage language) => CultureInfo.GetCultureInfo(language switch
    {
        TeachLanguage.Spanish => "es-ES",
        TeachLanguage.French => "fr-FR",
        TeachLanguage.Catalan => "ca-ES",
        _ => "en-GB",
    });
}
