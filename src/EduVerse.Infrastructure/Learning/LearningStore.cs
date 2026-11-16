using System.Text.Json;
using EduVerse.Core.Common;
using EduVerse.Core.Curriculum;
using EduVerse.Core.Learning;
using EduVerse.Core.Teaching;
using EduVerse.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EduVerse.Infrastructure.Learning;

/// <summary>Result of an answer: the exercise, the new mastery state and what happened (for feedback and alerts).</summary>
public sealed record AnswerOutcome(string SubjectId, Exercise Exercise, bool Correct, string? Misconception, MasteryState State, IReadOnlyList<LearningEvent> Events, bool Diagnostic)
{
    /// <summary>Hints of the ladder used before answering.</summary>
    public int Hints { get; init; }
}

/// <summary>One step of the hint ladder of the tutor: 1 = the hint text, 2 and 3 = wrong options taken away.</summary>
public sealed record TutorHint(int Level, string? Text, IReadOnlyList<int> Removed);

/// <summary>
/// Persistence of the learning loop. The rules stay in Core (<see cref="MasteryEngine"/>, <see cref="Diagnostic"/>, <see cref="NextStepPlanner"/>);
/// this class only loads and saves their state, and turns their events into alerts for the teacher.
/// </summary>
public sealed class LearningStore(EduVerseDb db, TimeProvider clock)
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>Concepts whose recent mistakes are passed to the tutor.</summary>
    public const int ContextMisconceptions = 3;

    public DateTimeOffset Now => clock.GetUtcNow();

    // ---------- Subjects ----------
    public async Task<Subject?> FindSubjectAsync(string? id, TeachLanguage language, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        if (id == BuiltInSubjects.FractionsKey)
        {
            return BuiltInSubjects.Fractions(language);
        }

        var record = await db.Subjects.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, ct).ConfigureAwait(false);
        return record is null ? null : ToSubject(record);
    }

    public static Subject ToSubject(SubjectRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        var concepts = JsonSerializer.Deserialize<List<Concept>>(record.ConceptsJson, JsonOptions) ?? [];
        return new Subject(record.Id, record.Name, record.Area, concepts, false);
    }

    public async Task<SubjectRecord> SaveSubjectAsync(Subject subject, string ownerId, TeachLanguage language, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(subject);
        var record = new SubjectRecord
        {
            Id = subject.Key,
            Name = subject.Name,
            Area = subject.Area,
            ConceptsJson = JsonSerializer.Serialize(subject.Concepts, JsonOptions),
            OwnerId = ownerId,
            Language = language,
            CreatedAt = Now,
        };
        db.Subjects.Add(record);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return record;
    }

    /// <summary>Subjects of a student: the built-in fractions, the ones created from a topic, and those of the classes joined.</summary>
    public async Task<IReadOnlyList<Subject>> SubjectsForStudentAsync(string userId, TeachLanguage language, CancellationToken ct = default)
    {
        var classSubjects = await db.ClassMembers.Where(m => m.UserId == userId)
            .Join(db.Classrooms, m => m.ClassroomId, c => c.Id, (m, c) => c.SubjectId).Distinct().ToListAsync(ct).ConfigureAwait(false);
        var records = await db.Subjects.AsNoTracking().Where(s => s.OwnerId == userId || classSubjects.Contains(s.Id))
            .OrderBy(s => s.CreatedAt).ToListAsync(ct).ConfigureAwait(false);
        var list = new List<Subject> { BuiltInSubjects.Fractions(language) };
        list.AddRange(records.Select(ToSubject));
        return list;
    }

    /// <summary>Subjects a teacher can assign to a class: fractions and the subjects the teacher created.</summary>
    public async Task<IReadOnlyList<Subject>> SubjectsForTeacherAsync(string userId, TeachLanguage language, CancellationToken ct = default)
    {
        var records = await db.Subjects.AsNoTracking().Where(s => s.OwnerId == userId).OrderBy(s => s.CreatedAt).ToListAsync(ct).ConfigureAwait(false);
        return [BuiltInSubjects.Fractions(language), .. records.Select(ToSubject)];
    }

    public async Task<bool> CanStudyAsync(string userId, string subjectId, CancellationToken ct = default) =>
        subjectId == BuiltInSubjects.FractionsKey
        || await db.Subjects.AnyAsync(s => s.Id == subjectId && s.OwnerId == userId, ct).ConfigureAwait(false)
        || await db.ClassMembers.Where(m => m.UserId == userId).Join(db.Classrooms, m => m.ClassroomId, c => c.Id, (m, c) => c.SubjectId)
            .AnyAsync(s => s == subjectId, ct).ConfigureAwait(false);

    // ---------- Profile ----------
    public async Task<StudentProfile> ProfileAsync(string userId, CancellationToken ct = default)
    {
        var profile = await db.StudentProfiles.FirstOrDefaultAsync(p => p.UserId == userId, ct).ConfigureAwait(false);
        if (profile is null)
        {
            profile = new StudentProfile { UserId = userId };
            db.StudentProfiles.Add(profile);
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        return profile;
    }

    public async Task SaveProfileAsync(StudentProfile profile, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(profile);
        if (db.Entry(profile).State == EntityState.Detached)
        {
            db.StudentProfiles.Update(profile);
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <summary>What the tutor may know when it writes: stage, language, mastery, mistakes. Never a name.</summary>
    public static StudentContext Context(StudentProfile profile, MasteryState state, TeachLanguage language)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(state);
        var mistakes = state.Misconceptions.OrderByDescending(m => m.Value).ThenBy(m => m.Key, StringComparer.Ordinal)
            .Take(ContextMisconceptions).Select(m => m.Key).ToList();
        return new StudentContext(profile.Stage, language, mistakes, state.PKnown, state.MethodsTried);
    }

    // ---------- Mastery ----------
    public async Task<Dictionary<string, MasteryState>> StatesAsync(string userId, string subjectId, CancellationToken ct = default)
    {
        var records = await db.Mastery.AsNoTracking().Where(m => m.UserId == userId && m.SubjectId == subjectId).ToListAsync(ct).ConfigureAwait(false);
        return records.ToDictionary(r => r.ConceptKey, ToState);
    }

    public async Task<MasteryState> StateAsync(string userId, string subjectId, string conceptKey, CancellationToken ct = default)
    {
        var record = await db.Mastery.AsNoTracking().FirstOrDefaultAsync(m => m.UserId == userId && m.SubjectId == subjectId && m.ConceptKey == conceptKey, ct).ConfigureAwait(false);
        return record is null ? MasteryState.New(conceptKey) : ToState(record);
    }

    public static MasteryState ToState(MasteryRecord r)
    {
        ArgumentNullException.ThrowIfNull(r);
        var tried = JsonSerializer.Deserialize<List<ExplanationMethod>>(r.MethodsTriedJson, JsonOptions) ?? [];
        var mistakes = JsonSerializer.Deserialize<Dictionary<string, int>>(r.MisconceptionsJson, JsonOptions) ?? [];
        return new MasteryState(r.ConceptKey, r.PKnown, r.Attempts, r.Correct, r.Streak, r.FailuresWithMethod, r.Method,
            tried.Count > 0 ? tried : [r.Method], mistakes, r.LastAttemptAt, r.Status, r.TeacherAlerted, r.NeedsExplanation);
    }

    public async Task SaveStateAsync(string userId, string subjectId, MasteryState state, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        var record = await db.Mastery.FirstOrDefaultAsync(m => m.UserId == userId && m.SubjectId == subjectId && m.ConceptKey == state.ConceptKey, ct).ConfigureAwait(false);
        if (record is null)
        {
            record = new MasteryRecord { UserId = userId, SubjectId = subjectId, ConceptKey = state.ConceptKey };
            db.Mastery.Add(record);
        }

        Copy(state, record);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    internal static void Copy(MasteryState s, MasteryRecord r)
    {
        r.PKnown = s.PKnown;
        r.Attempts = s.Attempts;
        r.Correct = s.Correct;
        r.Streak = s.Streak;
        r.FailuresWithMethod = s.FailuresWithMethod;
        r.Method = s.Method;
        r.MethodsTriedJson = JsonSerializer.Serialize(s.MethodsTried, JsonOptions);
        r.MisconceptionsJson = JsonSerializer.Serialize(s.Misconceptions, JsonOptions);
        r.LastAttemptAt = s.LastAttemptAt;
        r.Status = s.Status;
        r.TeacherAlerted = s.TeacherAlerted;
        r.NeedsExplanation = s.NeedsExplanation;
    }

    // ---------- Exercises (answers checked on the server) ----------
    public async Task<string> AddPendingAsync(string userId, string subjectId, Exercise exercise, bool diagnostic, bool served, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(exercise);
        var id = Guid.NewGuid().ToString("N");
        db.PendingExercises.Add(new PendingExercise
        {
            Id = id,
            UserId = userId,
            SubjectId = subjectId,
            ConceptKey = exercise.ConceptKey,
            Difficulty = exercise.Difficulty,
            Diagnostic = diagnostic,
            Served = served,
            ExerciseJson = JsonSerializer.Serialize(exercise with { Id = id }, JsonOptions),
            CreatedAt = Now,
        });
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return id;
    }

    /// <summary>An exercise generated earlier (the AI writes them in batches) and not shown yet, closest to the wanted difficulty.</summary>
    public async Task<Exercise?> TakeQueuedAsync(string userId, string subjectId, string conceptKey, int difficulty, CancellationToken ct = default)
    {
        var queued = await db.PendingExercises.Where(p => p.UserId == userId && p.SubjectId == subjectId && p.ConceptKey == conceptKey && !p.Served && !p.Diagnostic)
            .ToListAsync(ct).ConfigureAwait(false);
        var best = queued.OrderBy(p => Math.Abs(p.Difficulty - difficulty)).ThenBy(p => p.CreatedAt).FirstOrDefault();
        if (best is null || Math.Abs(best.Difficulty - difficulty) > 1)
        {
            return null;
        }

        best.Served = true;
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<Exercise>(best.ExerciseJson, JsonOptions);
    }

    /// <summary>Next step of the hint ladder (none during a placement test). Each step is counted and lowers the XP.</summary>
    public async Task<TutorHint?> HintAsync(string userId, string exerciseId, CancellationToken ct = default)
    {
        var pending = await db.PendingExercises.FirstOrDefaultAsync(p => p.Id == exerciseId && p.UserId == userId && p.Served && !p.Diagnostic, ct).ConfigureAwait(false);
        var exercise = pending is null ? null : JsonSerializer.Deserialize<Exercise>(pending.ExerciseJson, JsonOptions);
        if (pending is null || exercise is null)
        {
            return null;
        }

        pending.Hints = Math.Min(3, pending.Hints + 1);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        var rng = new Random(Gamification.GamificationStore.StableSeed(exercise.Id));
        var wrong = Enumerable.Range(0, exercise.Options.Count).Where(i => i != exercise.CorrectIndex).OrderBy(_ => rng.Next()).ToList();
        var removed = wrong.Take(Math.Min(pending.Hints - 1, Math.Max(0, wrong.Count - 1))).ToList();
        return new TutorHint(pending.Hints, exercise.Hint, removed);
    }

    /// <summary>Records the answer to an exercise shown to this student: mastery update, attempt, alerts. Null if unknown or already answered.</summary>
    public async Task<AnswerOutcome?> AnswerAsync(string userId, string exerciseId, int choice, CancellationToken ct = default)
    {
        var pending = await db.PendingExercises.FirstOrDefaultAsync(p => p.Id == exerciseId && p.UserId == userId && p.Served, ct).ConfigureAwait(false);
        var exercise = pending is null ? null : JsonSerializer.Deserialize<Exercise>(pending.ExerciseJson, JsonOptions);
        if (pending is null || exercise is null || choice < 0 || choice >= exercise.Options.Count)
        {
            return null;
        }

        db.PendingExercises.Remove(pending);
        var correct = choice == exercise.CorrectIndex;
        var misconception = correct ? null : exercise.Misconceptions.ElementAtOrDefault(choice);
        var before = await StateAsync(userId, pending.SubjectId, exercise.ConceptKey, ct).ConfigureAwait(false);
        db.Attempts.Add(new Attempt
        {
            UserId = userId,
            SubjectId = pending.SubjectId,
            ConceptKey = exercise.ConceptKey,
            ExerciseId = exercise.Id,
            Question = exercise.Question.Length > 500 ? exercise.Question[..500] : exercise.Question,
            Answer = exercise.Options[choice],
            Correct = correct,
            Misconception = misconception,
            Method = before.Method,
            Difficulty = exercise.Difficulty,
            Diagnostic = pending.Diagnostic,
            At = Now,
        });

        if (pending.Diagnostic)
        {
            // Placement test: the answer moves the binary search, the mastery is set at the end of the test
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
            return new AnswerOutcome(pending.SubjectId, exercise, correct, misconception, before, [], true) { Hints = pending.Hints };
        }

        var (state, events) = MasteryEngine.Record(before, correct, exercise.Difficulty, misconception, Now);
        foreach (var e in events.Where(e => e.Kind is LearningEventKind.NeedsTeacher or LearningEventKind.RecurringMisconception))
        {
            db.Alerts.Add(new Alert
            {
                StudentId = userId,
                SubjectId = pending.SubjectId,
                ConceptKey = e.ConceptKey,
                Kind = e.Kind == LearningEventKind.NeedsTeacher ? AlertKind.NeedsTeacher : AlertKind.RecurringMisconception,
                Detail = e.Detail,
                CreatedAt = Now,
            });
        }

        await SaveStateAsync(userId, pending.SubjectId, state, ct).ConfigureAwait(false);
        return new AnswerOutcome(pending.SubjectId, exercise, correct, misconception, state, events, false) { Hints = pending.Hints };
    }

    // ---------- Placement test ----------
    public async Task<DiagnosticState?> DiagnosticAsync(string userId, string subjectId, CancellationToken ct = default)
    {
        var record = await db.Diagnostics.AsNoTracking().FirstOrDefaultAsync(d => d.UserId == userId && d.SubjectId == subjectId, ct).ConfigureAwait(false);
        return record is null ? null : JsonSerializer.Deserialize<DiagnosticState>(record.StateJson, JsonOptions);
    }

    public async Task SaveDiagnosticAsync(string userId, string subjectId, DiagnosticState state, CancellationToken ct = default)
    {
        var record = await db.Diagnostics.FirstOrDefaultAsync(d => d.UserId == userId && d.SubjectId == subjectId, ct).ConfigureAwait(false);
        if (record is null)
        {
            record = new DiagnosticRecord { UserId = userId, SubjectId = subjectId, StartedAt = Now };
            db.Diagnostics.Add(record);
        }

        record.StateJson = JsonSerializer.Serialize(state, JsonOptions);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// End of the placement test: starting probabilities saved as mastery states. Concepts the test says are known
    /// skip the explanation and go straight to a quick practice check.
    /// </summary>
    public async Task<string?> FinishDiagnosticAsync(string userId, Subject subject, DiagnosticState state, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(subject);
        var (priors, start) = Diagnostic.Result(state);
        foreach (var (key, prior) in priors)
        {
            var s = MasteryState.New(key, prior);
            await SaveStateAsync(userId, subject.Key, prior >= Diagnostic.KnownPrior ? MasteryEngine.Explained(s) : s, ct).ConfigureAwait(false);
        }

        await db.Diagnostics.Where(d => d.UserId == userId && d.SubjectId == subject.Key).ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.PendingExercises.Where(p => p.UserId == userId && p.SubjectId == subject.Key && p.Diagnostic).ExecuteDeleteAsync(ct).ConfigureAwait(false);
        return start;
    }

    /// <summary>Starts over on a subject (the student asked to retake the placement test).</summary>
    public async Task ResetAsync(string userId, string subjectId, CancellationToken ct = default)
    {
        await db.Mastery.Where(m => m.UserId == userId && m.SubjectId == subjectId).ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.Diagnostics.Where(d => d.UserId == userId && d.SubjectId == subjectId).ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.PendingExercises.Where(p => p.UserId == userId && p.SubjectId == subjectId).ExecuteDeleteAsync(ct).ConfigureAwait(false);
    }

    // ---------- Activity (mini charts of the dashboards) ----------
    /// <summary>Answers of these students in the last 2 weeks (placement tests excluded), summarised by <see cref="Activity"/>.</summary>
    public async Task<ActivitySummary> ActivityAsync(IReadOnlyCollection<string> userIds, string? subjectId = null, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(userIds);
        var today = DateOnly.FromDateTime(Now.UtcDateTime);
        var since = Now.AddDays(-2 * Activity.Days - 1);
        var query = db.Attempts.AsNoTracking().Where(a => userIds.Contains(a.UserId) && !a.Diagnostic && a.At >= since);
        if (subjectId is not null)
        {
            query = query.Where(a => a.SubjectId == subjectId);
        }

        var rows = await query.Select(a => new { a.At, a.Correct }).ToListAsync(ct).ConfigureAwait(false);
        return Activity.Summarize(rows.Select(r => (r.At, r.Correct)), today);
    }

    // ---------- Socratic chat ----------
    public const int ChatHistory = 20;

    public async Task<IReadOnlyList<ChatMessage>> ChatAsync(string userId, string subjectId, string conceptKey, CancellationToken ct = default)
    {
        var last = await db.ChatMessages.AsNoTracking().Where(c => c.UserId == userId && c.SubjectId == subjectId && c.ConceptKey == conceptKey)
            .OrderByDescending(c => c.At).ThenByDescending(c => c.Id).Take(ChatHistory).ToListAsync(ct).ConfigureAwait(false);
        last.Reverse();
        return last;
    }

    public async Task AddChatAsync(string userId, string subjectId, string conceptKey, bool fromStudent, string text, CancellationToken ct = default)
    {
        db.ChatMessages.Add(new ChatMessage { UserId = userId, SubjectId = subjectId, ConceptKey = conceptKey, FromStudent = fromStudent, Text = text, At = Now });
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
