using EduVerse.Infrastructure.Data;
using EduVerse.Infrastructure.Teaching;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduVerse.Web.Pages.Teacher;

/// <summary>Class follow-up: heatmap students × concepts, alerts from the tutor, frequent mistakes, one student's recent answers.</summary>
public sealed class ClassModel(ClassroomStore classrooms, EduVerse.Infrastructure.Learning.LearningStore store, EduVerse.Infrastructure.Tutor.DefenceStore defences, EduVerse.Infrastructure.Tutor.TeacherNoteStore teacherNotes, EduVerse.Infrastructure.Tutor.DetectiveStore detective, Microsoft.Extensions.Localization.IStringLocalizer<SharedResource> texts) : PageModel
{
    public ClassDetail Detail { get; private set; } = null!;

    public (string Name, IReadOnlyList<Attempt> Attempts)? Student { get; private set; }

    public string? StudentId { get; private set; }

    /// <summary>Topics each student defended with other numbers and an explanation (verified of asked).</summary>
    public IReadOnlyDictionary<string, (int Verified, int Total)> Defences { get; private set; } = new Dictionary<string, (int, int)>();

    public IReadOnlyList<TeacherNote> Notes { get; private set; } = [];

    /// <summary>What the Tutor found about this student: roots one step below and topics they wrongly feel sure about.</summary>
    public IReadOnlyList<string> Signals { get; private set; } = [];

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
        Defences = await defences.SummaryAsync([.. detail.Classroom.Members.Select(m => m.UserId)], HttpContext.RequestAborted);
        if (student is not null)
        {
            Student = await classrooms.StudentHistoryAsync(User.Id(), id, student, HttpContext.RequestAborted);
            StudentId = Student is null ? null : student;
            Notes = Student is null ? [] : await teacherNotes.OfTeacherAsync(User.Id(), student, HttpContext.RequestAborted);
            if (Student is not null)
            {
                var language = Localization.LanguageForCurrentCulture();
                var signals = (await detective.CasesAsync(student, HttpContext.RequestAborted)).Where(c => c.Verdict != EduVerse.Core.Tutor.RootVerdict.Check)
                    .Select(c => "🕵️ " + EduVerse.Core.Tutor.Prerequisites.Say(c.Verdict, QuizEndpoints.TopicName(c.Topic, texts), QuizEndpoints.TopicName(c.Below.Topic, texts), language)).ToList();
                signals.AddRange((await detective.FalseCertaintiesAsync(student, HttpContext.RequestAborted))
                    .Select(f => "😎 " + string.Format(System.Globalization.CultureInfo.CurrentCulture, texts["Certain_Teacher"].Value, QuizEndpoints.TopicName(f.Topic, texts), f.SureWrong, f.Sure)));
                Signals = signals;
            }
        }

        return Page();
    }

    public async Task<IActionResult> OnPostNoteAsync(int id, string student, string? text)
    {
        var outcome = await teacherNotes.AddAsync(User.Id(), student, text, HttpContext.RequestAborted);
        TempData["NoteResult"] = outcome.ToString();
        return Redirect($"/Teacher/Class?id={id}&student={Uri.EscapeDataString(student)}#student");
    }

    public async Task<IActionResult> OnPostNoteDeleteAsync(int id, string student, int note)
    {
        await teacherNotes.RemoveAsync(User.Id(), note, HttpContext.RequestAborted);
        return Redirect($"/Teacher/Class?id={id}&student={Uri.EscapeDataString(student)}#student");
    }

    public async Task<IActionResult> OnPostResolveAsync(int id, int alert)
    {
        await classrooms.ResolveAlertAsync(User.Id(), alert, HttpContext.RequestAborted);
        return RedirectToPage(new { id });
    }
}
