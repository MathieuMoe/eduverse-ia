using EduVerse.Core.Tutor;
using EduVerse.Infrastructure.Learning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduVerse.Web.Pages.Student;

/// <summary>The dossier of a historical figure: discovery, dates, historical evidence with links to the original sources.</summary>
public sealed class DossierModel(LearningStore learning) : PageModel
{
    public Figure Figure { get; private set; } = null!;

    public EduVerse.Core.Catalog.Level Level { get; private set; }

    public async Task<IActionResult> OnGetAsync(string? figure)
    {
        var f = Interviews.Find(figure);
        if (f is null || Dossiers.For(f.Key) is null)
        {
            return RedirectToPage("/Student/Interview");
        }

        Figure = f;
        Level = (await learning.ProfileAsync(User.Id(), HttpContext.RequestAborted)).Level;
        return Page();
    }
}
