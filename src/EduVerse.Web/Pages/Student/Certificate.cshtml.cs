using EduVerse.Core.Curriculum;
using EduVerse.Infrastructure.Data;
using EduVerse.Infrastructure.Learning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduVerse.Web.Pages.Student;

/// <summary>The printable internal certificate of a trade (only for its owner, only once earned).</summary>
public sealed class CertificateModel(CertificateStore certificates) : PageModel
{
    public Trade Trade { get; private set; } = null!;

    public Certificate Certificate { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync(string? subject)
    {
        var trade = Trades.Find(subject);
        var certificate = trade is null ? null : await certificates.FindAsync(User.Id(), trade.Key, HttpContext.RequestAborted);
        if (trade is null || certificate is null)
        {
            return Redirect("/Student/Learn?tab=" + (trade is null ? "trades" : Trades.TabOf(trade)));
        }

        (Trade, Certificate) = (trade, certificate);
        return Page();
    }
}
