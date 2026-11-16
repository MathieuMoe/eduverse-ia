using EduVerse.Core.Catalog;
using EduVerse.Core.Common;
using EduVerse.Core.Tutor;
using EduVerse.Infrastructure.Tutor;
using Microsoft.Extensions.DependencyInjection;

namespace EduVerse.Tests;

public class PipRuleTests
{
    [Fact]
    public void Pip_starts_in_the_second_year_of_primary() =>
        Assert.Equal([false, false, true, true, true, true, true, false, false, false], Enum.GetValues<Level>().Select(Pip.Available));

    [Theory]
    [InlineData(Level.Primary2)]
    [InlineData(Level.Primary3)]
    [InlineData(Level.Secondary1)]
    [InlineData(Level.HighSchool)]
    public void Every_problem_has_a_first_wrong_line_that_differs_and_an_honest_reason(Level level)
    {
        foreach (var language in Enum.GetValues<TeachLanguage>())
        {
            for (var seed = 1; seed <= 300; seed++)
            {
                var p = Pip.Generate(level, seed, language);
                Assert.Equal(p.Shown.Count, p.Right.Count);
                Assert.InRange(p.WrongStep, 0, p.Shown.Count - 1);
                Assert.Equal(p.Right.Take(p.WrongStep), p.Shown.Take(p.WrongStep)); // everything before is right
                Assert.NotEqual(p.Right[p.WrongStep], p.Shown[p.WrongStep]);
                Assert.NotEqual(p.Right[^1].Split('=')[^1].Trim(), p.Shown[^1].Split('=')[^1].Trim()); // the final answer is wrong
                Assert.Equal(Pip.Reasons, p.Reasons.Count);
                Assert.Equal(Pip.Reasons, p.Reasons.Distinct().Count());
                Assert.Equal(Pip.Describe(p.Mistake, language), p.Reasons[p.ReasonIndex]);
                Assert.Contains(p.Mistake, Pip.Kinds(level));
                Assert.DoesNotContain("{", p.Fix, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void The_same_seed_gives_the_same_problem_and_the_right_lines_are_really_right()
    {
        var (one, two) = (Pip.Generate(Level.Primary3, 42, TeachLanguage.English), Pip.Generate(Level.Primary3, 42, TeachLanguage.English));
        Assert.Equal((one.Problem, string.Join("|", one.Shown), one.WrongStep, one.ReasonIndex, one.Fix), (two.Problem, string.Join("|", two.Shown), two.WrongStep, two.ReasonIndex, two.Fix));
        for (var seed = 1; seed <= 200; seed++)
        {
            var p = Pip.Generate(Level.Secondary1, seed, TeachLanguage.English);
            if (p.Mistake == PipMistake.Order)
            {
                var n = p.Problem.Split([' ', '+', '×'], StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToArray();
                Assert.Equal((n[0] + (n[1] * n[2])).ToString(System.Globalization.CultureInfo.InvariantCulture), p.Right[^1].Split('=')[^1].Trim());
            }
        }
    }
}

public class PipLanguageTests
{
    [Fact]
    public void Language_problems_are_right_in_every_language_and_the_wrong_line_is_the_first_to_differ()
    {
        foreach (var language in Enum.GetValues<TeachLanguage>())
        {
            var seen = new HashSet<PipMistake>();
            for (var seed = 1; seed <= 200; seed++)
            {
                var p = Pip.Generate(Level.Primary3, seed, language, PipSubject.Language);
                seen.Add(p.Mistake);
                Assert.Contains(p.Mistake, Pip.Kinds(Level.Primary3, PipSubject.Language));
                Assert.Equal(p.Right.Take(p.WrongStep), p.Shown.Take(p.WrongStep));
                Assert.NotEqual(p.Right[p.WrongStep], p.Shown[p.WrongStep]);
                Assert.NotEqual(p.Right[^1], p.Shown[^1]);
                Assert.Equal(Pip.Reasons, p.Reasons.Distinct().Count());
                Assert.All(p.Reasons, r => Assert.DoesNotContain(r, Pip.Describe(PipMistake.Carry, language))); // only language reasons
                Assert.DoesNotContain("{", p.Fix, StringComparison.Ordinal);
            }

            Assert.Equal(3, seen.Count);
        }
    }

    [Fact]
    public void The_sentences_use_the_forms_of_their_own_language()
    {
        var agreement = Enumerable.Range(1, 200).Select(s => Pip.Generate(Level.Primary3, s, TeachLanguage.French, PipSubject.Language)).First(p => p.Mistake == PipMistake.Agreement);
        Assert.Contains("ent ", agreement.Right[^1], StringComparison.Ordinal); // marchent, chantent…
        var past = Enumerable.Range(1, 200).Select(s => Pip.Generate(Level.Primary3, s, TeachLanguage.Catalan, PipSubject.Language)).First(p => p.Mistake == PipMistake.Past);
        Assert.Contains(" va ", past.Right[^1], StringComparison.Ordinal);
        var capital = Enumerable.Range(1, 200).Select(s => Pip.Generate(Level.Primary3, s, TeachLanguage.English, PipSubject.Language)).First(p => p.Mistake == PipMistake.Capital);
        Assert.True(char.IsUpper(capital.Right[^1].Split('=')[^1].Trim()[0]));
        Assert.True(char.IsLower(capital.Shown[^1].Split('=')[^1].Trim()[0]));
    }
}

public class PipVoiceTests
{
    [Fact]
    public void Pip_answers_in_four_languages_for_every_outcome()
    {
        foreach (var language in Enum.GetValues<TeachLanguage>())
        {
            foreach (var (line, reason) in new[] { (true, true), (true, false), (false, false) })
            {
                var text = PipVoice.Offline(line, reason, PipMistake.Order, language);
                Assert.InRange(text.Length, 20, PipVoice.MaxChars);
                Assert.DoesNotContain("{", text, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void The_prompt_carries_only_the_facts_and_the_reply_is_parsed_strictly()
    {
        var p = Pip.Generate(Level.Primary3, 5, TeachLanguage.English);
        var prompt = PipVoice.Build(p, true, false, Level.Primary3, TeachLanguage.French);
        Assert.Contains(p.Problem, prompt.User, StringComparison.Ordinal);
        Assert.Contains("French", prompt.System, StringComparison.Ordinal);
        Assert.Contains("the student named the reason correctly: no", prompt.User, StringComparison.Ordinal);
        Assert.Equal("Hi!", PipVoice.Parse(System.Text.Json.JsonDocument.Parse("{\"reply\":\"Hi!\"}").RootElement));
        Assert.Throws<EduVerse.Core.Abstractions.AiGenerationException>(() => PipVoice.Parse(System.Text.Json.JsonDocument.Parse("{\"reply\":\"\"}").RootElement));
    }
}

public class PipStoreTests(Database database) : IClassFixture<Database>
{
    [Fact]
    public async Task A_challenge_is_answered_once_by_its_owner_and_rewards_only_a_full_answer()
    {
        using var scope = database.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<PipStore>();
        var omar = await database.UserIdAsync("omar@eduverse.demo");
        Assert.Null(await store.StartAsync(omar, Level.Primary1, TeachLanguage.English));

        var view = (await store.StartAsync(omar, Level.Primary3, TeachLanguage.English))!;
        Assert.Equal(Pip.Reasons, view.Reasons.Count);
        var lang = (await store.StartAsync(omar, Level.Primary3, TeachLanguage.Spanish, PipSubject.Language))!;
        Assert.NotEmpty(lang.Problem);
        Assert.Null(await store.AnswerAsync("someone-else", view.Id, 0, 0));
        var wrong = (await store.AnswerAsync(omar, view.Id, 9, 0))!;
        Assert.False(wrong.LineOk);
        Assert.Equal(0, await store.SolvedAsync(omar));
        Assert.Null(await store.AnswerAsync(omar, view.Id, wrong.WrongStep, wrong.ReasonIndex)); // once

        var second = (await store.StartAsync(omar, Level.Primary3, TeachLanguage.English))!;
        var probe = (await store.AnswerAsync(omar, second.Id, 9, 9))!; // a wrong answer, to learn the right one
        var third = (await store.StartAsync(omar, Level.Primary3, TeachLanguage.English))!;
        Assert.Equal(0, await store.SolvedAsync(omar));
        Assert.NotEqual(second.Id, third.Id);
        Assert.NotEmpty(probe.Fix);

        Assert.Null(await store.FactsAsync(omar, third.Id)); // not answered yet
        Assert.Null(await store.FactsAsync("someone-else", second.Id));
        var facts = (await store.FactsAsync(omar, second.Id))!;
        Assert.Equal(probe.WrongStep, facts.Problem.WrongStep);
        var stats = await store.StatsAsync(omar);
        Assert.True(stats.Sum(x => x.Total) >= 2);
        Assert.All(stats, x => Assert.InRange(x.Found, 0, x.Total));
    }
}
