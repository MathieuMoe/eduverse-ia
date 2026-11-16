namespace EduVerse.Core.Common;

public enum TeachLanguage
{
    English,
    Spanish,
    French,
    Catalan,
}

/// <summary>School stage: guides the vocabulary and the length of the texts.</summary>
public enum Stage
{
    Primary,
    Secondary,
    HighSchool,
    University,
    Adults,
    ExamPrep,
}

public static class Lang
{
    public static string T(TeachLanguage language, string en, string es, string fr, string ca) => language switch
    {
        TeachLanguage.Spanish => es,
        TeachLanguage.French => fr,
        TeachLanguage.Catalan => ca,
        _ => en,
    };

    public static string Name(TeachLanguage language) => language switch
    {
        TeachLanguage.Spanish => "Spanish (Spain)",
        TeachLanguage.French => "French",
        TeachLanguage.Catalan => "Catalan",
        _ => "English",
    };

    public static string StageName(Stage stage) => stage switch
    {
        Stage.Primary => "primary school (6-12 years old)",
        Stage.Secondary => "secondary school (12-16 years old)",
        Stage.HighSchool => "upper secondary / high school (16-18 years old)",
        Stage.University => "university students",
        Stage.Adults => "adult learners",
        _ => "adults preparing a competitive public exam",
    };
}
