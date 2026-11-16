namespace EduVerse.Core.Teaching;

/// <summary>Short codes students type to join a class: 6 characters without look-alikes (no 0/O, 1/I/L).</summary>
public static class JoinCode
{
    public const int Length = 6;

    public const string Alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

    public static string Generate(Random random)
    {
        ArgumentNullException.ThrowIfNull(random);
        return new string(Enumerable.Range(0, Length).Select(_ => Alphabet[random.Next(Alphabet.Length)]).ToArray());
    }

    /// <summary>Tolerant input: lower case, spaces and dashes are accepted ("frc 2b4" → "FRC2B4").</summary>
    public static string Normalize(string? input) =>
        new((input ?? "").ToUpperInvariant().Where(char.IsLetterOrDigit).Take(Length + 4).ToArray());

    public static bool IsValid(string? code) => code is { Length: Length } && code.All(Alphabet.Contains);
}
