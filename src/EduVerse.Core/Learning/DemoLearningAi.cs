using System.Globalization;
using EduVerse.Core.Common;
using EduVerse.Core.Curriculum;

namespace EduVerse.Core.Learning;

/// <summary>
/// Offline tutor: full support for the built-in fractions subject (exercises generated in code, explanations from templates
/// in the 5 methods); subjects generated from a free topic need an AI engine.
/// </summary>
public sealed class DemoLearningAi : ILearningAi
{
    public Task<Explanation> ExplainAsync(Subject subject, Concept concept, ExplanationMethod method, StudentContext student, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(concept);
        ArgumentNullException.ThrowIfNull(student);
        var l = student.Language;
        string T(string en, string es, string fr, string ca) => Lang.T(l, en, es, fr, ca);
        var random = new Random(HashCode.Combine(concept.Key, method));
        Exercise? sample = subject.Key == BuiltInSubjects.FractionsKey ? FractionExercises.Create(concept.Key, 1, random, l) : null;
        Exercise? check = subject.Key == BuiltInSubjects.FractionsKey ? FractionExercises.Create(concept.Key, 1, random, l) : null;

        var intro = method switch
        {
            ExplanationMethod.Visual => T("Picture it. Draw a bar and cut it into equal parts: each part is one piece of the whole.", "Imagínalo. Dibuja una barra y córtala en partes iguales: cada parte es un trozo del todo.", "Imagine-le. Dessine une barre et coupe-la en parts égales : chaque part est un morceau du tout.", "Imagina-ho. Dibuixa una barra i talla-la en parts iguals: cada part és un tros del tot.") + "\n■■■□□",
            ExplanationMethod.RealLife => T("Think of a pizza shared with friends: how many slices, how many you eat.", "Piensa en una pizza que compartes con amigos: cuántos trozos hay y cuántos te comes.", "Pense à une pizza partagée entre amis : combien de parts, combien tu en manges.", "Pensa en una pizza que comparteixes amb amics: quants trossos hi ha i quants te'n menges."),
            ExplanationMethod.WorkedExample => T("Let's solve one together, step by step, and see why each step works.", "Vamos a resolver uno juntos, paso a paso, y ver por qué funciona cada paso.", "Résolvons-en un ensemble, étape par étape, et voyons pourquoi chaque étape marche.", "Resolem-ne un junts, pas a pas, i vegem per què funciona cada pas."),
            ExplanationMethod.Analogy => T("It's like money: 1/2 of a euro is 50 cents, 2/4 of a euro is also 50 cents.", "Es como el dinero: 1/2 de euro son 50 céntimos, y 2/4 de euro también son 50 céntimos.", "C'est comme l'argent : 1/2 euro, c'est 50 centimes, et 2/4 d'euro aussi.", "És com els diners: 1/2 d'euro són 50 cèntims, i 2/4 d'euro també."),
            _ => T("Let's go step by step.", "Vamos paso a paso.", "Allons-y étape par étape.", "Anem pas a pas."),
        };

        var steps = method == ExplanationMethod.StepByStep
            ? string.Join("\n", concept.Description.Split([". ", ": ", ", "], StringSplitOptions.RemoveEmptyEntries).Select((x, i) => $"{i + 1}. {x.TrimEnd('.')}."))
            : concept.Description;
        var misconceptions = student.RecentMisconceptions.Count == 0 ? "" : "\n\n" + T("Watch out: ", "Cuidado: ", "Attention : ", "Compte: ")
            + string.Join(" ", student.RecentMisconceptions.Take(2).Select(m => Warning(m, l)).Select(w => char.ToUpper(w[0], System.Globalization.CultureInfo.InvariantCulture) + w[1..]));
        var body = intro + "\n\n" + steps + misconceptions;
        var example = sample is null ? concept.Description : sample.Question + "\n→ " + sample.Options[sample.CorrectIndex] + ". " + sample.Explanation;
        return Task.FromResult(new Explanation(concept.Key, method, concept.Name, body, example,
            check?.Question ?? concept.Name + "?", check is null ? "" : check.Options[check.CorrectIndex]));
    }

    public Task<IReadOnlyList<Exercise>> ExercisesAsync(Subject subject, Concept concept, int difficulty, int count, StudentContext student, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(concept);
        ArgumentNullException.ThrowIfNull(student);
        if (subject.Key != BuiltInSubjects.FractionsKey)
        {
            throw new NeedsAiException();
        }

        var random = new Random(HashCode.Combine(concept.Key, difficulty, DateTime.UtcNow.Ticks));
        IReadOnlyList<Exercise> list = Enumerable.Range(0, count).Select(_ => FractionExercises.Create(concept.Key, difficulty, random, student.Language)).ToList();
        return Task.FromResult(list);
    }

    public Task<Subject> ConceptMapAsync(string topic, Stage stage, TeachLanguage language, CancellationToken cancellationToken = default) => throw new NeedsAiException();

