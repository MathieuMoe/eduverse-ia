using System.Security.Claims;
using EduVerse.Infrastructure.Data;

namespace EduVerse.Web;

public static class CurrentUser
{
    public const string StudentPolicy = "Student";
    public const string TeacherPolicy = "Teacher";
    public const string DirectorPolicy = "Director";
    public const string TutorPolicy = "Tutor";

    /// <summary>Adults who review the ideas of the students: teachers and directors.</summary>
    public const string ModeratorPolicy = "Moderator";

    public static string Id(this ClaimsPrincipal user)
    {
        ArgumentNullException.ThrowIfNull(user);
        return user.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("Not signed in.");
    }

    public static string DisplayName(this ClaimsPrincipal user)
    {
        ArgumentNullException.ThrowIfNull(user);
        return user.FindFirstValue(AppClaimsFactory.DisplayNameClaim) ?? user.Identity?.Name ?? "";
    }

    public static bool IsStudent(this ClaimsPrincipal user) => user?.IsInRole(Roles.Student) == true;

    public static bool IsTeacher(this ClaimsPrincipal user) => user?.IsInRole(Roles.Teacher) == true;

    public static bool IsDirector(this ClaimsPrincipal user) => user?.IsInRole(Roles.Director) == true;

    public static bool IsTutor(this ClaimsPrincipal user) => user?.IsInRole(Roles.Tutor) == true;

    /// <summary>Where a signed-in user lands: the space of the role.</summary>
    public static string Home(this ClaimsPrincipal user) =>
        user.IsDirector() ? "/Institution" : user.IsTutor() ? "/Tutor" : user.IsTeacher() ? "/Teacher" : user.IsStudent() ? "/Student" : "/";

    public static string HomeFor(string role) => role switch
    {
        Roles.Director => "/Institution",
        Roles.Tutor => "/Tutor",
        Roles.Teacher => "/Teacher",
        _ => "/Student",
    };

    public static string Initials(string name) =>
        string.Concat(name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(p => char.ToUpperInvariant(p[0])));
}
