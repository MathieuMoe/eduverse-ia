namespace EduVerse.Core.Catalog;

/// <summary>Mathematics, generated and solved in code: every answer is computed, never written by hand.</summary>
internal static class MathBank
{
    private static readonly string[] Things = ["🍎", "⭐", "🐶", "🎈", "🌸", "🐟", "🚗", "🧸"];

    public static QuizQuestion Generate(Mc q) => q.Level switch
    {
        Level.EarlyYears => q.Topic(Count, AddSmall, Biggest)(q),
        Level.Primary1 => q.Topic(Shapes, Add, Subtract)(q),
        Level.Primary2 => q.Topic(Multiply, Divide, AddBig)(q),
        Level.Primary3 => q.Topic(Percent, Rectangle, FractionOf)(q),
        Level.Secondary1 => q.Topic(Integers, Proportion, LinearEquation, Powers)(q),
        Level.Secondary2 => q.Topic(Pythagoras, System2, Slope, Quadratic)(q),
        Level.HighSchool => q.Topic(Sequence, Logarithm, Derivative, Trigonometry, Dice)(q),
        Level.Bachelor => q.Topic(Determinant, Limit, Integral)(q),
        Level.Master => q.Topic(Expectation, Eigenvalue, Binomial)(q),
        _ => q.Topic(Variance, Radius, Taylor)(q),
    };

    private static QuizQuestion Count(Mc q)
    {
        var n = q.Between(1, q.Difficulty switch { 1 => 5, 2 => 8, _ => 10 });
        var thing = q.Pick(Things);
        return q.Number("math.count", q.T("How many are there?|¿Cuántos hay?|Combien y en a-t-il ?|Quants n'hi ha?"), n, [n - 1, n + 1, n + 2],
            q.F("Count them one by one: there are {0}.|Cuéntalos uno a uno: hay {0}.|Compte-les un par un : il y en a {0}.|Compta'ls un per un: n'hi ha {0}.", n),
            visual: string.Join(' ', Enumerable.Repeat(thing, n)));
    }

    private static QuizQuestion AddSmall(Mc q)
    {
        var a = q.Between(1, 5);
        var b = q.Between(1, q.Difficulty == 1 ? 3 : 5);
        var thing = q.Pick(Things);
        return q.Number("math.add", q.T("How many are there in total?|¿Cuántos hay en total?|Combien y en a-t-il en tout ?|Quants n'hi ha en total?"), a + b, [a + b - 1, a + b + 1, a],
            q.F("{0} and {1} more make {2}.|{0} y {1} más son {2}.|{0} et encore {1}, ça fait {2}.|{0} i {1} més fan {2}.", a, b, a + b),
            visual: string.Join(' ', Enumerable.Repeat(thing, a)) + "  ➕  " + string.Join(' ', Enumerable.Repeat(thing, b)));
    }

    private static QuizQuestion Biggest(Mc q)
    {
        var numbers = Enumerable.Range(1, q.Difficulty == 3 ? 20 : 10).OrderBy(_ => q.Rng.Next()).Take(4).ToList();
        var max = numbers.Max();
        return q.Choice("math.compare", q.T("Which number is the biggest?|¿Qué número es el más grande?|Quel nombre est le plus grand ?|Quin nombre és el més gran?"),
            max.ToString(Levels.Culture(q.Language)), numbers.Where(n => n != max).Select(n => n.ToString(Levels.Culture(q.Language))),
            q.F("{0} comes after the others when you count.|El {0} va después de los demás al contar.|{0} vient après les autres quand on compte.|El {0} va després dels altres quan comptes.", max));
    }

    private static readonly (string Name, int Sides)[] ShapeList =
    [
        ("triangle|triángulo|triangle|triangle", 3), ("square|cuadrado|carré|quadrat", 4), ("pentagon|pentágono|pentagone|pentàgon", 5),
        ("hexagon|hexágono|hexagone|hexàgon", 6), ("octagon|octógono|octogone|octògon", 8),
    ];

    private static QuizQuestion Shapes(Mc q)
    {
        var (name, sides) = q.Pick(ShapeList);
        return q.Number("math.shapes", q.F("How many sides does a {0} have?|¿Cuántos lados tiene un {0}?|Combien de côtés a un {0} ?|Quants costats té un {0}?", q.T(name)), sides,
            [sides - 1, sides + 1, sides + 2], q.F("A {0} has {1} sides.|Un {0} tiene {1} lados.|Un {0} a {1} côtés.|Un {0} té {1} costats.", q.T(name), sides),
            visual: sides switch { 3 => "🔺", 4 => "🟥", 6 => "⬢", 8 => "🛑", _ => "⬠" });
    }

