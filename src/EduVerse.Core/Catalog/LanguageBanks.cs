using EduVerse.Core.Common;

namespace EduVerse.Core.Catalog;

/// <summary>The student's own language (the language of the interface): letters, word classes, opposites, figures of speech, literature, linguistics.</summary>
internal static class LanguageBank
{
    private sealed record Data(
        (string Emoji, string Word)[] Pictures,
        (string Word, int Class)[] Words,
        (string Word, string Opposite)[] Opposites,
        (string Sentence, int Device)[] Devices,
        (Level Level, string Question, string Answer, string Group)[] Literature);

    private static readonly string[] Classes = ["noun|sustantivo|nom|nom", "verb|verbo|verbe|verb", "adjective|adjetivo|adjectif|adjectiu", "adverb|adverbio|adverbe|adverbi"];

    private static readonly string[] DeviceNames = ["metaphor|metáfora|métaphore|metàfora", "simile|comparación (símil)|comparaison|comparació", "personification|personificación|personnification|personificació", "hyperbole|hipérbole|hyperbole|hipèrbole", "alliteration|aliteración|allitération|al·literació"];

    private static readonly Data English = new(
        [("🍎", "apple"), ("🐶", "dog"), ("🐱", "cat"), ("☀️", "sun"), ("🌙", "moon"), ("🐟", "fish"), ("🏠", "house"), ("🚗", "car"), ("🌳", "tree"), ("🎈", "balloon"), ("🐸", "frog"), ("🍌", "banana")],
        [("table", 0), ("happiness", 0), ("river", 0), ("teacher", 0), ("run", 1), ("write", 1), ("think", 1), ("swim", 1), ("blue", 2), ("tall", 2), ("brave", 2), ("noisy", 2), ("quickly", 3), ("always", 3), ("here", 3), ("carefully", 3)],
        [("hot", "cold"), ("big", "small"), ("happy", "sad"), ("fast", "slow"), ("open", "closed"), ("early", "late"), ("light", "dark"), ("full", "empty"), ("strong", "weak"), ("young", "old")],
        [("Time is a thief.", 0), ("The classroom was a zoo.", 0), ("She is as brave as a lion.", 1), ("He ran like the wind.", 1), ("The wind whispered through the trees.", 2), ("The old car coughed and died.", 2), ("I've told you a million times.", 3), ("This bag weighs a ton.", 3), ("Peter Piper picked a peck of pickled peppers.", 4), ("Sally sells seashells by the seashore.", 4)],
        [
            (Level.HighSchool, "Who wrote \"Romeo and Juliet\"?", "William Shakespeare", "author"), (Level.HighSchool, "Who wrote \"Pride and Prejudice\"?", "Jane Austen", "author"),
            (Level.HighSchool, "Who wrote \"Oliver Twist\"?", "Charles Dickens", "author"), (Level.HighSchool, "Who wrote \"Frankenstein\"?", "Mary Shelley", "author"),
            (Level.Bachelor, "Who wrote \"Ulysses\" (1922)?", "James Joyce", "author"), (Level.Bachelor, "Who wrote \"Mrs Dalloway\"?", "Virginia Woolf", "author"),
            (Level.HighSchool, "Wordsworth and Coleridge belong to which movement?", "Romanticism", "movement"), (Level.Bachelor, "T. S. Eliot and Virginia Woolf belong to which movement?", "Modernism", "movement"),
            (Level.HighSchool, "Shakespeare and Marlowe wrote during which period?", "the Elizabethan era", "movement"), (Level.Master, "Pynchon and Rushdie are associated with which movement?", "Postmodernism", "movement"),
        ]);

