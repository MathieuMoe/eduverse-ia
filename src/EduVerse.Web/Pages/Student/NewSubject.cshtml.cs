using EduVerse.Core.Common;
using EduVerse.Infrastructure.Learning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduVerse.Web.Pages.Student;

public sealed class NewSubjectModel(LearningStore store) : PageModel
{
    public Stage Stage { get; private set; }

    /// <summary>Pre-fills the topic field, e.g. when a student arrives here from a curiosity question they want to explore.</summary>
    [BindProperty(SupportsGet = true)]
    public string? Topic { get; set; }

    public async Task OnGetAsync() => Stage = (await store.ProfileAsync(User.Id(), HttpContext.RequestAborted)).Stage;
}
