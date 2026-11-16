namespace EduVerse.Core.Learning;

public enum MasteryStatus
{
    NotStarted,
    Learning,
    Practising,
    Mastered,
}

/// <summary>Ways to explain a concept. When one does not work for a student, the tutor switches to the next.</summary>
public enum ExplanationMethod
{
    StepByStep,
    Visual,
    RealLife,
    WorkedExample,
    Analogy,
}

/// <summary>What the platform knows about a student on one concept (Bayesian Knowledge Tracing).</summary>
public sealed record MasteryState(
    string ConceptKey,
    double PKnown,
    int Attempts,
    int Correct,
    int Streak,
    int FailuresWithMethod,
    ExplanationMethod Method,
    IReadOnlyList<ExplanationMethod> MethodsTried,
    IReadOnlyDictionary<string, int> Misconceptions,
    DateTimeOffset? LastAttemptAt,
    MasteryStatus Status,
    bool TeacherAlerted = false,
    bool NeedsExplanation = true)
{
    public static MasteryState New(string conceptKey, double prior = BktParameters.DefaultPrior) =>
        new(conceptKey, prior, 0, 0, 0, 0, ExplanationMethod.StepByStep, [ExplanationMethod.StepByStep], new Dictionary<string, int>(), null, MasteryStatus.NotStarted);
}

public enum LearningEventKind
{
    Mastered,
    SwitchMethod,
    RecurringMisconception,
    NeedsTeacher,
    ReviewPrerequisite,
}

/// <summary>Something that happened while learning, for the student (feedback) and the teacher (alerts).</summary>
public sealed record LearningEvent(LearningEventKind Kind, string ConceptKey, string? Detail = null);

/// <summary>One multiple-choice exercise. Each wrong option may carry the misconception it reveals.</summary>
public sealed record Exercise(
    string Id,
    string ConceptKey,
    int Difficulty,
    string Question,
    IReadOnlyList<string> Options,
    int CorrectIndex,
    IReadOnlyList<string?> Misconceptions,
    string Hint,
    string Explanation,
    string? Visual = null,
    string? Code = null)
{
    public const int MinDifficulty = 1;
    public const int MaxDifficulty = 3;
}

/// <summary>An explanation written for this student, with a method, an example and a quick check.</summary>
public sealed record Explanation(string ConceptKey, ExplanationMethod Method, string Title, string Body, string Example, string CheckQuestion, string CheckAnswer);
