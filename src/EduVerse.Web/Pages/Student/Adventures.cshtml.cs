using EduVerse.Core.Catalog;
using EduVerse.Infrastructure.Data;
using EduVerse.Infrastructure.Learning;
using EduVerse.Infrastructure.Quizzes;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduVerse.Web.Pages.Student;

/// <summary>Choose an adventure (subject, level, theme) and see the adventures played.</summary>
public sealed class AdventuresModel(LearningStore learning, QuizStore quizzes) : PageModel
{
    public Level Level { get; private set; }

    public IReadOnlyList<SubjectProgress> Progress { get; private set; } = [];

    public IReadOnlyList<QuizAttempt> Played { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var profile = await learning.ProfileAsync(User.Id(), HttpContext.RequestAborted);
        Level = profile.Level;
        Progress = await quizzes.ProgressAsync(User.Id(), Level, HttpContext.RequestAborted);
        Played = (await quizzes.HistoryAsync(User.Id(), take: 200, ct: HttpContext.RequestAborted)).Where(a => a.Mode == QuizMode.Adventure).Take(12).ToList();
    }
}
