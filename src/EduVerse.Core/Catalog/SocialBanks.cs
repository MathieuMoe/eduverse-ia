namespace EduVerse.Core.Catalog;

/// <summary>History: dated events (years, centuries, chronology computed in code), people and historians.</summary>
internal static class HistoryBank
{
    private static readonly (int Year, Level Level, Text Event)[] Events =
    [
        (1969, Level.Primary2, Text.Of("the first people walk on the Moon|el primer ser humano pisa la Luna|les premiers humains marchent sur la Lune|els primers humans trepitgen la Lluna")),
        (1492, Level.Primary2, Text.Of("Columbus reaches America|Colón llega a América|Christophe Colomb atteint l'Amérique|Colom arriba a Amèrica")),
        (1789, Level.Primary2, Text.Of("the French Revolution begins|empieza la Revolución francesa|la Révolution française commence|comença la Revolució francesa")),
        (1989, Level.Primary2, Text.Of("the Berlin Wall falls|cae el muro de Berlín|le mur de Berlin tombe|cau el mur de Berlín")),
        (1945, Level.Primary2, Text.Of("the Second World War ends|termina la Segunda Guerra Mundial|la Seconde Guerre mondiale se termine|acaba la Segona Guerra Mundial")),
        (476, Level.Primary3, Text.Of("the Western Roman Empire falls|cae el Imperio romano de Occidente|l'Empire romain d'Occident tombe|cau l'Imperi romà d'Occident")),
        (1450, Level.Primary3, Text.Of("Gutenberg develops the printing press (about)|Gutenberg desarrolla la imprenta (aprox.)|Gutenberg met au point l'imprimerie (environ)|Gutenberg desenvolupa la impremta (aprox.)")),
        (1914, Level.Primary3, Text.Of("the First World War begins|empieza la Primera Guerra Mundial|la Première Guerre mondiale commence|comença la Primera Guerra Mundial")),
        (1978, Level.Primary3, Text.Of("the current Spanish Constitution is approved|se aprueba la Constitución española actual|la Constitution espagnole actuelle est adoptée|s'aprova l'actual Constitució espanyola")),
        (1776, Level.Primary3, Text.Of("the United States declares independence|Estados Unidos declara su independencia|les États-Unis déclarent leur indépendance|els Estats Units declaren la independència")),
        (1066, Level.Secondary1, Text.Of("the Battle of Hastings|la batalla de Hastings|la bataille d'Hastings|la batalla de Hastings")),
        (1215, Level.Secondary1, Text.Of("the Magna Carta is sealed|se sella la Carta Magna|la Grande Charte est scellée|se segella la Carta Magna")),
        (1453, Level.Secondary1, Text.Of("Constantinople falls to the Ottomans|Constantinopla cae ante los otomanos|Constantinople tombe aux mains des Ottomans|Constantinoble cau en mans dels otomans")),
        (1917, Level.Secondary1, Text.Of("the Russian Revolution|la Revolución rusa|la révolution russe|la Revolució russa")),
        (1957, Level.Secondary1, Text.Of("the Treaty of Rome creates the EEC|el Tratado de Roma crea la CEE|le traité de Rome crée la CEE|el Tractat de Roma crea la CEE")),
        (1648, Level.Secondary2, Text.Of("the Peace of Westphalia|la Paz de Westfalia|les traités de Westphalie|la Pau de Westfàlia")),
        (1815, Level.Secondary2, Text.Of("the Congress of Vienna ends|termina el Congreso de Viena|le congrès de Vienne s'achève|acaba el Congrés de Viena")),
        (1919, Level.Secondary2, Text.Of("the Treaty of Versailles is signed|se firma el Tratado de Versalles|le traité de Versailles est signé|se signa el Tractat de Versalles")),
        (1936, Level.Secondary2, Text.Of("the Spanish Civil War begins|empieza la Guerra Civil española|la guerre d'Espagne commence|comença la Guerra Civil espanyola")),
        (2002, Level.Secondary2, Text.Of("euro notes and coins come into circulation|entran en circulación los billetes y monedas de euro|les billets et pièces en euros entrent en circulation|entren en circulació els bitllets i monedes d'euro")),
        (1713, Level.HighSchool, Text.Of("the Peace of Utrecht|la Paz de Utrecht|les traités d'Utrecht|la Pau d'Utrecht")),
        (1688, Level.HighSchool, Text.Of("England's Glorious Revolution|la Revolución Gloriosa inglesa|la Glorieuse Révolution anglaise|la Revolució Gloriosa anglesa")),
        (1861, Level.HighSchool, Text.Of("the Kingdom of Italy is proclaimed|se proclama el Reino de Italia|le royaume d'Italie est proclamé|es proclama el Regne d'Itàlia")),
        (1884, Level.HighSchool, Text.Of("the Berlin Conference on Africa opens|se inaugura la Conferencia de Berlín sobre África|la conférence de Berlin sur l'Afrique s'ouvre|s'inaugura la Conferència de Berlín sobre l'Àfrica")),
        (1962, Level.HighSchool, Text.Of("the Cuban Missile Crisis|la crisis de los misiles de Cuba|la crise des missiles de Cuba|la crisi dels míssils de Cuba")),
    ];

