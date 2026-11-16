using EduVerse.Core.Tutor;
using EduVerse.Infrastructure.Data;
using EduVerse.Infrastructure.Tutor;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EduVerse.Web.Pages.Student;

/// <summary>"How I learn": what the tutor measured and noticed, open to the student, who can correct it and add their own notes.</summary>
public sealed class LearnerModel(LearnerStore learner, DetectiveStore detective, EduVerseDb db, Microsoft.Extensions.Localization.IStringLocalizer<SharedResource> l) : PageModel
{
    public LearnerStats Stats { get; private set; } = LearnerStats.Empty;

    public IReadOnlyList<Insight> Insights { get; private set; } = [];

    public IReadOnlyList<LearnerNote> Notes { get; private set; } = [];

    public IReadOnlyList<string> Interests { get; private set; } = [];

    /// <summary>Weak topics with the step below and what the detective says (already in the student's language).</summary>
    public IReadOnlyList<(DetectiveCase Case, string Topic, string Below, string Text)> Cases { get; private set; } = [];

    /// <summary>Topics answered "sure" and wrong several times.</summary>
    public IReadOnlyList<(FalseCertainty Item, string Topic)> Certainties { get; private set; } = [];

    /// <summary>The confidence map: four boxes with counts and topic names, the score and the sentence about it.</summary>
    public IReadOnlyList<(QuadrantCell Cell, IReadOnlyList<string> Topics)> Map { get; private set; } = [];

    public int Bets { get; private set; }

    public string CalibrationText { get; private set; } = "";

    public async Task OnGetAsync()
    {
        var userId = User.Id();
        Interests = EduVerse.Core.Tutor.Interests.Parse(await db.StudentProfiles.AsNoTracking().Where(p => p.UserId == userId).Select(p => p.Interests).FirstOrDefaultAsync(HttpContext.RequestAborted));
        Stats = await learner.StatsAsync(User.Id(), HttpContext.RequestAborted);
        Insights = LearnerProfile.Insights(Stats, Localization.LanguageForCurrentCulture());
        Notes = await learner.NotesAsync(User.Id(), HttpContext.RequestAborted);
        var language = Localization.LanguageForCurrentCulture();
        Cases = (await detective.CasesAsync(userId, HttpContext.RequestAborted)).Select(c =>
        {
            var (topic, below) = (QuizEndpoints.TopicName(c.Topic, l), QuizEndpoints.TopicName(c.Below.Topic, l));
            return (c, topic, below, Prerequisites.Say(c.Verdict, topic, below, language));
        }).ToList();
        var bets = await detective.BetsAsync(userId, HttpContext.RequestAborted);
        Bets = bets.Count;
        Map = Calibration.Map(bets).Select(c => (c, (IReadOnlyList<string>)c.Topics.Select(t => QuizEndpoints.TopicName(t, l)).ToList())).ToList();
        CalibrationText = Calibration.Say(Calibration.Score(bets), Calibration.Trend(bets), language);
        Certainties = (await detective.FalseCertaintiesAsync(userId, HttpContext.RequestAborted)).Select(f => (f, QuizEndpoints.TopicName(f.Topic, l))).ToList();
    }
}
