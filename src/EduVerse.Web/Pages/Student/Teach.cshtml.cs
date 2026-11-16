using EduVerse.Core.Tutor;
using EduVerse.Infrastructure.Tutor;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduVerse.Web.Pages.Student;

/// <summary>"Explain it to Evo": the list of concepts, or a session with one concept.</summary>
public sealed class TeachModel(TeachStore store) : PageModel
{
    public TeachConcept? Concept { get; private set; }

    public IReadOnlyDictionary<string, int> Best { get; private set; } = new Dictionary<string, int>();

    public async Task OnGetAsync(string? concept)
    {
        Concept = TeachConcepts.Find(concept);
        Best = await store.BestAsync(User.Id(), HttpContext.RequestAborted);
    }
}