    private static readonly Fact[] People =
    [
        Fact.Of(Level.Primary3, "history.people", "person", "Who was the first Roman emperor?|¿Quién fue el primer emperador romano?|Qui fut le premier empereur romain ?|Qui va ser el primer emperador romà?", "Augustus|Augusto|Auguste|August"),
        Fact.Of(Level.Primary3, "history.people", "person", "Who was crowned emperor of the French in 1804?|¿Quién fue coronado emperador de los franceses en 1804?|Qui fut sacré empereur des Français en 1804 ?|Qui va ser coronat emperador dels francesos el 1804?", "Napoleon Bonaparte|Napoleón Bonaparte|Napoléon Bonaparte|Napoleó Bonaparte"),
        Fact.Of(Level.Primary3, "history.people", "person", "Which queen of Castile supported Columbus's voyage?|¿Qué reina de Castilla apoyó el viaje de Colón?|Quelle reine de Castille a soutenu le voyage de Colomb ?|Quina reina de Castella va donar suport al viatge de Colom?", "Isabella I|Isabel I|Isabelle Iʳᵉ|Isabel I"),
        Fact.Of(Level.Secondary1, "history.people", "person", "Who was crowned emperor in Rome in the year 800?|¿Quién fue coronado emperador en Roma el año 800?|Qui fut couronné empereur à Rome en 800 ?|Qui va ser coronat emperador a Roma l'any 800?", "Charlemagne|Carlomagno|Charlemagne|Carlemany"),
        Fact.Of(Level.Secondary2, "history.people", "person", "Who became South Africa's first democratically elected president in 1994?|¿Quién fue en 1994 el primer presidente elegido democráticamente de Sudáfrica?|Qui devint en 1994 le premier président démocratiquement élu d'Afrique du Sud ?|Qui va ser el 1994 el primer president elegit democràticament de Sud-àfrica?", "Nelson Mandela"),
        Fact.Of(Level.Secondary2, "history.people", "person", "Who was the British prime minister for most of the Second World War?|¿Quién fue primer ministro británico durante casi toda la Segunda Guerra Mundial?|Qui fut Premier ministre britannique pendant presque toute la Seconde Guerre mondiale ?|Qui va ser primer ministre britànic durant gairebé tota la Segona Guerra Mundial?", "Winston Churchill"),
        Fact.Of(Level.Master, "history.historiography", "historian", "Who founded the Annales school with Lucien Febvre?|¿Quién fundó la escuela de los Annales con Lucien Febvre?|Qui a fondé l'école des Annales avec Lucien Febvre ?|Qui va fundar l'escola dels Annales amb Lucien Febvre?", "Marc Bloch"),
        Fact.Of(Level.Master, "history.historiography", "historian", "Which historian developed the idea of the \"longue durée\"?|¿Qué historiador desarrolló la idea de la «larga duración»?|Quel historien a développé la notion de « longue durée » ?|Quin historiador va desenvolupar la idea de la «llarga durada»?", "Fernand Braudel"),
        Fact.Of(Level.Master, "history.historiography", "historian", "Who coined \"the long 19th century\" (1789–1914)?|¿Quién acuñó «el largo siglo XIX» (1789-1914)?|Qui a forgé l'expression « le long XIXᵉ siècle » (1789-1914) ?|Qui va encunyar «el llarg segle XIX» (1789-1914)?", "Eric Hobsbawm"),
        Fact.Of(Level.Doctorate, "history.historiography", "historian", "Who wrote \"The Cheese and the Worms\", a founding work of microhistory?|¿Quién escribió «El queso y los gusanos», obra fundacional de la microhistoria?|Qui a écrit « Le Fromage et les vers », œuvre fondatrice de la microhistoire ?|Qui va escriure «El formatge i els cucs», obra fundacional de la microhistòria?", "Carlo Ginzburg"),
        Fact.Of(Level.Doctorate, "history.historiography", "historian", "Who wrote \"Imagined Communities\" on the origins of nationalism?|¿Quién escribió «Comunidades imaginadas» sobre el origen del nacionalismo?|Qui a écrit « L'Imaginaire national » sur les origines du nationalisme ?|Qui va escriure «Comunitats imaginades» sobre l'origen del nacionalisme?", "Benedict Anderson"),
        Fact.Of(Level.Doctorate, "history.historiography", "historian", "Who wrote \"The Making of the English Working Class\"?|¿Quién escribió «La formación de la clase obrera en Inglaterra»?|Qui a écrit « La Formation de la classe ouvrière anglaise » ?|Qui va escriure «La formació de la classe obrera a Anglaterra»?", "E. P. Thompson"),
    ];

    public static QuizQuestion Generate(Mc q) => q.Level switch
    {
        <= Level.Primary3 => q.Topic(Chronology, Year, f => Facts.Ask(f, People))(q),
        <= Level.HighSchool => q.Topic(Year, Chronology, Century, f => Facts.Ask(f, People))(q),
        Level.Bachelor => q.Topic(Year, Century, f => Facts.Ask(f, People))(q),
        _ => q.Topic(f => Facts.Ask(f, People), Year)(q),
    };