    public Task<TutorReply> ChatAsync(Subject subject, Concept concept, IReadOnlyList<ChatTurn> history, StudentContext student, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(concept);
        ArgumentNullException.ThrowIfNull(history);
        ArgumentNullException.ThrowIfNull(student);
        var l = student.Language;
        string T(string en, string es, string fr, string ca) => Lang.T(l, en, es, fr, ca);
        var last = history.LastOrDefault(h => h.FromStudent)?.Text ?? "";
        var hasNumbers = last.Any(char.IsDigit);
        var reply = hasNumbers
            ? T("Good, you are trying. Check it with the rule: ", "Bien, lo estás intentando. Compruébalo con la regla: ", "Bien, tu essaies. Vérifie avec la règle : ", "Bé, ho estàs intentant. Comprova-ho amb la regla: ") + concept.Description
            : T("Let's think together. Remember: ", "Pensemos juntos. Recuerda: ", "Réfléchissons ensemble. Rappelle-toi : ", "Pensem junts. Recorda: ") + concept.Description;
        var question = hasNumbers
            ? T("Which step did you do first, and why?", "¿Qué paso hiciste primero y por qué?", "Quelle étape as-tu faite en premier, et pourquoi ?", "Quin pas has fet primer i per què?")
            : T("What would be your first step?", "¿Cuál sería tu primer paso?", "Quelle serait ta première étape ?", "Quin seria el teu primer pas?");
        return Task.FromResult(new TutorReply(reply, question));
    }

    /// <summary>One sentence about a misconception of the built-in subject.</summary>
    public static string Warning(string code, TeachLanguage l)
    {
        string T(string en, string es, string fr, string ca) => Lang.T(l, en, es, fr, ca);
        return code switch
        {
            FractionExercises.Inverted => T("the number of parts of the whole goes below.", "el número de partes del todo va abajo.", "le nombre de parts du tout va en bas.", "el nombre de parts del tot va a baix."),
            FractionExercises.CountsTheRest => T("count the parts that are taken, not the others.", "cuenta las partes que se toman, no las otras.", "compte les parts prises, pas les autres.", "compta les parts que es prenen, no les altres."),
            FractionExercises.PartToPart => T("compare the part with the WHOLE, not with the rest.", "compara la parte con el TODO, no con el resto.", "compare la part avec le TOUT, pas avec le reste.", "compara la part amb el TOT, no amb la resta."),
            FractionExercises.AddSameNumber => T("adding the same number to top and bottom changes the fraction; multiplying does not.", "sumar el mismo número arriba y abajo cambia la fracción; multiplicar no.", "ajouter le même nombre en haut et en bas change la fraction ; multiplier non.", "sumar el mateix nombre a dalt i a baix canvia la fracció; multiplicar no."),
            FractionExercises.OnlyNumerator => T("whatever you do to the top, do it to the bottom too.", "lo que hagas arriba, hazlo también abajo.", "ce que tu fais en haut, fais-le aussi en bas.", "el que facis a dalt, fes-ho també a baix."),
            FractionExercises.NotFullySimplified => T("keep dividing until no number divides both.", "sigue dividiendo hasta que ningún número divida a los dos.", "continue à diviser jusqu'à ce qu'aucun nombre ne divise les deux.", "continua dividint fins que cap nombre no divideixi tots dos."),
            FractionExercises.BiggerDenominatorBigger => T("more parts means SMALLER parts.", "más partes significa partes MÁS PEQUEÑAS.", "plus de parts veut dire des parts PLUS PETITES.", "més parts vol dir parts MÉS PETITES."),
            FractionExercises.SameNumeratorEqual => T("the same numerator does not mean the same size: the denominator says how big each part is.", "el mismo numerador no significa el mismo tamaño: el denominador indica lo grande que es cada parte.", "le même numérateur ne veut pas dire la même taille : le dénominateur dit la taille de chaque part.", "el mateix numerador no vol dir la mateixa mida: el denominador indica com és de gran cada part."),
            FractionExercises.AddDenominators or FractionExercises.AddAcross => T("never add the denominators: they say the size of the parts.", "nunca se suman los denominadores: indican el tamaño de las partes.", "on n'additionne jamais les dénominateurs : ils donnent la taille des parts.", "mai no se sumen els denominadors: indiquen la mida de les parts."),
            FractionExercises.MultipliedInsteadOfAdding => T("to add fractions, do not multiply them: write them with the same denominator and add the numerators.", "para sumar fracciones no se multiplican: se escriben con el mismo denominador y se suman los numeradores.", "pour additionner des fractions, on ne les multiplie pas : on les écrit avec le même dénominateur et on additionne les numérateurs.", "per sumar fraccions no es multipliquen: s'escriuen amb el mateix denominador i se sumen els numeradors."),
            FractionExercises.NotConverted => T("after choosing the common denominator, change each numerator too (multiply it by the same number as its denominator).", "después de elegir el denominador común, cambia también cada numerador (multiplícalo por el mismo número que su denominador).", "après avoir choisi le dénominateur commun, change aussi chaque numérateur (multiplie-le par le même nombre que son dénominateur).", "després de triar el denominador comú, canvia també cada numerador (multiplica'l pel mateix nombre que el seu denominador)."),
            FractionExercises.CrossMultiply => T("to multiply, top × top and bottom × bottom; crossing is for dividing.", "para multiplicar, arriba × arriba y abajo × abajo; en cruz es para dividir.", "pour multiplier, haut × haut et bas × bas ; en croix, c'est pour diviser.", "per multiplicar, dalt × dalt i baix × baix; en creu és per dividir."),
            _ => code,
        };
    }
}
