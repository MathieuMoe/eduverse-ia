using EduVerse.Core.Catalog;
using EduVerse.Infrastructure.Gamification;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduVerse.Web.Pages.Student;

/// <summary>"Review my steps": typed steps or a photo of the notebook; the tutor finds the first line where the reasoning breaks.</summary>
public sealed class HomeworkModel(GamificationStore game) : PageModel
{
    /// <summary>Young children cannot send photos (typed steps only).</summary>
    public bool Kids { get; private set; }

    public async Task OnGetAsync() =>
        Kids = Levels.IsKids((await game.ProfileAsync(User.Id(), HttpContext.RequestAborted)).Level);
}
