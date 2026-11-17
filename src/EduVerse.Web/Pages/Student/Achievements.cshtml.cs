using EduVerse.Core.Catalog;
using EduVerse.Infrastructure.Gamification;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduVerse.Web.Pages.Student;

/// <summary>Levels and titles, badges with their progress, and the quests of the day.</summary>
public sealed class AchievementsModel(GamificationStore game) : PageModel
{
    public GameSummary Game { get; private set; } = null!;

    /// <summary>True for a young child (see <see cref="Levels.IsKids"/>): badges become a sticker shelf,
    /// no XP thresholds or rank ladder to read.</summary>
    public bool Kids => Levels.IsKids(Game.Profile.Level);

    public async Task OnGetAsync() => Game = await game.SummaryAsync(User.Id(), HttpContext.RequestAborted);
}
