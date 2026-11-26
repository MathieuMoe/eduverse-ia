using EduVerse.Core.Catalog;
using EduVerse.Core.Common;
using EduVerse.Core.Tutor;
using EduVerse.Infrastructure.Data;
using EduVerse.Infrastructure.Tutor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EduVerse.Tests;

public class TeachFamilyTests
{
    private static MilestoneFacts Facts(int checks = 2) => new("Sara", "Fractions", "Mathematics",
        "A fraction splits a whole into equal parts.",
        Enumerable.Range(1, checks).Select(i => new CheckQuestion($"Question {i}?", $"Answer {i}")).ToList());

    [Fact]
    public void Needs_at_least_two_check_questions()
    {
        Assert.False(TeachFamily.HasContent(Facts(checks: 1)));
        Assert.True(TeachFamily.HasContent(Facts(checks: 2)));
    }

    [Fact]
    public void The_script_carries_the_measured_facts_in_four_languages()
    {
        foreach (var language in Enum.GetValues<TeachLanguage>())
        {
            var script = TeachFamily.Build(Facts(), language);
            Assert.Contains("Sara", script.Title + script.Intro, StringComparison.Ordinal);
            Assert.Contains("Fractions", script.Title + script.Intro, StringComparison.Ordinal);
            Assert.Equal("A fraction splits a whole into equal parts.", script.Explain);
            Assert.Equal(2, script.Checks.Count);
            Assert.DoesNotContain("{", script.Title + script.Intro + script.Closing, StringComparison.Ordinal);
        }
    }
}

public class FamilyStoreTests(Database database) : IClassFixture<Database>
{
    [Fact]
    public async Task No_milestone_when_nothing_is_freshly_mastered()
    {
        using var scope = database.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<FamilyStore>();
        Assert.Null(await store.RecentMilestoneAsync("nobody"));
    }

    [Fact]
    public async Task Finds_a_topic_just_past_the_mastery_threshold_but_not_an_old_one()
    {
        using var scope = database.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EduVerseDb>();
        var store = scope.ServiceProvider.GetRequiredService<FamilyStore>();
        var userId = await database.UserIdAsync("sara@eduverse.demo");

        async Task UpsertAsync(Area area, Level level, string topic, double known, int attempts)
        {
            var skill = await db.TopicSkills.FirstOrDefaultAsync(s => s.UserId == userId && s.Area == area && s.Level == level && s.Topic == topic);
            if (skill is null)
            {
                db.TopicSkills.Add(new TopicSkill { UserId = userId, Area = area, Level = level, Topic = topic, PKnown = known, Attempts = attempts, Correct = attempts });
            }
            else
            {
                skill.PKnown = known;
                skill.Attempts = attempts;
                skill.Correct = attempts;
            }
        }

        await UpsertAsync(Area.Mathematics, Level.Primary3, "math.fraction", 0.9, 4);
        await UpsertAsync(Area.Physics, Level.Secondary1, "physics.density", 0.97, 40);
        await db.SaveChangesAsync();

        // Isolate from whatever else the demo seed already gives this student.
        var competing = await db.TopicSkills.Where(s => s.UserId == userId && s.Topic != "math.fraction"
            && s.Attempts >= 2 && s.Attempts <= FamilyStore.MaxAttemptsSinceMastery && s.PKnown >= CrossSubjectBridges.KnownThreshold).ToListAsync();
        db.TopicSkills.RemoveRange(competing);
        await db.SaveChangesAsync();

        var milestone = await store.RecentMilestoneAsync(userId);
        Assert.NotNull(milestone);
        Assert.Equal("math.fraction", milestone!.Topic);
    }
}
