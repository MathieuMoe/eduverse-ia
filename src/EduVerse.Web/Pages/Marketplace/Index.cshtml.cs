using EduVerse.Core.Common;
using EduVerse.Core.Marketplace;
using EduVerse.Infrastructure.Marketplace;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduVerse.Web.Pages.Marketplace;

/// <summary>Private teachers: search with filters, and recommendations for a signed-in student (computed in code, with reasons).</summary>
public sealed class IndexModel(MarketplaceStore store) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? Subject { get; set; }

    [BindProperty(SupportsGet = true)]
    public TeachLanguage? Language { get; set; }

    [BindProperty(SupportsGet = true)]
    public Stage? Stage { get; set; }

    [BindProperty(SupportsGet = true)]
    public decimal? MaxPrice { get; set; }

    public IReadOnlyList<TutorCard> Tutors { get; private set; } = [];

    public IReadOnlyList<TutorMatchResult> Recommended { get; private set; } = [];

    public bool Filtered => !string.IsNullOrWhiteSpace(Subject) || Language is not null || Stage is not null || MaxPrice is not null;

    public async Task OnGetAsync()
    {
        Tutors = await store.TutorsAsync(new TutorFilter(Subject?.Trim(), Language, Stage, MaxPrice), HttpContext.RequestAborted);
        if (User.IsStudent() && !Filtered)
        {
            Recommended = await store.RecommendAsync(User.Id(), Localization.LanguageForCurrentCulture(), HttpContext.RequestAborted);
        }
    }
}
