using EduVerse.Infrastructure.Data;
using EduVerse.Infrastructure.Teaching;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduVerse.Web.Pages.Teacher;

/// <summary>Class follow-up: heatmap students × concepts, alerts from the tutor, frequent mistakes, one student's recent answers.</summary>
public sealed class ClassModel(ClassroomStore classrooms, EduVerse.Infrastructure.Learning.LearningStore store) : PageModel
{
    public ClassDetail Detail { get; private set; } = null!;

    public (string Name, IReadOnlyList<Attempt> Attempts)? Student { get; private set; }

    public string? StudentId { get; private set; }

    public EduVerse.Core.Teaching.ActivitySummary Activity { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id, string? student)
    {
        var detail = await classrooms.DetailAsync(User.Id(), id, Localization.LanguageForCurrentCulture(), HttpContext.RequestAborted);
        if (detail is null)
        {
            return RedirectToPage("/Teacher/Index");
        }

        Detail = detail;
        Activity = await store.ActivityAsync([.. detail.Classroom.Members.Select(m => m.UserId)], detail.Classroom.SubjectId, HttpContext.RequestAborted);
        if (student is not null)
        {
            Student = await classrooms.StudentHistoryAsync(User.Id(), id, student, HttpContext.RequestAborted);
            StudentId = Student is null ? null : student;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostResolveAsync(int id, int alert)
    {
        await classrooms.ResolveAlertAsync(User.Id(), alert, HttpContext.RequestAborted);
        return RedirectToPage(new { id });
    }
}
