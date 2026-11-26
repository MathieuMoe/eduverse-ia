using EduVerse.Core.Common;
using EduVerse.Core.Learning;
using EduVerse.Core.Tutor;
using EduVerse.Infrastructure.Data;
using EduVerse.Infrastructure.Quizzes;
using Microsoft.EntityFrameworkCore;

namespace EduVerse.Infrastructure.Tutor;

/// <summary>Gathers the facts for the <see cref="FutureSelf"/> letter: how many topics the student has mastered
/// across every subject, up to two past struggles now confirmed gone, and the open "what I wonder about" question
/// (if any) that has waited longest — the same sources <see cref="CompanionStore"/> and <see cref="LetterStore"/>
/// already use, just combined differently.</summary>
public sealed class FutureSelfStore(EduVerseDb db, QuizStore quizzes, CuriosityStore curiosity)
{
    public const int MaxOvercome = 2;

    public async Task<FutureSelfFacts> FactsAsync(string userId, string firstName, TeachLanguage language, CancellationToken ct = default)
    {
        var mastered = await db.TopicSkills.CountAsync(s => s.UserId == userId && s.Attempts >= 2 && s.PKnown >= CrossSubjectBridges.KnownThreshold, ct).ConfigureAwait(false);
        var overcome = (await quizzes.MisconceptionMemoryAsync(userId, ct).ConfigureAwait(false))
            .Where(m => m.Status == MisconceptionStatus.Overcome)
            .Take(MaxOvercome)
            .Select(m => Misconceptions.Describe(m.Code, Core.Curriculum.BuiltInSubjects.FractionsKey, language))
            .ToList();
        var question = await curiosity.OldestOpenAsync(userId, ct).ConfigureAwait(false);
        return new FutureSelfFacts(firstName, mastered, overcome, question?.Text,
            question is null ? null : "/Student/NewSubject?topic=" + Uri.EscapeDataString(question.Text));
    }
}
