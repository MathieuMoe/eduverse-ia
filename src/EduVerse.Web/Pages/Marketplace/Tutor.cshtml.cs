using System.Globalization;
using EduVerse.Core.Marketplace;
using EduVerse.Infrastructure.Data;
using EduVerse.Infrastructure.Marketplace;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduVerse.Web.Pages.Marketplace;

/// <summary>Profile of a private teacher, free slots of the next 2 weeks and booking (students only).</summary>
public sealed class TutorModel(MarketplaceStore store) : PageModel
{
    public TutorCard Tutor { get; private set; } = null!;

    public TutorProfile Profile { get; private set; } = null!;

    public IReadOnlyList<IGrouping<DateOnly, DateTimeOffset>> Days { get; private set; } = [];

    public IReadOnlyList<(int Rating, string? Review, string Student, DateTimeOffset Start)> Reviews { get; private set; } = [];

    public TimeZoneInfo Zone => store.Zone;

    [TempData]
    public string? Notice { get; set; }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        var card = (await store.TutorsAsync(null, HttpContext.RequestAborted)).FirstOrDefault(t => t.TutorId == id);
        if (card is null)
        {
            return RedirectToPage("/Marketplace/Index");
        }

        Tutor = card;
        Profile = await store.ProfileAsync(id, HttpContext.RequestAborted);
        var slots = await store.SlotsAsync(id, HttpContext.RequestAborted);
        Days = slots.GroupBy(s => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(s, Zone).DateTime)).ToList();
        Reviews = await store.ReviewsAsync(id, 5, HttpContext.RequestAborted);
        return Page();
    }

    public async Task<IActionResult> OnPostBookAsync(string id, string start, string subject, string? note)
    {
        if (!User.IsStudent())
        {
            return User.Identity?.IsAuthenticated == true ? Forbid() : Challenge();
        }

        if (!DateTimeOffset.TryParse(start, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var when))
        {
            Notice = "Book_Invalid";
            return RedirectToPage(new { id });
        }

        var (result, booking) = await store.BookAsync(User.Id(), id, when, subject, note, HttpContext.RequestAborted);
        if (result != BookResult.Booked)
        {
            Notice = result == BookResult.SlotTaken ? "Book_Taken" : "Book_Invalid";
            return RedirectToPage(new { id });
        }

        return RedirectToPage("/Marketplace/Bookings", new { booked = booking!.Id });
    }
}
