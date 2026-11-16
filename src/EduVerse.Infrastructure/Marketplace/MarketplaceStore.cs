using System.Text.Json;
using EduVerse.Core.Common;
using EduVerse.Core.Learning;
using EduVerse.Core.Marketplace;
using EduVerse.Infrastructure.Data;
using EduVerse.Infrastructure.Learning;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EduVerse.Infrastructure.Marketplace;

/// <summary>"Marketplace" section: time zone of the teachers' weekly availability.</summary>
public sealed class MarketplaceOptions
{
    public const string SectionName = "Marketplace";

    public string TimeZone { get; set; } = "Europe/Madrid";
}

public sealed record TutorFilter(string? Subject, TeachLanguage? Language, Stage? Stage, decimal? MaxPrice);

public sealed record BookingView(Booking Booking, string TutorName, string StudentName, BookingStatus Status, bool CanPay, bool CanCancel, bool CanRate, bool RefundIfCancelled);

public sealed record Earnings(decimal Gross, decimal Commission, decimal Net, int Lessons, decimal CurrentRate, IReadOnlyList<(DateOnly Month, decimal Net)> Months);

public enum BookResult
{
    Booked,
    SlotTaken,
    Invalid,
}

/// <summary>
/// The marketplace: private teachers' profiles, free slots, bookings with a simulated payment, earnings after commission.
/// Rules (slots, commission, refunds, matching) live in Core.
/// </summary>
public sealed class MarketplaceStore(EduVerseDb db, LearningStore learning, TimeProvider clock, IOptions<MarketplaceOptions> options)
{
    public const int MaxNoteLength = 400;

    private static readonly JsonSerializerOptions Json = LearningStore.JsonOptions;

    public DateTimeOffset Now => clock.GetUtcNow();

    public TimeZoneInfo Zone { get; } = TimeZoneInfo.FindSystemTimeZoneById(options?.Value.TimeZone ?? "Europe/Madrid");

