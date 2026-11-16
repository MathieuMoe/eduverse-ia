using EduVerse.Core.Common;
using EduVerse.Core.Learning;

namespace EduVerse.Core.Catalog;

/// <summary>
/// "Check Evo": the mascot answers and the student decides if it is right. When Evo is wrong, his answer is a typical
/// mistake (a distractor of the bank) and the student must pick the right answer. Evo is right 1 time in 3, so students
/// have to think instead of always saying "wrong".
/// </summary>
public static class EvoQuestions
{
    public static readonly Text EvoIsRight = Text.Of("✅ Evo is right|✅ Evo tiene razón|✅ Evo a raison|✅ L'Evo té raó");

    public static QuizQuestion Check(QuizQuestion question, Random rng, TeachLanguage language)
    {
        ArgumentNullException.ThrowIfNull(question);
        ArgumentNullException.ThrowIfNull(rng);
        var right = rng.Next(3) == 0;
        var evoIndex = right ? question.CorrectIndex : Enumerable.Range(0, question.Options.Count).Where(i => i != question.CorrectIndex).OrderBy(_ => rng.Next()).First();
        var evo = question.Options[evoIndex];
        var options = new List<string> { EvoIsRight.In(language) };
        options.AddRange(question.Options.Where((_, i) => i != evoIndex));
        options = options.Take(QuizQuestion.Choices).ToList();
        var correct = right ? options[0] : question.Answer;
        var note = right
            ? Text.Of("Evo had it right.|Evo acertó.|Evo avait raison.|L'Evo l'havia encertat.").In(language)
            : Text.Of("Evo answered {0}: a typical mistake.|Evo respondió {0}: un error típico.|Evo a répondu {0} : une erreur classique.|L'Evo va respondre {0}: un error típic.").Format(language, evo);
        return question with
        {
            Options = options,
            CorrectIndex = options.IndexOf(correct),
            Explanation = $"{note} {question.Explanation}",
            Evo = evo,
            EvoTask = Text.Of("Is Evo right? If not, choose the right answer.|¿Tiene razón Evo? Si no, elige la respuesta correcta.|Evo a-t-il raison ? Sinon, choisis la bonne réponse.|L'Evo té raó? Si no, tria la resposta correcta.").In(language),
        };
    }
}

/// <summary>
/// "Evo's notebook": Evo solves fraction exercises making the student's OWN typical mistakes (from the tutor history)
/// and the student has to say what he did wrong. Spotting a mistake in someone else's work is the best way to stop making it.
/// </summary>
public static class EvoNotebook
{
    private static readonly string[] Concepts = ["meaning", "equivalent", "simplify", "compare", "add-same", "add-different", "multiply"];

    private static readonly Dictionary<string, Text> Mistakes = new()
    {
        [FractionExercises.Inverted] = Text.Of("He put the number of parts of the whole on top.|Puso arriba el número de partes del todo.|Il a mis en haut le nombre de parts du tout.|Va posar a dalt el nombre de parts del tot."),
        [FractionExercises.CountsTheRest] = Text.Of("He counted the parts that are NOT coloured.|Contó las partes que NO están coloreadas.|Il a compté les parts qui ne sont PAS coloriées.|Va comptar les parts que NO estan pintades."),
        [FractionExercises.PartToPart] = Text.Of("He compared the part with the rest instead of the whole.|Comparó la parte con el resto en lugar de con el todo.|Il a comparé la part avec le reste au lieu du tout.|Va comparar la part amb la resta en lloc del tot."),
        [FractionExercises.AddSameNumber] = Text.Of("He added the same number to the top and the bottom.|Sumó el mismo número arriba y abajo.|Il a ajouté le même nombre en haut et en bas.|Va sumar el mateix nombre a dalt i a baix."),
        [FractionExercises.OnlyNumerator] = Text.Of("He changed only one of the two numbers.|Cambió solo uno de los dos números.|Il n'a changé qu'un seul des deux nombres.|Va canviar només un dels dos nombres."),
        [FractionExercises.NotFullySimplified] = Text.Of("He did not simplify to the end.|No simplificó hasta el final.|Il n'a pas simplifié jusqu'au bout.|No va simplificar fins al final."),
        [FractionExercises.BiggerDenominatorBigger] = Text.Of("He thought a bigger denominator means a bigger fraction.|Pensó que un denominador mayor da una fracción mayor.|Il a cru qu'un plus grand dénominateur donne une plus grande fraction.|Va pensar que un denominador més gran dona una fracció més gran."),
        [FractionExercises.AddDenominators] = Text.Of("He added the denominators.|Sumó los denominadores.|Il a additionné les dénominateurs.|Va sumar els denominadors."),
        [FractionExercises.AddAcross] = Text.Of("He added top with top and bottom with bottom.|Sumó numeradores entre sí y denominadores entre sí.|Il a additionné les numérateurs entre eux et les dénominateurs entre eux.|Va sumar numeradors entre si i denominadors entre si."),
        [FractionExercises.CrossMultiply] = Text.Of("He multiplied crosswise.|Multiplicó en cruz.|Il a multiplié en croix.|Va multiplicar en creu."),
        [FractionExercises.SameNumeratorEqual] = Text.Of("He thought the same numerator means the same size.|Pensó que el mismo numerador significa el mismo tamaño.|Il a cru que le même numérateur veut dire la même taille.|Va pensar que el mateix numerador vol dir la mateixa mida."),
        [FractionExercises.MultipliedInsteadOfAdding] = Text.Of("He multiplied instead of adding.|Multiplicó en lugar de sumar.|Il a multiplié au lieu d'additionner.|Va multiplicar en lloc de sumar."),
        [FractionExercises.NotConverted] = Text.Of("He changed the denominators but not the numerators.|Cambió los denominadores pero no los numeradores.|Il a changé les dénominateurs mais pas les numérateurs.|Va canviar els denominadors però no els numeradors."),
    };

