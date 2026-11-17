using System.Security.Claims;
using EduVerse.Core.Catalog;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EduVerse.Infrastructure.Data;

/// <summary>Adds the first name (and, for a student, whether they are in kids mode) to the sign-in cookie so the
/// layout can greet the user and pick the right nav without a database query on every page.</summary>
public sealed class AppClaimsFactory(UserManager<AppUser> users, RoleManager<IdentityRole> roles, IOptions<IdentityOptions> options, EduVerseDb db)
    : UserClaimsPrincipalFactory<AppUser, IdentityRole>(users, roles, options)
{
    public const string DisplayNameClaim = "display_name";

    /// <summary>"1" when the student's profile level is young enough for kids mode (see <see cref="Levels.IsKids"/>); absent for non-students.
    /// Baked in at sign-in like <see cref="DisplayNameClaim"/>, so it can go stale until the next sign-in if the profile level changes mid-session.</summary>
    public const string KidClaim = "is_kid";

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(AppUser user)
    {
        ArgumentNullException.ThrowIfNull(user);
        var identity = await base.GenerateClaimsAsync(user).ConfigureAwait(false);
        identity.AddClaim(new Claim(DisplayNameClaim, user.DisplayName));
        var level = await db.StudentProfiles.AsNoTracking().Where(p => p.UserId == user.Id)
            .Select(p => (Level?)p.Level).FirstOrDefaultAsync().ConfigureAwait(false);
        if (level is { } studentLevel)
        {
            identity.AddClaim(new Claim(KidClaim, Levels.IsKids(studentLevel) ? "1" : "0"));
        }

        return identity;
    }
}
