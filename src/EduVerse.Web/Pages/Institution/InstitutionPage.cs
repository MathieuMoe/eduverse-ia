using EduVerse.Infrastructure.Institutions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduVerse.Web.Pages.Institution;

/// <summary>Base of the director pages: the institution of the signed-in director and its dashboard (computed by the Core rules).</summary>
public abstract class InstitutionPage(InstitutionStore store) : PageModel
{
    public InstitutionDashboard Dashboard { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync()
    {
        var institution = await store.ForUserAsync(User.Id(), HttpContext.RequestAborted);
        if (institution is null)
        {
            return RedirectToPage("/Index");
        }

        Dashboard = await store.DashboardAsync(institution, Localization.LanguageForCurrentCulture(), HttpContext.RequestAborted);
        return Page();
    }
}

public sealed class IndexModel(InstitutionStore store) : InstitutionPage(store);

public sealed class RiskModel(InstitutionStore store) : InstitutionPage(store)
{
    [BindProperty(SupportsGet = true)]
    public string? Level { get; set; }
}

public sealed class QualityModel(InstitutionStore store) : InstitutionPage(store);
