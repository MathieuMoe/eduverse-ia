using EduVerse.Infrastructure.Tutor;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduVerse.Web.Pages.Student;

/// <summary>The documents the student's teachers added: to read, to be tested on, and that the Tutor takes into account.</summary>
public sealed class MaterialsModel(MaterialStore materials) : PageModel
{
    public IReadOnlyList<StudentMaterial> Items { get; private set; } = [];

    public async Task OnGetAsync() => Items = await materials.ForStudentAsync(User.Id(), HttpContext.RequestAborted);
}
