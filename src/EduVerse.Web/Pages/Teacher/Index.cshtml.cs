using EduVerse.Core.Common;
using EduVerse.Core.Curriculum;
using EduVerse.Core.Teaching;
using EduVerse.Infrastructure.Learning;
using EduVerse.Infrastructure.Teaching;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduVerse.Web.Pages.Teacher;

/// <summary>Teacher dashboard: classes with their join code and open alerts, new class, new subject from a topic.</summary>
public sealed class IndexModel(ClassroomStore classrooms, LearningStore store, EduVerse.Infrastructure.Institutions.InstitutionStore institutions) : PageModel
{
    public IReadOnlyList<ClassSummaryRow> Classes { get; private set; } = [];

    public IReadOnlyList<Subject> Subjects { get; private set; } = [];

    public ActivitySummary Activity { get; private set; } = null!;

    public int Students { get; private set; }

    public string? InstitutionName { get; private set; }

    [TempData]
    public string? Notice { get; set; }

    public async Task OnGetAsync()
    {
        var language = Localization.LanguageForCurrentCulture();
        Classes = await classrooms.ClassesAsync(User.Id(), language, HttpContext.RequestAborted);
        Subjects = await store.SubjectsForTeacherAsync(User.Id(), language, HttpContext.RequestAborted);
        var students = await classrooms.StudentIdsAsync(User.Id(), null, HttpContext.RequestAborted);
        Students = students.Count;
        Activity = await store.ActivityAsync([.. students], null, HttpContext.RequestAborted);
        InstitutionName = (await institutions.ForUserAsync(User.Id(), HttpContext.RequestAborted))?.Name;
    }

    public async Task<IActionResult> OnPostJoinInstitutionAsync(string? code)
    {
        Notice = await institutions.JoinAsTeacherAsync(User.Id(), code, HttpContext.RequestAborted) is null ? "Institution_Unknown" : "Institution_Joined";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCreateAsync(string name, string subject)
    {
        var classroom = await classrooms.CreateAsync(User.Id(), name, subject, HttpContext.RequestAborted);
        if (classroom is null)
        {
            Notice = "Class_Invalid";
            return RedirectToPage();
        }

        return RedirectToPage("/Teacher/Class", new { id = classroom.Id });
    }
}