    private static readonly Data Spanish = new(
        [("🍎", "manzana"), ("🐶", "perro"), ("🐱", "gato"), ("☀️", "sol"), ("🌙", "luna"), ("🐟", "pez"), ("🏠", "casa"), ("🚗", "coche"), ("🌳", "árbol"), ("🎈", "globo"), ("🐸", "rana"), ("🍌", "plátano")],
        [("mesa", 0), ("alegría", 0), ("río", 0), ("maestra", 0), ("correr", 1), ("escribir", 1), ("pensar", 1), ("nadar", 1), ("azul", 2), ("alto", 2), ("valiente", 2), ("ruidoso", 2), ("rápidamente", 3), ("siempre", 3), ("aquí", 3), ("despacio", 3)],
        [("caliente", "frío"), ("grande", "pequeño"), ("alegre", "triste"), ("rápido", "lento"), ("abierto", "cerrado"), ("temprano", "tarde"), ("claro", "oscuro"), ("lleno", "vacío"), ("fuerte", "débil"), ("joven", "viejo")],
        [("El tiempo es oro.", 0), ("Tus ojos son dos luceros.", 0), ("Es valiente como un león.", 1), ("Corría como el viento.", 1), ("El viento susurraba entre los árboles.", 2), ("La ciudad dormía bajo la lluvia.", 2), ("Te lo he dicho un millón de veces.", 3), ("Me muero de hambre.", 3), ("Tres tristes tigres tragaban trigo en un trigal.", 4), ("Con el ala aleve del leve abanico.", 4)],
        [
            (Level.HighSchool, "¿Quién escribió «Don Quijote de la Mancha»?", "Miguel de Cervantes", "author"), (Level.HighSchool, "¿Quién escribió «Rimas y leyendas»?", "Gustavo Adolfo Bécquer", "author"),
            (Level.HighSchool, "¿Quién escribió «La casa de Bernarda Alba»?", "Federico García Lorca", "author"), (Level.HighSchool, "¿Quién escribió «Cien años de soledad»?", "Gabriel García Márquez", "author"),
            (Level.Bachelor, "¿Quién escribió «Luces de bohemia»?", "Ramón María del Valle-Inclán", "author"), (Level.Bachelor, "¿Quién escribió «Rayuela»?", "Julio Cortázar", "author"),
            (Level.HighSchool, "¿A qué movimiento pertenece Bécquer?", "Romanticismo", "movement"), (Level.HighSchool, "¿A qué movimiento pertenece Rubén Darío?", "Modernismo", "movement"),
            (Level.Bachelor, "¿A qué generación pertenecen Lorca y Alberti?", "Generación del 27", "movement"), (Level.Master, "¿A qué corriente se asocia «Pedro Páramo» de Juan Rulfo?", "realismo mágico", "movement"),
        ]);

    private static readonly Data French = new(
        [("🍎", "pomme"), ("🐶", "chien"), ("🐱", "chat"), ("☀️", "soleil"), ("🌙", "lune"), ("🐟", "poisson"), ("🏠", "maison"), ("🚗", "voiture"), ("🌳", "arbre"), ("🎈", "ballon"), ("🐸", "grenouille"), ("🍌", "banane")],
        [("table", 0), ("bonheur", 0), ("rivière", 0), ("maîtresse", 0), ("courir", 1), ("écrire", 1), ("penser", 1), ("nager", 1), ("bleu", 2), ("grand", 2), ("courageux", 2), ("bruyant", 2), ("rapidement", 3), ("toujours", 3), ("ici", 3), ("lentement", 3)],
        [("chaud", "froid"), ("grand", "petit"), ("joyeux", "triste"), ("rapide", "lent"), ("ouvert", "fermé"), ("tôt", "tard"), ("clair", "sombre"), ("plein", "vide"), ("fort", "faible"), ("jeune", "vieux")],
        [("Cet homme est un roc.", 0), ("La vie est un long fleuve tranquille.", 0), ("Il est courageux comme un lion.", 1), ("Elle court comme le vent.", 1), ("Le vent murmurait dans les arbres.", 2), ("La ville dormait sous la pluie.", 2), ("Je te l'ai dit mille fois.", 3), ("Je meurs de faim.", 3), ("Pour qui sont ces serpents qui sifflent sur vos têtes ?", 4), ("Un chasseur sachant chasser sans son chien.", 4)],
        [
            (Level.HighSchool, "Qui a écrit « Les Misérables » ?", "Victor Hugo", "author"), (Level.HighSchool, "Qui a écrit « Le Cid » ?", "Pierre Corneille", "author"),
            (Level.HighSchool, "Qui a écrit « Madame Bovary » ?", "Gustave Flaubert", "author"), (Level.HighSchool, "Qui a écrit « Les Fleurs du mal » ?", "Charles Baudelaire", "author"),
            (Level.Bachelor, "Qui a écrit « À la recherche du temps perdu » ?", "Marcel Proust", "author"), (Level.Bachelor, "Qui a écrit « L'Étranger » ?", "Albert Camus", "author"),
            (Level.HighSchool, "À quel mouvement appartient Victor Hugo ?", "romantisme", "movement"), (Level.HighSchool, "À quel mouvement appartiennent Zola et Maupassant ?", "naturalisme", "movement"),
            (Level.Bachelor, "À quel mouvement appartient André Breton ?", "surréalisme", "movement"), (Level.Master, "Robbe-Grillet et Nathalie Sarraute sont associés à quel courant ?", "Nouveau Roman", "movement"),
        ]);

