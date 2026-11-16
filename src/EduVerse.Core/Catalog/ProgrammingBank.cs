namespace EduVerse.Core.Catalog;

/// <summary>Programming: small Python programs whose output is computed in code, plus algorithms and theory.</summary>
internal static class ProgrammingBank
{
    private static readonly Fact[] FactList =
    [
        Fact.Of(Level.Bachelor, "programming.complexity", "complexity", "Average time to look up a key in a hash table?|¿Tiempo medio de búsqueda de una clave en una tabla hash?|Temps moyen de recherche d'une clé dans une table de hachage ?|Temps mitjà de cerca d'una clau en una taula hash?", "O(1)"),
        Fact.Of(Level.Bachelor, "programming.complexity", "complexity", "Time complexity of merge sort?|¿Complejidad temporal de la ordenación por mezcla (merge sort)?|Complexité du tri fusion ?|Complexitat temporal de l'ordenació per fusió (merge sort)?", "O(n log n)"),
        Fact.Of(Level.Bachelor, "programming.complexity", "complexity", "Worst-case time of quicksort?|¿Tiempo en el peor caso de quicksort?|Temps du tri rapide dans le pire cas ?|Temps en el pitjor cas de quicksort?", "O(n²)"),
        Fact.Of(Level.Bachelor, "programming.complexity", "complexity", "Time of binary search in a sorted array?|¿Tiempo de la búsqueda binaria en un vector ordenado?|Temps de la recherche dichotomique dans un tableau trié ?|Temps de la cerca binària en un vector ordenat?", "O(log n)"),
        Fact.Of(Level.Master, "programming.complexity", "complexity", "Time of Dijkstra's algorithm with a binary heap?|¿Tiempo del algoritmo de Dijkstra con un montículo binario?|Temps de l'algorithme de Dijkstra avec un tas binaire ?|Temps de l'algorisme de Dijkstra amb un munt binari?", "O((V + E) log V)"),
        Fact.Of(Level.Master, "programming.complexity", "complexity", "Time of a breadth-first search on a graph stored as adjacency lists?|¿Tiempo de un recorrido en anchura en un grafo con listas de adyacencia?|Temps d'un parcours en largeur sur un graphe en listes d'adjacence ?|Temps d'un recorregut en amplada en un graf amb llistes d'adjacència?", "O(V + E)"),
        Fact.Of(Level.Doctorate, "programming.theory", "theorem", "Which result says no algorithm decides whether any program halts?|¿Qué resultado dice que ningún algoritmo decide si cualquier programa termina?|Quel résultat dit qu'aucun algorithme ne décide si un programme quelconque s'arrête ?|Quin resultat diu que cap algorisme decideix si qualsevol programa s'atura?", "undecidability of the halting problem (Turing)|indecidibilidad del problema de la parada (Turing)|indécidabilité du problème de l'arrêt (Turing)|indecidibilitat del problema de l'aturada (Turing)"),
        Fact.Of(Level.Doctorate, "programming.theory", "theorem", "Which theorem says every non-trivial semantic property of programs is undecidable?|¿Qué teorema dice que toda propiedad semántica no trivial de los programas es indecidible?|Quel théorème dit que toute propriété sémantique non triviale des programmes est indécidable ?|Quin teorema diu que tota propietat semàntica no trivial dels programes és indecidible?", "Rice's theorem|teorema de Rice|théorème de Rice|teorema de Rice"),
        Fact.Of(Level.Doctorate, "programming.theory", "theorem", "Which theorem proved that SAT is NP-complete?|¿Qué teorema demostró que SAT es NP-completo?|Quel théorème a montré que SAT est NP-complet ?|Quin teorema va demostrar que SAT és NP-complet?", "Cook–Levin theorem|teorema de Cook-Levin|théorème de Cook-Levin|teorema de Cook-Levin"),
        Fact.Of(Level.Doctorate, "programming.theory", "theorem", "Which theorem says a distributed store cannot guarantee consistency, availability and partition tolerance at once?|¿Qué teorema dice que un almacén distribuido no puede garantizar a la vez consistencia, disponibilidad y tolerancia a particiones?|Quel théorème dit qu'un stockage distribué ne peut garantir à la fois cohérence, disponibilité et tolérance au partitionnement ?|Quin teorema diu que un magatzem distribuït no pot garantir alhora consistència, disponibilitat i tolerància a particions?", "CAP theorem|teorema CAP|théorème CAP|teorema CAP"),
    ];

    public static QuizQuestion Generate(Mc q) => q.Level switch
    {
        <= Level.Primary3 => q.Topic(Stars, Repeat)(q),
        Level.Secondary1 => q.Topic(Variables, Stars, Condition)(q),
        Level.Secondary2 => q.Topic(Condition, Loop, Variables)(q),
        Level.HighSchool => q.Topic(Lists, Slicing, Function, Loop)(q),
        Level.Bachelor => q.Topic(Recursion, BinarySearch, f => Facts.Ask(f, FactList))(q),
        Level.Master => q.Topic(f => Facts.Ask(f, FactList), NestedLoops, Recursion)(q),
        _ => q.Topic(f => Facts.Ask(f, FactList), NestedLoops)(q),
    };

