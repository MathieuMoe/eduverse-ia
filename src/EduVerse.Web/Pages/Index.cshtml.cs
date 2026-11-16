using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduVerse.Web.Pages;

/// <summary>Landing page; signed-in users go straight to their space.</summary>
public sealed class IndexModel : PageModel
{
    public IActionResult OnGet() => User.Identity?.IsAuthenticated == true ? LocalRedirect(User.Home()) : Page();
}
