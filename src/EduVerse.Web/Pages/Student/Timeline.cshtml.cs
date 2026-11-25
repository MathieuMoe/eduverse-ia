using EduVerse.Core.Tutor;
using EduVerse.Infrastructure.Tutor;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduVerse.Web.Pages.Student;

/// <summary>Chronological timeline mini-game: put the figures the student has completed (see
/// <see cref="FigureDiscoveryStore"/>) in birth order. Client-side only — the check is honour-system, like the
/// curiosity meter, since the years are already shown elsewhere in the app.</summary>
public sealed class TimelineModel(FigureDiscoveryStore discoveries) : PageModel
{
    public IReadOnlyList<Figure> Figures { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var completed = await discoveries.CompletedAsync(User.Id(), HttpContext.RequestAborted);
        Figures = [.. Interviews.All.Where(f => completed.Contains(f.Key))];
    }
}
