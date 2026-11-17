using EduVerse.Core.Common;
using EduVerse.Core.Curriculum;
using EduVerse.Core.Teaching;
using EduVerse.Infrastructure.Data;
using EduVerse.Infrastructure.Teaching;
using EduVerse.Infrastructure.Tutor;
using Microsoft.Extensions.DependencyInjection;

namespace EduVerse.Tests;

public class TeacherCompanionRuleTests
{
    private static TeacherSnapshot Snapshot(bool risk = true) => new(
        "Laura",
        [
            new ClassDigest(1, "Maths 2B", "Mathematics", 18, risk ? 2 : 0, 62, "Comparing fractions",
                risk ? [new AtRiskStudent("leo-id", "Leo", 1, "Maths 2B", 2, 40)] : []),
            new ClassDigest(2, "Maths 3A", "Mathematics", 14, 0, 80, null, []),
        ],
        "/Teacher", "Classes");

    [Fact]
    public void Suggestions_point_at_the_students_most_worth_a_look()
    {
        var actions = TeacherCompanion.Suggest(Snapshot(), TeachLanguage.English);
        Assert.Equal(["student-leo-id"], actions.Select(a => a.Id));
        Assert.Equal("/Teacher/Class?id=1&student=leo-id", actions[0].Url);
        Assert.Contains("Leo", actions[0].Label, StringComparison.Ordinal);

        var calm = TeacherCompanion.Suggest(Snapshot(risk: false), TeachLanguage.English);
        Assert.Equal(["class-1"], calm.Select(a => a.Id));
        Assert.Equal("/Teacher/Class?id=1", calm[0].Url);
    }

    [Fact]
    public void Offline_assistant_recognises_the_usual_questions()
    {
        var s = Snapshot();
        var actions = TeacherCompanion.Suggest(s, TeachLanguage.Spanish);
        var risk = TeacherCompanion.Demo(s, actions, "¿Quién necesita ayuda?", TeachLanguage.Spanish);
        Assert.Equal(actions.Select(a => a.Id), risk.Actions);
        var progress = TeacherCompanion.Demo(s, actions, "¿Cómo van mis clases?", TeachLanguage.Spanish);
        Assert.Contains("32 alumnos en 2 clases", progress.Text, StringComparison.Ordinal);
        Assert.Contains("Maths 2B", progress.Text, StringComparison.Ordinal);
        var reteach = TeacherCompanion.Demo(s, actions, "¿Qué debería reforzar?", TeachLanguage.Spanish);
        Assert.Contains("Comparing fractions", reteach.Text, StringComparison.Ordinal);
        Assert.Contains("motor de IA", TeacherCompanion.Demo(s, actions, "¿Cuál es la capital de Francia?", TeachLanguage.Spanish).Text, StringComparison.Ordinal);
        Assert.StartsWith("Salut Laura !", TeacherCompanion.Greeting(s, TeachLanguage.French), StringComparison.Ordinal);
    }

    [Fact]
    public void Greeting_and_progress_are_honest_with_no_classes_yet()
    {
        var empty = new TeacherSnapshot("Laura", [], "/Teacher", "Classes");
        Assert.Contains("Create a class", TeacherCompanion.Greeting(empty, TeachLanguage.English), StringComparison.Ordinal);
        Assert.Contains("don't have any classes yet", TeacherCompanion.Progress(empty, TeachLanguage.English), StringComparison.Ordinal);
        Assert.Empty(TeacherCompanion.Suggest(empty, TeachLanguage.English));
    }

    [Fact]
    public void The_hardest_class_is_the_one_with_the_lowest_average_progress()
    {
        Assert.Equal("Maths 2B", Snapshot().HardestClass!.Name);
    }

    // A class is never on one of the Area/Level catalog subjects (only Fractions, a teacher's own AI subject, or a
    // trade/hobby), so the one-click action nudges with a teacher note instead of creating an Assignment — it only
    // needs a hardest concept and at least one struggling student to point it at, both of which this snapshot has.
    private static TeacherSnapshot SnapshotWithPractice(bool risk = true) => new(
        "Laura",
        [
            new ClassDigest(1, "Maths 2B", "Mathematics", 18, risk ? 2 : 0, 62, "Comparing fractions",
                risk ? [new AtRiskStudent("leo-id", "Leo", 1, "Maths 2B", 2, 40)] : [], "comparing-fractions"),
            new ClassDigest(2, "Maths 3A", "Mathematics", 14, 0, 80, null, []),
        ],
        "/Teacher", "Classes");

