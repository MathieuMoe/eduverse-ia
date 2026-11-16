using EduVerse.Core.Common;
using EduVerse.Core.Curriculum;
using EduVerse.Core.Learning;
using EduVerse.Core.Teaching;
using EduVerse.Infrastructure.Data;
using EduVerse.Infrastructure.Learning;
using Microsoft.EntityFrameworkCore;

namespace EduVerse.Infrastructure.Teaching;

public sealed record ClassSummaryRow(int Id, string Name, string Code, string SubjectId, string SubjectName, int Students, int OpenAlerts);

public sealed record AlertView(int Id, string StudentId, string StudentName, string ConceptKey, string ConceptName, AlertKind Kind, string? Detail, DateTimeOffset CreatedAt);

public sealed record ClassDetail(Classroom Classroom, Subject Subject, ClassReport Report, IReadOnlyList<AlertView> Alerts);

/// <summary>Classes of a teacher, joining with a code, and the class follow-up (computed by <see cref="ClassInsights"/>).</summary>
public sealed class ClassroomStore(EduVerseDb db, LearningStore learning, TimeProvider clock)
{
    public const int MaxClassesPerTeacher = 30;

    public async Task<IReadOnlyList<ClassSummaryRow>> ClassesAsync(string teacherId, TeachLanguage language, CancellationToken ct = default)
    {
        var classes = await db.Classrooms.AsNoTracking().Include(c => c.Members).Where(c => c.TeacherId == teacherId)
            .OrderBy(c => c.CreatedAt).ToListAsync(ct).ConfigureAwait(false);
        var rows = new List<ClassSummaryRow>();
        foreach (var c in classes)
        {
            var members = c.Members.Select(m => m.UserId).ToList();
            var alerts = await db.Alerts.CountAsync(a => members.Contains(a.StudentId) && a.SubjectId == c.SubjectId && a.ResolvedAt == null, ct).ConfigureAwait(false);
            var subject = await learning.FindSubjectAsync(c.SubjectId, language, ct).ConfigureAwait(false);
            rows.Add(new ClassSummaryRow(c.Id, c.Name, c.Code, c.SubjectId, subject?.Name ?? c.SubjectId, members.Count, alerts));
        }

        return rows;
    }

    /// <summary>Students of the teacher's classes (one class, or all of them).</summary>
    public async Task<IReadOnlyList<string>> StudentIdsAsync(string teacherId, int? classId = null, CancellationToken ct = default) =>
        await db.Classrooms.Where(c => c.TeacherId == teacherId && (classId == null || c.Id == classId))
            .SelectMany(c => c.Members.Select(m => m.UserId)).Distinct().ToListAsync(ct).ConfigureAwait(false);

    public async Task<Classroom?> CreateAsync(string teacherId, string name, string subjectId, CancellationToken ct = default)
    {
        name = (name ?? "").Trim();
        if (name.Length is 0 or > 80 || await db.Classrooms.CountAsync(c => c.TeacherId == teacherId, ct).ConfigureAwait(false) >= MaxClassesPerTeacher)
        {
            return null;
        }

        if (subjectId != BuiltInSubjects.FractionsKey && !await db.Subjects.AnyAsync(s => s.Id == subjectId && s.OwnerId == teacherId, ct).ConfigureAwait(false))
        {
            return null;
        }

        var random = Random.Shared;
        string code;
        do
        {
            code = JoinCode.Generate(random);
        }
        while (await db.Classrooms.AnyAsync(c => c.Code == code, ct).ConfigureAwait(false));

        var classroom = new Classroom { Name = name, Code = code, TeacherId = teacherId, SubjectId = subjectId, CreatedAt = clock.GetUtcNow() };
        db.Classrooms.Add(classroom);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return classroom;
    }