    private static QuizQuestion Add(Mc q)
    {
        var max = q.Difficulty switch { 1 => 20, 2 => 50, _ => 100 };
        var a = q.Between(2, max / 2);
        var b = q.Between(2, max - a);
        return q.Number("math.add", $"{a} + {b} = ?", a + b, [a + b + 1, a + b - 1, a + b + 10, a - b],
            q.F("Add the units, then the tens: {0} + {1} = {2}.|Suma las unidades y luego las decenas: {0} + {1} = {2}.|Ajoute les unités puis les dizaines : {0} + {1} = {2}.|Suma les unitats i després les desenes: {0} + {1} = {2}.", a, b, a + b));
    }

    private static QuizQuestion Subtract(Mc q)
    {
        var max = q.Difficulty switch { 1 => 20, 2 => 50, _ => 100 };
        var a = q.Between(5, max);
        var b = q.Between(1, a - 1);
        return q.Number("math.subtract", $"{a} − {b} = ?", a - b, [a + b, a - b + 1, a - b - 1, a - b + 10],
            q.F("Check it: {0} + {1} = {2}.|Compruébalo: {0} + {1} = {2}.|Vérifie : {0} + {1} = {2}.|Comprova-ho: {0} + {1} = {2}.", a - b, b, a));
    }

    private static QuizQuestion Multiply(Mc q)
    {
        var (lo, hi) = q.Difficulty switch { 1 => (2, 5), 2 => (2, 9), _ => (6, 12) };
        var a = q.Between(lo, hi);
        var b = q.Between(2, hi);
        return q.Number("math.multiply", $"{a} × {b} = ?", a * b, [a * (b + 1), a + b, a * b - a, a * b + 1],
            q.F("{0} × {1} is {1} added {0} times: {2}.|{0} × {1} es sumar {1} {0} veces: {2}.|{0} × {1}, c'est ajouter {1} {0} fois : {2}.|{0} × {1} és sumar {1} {0} vegades: {2}.", a, b, a * b));
    }

    private static QuizQuestion Divide(Mc q)
    {
        var a = q.Between(2, q.Difficulty == 3 ? 12 : 9);
        var b = q.Between(2, 10);
        return q.Number("math.divide", $"{a * b} ÷ {a} = ?", b, [b + 1, b - 1, a * b - a, a],
            q.F("Because {0} × {1} = {2}.|Porque {0} × {1} = {2}.|Parce que {0} × {1} = {2}.|Perquè {0} × {1} = {2}.", a, b, a * b));
    }

    private static QuizQuestion AddBig(Mc q)
    {
        var a = q.Between(105, 489);
        var b = q.Between(105, 499);
        var noCarry = (a % 10 + b % 10) % 10 + 10 * ((a / 10 % 10 + b / 10 % 10) % 10) + 100 * (a / 100 + b / 100);
        return q.Number("math.add", $"{a} + {b} = ?", a + b, [noCarry, a + b + 10, a + b - 100, a + b + 1],
            q.F("Add column by column and carry the tens: {0} + {1} = {2}.|Suma columna a columna y lleva las decenas: {0} + {1} = {2}.|Additionne colonne par colonne avec les retenues : {0} + {1} = {2}.|Suma columna a columna i porta les desenes: {0} + {1} = {2}.", a, b, a + b));
    }

    private static readonly int[] EasyPercents = [10, 50];

    private static readonly int[] Percents = [10, 20, 25, 50, 75];

    private static QuizQuestion Percent(Mc q)
    {
        var p = q.Pick(q.Difficulty == 1 ? EasyPercents : Percents);
        var baseValue = 20 * q.Between(1, q.Difficulty == 3 ? 30 : 10);
        var answer = baseValue * p / 100.0;
        return q.Number("math.percent", q.F("What is {0} % of {1}?|¿Cuánto es el {0} % de {1}?|Combien font {0} % de {1} ?|Quant és el {0} % de {1}?", p, baseValue), answer,
            [baseValue * p / 10.0, p, baseValue - answer, answer + 10],
            q.F("{0} % means {0} out of 100: {1} × {0} ÷ 100 = {2}.|{0} % significa {0} de cada 100: {1} × {0} ÷ 100 = {2}.|{0} % signifie {0} sur 100 : {1} × {0} ÷ 100 = {2}.|{0} % vol dir {0} de cada 100: {1} × {0} ÷ 100 = {2}.", p, baseValue, q.N(answer)));
    }

