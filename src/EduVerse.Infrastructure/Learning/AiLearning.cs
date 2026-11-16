using EduVerse.Core.Abstractions;
using EduVerse.Core.Common;
using EduVerse.Core.Content;
using EduVerse.Core.Curriculum;
using EduVerse.Core.Learning;

namespace EduVerse.Infrastructure.Learning;

/// <summary>
/// The writing part of the tutor done by the selected AI engine. Every answer goes through the strict parsers of
/// <see cref="LearningPrompts"/>: invalid exercises are dropped, concept maps with unknown prerequisites or cycles are repaired or rejected.
/// </summary>
public sealed class AiLearning(IStructuredAi ai) : ILearningAi
{
    public async Task<Explanation> ExplainAsync(Subject subject, Concept concept, ExplanationMethod method, StudentContext student, CancellationToken cancellationToken = default) =>
        LearningPrompts.ParseExplanation(await ai.GenerateAsync(LearningPrompts.Explain(subject, concept, method, student), cancellationToken).ConfigureAwait(false), concept, method);

    public async Task<IReadOnlyList<Exercise>> ExercisesAsync(Subject subject, Concept concept, int difficulty, int count, StudentContext student, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(subject);
        if (subject.BuiltIn)
        {
            // Built-in subject: exercises computed in code are always mathematically right, the AI is not needed
            return await new DemoLearningAi().ExercisesAsync(subject, concept, difficulty, count, student, cancellationToken).ConfigureAwait(false);
        }

        return LearningPrompts.ParseExercises(await ai.GenerateAsync(LearningPrompts.Exercises(subject, concept, difficulty, count, student), cancellationToken).ConfigureAwait(false), concept, difficulty);
    }

    public async Task<Subject> ConceptMapAsync(string topic, Stage stage, TeachLanguage language, CancellationToken cancellationToken = default) =>
        LearningPrompts.ParseConceptMap(await ai.GenerateAsync(LearningPrompts.ConceptMap(topic, stage, language), cancellationToken).ConfigureAwait(false), topic);

    public async Task<TutorReply> ChatAsync(Subject subject, Concept concept, IReadOnlyList<ChatTurn> history, StudentContext student, CancellationToken cancellationToken = default) =>
        LearningPrompts.ParseChat(await ai.GenerateAsync(LearningPrompts.Chat(subject, concept, history, student), cancellationToken).ConfigureAwait(false));
}

/// <summary>Lesson pack written by the selected AI engine and validated by <see cref="PackPrompt.Parse"/> (4 distinct options, valid index...).</summary>
public sealed class AiPackGenerator(IStructuredAi ai) : IPackGenerator
{
    public async Task<LessonPack> GenerateAsync(PackRequest request, CancellationToken cancellationToken = default) =>
        PackPrompt.Parse(await ai.GenerateAsync(PackPrompt.Build(request), cancellationToken).ConfigureAwait(false), request);
}
