using EduVerse.Core.Catalog;
using EduVerse.Infrastructure.Gamification;
using EduVerse.Infrastructure.Learning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduVerse.Web.Pages.Student;

/// <summary>Runs a test (practice, placement, duel or AI topic). The questions come one by one from /api/quiz.</summary>
public sealed class QuizModel(LearningStore learning, GamificationStore game, EduVerse.Infrastructure.Tutor.MaterialStore materials) : PageModel
{
    public Area Area { get; private set; }

    public Level Level { get; private set; }

    public QuizMode Mode { get; private set; }

    public string? Topic { get; private set; }

    /// <summary>The teacher's document this test is about (the questions come from /api/materials/{id}/quiz).</summary>
    public int? MaterialId { get; private set; }

    public int? DuelId { get; private set; }

    public string? Rival { get; private set; }

    public bool Kids => Levels.IsKids(Level);

    public Theme Theme { get; private set; }

    public EduVerse.Core.Tutor.Figure? Figure { get; private set; }

    /// <summary>The unit topic when this adventure is the boss of a unit of the learning path.</summary>
    public string? Unit { get; private set; }

    /// <summary>The unit boss is played as the legendary challenge (one heart, costs gems).</summary>
    public bool Legend { get; private set; }

    /// <summary>Where "continue the path" goes, when the test was started from the learning path.</summary>
    public string? PathUrl { get; private set; }

    /// <summary>Daily goal, happy hour and sound settings (header pill and sound toggle).</summary>
    public DailyGoalView Goal { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync(Area? area, Level? level, QuizMode? mode, string? topic, int? duel, Theme? theme, string? figure, int? material, string? unit = null, string? from = null, int? legend = null)
    {
        if (material is { } materialId)
        {
            var found = await materials.FindForStudentAsync(User.Id(), materialId, HttpContext.RequestAborted);
            if (found is null)
            {
                return RedirectToPage("/Student/Materials");
            }

            (MaterialId, mode, topic, area) = (materialId, QuizMode.Topic, found.Material.Title, Area.Language);
        }

        Figure = EduVerse.Core.Tutor.Interviews.Find(figure);
        if (mode == QuizMode.Interview && Figure is null)
        {
            return RedirectToPage("/Student/Interview");
        }

        if (Figure is not null)
        {
            area = Figure.Area;
        }
        Theme = theme is { } th && Enum.IsDefined(th) ? th : Theme.Space;
        Goal = await game.GoalAsync(User.Id(), HttpContext.RequestAborted);
        var profile = await learning.ProfileAsync(User.Id(), HttpContext.RequestAborted);
        Mode = mode is { } m && Enum.IsDefined(m) ? m : QuizMode.Practice;
        Topic = string.IsNullOrWhiteSpace(topic) ? null : topic.Trim()[..Math.Min(topic.Trim().Length, TopicQuizPrompt.MaxTopicLength)];
        if (mode == QuizMode.Focus)
        {
            Mode = QuizMode.Focus;
        }
        else if (Topic is not null)
        {
            Mode = QuizMode.Topic;
        }

        if (Mode == QuizMode.Duel)
        {
            var d = await game.DuelForAsync(User.Id(), duel ?? 0, HttpContext.RequestAborted);
            if (d is null)
            {
                return RedirectToPage("/Student/Arena");
            }

            (Area, Level, DuelId) = (d.Area, d.Level, d.Id);
            var views = await game.DuelsAsync(User.Id(), HttpContext.RequestAborted);
            Rival = views.FirstOrDefault(v => v.Duel.Id == d.Id)?.Rival;
            return Page();
        }

        Area = area is { } a && Enum.IsDefined(a) ? a : Area.Mathematics;
        Level = level is { } l && Enum.IsDefined(l) ? l : profile.Level;
        if (!Catalog.Available(Area, Level))
        {
            Level = Catalog.Info(Area).MinLevel;
        }

        if (Mode == QuizMode.Focus && (Topic is null || !EduVerse.Core.Curriculum.AreaSubjects.Topics(Area, Level).Contains(Topic)))
        {
            return RedirectToPage("/Student/Plan");
        }

        if (Mode == QuizMode.Adventure && unit is not null)
        {
            if (!EduVerse.Core.Curriculum.AreaSubjects.Topics(Area, Level).Contains(unit))
            {
                return RedirectToPage("/Student/Path", new { area = Area, level = Level });
            }

            (Unit, Theme, Legend) = (unit, Theme.Castle, legend == 1);
        }

        if (Unit is not null || (from == "path" && Mode == QuizMode.Focus))
        {
            PathUrl = $"/Student/Path?area={Area}&level={Level}#path-current";
        }

        return Page();
    }
}