    private static QuizQuestion Rectangle(Mc q)
    {
        var a = q.Between(2, 12);
        var b = q.Between(2, 12);
        if (q.Rng.Next(2) == 0)
        {
            return q.Number("math.area", q.F("Area of a rectangle of {0} cm by {1} cm?|¿Área de un rectángulo de {0} cm por {1} cm?|Aire d'un rectangle de {0} cm sur {1} cm ?|Àrea d'un rectangle de {0} cm per {1} cm?", a, b),
                a * b, [2 * (a + b), a + b, a * b + a], q.F("Area = length × width = {0} × {1} = {2} cm².|Área = largo × ancho = {0} × {1} = {2} cm².|Aire = longueur × largeur = {0} × {1} = {2} cm².|Àrea = llarg × ample = {0} × {1} = {2} cm².", a, b, a * b), unit: " cm²");
        }

        return q.Number("math.perimeter", q.F("Perimeter of a rectangle of {0} cm by {1} cm?|¿Perímetro de un rectángulo de {0} cm por {1} cm?|Périmètre d'un rectangle de {0} cm sur {1} cm ?|Perímetre d'un rectangle de {0} cm per {1} cm?", a, b),
            2 * (a + b), [a * b, a + b, 2 * a + b], q.F("Perimeter = 2 × ({0} + {1}) = {2} cm.|Perímetro = 2 × ({0} + {1}) = {2} cm.|Périmètre = 2 × ({0} + {1}) = {2} cm.|Perímetre = 2 × ({0} + {1}) = {2} cm.", a, b, 2 * (a + b)), unit: " cm");
    }

    private static QuizQuestion FractionOf(Mc q)
    {
        var d = q.Between(2, q.Difficulty == 1 ? 4 : 8);
        var n = q.Between(1, d - 1);
        var k = q.Between(2, 9);
        var total = d * k;
        return q.Number("math.fraction", q.F("What is {0}/{1} of {2}?|¿Cuánto es {0}/{1} de {2}?|Combien font {0}/{1} de {2} ?|Quant és {0}/{1} de {2}?", n, d, total), n * k,
            [k, total / (double)n, n * k + k, total - n],
            q.F("{2} ÷ {1} = {3}, then × {0} = {4}.|{2} ÷ {1} = {3}, y luego × {0} = {4}.|{2} ÷ {1} = {3}, puis × {0} = {4}.|{2} ÷ {1} = {3}, i després × {0} = {4}.", n, d, total, k, n * k));
    }

    private static QuizQuestion Integers(Mc q)
    {
        var a = q.Between(2, 15);
        var b = q.Between(2, 15);
        if (q.Rng.Next(2) == 0)
        {
            return q.Number("math.integers", $"(−{a}) + {b} = ?", b - a, [a + b, a - b, -(a + b)],
                q.F("Different signs: subtract and keep the sign of the larger one: {0}.|Signos distintos: resta y pon el signo del mayor: {0}.|Signes différents : on soustrait et on garde le signe du plus grand : {0}.|Signes diferents: resta i posa el signe del més gran: {0}.", b - a));
        }

        return q.Number("math.integers", $"(−{a}) × (−{b}) = ?", a * b, [-a * b, a + b, -(a + b)],
            q.T("Minus times minus gives plus.|Menos por menos da más.|Moins par moins donne plus.|Menys per menys dona més."));
    }

    private static QuizQuestion LinearEquation(Mc q)
    {
        var a = q.Between(2, q.Difficulty == 3 ? 12 : 6);
        var x = q.Between(-5, 12);
        var b = q.Between(1, 20);
        var c = a * x + b;
        return q.Number("math.equations", q.F("Solve: {0}x + {1} = {2}|Resuelve: {0}x + {1} = {2}|Résous : {0}x + {1} = {2}|Resol: {0}x + {1} = {2}", a, b, c), x,
            [(c + b) / (double)a, c - b, -x, x + 1], q.F("{0}x = {1} − {2} = {3}, so x = {3} ÷ {0} = {4}.|{0}x = {1} − {2} = {3}, así que x = {3} ÷ {0} = {4}.|{0}x = {1} − {2} = {3}, donc x = {3} ÷ {0} = {4}.|{0}x = {1} − {2} = {3}, per tant x = {3} ÷ {0} = {4}.", a, c, b, c - b, x));
    }

    private static QuizQuestion Proportion(Mc q)
    {
        var k = q.Between(2, 6);
        var unit = q.Between(2, 9);
        var m = q.Between(k + 1, 15);
        return q.Number("math.proportion", q.F("{0} notebooks cost {1} €. How much do {2} notebooks cost?|{0} cuadernos cuestan {1} €. ¿Cuánto cuestan {2} cuadernos?|{0} cahiers coûtent {1} €. Combien coûtent {2} cahiers ?|{0} llibretes costen {1} €. Quant costen {2} llibretes?", k, k * unit, m),
            m * unit, [m * k * unit, k * unit + m - k, m * unit + unit], q.F("One costs {0} ÷ {1} = {2} €, so {3} cost {4} €.|Uno cuesta {0} ÷ {1} = {2} €, así que {3} cuestan {4} €.|Un coûte {0} ÷ {1} = {2} €, donc {3} coûtent {4} €.|Una costa {0} ÷ {1} = {2} €, per tant {3} costen {4} €.", k * unit, k, unit, m, m * unit), unit: " €");
    }