    private static (int Year, Level Level, Text Event) PickEvent(Mc q)
    {
        var near = Events.Where(e => e.Level <= q.Level && e.Level >= q.Level - 2).ToArray();
        return q.Pick(near.Length > 0 ? near : Events.Where(e => e.Level <= q.Level).DefaultIfEmpty(Events[0]).ToArray());
    }

    private static QuizQuestion Year(Mc q)
    {
        var e = PickEvent(q);
        var spread = q.Difficulty switch { 1 => 100, 2 => 30, _ => 10 };
        var others = Events.Select(x => (double)x.Year).Where(y => Math.Abs(y - e.Year) <= 3 * spread && y != e.Year);
        return q.Number("history.dates", q.F("In which year: {0}?|¿En qué año: {0}?|En quelle année : {0} ?|En quin any: {0}?", e.Event.In(q.Language)), e.Year,
            others.Concat([e.Year + spread, e.Year - spread, e.Year + 2 * spread]), q.F("{0}: {1}.|{0}: {1}.|{0} : {1}.|{0}: {1}.", e.Year, e.Event.In(q.Language)), decimals: 0);
    }

    private static string Roman(int n)
    {
        var map = new (int Value, string Symbol)[] { (10, "X"), (9, "IX"), (5, "V"), (4, "IV"), (1, "I") };
        var result = "";
        foreach (var (value, symbol) in map)
        {
            while (n >= value)
            {
                result += symbol;
                n -= value;
            }
        }

        return result;
    }

    private static QuizQuestion Century(Mc q)
    {
        var e = PickEvent(q);
        var century = (e.Year - 1) / 100 + 1;
        return q.Choice("history.dates", q.F("In which century: {0} ({1})?|¿En qué siglo: {0} ({1})?|À quel siècle : {0} ({1}) ?|En quin segle: {0} ({1})?", e.Event.In(q.Language), e.Year),
            Roman(century), [Roman(century - 1), Roman(century + 1), Roman(century + 2), Roman(Math.Max(1, century - 2))],
            q.F("Years {0}01 to {1}00 form century {2}.|Los años {0}01 a {1}00 forman el siglo {2}.|Les années {0}01 à {1}00 forment le {2}ᵉ siècle.|Els anys {0}01 a {1}00 formen el segle {2}.", century - 1, century, Roman(century)));
    }

    private static QuizQuestion Chronology(Mc q)
    {
        var pool = Events.Where(e => e.Level <= LevelExtensions.Max(q.Level, Level.Primary2)).OrderBy(_ => q.Rng.Next()).Take(4).ToList();
        var first = pool.MinBy(e => e.Year);
        return q.Choice("history.chronology", q.T("Which of these happened first?|¿Cuál de estos hechos ocurrió primero?|Lequel de ces événements a eu lieu en premier ?|Quin d'aquests fets va passar primer?"),
            first.Event.In(q.Language), pool.Select(e => e.Event.In(q.Language)),
            string.Join(" → ", pool.OrderBy(e => e.Year).Select(e => $"{e.Year}")) + " · " + first.Event.In(q.Language));
    }
}

internal static class LevelExtensions
{
    public static Level Max(Level a, Level b) => a > b ? a : b;
}

/// <summary>Geography: flags, continents and capitals from a country table, physical geography, climates and theories.</summary>
internal static class GeographyBank
{
    private static readonly Text[] Continents =
    [
        Text.Of("Europe|Europa|Europe|Europa"), Text.Of("Asia|Asia|Asie|Àsia"), Text.Of("Africa|África|Afrique|Àfrica"),
        Text.Of("North America|América del Norte|Amérique du Nord|Amèrica del Nord"), Text.Of("South America|América del Sur|Amérique du Sud|Amèrica del Sud"), Text.Of("Oceania|Oceanía|Océanie|Oceania"),
    ];

