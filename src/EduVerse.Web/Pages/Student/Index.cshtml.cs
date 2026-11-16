using EduVerse.Core.Catalog;
using EduVerse.Core.Common;
using EduVerse.Core.Curriculum;
using EduVerse.Core.Learning;
using EduVerse.Core.Teaching;
using EduVerse.Infrastructure.Gamification;
using EduVerse.Infrastructure.Learning;
using EduVerse.Infrastructure.Quizzes;
using EduVerse.Infrastructure.Teaching;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduVerse.Web.Pages.Student;

public sealed record SubjectCard(Subject Subject, IReadOnlyList<(Concept Concept, MasteryState? State)> Concepts, int Mastered, int Progress, NextStep Next);

/// <summary>Student dashboard: subjects with their mastery map and next step, classes, settings.</summary>
public sealed class IndexModel(LearningStore store, ClassroomStore classrooms, GamificationStore game, QuizStore quizzes) : PageModel
{
    public IReadOnlyList<SubjectCard> Subjects { get; private set; } = [];

    public IReadOnlyList<(string Name, string Teacher)> Classes { get; private set; } = [];

    public Stage Stage { get; private set; }

    public Level Level { get; private set; }

    public GameSummary Game { get; private set; } = null!;

    public (int Position, int Size)? Position { get; private set; }

    public ClassBoard? Board { get; private set; }

    public IReadOnlyList<SubjectProgress> CatalogProgress { get; private set; } = [];

    public IReadOnlyList<EduVerse.Infrastructure.Data.QuizAttempt> RecentTests { get; private set; } = [];

    /// <summary>Topics to review today (spaced repetition).</summary>
    public int ReviewCount { get; private set; }

    /// <summary>The student's fraction mistakes, for Evo's notebook.</summary>
    public IReadOnlyList<string> Mistakes { get; private set; } = [];

    /// <summary>The subject of the catalog worked on most recently (or mathematics).</summary>
    public SubjectProgress? Practice => CatalogProgress.Where(c => c.Answers > 0).OrderByDescending(c => c.Answers).FirstOrDefault() ?? (CatalogProgress.Count > 0 ? CatalogProgress[0] : null);

    public ActivitySummary Activity { get; private set; } = null!;

    public int Mastered => Subjects.Sum(s => s.Mastered);

    public int Concepts => Subjects.Sum(s => s.Subject.Concepts.Count);

    /// <summary>The first subject with something to do (the built-in one comes first).</summary>
    public SubjectCard? Focus => Subjects.FirstOrDefault(s => s.Next.Kind != StepKind.Done) ?? (Subjects.Count > 0 ? Subjects[0] : null);

    [TempData]
    public string? Notice { get; set; }

    public async Task OnGetAsync()
    {
        var userId = User.Id();
        var language = Localization.LanguageForCurrentCulture();
        var profile = await store.ProfileAsync(userId, HttpContext.RequestAborted);
        (Stage, Level) = (profile.Stage, profile.Level);
        Game = await game.SummaryAsync(userId, HttpContext.RequestAborted);
        var boards = await game.BoardsAsync(userId, HttpContext.RequestAborted);
        Board = boards.Count > 0 ? boards[0] : null;
        Position = Board?.Students.FirstOrDefault(s => s.Me) is { } me ? (me.Position, Board.Students.Count) : null;
        CatalogProgress = await quizzes.ProgressAsync(userId, Level, HttpContext.RequestAborted);
        RecentTests = await quizzes.HistoryAsync(userId, take: 4, ct: HttpContext.RequestAborted);
        ReviewCount = (await quizzes.ReviewPlanAsync(userId, HttpContext.RequestAborted)).Count;
        Mistakes = await quizzes.MistakesAsync(userId, HttpContext.RequestAborted);
        var cards = new List<SubjectCard>();
        foreach (var subject in await store.SubjectsForStudentAsync(userId, language, HttpContext.RequestAborted))
        {
            var states = await store.StatesAsync(userId, subject.Key, HttpContext.RequestAborted);
            var concepts = subject.Ordered().Select(c => (c, states.TryGetValue(c.Key, out var s) ? s : null)).ToList();
            var mastered = states.Values.Count(s => s.Status == MasteryStatus.Mastered);
            cards.Add(new SubjectCard(subject, concepts, mastered, subject.Concepts.Count == 0 ? 0 : 100 * mastered / subject.Concepts.Count,
                NextStepPlanner.Plan(subject, states, store.Now)));
        }

        Subjects = cards;
        Classes = await classrooms.ClassesOfStudentAsync(userId, HttpContext.RequestAborted);
        Activity = await store.ActivityAsync([userId], null, HttpContext.RequestAborted);
    }

    public async Task<IActionResult> OnPostJoinAsync(string? code)
    {
        var classroom = await classrooms.JoinAsync(User.Id(), code, HttpContext.RequestAborted);
        Notice = classroom is null ? "Join_Unknown" : "Join_Done";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSettingsAsync(Level level)
    {
        var profile = await store.ProfileAsync(User.Id(), HttpContext.RequestAborted);
        if (Enum.IsDefined(level))
        {
            (profile.Level, profile.Stage) = (level, Levels.ToStage(level));
        }

        await store.SaveProfileAsync(profile, HttpContext.RequestAborted);
        await quizzes.ApplyLevelToAllAsync(User.Id(), profile.Level, HttpContext.RequestAborted);
        Notice = "Settings_Saved";
        return RedirectToPage();
    }
}
