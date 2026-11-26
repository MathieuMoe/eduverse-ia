using EduVerse.Core.Gamification;
using EduVerse.Infrastructure.Gamification;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduVerse.Web.Pages.Student;

/// <summary>Roleplays: talk to a character to reach missions checked in code (the AI plays the character).</summary>
public sealed class RoleplayModel(RoleplayStore store) : PageModel
{
    public IReadOnlySet<string> Completed { get; private set; } = new HashSet<string>();

    public Roleplay? Chosen { get; private set; }

    public async Task OnGetAsync(string? scenario)
    {
        Completed = await store.CompletedAsync(User.Id(), HttpContext.RequestAborted);
        Chosen = Roleplays.Find(scenario);
    }
}
