using System.Reflection;
using System.Text.Json;
using EduVerse.Core.Catalog;
using EduVerse.Core.Common;
using EduVerse.Infrastructure.Data;
using EduVerse.Infrastructure.Quizzes;

namespace EduVerse.Tests;

/// <summary>
/// Mathieu reported that practice/learn questions "sometimes repeat" — a real gap: fixed-fact subjects (History,
/// Geography...) have only a handful of distinct questions per difficulty tier, and a student's last 300 answered
/// questions are already excluded (<see cref="QuizStore.MemoryOfQuestions"/>), so the old retry loop could exhaust
/// a tier's whole pool and still hand back a duplicate. <see cref="QuizStore"/>'s private NextQuestion now widens
/// to the other two difficulties before accepting a repeat; these tests drive that private method directly via
/// reflection (it has no public seam) with a crafted QuizAttempt so the exact new code path is exercised.
/// </summary>
public class QuizRepeatAvoidanceTests
{
    private static readonly MethodInfo NextQuestionMethod =
        typeof(QuizStore).GetMethod("NextQuestion", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("QuizStore.NextQuestion not found — has it been renamed?");

    private static QuizQuestion NextQuestion(QuizAttempt attempt, TeachLanguage language) =>
        (QuizQuestion)NextQuestionMethod.Invoke(null, [attempt, language, Array.Empty<string>()])!;

    [Fact]
    public void Falls_back_to_another_difficulty_once_the_current_one_is_fully_excluded()
    {
        const Area area = Area.History;
        const Level level = Level.Secondary2;
        const TeachLanguage language = TeachLanguage.English;

        // Every distinct question this bank can produce at difficulty 1 for this area/level (sampled widely).
        var seenAtDifficulty1 = new HashSet<string>();
        for (var seed = 0; seed < 400; seed++)
        {
            seenAtDifficulty1.Add(QuizStore.KeyOf(QuestionBanks.Generate(area, level, 1, seed, language)));
        }

        Assert.True(seenAtDifficulty1.Count > 1, "History needs more than one difficulty-1 question for this test to mean anything");

        var attempt = new QuizAttempt
        {
            Id = "t1", UserId = "u1", Area = area, Level = level, Mode = QuizMode.Practice,
            Seed = 12345, Difficulty = 1, Answered = 0, Length = 10, CurrentJson = "{}",
            UsedJson = JsonSerializer.Serialize(seenAtDifficulty1.ToList()),
        };

        var question = NextQuestion(attempt, language);

        Assert.False(seenAtDifficulty1.Contains(QuizStore.KeyOf(question)),
            "with every difficulty-1 question already seen, the fallback should serve something from another difficulty rather than repeat one");
    }

    [Fact]
    public void Still_returns_a_question_when_every_difficulty_is_exhausted()
    {
        const Area area = Area.History;
        const Level level = Level.Secondary2;
        const TeachLanguage language = TeachLanguage.English;

        var seenEverywhere = new HashSet<string>();
        foreach (var difficulty in new[] { 1, 2, 3 })
        {
            for (var seed = 0; seed < 400; seed++)
            {
                seenEverywhere.Add(QuizStore.KeyOf(QuestionBanks.Generate(area, level, difficulty, seed, language)));
            }
        }

        var attempt = new QuizAttempt
        {
            Id = "t2", UserId = "u2", Area = area, Level = level, Mode = QuizMode.Practice,
            Seed = 999, Difficulty = 1, Answered = 0, Length = 10, CurrentJson = "{}",
            UsedJson = JsonSerializer.Serialize(seenEverywhere.ToList()),
        };

        // Every difficulty genuinely exhausted: the fallback has nothing new to offer, so it must still return
        // *some* question rather than throw — a duplicate here is an acceptable last resort, not a crash.
        var question = NextQuestion(attempt, language);
        Assert.NotNull(question);
    }

    [Fact]
    public void Does_not_widen_when_the_current_difficulty_still_has_something_unseen()
    {
        const Area area = Area.History;
        const Level level = Level.Secondary2;
        const TeachLanguage language = TeachLanguage.English;

        var attempt = new QuizAttempt
        {
            Id = "t3", UserId = "u3", Area = area, Level = level, Mode = QuizMode.Practice,
            Seed = 42, Difficulty = 2, Answered = 0, Length = 10, CurrentJson = "{}", UsedJson = "[]",
        };

        var question = NextQuestion(attempt, language);

        Assert.Equal(2, question.Difficulty);
    }
}
