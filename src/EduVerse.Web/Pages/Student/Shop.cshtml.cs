using EduVerse.Core.Gamification;
using EduVerse.Infrastructure.Gamification;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduVerse.Web.Pages.Student;

/// <summary>The gem shop: streak freezes, a double-XP boost and avatar frames. Gems are only earned (quests, badges, daily goal).</summary>
public sealed class ShopModel(GamificationStore game) : PageModel
{
    public ShopView Shop { get; private set; } = null!;

    public int Streak { get; private set; }

    public string Avatar { get; private set; } = "🦊";

    [TempData]
    public string? Notice { get; set; }

    public async Task OnGetAsync()
    {
        Shop = await game.ShopAsync(User.Id(), HttpContext.RequestAborted);
        Streak = await game.StreakAsync(User.Id(), HttpContext.RequestAborted);
        Avatar = (await game.ProfileAsync(User.Id(), HttpContext.RequestAborted)).Avatar;
    }

    public async Task<IActionResult> OnPostBuyAsync(ShopItem item)
    {
        var outcome = await game.BuyAsync(User.Id(), item, HttpContext.RequestAborted);
        Notice = "Shop_" + outcome;
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostFrameAsync(string? frame)
    {
        await game.EquipFrameAsync(User.Id(), frame ?? "", HttpContext.RequestAborted);
        Notice = "Shop_Equipped";
        return RedirectToPage();
    }
}
