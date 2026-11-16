using System.Globalization;
using EduVerse.Core.Learning;

namespace EduVerse.Web;

/// <summary>Tailwind class strings shared by the pages (EduVerse look: floating glass panels, pills, pastel tones).</summary>
public static class Ui
{
    public const string Card = "flex flex-col bg-card border border-card-line shadow-[0_12px_40px_-16px_rgb(53_43_56/0.18)] dark:shadow-none rounded-3xl glass";
    public const string CardHeader = "py-4 px-5 md:px-6 border-b border-card-divider flex flex-wrap items-center justify-between gap-3";
    public const string Input = "py-2.5 px-3.5 block w-full bg-layer border border-layer-line rounded-xl text-sm text-foreground placeholder:text-muted-foreground focus:border-primary focus:ring-2 focus:ring-primary/15 focus:outline-hidden disabled:opacity-50";
    public const string Label = "block mb-1.5 text-sm font-medium text-foreground";
    public const string Btn = "py-2 px-4 inline-flex justify-center items-center gap-x-2 text-sm font-semibold rounded-full focus:outline-hidden focus-visible:ring-2 focus-visible:ring-primary/30 disabled:opacity-50 disabled:pointer-events-none transition-colors";
    public const string BtnLight = Btn + " border border-layer-line bg-layer text-foreground shadow-2xs hover:bg-layer-hover";
    public const string BtnPrimary = Btn + " bg-primary text-primary-foreground hover:bg-primary-hover";
    public const string IconBtn = "size-9 shrink-0 inline-flex justify-center items-center rounded-full border border-layer-line bg-layer text-foreground hover:bg-layer-hover focus:outline-hidden focus-visible:ring-2 focus-visible:ring-primary/30";

    /// <summary>KPI card; add a tone class (tone-lavender, tone-sky, tone-pink, tone-mint, tone-plum).</summary>
    public const string Kpi = "relative flex flex-col min-w-0 rounded-3xl p-5 border border-white/70 dark:border-white/5 shadow-[0_12px_40px_-18px_rgb(53_43_56/0.25)] dark:shadow-none";
    public const string KpiLabel = "text-sm font-semibold text-foreground/80";
    public const string KpiValue = "mt-2 text-4xl font-semibold tracking-tight text-foreground";
    public const string Pill = "inline-flex shrink-0 items-center gap-x-1 py-0.5 px-2 rounded-full text-xs font-semibold whitespace-nowrap";
    public const string Sparkle = "<svg class=\"size-4\" xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"m12 3-1.9 5.8a2 2 0 0 1-1.3 1.3L3 12l5.8 1.9a2 2 0 0 1 1.3 1.3L12 21l1.9-5.8a2 2 0 0 1 1.3-1.3L21 12l-5.8-1.9a2 2 0 0 1-1.3-1.3Z\"/></svg>";

    /// <summary>Mastery chips of the student map (same pastel scale as the teacher heatmap).</summary>
    public static string StatusClass(MasteryStatus status) => status switch
    {
        MasteryStatus.Mastered => "heat-5",
        MasteryStatus.Practising => "heat-4",
        MasteryStatus.Learning => "heat-2",
        _ => "heat-none",
    };

    public static string StatusDot(MasteryStatus status) => status switch
    {
        MasteryStatus.Mastered => "bg-emerald-500",
        MasteryStatus.Practising => "bg-sky-500",
        MasteryStatus.Learning => "bg-amber-500",
        _ => "bg-gray-300 dark:bg-neutral-600",
    };

    /// <summary>Heatmap cell from the probability of knowing the concept.</summary>
    public static string HeatClass(double? p) => p switch
    {
        null => "heat-none",
        >= 0.95 => "heat-5",
        >= 0.75 => "heat-4",
        >= 0.6 => "heat-3",
        >= 0.4 => "heat-2",
        _ => "heat-1",
    };

    /// <summary>Trend pill (↗ +24 % / ↘ -14 %) on a KPI card; empty without a value to compare with.</summary>
    public static string Trend(int? value, string unit = "%")
    {
        if (value is not { } v)
        {
            return "";
        }

        var up = v >= 0;
        var cls = up ? "bg-white/80 text-emerald-700 dark:bg-emerald-500/15 dark:text-emerald-300" : "bg-white/80 text-rose-700 dark:bg-rose-500/15 dark:text-rose-300";
        var arrow = up ? "M3 17l6-6 4 4 8-8M14 7h7v7" : "M3 7l6 6 4-4 8 8M14 17h7v-7";
        var text = (up ? "+" : "") + v.ToString(CultureInfo.CurrentCulture) + (unit == "%" ? " %" : " " + unit);
        return $"<span class=\"{Pill} {cls}\"><svg class=\"size-3.5\" xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2.5\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"{arrow}\"/></svg>{text}</span>";
    }

    public static string Initials(string name) => CurrentUser.Initials(name);

    /// <summary>Rating as stars, for example "★★★★☆ 4.6".</summary>
    public static string Stars(double? rating)
    {
        if (rating is not { } r)
        {
            return "";
        }

        var full = (int)Math.Round(r);
        return new string('★', full) + new string('☆', 5 - full) + " " + r.ToString("0.0", CultureInfo.CurrentCulture);
    }

    /// <summary>Date and time in the marketplace time zone, in the interface language ("Tue 17 Nov · 18:00").</summary>
    public static string When(DateTimeOffset moment, TimeZoneInfo zone)
    {
        ArgumentNullException.ThrowIfNull(zone);
        var local = TimeZoneInfo.ConvertTime(moment, zone);
        return local.ToString("ddd d MMM · HH:mm", CultureInfo.CurrentCulture);
    }

    /// <summary>Amount in euros with the number format of the interface language ("22.50 €", "22,50 €").</summary>
    public static string Money(decimal value) =>
        string.Format(CultureInfo.CurrentCulture, value == Math.Round(value) ? "{0:N0} €" : "{0:N2} €", value);

    /// <summary>A stable soft colour per person for avatars (from the name).</summary>
    public static string AvatarTone(string name)
    {
        string[] tones = ["tone-lavender", "tone-sky", "tone-pink", "tone-mint"];
        var sum = (name ?? "").Sum(c => c);
        return tones[sum % tones.Length];
    }
}
