using System.Text.Json;
using EduVerse.Core.Common;
using EduVerse.Core.Gamification;
using EduVerse.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EduVerse.Infrastructure.Gamification;

public enum EraState
{
    Locked,
    Open,
    Done,
}

public sealed record CountryView(CountryHistory Country, IReadOnlyList<EraState> Eras)
{
    /// <summary>The timeline challenge is won: the stamp is gold.</summary>
    public bool Gold { get; init; }

    public int Done => Eras.Count(e => e == EraState.Done);

    public bool Stamped => Done == Eras.Count;
}

public sealed record CheckQuestion(string Prompt, IReadOnlyList<string> Options);

public sealed record CheckView(string Id, IReadOnlyList<CheckQuestion> Questions);

public sealed record OrderResult(int Right, int Total, int Gems)
{
    public bool Won => Right == Total;
}

public sealed record CheckResult(int Score, bool Passed, IReadOnlyList<HistoryQuestion> Questions, bool Stamp, Reward Reward);

/// <summary>
/// Trips through the history of countries: the timeline is walked in order (an era opens when the one before is
/// passed), each era ends with a check written by <see cref="CountryQuiz"/>. The first pass of an era gives XP and
/// gems, and passing every era stamps the country in the student's passport.
/// </summary>
public sealed class CountryStore(EduVerseDb db, GamificationStore game)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>How many AI timelines a student keeps (the oldest ones are not listed any more).</summary>
    public const int MaxCustom = 12;

    public const string CustomPrefix = "ai-";

    public async Task<CountryHistory?> FindAsync(string userId, string? key, CancellationToken ct = default)
    {
        if (Countries.Find(key) is { } country)
        {
            return country;
        }

        if (key is null || !key.StartsWith(CustomPrefix, StringComparison.Ordinal))
        {
            return null;
        }

        var custom = await db.CustomCountries.AsNoTracking().FirstOrDefaultAsync(c => c.Key == key && c.UserId == userId, ct).ConfigureAwait(false);
        return custom is null ? null : JsonSerializer.Deserialize<CountryHistory>(custom.Json, JsonOptions);
    }

    public async Task<IReadOnlyList<CountryHistory>> CustomAsync(string userId, CancellationToken ct = default) =>
        (await db.CustomCountries.AsNoTracking().Where(c => c.UserId == userId).OrderByDescending(c => c.CreatedAt).Take(MaxCustom)
            .Select(c => c.Json).ToListAsync(ct).ConfigureAwait(false))
        .Select(j => JsonSerializer.Deserialize<CountryHistory>(j, JsonOptions)!).ToList();

    public async Task<string> SaveCustomAsync(string userId, Func<string, CountryHistory> write, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(write);
        var key = CustomPrefix + Guid.NewGuid().ToString("N")[..12];
        var country = write(key);
        db.CustomCountries.Add(new CustomCountry { Key = key, UserId = userId, Json = JsonSerializer.Serialize(country, JsonOptions), CreatedAt = game.Now });
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return key;
    }

    /// <summary>Eras passed per country (for the passport).</summary>
    public async Task<IReadOnlyDictionary<string, int>> ProgressAsync(string userId, CancellationToken ct = default) =>
        (await db.CountryChecks.AsNoTracking().Where(c => c.UserId == userId && c.Passed && c.Era != CountryOrder.Key).Select(c => new { c.Country, c.Era }).Distinct().ToListAsync(ct).ConfigureAwait(false))
        .GroupBy(c => c.Country).ToDictionary(g => g.Key, g => g.Count());

    public async Task<CountryView> ViewAsync(string userId, CountryHistory country, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(country);
        var passed = await PassedAsync(userId, country.Key, ct).ConfigureAwait(false);
        var states = new List<EraState>();
        var open = false;
        foreach (var era in country.Eras)
        {
            var state = passed.Contains(era.Key) && !open ? EraState.Done : open ? EraState.Locked : EraState.Open;
            open |= state == EraState.Open;
            states.Add(state);
        }

        var gold = await db.CountryChecks.AnyAsync(c => c.UserId == userId && c.Country == country.Key && c.Era == CountryOrder.Key && c.Passed, ct).ConfigureAwait(false);
        return new CountryView(country, states) { Gold = gold };
    }

    private async Task<HashSet<string>> PassedAsync(string userId, string country, CancellationToken ct) =>
        (await db.CountryChecks.Where(c => c.UserId == userId && c.Country == country && c.Passed && c.Era != CountryOrder.Key).Select(c => c.Era).ToListAsync(ct).ConfigureAwait(false)).ToHashSet();

    /// <summary>A new check of an era that is open or done (null when the era is locked or does not exist).</summary>
    public async Task<CheckView?> StartCheckAsync(string userId, string key, int era, TeachLanguage language, CancellationToken ct = default)
    {
        var country = await FindAsync(userId, key, ct).ConfigureAwait(false);
        if (country is null || era < 0 || era >= country.Eras.Count)
        {
            return null;
        }

        var view = await ViewAsync(userId, country, ct).ConfigureAwait(false);
        if (view.Eras[era] == EraState.Locked)
        {
            return null;
        }

        var questions = CountryQuiz.Check(country, era, Random.Shared.Next(), language);
        var check = new CountryCheck
        {
            Id = Guid.NewGuid().ToString("N"), UserId = userId, Country = country.Key, Era = country.Eras[era].Key,
            QuestionsJson = JsonSerializer.Serialize(questions, JsonOptions), StartedAt = game.Now,
        };
        db.CountryChecks.Add(check);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return new CheckView(check.Id, questions.Select(q => new CheckQuestion(q.Prompt, q.Options)).ToList());
    }

    /// <summary>Marks the answers (once). The first pass of an era gives XP and gems; the last era stamps the country.</summary>
    public async Task<CheckResult?> AnswerCheckAsync(string userId, string id, IReadOnlyList<int> answers, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(answers);
        var check = await db.CountryChecks.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId, ct).ConfigureAwait(false);
        if (check is null || check.FinishedAt is not null)
        {
            return null;
        }

        var questions = JsonSerializer.Deserialize<List<HistoryQuestion>>(check.QuestionsJson, JsonOptions) ?? [];
        var already = await PassedAsync(userId, check.Country, ct).ConfigureAwait(false);
        check.Correct = CountryQuiz.Score(questions, answers);
        check.Passed = check.Correct >= Countries.PassMark;
        check.FinishedAt = game.Now;
        var reward = Reward.None;
        var stamp = false;
        if (check.Passed && !already.Contains(check.Era))
        {
            await game.AwardAsync(userId, Countries.EraXp, "history", ct: ct).ConfigureAwait(false);
            db.GemEvents.Add(new GemEvent { UserId = userId, Amount = Countries.EraGems, Reason = $"history:{check.Country}:{check.Era}", At = game.Now });
            var gems = Countries.EraGems;
            var country = await FindAsync(userId, check.Country, ct).ConfigureAwait(false);
            if (country is not null && country.Eras.All(e => e.Key == check.Era || already.Contains(e.Key)))
            {
                stamp = true;
                db.GemEvents.Add(new GemEvent { UserId = userId, Amount = Countries.StampGems, Reason = "stamp:" + check.Country, At = game.Now });
                gems += Countries.StampGems;
            }

            reward = new Reward(Countries.EraXp, [], []) { Gems = gems };
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        if (check.Passed)
        {
            reward = reward.Plus(await game.CheckAsync(userId, ct).ConfigureAwait(false));
        }

        return new CheckResult(check.Correct, check.Passed, questions, stamp, reward);
    }

    /// <summary>Countries whose timeline challenge is won (gold stamps).</summary>
    public async Task<IReadOnlySet<string>> GoldAsync(string userId, CancellationToken ct = default) =>
        (await db.CountryChecks.Where(c => c.UserId == userId && c.Era == CountryOrder.Key && c.Passed).Select(c => c.Country).Distinct().ToListAsync(ct).ConfigureAwait(false)).ToHashSet();

    /// <summary>
    /// The timeline challenge of a stamped country (null while the country is not stamped). The first win pays gems
    /// and turns the stamp gold; it can be replayed.
    /// </summary>
    public async Task<OrderResult?> OrderAsync(string userId, string key, IReadOnlyList<string> keys, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(keys);
        var country = await FindAsync(userId, key, ct).ConfigureAwait(false);
        if (country is null)
        {
            return null;
        }

        var view = await ViewAsync(userId, country, ct).ConfigureAwait(false);
        if (!view.Stamped)
        {
            return null;
        }

        var right = CountryOrder.RightPlaces(country, keys);
        var won = right == country.Eras.Count;
        var gems = won && !view.Gold ? CountryOrder.GoldGems : 0;
        db.CountryChecks.Add(new CountryCheck
        {
            Id = Guid.NewGuid().ToString("N"), UserId = userId, Country = country.Key, Era = CountryOrder.Key, Correct = right, Passed = won,
            StartedAt = game.Now, FinishedAt = game.Now,
        });
        if (gems > 0)
        {
            db.GemEvents.Add(new GemEvent { UserId = userId, Amount = gems, Reason = "gold:" + country.Key, At = game.Now });
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return new OrderResult(right, country.Eras.Count, gems);
    }

    /// <summary>The built-in country the student has started and not finished (the most advanced), for the dashboard.</summary>
    public async Task<(CountryHistory Country, int Passed)?> TripInProgressAsync(string userId, CancellationToken ct = default)
    {
        var progress = await ProgressAsync(userId, ct).ConfigureAwait(false);
        return Countries.All.Select(c => (Country: c, Passed: progress.TryGetValue(c.Key, out var n) ? n : 0))
            .Where(x => x.Passed > 0 && x.Passed < x.Country.Eras.Count).OrderByDescending(x => x.Passed)
            .Select(x => ((CountryHistory, int)?)x).FirstOrDefault();
    }
}
