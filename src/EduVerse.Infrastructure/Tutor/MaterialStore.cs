using EduVerse.Core.Tutor;
using EduVerse.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EduVerse.Infrastructure.Tutor;

public enum MaterialOutcome
{
    Saved,
    BadTitle,
    TooShort,
    TooLong,
    BadTarget,
    Full,
    NotAllowed,
}

/// <summary>A document as a student sees it: with the class it comes from.</summary>
public sealed record StudentMaterial(Material Material, string ClassName, string SubjectId);

/// <summary>Documents of the teacher for a class, and the pieces of them the Tutor may use for a student of that class.</summary>
public sealed class MaterialStore(EduVerseDb db, TimeProvider clock)
{
    public async Task<MaterialOutcome> AddAsync(string teacherId, int classId, MaterialTarget target, string? key, string? title, string? text, CancellationToken ct = default)
    {
        if (!await db.Classrooms.AnyAsync(c => c.Id == classId && c.TeacherId == teacherId, ct).ConfigureAwait(false))
        {
            return MaterialOutcome.NotAllowed;
        }

        var cleanTitle = Materials.Clean(title).Replace('\n', ' ');
        var clean = Materials.Clean(text);
        key = target == MaterialTarget.Figure ? (key ?? "").Trim() : "";
        if (cleanTitle.Length is < 3 or > Materials.MaxTitle)
        {
            return MaterialOutcome.BadTitle;
        }

        if (clean.Length < Materials.MinText)
        {
            return MaterialOutcome.TooShort;
        }

        if (clean.Length > Materials.MaxText)
        {
            return MaterialOutcome.TooLong;
        }

        if (!Enum.IsDefined(target) || (target == MaterialTarget.Figure && Interviews.Find(key) is null))
        {
            return MaterialOutcome.BadTarget;
        }

        if (await db.Materials.CountAsync(m => m.ClassroomId == classId, ct).ConfigureAwait(false) >= Materials.MaxPerClass)
        {
            return MaterialOutcome.Full;
        }

        db.Materials.Add(new Material { ClassroomId = classId, TeacherId = teacherId, Target = target, Key = key, Title = cleanTitle, Text = clean, At = clock.GetUtcNow() });
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return MaterialOutcome.Saved;
    }

    /// <summary>Removes a document of the teacher, and the assignments that asked for its test.</summary>
    public async Task<bool> RemoveAsync(string teacherId, int id, CancellationToken ct = default)
    {
        var material = await db.Materials.FirstOrDefaultAsync(m => m.Id == id && m.TeacherId == teacherId, ct).ConfigureAwait(false);
        if (material is null)
        {
            return false;
        }

        db.Assignments.RemoveRange(db.Assignments.Where(a => a.MaterialId == id));
        db.Materials.Remove(material);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return true;
    }

    public async Task<IReadOnlyList<Material>> OfClassAsync(string teacherId, int classId, CancellationToken ct = default) =>
        await db.Materials.AsNoTracking().Where(m => m.ClassroomId == classId && m.TeacherId == teacherId).OrderBy(m => m.Id).ToListAsync(ct).ConfigureAwait(false);

    /// <summary>The documents of the classes the student belongs to.</summary>
    public async Task<IReadOnlyList<StudentMaterial>> ForStudentAsync(string studentId, CancellationToken ct = default)
    {
        var rows = await db.ClassMembers.AsNoTracking().Where(m => m.UserId == studentId)
            .Join(db.Classrooms, m => m.ClassroomId, c => c.Id, (m, c) => c)
            .Join(db.Materials, c => c.Id, m => m.ClassroomId, (c, m) => new { Material = m, c.Name, c.SubjectId })
            .ToListAsync(ct).ConfigureAwait(false);
        return rows.OrderBy(r => r.Material.Id).Select(r => new StudentMaterial(r.Material, r.Name, r.SubjectId)).ToList();
    }

    public async Task<StudentMaterial?> FindForStudentAsync(string studentId, int id, CancellationToken ct = default) =>
        (await ForStudentAsync(studentId, ct).ConfigureAwait(false)).FirstOrDefault(m => m.Material.Id == id);

    /// <summary>
    /// The pieces the Tutor may use: documents of the course when the student is learning that course's subject (only pieces that share
    /// words with the question), or the documents attached to a figure (the beginning when nothing matches).
    /// </summary>
    public async Task<IReadOnlyList<MaterialExcerpt>> ExcerptsAsync(string studentId, MaterialTarget target, string key, string? query, CancellationToken ct = default)
    {
        var all = await ForStudentAsync(studentId, ct).ConfigureAwait(false);
        var documents = all.Where(m => m.Material.Target == target && (target == MaterialTarget.Course ? m.SubjectId == key : m.Material.Key == key))
            .Select(m => (m.Material.Title, m.Material.Text)).ToList();
        return Materials.Excerpts(documents, query, fallbackToStart: target == MaterialTarget.Figure);
    }

    /// <summary>Marks a test as made from a document (so an assignment about that document can see it was done).</summary>
    public async Task MarkAttemptAsync(string userId, string attemptId, int materialId, CancellationToken ct = default)
    {
        var attempt = await db.QuizAttempts.FirstOrDefaultAsync(a => a.Id == attemptId && a.UserId == userId, ct).ConfigureAwait(false);
        if (attempt is not null)
        {
            attempt.MaterialId = materialId;
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }
}
