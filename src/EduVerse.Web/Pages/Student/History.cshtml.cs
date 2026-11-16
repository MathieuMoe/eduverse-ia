using EduVerse.Core.Catalog;
using EduVerse.Infrastructure.Data;
using EduVerse.Infrastructure.Quizzes;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduVerse.Web.Pages.Student;

/// <summary>Tests done: the list (with a filter per subject) and the review of one test, question by question.</summary>
public sealed class HistoryModel(QuizStore quizzes) : PageModel
{
    public IReadOnlyList<QuizAttempt> Tests { get; private set; } = [];

    public Area? Area { get; private set; }

    public QuizAttempt? Review { get; private set; }

    public IReadOnlyList<QuizAnswer> Answers { get; private set; } = [];

    public async Task OnGetAsync(Area? area, string? id)
    {
        if (!string.IsNullOrEmpty(id) && await quizzes.ReviewAsync(User.Id(), id, HttpContext.RequestAborted) is { } review)
        {
            (Review, Answers) = review;
            return;
        }

        Area = area is { } a && Enum.IsDefined(a) ? a : null;
        Tests = await quizzes.HistoryAsync(User.Id(), Area, ct: HttpContext.RequestAborted);
    }
}
