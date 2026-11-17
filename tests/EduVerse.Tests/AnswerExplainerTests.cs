using EduVerse.Core.Catalog;
using EduVerse.Core.Common;

namespace EduVerse.Tests;

public class AnswerExplainerTests
{
    private static readonly QuizQuestion Question = new("math:fractions", 2, "What is 1/2 + 1/4?", ["3/4", "1/2", "2/6", "1/4"], 0, "Put both over 4 and add the numerators: 2/4 + 1/4 = 3/4.");

    public static TheoryData<Level, TeachLanguage> LevelsAndLanguages()
    {
        var data = new TheoryData<Level, TeachLanguage>();
        foreach (var level in new[] { Level.Primary2, Level.Secondary1, Level.HighSchool, Level.Bachelor, Level.Doctorate })
        {
            foreach (var language in Enum.GetValues<TeachLanguage>())
            {
                data.Add(level, language);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(LevelsAndLanguages))]
    public void A_right_answer_keeps_the_question_s_own_reasoning_and_is_framed_for_the_stage(Level level, TeachLanguage language)
    {
        var explained = AnswerExplainer.Explain(Question, correct: true, level, language);
        Assert.Contains(Question.Explanation.TrimEnd('.')[1..], explained, StringComparison.Ordinal); // same reasoning, only the first letter may change case
        Assert.NotEqual(Question.Explanation, explained); // it was actually framed, not just copied
        Assert.DoesNotContain("{0}", explained, StringComparison.Ordinal);
        Assert.DoesNotContain("{1}", explained, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(LevelsAndLanguages))]
    public void A_wrong_answer_names_the_right_one_and_keeps_the_reasoning(Level level, TeachLanguage language)
    {
        var explained = AnswerExplainer.Explain(Question, correct: false, level, language);
        Assert.Contains(Question.Answer, explained, StringComparison.Ordinal); // never shown before this change
        Assert.Contains(Question.Explanation.TrimEnd('.')[1..], explained, StringComparison.Ordinal);
        Assert.DoesNotContain("{0}", explained, StringComparison.Ordinal);
        Assert.DoesNotContain("{1}", explained, StringComparison.Ordinal);
    }

    [Fact]
    public void Primary_and_university_students_are_not_told_the_same_way()
    {
        var kid = AnswerExplainer.Explain(Question, correct: true, Level.Primary1, TeachLanguage.English);
        var adult = AnswerExplainer.Explain(Question, correct: true, Level.Doctorate, TeachLanguage.English);
        Assert.NotEqual(kid, adult);
    }
}
