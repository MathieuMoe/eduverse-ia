using EduVerse.Core.Catalog;
using EduVerse.Core.Curriculum;
using EduVerse.Core.Tutor;
using EduVerse.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EduVerse.Infrastructure.Tutor;

public enum AssignmentOutcome
{
    Saved,
    BadDate,
    BadWork,
    Full,
    NotAllowed,
}

/// <summary>What a student did for an assignment: done or not, and the result as "right of total" (criteria met for an open answer).</summary>
public sealed record WorkStatus(bool Done, int Score, int Total, DateTimeOffset? At);

/// <summary>An assignment as a student sees it.</summary>
public sealed record StudentAssignment(Assignment Assignment, string ClassName, string? MaterialTitle, WorkStatus Status);

/// <summary>An assignment as the teacher sees it: one status per student of the class.</summary>
public sealed record ClassAssignment(Assignment Assignment, string? MaterialTitle, IReadOnlyDictionary<string, WorkStatus> ByStudent);

/// <summary>How a student answered in two periods of 30 days: before and now (null when there are too few answers to say).</summary>
public sealed record ProgressRow(string StudentId, string Name, int AnswersBefore, int? PercentBefore, int AnswersNow, int? PercentNow, int Done, int Assigned)
{
    public int? Change => PercentBefore is { } b && PercentNow is { } n ? n - b : null;
}

public sealed record TopicProgress(string Topic, int? PercentBefore, int? PercentNow, int AnswersNow);

/// <summary>
/// Work set by the teacher and the proof of progress. Nothing is ticked by hand: an assignment is done when the student's own activity
/// after it was set shows it (a finished session on the topic, an open answer sent, the test of the document finished).
/// </summary>
public sealed class AssignmentStore(EduVerseDb db, TimeProvider clock)
{
    public const int MaxPerClass = 30;

    public const int MaxDays = 120;

    public const int PeriodDays = 30;

    /// <summary>Answers needed in a period before giving a percentage.</summary>
    public const int MinAnswers = 5;

