using System.Globalization;
using System.Text.RegularExpressions;
using EduVerse.Core.Catalog;
using EduVerse.Core.Common;

namespace EduVerse.Tests;

/// <summary>
/// Task #97: History, Geography and Economics were the subjects whose fixed-fact pools were thin enough to repeat
/// within a session or two (a single question per tier in some drills, two at Geography's university levels). These
/// tests pin a minimum pool size so a future edit can't quietly shrink them back, and check that the new computed
/// questions (map scale, time zones, density, economic indicators) give the right answer.
/// </summary>
public partial class ContentPoolTests
{
    public static TheoryData<Area> ThinSubjects() => [Area.History, Area.Geography, Area.Economics];

    [Theory]
    [MemberData(nameof(ThinSubjects))]
    public void Every_level_and_difficulty_has_a_wide_pool_of_distinct_classic_questions(Area area)
    {
        foreach (var level in Catalog.LevelsOf(area))
        {
            for (var difficulty = 1; difficulty <= 3; difficulty++)
            {
                var distinct = new HashSet<string>(StringComparer.Ordinal);
                for (var seed = 0; seed < 1500; seed++)
                {
                    var q = QuestionBanks.Generate(area, level, difficulty, seed * 977 + difficulty, TeachLanguage.Spanish);
                    distinct.Add(q.Prompt + "§" + q.Answer);
                }

                Assert.True(distinct.Count >= 12, $"{area} {level} d{difficulty}: only {distinct.Count} distinct questions");
            }
        }
    }

    [Theory]
    [MemberData(nameof(ThinSubjects))]
    public void Every_drill_kind_has_more_than_one_item_at_every_level(Area area)
    {
        // Economics starts at Secondary2, so only its last difficulty tier is ever reached: it gets the most items there.
        var minimum = area == Area.Economics ? 5 : 2;
        var kinds = new (string Name, Func<Level, int, PracticeExercise?> Pick)[]
        {
            ("TrueFalse", (level, seed) => ExerciseBank.TrueFalse(area, level, TeachLanguage.English, seed)),
            ("FillBlank", (level, seed) => ExerciseBank.FillBlank(area, level, TeachLanguage.English, seed)),
            ("Reorder", (level, seed) => ExerciseBank.Reorder(area, level, TeachLanguage.English, seed)),
            ("Match", (level, seed) => ExerciseBank.Match(area, level, TeachLanguage.English, seed)),
            ("MultiSelect", (level, seed) => ExerciseBank.MultiSelect(area, level, TeachLanguage.English, seed)),
        };

        foreach (var level in Catalog.LevelsOf(area))
        {
            foreach (var (name, pick) in kinds)
            {
                var topics = new HashSet<string>(StringComparer.Ordinal);
                for (var seed = 0; seed < 300; seed++)
                {
                    topics.Add(pick(level, seed)!.Topic);
                }

                Assert.True(topics.Count >= minimum, $"{area} {level} {name}: only {topics.Count} distinct items");
            }
        }
    }

    [Theory]
    [MemberData(nameof(ThinSubjects))]
    public void Every_drill_is_answerable_with_its_own_answer_in_every_language(Area area)
    {
        foreach (var level in Catalog.LevelsOf(area))
        {
            foreach (var language in Enum.GetValues<TeachLanguage>())
            {
                for (var seed = 0; seed < 60; seed++)
                {
                    var fill = ExerciseBank.FillBlank(area, level, language, seed)!;
                    Assert.True(fill.CheckFillBlank(fill.Answer!) && fill.Prompt.Contains("___", StringComparison.Ordinal), $"{area} {level} {language} {fill.Topic}");

                    var reorder = ExerciseBank.Reorder(area, level, language, seed)!;
                    Assert.True(reorder.CheckReorder(reorder.CorrectOrder!), $"{area} {level} {language} {reorder.Topic}");

                    var match = ExerciseBank.Match(area, level, language, seed)!;
                    Assert.True(match.CheckMatch(match.Pairs!.ToDictionary(p => p.Left, p => p.Right)), $"{area} {level} {language} {match.Topic}");
                    Assert.True(match.Pairs!.Select(p => p.Right).Distinct(StringComparer.Ordinal).Count() == match.Pairs!.Count, $"{area} {level} {language} {match.Topic}");

                    var multi = ExerciseBank.MultiSelect(area, level, language, seed)!;
                    Assert.True(multi.CheckMultiSelect(multi.CorrectIndices!), $"{area} {level} {language} {multi.Topic}");
                    Assert.True(multi.Options!.Distinct(StringComparer.Ordinal).Count() == multi.Options!.Count, $"{area} {level} {language} {multi.Topic}");
                }
            }
        }
    }

