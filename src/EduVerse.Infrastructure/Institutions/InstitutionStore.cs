using EduVerse.Core.Common;
using EduVerse.Core.Curriculum;
using EduVerse.Core.Institutions;
using EduVerse.Core.Learning;
using EduVerse.Core.Teaching;
using EduVerse.Infrastructure.Data;
using EduVerse.Infrastructure.Learning;
using Microsoft.EntityFrameworkCore;

namespace EduVerse.Infrastructure.Institutions;

public sealed record ClassOverview(int Id, string Name, string Teacher, string Subject, int Students, int Progress, int OpenAlerts, int AnswersThisWeek);

public sealed record TeacherOverview(string Id, string Name, int Classes, int Students, int OpenAlerts);

public sealed record InstitutionDashboard(
    Institution Institution,
    IReadOnlyList<ClassOverview> Classes,
    IReadOnlyList<TeacherOverview> Teachers,
    IReadOnlyList<RiskResult> Risks,
    ActivitySummary Activity,
    QualityReport Quality,
    Bill Bill,
    int Students,
    int ActiveStudents);

/// <summary>
/// The institution space: teachers join with a code; the director sees every class of the institution's teachers.
/// All numbers come from Core rules (<see cref="DropoutRisk"/>, <see cref="Quality"/>, <see cref="Plans"/>, <see cref="Activity"/>).
/// </summary>
public sealed class InstitutionStore(EduVerseDb db, LearningStore learning, TimeProvider clock)
{
    private DateTimeOffset Now => clock.GetUtcNow();

    public Task<Institution?> ForUserAsync(string userId, CancellationToken ct = default) =>
        db.Institutions.AsNoTracking().Include(i => i.Members).FirstOrDefaultAsync(i => i.Members.Any(m => m.UserId == userId), ct);

    public async Task<Institution> CreateAsync(string name, string directorId, CancellationToken ct = default)
    {
        string code;
        do
        {
            code = JoinCode.Generate(Random.Shared);
        }
        while (await db.Institutions.AnyAsync(i => i.Code == code, ct).ConfigureAwait(false));

        var institution = new Institution { Name = name.Trim(), Code = code, CreatedAt = Now };
        institution.Members.Add(new InstitutionMember { UserId = directorId, Role = Roles.Director, JoinedAt = Now });
        db.Institutions.Add(institution);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return institution;
    }

    /// <summary>A teacher joins the institution with its code (one institution per teacher).</summary>
    public async Task<Institution?> JoinAsTeacherAsync(string teacherId, string? code, CancellationToken ct = default)
    {
        var normal = JoinCode.Normalize(code);
        if (!JoinCode.IsValid(normal) || await db.InstitutionMembers.AnyAsync(m => m.UserId == teacherId, ct).ConfigureAwait(false))
        {
            return null;
        }

        var institution = await db.Institutions.FirstOrDefaultAsync(i => i.Code == normal, ct).ConfigureAwait(false);
        if (institution is null)
        {
            return null;
        }

        db.InstitutionMembers.Add(new InstitutionMember { InstitutionId = institution.Id, UserId = teacherId, Role = Roles.Teacher, JoinedAt = Now });
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return institution;
    }

    public async Task<InstitutionDashboard> DashboardAsync(Institution institution, TeachLanguage language, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(institution);
        var teacherIds = await db.InstitutionMembers.Where(m => m.InstitutionId == institution.Id && m.Role == Roles.Teacher).Select(m => m.UserId).ToListAsync(ct).ConfigureAwait(false);
        var classes = await db.Classrooms.AsNoTracking().Include(c => c.Members).Where(c => teacherIds.Contains(c.TeacherId)).OrderBy(c => c.Name).ToListAsync(ct).ConfigureAwait(false);
        var studentIds = classes.SelectMany(c => c.Members.Select(m => m.UserId)).Distinct().ToList();
        var userIds = studentIds.Concat(teacherIds).ToList();
        var names = await db.Users.Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.DisplayName, ct).ConfigureAwait(false);

        var since = Now.AddDays(-2 * Activity.Days - 1);
        var recent = await db.Attempts.AsNoTracking().Where(a => studentIds.Contains(a.UserId) && !a.Diagnostic && a.At >= since)
            .Select(a => new { a.UserId, a.At, a.Correct }).ToListAsync(ct).ConfigureAwait(false);
        var lastAnswers = await db.Attempts.Where(a => studentIds.Contains(a.UserId)).GroupBy(a => a.UserId)
            .Select(g => new { UserId = g.Key, Last = g.Max(a => a.At) }).ToListAsync(ct).ConfigureAwait(false);
        var alerts = await db.Alerts.AsNoTracking().Where(a => studentIds.Contains(a.StudentId)).ToListAsync(ct).ConfigureAwait(false);
        var records = await db.Mastery.AsNoTracking().Where(m => studentIds.Contains(m.UserId)).ToListAsync(ct).ConfigureAwait(false);
        var methods = await db.Attempts.AsNoTracking().Where(a => studentIds.Contains(a.UserId) && !a.Diagnostic)
            .Select(a => new { a.Method, a.Correct }).ToListAsync(ct).ConfigureAwait(false);

