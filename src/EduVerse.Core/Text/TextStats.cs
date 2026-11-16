using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using EduVerse.Core.Common;

namespace EduVerse.Core.Text;

public enum Readability
{
    VeryEasy,
    Easy,
    Normal,
    Difficult,
    VeryDifficult,
}

/// <param name="Score">0-100, higher is easier (Flesch for English, Szigriszt-Pazos for Spanish and Catalan, Kandel-Moles for French).</param>
public sealed record TextStats(int Words, int Sentences, double WordsPerSentence, double SyllablesPerWord, int LongWordsPercent, int ReadingMinutes, int Score, Readability Level);

/// <summary>Measures a text in code (words, sentences, syllables, readability), so the levels can be checked, not just trusted.</summary>
public static partial class TextMeasure
{
    [GeneratedRegex(@"[\p{L}\p{M}']+(?:-[\p{L}\p{M}]+)*")]
    private static partial Regex WordRegex();

    [GeneratedRegex(@"[^.!?…\n]+[.!?…]*")]
    private static partial Regex SentenceRegex();

    [GeneratedRegex("[aeiouyáéíóúàèìòùâêîôûäëïöüœæ]+")]
    private static partial Regex VowelGroups();

    public static IReadOnlyList<string> Words(string text) => WordRegex().Matches(text ?? "").Select(m => m.Value).ToList();

    public static IReadOnlyList<string> Sentences(string text) =>
        SentenceRegex().Matches(text ?? "").Select(m => m.Value.Trim()).Where(s => WordRegex().IsMatch(s)).ToList();

    /// <summary>Approximate syllables: groups of vowels, with a silent final "e" in English and French.</summary>
    public static int Syllables(string word, TeachLanguage language)
    {
        ArgumentNullException.ThrowIfNull(word);
        var w = word.ToLowerInvariant();
        var count = VowelGroups().Count(w);
        if (language is TeachLanguage.English or TeachLanguage.French && w.Length > 3 && w.EndsWith('e') && !w.EndsWith("le", StringComparison.Ordinal))
        {
            count--;
        }

        return Math.Max(1, count);
    }

    public static TextStats Measure(string text, TeachLanguage language, int wordsPerMinute = 180)
    {
        var words = Words(text);
        var sentences = Math.Max(1, Sentences(text).Count);
        if (words.Count == 0)
        {
            return new TextStats(0, 0, 0, 0, 0, 0, 100, Readability.VeryEasy);
        }

        var syllables = words.Sum(w => Syllables(w, language));
        var wps = (double)words.Count / sentences;
        var spw = (double)syllables / words.Count;
        var score = language switch
        {
            TeachLanguage.English => 206.835 - 1.015 * wps - 84.6 * spw,
            TeachLanguage.French => 207 - 1.015 * wps - 73.6 * spw,
            _ => 206.835 - 62.3 * spw - wps,
        };
        var clamped = (int)Math.Round(Math.Clamp(score, 0, 100));
        var level = clamped >= 80 ? Readability.VeryEasy : clamped >= 65 ? Readability.Easy : clamped >= 50 ? Readability.Normal : clamped >= 35 ? Readability.Difficult : Readability.VeryDifficult;
        return new TextStats(words.Count, sentences, Math.Round(wps, 1), Math.Round(spw, 2),
            (int)Math.Round(100.0 * words.Count(w => w.Length >= 9) / words.Count),
            Math.Max(1, (int)Math.Ceiling(words.Count / (double)wordsPerMinute)), clamped, level);
    }

    public static string Normalize(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var d = text.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        return new string(d.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray());
    }

    /// <summary>Very common words (4 languages), ignored when looking for key terms.</summary>
    public static readonly HashSet<string> StopWords = new(
        """
        the a an and or but of to in on at for with by from is are was were be been being this that these those it its as not no can will would should could has have had do does did
        than then there their they them we you he she his her our your which who whom what when where why how also more most such into about over under between after before
        el la los las un una unos unas y o pero de del al a en con por para es son era eran fue ser este esta estos estas ese esa eso lo le les se su sus que como cuando donde
        mas muy tambien sin sobre entre hay ya si no ni nos me te mi tu porque cada todo todos toda todas otro otra
        le la les un une des et ou mais de du au aux en avec par pour est sont etait etre ce cette ces qui que quoi dont ou sur dans pas plus tres aussi il elle ils elles nous vous leur leurs son sa ses
        el la els les un una uns unes i o pero de del al a en amb per es son era eren ser aquest aquesta aquests aquestes que com quan on mes molt tambe sense sobre entre hi ja si no ni ens em et seu seva seus
        """.Split([' ', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries), StringComparer.Ordinal);

    /// <summary>Key terms: long, non-common words, by frequency then length.</summary>
    public static IReadOnlyList<string> KeyTerms(string text, int max)
    {
        return Words(text)
            .Where(w => w.Length >= 7 && !StopWords.Contains(Normalize(w)))
            .GroupBy(w => Normalize(w))
            .OrderByDescending(g => g.Count()).ThenByDescending(g => g.Key.Length)
            .Select(g => g.FirstOrDefault(w => !char.IsUpper(w[0])) ?? g.First())
            .Take(max).ToList();
    }
}
