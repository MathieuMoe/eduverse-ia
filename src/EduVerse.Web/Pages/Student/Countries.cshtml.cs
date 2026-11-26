using EduVerse.Core.Gamification;
using EduVerse.Infrastructure.Engines;
using EduVerse.Infrastructure.Gamification;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduVerse.Web.Pages.Student;

/// <summary>Trips through the history of countries: a timeline of eras (story, main figures, weird facts) walked in order.</summary>
public sealed class CountriesModel(CountryStore store, AiEngines engines) : PageModel
{
    /// <summary>The demo engine is selected: a new country needs a real AI engine, so the form explains it instead of failing.</summary>
    public bool Offline { get; private set; }

    public IReadOnlyList<CountryHistory> Custom { get; private set; } = [];

    public IReadOnlyDictionary<string, int> Progress { get; private set; } = new Dictionary<string, int>();

    public CountryView? Chosen { get; private set; }

    public IReadOnlySet<string> Gold { get; private set; } = new HashSet<string>();

    /// <summary>The eras of the chosen country shuffled for the timeline challenge.</summary>
    public IReadOnlyList<HistoryEra> Shuffled { get; private set; } = [];

    public bool Missing { get; private set; }

    public async Task OnGetAsync(string? country)
    {
        var userId = User.Id();
        Offline = EngineSelection.Current(HttpContext, engines).IsDemo;
        Progress = await store.ProgressAsync(userId, HttpContext.RequestAborted);
        Custom = await store.CustomAsync(userId, HttpContext.RequestAborted);
        Gold = await store.GoldAsync(userId, HttpContext.RequestAborted);
        if (!string.IsNullOrEmpty(country))
        {
            var found = await store.FindAsync(userId, country, HttpContext.RequestAborted);
            Chosen = found is null ? null : await store.ViewAsync(userId, found, HttpContext.RequestAborted);
            Missing = found is null;
            Shuffled = found is null ? [] : CountryOrder.Shuffle(found, Random.Shared.Next());
        }
    }

    public int Passed(CountryHistory c) => Progress.TryGetValue(c.Key, out var n) ? Math.Min(n, c.Eras.Count) : 0;
}