    private static QuizQuestion Powers(Mc q)
    {
        var a = q.Between(2, 5);
        var n = q.Between(2, q.Difficulty == 3 ? 5 : 3);
        var answer = (int)Math.Pow(a, n);
        return q.Number("math.powers", $"{a}^{n} = ?", answer, [a * n, Math.Pow(a, n - 1), Math.Pow(n, a)],
            q.F("{0} multiplied by itself {1} times: {2}.|{0} multiplicado por sí mismo {1} veces: {2}.|{0} multiplié par lui-même {1} fois : {2}.|{0} multiplicat per ell mateix {1} vegades: {2}.", a, n, answer));
    }

    private static readonly (int A, int B, int C)[] Triples = [(3, 4, 5), (5, 12, 13), (8, 15, 17), (7, 24, 25), (6, 8, 10), (9, 12, 15), (20, 21, 29)];

    private static QuizQuestion Pythagoras(Mc q)
    {
        var (a, b, c) = q.Pick(Triples);
        return q.Number("math.pythagoras", q.F("A right triangle has legs of {0} and {1}. How long is the hypotenuse?|Un triángulo rectángulo tiene catetos de {0} y {1}. ¿Cuánto mide la hipotenusa?|Un triangle rectangle a des côtés de l'angle droit de {0} et {1}. Combien mesure l'hypoténuse ?|Un triangle rectangle té catets de {0} i {1}. Quant fa la hipotenusa?", a, b),
            c, [a + b, a * a + b * b, c + 1], q.F("c² = {0}² + {1}² = {2}, so c = {3}.|c² = {0}² + {1}² = {2}, así que c = {3}.|c² = {0}² + {1}² = {2}, donc c = {3}.|c² = {0}² + {1}² = {2}, per tant c = {3}.", a, b, a * a + b * b, c));
    }

    private static QuizQuestion System2(Mc q)
    {
        var x = q.Between(1, 15);
        var y = q.Between(1, 15);
        return q.Number("math.systems", q.F("x + y = {0} and x − y = {1}. What is x?|x + y = {0} y x − y = {1}. ¿Cuánto vale x?|x + y = {0} et x − y = {1}. Que vaut x ?|x + y = {0} i x − y = {1}. Quant val x?", x + y, x - y),
            x, [y, x + y, x - y], q.F("Add both equations: 2x = {0}, so x = {1}.|Suma las dos ecuaciones: 2x = {0}, así que x = {1}.|Additionne les deux équations : 2x = {0}, donc x = {1}.|Suma les dues equacions: 2x = {0}, per tant x = {1}.", 2 * x, x));
    }

    private static QuizQuestion Slope(Mc q)
    {
        var m = q.Between(-4, 5);
        var x1 = q.Between(-3, 3);
        var x2 = x1 + q.Between(1, 4);
        var c = q.Between(-5, 5);
        var (y1, y2) = (m * x1 + c, m * x2 + c);
        return q.Number("math.slope", q.F("Slope of the line through ({0}, {1}) and ({2}, {3})?|¿Pendiente de la recta que pasa por ({0}, {1}) y ({2}, {3})?|Pente de la droite passant par ({0}, {1}) et ({2}, {3}) ?|Pendent de la recta que passa per ({0}, {1}) i ({2}, {3})?", x1, y1, x2, y2),
            m, [-m, (x2 - x1) / (double)(y2 - y1 == 0 ? 1 : y2 - y1), m + 1, y2 - y1], q.F("m = ({0}) ÷ ({1}) = {2}.|m = ({0}) ÷ ({1}) = {2}.|m = ({0}) ÷ ({1}) = {2}.|m = ({0}) ÷ ({1}) = {2}.", y2 - y1, x2 - x1, m));
    }

    private static QuizQuestion Quadratic(Mc q)
    {
        var r1 = q.Between(1, 9);
        var r2 = -q.Between(1, 9);
        if (r2 == -r1)
        {
            r2--;
        }

        var equation = "x²" + Mc.Term(-(r1 + r2), "x") + Mc.Term((long)r1 * r2, "") + " = 0";
        return q.Number("math.quadratic", q.F("Positive solution of {0}?|¿Solución positiva de {0}?|Solution positive de {0} ?|Solució positiva de {0}?", equation),
            r1, [-r1, -r2, r1 + 1], q.F("It factors as (x − {0})(x + {1}) = 0.|Se factoriza como (x − {0})(x + {1}) = 0.|Elle se factorise en (x − {0})(x + {1}) = 0.|Es factoritza com (x − {0})(x + {1}) = 0.", r1, -r2));
    }

