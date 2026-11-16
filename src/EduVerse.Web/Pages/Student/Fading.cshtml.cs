using EduVerse.Core.Tutor;
using EduVerse.Infrastructure.Gamification;
using EduVerse.Infrastructure.Tutor;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduVerse.Web.Pages.Student;

/// <summary>"Step by step": the Tutor solves a problem, then removes the help little by little until the student solves it alone.</summary>
public sealed class FadingModel(GamificationStore game, FadingStore fading) : PageModel
{
    public IReadOnlyList<FadingKind> Kinds { get; private set; } = [];

    public int Done { get; private set; }

    public async Task OnGetAsync()
    {
        var level = (await game.ProfileAsync(User.Id(), HttpContext.RequestAborted)).Level;
        Kinds = Fading.Available(level) ? Fading.Kinds(level) : [];
        Done = await fading.DoneAsync(User.Id(), HttpContext.RequestAborted);
    }
}
