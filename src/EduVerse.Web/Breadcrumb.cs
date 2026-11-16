using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace EduVerse.Web;

/// <summary>
/// The navigable path shown at the top of every page: home, the intermediate pages and the current one.
/// A page sets <c>ViewData["Trail"]</c> (intermediate links) and <c>ViewData["Crumb"]</c> (current label) when it is deeper than the menu.
/// </summary>
public sealed record Breadcrumb(IReadOnlyList<(string Href, string Label)> Links, string Current)
{
    public string Back => Links[^1].Href;

    public static void Set(ViewDataDictionary view, string current, params (string Href, string Label)[] trail)
    {
        ArgumentNullException.ThrowIfNull(view);
        view["Trail"] = trail.ToList();
        view["Crumb"] = current;
    }

    /// <summary>Null on a home page (nothing above it).</summary>
    public static Breadcrumb? For(ViewDataDictionary view, string? page, string home, string homeLabel, IEnumerable<(string Page, string Href, string Label)> menu)
    {
        ArgumentNullException.ThrowIfNull(view);
        var trail = view["Trail"] as List<(string Href, string Label)>;
        var crumb = view["Crumb"] as string;
        var item = menu.FirstOrDefault(m => m.Page == page);
        if (trail is null && crumb is null && (page is null or "/Index" || item.Href == home))
        {
            return null;
        }

        var links = new List<(string, string)> { (home, homeLabel) };
        links.AddRange((trail ?? []).Where(t => t.Href != home));
        var current = crumb ?? (item.Label is { Length: > 0 } ? item.Label : view["Title"]?.ToString() ?? "");
        return new Breadcrumb(links, current);
    }
}
