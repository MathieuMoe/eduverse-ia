using EduVerse.Core.Catalog;
using EduVerse.Infrastructure.Learning;
using EduVerse.Infrastructure.Quizzes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduVerse.Web.Pages.Student;

/// <summary>Catalog of tests: every subject from early years to doctorate, with the student's level and weak topics.</summary>
public sealed class ExploreModel(LearningStore learning, QuizStore quizzes) : PageModel
{
    public Level Level { get; private set; }

    public Level ProfileLevel { get; private set; }

    public IReadOnlyList<SubjectProgress> Progress { get; private set; } = [];

    public async Task OnGetAsync(Level? level)
    {
        var profile = await learning.ProfileAsync(User.Id(), HttpContext.RequestAborted);
        ProfileLevel = profile.Level;
        Level = level is { } l && Enum.IsDefined(l) ? l : profile.Level;
        Progress = await quizzes.ProgressAsync(User.Id(), Level, HttpContext.RequestAborted);
    }

    public bool Kids => Levels.IsKids(Level);

    /// <summary>Level of the tests of a subject at the level shown (subjects that start later use their first level).</summary>
    public Level TestLevel(Area area) => Catalog.Available(area, Level) ? Level : Catalog.Info(area).MinLevel;

    [TempData]
    public string? Notice { get; set; }

    /// <summary>Makes the level shown the level of study of the student, for every subject.</summary>
    public async Task<IActionResult> OnPostApplyAsync(Level level)
    {
        if (!Enum.IsDefined(level))
        {
            return RedirectToPage();
        }

        var profile = await learning.ProfileAsync(User.Id(), HttpContext.RequestAborted);
        (profile.Level, profile.Stage) = (level, Levels.ToStage(level));
        await learning.SaveProfileAsync(profile, HttpContext.RequestAborted);
        await quizzes.ApplyLevelToAllAsync(User.Id(), level, HttpContext.RequestAborted);
        Notice = "Level_Applied";
        return RedirectToPage(new { level });
    }
}
