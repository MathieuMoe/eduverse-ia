using EduVerse.Core.Common;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduVerse.Web.Pages.Teacher;

public sealed class SummaryModel : PageModel
{
    public TeachLanguage DefaultLanguage { get; private set; }

    public void OnGet() => DefaultLanguage = Localization.LanguageForCurrentCulture();
}
