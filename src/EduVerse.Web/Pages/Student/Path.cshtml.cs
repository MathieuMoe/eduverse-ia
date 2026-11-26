using EduVerse.Core.Catalog;
using EduVerse.Infrastructure.Gamification;
using EduVerse.Infrastructure.Learning;
using EduVerse.Infrastructure.Quizzes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduVerse.Web.Pages.Student;

/// <summary>The learning path of a subject (Duolingo-style): units of lessons, a chest and a boss, unlocked in order.</summary>
public sealed class PathModel(PathStore paths, QuizStore quizzes, LearningStore learning) : PageModel
{
    public PathView Path { get; private set; } = null!;

    public IReadOnlyList<SubjectProgress> Subjects { get; private set; } = [];

    [TempData]
    public string? Notice { get; set; }

    [TempData]
    public int? ChestGems { get; set; }

    public async Task OnGetAsync(Area? area, Level? level)
    {
        var profile = await learning.ProfileAsync(User.Id(), HttpContext.RequestAborted);
        Subjects = await quizzes.ProgressAsync(User.Id(), profile.Level, HttpContext.RequestAborted);
        var chosen = area is { } a && Enum.IsDefined(a) ? a : Subjects.Where(s => s.Answers > 0).OrderByDescending(s => s.Answers).Select(s => (Area?)s.Area).FirstOrDefault() ?? Area.Mathematics;
        var subjectLevel = level is { } lv && Enum.IsDefined(lv) ? lv : Subjects.FirstOrDefault(s => s.Area == chosen)?.Level ?? profile.Level;
        Path = await paths.PathAsync(User.Id(), chosen, subjectLevel, HttpContext.RequestAborted);
    }

    public async Task<IActionResult> OnPostChestAsync(Area area, Level level, string? topic)
    {
        var gems = Enum.IsDefined(area) && Enum.IsDefined(level) && !string.IsNullOrEmpty(topic)
            ? await paths.OpenChestAsync(User.Id(), area, level, topic, HttpContext.RequestAborted)
            : null;
        (Notice, ChestGems) = gems is { } g ? ("Chest_Opened", g) : ("Chest_Locked", (int?)null);
        return RedirectToPage(null, null, new { area, level }, "path-current");
    }
}