    private static readonly Data Catalan = new(
        [("🍎", "poma"), ("🐶", "gos"), ("🐱", "gat"), ("☀️", "sol"), ("🌙", "lluna"), ("🐟", "peix"), ("🏠", "casa"), ("🚗", "cotxe"), ("🌳", "arbre"), ("🎈", "globus"), ("🐸", "granota"), ("🍌", "plàtan")],
        [("taula", 0), ("alegria", 0), ("riu", 0), ("mestra", 0), ("córrer", 1), ("escriure", 1), ("pensar", 1), ("nedar", 1), ("blau", 2), ("alt", 2), ("valent", 2), ("sorollós", 2), ("ràpidament", 3), ("sempre", 3), ("aquí", 3), ("a poc a poc", 3)],
        [("calent", "fred"), ("gran", "petit"), ("alegre", "trist"), ("ràpid", "lent"), ("obert", "tancat"), ("d'hora", "tard"), ("clar", "fosc"), ("ple", "buit"), ("fort", "feble"), ("jove", "vell")],
        [("El temps és or.", 0), ("La classe era un galliner.", 0), ("És valent com un lleó.", 1), ("Corria com el vent.", 1), ("El vent xiuxiuejava entre els arbres.", 2), ("La ciutat dormia sota la pluja.", 2), ("T'ho he dit un milió de vegades.", 3), ("Em moro de gana.", 3), ("Setze jutges d'un jutjat mengen fetge d'un penjat.", 4), ("Plou poc, però pel poc que plou, plou prou.", 4)],
        [
            (Level.HighSchool, "Qui va escriure «Canigó»?", "Jacint Verdaguer", "author"), (Level.HighSchool, "Qui va escriure «La plaça del Diamant»?", "Mercè Rodoreda", "author"),
            (Level.HighSchool, "Qui va escriure «Terra baixa»?", "Àngel Guimerà", "author"), (Level.HighSchool, "Qui va escriure «Tirant lo Blanc»?", "Joanot Martorell", "author"),
            (Level.Bachelor, "Qui va escriure «Incerta glòria»?", "Joan Sales", "author"), (Level.Bachelor, "Qui va escriure «Solitud»?", "Víctor Català", "author"),
            (Level.HighSchool, "A quin moviment pertany Jacint Verdaguer?", "Renaixença", "movement"), (Level.HighSchool, "A quin moviment pertany Eugeni d'Ors?", "Noucentisme", "movement"),
            (Level.Bachelor, "A quin moviment pertany Santiago Rusiñol?", "Modernisme", "movement"), (Level.Master, "Amb quin corrent s'associa J. V. Foix?", "avantguardes (surrealisme)", "movement"),
        ]);

