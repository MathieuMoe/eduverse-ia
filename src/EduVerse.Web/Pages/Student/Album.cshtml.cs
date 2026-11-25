using EduVerse.Core.Tutor;
using EduVerse.Infrastructure.Tutor;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduVerse.Web.Pages.Student;

/// <summary>The figure album: a collectible card per historical figure, unlocked once the student completes its
/// interview (every suggested question discovered and the riddle answered). See <see cref="FigureDiscoveryStore"/>.
/// Also surfaces the curated figure-to-figure connections (see <see cref="Interviews.Connections"/>), each one
/// revealed once the student has completed both figures it links.</summary>
public sealed class AlbumModel(FigureDiscoveryStore discoveries) : PageModel
{
    public IReadOnlySet<string> Completed { get; private set; } = new HashSet<string>();

    public IReadOnlyList<(FigureConnection Connection, Figure A, Figure B, bool Unlocked)> Connections { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Completed = await discoveries.CompletedAsync(User.Id(), HttpContext.RequestAborted);
        Connections = [.. Interviews.Connections().Select(c => (
            c,
            Interviews.Find(c.FigureA)!,
            Interviews.Find(c.FigureB)!,
            Completed.Contains(c.FigureA) && Completed.Contains(c.FigureB)))];
    }
}
