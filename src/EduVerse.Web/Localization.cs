using System.Globalization;
using EduVerse.Core.Common;

namespace EduVerse.Web;

public static class Localization
{
    /// <summary>Interface languages, with their name written in that language (for the switcher).</summary>
    public static readonly IReadOnlyList<(string Culture, string Name)> Cultures =
    [
        ("en", "English"),
        ("fr", "Français"),
        ("es", "Español"),
        ("ca", "Català"),
    ];

    public static readonly string[] SupportedCultures = [.. Cultures.Select(c => c.Culture)];

    public static string CurrentCulture => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

    /// <summary>Language of the generated texts = interface language.</summary>
    public static TeachLanguage LanguageForCurrentCulture() => LanguageFor(CurrentCulture);

    public static TeachLanguage LanguageFor(string culture) => culture switch
    {
        "fr" => TeachLanguage.French,
        "es" => TeachLanguage.Spanish,
        "ca" => TeachLanguage.Catalan,
        _ => TeachLanguage.English,
    };

    public static string CultureFor(TeachLanguage language) => language switch
    {
        TeachLanguage.French => "fr",
        TeachLanguage.Spanish => "es",
        TeachLanguage.Catalan => "ca",
        _ => "en",
    };

    /// <summary>Only redirect to local paths (prevents open redirects).</summary>
    public static bool IsLocalUrl(string? url) =>
        !string.IsNullOrEmpty(url) && url[0] == '/' && (url.Length == 1 || (url[1] != '/' && url[1] != '\\'));
}
