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
public sealed class IndexModel(LeagueStore leagues, FriendQuestStore friends, LearningStore store, ClassroomStore classrooms, GamificationStore game, QuizStore quizzes, EduVerse.Infrastructure.Tutor.OpeningStore openings, EduVerse.Infrastructure.Tutor.TeacherNoteStore teacherNotes, EduVerse.Infrastructure.Tutor.AssignmentStore assignments, EduVerse.Infrastructure.Tutor.CuriosityStore curiosity, Microsoft.Extensions.Localization.IStringLocalizer<SharedResource> l) : PageModel
{
    public IReadOnlyList<SubjectCard> Subjects { get; private set; } = [];

    public IReadOnlyList<(string Name, string Teacher)> Classes { get; private set; } = [];

    public Stage Stage { get; private set; }

    public Level Level { get; private set; }

    /// <summary>True for a young child (see <see cref="Levels.IsKids"/>): the home page swaps the adult dashboard
    /// (XP, rankings, percentages) for a simpler, image-led one with no numbers to read.</summary>
    public bool Kids => Levels.IsKids(Level);

    public GameSummary Game { get; private set; } = null!;

    /// <summary>Daily XP goal and happy hour.</summary>
    public DailyGoalView Goal { get; private set; } = null!;

    /// <summary>This week's friend quest (paired automatically from Tuesday).</summary>
    public FriendQuestView? FriendQuest { get; private set; }

    /// <summary>The evening reminder when the streak is alive but nothing was done today.</summary>
    public (int Streak, int HoursLeft, int Freezes)? StreakRisk { get; private set; }

    /// <summary>This week's league (null for a young child or a student who turned competition off).</summary>
    public LeagueView? League { get; private set; }

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

    /// <summary>"Last time we…": what the tutor remembers, with where to go next (null for a new student).</summary>
    public EduVerse.Core.Tutor.OpeningNote? Opening { get; private set; }

    /// <summary>Notes the student's teachers left for them (the Tutor takes them into account too).</summary>
    public IReadOnlyList<string> TeacherNotes { get; private set; } = [];

    /// <summary>Work set by the student's teachers: pending first.</summary>
    public IReadOnlyList<EduVerse.Infrastructure.Tutor.StudentAssignment> Assignments { get; private set; } = [];

    /// <summary>Things the student wonders about, waiting to be explored ("What I wonder about").</summary>
    public IReadOnlyList<EduVerse.Infrastructure.Tutor.CuriosityView> Curiosities { get; private set; } = [];

    public DateOnly Today => assignments.Today;

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
        Goal = await game.GoalAsync(userId, HttpContext.RequestAborted);
        League = await leagues.ViewAsync(userId, HttpContext.RequestAborted);
        StreakRisk = await game.StreakRiskAsync(userId, HttpContext.RequestAborted);
        FriendQuest = League is null ? null : await friends.AutoPairAsync(userId, HttpContext.RequestAborted);
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
        TeacherNotes = await teacherNotes.ForStudentAsync(userId, HttpContext.RequestAborted);
        Assignments = await assignments.ForStudentAsync(userId, HttpContext.RequestAborted);
        Curiosities = await curiosity.ForWidgetAsync(userId, HttpContext.RequestAborted);
        var facts = await openings.FactsAsync(userId, HttpContext.RequestAborted);
        Opening = EduVerse.Core.Tutor.Opening.Build(facts with
        {
            LastTopic = facts.LastTopic is null ? null : QuizEndpoints.TopicName(facts.LastTopic, l),
            HardTopic = facts.HardTopic is null ? null : QuizEndpoints.TopicName(facts.HardTopic, l),
        }, language);
    }

    public async Task<IActionResult> OnPostJoinAsync(string? code)
    {
        var classroom = await classrooms.JoinAsync(User.Id(), code, HttpContext.RequestAborted);
        Notice = classroom is null ? "Join_Unknown" : "Join_Done";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostGoalAsync(int goal, int happyHour)
    {
        await game.SaveGoalAsync(User.Id(), goal, happyHour, HttpContext.RequestAborted);
        Notice = "Goal_Saved";
        return RedirectToPage(null, null, "daily-goal");
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

    public async Task<IActionResult> OnPostCuriosityAddAsync(string? text)
    {
        Notice = await curiosity.AddAsync(User.Id(), text, HttpContext.RequestAborted) switch
        {
            EduVerse.Infrastructure.Tutor.CuriosityOutcome.Created => "Curiosity_Added",
            EduVerse.Infrastructure.Tutor.CuriosityOutcome.TooShort => "Curiosity_TooShort",
            EduVerse.Infrastructure.Tutor.CuriosityOutcome.TooLong => "Curiosity_TooLong",
            EduVerse.Infrastructure.Tutor.CuriosityOutcome.NotAllowed => "Curiosity_NotAllowed",
            _ => "Curiosity_Limit",
        };
        return RedirectToPage();
    }

    /// <summary>Marks the question explored and sends the student straight into exploring it as a new subject.</summary>
    public async Task<IActionResult> OnPostCuriosityExploreAsync(int id)
    {
        var text = await curiosity.ExploreAsync(User.Id(), id, HttpContext.RequestAborted);
        return text is null ? RedirectToPage() : LocalRedirect("/Student/NewSubject?topic=" + Uri.EscapeDataString(text));
    }

    public async Task<IActionResult> OnPostCuriosityDismissAsync(int id)
    {
        await curiosity.DismissAsync(User.Id(), id, HttpContext.RequestAborted);
        return RedirectToPage();
    }
}