        var today = DateOnly.FromDateTime(Now.UtcDateTime);
        var weekStart = today.AddDays(1 - Activity.Days);
        var progress = new Dictionary<string, List<int>>();
        var overviews = new List<ClassOverview>();
        foreach (var c in classes)
        {
            var subject = await learning.FindSubjectAsync(c.SubjectId, language, ct).ConfigureAwait(false);
            var concepts = subject?.Concepts.Count ?? 0;
            var members = c.Members.Select(m => m.UserId).ToList();
            foreach (var id in members)
            {
                var mastered = records.Count(r => r.UserId == id && r.SubjectId == c.SubjectId && r.Status == MasteryStatus.Mastered);
                (progress.TryGetValue(id, out var list) ? list : progress[id] = []).Add(concepts == 0 ? 0 : 100 * mastered / concepts);
            }

            var classProgress = members.Count == 0 ? 0 : (int)Math.Round(members.Average(id => progress[id][^1]));
            overviews.Add(new ClassOverview(c.Id, c.Name, names.GetValueOrDefault(c.TeacherId, "?"), subject?.Name ?? c.SubjectId, members.Count, classProgress,
                alerts.Count(a => members.Contains(a.StudentId) && a.SubjectId == c.SubjectId && a.ResolvedAt == null),
                recent.Count(a => members.Contains(a.UserId) && DateOnly.FromDateTime(a.At.UtcDateTime) >= weekStart)));
        }

        var signals = studentIds.Select(id =>
        {
            var mine = recent.Where(a => a.UserId == id).ToList();
            var week = mine.Count(a => DateOnly.FromDateTime(a.At.UtcDateTime) >= weekStart);
            var last = lastAnswers.FirstOrDefault(l => l.UserId == id)?.Last;
            var joined = classes.SelectMany(c => c.Members).Where(m => m.UserId == id).Min(m => m.JoinedAt);
            return new StudentSignals(id, names.GetValueOrDefault(id, "?"),
                last is { } l ? (int)(Now - l).TotalDays : null, week, mine.Count - week,
                mine.Count == 0 ? null : (int)Math.Round(100.0 * mine.Count(a => a.Correct) / mine.Count),
                alerts.Count(a => a.StudentId == id && a.ResolvedAt == null),
                progress.TryGetValue(id, out var p) ? (int)Math.Round(p.Average()) : 0,
                (int)(Now - joined).TotalDays);
        }).ToList();

        var teachers = teacherIds.Select(t =>
        {
            var own = classes.Where(c => c.TeacherId == t).ToList();
            var students = own.SelectMany(c => c.Members.Select(m => m.UserId)).Distinct().ToList();
            return new TeacherOverview(t, names.GetValueOrDefault(t, "?"), own.Count, students.Count, alerts.Count(a => students.Contains(a.StudentId) && a.ResolvedAt == null));
        }).OrderBy(t => t.Name, StringComparer.CurrentCulture).ToList();

        var activity = Activity.Summarize(recent.Select(a => (a.At, a.Correct)), today);
        var quality = Quality.Build(methods.Select(m => (m.Method, m.Correct)), alerts.Select(a => (a.CreatedAt, a.ResolvedAt)), records.Select(LearningStore.ToState));
        var active = signals.Count(s => s.AnswersThisWeek > 0);
        return new InstitutionDashboard(institution, overviews, teachers, DropoutRisk.Rank(signals), activity, quality,
            Plans.Compute(Plans.Academy, studentIds.Count, active), studentIds.Count, active);
    }

    /// <summary>Facts for the message to the family of a student of the institution (null if the student is not in it).</summary>
    public async Task<FamilyFacts?> FamilyFactsAsync(Institution institution, string studentId, TeachLanguage language, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(institution);
        var teacherIds = await db.InstitutionMembers.Where(m => m.InstitutionId == institution.Id && m.Role == Roles.Teacher).Select(m => m.UserId).ToListAsync(ct).ConfigureAwait(false);
        var classroom = await db.Classrooms.AsNoTracking().Where(c => teacherIds.Contains(c.TeacherId) && c.Members.Any(m => m.UserId == studentId))
            .OrderBy(c => c.CreatedAt).FirstOrDefaultAsync(ct).ConfigureAwait(false);
        if (classroom is null || await learning.FindSubjectAsync(classroom.SubjectId, language, ct).ConfigureAwait(false) is not { } subject)
        {
            return null;
        }

        var name = await db.Users.Where(u => u.Id == studentId).Select(u => u.DisplayName).FirstAsync(ct).ConfigureAwait(false);
        var states = await learning.StatesAsync(studentId, subject.Key, ct).ConfigureAwait(false);
        var activity = await learning.ActivityAsync([studentId], subject.Key, ct).ConfigureAwait(false);
        var last = await db.Attempts.Where(a => a.UserId == studentId).OrderByDescending(a => a.At).Select(a => (DateTimeOffset?)a.At).FirstOrDefaultAsync(ct).ConfigureAwait(false);
        var codes = await db.Attempts.Where(a => a.UserId == studentId && a.SubjectId == subject.Key && a.Misconception != null)
            .GroupBy(a => a.Misconception!).OrderByDescending(g => g.Count()).Select(g => g.Key).Take(2).ToListAsync(ct).ConfigureAwait(false);

        var ordered = subject.Ordered();
        var mastered = ordered.Where(c => states.TryGetValue(c.Key, out var s) && s.Status == MasteryStatus.Mastered).ToList();
        var toWork = ordered.Where(c => states.TryGetValue(c.Key, out var s) && s.Status is MasteryStatus.Learning or MasteryStatus.Practising).Take(2).ToList();
        if (toWork.Count == 0)
        {
            toWork = ordered.Where(c => !mastered.Contains(c)).Take(1).ToList();
        }

        return new FamilyFacts(name, subject.Name, ordered.Count == 0 ? 0 : 100 * mastered.Count / ordered.Count, mastered.Count, ordered.Count,
            activity.Answers, activity.Accuracy, last is { } l ? (int)(Now - l).TotalDays : null,
            mastered.TakeLast(3).Select(c => c.Name).ToList(), toWork.Select(c => c.Name).ToList(),
            codes.Select(c => Misconceptions.Describe(c, subject.Key, language)).ToList());
    }
}
