using EduVerse.Core.Ideas;
using EduVerse.Core.Tutor;
using EduVerse.Infrastructure.Data;
using EduVerse.Infrastructure.Teaching;
using Microsoft.EntityFrameworkCore;

namespace EduVerse.Infrastructure.Tutor;

/// <summary>
/// The human teacher in the loop: a short note for a student of their own class. The student reads it and the Tutor takes it into
/// account (as data, never as instructions). Same text rules as the student's own notes; a teacher cannot write to a student outside their classes.
/// </summary>
public sealed class TeacherNoteStore(EduVerseDb db, ClassroomStore classrooms, TimeProvider clock)
{
    public const int MaxNote = 160;

    public const int MaxPerStudent = 3;

    public async Task<NoteOutcome> AddAsync(string teacherId, string studentId, string? text, CancellationToken ct = default)
    {
        var clean = IdeaRules.Clean(text);
        if (IdeaRules.Normalize(clean).Length < 3)
        {
            return NoteOutcome.TooShort;
        }

        if (clean.Length > MaxNote)
        {
            return NoteOutcome.TooLong;
        }

        if (!IdeaRules.Safe(clean) || clean.Contains('<', StringComparison.Ordinal) || clean.Contains('>', StringComparison.Ordinal))
        {
            return NoteOutcome.NotAllowed;
        }

        if (!(await classrooms.StudentIdsAsync(teacherId, ct: ct).ConfigureAwait(false)).Contains(studentId))
        {
            return NoteOutcome.NotAllowed;
        }

        if (await db.TeacherNotes.CountAsync(n => n.TeacherId == teacherId && n.StudentId == studentId, ct).ConfigureAwait(false) >= MaxPerStudent)
        {
            return NoteOutcome.Full;
        }

        db.TeacherNotes.Add(new TeacherNote { TeacherId = teacherId, StudentId = studentId, Text = clean, At = clock.GetUtcNow() });
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return NoteOutcome.Saved;
    }

    public async Task<bool> RemoveAsync(string teacherId, int id, CancellationToken ct = default)
    {
        var note = await db.TeacherNotes.FirstOrDefaultAsync(n => n.Id == id && n.TeacherId == teacherId, ct).ConfigureAwait(false);
        if (note is null)
        {
            return false;
        }

        db.TeacherNotes.Remove(note);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return true;
    }

    /// <summary>The notes the teacher wrote for this student (to show and edit).</summary>
    public async Task<IReadOnlyList<TeacherNote>> OfTeacherAsync(string teacherId, string studentId, CancellationToken ct = default) =>
        await db.TeacherNotes.AsNoTracking().Where(n => n.TeacherId == teacherId && n.StudentId == studentId).OrderBy(n => n.Id).ToListAsync(ct).ConfigureAwait(false);

    /// <summary>The notes of the student's teachers that are still relevant (newest first, at most 3): what the student and the Tutor see.</summary>
    public async Task<IReadOnlyList<string>> ForStudentAsync(string studentId, CancellationToken ct = default) =>
        await db.TeacherNotes.AsNoTracking().Where(n => n.StudentId == studentId).OrderByDescending(n => n.Id).Take(MaxPerStudent).Select(n => n.Text).ToListAsync(ct).ConfigureAwait(false);
}
