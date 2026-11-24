using EduVerse.Core.Common;

namespace EduVerse.Core.Catalog;

/// <summary>
/// Demo content for the Duolingo-style exercise kinds (<see cref="ExerciseKind"/>) that are mixed into Practice-mode
/// quizzes alongside classic multiple-choice questions. Starts small (a handful of true/false items for Mathematics)
/// to prove the mechanism end to end; the other subjects and the other three kinds (fill-the-blank, reorder, match)
/// are filled in incrementally in later passes.
/// </summary>
public static class ExerciseBank
{
    private sealed record TrueFalseItem(string Topic, int Difficulty, Text Prompt, bool Answer, Text Explanation);

    private static readonly IReadOnlyList<TrueFalseItem> MathTrueFalse =
    [
        new("math.compare", 1,
            Text.Of("7 is bigger than 4.|7 es más grande que 4.|7 est plus grand que 4.|7 és més gran que 4."),
            true,
            Text.Of("Yes: 7 comes after 4, so it's bigger.|Sí: 7 va después de 4, así que es más grande.|Oui : 7 vient après 4, donc c'est plus grand.|Sí: 7 va després de 4, així que és més gran.")),
        new("math.add", 1,
            Text.Of("3 + 4 = 8.|3 + 4 = 8.|3 + 4 = 8.|3 + 4 = 8."),
            false,
            Text.Of("No: 3 + 4 = 7, not 8.|No: 3 + 4 = 7, no 8.|Non : 3 + 4 = 7, pas 8.|No: 3 + 4 = 7, no 8.")),
        new("math.percent", 2,
            Text.Of("50% of 20 is 10.|El 50% de 20 es 10.|50 % de 20, c'est 10.|El 50% de 20 és 10."),
            true,
            Text.Of("Yes: 50% is half, and half of 20 is 10.|Sí: el 50% es la mitad, y la mitad de 20 es 10.|Oui : 50 % c'est la moitié, et la moitié de 20 c'est 10.|Sí: el 50% és la meitat, i la meitat de 20 és 10.")),
        new("math.integers", 2,
            Text.Of("-3 is bigger than -1.|-3 es más grande que -1.|-3 est plus grand que -1.|-3 és més gran que -1."),
            false,
            Text.Of("No: on the number line, -1 is to the right of -3, so -1 is bigger.|No: en la recta numérica, -1 está a la derecha de -3, así que -1 es más grande.|Non : sur la droite numérique, -1 est à droite de -3, donc -1 est plus grand.|No: a la recta numèrica, -1 està a la dreta de -3, així que -1 és més gran.")),
        new("math.pythagoras", 3,
            Text.Of("In a right triangle, the square of the hypotenuse equals the sum of the squares of the other two sides.|En un triángulo rectángulo, el cuadrado de la hipotenusa es igual a la suma de los cuadrados de los otros dos lados.|Dans un triangle rectangle, le carré de l'hypoténuse est égal à la somme des carrés des deux autres côtés.|En un triangle rectangle, el quadrat de la hipotenusa és igual a la suma dels quadrats dels altres dos costats."),
            true,
            Text.Of("Yes: that's the Pythagorean theorem, a² + b² = c².|Sí: es el teorema de Pitágoras, a² + b² = c².|Oui : c'est le théorème de Pythagore, a² + b² = c².|Sí: és el teorema de Pitàgores, a² + b² = c².")),
    ];

    /// <summary>A true/false drill for the given subject and level, or null if the bank has none yet.</summary>
    public static PracticeExercise? TrueFalse(Area area, Level level, TeachLanguage language, int seed)
    {
        if (area != Area.Mathematics)
        {
            return null;
        }

        var difficulty = level switch
        {
            Level.EarlyYears or Level.Primary1 or Level.Primary2 => 1,
            Level.Primary3 or Level.Secondary1 => 2,
            _ => 3,
        };

        var pool = MathTrueFalse.Where(i => i.Difficulty == difficulty).ToList();
        if (pool.Count == 0)
        {
            return null;
        }

        var item = pool[new Random(seed).Next(pool.Count)];
        return new PracticeExercise(item.Topic, item.Difficulty, ExerciseKind.TrueFalse, item.Prompt.In(language), item.Explanation.In(language))
        {
            CorrectBool = item.Answer,
        };
    }
}