    public static readonly Text EvoGotItRight = Text.Of("Nothing: Evo got it right!|Nada: ¡Evo acertó!|Rien : Evo a juste !|Res: l'Evo ho ha encertat!");

    public static IReadOnlyCollection<string> Codes => Mistakes.Keys;

    public static string Describe(string code, TeachLanguage language) => Mistakes.TryGetValue(code, out var t) ? t.In(language) : Misconceptions.Humanize(code);

    /// <summary>
    /// A worked exercise with Evo's answer. <paramref name="mistakes"/> are the student's misconception codes (most frequent first):
    /// Evo makes them 2 times in 3; otherwise another typical mistake, and 1 time in 5 Evo is right.
    /// </summary>
    public static QuizQuestion Create(IReadOnlyList<string> mistakes, Random rng, TeachLanguage language)
    {
        ArgumentNullException.ThrowIfNull(mistakes);
        ArgumentNullException.ThrowIfNull(rng);
        var known = mistakes.Where(Mistakes.ContainsKey).ToList();
        string? target = rng.Next(5) == 0 ? null : known.Count > 0 && rng.Next(3) < 2 ? known[Math.Min(rng.Next(known.Count), rng.Next(known.Count))] : Mistakes.Keys.ElementAt(rng.Next(Mistakes.Count));

        for (var attempt = 0; attempt < 200; attempt++)
        {
            var exercise = FractionExercises.Create(Concepts[rng.Next(Concepts.Length)], rng.Next(1, 4), rng, language);
            var evoIndex = target is null ? exercise.CorrectIndex : Enumerable.Range(0, exercise.Options.Count).FirstOrDefault(i => exercise.Misconceptions[i] == target, -1);
            if (evoIndex < 0)
            {
                continue;
            }

            var answer = target is null ? EvoGotItRight.In(language) : Describe(target, language);
            var others = exercise.Misconceptions.Where(c => c is not null && c != target).Select(c => Describe(c!, language))
                .Concat(Mistakes.Keys.Where(c => c != target).OrderBy(_ => rng.Next()).Select(c => Describe(c, language)));
            if (target is not null)
            {
                others = others.Prepend(EvoGotItRight.In(language));
            }

            var options = new List<string> { answer };
            foreach (var o in others)
            {
                if (options.Count < QuizQuestion.Choices && !options.Contains(o))
                {
                    options.Add(o);
                }
            }

            options = options.OrderBy(_ => rng.Next()).ToList();
            var right = exercise.Options[exercise.CorrectIndex];
            var explanation = target is null
                ? Text.Of("Right: {0}. {1}|Correcto: {0}. {1}|Juste : {0}. {1}|Correcte: {0}. {1}").Format(language, right, exercise.Explanation)
                : Text.Of("The right answer is {0}. {1} Remember: {2}|La respuesta correcta es {0}. {1} Recuerda: {2}|La bonne réponse est {0}. {1} Retiens : {2}|La resposta correcta és {0}. {1} Recorda: {2}")
                    .Format(language, right, exercise.Explanation, DemoLearningAi.Warning(target, language));
            return new QuizQuestion("math.fraction", exercise.Difficulty, exercise.Question, options, options.IndexOf(answer), explanation,
                Evo: exercise.Options[evoIndex],
                EvoTask: Text.Of("What did Evo do wrong?|¿Qué ha hecho mal Evo?|Qu'est-ce qu'Evo a fait de travers ?|Què ha fet malament l'Evo?").In(language));
        }

        throw new InvalidOperationException("No exercise for the mistake " + target);
    }
}

/// <summary>Spaced repetition per topic (Leitner boxes): a topic comes back after 1, 2, 4, 7, 15 then 30 days if it keeps being right.</summary>
public static class Spaced
{
    public static readonly int[] Days = [1, 2, 4, 7, 15, 30];
    public const int ReviewLength = 6;

    /// <summary>Confidence given before answering: 0 = not sure, 1 = I think so, 2 = sure.</summary>
    public const int Sure = 2;

    /// <summary>Right: next box. Wrong: back to box 0 for tomorrow — or for today if the student was sure (a false certainty must be fixed now).</summary>
    public static (int Box, DateTimeOffset Due) Next(int box, bool correct, int confidence, DateTimeOffset now)
    {
        if (!correct)
        {
            return (0, confidence == Sure ? now : now.AddDays(1));
        }

        var next = Math.Clamp(box + 1, 1, Days.Length);
        return (next, now.AddDays(Days[next - 1]));
    }

    /// <summary>Percentage of right answers among the ones given with "sure" (null if not enough data).</summary>
    public static int? Calibration(int sure, int sureRight) => sure < 5 ? null : 100 * sureRight / sure;
}