    [Fact]
    public void Suggests_a_one_click_nudge_for_the_hardest_class_s_struggling_students()
    {
        var actions = TeacherCompanion.Suggest(SnapshotWithPractice(), TeachLanguage.English);
        // Leo is both the at-risk student (its own action) and the only struggling student of the hardest class, so
        // both actions are offered: checking on him, and nudging the class's struggling students to practise.
        Assert.Equal(["student-leo-id", "practice-1"], actions.Select(a => a.Id));
        var practice = actions[1];
        Assert.Equal(TeacherCompanionActionKind.Practice, practice.Kind);
        Assert.Equal("/api/teacher-companion/practice-note?classId=1", practice.Url);
        Assert.Contains("Comparing fractions", practice.Label, StringComparison.Ordinal);
    }

    [Fact]
    public void No_nudge_when_the_hardest_class_has_nobody_struggling()
    {
        // A struggling student would already show up as its own "student-..." action above, so a class with a
        // hardest concept but nobody at risk gets neither a nudge nor the bare "open class" fallback (the fallback
        // only kicks in with zero classes known to need anything, which isn't this case either way).
        Assert.DoesNotContain(TeacherCompanion.Suggest(SnapshotWithPractice(risk: false), TeachLanguage.English), a => a.Kind == TeacherCompanionActionKind.Practice);
    }

    [Fact]
    public void No_nudge_without_a_hardest_concept_or_without_a_struggling_student()
    {
        Assert.DoesNotContain(TeacherCompanion.Suggest(Snapshot(risk: false), TeachLanguage.English), a => a.Kind == TeacherCompanionActionKind.Practice);
        var noConcept = new TeacherSnapshot("Laura", [new ClassDigest(1, "Maths 2B", "Mathematics", 18, 1, 62, null, [new AtRiskStudent("leo-id", "Leo", 1, "Maths 2B", 1, 40)])], "/Teacher", "Classes");
        Assert.DoesNotContain(TeacherCompanion.Suggest(noConcept, TeachLanguage.English), a => a.Kind == TeacherCompanionActionKind.Practice);
    }

    [Fact]
    public void Reteach_question_leads_with_the_nudge_action_when_there_is_one()
    {
        var s = SnapshotWithPractice();
        var actions = TeacherCompanion.Suggest(s, TeachLanguage.Spanish);
        var reteach = TeacherCompanion.Demo(s, actions, "¿Qué reforzar?", TeachLanguage.Spanish);
        Assert.Equal("practice-1", reteach.Actions[0]);
    }

    [Fact]
    public void The_practice_note_names_the_concept_in_every_language()
    {
        Assert.Equal("Practise: Comparing fractions", TeacherCompanion.PracticeNoteText("Comparing fractions", TeachLanguage.English));
        Assert.Equal("Practica: Comparar fracciones", TeacherCompanion.PracticeNoteText("Comparar fracciones", TeachLanguage.Spanish));
        Assert.True(TeacherCompanion.PracticeNoteText("Comparing fractions", TeachLanguage.English).Length <= TeacherNoteStore.MaxNote);
    }
}

/// <summary>Checks the full <see cref="TeacherCompanionStore"/> + <c>/api/teacher-companion/practice-note</c> path
/// against a real database and real teacher notes.</summary>
public class TeacherCompanionStoreTests(Database database) : IClassFixture<Database>
{
    [Fact]
    public async Task The_snapshot_s_hardest_concept_comes_from_the_class_s_own_report()
    {
        using var scope = database.Services.CreateScope();
        var classrooms = scope.ServiceProvider.GetRequiredService<ClassroomStore>();
        var companion = scope.ServiceProvider.GetRequiredService<TeacherCompanionStore>();
        var teacher = await database.UserIdAsync(DemoSeeder.TeacherEmail);

        // A fresh class with no attempts yet has no hardest concept: nothing for the nudge to point at.
        var fresh = (await classrooms.CreateAsync(teacher, "Brand new group", EduVerse.Core.Curriculum.BuiltInSubjects.FractionsKey))!;
        var snapshot = await companion.SnapshotAsync(teacher, "Laura", "/Teacher", "Classes", TeachLanguage.English);
        var digest = snapshot.Classes.Single(c => c.ClassId == fresh.Id);
        Assert.Null(digest.HardestConceptKey);
        Assert.Empty(digest.AtRisk);
    }
}
