using EduVerse.Infrastructure.Gamification;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduVerse.Web.Pages.Student;

/// <summary>The weekly league: the group's table with the promotion and demotion zones, and how last week ended.</summary>
public sealed class LeagueModel(LeagueStore leagues, FriendQuestStore friends) : PageModel
{
    public LeagueView? League { get; private set; }

    public TimeSpan Left { get; private set; }

    /// <summary>This week's friend quest, or the classmates to invite (phase 7).</summary>
    public FriendQuestView? Quest { get; private set; }

    public IReadOnlyList<FriendView> Candidates { get; private set; } = [];

    [TempData]
    public string? Notice { get; set; }

    public async Task OnGetAsync()
    {
        League = await leagues.ViewAsync(User.Id(), HttpContext.RequestAborted);
        if (League is not null)
        {
            Left = League.EndsAt - DateTimeOffset.UtcNow;
            if (League.ShowResult)
            {
                await leagues.MarkResultSeenAsync(User.Id(), HttpContext.RequestAborted);
            }

            Quest = await friends.AutoPairAsync(User.Id(), HttpContext.RequestAborted);
            Candidates = Quest is null ? await friends.CandidatesAsync(User.Id(), HttpContext.RequestAborted) : [];
        }
    }

    public async Task<IActionResult> OnPostFriendAsync(string? friend)
    {
        var outcome = await friends.StartAsync(User.Id(), friend ?? "", HttpContext.RequestAborted);
        Notice = "Friend_" + outcome;
        return RedirectToPage(null, null, null, "friend-quest");
    }

    public async Task<IActionResult> OnPostClaimAsync()
    {
        Notice = await friends.ClaimAsync(User.Id(), HttpContext.RequestAborted) is null ? "Friend_NotYet" : "Friend_Claimed";
        return RedirectToPage(null, null, null, "friend-quest");
    }
}
