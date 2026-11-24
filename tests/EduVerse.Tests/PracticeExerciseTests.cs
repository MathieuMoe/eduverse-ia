using EduVerse.Core.Catalog;

namespace EduVerse.Tests;

public class PracticeExerciseTests
{
    private static PracticeExercise TrueFalse(bool correct) =>
        new("demo", 1, ExerciseKind.TrueFalse, "Water boils at 100°C at sea level.", "Explanation.", CorrectBool: correct);

    private static PracticeExercise FillBlank(string answer, params string[] alternatives) =>
        new("demo", 1, ExerciseKind.FillBlank, "The capital of France is ___.", "Explanation.",
            Answer: answer, AcceptedAlternatives: alternatives);

    private static PracticeExercise Reorder(params string[] correctOrder) =>
        new("demo", 1, ExerciseKind.Reorder, "Put the words in order.", "Explanation.",
            Tokens: correctOrder, CorrectOrder: correctOrder);

    private static PracticeExercise Match(params (string Left, string Right)[] pairs) =>
        new("demo", 1, ExerciseKind.Match, "Match each pair.", "Explanation.",
            Pairs: pairs.Select(p => new MatchPair(p.Left, p.Right)).ToList());

    [Theory]
    [InlineData(true, true, true)]
    [InlineData(true, false, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, false)]
    public void TrueFalse_checks_against_the_correct_bool(bool correct, bool given, bool expected) =>
        Assert.Equal(expected, TrueFalse(correct).CheckTrueFalse(given));

    [Fact]
    public void FillBlank_wrong_kind_throws() =>
        Assert.Throws<InvalidOperationException>(() => TrueFalse(true).CheckFillBlank("x"));

    [Theory]
    [InlineData("Paris", true)]
    [InlineData("paris", true)]
    [InlineData("  PARIS  ", true)]
    [InlineData("Lyon", false)]
    public void FillBlank_matches_the_answer_case_and_space_insensitively(string given, bool expected) =>
        Assert.Equal(expected, FillBlank("Paris").CheckFillBlank(given));

    [Fact]
    public void FillBlank_matches_accents_insensitively() =>
        Assert.True(FillBlank("café").CheckFillBlank("cafe"));

    [Fact]
    public void FillBlank_accepts_a_listed_alternative() =>
        Assert.True(FillBlank("colour", "color").CheckFillBlank("color"));

    [Fact]
    public void FillBlank_rejects_an_unlisted_alternative() =>
        Assert.False(FillBlank("colour", "color").CheckFillBlank("couleur"));

    [Fact]
    public void Reorder_requires_the_exact_sequence()
    {
        var ex = Reorder("The", "cat", "sleeps");
        Assert.True(ex.CheckReorder(["The", "cat", "sleeps"]));
        Assert.False(ex.CheckReorder(["cat", "The", "sleeps"]));
        Assert.False(ex.CheckReorder(["The", "cat"]));
    }

    [Fact]
    public void Match_requires_every_pair_and_only_those_pairs()
    {
        var ex = Match(("cat", "gato"), ("dog", "perro"));
        Assert.True(ex.CheckMatch(new Dictionary<string, string> { ["cat"] = "gato", ["dog"] = "perro" }));
        Assert.False(ex.CheckMatch(new Dictionary<string, string> { ["cat"] = "perro", ["dog"] = "gato" }));
        Assert.False(ex.CheckMatch(new Dictionary<string, string> { ["cat"] = "gato" }));
    }

    [Fact]
    public void FromQuizQuestion_wraps_a_multiple_choice_question_unchanged()
    {
        var q = new QuizQuestion("demo", 2, "2 + 2 = ?", ["3", "4", "5", "6"], 1, "Basic addition.");
        var ex = PracticeExercise.FromQuizQuestion(q);

        Assert.Equal(ExerciseKind.MultipleChoice, ex.Kind);
        Assert.Equal(q.Topic, ex.Topic);
        Assert.Equal(q.Difficulty, ex.Difficulty);
        Assert.Equal(q.Prompt, ex.Prompt);
        Assert.Equal(q.Options, ex.Options);
        Assert.Equal(q.CorrectIndex, ex.CorrectIndex);
    }
}
