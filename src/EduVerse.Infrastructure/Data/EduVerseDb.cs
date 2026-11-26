using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace EduVerse.Infrastructure.Data;

/// <summary>
/// SQLite database (EF Core) with ASP.NET Core Identity. Switching to SQL Server later is a provider and connection string change.
/// </summary>
public sealed class EduVerseDb(DbContextOptions<EduVerseDb> options) : IdentityDbContext<AppUser>(options)
{
    public DbSet<StudentProfile> StudentProfiles => Set<StudentProfile>();

    public DbSet<SubjectRecord> Subjects => Set<SubjectRecord>();

    public DbSet<MasteryRecord> Mastery => Set<MasteryRecord>();

    public DbSet<Attempt> Attempts => Set<Attempt>();

    public DbSet<PendingExercise> PendingExercises => Set<PendingExercise>();

    public DbSet<DiagnosticRecord> Diagnostics => Set<DiagnosticRecord>();

    public DbSet<Classroom> Classrooms => Set<Classroom>();

    public DbSet<ClassMember> ClassMembers => Set<ClassMember>();

    public DbSet<Alert> Alerts => Set<Alert>();

    public DbSet<LessonPackRecord> LessonPacks => Set<LessonPackRecord>();

    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

    public DbSet<Institution> Institutions => Set<Institution>();

    public DbSet<InstitutionMember> InstitutionMembers => Set<InstitutionMember>();

    public DbSet<TutorProfile> TutorProfiles => Set<TutorProfile>();

    public DbSet<Booking> Bookings => Set<Booking>();

    public DbSet<QuizAttempt> QuizAttempts => Set<QuizAttempt>();

    public DbSet<QuizAnswer> QuizAnswers => Set<QuizAnswer>();

    public DbSet<TopicSkill> TopicSkills => Set<TopicSkill>();

    public DbSet<SubjectLevel> SubjectLevels => Set<SubjectLevel>();

    public DbSet<XpEvent> XpEvents => Set<XpEvent>();

    public DbSet<GemEvent> GemEvents => Set<GemEvent>();

    public DbSet<StreakFreezeUse> StreakFreezeUses => Set<StreakFreezeUse>();

    public DbSet<LeagueMember> LeagueMembers => Set<LeagueMember>();

    public DbSet<RoleplaySession> RoleplaySessions => Set<RoleplaySession>();

    public DbSet<FriendQuest> FriendQuests => Set<FriendQuest>();

    public DbSet<BadgeAward> BadgeAwards => Set<BadgeAward>();

    public DbSet<QuestClaim> QuestClaims => Set<QuestClaim>();

    public DbSet<Duel> Duels => Set<Duel>();

    public DbSet<TeachSession> TeachSessions => Set<TeachSession>();

    public DbSet<CompanionMessage> CompanionMessages => Set<CompanionMessage>();

    public DbSet<Certificate> Certificates => Set<Certificate>();

    public DbSet<StudentIdea> Ideas => Set<StudentIdea>();

    public DbSet<IdeaVote> IdeaVotes => Set<IdeaVote>();

    public DbSet<AdventureRating> AdventureRatings => Set<AdventureRating>();

    public DbSet<ThinkingNote> ThinkingNotes => Set<ThinkingNote>();

    public DbSet<LearnerNote> LearnerNotes => Set<LearnerNote>();

    public DbSet<TeacherNote> TeacherNotes => Set<TeacherNote>();

    public DbSet<PipChallenge> PipChallenges => Set<PipChallenge>();

    public DbSet<FadingRun> FadingRuns => Set<FadingRun>();

    public DbSet<Material> Materials => Set<Material>();

    public DbSet<WritingSubmission> WritingSubmissions => Set<WritingSubmission>();

    public DbSet<Assignment> Assignments => Set<Assignment>();

    public DbSet<LearnerHidden> LearnerHidden => Set<LearnerHidden>();

    public DbSet<StudyGoal> StudyGoals => Set<StudyGoal>();

    public DbSet<DefenceResult> DefenceResults => Set<DefenceResult>();

    public DbSet<CuriosityQuestion> CuriosityQuestions => Set<CuriosityQuestion>();

    public DbSet<FigureDiscovery> FigureDiscoveries => Set<FigureDiscovery>();

