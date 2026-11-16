using System.Text.RegularExpressions;
using EduVerse.Core.Common;
using EduVerse.Core.Curriculum;
using EduVerse.Core.Learning;

namespace EduVerse.Tests;

/// <summary>The exercises generated in code must be mathematically right, whatever the random numbers.</summary>
public partial class FractionExerciseTests
{
    [GeneratedRegex(@"(\d+)/(\d+)")]
    private static partial Regex FractionRegex();

    private static (int N, int D) Parse(string text)
    {
        var m = FractionRegex().Match(text);
        Assert.True(m.Success, $"Not a fraction: {text}");
        return (int.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture), int.Parse(m.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture));
    }

    private static List<(int N, int D)> Fractions(string text) =>
        FractionRegex().Matches(text).Select(m => (int.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture), int.Parse(m.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture))).ToList();

    private static (int N, int D) Add((int N, int D) a, (int N, int D) b) => (a.N * b.D + b.N * a.D, a.D * b.D);

    private static (int N, int D) Times((int N, int D) a, (int N, int D) b) => (a.N * b.N, a.D * b.D);

    private static bool Bigger((int N, int D) a, (int N, int D) b) => a.N * b.D > b.N * a.D;

    private static bool Same((int N, int D) a, (int N, int D) b) => a.N * b.D == b.N * a.D;

    public static TheoryData<string, int> Cases()
    {
        var data = new TheoryData<string, int>();
        foreach (var concept in BuiltInSubjects.FractionConcepts)
        {
            for (var difficulty = 1; difficulty <= 3; difficulty++)
            {
                data.Add(concept, difficulty);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void Correct_option_is_right_and_wrong_options_are_distinct_tagged_mistakes(string concept, int difficulty)
    {
        var random = new Random(concept.GetHashCode(StringComparison.Ordinal) ^ difficulty);
        for (var i = 0; i < 300; i++)
        {
            var e = FractionExercises.Create(concept, difficulty, random, TeachLanguage.English);
            Assert.InRange(e.Options.Count, 3, 4);
            Assert.Equal(e.Options.Count, e.Options.Distinct().Count());
            Assert.InRange(e.CorrectIndex, 0, e.Options.Count - 1);
            Assert.Null(e.Misconceptions[e.CorrectIndex]);
            Assert.All(Enumerable.Range(0, e.Options.Count).Where(j => j != e.CorrectIndex), j => Assert.False(string.IsNullOrEmpty(e.Misconceptions[j])));

            var correct = Parse(e.Options[e.CorrectIndex]);
            var q = Fractions(e.Question);
            switch (concept)
            {
                case "meaning":
                {
                    var numbers = Regex.Matches(e.Question, @"\d+").Select(m => int.Parse(m.Value, System.Globalization.CultureInfo.InvariantCulture)).ToList();
                    Assert.Equal((numbers[1], numbers[0]), correct);
                    break;
                }

                case "equivalent":
                    Assert.True(Same(correct, q[0]));
                    Assert.NotEqual(q[0], correct);
                    break;
                case "simplify":
                    Assert.True(Same(correct, q[0]));
                    Assert.Equal(1, FractionExercises.Gcd(correct.N, correct.D));
                    break;
                case "compare":
                    Assert.Equal(Bigger(q[0], q[1]) ? q[0] : q[1], correct);
                    break;
                case "add-same":
                case "add-different":
                    Assert.True(Same(Add(q[0], q[1]), correct));
                    break;
                default:
                    Assert.True(Same(Times(q[0], q[1]), correct));
                    Assert.Equal(1, FractionExercises.Gcd(correct.N, correct.D));
                    break;
            }

            // No wrong option may be worth the same as the right answer (it would be marked wrong although right),
            // except "not fully simplified", which is equal on purpose but not in its simplest form
            foreach (var j in Enumerable.Range(0, e.Options.Count).Where(j => j != e.CorrectIndex && FractionRegex().IsMatch(e.Options[j])
                && e.Misconceptions[j] != FractionExercises.NotFullySimplified))
            {
                Assert.False(Same(Parse(e.Options[j]), correct), $"{e.Question}: '{e.Options[j]}' equals '{e.Options[e.CorrectIndex]}'");
            }
        }
    }

    [Fact]
    public void Every_misconception_of_the_built_in_subject_has_a_warning_in_4_languages()
    {
        var codes = new HashSet<string>();
        var random = new Random(7);
        foreach (var concept in BuiltInSubjects.FractionConcepts)
        {
            for (var i = 0; i < 200; i++)
            {
                codes.UnionWith(FractionExercises.Create(concept, 1 + i % 3, random, TeachLanguage.English).Misconceptions.OfType<string>());
            }
        }

        foreach (var language in Enum.GetValues<TeachLanguage>())
        {
            Assert.All(codes, code => Assert.NotEqual(code, DemoLearningAi.Warning(code, language)));
        }
    }

    [Theory]
    [InlineData(TeachLanguage.Spanish, "¿Qué fracción es igual a")]
    [InlineData(TeachLanguage.French, "Quelle fraction est égale à")]
    [InlineData(TeachLanguage.Catalan, "Quina fracció és igual a")]
    public void Exercises_are_written_in_the_student_language(TeachLanguage language, string start)
    {
        var e = FractionExercises.Create("equivalent", 1, new Random(1), language);
        Assert.StartsWith(start, e.Question, StringComparison.Ordinal);
    }
}
