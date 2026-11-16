using EduVerse.Core.Catalog;
using EduVerse.Infrastructure.Gamification;
using EduVerse.Infrastructure.Learning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduVerse.Web.Pages.Student;

/// <summary>Runs a test (practice, placement, duel or AI topic). The questions come one by one from /api/quiz.</summary>
public sealed class QuizModel(LearningStore learning, GamificationStore game) : PageModel
{
    public Area Area { get; private set; }

    public Level Level { get; private set; }

    public QuizMode Mode { get; private set; }

    public string? Topic { get; private set; }

    public int? DuelId { get; private set; }

    public string? Rival { get; private set; }

    public bool Kids => Levels.IsKids(Level);

    public Theme Theme { get; private set; }

    public EduVerse.Core.Tutor.Figure? Figure { get; private set; }

    public async Task<IActionResult> OnGetAsync(Area? area, Level? level, QuizMode? mode, string? topic, int? duel, Theme? theme, string? figure)
    {
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
        var profile = await learning.ProfileAsync(User.Id(), HttpContext.RequestAborted);
        Mode = mode is { } m && Enum.IsDefined(m) ? m : QuizMode.Practice;
        Topic = string.IsNullOrWhiteSpace(topic) ? null : topic.Trim()[..Math.Min(topic.Trim().Length, TopicQuizPrompt.MaxTopicLength)];
        if (Topic is not null)
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

        return Page();
    }
}
