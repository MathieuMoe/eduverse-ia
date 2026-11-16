using EduVerse.Core.Tutor;
using EduVerse.Infrastructure.Tutor;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;

namespace EduVerse.Web.Pages.Student;

/// <summary>The week as a letter for the family: written by code from measured facts, printable, nothing is sent anywhere.</summary>
public sealed class LetterModel(LetterStore letters, IStringLocalizer<SharedResource> l) : PageModel
{
    public Letter? Letter { get; private set; }

    public async Task OnGetAsync()
    {
        var facts = await letters.FactsAsync(User.Id(), User.DisplayName().Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "", HttpContext.RequestAborted);
        if (!WeeklyLetter.HasContent(facts))
        {
            return;
        }

        Letter = WeeklyLetter.Build(facts with
        {
            BestTopic = facts.BestTopic is null ? null : QuizEndpoints.TopicName(facts.BestTopic, l),
            WorkTopic = facts.WorkTopic is null ? null : QuizEndpoints.TopicName(facts.WorkTopic, l),
            SureWrongTopic = facts.SureWrongTopic is null ? null : QuizEndpoints.TopicName(facts.SureWrongTopic, l),
            UnsureRightTopic = facts.UnsureRightTopic is null ? null : QuizEndpoints.TopicName(facts.UnsureRightTopic, l),
        }, Localization.LanguageForCurrentCulture());
    }
}