    public DateOnly Today => DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);

    public async Task<AssignmentOutcome> CreateAsync(string teacherId, int classId, AssignmentKind kind, Area area, Level level, string? topic, string? writingKey, int? materialId, DateOnly dueOn, CancellationToken ct = default)
    {
        if (!await db.Classrooms.AnyAsync(c => c.Id == classId && c.TeacherId == teacherId, ct).ConfigureAwait(false))
        {
            return AssignmentOutcome.NotAllowed;
        }

        if (dueOn < Today || dueOn.DayNumber - Today.DayNumber > MaxDays)
        {
            return AssignmentOutcome.BadDate;
        }

        var valid = kind switch
        {
            AssignmentKind.Practice => Enum.IsDefined(area) && Enum.IsDefined(level) && Catalog.Available(area, level) && topic is not null && AreaSubjects.Topics(area, level).Contains(topic),
            AssignmentKind.Writing => Writing.Keys.Contains(writingKey ?? ""),
            AssignmentKind.Material => materialId is { } id && await db.Materials.AnyAsync(m => m.Id == id && m.ClassroomId == classId, ct).ConfigureAwait(false),
            _ => false,
        };
        if (!valid)
        {
            return AssignmentOutcome.BadWork;
        }

        if (await db.Assignments.CountAsync(a => a.ClassroomId == classId, ct).ConfigureAwait(false) >= MaxPerClass)
        {
            return AssignmentOutcome.Full;
        }

        db.Assignments.Add(new Assignment
        {
            ClassroomId = classId, TeacherId = teacherId, Kind = kind, Area = kind == AssignmentKind.Practice ? area : default, Level = kind == AssignmentKind.Practice ? level : default,
            Topic = kind == AssignmentKind.Practice ? topic! : "", WritingKey = kind == AssignmentKind.Writing ? writingKey! : "", MaterialId = kind == AssignmentKind.Material ? materialId : null,
            DueOn = dueOn.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), At = clock.GetUtcNow(),
        });
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return AssignmentOutcome.Saved;
    }

    public async Task<bool> RemoveAsync(string teacherId, int id, CancellationToken ct = default)
    {
        var assignment = await db.Assignments.FirstOrDefaultAsync(a => a.Id == id && a.TeacherId == teacherId, ct).ConfigureAwait(false);
        if (assignment is null)
        {
            return false;
        }

        db.Assignments.Remove(assignment);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return true;
    }

    /// <summary>The best result of each student for each assignment, read from their activity after the assignment was set.</summary>
    private async Task<Dictionary<(int Assignment, string User), WorkStatus>> StatusAsync(List<Assignment> assignments, List<string> users, CancellationToken ct)
    {
        var result = new Dictionary<(int, string), WorkStatus>();
        if (assignments.Count == 0 || users.Count == 0)
        {
            return result;
        }

        var first = assignments.Min(a => a.At);
        var attempts = (await db.QuizAttempts.AsNoTracking().Where(q => users.Contains(q.UserId) && q.FinishedAt != null && (q.Mode == QuizMode.Focus || q.MaterialId != null))
            .Select(q => new { q.UserId, q.Mode, q.Area, q.Level, q.Topic, q.MaterialId, q.Correct, q.Answered, q.StartedAt, q.FinishedAt }).ToListAsync(ct).ConfigureAwait(false))
            .Where(q => q.StartedAt >= first).ToList();
        var writings = (await db.WritingSubmissions.AsNoTracking().Where(w => users.Contains(w.UserId)).ToListAsync(ct).ConfigureAwait(false)).Where(w => w.At >= first).ToList();
        foreach (var a in assignments)
        {
            foreach (var user in users)
            {
                var status = a.Kind switch
                {
                    AssignmentKind.Writing => writings.Where(w => w.UserId == user && w.TaskKey == a.WritingKey && w.At >= a.At).OrderByDescending(w => w.Passed).ThenBy(w => w.At)
                        .Select(w => new WorkStatus(true, w.Passed, w.Checks, w.At)).FirstOrDefault(),
                    AssignmentKind.Material => attempts.Where(q => q.UserId == user && q.MaterialId == a.MaterialId && q.StartedAt >= a.At).OrderByDescending(q => q.Correct)
                        .Select(q => new WorkStatus(true, q.Correct, q.Answered, q.FinishedAt)).FirstOrDefault(),
                    _ => attempts.Where(q => q.UserId == user && q.Mode == QuizMode.Focus && q.Area == a.Area && q.Level == a.Level && q.Topic == a.Topic && q.StartedAt >= a.At).OrderByDescending(q => q.Correct)
                        .Select(q => new WorkStatus(true, q.Correct, q.Answered, q.FinishedAt)).FirstOrDefault(),
                };
                result[(a.Id, user)] = status ?? new WorkStatus(false, 0, 0, null);
            }
        }

        return result;
    }

    private async Task<Dictionary<int, string>> MaterialTitlesAsync(IEnumerable<Assignment> assignments, CancellationToken ct)
    {
        var ids = assignments.Where(a => a.MaterialId != null).Select(a => a.MaterialId!.Value).Distinct().ToList();
        return ids.Count == 0 ? [] : await db.Materials.AsNoTracking().Where(m => ids.Contains(m.Id)).ToDictionaryAsync(m => m.Id, m => m.Title, ct).ConfigureAwait(false);
    }

    /// <summary>The assignments of a class of the teacher with what each student did (null when the class is not theirs).</summary>
    public async Task<IReadOnlyList<ClassAssignment>?> ForClassAsync(string teacherId, int classId, CancellationToken ct = default)
    {
        var classroom = await db.Classrooms.AsNoTracking().Include(c => c.Members).FirstOrDefaultAsync(c => c.Id == classId && c.TeacherId == teacherId, ct).ConfigureAwait(false);
        if (classroom is null)
        {
            return null;
        }

        var assignments = await db.Assignments.AsNoTracking().Where(a => a.ClassroomId == classId).OrderBy(a => a.DueOn).ThenBy(a => a.Id).ToListAsync(ct).ConfigureAwait(false);
        var users = classroom.Members.Select(m => m.UserId).ToList();
        var status = await StatusAsync(assignments, users, ct).ConfigureAwait(false);
        var titles = await MaterialTitlesAsync(assignments, ct).ConfigureAwait(false);
        return assignments.Select(a => new ClassAssignment(a, a.MaterialId is { } m ? titles.GetValueOrDefault(m) : null, users.ToDictionary(u => u, u => status[(a.Id, u)]))).ToList();
    }

    /// <summary>The assignments of the classes of a student: pending first (nearest date first), then done.</summary>
    public async Task<IReadOnlyList<StudentAssignment>> ForStudentAsync(string studentId, CancellationToken ct = default)
    {
        var rows = await db.ClassMembers.AsNoTracking().Where(m => m.UserId == studentId)
            .Join(db.Classrooms, m => m.ClassroomId, c => c.Id, (m, c) => c)
            .Join(db.Assignments, c => c.Id, a => a.ClassroomId, (c, a) => new { Assignment = a, c.Name })
            .ToListAsync(ct).ConfigureAwait(false);
        var assignments = rows.Select(r => r.Assignment).ToList();
        var status = await StatusAsync(assignments, [studentId], ct).ConfigureAwait(false);
        var titles = await MaterialTitlesAsync(assignments, ct).ConfigureAwait(false);
        return rows.Select(r => new StudentAssignment(r.Assignment, r.Name, r.Assignment.MaterialId is { } m ? titles.GetValueOrDefault(m) : null, status[(r.Assignment.Id, studentId)]))
            .OrderBy(s => s.Status.Done).ThenBy(s => s.Assignment.DueOn).ThenBy(s => s.Assignment.Id).ToList();
    }

    private static int? Percent(int right, int total) => total < MinAnswers ? null : 100 * right / total;

    /// <summary>Measured progress of each student of the class: the last 30 days against the 30 before (null when the class is not theirs).</summary>
    public async Task<IReadOnlyList<ProgressRow>?> ProgressAsync(string teacherId, int classId, CancellationToken ct = default)
    {
        var assignments = await ForClassAsync(teacherId, classId, ct).ConfigureAwait(false);
        if (assignments is null)
        {
            return null;
        }

        var users = await db.ClassMembers.AsNoTracking().Where(m => m.ClassroomId == classId).Select(m => m.UserId).ToListAsync(ct).ConfigureAwait(false);
        var names = await db.Users.AsNoTracking().Where(u => users.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.DisplayName, ct).ConfigureAwait(false);
        var now = clock.GetUtcNow();
        var answers = (await db.QuizAnswers.AsNoTracking().Where(a => users.Contains(a.UserId)).Select(a => new { a.UserId, a.Correct, a.At }).ToListAsync(ct).ConfigureAwait(false))
            .Where(a => a.At >= now.AddDays(-2 * PeriodDays)).ToList();
        return users.Select(u =>
        {
            var before = answers.Where(a => a.UserId == u && a.At < now.AddDays(-PeriodDays)).ToList();
            var recent = answers.Where(a => a.UserId == u && a.At >= now.AddDays(-PeriodDays)).ToList();
            return new ProgressRow(u, names.GetValueOrDefault(u, "?"), before.Count, Percent(before.Count(a => a.Correct), before.Count), recent.Count, Percent(recent.Count(a => a.Correct), recent.Count),
                assignments.Count(a => a.ByStudent[u].Done), assignments.Count);
        }).OrderBy(r => r.Name, StringComparer.CurrentCulture).ToList();
    }

    /// <summary>The topics of one student, before and now (topics with answers in the last 30 days, most answered first).</summary>
    public async Task<IReadOnlyList<TopicProgress>> TopicsAsync(string studentId, CancellationToken ct = default)
    {
        var now = clock.GetUtcNow();
        var answers = (await db.QuizAnswers.AsNoTracking().Where(a => a.UserId == studentId).Select(a => new { a.Topic, a.Correct, a.At }).ToListAsync(ct).ConfigureAwait(false))
            .Where(a => a.At >= now.AddDays(-2 * PeriodDays) && !a.Topic.Contains(':', StringComparison.Ordinal)).ToList();
        return answers.GroupBy(a => a.Topic).Select(g =>
        {
            var before = g.Where(a => a.At < now.AddDays(-PeriodDays)).ToList();
            var recent = g.Where(a => a.At >= now.AddDays(-PeriodDays)).ToList();
            return new TopicProgress(g.Key, before.Count >= 3 ? 100 * before.Count(a => a.Correct) / before.Count : null, recent.Count >= 3 ? 100 * recent.Count(a => a.Correct) / recent.Count : null, recent.Count);
        }).Where(t => t.AnswersNow > 0).OrderByDescending(t => t.AnswersNow).Take(8).ToList();
    }
}
