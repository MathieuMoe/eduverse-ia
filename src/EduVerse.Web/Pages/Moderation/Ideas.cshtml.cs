using EduVerse.Core.Ideas;
using EduVerse.Infrastructure.Ideas;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduVerse.Web.Pages.Moderation;

/// <summary>Ideas of the students and ratings of the adventures, for teachers and directors.</summary>
public sealed class IdeasModel(IdeaStore ideas) : PageModel
{
    public IdeaKind? Kind { get; private set; }

    public IReadOnlyList<IdeaRow> Rows { get; private set; } = [];

    public IReadOnlyList<ThemeRating> Ratings { get; private set; } = [];

    public bool Saved { get; private set; }

    public async Task OnGetAsync(IdeaKind? kind, bool saved = false)
    {
        Kind = kind;
        Saved = saved;
        Rows = await ideas.AllAsync(kind, ct: HttpContext.RequestAborted);
        Ratings = await ideas.AdventureRatingsAsync(HttpContext.RequestAborted);
    }

    public async Task<IActionResult> OnPostStatusAsync(int id, IdeaStatus status, IdeaKind? kind)
    {
        if (!Enum.IsDefined(status) || !await ideas.SetStatusAsync(id, status, HttpContext.RequestAborted))
        {
            return BadRequest();
        }

        return RedirectToPage(new { kind, saved = true });
    }
}
