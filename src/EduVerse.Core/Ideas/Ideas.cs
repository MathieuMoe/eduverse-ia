using System.Globalization;
using System.Text;
using EduVerse.Core.Catalog;
using EduVerse.Core.Common;
using EduVerse.Core.Curriculum;
using EduVerse.Core.Tutor;

namespace EduVerse.Core.Ideas;

/// <summary>What a student proposes: a figure to interview, an adventure world, a trade or a hobby.</summary>
public enum IdeaKind
{
    Figure,
    Adventure,
    Trade,
    Hobby,
}

/// <summary>Life of an idea. Only Approved, InProgress and Available ideas are shown to other students.</summary>
public enum IdeaStatus
{
    New,
    Approved,
    InProgress,
    Available,
    Rejected,
}

/// <summary>How hard an adventure felt.</summary>
public enum Felt
{
    TooEasy,
    JustRight,
    TooHard,
}

/// <summary>Why a proposal was not saved as typed.</summary>
public enum IdeaCheck
{
    Ok,
    TooShort,
    TooLong,
    NotAllowed,
}

/// <summary>
/// Ideas of the students: rules decided in code. Texts are short, normalised so that "Napoleón" and "napoleon" are the same idea,
/// filtered (no links, e-mails, phone numbers or rude words) and compared with what the platform already offers.
/// </summary>
public static class IdeaRules
{
    public const int MinLength = 2;
    public const int MaxLength = 60;
    public const int MaxReason = 140;

    /// <summary>New proposals a student can make per day (votes are not limited).</summary>
    public const int DailyLimit = 5;

    /// <summary>XP for the student who proposed an idea when it becomes available.</summary>
    public const int AvailableXp = 30;

    public static bool IsPublic(IdeaStatus status) => status is IdeaStatus.Approved or IdeaStatus.InProgress or IdeaStatus.Available;

    /// <summary>Lower case, no accents, no punctuation, single spaces: the key that groups the same idea.</summary>
    public static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "";
        }

        var decomposed = text.Trim().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        var space = false;
        foreach (var c in decomposed)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(c))
            {
                sb.Append(char.ToLowerInvariant(c));
                space = false;
            }
            else if (!space && sb.Length > 0)
            {
                sb.Append(' ');
                space = true;
            }
        }

        return sb.ToString().Trim();
    }

    /// <summary>Clean display text: trimmed, single spaces, first letter in upper case.</summary>
    public static string Clean(string? text)
    {
        var t = string.Join(' ', (text ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return t.Length == 0 ? t : char.ToUpper(t[0], CultureInfo.InvariantCulture) + t[1..];
    }

    // A short list of rude words in the 4 languages, compared on whole normalised words (moderation by an adult comes after).
    private static readonly HashSet<string> Blocked = new(StringComparer.Ordinal)
    {
        "fuck", "shit", "bitch", "asshole", "dick", "idiot", "stupid", "puta", "puto", "mierda", "joder", "gilipollas", "cabron", "idiota",
        "imbecil", "estupido", "estupida", "merde", "putain", "connard", "connasse", "salope", "encule", "debile",
        "merda", "collons", "nazi", "hitler", "porn", "porno", "xxx",
    };

    public static IdeaCheck Check(string? text, string? reason = null)
    {
        var t = (text ?? "").Trim();
        if (Normalize(t).Length < MinLength)
        {
            return IdeaCheck.TooShort;
        }

        if (t.Length > MaxLength || (reason?.Trim().Length ?? 0) > MaxReason)
        {
            return IdeaCheck.TooLong;
        }

        return Safe(t) && Safe(reason) ? IdeaCheck.Ok : IdeaCheck.NotAllowed;
    }

    /// <summary>A short text written by a student carries no link, e-mail, phone number or rude word.</summary>
    public static bool Safe(string? text)
    {
        var part = text ?? "";
        return !(part.Contains('@', StringComparison.Ordinal) || part.Contains("http", StringComparison.OrdinalIgnoreCase) || part.Contains("www", StringComparison.OrdinalIgnoreCase)
            || part.Count(char.IsDigit) >= 6 || Normalize(part).Split(' ').Any(Blocked.Contains));
    }

    private static readonly (Theme Theme, string Words)[] ThemeWords =
    [
        (Theme.Space, "space;espacio;espace;espai;planetas;planets;planetes"),
        (Theme.Ocean, "ocean;oceano;ocea;mar;mer;sea;submarino"),
        (Theme.Jungle, "jungle;jungla;selva;forest"),
        (Theme.Castle, "castle;castillo;chateau;castell;medieval"),
        (Theme.Dino, "dino;dinosaur;dinosaurio;dinosaure;dinosauri;dinosaurios"),
        (Theme.Circus, "circus;circo;cirque;circ;clown;payaso"),
    ];

    /// <summary>The name of what the platform already offers for this proposal, if any (then it is not saved).</summary>
    public static string? Existing(IdeaKind kind, string text, TeachLanguage language)
    {
        var key = Normalize(text);
        if (key.Length == 0)
        {
            return null;
        }

        bool Same(string name)
        {
            var n = Normalize(name);
            return n == key || (key.Length >= 4 && (n.Contains(key, StringComparison.Ordinal) || key.Contains(n, StringComparison.Ordinal)))
                || n.Split(' ').LastOrDefault() == key;
        }

        switch (kind)
        {
            case IdeaKind.Figure:
                return Interviews.All.FirstOrDefault(f => Same(f.Name))?.Name;
            case IdeaKind.Adventure:
                var words = key.Split(' ');
                var theme = ThemeWords.FirstOrDefault(t => t.Words.Split(';').Any(w => words.Contains(w)));
                return theme.Words is null ? null : Adventure.Emoji(theme.Theme) + " " + theme.Theme;
            default:
                var trade = Trades.All.FirstOrDefault(t => new[] { t.Name.En, t.Name.Es, t.Name.Fr, t.Name.Ca }.Any(Same));
                return trade is null ? null : trade.Icon + " " + trade.Name.In(language);
        }
    }

    /// <summary>Where a student finds an idea of this kind.</summary>
    public static string Url(IdeaKind kind) => kind switch
    {
        IdeaKind.Figure => "/Student/Interview",
        IdeaKind.Adventure => "/Student/Adventures",
        IdeaKind.Trade => "/Student/Learn?tab=trades",
        _ => "/Student/Learn?tab=hobbies",
    };

    public static string Icon(IdeaKind kind) => kind switch
    {
        IdeaKind.Figure => "🎤",
        IdeaKind.Adventure => "🗺️",
        IdeaKind.Trade => "🛠️",
        _ => "🎨",
    };
}

/// <summary>An idea of the student that is now available and that the Tutor announces.</summary>
public sealed record IdeaNews(string Text, IdeaKind Kind)
{
    public string Url => IdeaRules.Url(Kind);
}
