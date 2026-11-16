using EduVerse.Core.Common;
using EduVerse.Core.Marketplace;
using EduVerse.Infrastructure.Data;
using EduVerse.Infrastructure.Marketplace;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduVerse.Web.Pages.Tutor;

/// <summary>Marketplace profile of the private teacher: presentation, subjects, languages, stages, price, weekly availability.</summary>
public sealed class ProfileModel(MarketplaceStore store) : PageModel
{
    public static readonly DayOfWeek[] WeekOrder = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday];

    public TutorProfile Profile { get; private set; } = null!;

    public IReadOnlyList<string> Subjects { get; private set; } = [];

    public IReadOnlyList<TeachLanguage> Languages { get; private set; } = [];

    public IReadOnlyList<Stage> Stages { get; private set; } = [];

    public IReadOnlyList<WeeklySlot> Week { get; private set; } = [];

    public TimeZoneInfo Zone => store.Zone;

    [TempData]
    public string? Notice { get; set; }

    public async Task OnGetAsync()
    {
        Profile = await store.ProfileAsync(User.Id(), HttpContext.RequestAborted);
        Subjects = MarketplaceStore.Subjects(Profile);
        Languages = MarketplaceStore.Languages(Profile);
        Stages = MarketplaceStore.Stages(Profile);
        Week = MarketplaceStore.Week(Profile);
    }

    public async Task<IActionResult> OnPostAsync(string? headline, string? bio, string? subjects, decimal price, bool published,
        List<TeachLanguage> languages, List<Stage> stages, Dictionary<DayOfWeek, int?> from, Dictionary<DayOfWeek, int?> to)
    {
        var profile = await store.ProfileAsync(User.Id(), HttpContext.RequestAborted);
        profile.Headline = Clip(headline, 140);
        profile.Bio = Clip(bio, 1200);
        profile.PricePerHour = price;
        var subjectList = (subjects ?? "").Split([',', ';', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        var week = WeekOrder.Where(d => from?.GetValueOrDefault(d) is not null && to?.GetValueOrDefault(d) is not null)
            .Select(d => new WeeklySlot(d, from![d]!.Value, to![d]!.Value)).Where(w => w.IsValid).ToList();

        // Publishing needs what a family looks at first
        profile.Published = published && profile.Headline.Length > 0 && subjectList.Count > 0 && languages?.Count > 0 && week.Count > 0;
        await store.SaveProfileAsync(profile, subjectList, languages ?? [], stages ?? [], week, HttpContext.RequestAborted);
        Notice = published && !profile.Published ? "Profile_Incomplete" : "Profile_Saved";
        return RedirectToPage();
    }

    private static string Clip(string? text, int max)
    {
        var t = (text ?? "").Trim();
        return t.Length > max ? t[..max] : t;
    }
}
