using EduVerse.Core.Tutor;
using EduVerse.Infrastructure.Data;
using EduVerse.Infrastructure.Teaching;
using EduVerse.Infrastructure.Tutor;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace EduVerse.Web.Pages.Teacher;

/// <summary>The measured progress of one student of a class, to print or show to the family: before and now, topic by topic, and the work set.</summary>
public sealed class ReportModel(EduVerseDb db, ClassroomStore classrooms, AssignmentStore assignments, DetectiveStore detective, IStringLocalizer<SharedResource> l) : PageModel
{
    public string ClassName { get; private set; } = "";

    public int ClassId { get; private set; }

    public ProgressRow Row { get; private set; } = null!;

    public IReadOnlyList<(string Topic, int? Before, int? Now, int Answers)> Topics { get; private set; } = [];

    public IReadOnlyList<(ClassAssignment Assignment, WorkStatus Status)> Work { get; private set; } = [];

    public IReadOnlyList<string> Signals { get; private set; } = [];

    public DateOnly Today => assignments.Today;

    public async Task<IActionResult> OnGetAsync(int id, string? student)
    {
        var teacherId = User.Id();
        var classroom = await db.Classrooms.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id && c.TeacherId == teacherId, HttpContext.RequestAborted);
        var row = classroom is null || student is null || !(await classrooms.StudentIdsAsync(teacherId, id, HttpContext.RequestAborted)).Contains(student)
            ? null : (await assignments.ProgressAsync(teacherId, id, HttpContext.RequestAborted))?.FirstOrDefault(r => r.StudentId == student);
        if (classroom is null || row is null)
        {
            return RedirectToPage("/Teacher/Index");
        }

        (ClassName, ClassId, Row) = (classroom.Name, id, row);
        Topics = (await assignments.TopicsAsync(student!, HttpContext.RequestAborted)).Select(t => (QuizEndpoints.TopicName(t.Topic, l), t.PercentBefore, t.PercentNow, t.AnswersNow)).ToList();
        Work = (await assignments.ForClassAsync(teacherId, id, HttpContext.RequestAborted) ?? []).Select(a => (a, a.ByStudent[student!])).ToList();
        var language = Localization.LanguageForCurrentCulture();
        var signals = (await detective.CasesAsync(student!, HttpContext.RequestAborted)).Where(c => c.Verdict != RootVerdict.Check)
            .Select(c => Prerequisites.Say(c.Verdict, QuizEndpoints.TopicName(c.Topic, l), QuizEndpoints.TopicName(c.Below.Topic, l), language)).ToList();
        signals.AddRange((await detective.FalseCertaintiesAsync(student!, HttpContext.RequestAborted))
            .Select(f => string.Format(System.Globalization.CultureInfo.CurrentCulture, l["Certain_Teacher"].Value, QuizEndpoints.TopicName(f.Topic, l), f.SureWrong, f.Sure)));
        Signals = signals;
        return Page();
    }
}
