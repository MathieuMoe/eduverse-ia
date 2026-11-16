using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduVerse.Web.Pages.Student;

/// <summary>"Defend your answers": the same topics of a finished test again with other numbers, and an explanation of why.</summary>
public sealed class DefendModel : PageModel
{
    public string Attempt { get; private set; } = "";

    public void OnGet(string? attempt) => Attempt = attempt is { Length: > 0 and <= 40 } && attempt.All(char.IsLetterOrDigit) ? attempt : "";
}
