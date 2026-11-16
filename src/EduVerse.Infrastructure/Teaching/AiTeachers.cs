using EduVerse.Core.Abstractions;
using EduVerse.Core.Lessons;
using EduVerse.Core.Summaries;

namespace EduVerse.Infrastructure.Teaching;

/// <summary>Lesson in 3 levels written by the selected AI engine; readability measured and levels checked by the parser.</summary>
public sealed class AiLessonAdapter(IStructuredAi ai) : ILessonAdapter
{
    public async Task<LessonResult> AdaptAsync(LessonRequest request, CancellationToken cancellationToken = default) =>
        LessonPrompt.Parse(await ai.GenerateAsync(LessonPrompt.Build(request), cancellationToken), request);
}

/// <summary>Class summary written by the selected AI engine; homework cross-checked with the teacher's notes.</summary>
public sealed class AiClassSummarizer(IStructuredAi ai) : IClassSummarizer
{
    public async Task<ClassSummary> SummarizeAsync(SummaryRequest request, CancellationToken cancellationToken = default) =>
        SummaryPrompt.Parse(await ai.GenerateAsync(SummaryPrompt.Build(request), cancellationToken), request);
}