    private static readonly (Text Name, Text Capital, int Continent, string Flag, Level Level)[] Countries =
    [
        (Text.Of("Spain|España|Espagne|Espanya"), Text.Of("Madrid"), 0, "🇪🇸", Level.Primary1),
        (Text.Of("France|Francia|France|França"), Text.Of("Paris|París|Paris|París"), 0, "🇫🇷", Level.Primary1),
        (Text.Of("Italy|Italia|Italie|Itàlia"), Text.Of("Rome|Roma|Rome|Roma"), 0, "🇮🇹", Level.Primary1),
        (Text.Of("Germany|Alemania|Allemagne|Alemanya"), Text.Of("Berlin|Berlín|Berlin|Berlín"), 0, "🇩🇪", Level.Primary1),
        (Text.Of("Portugal"), Text.Of("Lisbon|Lisboa|Lisbonne|Lisboa"), 0, "🇵🇹", Level.Primary1),
        (Text.Of("United Kingdom|Reino Unido|Royaume-Uni|Regne Unit"), Text.Of("London|Londres|Londres|Londres"), 0, "🇬🇧", Level.Primary1),
        (Text.Of("United States|Estados Unidos|États-Unis|Estats Units"), Text.Of("Washington, D.C."), 3, "🇺🇸", Level.Primary1),
        (Text.Of("Brazil|Brasil|Brésil|Brasil"), Text.Of("Brasília|Brasilia|Brasilia|Brasília"), 4, "🇧🇷", Level.Primary1),
        (Text.Of("Japan|Japón|Japon|Japó"), Text.Of("Tokyo|Tokio|Tokyo|Tòquio"), 1, "🇯🇵", Level.Primary1),
        (Text.Of("China|China|Chine|Xina"), Text.Of("Beijing|Pekín|Pékin|Pequín"), 1, "🇨🇳", Level.Primary1),
        (Text.Of("Egypt|Egipto|Égypte|Egipte"), Text.Of("Cairo|El Cairo|Le Caire|el Caire"), 2, "🇪🇬", Level.Primary1),
        (Text.Of("Australia|Australia|Australie|Austràlia"), Text.Of("Canberra"), 5, "🇦🇺", Level.Primary1),
        (Text.Of("Mexico|México|Mexique|Mèxic"), Text.Of("Mexico City|Ciudad de México|Mexico|Ciutat de Mèxic"), 3, "🇲🇽", Level.Primary3),
        (Text.Of("Argentina|Argentina|Argentine|Argentina"), Text.Of("Buenos Aires"), 4, "🇦🇷", Level.Primary3),
        (Text.Of("Canada|Canadá|Canada|Canadà"), Text.Of("Ottawa"), 3, "🇨🇦", Level.Primary3),
        (Text.Of("India|India|Inde|Índia"), Text.Of("New Delhi|Nueva Delhi|New Delhi|Nova Delhi"), 1, "🇮🇳", Level.Primary3),
        (Text.Of("Morocco|Marruecos|Maroc|Marroc"), Text.Of("Rabat"), 2, "🇲🇦", Level.Primary3),
        (Text.Of("Russia|Rusia|Russie|Rússia"), Text.Of("Moscow|Moscú|Moscou|Moscou"), 0, "🇷🇺", Level.Primary3),
        (Text.Of("Kenya|Kenia|Kenya|Kenya"), Text.Of("Nairobi"), 2, "🇰🇪", Level.Secondary1),
        (Text.Of("Peru|Perú|Pérou|Perú"), Text.Of("Lima"), 4, "🇵🇪", Level.Secondary1),
        (Text.Of("Greece|Grecia|Grèce|Grècia"), Text.Of("Athens|Atenas|Athènes|Atenes"), 0, "🇬🇷", Level.Secondary1),
        (Text.Of("Sweden|Suecia|Suède|Suècia"), Text.Of("Stockholm|Estocolmo|Stockholm|Estocolm"), 0, "🇸🇪", Level.Secondary1),
        (Text.Of("South Korea|Corea del Sur|Corée du Sud|Corea del Sud"), Text.Of("Seoul|Seúl|Séoul|Seül"), 1, "🇰🇷", Level.Secondary1),
        (Text.Of("New Zealand|Nueva Zelanda|Nouvelle-Zélande|Nova Zelanda"), Text.Of("Wellington"), 5, "🇳🇿", Level.Secondary2),
        (Text.Of("Nigeria|Nigeria|Nigeria|Nigèria"), Text.Of("Abuja"), 2, "🇳🇬", Level.Secondary2),
        (Text.Of("Turkey|Turquía|Turquie|Turquia"), Text.Of("Ankara"), 1, "🇹🇷", Level.Secondary2),
        (Text.Of("Switzerland|Suiza|Suisse|Suïssa"), Text.Of("Bern|Berna|Berne|Berna"), 0, "🇨🇭", Level.Secondary2),
    ];

