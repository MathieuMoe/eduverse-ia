using EduVerse.Core.Catalog;
using EduVerse.Core.Common;
using EduVerse.Core.Tutor;

namespace EduVerse.Tests;

public class CrossSubjectBridgesTests
{
    [Fact]
    public void Finds_a_bridge_when_the_other_side_is_mastered()
    {
        var match = CrossSubjectBridges.FindFor(Area.Economics, "economics.percentages", ["math.proportion"]);
        Assert.NotNull(match);
        Assert.Equal(Area.Mathematics, match!.KnownArea);
        Assert.Equal("math.proportion", match.KnownTopic);
        Assert.Equal(Area.Economics, match.StruggleArea);
        Assert.Equal("economics.percentages", match.StruggleTopic);
    }

    [Fact]
    public void Works_from_either_side_of_the_bridge()
    {
        var match = CrossSubjectBridges.FindFor(Area.Mathematics, "math.proportion", ["economics.percentages"]);
        Assert.NotNull(match);
        Assert.Equal(Area.Economics, match!.KnownArea);
        Assert.Equal("economics.percentages", match.KnownTopic);
    }

    [Fact]
    public void Returns_null_when_nothing_mastered_connects()
    {
        Assert.Null(CrossSubjectBridges.FindFor(Area.History, "history.rome", ["math.proportion"]));
        Assert.Null(CrossSubjectBridges.FindFor(Area.Economics, "economics.percentages", []));
    }

    [Fact]
    public void Every_curated_bridge_has_text_in_all_four_languages()
    {
        foreach (var b in CrossSubjectBridges.All())
        {
            foreach (var language in Enum.GetValues<TeachLanguage>())
            {
                Assert.False(string.IsNullOrWhiteSpace(b.Insight.In(language)));
            }
        }
    }
}
