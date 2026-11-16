using EduVerse.Core.Content;
using EduVerse.Core.Learning;
using EduVerse.Infrastructure.Data;
using EduVerse.Infrastructure.Institutions;
using EduVerse.Infrastructure.Learning;
using EduVerse.Infrastructure.Marketplace;
using EduVerse.Infrastructure.Teaching;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using EduVerse.Infrastructure.Claude;
using EduVerse.Infrastructure.Engines;
using EduVerse.Infrastructure.OpenAi;

namespace EduVerse.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the AI engines. Claude, ChatGPT, Gemini and Ollama are offered when configured;
    /// the offline demo is always available. The user picks the engine in the interface.
    /// </summary>
    public static IServiceCollection AddEduVerse(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<AiOptions>().Bind(configuration.GetSection(AiOptions.SectionName));
        services.AddOptions<ClaudeOptions>()
            .Bind(configuration.GetSection(ClaudeOptions.SectionName))
            .PostConfigure(o => o.ApiKey ??= configuration["ANTHROPIC_API_KEY"]);

        services.AddHttpClient(AiEngines.ClaudeId, (sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<ClaudeOptions>>().Value;
            client.BaseAddress = options.BaseUrl;
            client.Timeout = options.Timeout;
            if (!string.IsNullOrWhiteSpace(options.ApiKey))
            {
                client.DefaultRequestHeaders.Add("x-api-key", options.ApiKey);
            }

            client.DefaultRequestHeaders.Add("anthropic-version", options.ApiVersion);
        });

        AddOpenAiCompatible(services, configuration, OpenAiCompatibleOptions.OpenAi, AiEngines.OpenAiId,
            "gpt-5-mini", "https://api.openai.com/v1/", "OPENAI_API_KEY");
        AddOpenAiCompatible(services, configuration, OpenAiCompatibleOptions.Gemini, AiEngines.GeminiId,
            "gemini-flash-lite-latest", "https://generativelanguage.googleapis.com/v1beta/", "GEMINI_API_KEY", googleKeyHeader: true);
        AddOpenAiCompatible(services, configuration, OpenAiCompatibleOptions.Ollama, AiEngines.OllamaId,
            "llama3.2", "http://localhost:11434/v1/", apiKeyVariable: null);

        services.AddSingleton<Core.Lessons.DemoLessonAdapter>();
        services.AddSingleton<Core.Summaries.DemoClassSummarizer>();
        services.AddSingleton<DemoLearningAi>();
        services.AddSingleton<DemoPackGenerator>();
        services.AddScoped<AiEngines>();
        services.AddScoped<LearningStore>();
        services.AddScoped<ClassroomStore>();
        services.AddScoped<InstitutionStore>();
        services.AddScoped<MarketplaceStore>();
        services.AddScoped<Gamification.GamificationStore>();
        services.AddScoped<Quizzes.QuizStore>();
        services.AddScoped<Tutor.TeachStore>();
        services.AddScoped<Tutor.CompanionStore>();
        services.AddSingleton<Core.Institutions.DemoFamilyWriter>();
        services.AddOptions<MarketplaceOptions>().Bind(configuration.GetSection(MarketplaceOptions.SectionName));
        return services;
    }

    public const string DefaultConnection = "Data Source=App_Data/eduverse.db";

    /// <summary>
    /// SQLite database and ASP.NET Core Identity (accounts with the Student or Teacher role).
    /// Connection string "ConnectionStrings:EduVerse"; switching to SQL Server is a provider change.
    /// </summary>
    public static IServiceCollection AddEduVerseData(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var connection = configuration.GetConnectionString("EduVerse") ?? DefaultConnection;
        services.AddDbContext<EduVerseDb>(o => o.UseSqlite(connection));
        services.AddIdentity<AppUser, IdentityRole>(o =>
            {
                o.User.RequireUniqueEmail = true;
                o.Password.RequiredLength = 8;
                o.Password.RequireNonAlphanumeric = false;
                o.Password.RequireUppercase = false;
                o.Lockout.MaxFailedAccessAttempts = 5;
            })
            .AddEntityFrameworkStores<EduVerseDb>()
            .AddClaimsPrincipalFactory<AppClaimsFactory>()
            .AddDefaultTokenProviders();
        return services;
    }

    /// <summary>
    /// Creates the database (EnsureCreated) and the demo accounts when they do not exist yet.
    /// EnsureCreated cannot update an existing schema: a database of an older version is recreated when
    /// <paramref name="allowReset"/> is true (development and demo), otherwise starting fails with a clear message.
    /// </summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, bool seedDemo, bool allowReset = true, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EduVerseDb>();
        var source = db.Database.GetDbConnection().DataSource;
        if (!string.IsNullOrEmpty(source) && Path.GetDirectoryName(Path.GetFullPath(source)) is { } folder)
        {
            Directory.CreateDirectory(folder);
        }

        var (version, hasTables) = await SchemaAsync(db, ct).ConfigureAwait(false);
        if (hasTables && version < EduVerseDb.DatabaseVersion)
        {
            if (!allowReset)
            {
                throw new InvalidOperationException($"The database schema is version {version}, the application needs version {EduVerseDb.DatabaseVersion}. Back it up and delete it, or add migrations.");
            }

            await db.Database.EnsureDeletedAsync(ct).ConfigureAwait(false);
        }

        await db.Database.EnsureCreatedAsync(ct).ConfigureAwait(false);
        await SetVersionAsync(db, ct).ConfigureAwait(false);
        if (seedDemo)
        {
            await DemoSeeder.SeedAsync(db, scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>(),
                scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>(), scope.ServiceProvider.GetRequiredService<TimeProvider>(), ct).ConfigureAwait(false);
        }
    }

    private static void AddOpenAiCompatible(IServiceCollection services, IConfiguration configuration,
        string section, string clientName, string defaultModel, string defaultBaseUrl, string? apiKeyVariable, bool googleKeyHeader = false)
    {
        services.AddOptions<OpenAiCompatibleOptions>(section)
            .Configure(o =>
            {
                o.Model = defaultModel;
                o.BaseUrl = new Uri(defaultBaseUrl);
            })
            .Bind(configuration.GetSection(section))
            .PostConfigure(o =>
            {
                if (apiKeyVariable is not null)
                {
                    o.ApiKey ??= configuration[apiKeyVariable];
                }
            });

        services.AddHttpClient(clientName, (sp, client) =>
        {
            var options = sp.GetRequiredService<IOptionsMonitor<OpenAiCompatibleOptions>>().Get(section);
            client.BaseAddress = options.BaseUrl;
            client.Timeout = options.Timeout;
            if (!string.IsNullOrWhiteSpace(options.ApiKey) && googleKeyHeader)
            {
                // Native Gemini API: works with both the old "AIza" keys and the new "AQ." keys.
                client.DefaultRequestHeaders.Add("x-goog-api-key", options.ApiKey);
            }
            else if (!string.IsNullOrWhiteSpace(options.ApiKey))
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.ApiKey);
            }
        });
    }

    private static async Task SetVersionAsync(EduVerseDb db, CancellationToken ct)
    {
        // PRAGMA values cannot be parameters; the version is a compile-time constant
        var connection = db.Database.GetDbConnection();
        await connection.OpenAsync(ct).ConfigureAwait(false);
        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA user_version = " + EduVerseDb.DatabaseVersion.ToString(System.Globalization.CultureInfo.InvariantCulture);
            await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            await connection.CloseAsync().ConfigureAwait(false);
        }
    }

    private static async Task<(int Version, bool HasTables)> SchemaAsync(EduVerseDb db, CancellationToken ct)
    {
        var connection = db.Database.GetDbConnection();
        await connection.OpenAsync(ct).ConfigureAwait(false);
        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA user_version";
            var version = Convert.ToInt32(await command.ExecuteScalarAsync(ct).ConfigureAwait(false), System.Globalization.CultureInfo.InvariantCulture);
            command.CommandText = "SELECT count(*) FROM sqlite_master WHERE type = 'table'";
            var tables = Convert.ToInt32(await command.ExecuteScalarAsync(ct).ConfigureAwait(false), System.Globalization.CultureInfo.InvariantCulture);
            return (version, tables > 0);
        }
        finally
        {
            await connection.CloseAsync().ConfigureAwait(false);
        }
    }
}