    private static QuizQuestion Sequence(Mc q)
    {
        var a1 = q.Between(-5, 10);
        var d = q.Between(2, 9);
        var n = q.Between(8, 30);
        var an = a1 + (n - 1) * d;
        return q.Number("math.sequences", q.F("Arithmetic sequence: a₁ = {0}, d = {1}. What is a{2}?|Progresión aritmética: a₁ = {0}, d = {1}. ¿Cuánto es a{2}?|Suite arithmétique : u₁ = {0}, r = {1}. Que vaut u{2} ?|Progressió aritmètica: a₁ = {0}, d = {1}. Quant és a{2}?", a1, d, Sub(n)),
            an, [a1 + n * d, a1 * n, an - a1], q.F("aₙ = a₁ + (n − 1)d = {0} + {1} × {2} = {3}.|aₙ = a₁ + (n − 1)d = {0} + {1} × {2} = {3}.|uₙ = u₁ + (n − 1)r = {0} + {1} × {2} = {3}.|aₙ = a₁ + (n − 1)d = {0} + {1} × {2} = {3}.", a1, n - 1, d, an));
    }

    private static string Sub(int n) => string.Concat(n.ToString(System.Globalization.CultureInfo.InvariantCulture).Select(c => "₀₁₂₃₄₅₆₇₈₉"[c - '0']));

    private static QuizQuestion Logarithm(Mc q)
    {
        var b = q.Pick([2, 3, 10]);
        var n = q.Between(2, b == 10 ? 6 : 6);
        var x = (long)Math.Pow(b, n);
        return q.Number("math.logarithms", $"log{Sub(b)}({x}) = ?", n, [x / (double)b, n + 1, b * n],
            q.F("{0} to the power {1} is {2}.|{0} elevado a {1} es {2}.|{0} puissance {1} vaut {2}.|{0} elevat a {1} és {2}.", b, n, x));
    }

    private static QuizQuestion Derivative(Mc q)
    {
        var a = q.Between(1, 4);
        var b = q.Between(-5, 5);
        var c = q.Between(-6, 6);
        var x0 = q.Between(-3, 3);
        var f = Mc.Term(a, "x³", true) + Mc.Term(b, "x²") + Mc.Term(c, "x");
        var d = 3 * a * x0 * x0 + 2 * b * x0 + c;
        return q.Number("math.derivatives", q.F("f(x) = {0}. What is f′({1})?|f(x) = {0}. ¿Cuánto vale f′({1})?|f(x) = {0}. Que vaut f′({1}) ?|f(x) = {0}. Quant val f′({1})?", f, x0),
            d, [a * x0 * x0 * x0 + b * x0 * x0 + c * x0, 3 * a * x0 + 2 * b + c, d + a],
            q.F("f′(x) = {0}x² + {1}x + {2}, so f′({3}) = {4}.|f′(x) = {0}x² + {1}x + {2}, así que f′({3}) = {4}.|f′(x) = {0}x² + {1}x + {2}, donc f′({3}) = {4}.|f′(x) = {0}x² + {1}x + {2}, per tant f′({3}) = {4}.", 3 * a, 2 * b, c, x0, d));
    }

    private static readonly (string Function, int Angle, string Value)[] Trig =
    [
        ("sin", 30, "1/2"), ("sin", 45, "√2/2"), ("sin", 60, "√3/2"), ("sin", 90, "1"), ("cos", 0, "1"), ("cos", 30, "√3/2"),
        ("cos", 60, "1/2"), ("cos", 90, "0"), ("tan", 45, "1"), ("tan", 60, "√3"), ("tan", 30, "√3/3"),
    ];

    private static QuizQuestion Trigonometry(Mc q)
    {
        var (function, angle, value) = q.Pick(Trig);
        return q.Choice("math.trigonometry", $"{function}({angle}°) = ?", value, Trig.Select(t => t.Value).Distinct(),
            q.F("{0}({1}°) = {2}: remember the triangle with angles 30°, 60° and 90° and the one with 45°.|{0}({1}°) = {2}: recuerda el triángulo de 30°, 60° y 90° y el de 45°.|{0}({1}°) = {2} : souviens-toi du triangle à 30°, 60°, 90° et de celui à 45°.|{0}({1}°) = {2}: recorda el triangle de 30°, 60° i 90° i el de 45°.", function, angle, value));
    }

