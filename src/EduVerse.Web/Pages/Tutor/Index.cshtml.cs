using EduVerse.Core.Marketplace;
using EduVerse.Infrastructure.Data;
using EduVerse.Infrastructure.Marketplace;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduVerse.Web.Pages.Tutor;

/// <summary>Private teacher dashboard: earnings after commission, upcoming lessons, reviews.</summary>
public sealed class IndexModel(MarketplaceStore store) : PageModel
{
    public Earnings Earnings { get; private set; } = null!;

    public IReadOnlyList<BookingView> Bookings { get; private set; } = [];

    public IReadOnlyList<(int Rating, string? Review, string Student, DateTimeOffset Start)> Reviews { get; private set; } = [];

    public TutorCard? Card { get; private set; }

    public TutorProfile Profile { get; private set; } = null!;

    public TimeZoneInfo Zone => store.Zone;

    [TempData]
    public string? Notice { get; set; }

    public async Task OnGetAsync()
    {
        var id = User.Id();
        Profile = await store.ProfileAsync(id, HttpContext.RequestAborted);
        Earnings = await store.EarningsAsync(id, HttpContext.RequestAborted);
        Bookings = await store.ForTutorAsync(id, HttpContext.RequestAborted);
        Reviews = await store.ReviewsAsync(id, 4, HttpContext.RequestAborted);
        Card = (await store.TutorsAsync(null, HttpContext.RequestAborted)).FirstOrDefault(t => t.TutorId == id);
    }

    public async Task<IActionResult> OnPostCancelAsync(int id)
    {
        Notice = await store.CancelAsync(User.Id(), id, HttpContext.RequestAborted) ? "Cancel_DoneTutor" : "Action_Failed";
        return RedirectToPage();
    }
}
