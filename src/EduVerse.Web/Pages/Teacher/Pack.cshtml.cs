using EduVerse.Core.Common;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduVerse.Web.Pages.Teacher;

/// <summary>Lesson pack from a document: class plan, slides, worksheet, 10 exercises, exam with solutions, rubric (pack.js + /api/packs).</summary>
public sealed class PackModel : PageModel
{
    public TeachLanguage DefaultLanguage { get; private set; }

    public void OnGet() => DefaultLanguage = Localization.LanguageForCurrentCulture();
}