    private static readonly Fact[] Linguistics =
    [
        Fact.Of(Level.Bachelor, "language.linguistics", "concept", "Smallest unit of sound that distinguishes meaning?|¿Unidad mínima de sonido que distingue significados?|Plus petite unité sonore qui distingue des sens ?|Unitat mínima de so que distingeix significats?", "phoneme|fonema|phonème|fonema"),
        Fact.Of(Level.Bachelor, "language.linguistics", "concept", "Smallest unit of language that carries meaning?|¿Unidad mínima de la lengua con significado?|Plus petite unité de la langue porteuse de sens ?|Unitat mínima de la llengua amb significat?", "morpheme|morfema|morphème|morfema"),
        Fact.Of(Level.Bachelor, "language.linguistics", "concept", "Study of meaning in context and of the speaker's intention?|¿Estudio del significado en contexto y de la intención del hablante?|Étude du sens en contexte et de l'intention du locuteur ?|Estudi del significat en context i de la intenció del parlant?", "pragmatics|pragmática|pragmatique|pragmàtica"),
        Fact.Of(Level.Bachelor, "language.linguistics", "concept", "Study of how words combine into sentences?|¿Estudio de cómo se combinan las palabras en oraciones?|Étude de la combinaison des mots en phrases ?|Estudi de com es combinen les paraules en oracions?", "syntax|sintaxis|syntaxe|sintaxi"),
        Fact.Of(Level.Master, "language.linguistics", "linguist", "Who distinguished the signifier from the signified?|¿Quién distinguió significante y significado?|Qui a distingué signifiant et signifié ?|Qui va distingir significant i significat?", "Ferdinand de Saussure"),
        Fact.Of(Level.Master, "language.linguistics", "linguist", "Who proposed generative grammar?|¿Quién propuso la gramática generativa?|Qui a proposé la grammaire générative ?|Qui va proposar la gramàtica generativa?", "Noam Chomsky"),
        Fact.Of(Level.Master, "language.linguistics", "linguist", "Who described the six functions of language?|¿Quién describió las seis funciones del lenguaje?|Qui a décrit les six fonctions du langage ?|Qui va descriure les sis funcions del llenguatge?", "Roman Jakobson"),
        Fact.Of(Level.Doctorate, "language.linguistics", "linguist", "Who developed speech act theory in \"How to Do Things with Words\"?|¿Quién desarrolló la teoría de los actos de habla en «Cómo hacer cosas con palabras»?|Qui a développé la théorie des actes de langage dans « Quand dire, c'est faire » ?|Qui va desenvolupar la teoria dels actes de parla a «Com fer coses amb paraules»?", "J. L. Austin"),
        Fact.Of(Level.Doctorate, "language.linguistics", "linguist", "Who formulated the cooperative principle and conversational maxims?|¿Quién formuló el principio de cooperación y las máximas conversacionales?|Qui a formulé le principe de coopération et les maximes conversationnelles ?|Qui va formular el principi de cooperació i les màximes conversacionals?", "Paul Grice"),
    ];

    private static Data For(TeachLanguage language) => language switch
    {
        TeachLanguage.Spanish => Spanish,
        TeachLanguage.French => French,
        TeachLanguage.Catalan => Catalan,
        _ => English,
    };

    public static QuizQuestion Generate(Mc q) => q.Level switch
    {
        Level.EarlyYears => FirstLetter(q),
        Level.Primary1 => q.Topic(FirstLetter, Opposite, WordClass)(q),
        Level.Primary2 or Level.Primary3 => q.Topic(Opposite, WordClass)(q),
        Level.Secondary1 or Level.Secondary2 => q.Topic(WordClass, Device)(q),
        Level.HighSchool => q.Topic(Device, Literature)(q),
        _ => q.Topic(Literature, f => Facts.Ask(f, Linguistics))(q),
    };

    private static QuizQuestion FirstLetter(Mc q)
    {
        var data = For(q.Language);
        var (emoji, word) = q.Pick(data.Pictures);
        var letter = word[..1].ToUpper(Levels.Culture(q.Language));
        return q.Choice("language.letters", q.T("With which letter does its name begin?|¿Con qué letra empieza su nombre?|Par quelle lettre commence son nom ?|Amb quina lletra comença el seu nom?"),
            letter, data.Pictures.Select(p => p.Word[..1].ToUpper(Levels.Culture(q.Language))).Concat(["M", "R", "T", "L", "E"]),
            q.F("{0} begins with {1}.|«{0}» empieza por {1}.|« {0} » commence par {1}.|«{0}» comença per {1}.", word, letter), visual: emoji);
    }

    private static QuizQuestion Opposite(Mc q)
    {
        var data = For(q.Language);
        var (word, opposite) = q.Pick(data.Opposites);
        return q.Choice("language.vocabulary", q.F("What is the opposite of \"{0}\"?|¿Cuál es el contrario de «{0}»?|Quel est le contraire de « {0} » ?|Quin és el contrari de «{0}»?", word),
            opposite, data.Opposites.Select(o => o.Opposite).Where(o => o != opposite).Append(word),
            q.F("\"{0}\" and \"{1}\" are antonyms.|«{0}» y «{1}» son antónimos.|« {0} » et « {1} » sont des antonymes.|«{0}» i «{1}» són antònims.", word, opposite));
    }