    // ---------- Profiles ----------
    public async Task<TutorProfile> ProfileAsync(string tutorId, CancellationToken ct = default)
    {
        var profile = await db.TutorProfiles.FirstOrDefaultAsync(p => p.UserId == tutorId, ct).ConfigureAwait(false);
        if (profile is null)
        {
            profile = new TutorProfile { UserId = tutorId, CreatedAt = Now };
            db.TutorProfiles.Add(profile);
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        return profile;
    }

    public async Task SaveProfileAsync(TutorProfile profile, IReadOnlyList<string> subjects, IReadOnlyList<TeachLanguage> languages, IReadOnlyList<Stage> stages, IReadOnlyList<WeeklySlot> week, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(profile);
        profile.SubjectsJson = JsonSerializer.Serialize(subjects.Select(s => s.Trim()).Where(s => s.Length is > 0 and <= 40).Distinct(StringComparer.OrdinalIgnoreCase).Take(8), Json);
        profile.LanguagesJson = JsonSerializer.Serialize(languages.Distinct(), Json);
        profile.StagesJson = JsonSerializer.Serialize(stages.Distinct(), Json);
        profile.AvailabilityJson = JsonSerializer.Serialize(week.Where(w => w.IsValid), Json);
        profile.PricePerHour = Math.Clamp(Math.Round(profile.PricePerHour, 2), 5m, 200m);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public static IReadOnlyList<string> Subjects(TutorProfile p) => JsonSerializer.Deserialize<List<string>>(p.SubjectsJson, Json) ?? [];

    public static IReadOnlyList<TeachLanguage> Languages(TutorProfile p) => JsonSerializer.Deserialize<List<TeachLanguage>>(p.LanguagesJson, Json) ?? [];

    public static IReadOnlyList<Stage> Stages(TutorProfile p) => JsonSerializer.Deserialize<List<Stage>>(p.StagesJson, Json) ?? [];

    public static IReadOnlyList<WeeklySlot> Week(TutorProfile p) => JsonSerializer.Deserialize<List<WeeklySlot>>(p.AvailabilityJson, Json) ?? [];

    /// <summary>Published teachers with their rating (average of the rated lessons) and number of lessons given.</summary>
    public async Task<IReadOnlyList<TutorCard>> TutorsAsync(TutorFilter? filter = null, CancellationToken ct = default)
    {
        var profiles = await db.TutorProfiles.AsNoTracking().Where(p => p.Published).ToListAsync(ct).ConfigureAwait(false);
        var ids = profiles.Select(p => p.UserId).ToList();
        var names = await db.Users.Where(u => ids.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.DisplayName, ct).ConfigureAwait(false);
        var stats = await db.Bookings.Where(b => ids.Contains(b.TutorId) && b.Status == BookingStatus.Completed).GroupBy(b => b.TutorId)
            .Select(g => new { g.Key, Lessons = g.Count(), Reviews = g.Count(b => b.Rating != null), Sum = g.Sum(b => b.Rating ?? 0) }).ToListAsync(ct).ConfigureAwait(false);

        var cards = profiles.Select(p =>
        {
            var s = stats.FirstOrDefault(x => x.Key == p.UserId);
            return new TutorCard(p.UserId, names.GetValueOrDefault(p.UserId, "?"), p.Headline, Subjects(p), Languages(p), Stages(p), p.PricePerHour,
                s is { Reviews: > 0 } ? Math.Round((double)s.Sum / s.Reviews, 1) : null, s?.Reviews ?? 0, s?.Lessons ?? 0);
        });

        if (filter is not null)
        {
            if (!string.IsNullOrWhiteSpace(filter.Subject))
            {
                cards = cards.Where(c => c.Subjects.Any(s => TutorMatch.Same(s, filter.Subject)) || c.Headline.Contains(filter.Subject, StringComparison.OrdinalIgnoreCase));
            }

            if (filter.Language is { } language)
            {
                cards = cards.Where(c => c.Languages.Contains(language));
            }

            if (filter.Stage is { } stage)
            {
                cards = cards.Where(c => c.Stages.Contains(stage));
            }

            if (filter.MaxPrice is { } max)
            {
                cards = cards.Where(c => c.PricePerHour <= max);
            }
        }

        return cards.OrderByDescending(c => c.Rating ?? 0).ThenByDescending(c => c.Lessons).ToList();
    }

    /// <summary>Teachers recommended to a student from the subjects not mastered yet, the stage and the interface language.</summary>
    public async Task<IReadOnlyList<TutorMatchResult>> RecommendAsync(string studentId, TeachLanguage language, CancellationToken ct = default)
    {
        var profile = await learning.ProfileAsync(studentId, ct).ConfigureAwait(false);
        var subjects = new List<string>();
        foreach (var subject in await learning.SubjectsForStudentAsync(studentId, language, ct).ConfigureAwait(false))
        {
            var states = await learning.StatesAsync(studentId, subject.Key, ct).ConfigureAwait(false);
            if (states.Values.Count(s => s.Status == MasteryStatus.Mastered) < subject.Concepts.Count)
            {
                subjects.Add(subject.Area);
                subjects.Add(subject.Name);
                if (subject.BuiltIn)
                {
                    // Teachers write their subjects in their own language: match the built-in area in the 4 languages
                    subjects.AddRange(Enum.GetValues<TeachLanguage>().Select(l => Core.Curriculum.BuiltInSubjects.Fractions(l).Area));
                }
            }
        }

        var tutors = await TutorsAsync(null, ct).ConfigureAwait(false);
        return TutorMatch.Rank(tutors, new MatchNeeds(subjects, profile.Stage, language, null)).Take(3).ToList();
    }

    // ---------- Slots and bookings ----------
    public async Task<IReadOnlyList<DateTimeOffset>> SlotsAsync(string tutorId, CancellationToken ct = default)
    {
        var profile = await db.TutorProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == tutorId && p.Published, ct).ConfigureAwait(false);
        if (profile is null)
        {
            return [];
        }

        var from = Now.AddHours(-2);
        var booked = await db.Bookings.Where(b => b.TutorId == tutorId && b.Start >= from && (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Pending))
            .ToListAsync(ct).ConfigureAwait(false);
        var holding = booked.Where(b => BookingRules.Current(b.Status, b.Start, b.Minutes, b.CreatedAt, Now) is BookingStatus.Confirmed or BookingStatus.Pending)
            .Select(b => (b.Start, b.Minutes)).ToList();
        return Availability.FreeSlots(Week(profile), holding, Now, Zone);
    }

    public async Task<(BookResult Result, Booking? Booking)> BookAsync(string studentId, string tutorId, DateTimeOffset start, string subject, string? note, CancellationToken ct = default)
    {
        subject = (subject ?? "").Trim();
        if (studentId == tutorId || subject.Length is 0 or > 80)
        {
            return (BookResult.Invalid, null);
        }

        var slots = await SlotsAsync(tutorId, ct).ConfigureAwait(false);
        if (!slots.Contains(start))
        {
            return (BookResult.SlotTaken, null);
        }

        var profile = await db.TutorProfiles.AsNoTracking().FirstAsync(p => p.UserId == tutorId, ct).ConfigureAwait(false);
        var booking = new Booking
        {
            TutorId = tutorId,
            StudentId = studentId,
            Subject = subject,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim()[..Math.Min(note.Trim().Length, MaxNoteLength)],
            Start = start,
            Minutes = Availability.LessonMinutes,
            Price = profile.PricePerHour * Availability.LessonMinutes / 60,
            Status = BookingStatus.Pending,
            CreatedAt = Now,
        };
        db.Bookings.Add(booking);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return (BookResult.Booked, booking);
    }

    /// <summary>Simulated payment: confirms the lesson and fixes the commission with the teacher's volume of the month. No card data.</summary>
    public async Task<bool> PayAsync(string studentId, int bookingId, CancellationToken ct = default)
    {
        var booking = await db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId && b.StudentId == studentId, ct).ConfigureAwait(false);
        if (booking is null || !BookingRules.CanPay(BookingRules.Current(booking.Status, booking.Start, booking.Minutes, booking.CreatedAt, Now), booking.Start, Now))
        {
            return false;
        }

        var monthStart = new DateTimeOffset(Now.Year, Now.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var paid = await db.Bookings.Where(b => b.TutorId == booking.TutorId && b.PaidAt >= monthStart && !b.Refunded
                && (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Completed))
            .Select(b => b.Price).ToListAsync(ct).ConfigureAwait(false);
        booking.CommissionRate = Commission.Rate(paid.Sum());
        booking.Commission = Commission.Split(booking.Price, booking.CommissionRate).Commission;
        booking.Status = BookingStatus.Confirmed;
        booking.PaidAt = Now;
        booking.PaymentReference = "SIM-" + Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return true;
    }

    /// <summary>The student or the teacher cancels; a paid lesson cancelled 24 h before or more is refunded (always refunded when the teacher cancels).</summary>
    public async Task<bool> CancelAsync(string userId, int bookingId, CancellationToken ct = default)
    {
        var booking = await db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId && (b.StudentId == userId || b.TutorId == userId), ct).ConfigureAwait(false);
        if (booking is null)
        {
            return false;
        }

        var status = BookingRules.Current(booking.Status, booking.Start, booking.Minutes, booking.CreatedAt, Now);
        if (!BookingRules.CanCancel(status, booking.Start, Now))
        {
            return false;
        }

        booking.Refunded = status == BookingStatus.Confirmed && (booking.TutorId == userId || BookingRules.Refunded(status, booking.Start, Now));
        booking.Status = BookingStatus.Cancelled;
        booking.CancelledAt = Now;
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return true;
    }

