using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.WebEncoders;
using EduVerse.Infrastructure;
using EduVerse.Infrastructure.Data;
using EduVerse.Infrastructure.Learning;
using EduVerse.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLocalization(o => o.ResourcesPath = "Resources");
builder.Services.Configure<RequestLocalizationOptions>(o => o
    .SetDefaultCulture("en")
    .AddSupportedCultures(Localization.SupportedCultures)
    .AddSupportedUICultures(Localization.SupportedCultures));
builder.Services.AddRazorPages(o =>
{
    o.Conventions.AuthorizeFolder("/Student", CurrentUser.StudentPolicy);
    o.Conventions.AuthorizeFolder("/Teacher", CurrentUser.TeacherPolicy);
    o.Conventions.AuthorizeFolder("/Institution", CurrentUser.DirectorPolicy);
    o.Conventions.AuthorizeFolder("/Tutor", CurrentUser.TutorPolicy);
    o.Conventions.AuthorizeFolder("/Moderation", CurrentUser.ModeratorPolicy);
    o.Conventions.AuthorizePage("/Marketplace/Bookings", CurrentUser.StudentPolicy);
});
// Write accented characters (é, ñ, ç, à…) as-is in the HTML instead of &#xE9; entities.
builder.Services.Configure<WebEncoderOptions>(o => o.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));
builder.Services.AddSingleton<EduVerse.Core.Curriculum.ICatalogNames, EduVerse.Web.LocalizedCatalogNames>();
builder.Services.AddEduVerse(builder.Configuration);
builder.Services.AddEduVerseData(builder.Configuration);
builder.Services.ConfigureApplicationCookie(o =>
{
    o.LoginPath = "/Account/Login";
    o.AccessDeniedPath = "/";
    o.Cookie.Name = "eduverse_auth";
    o.Cookie.SameSite = SameSiteMode.Lax;
    o.SlidingExpiration = true;
    o.ExpireTimeSpan = TimeSpan.FromDays(14);

    // APIs answer 401/403 instead of redirecting to the login page
    o.Events.OnRedirectToLogin = c => Deny(c, StatusCodes.Status401Unauthorized);
    o.Events.OnRedirectToAccessDenied = c => Deny(c, StatusCodes.Status403Forbidden);
});
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(CurrentUser.StudentPolicy, p => p.RequireRole(Roles.Student))
    .AddPolicy(CurrentUser.TeacherPolicy, p => p.RequireRole(Roles.Teacher))
    .AddPolicy(CurrentUser.DirectorPolicy, p => p.RequireRole(Roles.Director))
    .AddPolicy(CurrentUser.TutorPolicy, p => p.RequireRole(Roles.Tutor))
    .AddPolicy(CurrentUser.ModeratorPolicy, p => p.RequireRole(Roles.Teacher, Roles.Director));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHealthChecks();
builder.Services.AddProblemDetails();
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Protects the (paid) AI API from abuse: 10 AI calls per minute per client IP.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("ai", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));

    // Practice loop (exercises and answers): generous, but still a ceiling
    options.AddPolicy("learn", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 40, Window = TimeSpan.FromMinutes(1) }));

    // Explain it to Evo and interviews: short messages, often answered offline
    options.AddPolicy("ideas", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 30, Window = TimeSpan.FromMinutes(1) }));
    options.AddPolicy("tutor", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 30, Window = TimeSpan.FromMinutes(1) }));

    // Tests of the catalog: no AI call, one request per answer, so a larger budget of its own
    options.AddPolicy("quiz", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 120, Window = TimeSpan.FromMinutes(1) }));
});

var app = builder.Build();

// Development and demo: create the SQLite database and the demo accounts (teacher + 4 students) on start
await app.Services.InitializeDatabaseAsync(seedDemo: app.Configuration.GetValue("Demo:Enabled", true), allowReset: app.Environment.IsDevelopment());

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRequestLocalization();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapRazorPages();
app.MapHealthChecks("/health");

// Interface language switcher: stores the choice in the standard ASP.NET Core culture cookie.
app.MapGet("/culture/{culture}", async (string culture, string? returnUrl, HttpContext context, LearningStore store) =>
{
    if (!Localization.SupportedCultures.Contains(culture))
    {
        culture = "en";
    }

    context.Response.Cookies.Append(
        CookieRequestCultureProvider.DefaultCookieName,
        CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
        new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true, SameSite = SameSiteMode.Lax });

    // A student's explanations follow the interface language: remember it for the next sign-in
    if (context.User.IsStudent())
    {
        var profile = await store.ProfileAsync(context.User.Id(), context.RequestAborted);
        profile.Language = Localization.LanguageFor(culture);
        await store.SaveProfileAsync(profile, context.RequestAborted);
    }

    return Results.LocalRedirect(Localization.IsLocalUrl(returnUrl) ? returnUrl! : "/");
});

app.MapTeaching();
app.MapLearning();
app.MapQuiz();
app.MapGame();
app.MapRoleplay();
app.MapCountries();
app.MapTutor();
app.MapTeacherCompanion();
app.MapIdeas();
app.MapLearner();
app.MapPlan();
app.MapHomework();
app.MapDefence();
app.MapPip();
app.MapPipClaims();
app.MapFading();
app.MapMaterials();
app.MapContent();
app.MapInstitution();
app.MapEngineSelection();

await app.RunAsync();

static Task Deny(Microsoft.AspNetCore.Authentication.RedirectContext<Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions> c, int status)
{
    if (c.Request.Path.StartsWithSegments("/api", StringComparison.Ordinal))
    {
        c.Response.StatusCode = status;
    }
    else
    {
        c.Response.Redirect(c.RedirectUri);
    }

    return Task.CompletedTask;
}

/// <summary>Exposed for integration tests (WebApplicationFactory).</summary>
public partial class Program;

internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "AI generation failed")]
    public static partial void AiFailed(ILogger logger, Exception ex);
}
