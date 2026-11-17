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

    /// <summary>The animal option (see <see cref="Nicknames"/>) behind the current avatar, for pre-selecting the picker.</summary>
    public int CurrentAnimal => Nicknames.AnimalIndexOf(Profile.Avatar);

    /// <summary>The word option (see <see cref="Nicknames"/>) behind the current nickname, for pre-selecting the picker.</summary>
    public int CurrentWord => Nicknames.WordIndexOf(Profile.Nickname, Profile.Language);

    /// <summary>Animal choices for the nickname/avatar picker: (index, emoji, localized label).</summary>
    public IEnumerable<(int Index, string Emoji, string Label)> AnimalOptions =>
        Enumerable.Range(0, Nicknames.AnimalCount).Select(i => (i, Nicknames.AnimalEmoji(i), Nicknames.AnimalLabel(i, Profile.Language)));

    /// <summary>Word choices for the nickname/avatar picker: (index, localized label).</summary>
    public IEnumerable<(int Index, string Label)> WordOptions =>
        Enumerable.Range(0, Nicknames.WordCount).Select(i => (i, Nicknames.WordLabel(i, Profile.Language)));

    public async Task OnGetAsync()
    {
        var userId = User.Id();
        Profile = await game.ProfileAsync(userId, HttpContext.RequestAborted);
        Boards = await game.BoardsAsync(userId, HttpContext.RequestAborted);
        Rivals = await game.RivalsAsync(userId, HttpContext.RequestAborted);
        Duels = await game.DuelsAsync(userId, HttpContext.RequestAborted);
    }

    public async Task<IActionResult> OnPostSettingsAsync(Team team, bool compete, bool newNickname, int? animal, int? word)
    {
        await game.SaveSettingsAsync(User.Id(), team, compete, newNickname, animal, word, HttpContext.RequestAborted);
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
