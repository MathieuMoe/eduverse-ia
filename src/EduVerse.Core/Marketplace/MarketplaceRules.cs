using System.Globalization;
using System.Text;
using EduVerse.Core.Common;

namespace EduVerse.Core.Marketplace;

/// <summary>A weekly availability window of a private teacher, in the teacher's local time (hours 0-24).</summary>
public sealed record WeeklySlot(DayOfWeek Day, int StartHour, int EndHour)
{
    public bool IsValid => StartHour is >= 0 and < 24 && EndHour > StartHour && EndHour <= 24;
}

/// <summary>What a student or a family sees of a private teacher.</summary>
public sealed record TutorCard(
    string TutorId,
    string Name,
    string Headline,
    IReadOnlyList<string> Subjects,
    IReadOnlyList<TeachLanguage> Languages,
    IReadOnlyList<Stage> Stages,
    decimal PricePerHour,
    double? Rating,
    int Reviews,
    int Lessons);

/// <summary>Free one-hour slots computed from the weekly availability, minus the bookings, from a minimum notice.</summary>
public static class Availability
{
    public const int LessonMinutes = 60;
    public static readonly TimeSpan MinNotice = TimeSpan.FromHours(12);

    public static IReadOnlyList<DateTimeOffset> FreeSlots(
        IReadOnlyList<WeeklySlot> week, IReadOnlyList<(DateTimeOffset Start, int Minutes)> booked, DateTimeOffset now, TimeZoneInfo zone, int days = 14)
    {
        ArgumentNullException.ThrowIfNull(week);
        ArgumentNullException.ThrowIfNull(booked);
        ArgumentNullException.ThrowIfNull(zone);
        var result = new List<DateTimeOffset>();
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, zone).DateTime);
        for (var d = 0; d < days; d++)
        {
            var date = today.AddDays(d);
            foreach (var w in week.Where(w => w.IsValid && w.Day == date.DayOfWeek).OrderBy(w => w.StartHour))
            {
                for (var h = w.StartHour; h < w.EndHour; h++)
                {
                    var local = date.ToDateTime(new TimeOnly(h, 0));
                    if (zone.IsInvalidTime(local))
                    {
                        continue; // the hour skipped when the clocks go forward
                    }

                    var start = new DateTimeOffset(local, zone.GetUtcOffset(local));
                    var end = start.AddMinutes(LessonMinutes);
                    if (start - now < MinNotice || booked.Any(b => start < b.Start.AddMinutes(b.Minutes) && b.Start < end))
                    {
                        continue;
                    }

                    result.Add(start);
                }
            }
        }

        return result.Distinct().OrderBy(s => s).ToList();
    }
}

/// <summary>Platform commission, from 15 % down to 8 % with the teacher's volume of the month (business plan: 8-15 %).</summary>
public static class Commission
{
    public const decimal Standard = 0.15m;
    public const decimal Medium = 0.12m;
    public const decimal Low = 0.08m;
    public const decimal MediumFrom = 500m;
    public const decimal LowFrom = 1500m;

    /// <param name="monthGross">Gross amount of the teacher's lessons already paid this month, before this one.</param>
    public static decimal Rate(decimal monthGross) => monthGross >= LowFrom ? Low : monthGross >= MediumFrom ? Medium : Standard;

    public static (decimal Commission, decimal Net) Split(decimal price, decimal rate)
    {
        var commission = Math.Round(price * rate, 2, MidpointRounding.AwayFromZero);
        return (commission, price - commission);
    }
}

/// <summary>What the student needs, from the platform's data (subject of the weakest concepts, stage, language).</summary>
public sealed record MatchNeeds(IReadOnlyList<string> Subjects, Stage? Stage, TeachLanguage? Language, decimal? MaxPrice);

public sealed record TutorMatchResult(TutorCard Tutor, int Score, IReadOnlyList<string> Reasons);

