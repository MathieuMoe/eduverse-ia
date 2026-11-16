using EduVerse.Core.Catalog;
using EduVerse.Core.Curriculum;
using EduVerse.Core.Learning;
using EduVerse.Infrastructure.Learning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduVerse.Web.Pages.Student;

/// <summary>A subject of the catalog, with the progress of the student in it.</summary>
public sealed record LearnChoice(Subject Subject, string Icon, string Tone, int Mastered, int Total, DateTimeOffset? LastAt, Level? Level = null);

/// <summary>Without a subject: the catalog to choose what to learn. With one: the learning loop (learn.js and /api/learn).</summary>
public sealed class LearnModel(LearningStore store, ICatalogNames names, CertificateStore certificates) : PageModel
{
    public IReadOnlyList<Subject> Subjects { get; private set; } = [];

    public Subject? Current { get; private set; }

    public IReadOnlyList<LearnChoice> Areas { get; private set; } = [];

    public IReadOnlyList<LearnChoice> Own { get; private set; } = [];

    public IReadOnlyList<LearnChoice> TradeChoices { get; private set; } = [];

    public LearnChoice? Fractions { get; private set; }

    /// <summary>Tab of the catalog: "subjects", "trades" or "hobbies".</summary>
    public string Tab { get; private set; } = "subjects";

    public IReadOnlyList<TradeProgress> TradeProgress { get; private set; } = [];

    public IReadOnlyList<LearnChoice> Started => [.. new[] { Fractions! }.Concat(Areas).Concat(Own).Concat(TradeChoices).Where(c => c is not null && c.LastAt is not null).OrderByDescending(c => c.LastAt).Take(4)];

    public async Task<IActionResult> OnGetAsync(string? subject, string? tab)
    {
        Tab = tab is "trades" or "hobbies" ? tab : "subjects";
        var language = Localization.LanguageForCurrentCulture();
        var ct = HttpContext.RequestAborted;
        Subjects = await store.SubjectsForStudentAsync(User.Id(), language, ct);
        if (subject is not null)
        {
            Current = Subjects.FirstOrDefault(s => s.Key == subject) ?? await store.FindSubjectAsync(subject, language, ct);
            if (Current is null || !await store.CanStudyAsync(User.Id(), Current.Key, ct))
            {
                return RedirectToPage(new { subject = (string?)null });
            }

            return Page();
        }

        async Task<LearnChoice> Choice(Subject s, string icon, string tone, Level? level = null)
        {
            var states = await store.StatesAsync(User.Id(), s.Key, ct);
            return new LearnChoice(s, icon, tone, states.Values.Count(x => x.Status == MasteryStatus.Mastered), s.Concepts.Count,
                states.Values.Max(x => x.LastAttemptAt), level);
        }

        Fractions = await Choice(Subjects.First(s => s.Key == BuiltInSubjects.FractionsKey), "🍕", "lavender");
        var areas = new List<LearnChoice>();
        foreach (var (area, level) in await store.AreaLevelsAsync(User.Id(), ct))
        {
            var info = Catalog.Info(area);
            areas.Add(await Choice(AreaSubjects.Subject(area, level, names, language), info.Icon, info.Tone, level));
        }

        Areas = areas;
        var own = new List<LearnChoice>();
        foreach (var s in Subjects.Where(s => !s.BuiltIn))
        {
            own.Add(await Choice(s, "✨", "pink"));
        }

        Own = own;
        var trades = new List<LearnChoice>();
        foreach (var t in Trades.All)
        {
            trades.Add(await Choice(Trades.Subject(t, language), t.Icon, t.Tone));
        }

        TradeChoices = trades;
        if (Tab != "subjects")
        {
            var kind = Tab == "hobbies" ? TradeKind.Hobby : TradeKind.Trade;
            TradeProgress = [.. (await certificates.ProgressAsync(User.Id(), ct)).Where(p => p.Trade.Kind == kind)];
        }

        return Page();
    }
}