    /// <summary>A student joins a class with its code. Returns the class, or null when the code is unknown.</summary>
    public async Task<Classroom?> JoinAsync(string studentId, string? code, CancellationToken ct = default)
    {
        var normal = JoinCode.Normalize(code);
        if (!JoinCode.IsValid(normal))
        {
            return null;
        }

        var classroom = await db.Classrooms.Include(c => c.Members).FirstOrDefaultAsync(c => c.Code == normal, ct).ConfigureAwait(false);
        if (classroom is null)
        {
            return null;
        }

        if (classroom.Members.All(m => m.UserId != studentId))
        {
            classroom.Members.Add(new ClassMember { ClassroomId = classroom.Id, UserId = studentId, JoinedAt = clock.GetUtcNow() });
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        return classroom;
    }

    public async Task<IReadOnlyList<(string Name, string Teacher)>> ClassesOfStudentAsync(string studentId, CancellationToken ct = default)
    {
        var list = await db.ClassMembers.Where(m => m.UserId == studentId)
            .Join(db.Classrooms, m => m.ClassroomId, c => c.Id, (m, c) => c)
            .Join(db.Users, c => c.TeacherId, u => u.Id, (c, u) => new { c.Name, u.DisplayName })
            .ToListAsync(ct).ConfigureAwait(false);
        return list.Select(x => (x.Name, x.DisplayName)).ToList();
    }

    public async Task<ClassDetail?> DetailAsync(string teacherId, int classId, TeachLanguage language, CancellationToken ct = default)
    {
        var classroom = await db.Classrooms.AsNoTracking().Include(c => c.Members).FirstOrDefaultAsync(c => c.Id == classId && c.TeacherId == teacherId, ct).ConfigureAwait(false);
        if (classroom is null)
        {
            return null;
        }

        var subject = await learning.FindSubjectAsync(classroom.SubjectId, language, ct).ConfigureAwait(false);
        if (subject is null)
        {
            return null;
        }

        var ids = classroom.Members.Select(m => m.UserId).ToList();
        var names = await db.Users.Where(u => ids.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.DisplayName, ct).ConfigureAwait(false);
        var records = await db.Mastery.AsNoTracking().Where(m => ids.Contains(m.UserId) && m.SubjectId == subject.Key).ToListAsync(ct).ConfigureAwait(false);
        var alerts = await db.Alerts.AsNoTracking().Where(a => ids.Contains(a.StudentId) && a.SubjectId == subject.Key && a.ResolvedAt == null)
            .OrderByDescending(a => a.CreatedAt).ToListAsync(ct).ConfigureAwait(false);
        var mistakes = await db.Attempts.AsNoTracking().Where(a => ids.Contains(a.UserId) && a.SubjectId == subject.Key && !a.Correct && a.Misconception != null)
            .Select(a => new Mistake(a.UserId, a.ConceptKey, a.Misconception!)).ToListAsync(ct).ConfigureAwait(false);

        var students = ids.Select(id => new StudentMastery(id, names.GetValueOrDefault(id, "?"),
            records.Where(r => r.UserId == id).ToDictionary(r => r.ConceptKey, LearningStore.ToState),
            alerts.Count(a => a.StudentId == id))).ToList();
        var report = ClassInsights.Build(subject, students, mistakes);
        var views = alerts.Select(a => new AlertView(a.Id, a.StudentId, names.GetValueOrDefault(a.StudentId, "?"), a.ConceptKey,
            subject.Find(a.ConceptKey)?.Name ?? a.ConceptKey, a.Kind, a.Detail, a.CreatedAt)).ToList();
        return new ClassDetail(classroom, subject, report, views);
    }

    /// <summary>The teacher has dealt with an alert (only alerts of students in one of the teacher's classes).</summary>
    public async Task<bool> ResolveAlertAsync(string teacherId, int alertId, CancellationToken ct = default)
    {
        var alert = await db.Alerts.FirstOrDefaultAsync(a => a.Id == alertId && a.ResolvedAt == null, ct).ConfigureAwait(false);
        if (alert is null)
        {
            return false;
        }

        var allowed = await db.Classrooms.Where(c => c.TeacherId == teacherId && c.SubjectId == alert.SubjectId)
            .AnyAsync(c => c.Members.Any(m => m.UserId == alert.StudentId), ct).ConfigureAwait(false);
        if (!allowed)
        {
            return false;
        }

        alert.ResolvedAt = clock.GetUtcNow();
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return true;
    }

    /// <summary>Recent answers of one student of the teacher's class, for the individual follow-up.</summary>
    public async Task<(string Name, IReadOnlyList<Attempt> Attempts)?> StudentHistoryAsync(string teacherId, int classId, string studentId, CancellationToken ct = default)
    {
        var classroom = await db.Classrooms.AsNoTracking().Include(c => c.Members).FirstOrDefaultAsync(c => c.Id == classId && c.TeacherId == teacherId, ct).ConfigureAwait(false);
        if (classroom is null || classroom.Members.All(m => m.UserId != studentId))
        {
            return null;
        }

        var name = await db.Users.Where(u => u.Id == studentId).Select(u => u.DisplayName).FirstAsync(ct).ConfigureAwait(false);
        var attempts = await db.Attempts.AsNoTracking().Where(a => a.UserId == studentId && a.SubjectId == classroom.SubjectId)
            .OrderByDescending(a => a.At).Take(30).ToListAsync(ct).ConfigureAwait(false);
        return (name, attempts);
    }
}