    [Fact]
    public void Map_scale_and_time_zone_answers_are_computed_right()
    {
        var (scales, zones) = (0, 0);
        for (var seed = 0; seed < 400; seed++)
        {
            var q = QuestionBanks.Generate(Area.Geography, Level.HighSchool, 2, seed, TeachLanguage.English);
            if (ScalePrompt().Match(q.Prompt) is { Success: true } s)
            {
                var km = int.Parse(s.Groups[1].Value, CultureInfo.InvariantCulture) * int.Parse(s.Groups[2].Value, CultureInfo.InvariantCulture) / 100000.0;
                Assert.Equal(Math.Round(km, 2).ToString("0.##", CultureInfo.InvariantCulture) + " km", q.Answer);
                scales++;
            }
            else if (ZonePrompt().Match(q.Prompt) is { Success: true } z)
            {
                var start = int.Parse(z.Groups[1].Value, CultureInfo.InvariantCulture);
                var hours = int.Parse(z.Groups[2].Value, CultureInfo.InvariantCulture) / 15 * (z.Groups[3].Value == "east" ? 1 : -1);
                Assert.Equal($"{((start + hours) % 24 + 24) % 24:00}:00", q.Answer);
                zones++;
            }
        }

        Assert.True(scales > 5 && zones > 5, $"scales {scales}, zones {zones}");
    }

    [Fact]
    public void Economic_indicators_are_computed_right()
    {
        var (rates, real) = (0, 0);
        for (var seed = 0; seed < 600; seed++)
        {
            var q = QuestionBanks.Generate(Area.Economics, Level.HighSchool, 2, seed, TeachLanguage.English);
            if (UnemploymentPrompt().Match(q.Prompt) is { Success: true } u)
            {
                var rate = int.Parse(u.Groups[2].Value, CultureInfo.InvariantCulture) * 100.0 / int.Parse(u.Groups[1].Value, CultureInfo.InvariantCulture);
                Assert.Equal(rate.ToString("0.#", CultureInfo.InvariantCulture) + " %", q.Answer);
                rates++;
            }
            else if (RealInterestPrompt().Match(q.Prompt) is { Success: true } r)
            {
                var expected = int.Parse(r.Groups[1].Value, CultureInfo.InvariantCulture) - int.Parse(r.Groups[2].Value, CultureInfo.InvariantCulture);
                Assert.Equal(expected.ToString(CultureInfo.InvariantCulture) + " %", q.Answer);
                real++;
            }
        }

        Assert.True(rates > 5 && real > 5, $"unemployment {rates}, real interest {real}");
    }

    [GeneratedRegex(@"^On a 1:(\d+) map, two towns are (\d+) cm apart")]
    private static partial Regex ScalePrompt();

    [GeneratedRegex(@"When it is (\d+):00 at longitude 0°, what time is it at (\d+)° (east|west)\?")]
    private static partial Regex ZonePrompt();

    [GeneratedRegex(@"^Of (\d+) people in the labour force, (\d+) are unemployed")]
    private static partial Regex UnemploymentPrompt();

    [GeneratedRegex(@"^A deposit pays (\d+) % a year and inflation is (\d+) %")]
    private static partial Regex RealInterestPrompt();
}
