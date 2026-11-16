using EduVerse.Core.Tutor;
using EduVerse.Infrastructure.Data;
using EduVerse.Infrastructure.Gamification;
using EduVerse.Infrastructure.Tutor;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduVerse.Web.Pages.Student;

/// <summary>"Write it, say it": open answers measured by the code and judged on four criteria by the AI.</summary>
public sealed class WritingModel(GamificationStore game, WritingStore writing) : PageModel
{
    public bool Available { get; private set; }

    public IReadOnlyList<WritingTask> Tasks { get; private set; } = [];

    public IReadOnlyDictionary<string, WritingSubmission> Best { get; private set; } = new Dictionary<string, WritingSubmission>();

    public string? Selected { get; private set; }

    public async Task OnGetAsync(string? task)
    {
        Available = Writing.Available((await game.ProfileAsync(User.Id(), HttpContext.RequestAborted)).Level);
        Tasks = Writing.Tasks(Localization.LanguageForCurrentCulture());
        Best = await writing.BestAsync(User.Id(), HttpContext.RequestAborted);
        Selected = Tasks.Any(t => t.Key == task) ? task : null;
    }
}
