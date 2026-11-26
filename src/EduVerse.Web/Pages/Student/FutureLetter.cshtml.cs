using EduVerse.Core.Tutor;
using EduVerse.Infrastructure.Tutor;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduVerse.Web.Pages.Student;

/// <summary>A one-off letter from the student's own future self, shown when they reach a round number of topics
/// mastered: built entirely by code from measured facts, nothing is sent anywhere.</summary>
public sealed class FutureLetterModel(FutureSelfStore futureSelf) : PageModel
{
    public FutureSelfLetter? Letter { get; private set; }

    public async Task OnGetAsync()
    {
        var firstName = User.DisplayName().Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
        var language = Localization.LanguageForCurrentCulture();
        var facts = await futureSelf.FactsAsync(User.Id(), firstName, language, HttpContext.RequestAborted);
        if (!FutureSelf.HasContent(facts))
        {
            return;
        }

        Letter = FutureSelf.Build(facts, language);
    }
}