    private static readonly Fact[] FactList =
    [
        Fact.Of(Level.Secondary1, "geography.physical", "mountain", "Highest mountain in the world?|¿Montaña más alta del mundo?|Plus haute montagne du monde ?|Muntanya més alta del món?", "Everest", null, "🏔️"),
        Fact.Of(Level.Secondary1, "geography.physical", "mountain", "Highest mountain of the Alps?|¿Montaña más alta de los Alpes?|Plus haute montagne des Alpes ?|Muntanya més alta dels Alps?", "Mont Blanc|Mont Blanc|mont Blanc|Mont Blanc"),
        Fact.Of(Level.Secondary1, "geography.physical", "mountain", "Highest mountain of South America?|¿Montaña más alta de América del Sur?|Plus haute montagne d'Amérique du Sud ?|Muntanya més alta d'Amèrica del Sud?", "Aconcagua"),
        Fact.Of(Level.Secondary1, "geography.physical", "mountain", "Highest mountain of Africa?|¿Montaña más alta de África?|Plus haute montagne d'Afrique ?|Muntanya més alta d'Àfrica?", "Kilimanjaro|Kilimanjaro|Kilimandjaro|Kilimanjaro"),
        Fact.Of(Level.Secondary1, "geography.physical", "mountain", "Highest mountain of the Iberian Peninsula?|¿Montaña más alta de la península ibérica?|Plus haute montagne de la péninsule Ibérique ?|Muntanya més alta de la península Ibèrica?", "Mulhacén"),
        Fact.Of(Level.Secondary1, "geography.physical", "river", "Longest river of Europe?|¿Río más largo de Europa?|Plus long fleuve d'Europe ?|Riu més llarg d'Europa?", "Volga"),
        Fact.Of(Level.Secondary1, "geography.physical", "river", "Longest river of the Iberian Peninsula?|¿Río más largo de la península ibérica?|Plus long fleuve de la péninsule Ibérique ?|Riu més llarg de la península Ibèrica?", "Tagus|Tajo|Tage|Tajo"),
        Fact.Of(Level.Secondary1, "geography.physical", "river", "Which river flows through Paris?|¿Qué río pasa por París?|Quel fleuve traverse Paris ?|Quin riu passa per París?", "Seine|Sena|Seine|Sena"),
        Fact.Of(Level.Secondary1, "geography.physical", "river", "Which river flows through London?|¿Qué río pasa por Londres?|Quel fleuve traverse Londres ?|Quin riu passa per Londres?", "Thames|Támesis|Tamise|Tàmesi"),
        Fact.Of(Level.Secondary1, "geography.physical", "river", "Which river flows through Zaragoza?|¿Qué río pasa por Zaragoza?|Quel fleuve traverse Saragosse ?|Quin riu passa per Saragossa?", "Ebro|Ebro|Èbre|Ebre"),
        Fact.Of(Level.HighSchool, "geography.climate", "climate", "Climate with hot dry summers and mild rainy winters?|¿Clima con veranos cálidos y secos e inviernos suaves y lluviosos?|Climat aux étés chauds et secs et aux hivers doux et pluvieux ?|Clima amb estius càlids i secs i hiverns suaus i plujosos?", "Mediterranean|mediterráneo|méditerranéen|mediterrani"),
        Fact.Of(Level.HighSchool, "geography.climate", "climate", "Climate hot and rainy all year, near the equator?|¿Clima cálido y lluvioso todo el año, cerca del ecuador?|Climat chaud et pluvieux toute l'année, près de l'équateur ?|Clima càlid i plujós tot l'any, prop de l'equador?", "equatorial|ecuatorial|équatorial|equatorial"),
        Fact.Of(Level.HighSchool, "geography.climate", "climate", "Very cold climate with permanently frozen subsoil (permafrost)?|¿Clima muy frío con el subsuelo helado todo el año (permafrost)?|Climat très froid au sous-sol gelé en permanence (pergélisol) ?|Clima molt fred amb el subsòl glaçat tot l'any (permafrost)?", "tundra|tundra|toundra|tundra"),
        Fact.Of(Level.HighSchool, "geography.climate", "climate", "Climate with a rainy season brought by seasonal winds, as in India?|¿Clima con una estación de lluvias traída por vientos estacionales, como en la India?|Climat avec une saison des pluies apportée par des vents saisonniers, comme en Inde ?|Clima amb una estació de pluges portada per vents estacionals, com a l'Índia?", "monsoon|monzónico|de mousson|monsònic"),
        Fact.Of(Level.Bachelor, "geography.theory", "geographer", "Who proposed central place theory?|¿Quién propuso la teoría de los lugares centrales?|Qui a proposé la théorie des lieux centraux ?|Qui va proposar la teoria dels llocs centrals?", "Walter Christaller"),
        Fact.Of(Level.Bachelor, "geography.theory", "geographer", "Whose model describes rings of agricultural land use around a city?|¿Qué modelo describe anillos de uso agrícola del suelo alrededor de una ciudad?|Quel modèle décrit des anneaux d'utilisation agricole du sol autour d'une ville ?|Quin model descriu anells d'ús agrícola del sòl al voltant d'una ciutat?", "Johann Heinrich von Thünen"),
        Fact.Of(Level.Master, "geography.theory", "geographer", "Who stated: \"Everything is related to everything else, but near things are more related\"?|¿Quién formuló: «Todo está relacionado con todo, pero lo cercano más que lo lejano»?|Qui a énoncé : « Tout interagit avec tout, mais deux objets proches ont plus de chances de le faire » ?|Qui va formular: «Tot està relacionat amb tot, però el que és a prop més que el que és lluny»?", "Waldo Tobler"),
        Fact.Of(Level.Master, "geography.theory", "geographer", "Who developed world-systems analysis (core, semi-periphery, periphery)?|¿Quién desarrolló el análisis del sistema-mundo (centro, semiperiferia, periferia)?|Qui a développé l'analyse du système-monde (centre, semi-périphérie, périphérie) ?|Qui va desenvolupar l'anàlisi del sistema món (centre, semiperifèria, perifèria)?", "Immanuel Wallerstein"),
        Fact.Of(Level.Doctorate, "geography.theory", "geographer", "Who wrote \"The Production of Space\" (1974)?|¿Quién escribió «La producción del espacio» (1974)?|Qui a écrit « La Production de l'espace » (1974) ?|Qui va escriure «La producció de l'espai» (1974)?", "Henri Lefebvre"),
        Fact.Of(Level.Doctorate, "geography.theory", "geographer", "Who founded time geography?|¿Quién fundó la geografía del tiempo?|Qui a fondé la géographie du temps (time geography) ?|Qui va fundar la geografia del temps?", "Torsten Hägerstrand"),
    ];

