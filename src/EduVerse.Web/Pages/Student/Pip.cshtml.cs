using EduVerse.Core.Tutor;
using EduVerse.Infrastructure.Gamification;
using EduVerse.Infrastructure.Tutor;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduVerse.Web.Pages.Student;

/// <summary>"Correct Pip": Pip, a student-AI, solves a problem with one mistake and the student finds it. Teaching by correcting.</summary>
public sealed class PipModel(GamificationStore game, PipStore pips) : PageModel
{
    public bool Available { get; private set; }

    public int Solved { get; private set; }

    public IReadOnlyList<PipStat> Stats { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Available = Pip.Available((await game.ProfileAsync(User.Id(), HttpContext.RequestAborted)).Level);
        Stats = await pips.StatsAsync(User.Id(), HttpContext.RequestAborted);
        Solved = await pips.SolvedAsync(User.Id(), HttpContext.RequestAborted);
    }
}