    private static QuizQuestion WordClass(Mc q)
    {
        var (word, wordClass) = q.Pick(For(q.Language).Words.Where(w => q.Difficulty > 1 || w.Class < 2).ToArray());
        var names = Classes.Select(q.T).ToList();
        return q.Choice("language.grammar", q.F("What kind of word is \"{0}\"?|¿Qué clase de palabra es «{0}»?|Quelle est la nature du mot « {0} » ?|Quina classe de paraula és «{0}»?", word),
            names[wordClass], names, q.F("\"{0}\" is a {1}.|«{0}» es un {1}.|« {0} » est un {1}.|«{0}» és un {1}.", word, names[wordClass]));
    }

    private static QuizQuestion Device(Mc q)
    {
        var (sentence, device) = q.Pick(For(q.Language).Devices);
        var names = DeviceNames.Select(q.T).ToList();
        return q.Choice("language.literature", q.F("Which figure of speech is used in: \"{0}\"?|¿Qué figura retórica aparece en: «{0}»?|Quelle figure de style trouve-t-on dans : « {0} » ?|Quina figura retòrica apareix a: «{0}»?", sentence),
            names[device], names, q.T(device switch
            {
                0 => "A metaphor says one thing is another, without \"like\" or \"as\".|La metáfora identifica una cosa con otra sin «como».|La métaphore assimile une chose à une autre sans outil de comparaison.|La metàfora identifica una cosa amb una altra sense «com».",
                1 => "A simile compares with \"like\" or \"as\".|La comparación usa «como».|La comparaison utilise « comme ».|La comparació fa servir «com».",
                2 => "Personification gives human actions to things or animals.|La personificación atribuye acciones humanas a cosas o animales.|La personnification prête des actions humaines aux choses ou aux animaux.|La personificació atribueix accions humanes a coses o animals.",
                3 => "A hyperbole exaggerates on purpose.|La hipérbole exagera a propósito.|L'hyperbole exagère volontairement.|La hipèrbole exagera expressament.",
                _ => "Alliteration repeats the same sounds.|La aliteración repite los mismos sonidos.|L'allitération répète les mêmes sons consonantiques.|L'al·literació repeteix els mateixos sons.",
            }));
    }

    private static QuizQuestion Literature(Mc q)
    {
        var items = For(q.Language).Literature;
        var near = items.Where(i => i.Level <= q.Level).ToArray();
        var (_, question, answer, group) = q.Pick(near.Length > 0 ? near : items);
        return q.Choice("language.literature", question, answer, items.Where(i => i.Group == group).Select(i => i.Answer),
            q.F("The right answer is: {0}.|La respuesta correcta es: {0}.|La bonne réponse est : {0}.|La resposta correcta és: {0}.", answer));
    }
}

/// <summary>A foreign language: English, or Spanish when the interface is in English. Vocabulary, sentences, verbs and idioms.</summary>
internal static class ForeignLanguageBank
{
    private static readonly (string Emoji, Text Word)[] Words =
    [
        ("🐶", Text.Of("dog|perro|chien|gos")), ("🐱", Text.Of("cat|gato|chat|gat")), ("🍎", Text.Of("apple|manzana|pomme|poma")), ("🏠", Text.Of("house|casa|maison|casa")),
        ("☀️", Text.Of("sun|sol|soleil|sol")), ("🌙", Text.Of("moon|luna|lune|lluna")), ("📖", Text.Of("book|libro|livre|llibre")), ("🚗", Text.Of("car|coche|voiture|cotxe")),
        ("🐟", Text.Of("fish|pez|poisson|peix")), ("🌳", Text.Of("tree|árbol|arbre|arbre")), ("💧", Text.Of("water|agua|eau|aigua")), ("🍞", Text.Of("bread|pan|pain|pa")),
        ("🐦", Text.Of("bird|pájaro|oiseau|ocell")), ("🌸", Text.Of("flower|flor|fleur|flor")), ("👟", Text.Of("shoe|zapato|chaussure|sabata")), ("🔴", Text.Of("red|rojo|rouge|vermell")),
        ("🟢", Text.Of("green|verde|vert|verd")), ("🔵", Text.Of("blue|azul|bleu|blau")), ("🏫", Text.Of("school|escuela|école|escola")), ("🧀", Text.Of("cheese|queso|fromage|formatge")),
    ];