    public static QuizQuestion Generate(Mc q) => q.Level switch
    {
        Level.Primary1 => q.Topic(Flag, Continent)(q),
        Level.Primary2 => q.Topic(Flag, Continent, Capital)(q),
        Level.Primary3 => q.Topic(Continent, Capital)(q),
        Level.Secondary1 or Level.Secondary2 => q.Topic(Capital, f => Facts.Ask(f, FactList))(q),
        Level.HighSchool => q.Topic(Capital, f => Facts.Ask(f, FactList))(q),
        _ => Facts.Ask(q, FactList),
    };

    private static (Text Name, Text Capital, int Continent, string Flag, Level Level) PickCountry(Mc q)
    {
        var near = Countries.Where(c => c.Level <= q.Level && (q.Difficulty < 3 || c.Level >= q.Level - 1)).ToArray();
        return q.Pick(near.Length >= 4 ? near : Countries.Where(c => c.Level <= LevelExtensions.Max(q.Level, Level.Primary1)).ToArray());
    }

    private static QuizQuestion Flag(Mc q)
    {
        var c = PickCountry(q);
        return q.Choice("geography.countries", q.T("Which country has this flag?|¿De qué país es esta bandera?|De quel pays est ce drapeau ?|De quin país és aquesta bandera?"),
            c.Name.In(q.Language), Countries.Where(x => x.Level <= LevelExtensions.Max(q.Level, Level.Primary1)).Select(x => x.Name.In(q.Language)),
            q.F("{0} is the flag of {1}.|{0} es la bandera de {1}.|{0} est le drapeau du pays {1}.|{0} és la bandera de {1}.", c.Flag, c.Name.In(q.Language)), visual: c.Flag);
    }

    private static QuizQuestion Continent(Mc q)
    {
        var c = PickCountry(q);
        return q.Choice("geography.continents", q.F("On which continent is {0}?|¿En qué continente está {0}?|Sur quel continent se trouve le pays {0} ?|A quin continent és {0}?", c.Name.In(q.Language)),
            Continents[c.Continent].In(q.Language), Continents.Select(x => x.In(q.Language)),
            q.F("{0} is in {1}.|{0} está en {1}.|{0} se trouve en {1}.|{0} és a {1}.", c.Name.In(q.Language), Continents[c.Continent].In(q.Language)), visual: c.Flag);
    }

    private static QuizQuestion Capital(Mc q)
    {
        var c = PickCountry(q);
        return q.Choice("geography.capitals", q.F("Capital of {0}?|¿Capital de {0}?|Capitale du pays {0} ?|Capital de {0}?", c.Name.In(q.Language)),
            c.Capital.In(q.Language), Countries.Select(x => x.Capital.In(q.Language)).Concat([q.T("Barcelona"), q.T("Sydney|Sídney|Sydney|Sydney"), q.T("Istanbul|Estambul|Istanbul|Istanbul"), q.T("Toronto")]),
            q.F("The capital of {0} is {1}.|La capital de {0} es {1}.|La capitale du pays {0} est {1}.|La capital de {0} és {1}.", c.Name.In(q.Language), c.Capital.In(q.Language)), visual: c.Flag);
    }
}