    private static QuizQuestion Dice(Mc q)
    {
        var sum = q.Between(2, 12);
        var ways = 6 - Math.Abs(7 - sum);
        return q.Choice("math.probability", q.F("Two dice are rolled. Probability that the sum is {0}?|Se lanzan dos dados. ¿Probabilidad de que la suma sea {0}?|On lance deux dés. Probabilité que la somme fasse {0} ?|Es llancen dos daus. Probabilitat que la suma sigui {0}?", sum),
            Mc.Frac(ways, 36), [Mc.Frac(1, 11), Mc.Frac(ways + 1, 36), Mc.Frac(1, 6), Mc.Frac(1, 12), Mc.Frac(sum, 36), "1/36"],
            q.F("{0} of the 36 equally likely outcomes give {1}: {0}/36 = {2}.|{0} de los 36 resultados igual de probables dan {1}: {0}/36 = {2}.|{0} des 36 issues équiprobables donnent {1} : {0}/36 = {2}.|{0} dels 36 resultats igual de probables donen {1}: {0}/36 = {2}.", ways, sum, Mc.Frac(ways, 36)));
    }

    private static QuizQuestion Determinant(Mc q)
    {
        if (q.Difficulty < 3)
        {
            int a = q.Between(-5, 9), b = q.Between(-5, 9), c = q.Between(-5, 9), d = q.Between(-5, 9);
            return q.Number("math.determinants", q.F("Determinant of the matrix [[{0}, {1}], [{2}, {3}]]?|¿Determinante de la matriz [[{0}, {1}], [{2}, {3}]]?|Déterminant de la matrice [[{0}, {1}], [{2}, {3}]] ?|Determinant de la matriu [[{0}, {1}], [{2}, {3}]]?", a, b, c, d),
                a * d - b * c, [a * d + b * c, b * c - a * d, a * d, a * b - c * d], q.F("ad − bc = {0} − {1} = {2}.|ad − bc = {0} − {1} = {2}.|ad − bc = {0} − {1} = {2}.|ad − bc = {0} − {1} = {2}.", a * d, b * c, a * d - b * c));
        }

        var m = Enumerable.Range(0, 9).Select(_ => q.Between(-3, 4)).ToArray();
        var det = m[0] * (m[4] * m[8] - m[5] * m[7]) - m[1] * (m[3] * m[8] - m[5] * m[6]) + m[2] * (m[3] * m[7] - m[4] * m[6]);
        var diagonal = m[0] * m[4] * m[8];
        return q.Number("math.determinants", q.F("Determinant of [[{0}, {1}, {2}], [{3}, {4}, {5}], [{6}, {7}, {8}]]?|¿Determinante de [[{0}, {1}, {2}], [{3}, {4}, {5}], [{6}, {7}, {8}]]?|Déterminant de [[{0}, {1}, {2}], [{3}, {4}, {5}], [{6}, {7}, {8}]] ?|Determinant de [[{0}, {1}, {2}], [{3}, {4}, {5}], [{6}, {7}, {8}]]?", m.Cast<object>().ToArray()),
            det, [-det, diagonal, det + 1, det - 2], q.T("Expand along the first row (Laplace expansion) or use Sarrus' rule.|Desarrolla por la primera fila (Laplace) o usa la regla de Sarrus.|Développe selon la première ligne (Laplace) ou utilise la règle de Sarrus.|Desenvolupa per la primera fila (Laplace) o fes servir la regla de Sarrus."));
    }

    private static QuizQuestion Limit(Mc q)
    {
        var a = q.Between(1, 9);
        var b = q.Between(1, 9);
        var top = q.Between(1, 3);
        var bottom = q.Difficulty == 1 ? top : q.Between(1, 3);
        var answer = top == bottom ? Mc.Frac(a, b) : top < bottom ? "0" : "∞";
        return q.Choice("math.limits", q.F("lim x→∞ of ({0}x{1} + 1) / ({2}x{3} − 4)?|¿lím x→∞ de ({0}x{1} + 1) / ({2}x{3} − 4)?|lim x→∞ de ({0}x{1} + 1) / ({2}x{3} − 4) ?|lím x→∞ de ({0}x{1} + 1) / ({2}x{3} − 4)?", a, Sup(top), b, Sup(bottom)),
            answer, ["0", "∞", Mc.Frac(a, b), Mc.Frac(b, a), "1", Mc.Frac(-1, 4)],
            q.T("Only the terms of highest degree matter: compare the degrees, and if they are equal take the ratio of the leading coefficients.|Solo importan los términos de mayor grado: compara los grados y, si son iguales, divide los coeficientes principales.|Seuls les termes de plus haut degré comptent : compare les degrés et, s'ils sont égaux, prends le rapport des coefficients dominants.|Només compten els termes de grau més alt: compara els graus i, si són iguals, divideix els coeficients principals."));
    }

