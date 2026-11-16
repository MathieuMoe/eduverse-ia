using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace EduVerse.Infrastructure.Data;

/// <summary>Adds the first name to the sign-in cookie so the layout can greet the user without a database query.</summary>
public sealed class AppClaimsFactory(UserManager<AppUser> users, RoleManager<IdentityRole> roles, IOptions<IdentityOptions> options)
    : UserClaimsPrincipalFactory<AppUser, IdentityRole>(users, roles, options)
{
    public const string DisplayNameClaim = "display_name";

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(AppUser user)
    {
        ArgumentNullException.ThrowIfNull(user);
        var identity = await base.GenerateClaimsAsync(user).ConfigureAwait(false);
        identity.AddClaim(new Claim(DisplayNameClaim, user.DisplayName));
        return identity;
    }
}
