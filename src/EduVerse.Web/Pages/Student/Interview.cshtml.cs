using EduVerse.Core.Tutor;
using EduVerse.Infrastructure.Learning;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduVerse.Web.Pages.Student;

/// <summary>Interviews with historical figures (a simulation from documented facts).</summary>
public sealed class InterviewModel(LearningStore learning) : PageModel
{
    public Figure? Figure { get; private set; }

    public EduVerse.Core.Catalog.Level Level { get; private set; }

    public async Task OnGetAsync(string? figure)
    {
        Figure = Interviews.Find(figure);
        Level = (await learning.ProfileAsync(User.Id(), HttpContext.RequestAborted)).Level;
    }
}
