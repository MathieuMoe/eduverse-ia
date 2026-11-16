using EduVerse.Core.Curriculum;
using EduVerse.Infrastructure.Learning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduVerse.Web.Pages.Student;

/// <summary>The learning loop of one subject (driven by learn.js and the /api/learn endpoints).</summary>
public sealed class LearnModel(LearningStore store) : PageModel
{
    public IReadOnlyList<Subject> Subjects { get; private set; } = [];

    public Subject Current { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync(string? subject)
    {
        Subjects = await store.SubjectsForStudentAsync(User.Id(), Localization.LanguageForCurrentCulture(), HttpContext.RequestAborted);
        var found = Subjects.FirstOrDefault(s => s.Key == subject);
        if (subject is not null && found is null)
        {
            return RedirectToPage(new { subject = (string?)null });
        }

        Current = found ?? Subjects[0];
        return Page();
    }
}
