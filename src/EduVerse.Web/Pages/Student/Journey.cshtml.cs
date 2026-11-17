using EduVerse.Core.Common;
using EduVerse.Core.Tutor;
using EduVerse.Infrastructure.Tutor;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;

namespace EduVerse.Web.Pages.Student;

/// <summary>"My path": the student's own evolution told as a story, built only from facts already measured elsewhere.</summary>
public sealed class JourneyModel(JourneyStore journey, IStringLocalizer<SharedResource> l) : PageModel
{
    public IReadOnlyList<Milestone> Milestones { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var candidates = await journey.CandidatesAsync(User.Id(), HttpContext.RequestAborted);
        var language = Localization.LanguageForCurrentCulture();
        var list = new List<Milestone>();
        foreach (var t in candidates.Turnarounds)
        {
            list.Add(new Milestone(MilestoneKind.TopicTurnaround, t.ToMonth, "📈", Journey.Say(t, QuizEndpoints.TopicName(t.Topic, l), language)));
        }

        if (candidates.FirstDefence is { } defence)
        {
            list.Add(new Milestone(MilestoneKind.DefenceVerified, defence.Date, "🛡️", Journey.SayDefence(QuizEndpoints.TopicName(defence.Topic, l), language)));
        }

        if (candidates.FirstTaught is { } taught)
        {
            var name = TeachConcepts.Find(taught.ConceptKey)?.Title.In(language) ?? taught.ConceptKey;
            list.Add(new Milestone(MilestoneKind.ConceptTaught, taught.Date, "🗣️", Journey.SayTaught(name, language)));
        }

        if (candidates.BestStreakDays >= Journey.MinStreakDays && candidates.BestStreakEnd is { } end)
        {
            list.Add(new Milestone(MilestoneKind.Streak, end, "🔥", Journey.SayStreak(candidates.BestStreakDays, language)));
        }

        Milestones = list.OrderBy(m => m.Date).ToList();
    }
}
