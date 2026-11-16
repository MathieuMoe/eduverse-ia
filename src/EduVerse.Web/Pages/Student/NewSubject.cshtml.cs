using EduVerse.Core.Common;
using EduVerse.Infrastructure.Learning;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduVerse.Web.Pages.Student;

public sealed class NewSubjectModel(LearningStore store) : PageModel
{
    public Stage Stage { get; private set; }

    public async Task OnGetAsync() => Stage = (await store.ProfileAsync(User.Id(), HttpContext.RequestAborted)).Stage;
}