    private static string Output(Mc q) => q.T("What does this program print?|¿Qué muestra este programa?|Qu'affiche ce programme ?|Què mostra aquest programa?");

    private static QuizQuestion Stars(Mc q)
    {
        var n = q.Between(2, q.Difficulty == 1 ? 5 : 8);
        return q.Number("programming.loops", q.T("How many stars does this program draw?|¿Cuántas estrellas dibuja este programa?|Combien d'étoiles ce programme dessine-t-il ?|Quantes estrelles dibuixa aquest programa?"), n, [n - 1, n + 1, 1],
            q.F("range({0}) repeats the loop {0} times: 0, 1, … {1}.|range({0}) repite el bucle {0} veces: 0, 1, … {1}.|range({0}) répète la boucle {0} fois : 0, 1, … {1}.|range({0}) repeteix el bucle {0} vegades: 0, 1, … {1}.", n, n - 1),
            code: $"for i in range({n}):\n    print(\"⭐\")");
    }

    private static QuizQuestion Repeat(Mc q)
    {
        var a = q.Between(1, 4);
        var b = a + q.Between(2, 6);
        return q.Number("programming.loops", q.T("How many times is \"hello\" printed?|¿Cuántas veces se muestra \"hola\"?|Combien de fois « bonjour » est-il affiché ?|Quantes vegades es mostra \"hola\"?"), b - a, [b - a + 1, b, a],
            q.F("range({0}, {1}) goes from {0} to {2}: {3} values.|range({0}, {1}) va de {0} a {2}: {3} valores.|range({0}, {1}) va de {0} à {2} : {3} valeurs.|range({0}, {1}) va de {0} a {2}: {3} valors.", a, b, b - 1, b - a),
            code: $"for i in range({a}, {b}):\n    print(\"{q.T("hello|hola|bonjour|hola")}\")");
    }

    private static QuizQuestion Variables(Mc q)
    {
        var a = q.Between(1, 9);
        var b = q.Between(1, 9);
        var c = q.Between(2, 4);
        var result = (a + b) * c;
        return q.Number("programming.variables", Output(q), result, [a + b * c, a * c, a + b],
            q.F("x becomes {0} + {1} = {2}, then x × {3} = {4}.|x pasa a valer {0} + {1} = {2} y luego x × {3} = {4}.|x devient {0} + {1} = {2}, puis x × {3} = {4}.|x passa a valer {0} + {1} = {2} i després x × {3} = {4}.", a, b, a + b, c, result),
            code: $"x = {a}\nx = x + {b}\nprint(x * {c})");
    }

    private static QuizQuestion Condition(Mc q)
    {
        var x = q.Between(1, 20);
        var limit = q.Between(1, 20);
        var answer = x > limit ? "big" : x == limit ? "equal" : "small";
        return q.Choice("programming.conditions", Output(q), answer, ["big", "equal", "small", "x"],
            q.F("{0} > {1} is {2}; {0} == {1} is {3}.|{0} > {1} es {2}; {0} == {1} es {3}.|{0} > {1} vaut {2} ; {0} == {1} vaut {3}.|{0} > {1} és {2}; {0} == {1} és {3}.", x, limit, x > limit ? "True" : "False", x == limit ? "True" : "False"),
            code: $"x = {x}\nif x > {limit}:\n    print(\"big\")\nelif x == {limit}:\n    print(\"equal\")\nelse:\n    print(\"small\")");
    }

    private static QuizQuestion Loop(Mc q)
    {
        var n = q.Between(3, 8);
        var sum = n * (n + 1) / 2;
        return q.Number("programming.loops", Output(q), sum, [sum - n, n, sum + n + 1],
            q.F("total = 1 + 2 + … + {0} = {1}.|total = 1 + 2 + … + {0} = {1}.|total = 1 + 2 + … + {0} = {1}.|total = 1 + 2 + … + {0} = {1}.", n, sum),
            code: $"total = 0\nfor i in range(1, {n + 1}):\n    total = total + i\nprint(total)");
    }

    private static QuizQuestion Lists(Mc q)
    {
        var items = Enumerable.Range(0, 5).Select(_ => q.Between(1, 30)).Distinct().ToList();
        while (items.Count < 5)
        {
            items.Add(items.Max() + 1);
        }

        var i = q.Between(1, 4);
        return q.Number("programming.lists", Output(q), items[i], [items[i - 1], items.Count, items[0]],
            q.F("Indexes start at 0, so nums[{0}] is the element number {1}: {2}.|Los índices empiezan en 0: nums[{0}] es el elemento número {1}: {2}.|Les indices commencent à 0 : nums[{0}] est le {1}ᵉ élément : {2}.|Els índexs comencen a 0: nums[{0}] és l'element número {1}: {2}.", i, i + 1, items[i]),
            code: $"nums = [{string.Join(", ", items)}]\nprint(nums[{i}])");
    }