    private static string Sup(int n) => n switch { 1 => "", 2 => "²", _ => "³" };

    private static QuizQuestion Integral(Mc q)
    {
        var a = q.Pick([3, 6]);
        var c = q.Between(-4, 5);
        var b = q.Between(1, q.Difficulty == 3 ? 4 : 3);
        var answer = a * b * b * b / 3 + c * b;
        return q.Number("math.integrals", q.F("∫ from 0 to {0} of ({1}x² {2}) dx = ?|∫ de 0 a {0} de ({1}x² {2}) dx = ?|∫ de 0 à {0} de ({1}x² {2}) dx = ?|∫ de 0 a {0} de ({1}x² {2}) dx = ?", b, a, (c < 0 ? "− " : "+ ") + Math.Abs(c)),
            answer, [a * b * b + c, a * b * b * b + c * b, 2 * a * b + c], q.F("A primitive is {0}x³ + {1}x; evaluate it at {2} and at 0: {3}.|Una primitiva es {0}x³ + {1}x; evalúala en {2} y en 0: {3}.|Une primitive est {0}x³ + {1}x ; on l'évalue en {2} et en 0 : {3}.|Una primitiva és {0}x³ + {1}x; avalua-la a {2} i a 0: {3}.", a / 3, c, b, answer));
    }

    private static QuizQuestion Expectation(Mc q)
    {
        var dice = q.Between(1, 4);
        var faces = q.Pick([4, 6, 8, 10, 12, 20]);
        var answer = dice * (faces + 1) / 2.0;
        return q.Number("math.expectation", q.F("Expected value of the sum of {0} fair dice with {1} faces (1 to {1})?|¿Esperanza de la suma de {0} dados equilibrados de {1} caras (1 a {1})?|Espérance de la somme de {0} dés équilibrés à {1} faces (1 à {1}) ?|Esperança de la suma de {0} daus equilibrats de {1} cares (1 a {1})?", dice, faces),
            answer, [dice * faces / 2.0, (faces + 1) / 2.0, answer + 1, dice * faces], q.F("One die: ({0} + 1) / 2 = {1}. Expectations add up: {2} × {1} = {3}.|Un dado: ({0} + 1) / 2 = {1}. Las esperanzas se suman: {2} × {1} = {3}.|Un dé : ({0} + 1) / 2 = {1}. Les espérances s'additionnent : {2} × {1} = {3}.|Un dau: ({0} + 1) / 2 = {1}. Les esperances se sumen: {2} × {1} = {3}.", faces, q.N((faces + 1) / 2.0), dice, q.N(answer)), decimals: 1);
    }

    private static QuizQuestion Eigenvalue(Mc q)
    {
        var p = q.Between(-4, 6);
        var r = q.Between(1, 6);
        return q.Number("math.eigenvalues", q.F("Largest eigenvalue of the matrix [[{0}, {1}], [{1}, {0}]]?|¿Mayor valor propio de la matriz [[{0}, {1}], [{1}, {0}]]?|Plus grande valeur propre de la matrice [[{0}, {1}], [{1}, {0}]] ?|Valor propi més gran de la matriu [[{0}, {1}], [{1}, {0}]]?", p, r),
            p + r, [p, p - r, p * r, p + 2 * r], q.F("The eigenvectors are (1, 1) and (1, −1), with eigenvalues {0} + {1} and {0} − {1}.|Los vectores propios son (1, 1) y (1, −1), con valores propios {0} + {1} y {0} − {1}.|Les vecteurs propres sont (1, 1) et (1, −1), de valeurs propres {0} + {1} et {0} − {1}.|Els vectors propis són (1, 1) i (1, −1), amb valors propis {0} + {1} i {0} − {1}.", p, r));
    }

    private static long Choose(int n, int k) => k == 0 ? 1 : Choose(n - 1, k - 1) * n / k;

