using EduVerse.Core.Catalog;
using EduVerse.Core.Curriculum;
using EduVerse.Core.Tutor;
using EduVerse.Infrastructure.Tutor;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduVerse.Web.Pages.Student;

/// <summary>A short script to teach a parent or sibling what the student just mastered: the explanation and the
/// check-questions come straight from a real exercise of the topic, built entirely by code. Nothing is sent anywhere.</summary>
public sealed class FamilyModel(FamilyStore family, ICatalogNames names) : PageModel
{
    public FamilyScript? Script { get; private set; }

    public async Task OnGetAsync()
    {
        var milestone = await family.RecentMilestoneAsync(User.Id(), HttpContext.RequestAborted);
        if (milestone is null)
        {
            return;
        }

        var language = Localization.LanguageForCurrentCulture();
        var firstName = User.DisplayName().Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
        var sample = QuestionBanks.ForTopic(milestone.Area, milestone.Level, milestone.Topic, 2, 11, language);
        var checks = new List<CheckQuestion> { new(sample.Prompt, sample.Options[sample.CorrectIndex]) };
        foreach (var seed in new[] { 29, 53, 71 })
        {
            if (checks.Count >= 2)
            {
                break;
            }

            var other = QuestionBanks.ForTopic(milestone.Area, milestone.Level, milestone.Topic, 2, seed, language);
            if (!checks.Any(c => c.Question == other.Prompt))
            {
                checks.Add(new(other.Prompt, other.Options[other.CorrectIndex]));
            }
        }

        if (checks.Count < TeachFamily.MinChecks)
        {
            return;
        }

        var facts = new MilestoneFacts(firstName, names.Topic(milestone.Topic), names.Area(milestone.Area), sample.Explanation, checks);
        Script = TeachFamily.Build(facts, language);
    }
}