    private static QuizQuestion Slicing(Mc q)
    {
        var word = q.Pick(["python", "planet", "rocket", "garden", "silver", "orange"]);
        var start = q.Between(0, 2);
        var end = start + q.Between(2, 3);
        var answer = word[start..end];
        return q.Choice("programming.strings", Output(q), answer, [word[(start + 1)..Math.Min(word.Length, end + 1)], word[start..Math.Min(word.Length, end + 1)], word[..end], word[start..]],
            q.F("[{0}:{1}] takes from index {0} up to {1} (not included).|[{0}:{1}] toma desde el índice {0} hasta el {1} (sin incluirlo).|[{0}:{1}] prend de l'indice {0} jusqu'à {1} (exclu).|[{0}:{1}] agafa des de l'índex {0} fins al {1} (sense incloure'l).", start, end),
            code: $"word = \"{word}\"\nprint(word[{start}:{end}])");
    }

    private static QuizQuestion Function(Mc q)
    {
        var a = q.Between(2, 3);
        var b = q.Between(1, 5);
        var x = q.Between(1, 4);
        var once = a * x + b;
        var twice = a * once + b;
        return q.Number("programming.functions", Output(q), twice, [once, a * x * a + b, twice + b],
            q.F("f({0}) = {1}, then f({1}) = {2}.|f({0}) = {1}, luego f({1}) = {2}.|f({0}) = {1}, puis f({1}) = {2}.|f({0}) = {1}, després f({1}) = {2}.", x, once, twice),
            code: $"def f(x):\n    return {a} * x + {b}\n\nprint(f(f({x})))");
    }

    private static QuizQuestion Recursion(Mc q)
    {
        var n = q.Between(4, 9);
        long Fib(int k) => k < 2 ? k : Fib(k - 1) + Fib(k - 2);
        return q.Number("programming.recursion", Output(q), Fib(n), [Fib(n - 1), Fib(n + 1), n],
            q.F("fib: 0, 1, 1, 2, 3, 5, 8, 13, 21, 34… fib({0}) = {1}.|fib: 0, 1, 1, 2, 3, 5, 8, 13, 21, 34… fib({0}) = {1}.|fib : 0, 1, 1, 2, 3, 5, 8, 13, 21, 34… fib({0}) = {1}.|fib: 0, 1, 1, 2, 3, 5, 8, 13, 21, 34… fib({0}) = {1}.", n, Fib(n)),
            code: $"def fib(n):\n    if n < 2:\n        return n\n    return fib(n - 1) + fib(n - 2)\n\nprint(fib({n}))");
    }

    private static QuizQuestion BinarySearch(Mc q)
    {
        var size = q.Pick([7, 15, 31, 63, 100, 1000, 1_000_000]);
        var steps = (int)Math.Ceiling(Math.Log2(size + 1));
        return q.Number("programming.algorithms", q.F("Maximum number of comparisons of a binary search in a sorted list of {0} elements?|¿Número máximo de comparaciones de una búsqueda binaria en una lista ordenada de {0} elementos?|Nombre maximal de comparaisons d'une recherche dichotomique dans une liste triée de {0} éléments ?|Nombre màxim de comparacions d'una cerca binària en una llista ordenada de {0} elements?", size),
            steps, [size / 2, steps + 3, Math.Sqrt(size)], q.F("Each comparison halves the list: ⌈log₂({0} + 1)⌉ = {1}.|Cada comparación divide la lista por dos: ⌈log₂({0} + 1)⌉ = {1}.|Chaque comparaison divise la liste par deux : ⌈log₂({0} + 1)⌉ = {1}.|Cada comparació divideix la llista per dos: ⌈log₂({0} + 1)⌉ = {1}.", size, steps), decimals: 0);
    }

    private static QuizQuestion NestedLoops(Mc q)
    {
        var halving = q.Rng.Next(2) == 0;
        var answer = halving ? "O(n log n)" : "O(n²)";
        var code = halving
            ? "for i in range(n):\n    j = n\n    while j > 1:\n        j = j // 2"
            : "for i in range(n):\n    for j in range(i, n):\n        count += 1";
        return q.Choice("programming.complexity", q.T("Time complexity of this code?|¿Complejidad temporal de este código?|Complexité de ce code ?|Complexitat temporal d'aquest codi?"), answer, ["O(n)", "O(n log n)", "O(n²)", "O(log n)", "O(2ⁿ)"],
            q.T(halving ? "n iterations of the outer loop × log₂ n halvings.|n iteraciones del bucle exterior × log₂ n divisiones por dos.|n itérations de la boucle externe × log₂ n divisions par deux.|n iteracions del bucle exterior × log₂ n divisions per dos." : "n + (n − 1) + … + 1 = n(n + 1)/2 steps.|n + (n − 1) + … + 1 = n(n + 1)/2 pasos.|n + (n − 1) + … + 1 = n(n + 1)/2 étapes.|n + (n − 1) + … + 1 = n(n + 1)/2 passos."), code: code);
    }
}