    private static readonly Text[] Sentences =
    [
        Text.Of("Good morning!|¡Buenos días!|Bonjour !|Bon dia!"), Text.Of("How are you?|¿Cómo estás?|Comment ça va ?|Com estàs?"),
        Text.Of("I am hungry.|Tengo hambre.|J'ai faim.|Tinc gana."), Text.Of("Where is the station?|¿Dónde está la estación?|Où est la gare ?|On és l'estació?"),
        Text.Of("I like reading.|Me gusta leer.|J'aime lire.|M'agrada llegir."), Text.Of("It is raining.|Está lloviendo.|Il pleut.|Està plovent."),
        Text.Of("See you tomorrow.|Hasta mañana.|À demain.|Fins demà."), Text.Of("Thank you very much.|Muchas gracias.|Merci beaucoup.|Moltes gràcies."),
        Text.Of("I don't understand.|No entiendo.|Je ne comprends pas.|No ho entenc."), Text.Of("What time is it?|¿Qué hora es?|Quelle heure est-il ?|Quina hora és?"),
        Text.Of("Can you help me?|¿Puedes ayudarme?|Peux-tu m'aider ?|Em pots ajudar?"), Text.Of("I have two brothers.|Tengo dos hermanos.|J'ai deux frères.|Tinc dos germans."),
    ];

    private static readonly (string Base, string Form)[] EnglishVerbs =
    [
        ("go", "went"), ("see", "saw"), ("eat", "ate"), ("take", "took"), ("write", "wrote"), ("buy", "bought"), ("think", "thought"), ("come", "came"),
        ("drink", "drank"), ("speak", "spoke"), ("bring", "brought"), ("know", "knew"),
    ];

    private static readonly (string Base, string Form)[] SpanishVerbs =
    [
        ("tener", "tengo"), ("ir", "voy"), ("ser", "soy"), ("hacer", "hago"), ("decir", "digo"), ("poder", "puedo"), ("querer", "quiero"), ("saber", "sé"),
        ("venir", "vengo"), ("salir", "salgo"), ("poner", "pongo"), ("conocer", "conozco"),
    ];

    private static readonly (string Idiom, Text Meaning)[] EnglishIdioms =
    [
        ("to break the ice", Text.Of("to make people feel relaxed|romper el hielo, hacer que la gente se relaje|briser la glace, mettre les gens à l'aise|trencar el gel, fer que la gent es relaxi")),
        ("a piece of cake", Text.Of("something very easy|algo muy fácil (pan comido)|quelque chose de très facile|una cosa molt fàcil (bufar i fer ampolles)")),
        ("to cost an arm and a leg", Text.Of("to be very expensive|costar un ojo de la cara|coûter les yeux de la tête|costar un ull de la cara")),
        ("to hit the books", Text.Of("to study hard|ponerse a estudiar en serio|se mettre à étudier sérieusement|posar-se a estudiar de debò")),
        ("under the weather", Text.Of("a bit ill|un poco enfermo, pachucho|un peu malade, patraque|una mica malalt")),
        ("to let the cat out of the bag", Text.Of("to reveal a secret|revelar un secreto|révéler un secret|revelar un secret")),
        ("to pull someone's leg", Text.Of("to joke with someone|tomar el pelo a alguien|faire marcher quelqu'un|prendre el pèl a algú")),
        ("once in a blue moon", Text.Of("very rarely|muy de vez en cuando|très rarement|molt de tant en tant")),
    ];

    private static readonly (string Idiom, string Meaning)[] SpanishIdioms =
    [
        ("tomar el pelo", "to pull someone's leg"), ("estar en las nubes", "to be daydreaming"), ("costar un ojo de la cara", "to be very expensive"),
        ("ser pan comido", "to be very easy"), ("meter la pata", "to make a blunder"), ("estar como una cabra", "to be a bit crazy"),
        ("no tener pelos en la lengua", "to speak one's mind"), ("echar una mano", "to give someone a hand"),
    ];

    public static TeachLanguage Target(TeachLanguage interfaceLanguage) => interfaceLanguage == TeachLanguage.English ? TeachLanguage.Spanish : TeachLanguage.English;

    public static QuizQuestion Generate(Mc q) => q.Level switch
    {
        <= Level.Primary1 => Picture(q),
        Level.Primary2 or Level.Primary3 => q.Topic(Picture, Translate)(q),
        Level.Secondary1 => q.Topic(Translate, Sentence, Verb)(q),
        Level.Secondary2 or Level.HighSchool => q.Topic(Sentence, Verb, Idiom)(q),
        _ => q.Topic(Verb, Idiom)(q),
    };

