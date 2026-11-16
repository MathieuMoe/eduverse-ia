using System.ComponentModel.DataAnnotations;
using EduVerse.Core.Common;
using EduVerse.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduVerse.Web.Pages.Account;

/// <summary>New account as a student or a teacher. Only a first name is asked (data minimisation).</summary>
public sealed class RegisterModel(SignInManager<AppUser> signIn, UserManager<AppUser> users, EduVerseDb db, TimeProvider clock,
    EduVerse.Infrastructure.Institutions.InstitutionStore institutions, EduVerse.Infrastructure.Marketplace.MarketplaceStore marketplace) : PageModel
{
    [BindProperty]
    [Required]
    [StringLength(40, MinimumLength = 2)]
    public string FirstName { get; set; } = "";

    [BindProperty]
    [Required]
    [EmailAddress]
    public string Email { get; set; } = "";

    [BindProperty]
    [Required]
    [StringLength(100, MinimumLength = 8)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [BindProperty]
    public string Role { get; set; } = Roles.Student;

    [BindProperty]
    public Stage Stage { get; set; } = Stage.Secondary;

    /// <summary>Name of the academy, school or centre (directors only).</summary>
    [BindProperty]
    public string? InstitutionName { get; set; }

    public IReadOnlyList<string> Errors { get; private set; } = [];

    public IActionResult OnGet() => User.Identity?.IsAuthenticated == true ? LocalRedirect(User.Home()) : Page();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!Roles.All.Contains(Role))
        {
            Role = Roles.Student;
        }

        if (Role == Roles.Director && (InstitutionName ?? "").Trim().Length is < 2 or > 100)
        {
            ModelState.AddModelError(nameof(InstitutionName), "required");
        }

        if (!ModelState.IsValid)
        {
            Errors = ModelState.Where(e => e.Value?.Errors.Count > 0).Select(e => "Invalid_" + e.Key).ToList();
            return Page();
        }

        var user = new AppUser { UserName = Email.Trim(), Email = Email.Trim(), DisplayName = FirstName.Trim().Split(' ')[0], CreatedAt = clock.GetUtcNow() };
        var result = await users.CreateAsync(user, Password);
        if (!result.Succeeded)
        {
            Errors = result.Errors.Select(e => e.Code).ToList();
            return Page();
        }

        await users.AddToRoleAsync(user, Role);
        if (Role == Roles.Student)
        {
            db.StudentProfiles.Add(new StudentProfile { UserId = user.Id, Stage = Stage, Language = Localization.LanguageForCurrentCulture() });
            await db.SaveChangesAsync();
        }

        if (Role == Roles.Director)
        {
            await institutions.CreateAsync(InstitutionName!, user.Id);
        }

        if (Role == Roles.Tutor)
        {
            await marketplace.ProfileAsync(user.Id);
        }

        await signIn.SignInAsync(user, isPersistent: true);
        return LocalRedirect(Role == Roles.Tutor ? "/Tutor/Profile" : CurrentUser.HomeFor(Role));
    }
}