    public async Task<bool> RateAsync(string studentId, int bookingId, int rating, string? review, CancellationToken ct = default)
    {
        var booking = await db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId && b.StudentId == studentId, ct).ConfigureAwait(false);
        if (booking is null || rating is < 1 or > 5 || !BookingRules.CanRate(BookingRules.Current(booking.Status, booking.Start, booking.Minutes, booking.CreatedAt, Now), booking.Rating))
        {
            return false;
        }

        booking.Status = BookingStatus.Completed;
        booking.Rating = rating;
        booking.Review = string.IsNullOrWhiteSpace(review) ? null : review.Trim()[..Math.Min(review.Trim().Length, MaxNoteLength)];
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return true;
    }

    public Task<IReadOnlyList<BookingView>> ForStudentAsync(string studentId, CancellationToken ct = default) => ViewsAsync(b => b.StudentId == studentId, ct);

    public Task<IReadOnlyList<BookingView>> ForTutorAsync(string tutorId, CancellationToken ct = default) => ViewsAsync(b => b.TutorId == tutorId, ct);

    private async Task<IReadOnlyList<BookingView>> ViewsAsync(System.Linq.Expressions.Expression<Func<Booking, bool>> where, CancellationToken ct)
    {
        var bookings = await db.Bookings.Where(where).ToListAsync(ct).ConfigureAwait(false);

        // Statuses that changed with time (lesson finished, payment window over) are saved
        foreach (var b in bookings)
        {
            var current = BookingRules.Current(b.Status, b.Start, b.Minutes, b.CreatedAt, Now);
            if (current != b.Status)
            {
                b.Status = current;
            }
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        var ids = bookings.SelectMany(b => new[] { b.TutorId, b.StudentId }).Distinct().ToList();
        var names = await db.Users.Where(u => ids.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.DisplayName, ct).ConfigureAwait(false);
        return bookings
            .OrderBy(b => b.Status is BookingStatus.Completed or BookingStatus.Cancelled).ThenBy(b => b.Status is BookingStatus.Completed or BookingStatus.Cancelled ? -b.Start.UtcTicks : b.Start.UtcTicks)
            .Select(b => new BookingView(b, names.GetValueOrDefault(b.TutorId, "?"), names.GetValueOrDefault(b.StudentId, "?"), b.Status,
                BookingRules.CanPay(b.Status, b.Start, Now), BookingRules.CanCancel(b.Status, b.Start, Now), BookingRules.CanRate(b.Status, b.Rating),
                BookingRules.Refunded(b.Status, b.Start, Now)))
            .ToList();
    }

    /// <summary>Teacher's earnings this month (after commission) and the last 6 months.</summary>
    public async Task<Earnings> EarningsAsync(string tutorId, CancellationToken ct = default)
    {
        var paid = await db.Bookings.AsNoTracking().Where(b => b.TutorId == tutorId && b.PaidAt != null && !b.Refunded
                && (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Completed))
            .ToListAsync(ct).ConfigureAwait(false);
        var month = new DateOnly(Now.Year, Now.Month, 1);
        DateOnly MonthOf(DateTimeOffset d) => new(d.Year, d.Month, 1);
        var current = paid.Where(b => MonthOf(b.PaidAt!.Value) == month).ToList();
        var months = Enumerable.Range(0, 6).Select(i => month.AddMonths(i - 5))
            .Select(m => (m, paid.Where(b => MonthOf(b.PaidAt!.Value) == m).Sum(b => b.Price - b.Commission))).ToList();
        var gross = current.Sum(b => b.Price);
        var commission = current.Sum(b => b.Commission);
        return new Earnings(gross, commission, gross - commission, current.Count, Commission.Rate(gross), months);
    }

    public async Task<IReadOnlyList<(int Rating, string? Review, string Student, DateTimeOffset Start)>> ReviewsAsync(string tutorId, int take = 5, CancellationToken ct = default)
    {
        var rows = await db.Bookings.AsNoTracking().Where(b => b.TutorId == tutorId && b.Rating != null).OrderByDescending(b => b.Start).Take(take)
            .Join(db.Users, b => b.StudentId, u => u.Id, (b, u) => new { b.Rating, b.Review, u.DisplayName, b.Start }).ToListAsync(ct).ConfigureAwait(false);
        return rows.Select(r => (r.Rating!.Value, r.Review, r.DisplayName, r.Start)).ToList();
    }
}
