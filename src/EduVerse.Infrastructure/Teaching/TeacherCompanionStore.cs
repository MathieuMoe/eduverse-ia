using EduVerse.Core.Common;
using EduVerse.Core.Teaching;

namespace EduVerse.Infrastructure.Teaching;

/// <summary>What the teacher's assistant knows: the same per-class follow-up already shown on <c>/Teacher/Class</c>
/// (<see cref="ClassroomStore.DetailAsync"/>), read for every one of the teacher's classes at once.</summary>
public sealed class TeacherCompanionStore(ClassroomStore classrooms)
{
    public async Task<TeacherSnapshot> SnapshotAsync(string teacherId, string name, string page, string title, TeachLanguage language, CancellationToken ct = default)
    {
        var rows = await classrooms.ClassesAsync(teacherId, language, ct).ConfigureAwait(false);
        var digests = new List<ClassDigest>();
        foreach (var row in rows)
        {
            var detail = await classrooms.DetailAsync(teacherId, row.Id, language, ct).ConfigureAwait(false);
            if (detail is null)
            {
                continue;
            }

            var report = detail.Report;
            var atRisk = report.Rows.Where(r => r.AtRisk)
                .Select(r => new AtRiskStudent(r.StudentId, r.Name, row.Id, row.Name, r.OpenAlerts, r.Progress)).ToList();
            var hardest = report.HardestConcept is null ? null : report.Concepts.FirstOrDefault(c => c.ConceptKey == report.HardestConcept)?.Name;
            digests.Add(new ClassDigest(row.Id, row.Name, row.SubjectName, row.Students, row.OpenAlerts, report.AverageProgress, hardest, atRisk, report.HardestConcept));
        }

        return new TeacherSnapshot(name, digests, page ?? "", title ?? "");
    }
}
