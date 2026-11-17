namespace EduVerse.Core.Learning;

/// <summary>
/// The genuine confidence detector: distinguishes a string of lucky guesses from real, confirmed knowledge — something
/// raw accuracy and the Bayesian knowledge tracker (<see cref="Bkt"/>) cannot see, because neither one looks at whether
/// the student themself felt sure. "Not sure" (confidence 0) on a right answer is the honest signal of a guess; this is
/// the one place that signal meets the scoreboard.
/// </summary>
public static class Confidence
{
    /// <summary>Correct answers needed on a topic before a guess share means anything (one lucky guess is not a pattern).</summary>
    public const int MinSample = 3;

    /// <summary>Share of "not sure" correct answers, in percent, that counts as mostly guessed rather than known.</summary>
    public const int GuessThreshold = 60;

    /// <summary>
    /// A topic that otherwise looks fine (enough correct answers, and already "known" by knowledge tracing) but where
    /// most of the right answers were "not sure" guesses: the scoreboard says mastered, the student's own honesty says
    /// otherwise.
    /// </summary>
    public static bool LooksGuessed(int correct, int guessed) => correct >= MinSample && 100 * guessed >= GuessThreshold * correct;
}
