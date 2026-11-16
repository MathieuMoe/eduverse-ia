using System.ComponentModel.DataAnnotations;
using EduVerse.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EduVerse.Web.Pages.Account;

public sealed class LoginModel(SignInManager<AppUser> signIn, UserManager<AppUser> users, EduVerseDb db, IConfiguration configuration) : PageModel
{
    [BindProperty]
    [Required]
    [EmailAddress]
    public string Email { get; set; } = "";

    [BindProperty]
    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public bool Failed { get; private set; }

    public bool DemoEnabled => configuration.GetValue("Demo:Enabled", true);

    public IActionResult OnGet() => User.Identity?.IsAuthenticated == true ? LocalRedirect(User.Home()) : Page();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            Failed = true;
            return Page();
        }

        var result = await signIn.PasswordSignInAsync(Email.Trim(), Password, isPersistent: true, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            Failed = true;
            return Page();
        }

        var user = await users.FindByNameAsync(Email.Trim());
        return await LandAsync(user!);
    }

    /// <summary>"Try as a student / teacher": signs in a demo account without a password.</summary>
    public async Task<IActionResult> OnPostDemoAsync(string role)
    {
        if (!DemoEnabled)
        {
            return NotFound();
        }

        var email = role switch
        {
            Roles.Teacher => DemoSeeder.TeacherEmail,
            Roles.Director => DemoSeeder.DirectorEmail,
            Roles.Tutor => DemoSeeder.TutorEmail,
            "Kid" => DemoSeeder.KidEmail,
            _ => DemoSeeder.DemoStudentEmail,
        };
        var user = await users.FindByEmailAsync(email);
        if (user is null)
        {
            return RedirectToPage();
        }

        await signIn.SignInAsync(user, isPersistent: false);
        return await LandAsync(user, demo: true);
    }

    private async Task<IActionResult> LandAsync(AppUser user, bool demo = false)
    {
        // A student finds the interface (and the explanations) in the language chosen last time, unless one is already chosen
        if (!demo && !Request.Cookies.ContainsKey(CookieRequestCultureProvider.DefaultCookieName)
            && await db.StudentProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == user.Id) is { } profile)
        {
            Response.Cookies.Append(CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(Localization.CultureFor(profile.Language))),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true, SameSite = SameSiteMode.Lax });
        }

        if (Localization.IsLocalUrl(ReturnUrl))
        {
            return LocalRedirect(ReturnUrl!);
        }

        var roles = await users.GetRolesAsync(user);
        return LocalRedirect(CurrentUser.HomeFor(Roles.All.FirstOrDefault(roles.Contains) ?? Roles.Student));
    }
}