/// <summary>Economics: personal finance, markets and investment computed in code; concepts and economists.</summary>
internal static class EconomicsBank
{
    private static readonly Fact[] FactList =
    [
        Fact.Of(Level.Secondary2, "economics.concepts", "concept", "General and sustained rise in prices?|¿Subida general y sostenida de los precios?|Hausse générale et durable des prix ?|Pujada general i sostinguda dels preus?", "inflation|inflación|inflation|inflació"),
        Fact.Of(Level.Secondary2, "economics.concepts", "concept", "Value of all final goods and services produced in a country in a year?|¿Valor de todos los bienes y servicios finales producidos en un país en un año?|Valeur de tous les biens et services finaux produits dans un pays en un an ?|Valor de tots els béns i serveis finals produïts en un país en un any?", "GDP|PIB|PIB|PIB"),
        Fact.Of(Level.Secondary2, "economics.concepts", "concept", "Value of the best alternative you give up when you choose?|¿Valor de la mejor alternativa a la que renuncias al elegir?|Valeur de la meilleure alternative à laquelle on renonce en choisissant ?|Valor de la millor alternativa a què renuncies quan tries?", "opportunity cost|coste de oportunidad|coût d'opportunité|cost d'oportunitat"),
        Fact.Of(Level.Secondary2, "economics.concepts", "concept", "Market with a single seller?|¿Mercado con un solo vendedor?|Marché avec un seul vendeur ?|Mercat amb un sol venedor?", "monopoly|monopolio|monopole|monopoli"),
        Fact.Of(Level.HighSchool, "economics.thinkers", "economist", "Who described the \"invisible hand\" of the market?|¿Quién describió la «mano invisible» del mercado?|Qui a décrit la « main invisible » du marché ?|Qui va descriure la «mà invisible» del mercat?", "Adam Smith"),
        Fact.Of(Level.HighSchool, "economics.thinkers", "economist", "Who wrote \"The General Theory of Employment, Interest and Money\"?|¿Quién escribió la «Teoría general del empleo, el interés y el dinero»?|Qui a écrit la « Théorie générale de l'emploi, de l'intérêt et de la monnaie » ?|Qui va escriure la «Teoria general de l'ocupació, l'interès i el diner»?", "John Maynard Keynes"),
        Fact.Of(Level.HighSchool, "economics.thinkers", "economist", "Who developed the theory of comparative advantage?|¿Quién desarrolló la teoría de la ventaja comparativa?|Qui a développé la théorie de l'avantage comparatif ?|Qui va desenvolupar la teoria de l'avantatge comparatiu?", "David Ricardo"),
        Fact.Of(Level.Master, "economics.thinkers", "economist", "Who proved that no voting rule can satisfy a set of fair conditions at once (impossibility theorem)?|¿Quién demostró que ninguna regla de votación cumple a la vez un conjunto de condiciones justas (teorema de imposibilidad)?|Qui a démontré qu'aucune règle de vote ne satisfait à la fois un ensemble de conditions équitables (théorème d'impossibilité) ?|Qui va demostrar que cap regla de votació compleix alhora un conjunt de condicions justes (teorema d'impossibilitat)?", "Kenneth Arrow"),
        Fact.Of(Level.Master, "economics.thinkers", "economist", "Whose growth model explains long-run growth with capital accumulation and technology?|¿Qué modelo de crecimiento explica el crecimiento a largo plazo con acumulación de capital y tecnología?|Quel modèle de croissance explique la croissance de long terme par l'accumulation du capital et la technologie ?|Quin model de creixement explica el creixement a llarg termini amb acumulació de capital i tecnologia?", "Robert Solow"),
        Fact.Of(Level.Doctorate, "economics.thinkers", "economist", "Who argued that policy evaluations based on past relations fail when expectations change (the critique)?|¿Quién sostuvo que evaluar políticas con relaciones pasadas falla cuando cambian las expectativas (la crítica)?|Qui a soutenu que l'évaluation des politiques fondée sur des relations passées échoue quand les anticipations changent (la critique) ?|Qui va sostenir que avaluar polítiques amb relacions passades falla quan canvien les expectatives (la crítica)?", "Robert Lucas"),
        Fact.Of(Level.Doctorate, "economics.thinkers", "economist", "Who introduced the equilibrium in which no player gains by changing strategy alone?|¿Quién introdujo el equilibrio en el que ningún jugador gana cambiando de estrategia él solo?|Qui a introduit l'équilibre où aucun joueur ne gagne à changer seul de stratégie ?|Qui va introduir l'equilibri en què cap jugador guanya canviant d'estratègia tot sol?", "John Nash"),
    ];

    public static QuizQuestion Generate(Mc q) => q.Level switch
    {
        <= Level.Secondary2 => q.Topic(PercentChange, Discount, f => Facts.Ask(f, FactList))(q),
        Level.HighSchool => q.Topic(Interest, Equilibrium, f => Facts.Ask(f, FactList))(q),
        Level.Bachelor => q.Topic(Elasticity, PresentValue, Equilibrium)(q),
        _ => q.Topic(f => Facts.Ask(f, FactList), PresentValue, Elasticity)(q),
    };

    private static QuizQuestion PercentChange(Mc q)
    {
        var before = q.Between(2, 20) * 10;
        var change = q.Pick([-50, -25, -20, -10, 10, 20, 25, 50, 100]);
        var after = before * (100 + change) / 100;
        return q.Number("economics.percentages", q.F("A price goes from {0} € to {1} €. Percentage change?|Un precio pasa de {0} € a {1} €. ¿Variación porcentual?|Un prix passe de {0} € à {1} €. Variation en pourcentage ?|Un preu passa de {0} € a {1} €. Variació percentual?", before, after),
            change, [after - before, (after - before) * 100.0 / after, -change], q.F("({0} − {1}) ÷ {1} × 100 = {2} %.|({0} − {1}) ÷ {1} × 100 = {2} %.|({0} − {1}) ÷ {1} × 100 = {2} %.|({0} − {1}) ÷ {1} × 100 = {2} %.", after, before, change), unit: " %");
    }

    private static QuizQuestion Discount(Mc q)
    {
        var price = q.Between(2, 30) * 10;
        var discount = q.Pick([10, 20, 25, 50]);
        var final = price * (100 - discount) / 100.0;
        return q.Number("economics.percentages", q.F("A {0} € jacket has a {1} % discount. Final price?|Una chaqueta de {0} € tiene un {1} % de descuento. ¿Precio final?|Une veste à {0} € a {1} % de remise. Prix final ?|Una jaqueta de {0} € té un {1} % de descompte. Preu final?", price, discount),
            final, [price - discount, price * discount / 100.0, final - 10], q.F("{0} × (1 − {1}/100) = {2} €.|{0} × (1 − {1}/100) = {2} €.|{0} × (1 − {1}/100) = {2} €.|{0} × (1 − {1}/100) = {2} €.", price, discount, q.N(final)), unit: " €");
    }

