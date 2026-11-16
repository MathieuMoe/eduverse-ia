using EduVerse.Core.Catalog;
using EduVerse.Infrastructure.Gamification;
using EduVerse.Infrastructure.Tutor;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduVerse.Web.Pages.Student;

/// <summary>Plan towards an exam: pick the subject, the date and the minutes a day; the tutor builds the plan and adjusts it every day.</summary>
public sealed class PlanModel(PlanStore plans, GamificationStore game) : PageModel
{
    public GoalView? Goal { get; private set; }

    public Level DefaultLevel { get; private set; }

    public DateOnly Today => plans.Today;

    public async Task OnGetAsync()
    {
        Goal = await plans.GetAsync(User.Id(), HttpContext.RequestAborted);
        DefaultLevel = (await game.ProfileAsync(User.Id(), HttpContext.RequestAborted)).Level;
    }
}