    private static string TargetName(Mc q) =>
        q.T(Target(q.Language) == TeachLanguage.English ? "English|inglés|anglais|anglès" : "Spanish|español|espagnol|castellà");

    private static QuizQuestion Picture(Mc q)
    {
        var target = Target(q.Language);
        var (emoji, word) = q.Pick(Words);
        return q.Choice("foreign.vocabulary", q.F("How do you say it in {0}?|¿Cómo se dice en {0}?|Comment le dit-on en {0} ?|Com es diu en {0}?", TargetName(q)),
            word.In(target), Words.Select(w => w.Word.In(target)), q.F("{0} → {1}.|{0} → {1}.|{0} → {1}.|{0} → {1}.", emoji, word.In(target)), visual: emoji);
    }

    private static QuizQuestion Translate(Mc q)
    {
        var target = Target(q.Language);
        var (_, word) = q.Pick(Words);
        return q.Choice("foreign.vocabulary", q.F("How do you say \"{0}\" in {1}?|¿Cómo se dice «{0}» en {1}?|Comment dit-on « {0} » en {1} ?|Com es diu «{0}» en {1}?", word.In(q.Language), TargetName(q)),
            word.In(target), Words.Select(w => w.Word.In(target)), q.F("\"{0}\" = \"{1}\".|«{0}» = «{1}».|« {0} » = « {1} ».|«{0}» = «{1}».", word.In(q.Language), word.In(target)));
    }

    private static QuizQuestion Sentence(Mc q)
    {
        var target = Target(q.Language);
        var sentence = q.Pick(Sentences);
        return q.Choice("foreign.sentences", q.F("Translate into {0}: \"{1}\"|Traduce al {0}: «{1}»|Traduis en {0} : « {1} »|Tradueix al {0}: «{1}»", TargetName(q), sentence.In(q.Language)),
            sentence.In(target), Sentences.Select(s => s.In(target)), q.F("\"{0}\" → \"{1}\".|«{0}» → «{1}».|« {0} » → « {1} ».|«{0}» → «{1}».", sentence.In(q.Language), sentence.In(target)));
    }

    private static QuizQuestion Verb(Mc q)
    {
        if (Target(q.Language) == TeachLanguage.English)
        {
            var (verb, past) = q.Pick(EnglishVerbs);
            return q.Choice("foreign.grammar", q.F("Past simple of \"to {0}\"?|¿Pasado simple de «to {0}»?|Prétérit de « to {0} » ?|Passat simple de «to {0}»?", verb),
                past, EnglishVerbs.Select(v => v.Form).Append(verb + "ed"), q.F("\"{0}\" is irregular: {0} → {1}.|«{0}» es irregular: {0} → {1}.|« {0} » est irrégulier : {0} → {1}.|«{0}» és irregular: {0} → {1}.", verb, past));
        }

        var (infinitive, form) = q.Pick(SpanishVerbs);
        return q.Choice("foreign.grammar", $"Present tense, first person: \"yo ___\" ({infinitive})", form,
            SpanishVerbs.Select(v => v.Form).Append(infinitive[..^2] + "o"), $"\"{infinitive}\" is irregular in the first person: yo {form}.");
    }

    private static QuizQuestion Idiom(Mc q)
    {
        if (Target(q.Language) == TeachLanguage.English)
        {
            var (idiom, meaning) = q.Pick(EnglishIdioms);
            return q.Choice("foreign.idioms", q.F("What does \"{0}\" mean?|¿Qué significa «{0}»?|Que signifie « {0} » ?|Què vol dir «{0}»?", idiom),
                meaning.In(q.Language), EnglishIdioms.Select(i => i.Meaning.In(q.Language)), q.F("\"{0}\": {1}.|«{0}»: {1}.|« {0} » : {1}.|«{0}»: {1}.", idiom, meaning.In(q.Language)));
        }

        var (spanish, english) = q.Pick(SpanishIdioms);
        return q.Choice("foreign.idioms", $"What does \"{spanish}\" mean?", english, SpanishIdioms.Select(i => i.Meaning), $"\"{spanish}\": {english}.");
    }
}
