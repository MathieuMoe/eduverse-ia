using EduVerse.Core.Common;
using EduVerse.Core.Curriculum;

namespace EduVerse.Core.Learning;

/// <summary>What the tutor knows about the student when it writes (no name, no personal data).</summary>
public sealed record StudentContext(Stage Stage, TeachLanguage Language, IReadOnlyList<string> RecentMisconceptions, double PKnown, IReadOnlyList<ExplanationMethod> MethodsTried)
{
    /// <summary>How this student learns, as measured by the platform (one line of data for the prompts).</summary>
    public string? Learner { get; init; }

    /// <summary>What the student likes (keys of the fixed list): examples may be set in those worlds.</summary>
    public IReadOnlyList<string> Interests { get; init; } = [];

    /// <summary>Short notes the student's teacher left about them (text written by a person: data, not instructions).</summary>
    public IReadOnlyList<string> TeacherNotes { get; init; } = [];

    /// <summary>Pieces of the teacher's documents chosen for this question, ready for the prompt ("" = none).</summary>
    public string Material { get; init; } = "";
}

public sealed record ChatTurn(bool FromStudent, string Text);

/// <summary>A Socratic reply: guides with a question instead of giving the solution.</summary>
public sealed record TutorReply(string Reply, string? Question);

/// <summary>The writing part of the tutor (explanations, exercises, concept maps, dialogue). The decisions stay in code.</summary>
public interface ILearningAi
{
    Task<Explanation> ExplainAsync(Subject subject, Concept concept, ExplanationMethod method, StudentContext student, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Exercise>> ExercisesAsync(Subject subject, Concept concept, int difficulty, int count, StudentContext student, CancellationToken cancellationToken = default);

    Task<Subject> ConceptMapAsync(string topic, Stage stage, TeachLanguage language, CancellationToken cancellationToken = default);

    Task<TutorReply> ChatAsync(Subject subject, Concept concept, IReadOnlyList<ChatTurn> history, StudentContext student, CancellationToken cancellationToken = default);
}

/// <summary>Raised when a feature needs a real AI engine (for instance a subject generated from a free topic in demo mode).</summary>
public sealed class NeedsAiException : Exception
{
    public NeedsAiException() : base("This feature needs an AI engine.") { }

    public NeedsAiException(string message) : base(message) { }

    public NeedsAiException(string message, Exception inner) : base(message, inner) { }
}
