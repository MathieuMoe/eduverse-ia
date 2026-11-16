using System.Globalization;
using EduVerse.Core.Common;

namespace EduVerse.Core.Learning;

/// <summary>
/// Exercises on fractions generated in code, in 4 languages. Every wrong option is a typical mistake,
/// tagged with its misconception code, so the tutor knows WHY the student failed, not only that he failed.
/// </summary>
public static class FractionExercises
{
    // Misconception codes (translated by the interface)
    public const string Inverted = "Inverted";
    public const string CountsTheRest = "CountsTheRest";
    public const string PartToPart = "PartToPart";
    public const string AddSameNumber = "AddSameNumber";
    public const string OnlyNumerator = "OnlyNumerator";
    public const string NotFullySimplified = "NotFullySimplified";
    public const string BiggerDenominatorBigger = "BiggerDenominatorBigger";
    public const string AddDenominators = "AddDenominators";
    public const string AddAcross = "AddAcross";
    public const string CrossMultiply = "CrossMultiply";
    public const string SameNumeratorEqual = "SameNumeratorEqual";
    public const string MultipliedInsteadOfAdding = "MultipliedInsteadOfAdding";
    public const string NotConverted = "NotConverted";

    public static Exercise Create(string conceptKey, int difficulty, Random random, TeachLanguage l)
    {
        ArgumentNullException.ThrowIfNull(random);
        difficulty = Math.Clamp(difficulty, Exercise.MinDifficulty, Exercise.MaxDifficulty);
        string T(string en, string es, string fr, string ca) => Lang.T(l, en, es, fr, ca);
        string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
        var max = difficulty switch { 1 => 6, 2 => 10, _ => 15 };
        var id = $"{conceptKey}-{difficulty}-{random.Next(100000):00000}";

        Exercise Build(string question, string correct, IEnumerable<(string Text, string Code)> wrong, string hint, string explanation)
        {
            // Distinct options, the correct one at a random position
            var options = new List<(string Text, string? Code)> { (correct, null) };
            foreach (var w in wrong.Where(w => w.Text != correct))
            {
                if (options.All(o => o.Text != w.Text))
                {
                    options.Add(w);
                }
            }

            options = options.Take(4).OrderBy(_ => random.Next()).ToList();
            return new Exercise(id, conceptKey, difficulty, question, options.Select(o => o.Text).ToList(),
                options.FindIndex(o => o.Code is null && o.Text == correct), options.Select(o => o.Code).ToList(), hint, explanation);
        }

        switch (conceptKey)
        {
            case "meaning":
            {
                var b = random.Next(3, max + 1);
                var a = random.Next(1, b);
                var bar = new string('■', a) + new string('□', b - a);
                return Build(
                    F(T("A bar is divided into {0} equal parts and {1} are coloured: {2}. What fraction is coloured?", "Una barra está dividida en {0} partes iguales y {1} están coloreadas: {2}. ¿Qué fracción está coloreada?", "Une barre est divisée en {0} parts égales et {1} sont coloriées : {2}. Quelle fraction est coloriée ?", "Una barra està dividida en {0} parts iguals i {1} estan pintades: {2}. Quina fracció està pintada?"), b, a, bar),
                    $"{a}/{b}", [($"{b}/{a}", Inverted), ($"{b - a}/{b}", CountsTheRest), ($"{a}/{b - a}", PartToPart)],
                    T("The denominator is the number of parts of the whole.", "El denominador es el número de partes del todo.", "Le dénominateur est le nombre de parts du tout.", "El denominador és el nombre de parts del tot."),
                    F(T("{0} coloured parts out of {1}: {0}/{1}.", "{0} partes coloreadas de {1}: {0}/{1}.", "{0} parts coloriées sur {1} : {0}/{1}.", "{0} parts pintades de {1}: {0}/{1}."), a, b));
            }

            case "equivalent":
            {
                var b = random.Next(2, Math.Max(4, max / 2) + 1);
                var a = random.Next(1, b);
                var k = random.Next(2, difficulty + 3);
                return Build(
                    F(T("Which fraction is equal to {0}/{1}?", "¿Qué fracción es igual a {0}/{1}?", "Quelle fraction est égale à {0}/{1} ?", "Quina fracció és igual a {0}/{1}?"), a, b),
                    $"{a * k}/{b * k}", [($"{a + k}/{b + k}", AddSameNumber), ($"{a * k}/{b}", OnlyNumerator), ($"{a}/{b * k}", OnlyNumerator)],
                    T("Multiply the top AND the bottom by the same number.", "Multiplica el de arriba Y el de abajo por el mismo número.", "Multiplie le haut ET le bas par le même nombre.", "Multiplica el de dalt I el de baix pel mateix nombre."),
                    F(T("{0}/{1} × {2}/{2} = {3}/{4}: multiplying by {2}/{2} is multiplying by 1.", "{0}/{1} × {2}/{2} = {3}/{4}: multiplicar por {2}/{2} es multiplicar por 1.", "{0}/{1} × {2}/{2} = {3}/{4} : multiplier par {2}/{2}, c'est multiplier par 1.", "{0}/{1} × {2}/{2} = {3}/{4}: multiplicar per {2}/{2} és multiplicar per 1."), a, b, k, a * k, b * k));
            }

            case "simplify":
            {
                var b = random.Next(2, Math.Max(4, max / 2) + 1);
                var a = random.Next(1, b);
                var g = Gcd(a, b);
                (a, b) = (a / g, b / g);
                var k = random.Next(2, difficulty + 4) * (difficulty == 3 ? 2 : 1);
                var half = k % 2 == 0 && k > 2 ? $"{a * k / 2}/{b * k / 2}" : $"{a * k}/{b}";
                return Build(
                    F(T("Simplify {0}/{1} as much as possible.", "Simplifica {0}/{1} al máximo.", "Simplifie {0}/{1} au maximum.", "Simplifica {0}/{1} al màxim."), a * k, b * k),
                    $"{a}/{b}", [(half, NotFullySimplified), ($"{a}/{b * k}", OnlyNumerator), ($"{Math.Max(1, a * k - 1)}/{b * k - 1}", AddSameNumber)],
                    T("Find the greatest number that divides both.", "Busca el mayor número que divide a los dos.", "Cherche le plus grand nombre qui divise les deux.", "Busca el nombre més gran que divideix tots dos."),
                    F(T("Both are divisible by {0}: {1}/{2} = {3}/{4}.", "Los dos son divisibles entre {0}: {1}/{2} = {3}/{4}.", "Les deux sont divisibles par {0} : {1}/{2} = {3}/{4}.", "Tots dos són divisibles per {0}: {1}/{2} = {3}/{4}."), k, a * k, b * k, a, b));
            }

            case "compare":
            {
                // Same numerator, different denominators: the classic trap
                var a = random.Next(1, 4);
                var b = random.Next(a + 1, max);
                var d = random.Next(b + 1, max + 3);
                return Build(
                    F(T("Which is bigger: {0}/{1} or {0}/{2}?", "¿Cuál es mayor: {0}/{1} o {0}/{2}?", "Laquelle est la plus grande : {0}/{1} ou {0}/{2} ?", "Quina és més gran: {0}/{1} o {0}/{2}?"), a, b, d),
                    $"{a}/{b}", [($"{a}/{d}", BiggerDenominatorBigger), (T("They are equal", "Son iguales", "Elles sont égales", "Són iguals"), SameNumeratorEqual)],
                    T("Imagine a pizza cut into more slices: is each slice bigger or smaller?", "Imagina una pizza cortada en más trozos: ¿cada trozo es más grande o más pequeño?", "Imagine une pizza coupée en plus de parts : chaque part est-elle plus grande ou plus petite ?", "Imagina una pizza tallada en més trossos: cada tros és més gran o més petit?"),
                    F(T("With the same numerator, the smaller denominator gives bigger parts: {0}/{1} > {0}/{2}.", "Con el mismo numerador, el denominador más pequeño da partes más grandes: {0}/{1} > {0}/{2}.", "Avec le même numérateur, le plus petit dénominateur donne des parts plus grandes : {0}/{1} > {0}/{2}.", "Amb el mateix numerador, el denominador més petit dona parts més grans: {0}/{1} > {0}/{2}."), a, b, d));
            }

            case "add-same":
            {
                var b = random.Next(4, max + 3);
                var a = random.Next(1, b - 1);
                var c = random.Next(1, b - a);
                return Build(
                    F(T("{0}/{2} + {1}/{2} = ?", "{0}/{2} + {1}/{2} = ?", "{0}/{2} + {1}/{2} = ?", "{0}/{2} + {1}/{2} = ?"), a, c, b),
                    $"{a + c}/{b}", [($"{a + c}/{b * 2}", AddDenominators), ($"{a * c}/{b}", MultipliedInsteadOfAdding), ($"{a + c}/{b + b + 1}", AddDenominators)],
                    T("Are the parts still the same size after adding?", "¿Las partes siguen siendo del mismo tamaño después de sumar?", "Les parts ont-elles toujours la même taille après l'addition ?", "Les parts continuen sent de la mateixa mida després de sumar?"),
                    F(T("Same parts ({2}ths): {0} + {1} = {3}, so {3}/{2}.", "Mismas partes (de {2}): {0} + {1} = {3}, así que {3}/{2}.", "Mêmes parts (des {2}e) : {0} + {1} = {3}, donc {3}/{2}.", "Mateixes parts (de {2}): {0} + {1} = {3}, així que {3}/{2}."), a, c, b, a + c));
            }

            case "add-different":
            {
                var b = random.Next(2, difficulty == 1 ? 5 : 7);
                var d = b * random.Next(2, difficulty + 2);
                if (difficulty == 3)
                {
                    d = random.Next(b + 1, b + 5);
                }

                var a = random.Next(1, b);
                var c = random.Next(1, d);
                var den = Lcm(b, d);
                var num = a * (den / b) + c * (den / d);
                var g = Gcd(num, den);
                return Build(
                    F(T("{0}/{1} + {2}/{3} = ?", "{0}/{1} + {2}/{3} = ?", "{0}/{1} + {2}/{3} = ?", "{0}/{1} + {2}/{3} = ?"), a, b, c, d),
                    $"{num / g}/{den / g}", [($"{a + c}/{b + d}", AddAcross), ($"{a + c}/{den}", NotConverted), ($"{a * c}/{b * d}", MultipliedInsteadOfAdding)],
                    F(T("Write both fractions with denominator {0} first.", "Escribe primero las dos fracciones con denominador {0}.", "Écris d'abord les deux fractions avec le dénominateur {0}.", "Escriu primer les dues fraccions amb denominador {0}."), den),
                    F(T("{0}/{1} = {4}/{6} and {2}/{3} = {5}/{6}, so the sum is {7}/{6}{8}.", "{0}/{1} = {4}/{6} y {2}/{3} = {5}/{6}, así que la suma es {7}/{6}{8}.", "{0}/{1} = {4}/{6} et {2}/{3} = {5}/{6}, donc la somme est {7}/{6}{8}.", "{0}/{1} = {4}/{6} i {2}/{3} = {5}/{6}, així que la suma és {7}/{6}{8}."),
                        a, b, c, d, a * (den / b), c * (den / d), den, num, g > 1 ? $" = {num / g}/{den / g}" : ""));
            }

            default:
            {
                var a = random.Next(1, 5);
                var b = random.Next(a + 1, max);
                var c = random.Next(1, 5);
                var d = random.Next(c + 1, max);
                var g = Gcd(a * c, b * d);
                return Build(
                    F(T("{0}/{1} × {2}/{3} = ?", "{0}/{1} × {2}/{3} = ?", "{0}/{1} × {2}/{3} = ?", "{0}/{1} × {2}/{3} = ?"), a, b, c, d),
                    $"{a * c / g}/{b * d / g}", [($"{a * d}/{b * c}", CrossMultiply), ($"{a + c}/{b + d}", AddAcross), ($"{a * c}/{b}", OnlyNumerator)],
                    T("Top times top, bottom times bottom.", "Arriba por arriba, abajo por abajo.", "Haut fois haut, bas fois bas.", "Dalt per dalt, baix per baix."),
                    F(T("{0}×{2} = {4} and {1}×{3} = {5}: {4}/{5}{6}.", "{0}×{2} = {4} y {1}×{3} = {5}: {4}/{5}{6}.", "{0}×{2} = {4} et {1}×{3} = {5} : {4}/{5}{6}.", "{0}×{2} = {4} i {1}×{3} = {5}: {4}/{5}{6}."), a, b, c, d, a * c, b * d, g > 1 ? $" = {a * c / g}/{b * d / g}" : ""));
            }
        }
    }

    public static int Gcd(int a, int b) => b == 0 ? Math.Abs(a) : Gcd(b, a % b);

    public static int Lcm(int a, int b) => a / Gcd(a, b) * b;
}
