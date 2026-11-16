using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduVerse.Web.Pages.Student;

/// <summary>Old address of the trades: they are now a tab of Learn.</summary>
public sealed class TradesModel : PageModel
{
    public IActionResult OnGet() => RedirectPermanent("/Student/Learn?tab=trades");
}
