using EduVerse.Core.Catalog;
using EduVerse.Core.Gamification;
using EduVerse.Infrastructure.Data;
using EduVerse.Infrastructure.Gamification;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduVerse.Web.Pages.Student;

/// <summary>Weekly rankings of the class, teams, class challenge and duels between classmates (nicknames only).</summary>
public sealed class ArenaModel(GamificationStore game) : PageModel
{
    public StudentProfile Profile { get; private set; } = null!;

    public IReadOnlyList<ClassBoard> Boards { get; private set; } = [];

    public IReadOnlyList<RankEntry> Rivals { get; private set; } = [];

    public IReadOnlyList<DuelView> Duels { get; private set; } = [];

    [TempData]
    public string? Notice { get; set; }

    public async Task OnGetAsync()
    {
        var userId = User.Id();
        Profile = await game.ProfileAsync(userId, HttpContext.RequestAborted);
        Boards = await game.BoardsAsync(userId, HttpContext.RequestAborted);
        Rivals = await game.RivalsAsync(userId, HttpContext.RequestAborted);
        Duels = await game.DuelsAsync(userId, HttpContext.RequestAborted);
    }

    public async Task<IActionResult> OnPostSettingsAsync(Team team, bool compete, bool newNickname)
    {
        await game.SaveSettingsAsync(User.Id(), team, compete, newNickname, HttpContext.RequestAborted);
        Notice = "Arena_Saved";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostChallengeAsync(string opponent, Area area, Level level)
    {
        var (result, duel) = await game.ChallengeAsync(User.Id(), opponent ?? "", area, level, HttpContext.RequestAborted);
        if (result != GamificationStore.ChallengeResult.Created)
        {
            Notice = $"Duel_{result}";
            return RedirectToPage();
        }

        return Redirect($"/Student/Quiz?mode=Duel&duel={duel!.Id}");
    }
}
