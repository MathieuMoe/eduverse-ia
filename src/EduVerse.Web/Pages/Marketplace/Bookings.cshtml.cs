using EduVerse.Infrastructure.Marketplace;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduVerse.Web.Pages.Marketplace;

/// <summary>The student's lessons: simulated payment, cancellation (refund 24 h before or more), rating after the lesson.</summary>
public sealed class BookingsModel(MarketplaceStore store) : PageModel
{
    public IReadOnlyList<BookingView> Bookings { get; private set; } = [];

    public TimeZoneInfo Zone => store.Zone;

    [BindProperty(SupportsGet = true)]
    public int? Booked { get; set; }

    [TempData]
    public string? Notice { get; set; }

    public async Task OnGetAsync() => Bookings = await store.ForStudentAsync(User.Id(), HttpContext.RequestAborted);

    public async Task<IActionResult> OnPostPayAsync(int id)
    {
        Notice = await store.PayAsync(User.Id(), id, HttpContext.RequestAborted) ? "Pay_Done" : "Action_Failed";
        return RedirectToPage(new { booked = (int?)null });
    }

    public async Task<IActionResult> OnPostCancelAsync(int id)
    {
        Notice = await store.CancelAsync(User.Id(), id, HttpContext.RequestAborted) ? "Cancel_Done" : "Action_Failed";
        return RedirectToPage(new { booked = (int?)null });
    }

    public async Task<IActionResult> OnPostRateAsync(int id, int rating, string? review)
    {
        Notice = await store.RateAsync(User.Id(), id, rating, review, HttpContext.RequestAborted) ? "Rate_Done" : "Action_Failed";
        return RedirectToPage(new { booked = (int?)null });
    }
}