    private static QuizQuestion Interest(Mc q)
    {
        var capital = q.Between(1, 10) * 1000;
        var rate = q.Pick([2, 5, 10]);
        var years = q.Between(2, 3);
        var compound = capital * Math.Pow(1 + rate / 100.0, years);
        return q.Number("economics.finance", q.F("{0} € invested at {1} % compound annual interest. Amount after {2} years?|{0} € invertidos al {1} % de interés compuesto anual. ¿Capital tras {2} años?|{0} € placés à {1} % d'intérêts composés annuels. Montant après {2} ans ?|{0} € invertits al {1} % d'interès compost anual. Capital després de {2} anys?", capital, rate, years),
            compound, [capital * (1 + rate * years / 100.0), capital * rate * years / 100.0, compound + 100], q.F("C = {0} × (1 + {1}/100)^{2} = {3} €.|C = {0} × (1 + {1}/100)^{2} = {3} €.|C = {0} × (1 + {1}/100)^{2} = {3} €.|C = {0} × (1 + {1}/100)^{2} = {3} €.", capital, rate, years, q.N(compound)), unit: " €");
    }

    private static QuizQuestion Equilibrium(Mc q)
    {
        var p = q.Between(2, 20);
        var b = q.Between(1, 5);
        var d = q.Between(1, 5);
        var c = q.Between(0, 10);
        var a = c + (b + d) * p;
        return q.Number("economics.markets", q.F("Demand Qd = {0} − {1}P and supply Qs = {2} + {3}P. Equilibrium price?|Demanda Qd = {0} − {1}P y oferta Qs = {2} + {3}P. ¿Precio de equilibrio?|Demande Qd = {0} − {1}P et offre Qs = {2} + {3}P. Prix d'équilibre ?|Demanda Qd = {0} − {1}P i oferta Qs = {2} + {3}P. Preu d'equilibri?", a, b, c, d),
            p, [a - b * p, (a + c) / (double)(b + d), p + 1], q.F("Qd = Qs ⇒ {0} − {1}P = {2} + {3}P ⇒ P = {4}.|Qd = Qs ⇒ {0} − {1}P = {2} + {3}P ⇒ P = {4}.|Qd = Qs ⇒ {0} − {1}P = {2} + {3}P ⇒ P = {4}.|Qd = Qs ⇒ {0} − {1}P = {2} + {3}P ⇒ P = {4}.", a, b, c, d, p), visual: Diagram.Market());
    }

    private static QuizQuestion Elasticity(Mc q)
    {
        var priceChange = q.Pick([5, 10, 20]);
        var quantityChange = -q.Pick([5, 10, 15, 20, 30, 40]);
        var e = quantityChange / (double)priceChange;
        return q.Number("economics.markets", q.F("The price rises {0} % and the quantity demanded changes {1} %. Price elasticity of demand?|El precio sube un {0} % y la cantidad demandada varía un {1} %. ¿Elasticidad precio de la demanda?|Le prix augmente de {0} % et la quantité demandée varie de {1} %. Élasticité-prix de la demande ?|El preu puja un {0} % i la quantitat demandada varia un {1} %. Elasticitat preu de la demanda?", priceChange, quantityChange),
            e, [-e, priceChange / (double)quantityChange, e - 1], q.F("E = %ΔQ ÷ %ΔP = {0} ÷ {1} = {2} ({3}).|E = %ΔQ ÷ %ΔP = {0} ÷ {1} = {2} ({3}).|E = %ΔQ ÷ %ΔP = {0} ÷ {1} = {2} ({3}).|E = %ΔQ ÷ %ΔP = {0} ÷ {1} = {2} ({3}).", quantityChange, priceChange, q.N(e),
                q.T(Math.Abs(e) > 1 ? "elastic|elástica|élastique|elàstica" : Math.Abs(e) < 1 ? "inelastic|inelástica|inélastique|inelàstica" : "unit elastic|unitaria|unitaire|unitària")));
    }

    private static QuizQuestion PresentValue(Mc q)
    {
        var rate = q.Pick([5, 10, 20]);
        var years = q.Between(1, 2);
        var present = q.Between(1, 9) * 100;
        var future = present * Math.Pow(1 + rate / 100.0, years);
        return q.Number("economics.finance", q.F("Present value of {0} € received in {1} year(s), discount rate {2} %?|¿Valor actual de {0} € recibidos dentro de {1} año(s), tasa de descuento {2} %?|Valeur actuelle de {0} € reçus dans {1} an(s), taux d'actualisation {2} % ?|Valor actual de {0} € rebuts d'aquí a {1} any(s), taxa de descompte {2} %?", q.N(future), years, rate),
            present, [future, future * (1 - rate * years / 100.0), present - 50], q.F("PV = FV ÷ (1 + r)^n = {0} ÷ {1}^{2} = {3} €.|VA = VF ÷ (1 + r)^n = {0} ÷ {1}^{2} = {3} €.|VA = VF ÷ (1 + r)^n = {0} ÷ {1}^{2} = {3} €.|VA = VF ÷ (1 + r)^n = {0} ÷ {1}^{2} = {3} €.", q.N(future), q.N(1 + rate / 100.0), years, present), unit: " €");
    }
}