    private static QuizQuestion Binomial(Mc q)
    {
        var n = q.Between(3, q.Difficulty == 3 ? 8 : 5);
        var k = q.Between(1, n - 1);
        var total = 1L << n;
        return q.Choice("math.binomial", q.F("A fair coin is tossed {0} times. Probability of exactly {1} heads?|Se lanza {0} veces una moneda equilibrada. ¿Probabilidad de exactamente {1} caras?|On lance {0} fois une pièce équilibrée. Probabilité d'obtenir exactement {1} piles ?|Es llança {0} vegades una moneda equilibrada. Probabilitat d'exactament {1} cares?", n, k),
            Mc.Frac(Choose(n, k), total), [Mc.Frac(1, total), Mc.Frac(k, n), Mc.Frac(Choose(n, k), n * n), "1/2", Mc.Frac(Choose(n, k) + 1, total)],
            q.F("C({0}, {1}) = {2} favourable sequences out of 2^{0} = {3}.|C({0}, {1}) = {2} secuencias favorables de 2^{0} = {3}.|C({0}, {1}) = {2} suites favorables sur 2^{0} = {3}.|C({0}, {1}) = {2} seqüències favorables de 2^{0} = {3}.", n, k, Choose(n, k), total));
    }

    private static QuizQuestion Variance(Mc q)
    {
        int[] data;
        do
        {
            data = Enumerable.Range(0, 4).Select(_ => q.Between(1, 12)).ToArray();
        }
        while (data.Sum() % 4 != 0 || data.Distinct().Count() < 2);
        var mean = data.Sum() / 4.0;
        var squares = data.Sum(x => (x - mean) * (x - mean));
        return q.Number("math.statistics", q.F("Population variance of the data {0}?|¿Varianza poblacional de los datos {0}?|Variance (population) des données {0} ?|Variància poblacional de les dades {0}?", string.Join("; ", data)),
            squares / 4, [squares / 3, Math.Sqrt(squares / 4), squares, mean], q.F("Mean = {0}; sum of squared deviations = {1}; divided by n = 4.|Media = {0}; suma de desviaciones al cuadrado = {1}; dividida por n = 4.|Moyenne = {0} ; somme des carrés des écarts = {1} ; divisée par n = 4.|Mitjana = {0}; suma de desviacions al quadrat = {1}; dividida per n = 4.", q.N(mean), q.N(squares)));
    }

    private static QuizQuestion Radius(Mc q)
    {
        var k = q.Between(2, 9);
        var kind = q.Rng.Next(3);
        var (series, answer) = kind switch
        {
            0 => ($"Σ xⁿ / {k}ⁿ", k.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            1 => ($"Σ n · xⁿ / {k}ⁿ", k.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            _ => ("Σ xⁿ / n!", "∞"),
        };
        return q.Choice("math.series", q.F("Radius of convergence of {0}?|¿Radio de convergencia de {0}?|Rayon de convergence de {0} ?|Radi de convergència de {0}?", series), answer,
            [Mc.Frac(1, k), "1", "∞", "0", k.ToString(System.Globalization.CultureInfo.InvariantCulture)],
            q.T("Ratio test: R = lim ∣aₙ / aₙ₊₁∣. Polynomial factors such as n do not change the radius.|Criterio del cociente: R = lím ∣aₙ / aₙ₊₁∣. Los factores polinómicos como n no cambian el radio.|Critère de d'Alembert : R = lim ∣aₙ / aₙ₊₁∣. Les facteurs polynomiaux comme n ne changent pas le rayon.|Criteri del quocient: R = lím ∣aₙ / aₙ₊₁∣. Els factors polinòmics com n no canvien el radi."));
    }

    private static QuizQuestion Taylor(Mc q)
    {
        var a = q.Between(2, 4);
        var n = q.Between(2, 4);
        var factorial = Enumerable.Range(1, n).Aggregate(1L, (x, y) => x * y);
        var power = (long)Math.Pow(a, n);
        return q.Choice("math.series", q.F("Coefficient of x{0} in the Taylor series of e^({1}x) at 0?|¿Coeficiente de x{0} en la serie de Taylor de e^({1}x) en 0?|Coefficient de x{0} dans la série de Taylor de e^({1}x) en 0 ?|Coeficient de x{0} a la sèrie de Taylor de e^({1}x) a 0?", n == 2 ? "²" : n == 3 ? "³" : "⁴", a),
            Mc.Frac(power, factorial), [Mc.Frac(a, factorial), Mc.Frac(power, n), Mc.Frac(1, factorial), Mc.Frac(a * n, factorial), Mc.Frac(power, factorial * 2), Mc.Frac(power * 2, factorial), Mc.Frac(power + 1, factorial)],
            q.F("e^u = Σ uⁿ/n!, with u = {0}x: {0}^{1} / {1}! = {2}.|e^u = Σ uⁿ/n!, con u = {0}x: {0}^{1} / {1}! = {2}.|e^u = Σ uⁿ/n!, avec u = {0}x : {0}^{1} / {1}! = {2}.|e^u = Σ uⁿ/n!, amb u = {0}x: {0}^{1} / {1}! = {2}.", a, n, Mc.Frac(power, factorial)));
    }
}
