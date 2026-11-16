using EduVerse.Core.Catalog;
using EduVerse.Core.Tutor;
using EduVerse.Infrastructure.Data;
using EduVerse.Infrastructure.Tutor;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EduVerse.Web.Pages.Teacher;

/// <summary>Documents and work of a class: what the Tutor may use, what the students must do, and the measured progress of each one.</summary>
public sealed class CourseModel(EduVerseDb db, MaterialStore materials, AssignmentStore assignments) : PageModel
{
    public Classroom Classroom { get; private set; } = null!;

    public IReadOnlyList<Material> Materials { get; private set; } = [];

    public IReadOnlyList<ClassAssignment> Assignments { get; private set; } = [];

    public IReadOnlyList<ProgressRow> Progress { get; private set; } = [];

    public Area Area { get; private set; }

    public Level Level { get; private set; }

    public IReadOnlyList<string> Topics { get; private set; } = [];

    public DateOnly Today => assignments.Today;

    public async Task<IActionResult> OnGetAsync(int id, Area? area, Level? level)
    {
        var classroom = await db.Classrooms.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id && c.TeacherId == User.Id(), HttpContext.RequestAborted);
        if (classroom is null)
        {
            return RedirectToPage("/Teacher/Index");
        }

        Classroom = classroom;
        Area = area is { } a && Enum.IsDefined(a) ? a : Area.Mathematics;
        Level = level is { } l && Enum.IsDefined(l) && Catalog.Available(Area, l) ? l : Catalog.Available(Area, Level.Primary3) ? Level.Primary3 : Catalog.Info(Area).MinLevel;
        Topics = EduVerse.Core.Curriculum.AreaSubjects.Topics(Area, Level);
        Materials = await materials.OfClassAsync(User.Id(), id, HttpContext.RequestAborted);
        Assignments = await assignments.ForClassAsync(User.Id(), id, HttpContext.RequestAborted) ?? [];
        Progress = await assignments.ProgressAsync(User.Id(), id, HttpContext.RequestAborted) ?? [];
        return Page();
    }

    public async Task<IActionResult> OnPostMaterialAsync(int id, MaterialTarget target, string? key, string? title, string? text)
    {
        TempData["MaterialResult"] = (await materials.AddAsync(User.Id(), id, target, key, title, text, HttpContext.RequestAborted)).ToString();
        return Redirect($"/Teacher/Course?id={id}#materials");
    }

    public async Task<IActionResult> OnPostMaterialDeleteAsync(int id, int material)
    {
        await materials.RemoveAsync(User.Id(), material, HttpContext.RequestAborted);
        return Redirect($"/Teacher/Course?id={id}#materials");
    }

    public async Task<IActionResult> OnPostAssignAsync(int id, AssignmentKind kind, Area area, Level level, string? topic, string? writing, int? material, DateOnly due)
    {
        TempData["AssignResult"] = (await assignments.CreateAsync(User.Id(), id, kind, area, level, topic, writing, material, due, HttpContext.RequestAborted)).ToString();
        return Redirect($"/Teacher/Course?id={id}#assignments");
    }

    public async Task<IActionResult> OnPostAssignDeleteAsync(int id, int assignment)
    {
        await assignments.RemoveAsync(User.Id(), assignment, HttpContext.RequestAborted);
        return Redirect($"/Teacher/Course?id={id}#assignments");
    }
}