/// <summary>Recommendation of private teachers in code, with the reasons shown to the student.</summary>
public static class TutorMatch
{
    public static TutorMatchResult Score(TutorCard tutor, MatchNeeds needs, decimal medianPrice)
    {
        ArgumentNullException.ThrowIfNull(tutor);
        ArgumentNullException.ThrowIfNull(needs);
        var reasons = new List<string>();
        var score = 0;
        if (needs.Subjects.Any(n => tutor.Subjects.Any(s => Same(s, n))))
        {
            score += 40;
            reasons.Add("Subject");
        }

        if (needs.Stage is { } stage && tutor.Stages.Contains(stage))
        {
            score += 20;
            reasons.Add("Stage");
        }

        if (needs.Language is { } language && tutor.Languages.Contains(language))
        {
            score += 15;
            reasons.Add("Language");
        }

        if (tutor.Rating is >= 4.5 && tutor.Reviews >= 3)
        {
            score += 15;
            reasons.Add("TopRated");
        }

        if (tutor.PricePerHour <= medianPrice)
        {
            score += 10;
            reasons.Add("GoodPrice");
        }

        if (needs.MaxPrice is { } max && tutor.PricePerHour > max)
        {
            score -= 30;
        }

        return new TutorMatchResult(tutor, Math.Max(0, score), reasons);
    }

    public static IReadOnlyList<TutorMatchResult> Rank(IReadOnlyList<TutorCard> tutors, MatchNeeds needs)
    {
        ArgumentNullException.ThrowIfNull(tutors);
        var prices = tutors.Select(t => t.PricePerHour).OrderBy(p => p).ToList();
        var median = prices.Count == 0 ? 0 : prices[prices.Count / 2];
        return tutors.Select(t => Score(t, needs, median)).OrderByDescending(r => r.Score).ThenByDescending(r => r.Tutor.Rating ?? 0).ToList();
    }

    /// <summary>Accent- and case-insensitive comparison ("Matemáticas" = "matematicas", "Mathematics" contains "math").</summary>
    public static bool Same(string a, string b)
    {
        var x = Normalize(a);
        var y = Normalize(b);
        return x.Length > 0 && y.Length > 0 && (x == y || x.StartsWith(y, StringComparison.Ordinal) || y.StartsWith(x, StringComparison.Ordinal));
    }

    private static string Normalize(string text)
    {
        var d = (text ?? "").Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        return new string(d.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray());
    }
}

public enum BookingStatus
{
    /// <summary>Booked, waiting for the payment.</summary>
    Pending,
    Confirmed,
    Completed,
    Cancelled,
}

/// <summary>Booking lifecycle in code: who can do what and when (payment, cancellation and refund, completion, rating).</summary>
public static class BookingRules
{
    public static readonly TimeSpan FreeCancellation = TimeSpan.FromHours(24);
    public static readonly TimeSpan PaymentWindow = TimeSpan.FromMinutes(30);

    public static bool CanPay(BookingStatus status, DateTimeOffset start, DateTimeOffset now) => status == BookingStatus.Pending && start > now;

    public static bool CanCancel(BookingStatus status, DateTimeOffset start, DateTimeOffset now) =>
        status is BookingStatus.Pending or BookingStatus.Confirmed && start > now;

    /// <summary>A paid lesson cancelled at least 24 h before is fully refunded; later, it is not.</summary>
    public static bool Refunded(BookingStatus status, DateTimeOffset start, DateTimeOffset now) =>
        status == BookingStatus.Confirmed && start - now >= FreeCancellation;

    /// <summary>The status as it is now: a confirmed lesson that has ended is completed; an unpaid one past its window is cancelled.</summary>
    public static BookingStatus Current(BookingStatus status, DateTimeOffset start, int minutes, DateTimeOffset created, DateTimeOffset now) => status switch
    {
        BookingStatus.Confirmed when start.AddMinutes(minutes) <= now => BookingStatus.Completed,
        BookingStatus.Pending when now - created > PaymentWindow || start <= now => BookingStatus.Cancelled,
        _ => status,
    };

    public static bool CanRate(BookingStatus status, int? rating) => status == BookingStatus.Completed && rating is null;
}