    /// <summary>Version of the schema created by EnsureCreated (stored in SQLite "PRAGMA user_version").</summary>
    public const int DatabaseVersion = 27;

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);

        // SQLite cannot sort or compare DateTimeOffset: store them as sortable numbers
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<DateTimeOffsetToBinaryConverter>();
        configurationBuilder.Properties<DateTimeOffset?>().HaveConversion<DateTimeOffsetToBinaryConverter>();
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        base.OnModelCreating(builder);

        builder.Entity<AppUser>().Property(u => u.DisplayName).HasMaxLength(60);
        builder.Entity<StudentProfile>().HasKey(p => p.UserId);
        builder.Entity<SubjectRecord>(e =>
        {
            e.Property(s => s.Id).HasMaxLength(80);
            e.Property(s => s.Name).HasMaxLength(120);
            e.HasIndex(s => s.OwnerId);
        });
        builder.Entity<MasteryRecord>().HasIndex(m => new { m.UserId, m.SubjectId, m.ConceptKey }).IsUnique();
        builder.Entity<Attempt>().HasIndex(a => new { a.UserId, a.SubjectId, a.At });
        builder.Entity<PendingExercise>().HasIndex(p => new { p.UserId, p.SubjectId, p.ConceptKey, p.Served });
        builder.Entity<DiagnosticRecord>().HasIndex(d => new { d.UserId, d.SubjectId }).IsUnique();
        builder.Entity<Classroom>(e =>
        {
            e.HasIndex(c => c.Code).IsUnique();
            e.Property(c => c.Code).HasMaxLength(12);
            e.Property(c => c.Name).HasMaxLength(80);
            e.HasMany(c => c.Members).WithOne().HasForeignKey(m => m.ClassroomId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<ClassMember>().HasKey(m => new { m.ClassroomId, m.UserId });
        builder.Entity<Alert>().HasIndex(a => new { a.StudentId, a.ResolvedAt });
        builder.Entity<LessonPackRecord>().HasIndex(p => p.TeacherId);
        builder.Entity<ChatMessage>().HasIndex(c => new { c.UserId, c.SubjectId, c.ConceptKey });
        builder.Entity<Institution>(e =>
        {
            e.HasIndex(i => i.Code).IsUnique();
            e.Property(i => i.Code).HasMaxLength(12);
            e.Property(i => i.Name).HasMaxLength(100);
            e.HasMany(i => i.Members).WithOne().HasForeignKey(m => m.InstitutionId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<InstitutionMember>(e =>
        {
            e.HasKey(m => new { m.InstitutionId, m.UserId });
            e.HasIndex(m => m.UserId);
        });
        builder.Entity<TutorProfile>(e =>
        {
            e.HasKey(t => t.UserId);
            e.Property(t => t.PricePerHour).HasConversion<double>();
        });
        builder.Entity<Booking>(e =>
        {
            e.HasIndex(b => new { b.TutorId, b.Start });
            e.HasIndex(b => new { b.StudentId, b.Start });
            e.Property(b => b.Price).HasConversion<double>();
            e.Property(b => b.CommissionRate).HasConversion<double>();
            e.Property(b => b.Commission).HasConversion<double>();
        });
        builder.Entity<StudentProfile>().Property(p => p.Nickname).HasMaxLength(40);
        builder.Entity<QuizAttempt>(e =>
        {
            e.Property(a => a.Id).HasMaxLength(40);
            e.HasIndex(a => new { a.UserId, a.FinishedAt });
            e.HasIndex(a => a.DuelId);
        });
        builder.Entity<QuizAnswer>(e =>
        {
            e.HasIndex(a => new { a.UserId, a.At });
            e.HasIndex(a => new { a.UserId, a.Area, a.Level });
            e.HasIndex(a => a.AttemptId);
        });
        builder.Entity<TopicSkill>().HasKey(t => new { t.UserId, t.Area, t.Level, t.Topic });
        builder.Entity<SubjectLevel>().HasKey(s => new { s.UserId, s.Area });
        builder.Entity<XpEvent>().HasIndex(x => new { x.UserId, x.At });
        builder.Entity<GemEvent>().HasIndex(g => new { g.UserId, g.At });
        builder.Entity<StreakFreezeUse>().HasIndex(f => new { f.UserId, f.Day }).IsUnique();
        builder.Entity<LeagueMember>().HasIndex(m => new { m.UserId, m.Week }).IsUnique();
        builder.Entity<LeagueMember>().HasIndex(m => new { m.Week, m.Group });
        builder.Entity<RoleplaySession>().HasIndex(r => new { r.UserId, r.StartedAt });
        builder.Entity<FriendQuest>().HasIndex(f => new { f.Week, f.UserA });
        builder.Entity<FriendQuest>().HasIndex(f => new { f.Week, f.UserB });
        builder.Entity<BadgeAward>().HasKey(b => new { b.UserId, b.Key });
        builder.Entity<QuestClaim>().HasKey(q => new { q.UserId, q.Day, q.Key });
        builder.Entity<CompanionMessage>(e => e.HasIndex(m => new { m.UserId, m.At }));
        builder.Entity<ThinkingNote>(e =>
        {
            e.HasIndex(t => new { t.AttemptId, t.Number }).IsUnique();
            e.HasIndex(t => new { t.UserId, t.At });
            e.Property(t => t.AttemptId).HasMaxLength(40);
        });
        builder.Entity<StudyGoal>(e =>
        {
            e.HasKey(g => g.UserId);
            e.Property(g => g.Title).HasMaxLength(60);
        });
        builder.Entity<DefenceResult>(e =>
        {
            e.HasIndex(d => new { d.UserId, d.At });
            e.HasIndex(d => new { d.AttemptId, d.Topic }).IsUnique();
            e.Property(d => d.AttemptId).HasMaxLength(40);
        });
        builder.Entity<Material>(e =>
        {
            e.HasIndex(m => m.ClassroomId);
            e.Property(m => m.Title).HasMaxLength(80);
            e.Property(m => m.Key).HasMaxLength(40);
        });
        builder.Entity<WritingSubmission>(e => e.HasIndex(w => new { w.UserId, w.At }));
        builder.Entity<Assignment>(e => e.HasIndex(a => a.ClassroomId));
        builder.Entity<FadingRun>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Id).HasMaxLength(40);
            e.HasIndex(c => new { c.UserId, c.At });
        });
        builder.Entity<PipChallenge>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Id).HasMaxLength(40);
            e.HasIndex(c => new { c.UserId, c.At });
        });
        builder.Entity<TeacherNote>(e =>
        {
            e.HasIndex(n => n.StudentId);
            e.Property(n => n.Text).HasMaxLength(160);
        });
        builder.Entity<LearnerNote>(e =>
        {
            e.HasIndex(n => n.UserId);
            e.Property(n => n.Text).HasMaxLength(160);
        });
        builder.Entity<LearnerHidden>(e =>
        {
            e.HasKey(h => new { h.UserId, h.Key });
            e.Property(h => h.Key).HasMaxLength(40);
        });
        builder.Entity<StudentIdea>(e =>
        {
            e.HasIndex(i => new { i.Kind, i.Key }).IsUnique();
            e.Property(i => i.Key).HasMaxLength(80);
            e.Property(i => i.Text).HasMaxLength(80);
            e.HasMany(i => i.Votes).WithOne().HasForeignKey(v => v.IdeaId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<IdeaVote>(e =>
        {
            e.HasKey(v => new { v.IdeaId, v.UserId });
            e.HasIndex(v => new { v.UserId, v.At });
            e.Property(v => v.Reason).HasMaxLength(160);
        });
        builder.Entity<AdventureRating>(e =>
        {
            e.HasKey(r => r.AttemptId);
            e.Property(r => r.AttemptId).HasMaxLength(40);
            e.HasIndex(r => r.Theme);
        });
        builder.Entity<Certificate>(e =>
        {
            e.HasIndex(c => new { c.UserId, c.SubjectKey }).IsUnique();
            e.HasIndex(c => c.Code).IsUnique();
            e.Property(c => c.Code).HasMaxLength(20);
        });
        builder.Entity<TeachSession>(e =>
        {
            e.Property(t => t.Id).HasMaxLength(40);
            e.HasIndex(t => new { t.UserId, t.Concept });
        });
        builder.Entity<Duel>(e =>
        {
            e.HasIndex(d => d.ChallengerId);
            e.HasIndex(d => d.OpponentId);
        });
        builder.Entity<CuriosityQuestion>(e =>
        {
            e.Property(c => c.Text).HasMaxLength(200);
            e.HasIndex(c => new { c.UserId, c.Status, c.CreatedAt });
        });
        builder.Entity<FigureDiscovery>(e =>
        {
            e.Property(f => f.FigureKey).HasMaxLength(40);
            e.HasIndex(f => new { f.UserId, f.FigureKey }).IsUnique();
        });
    }
}
