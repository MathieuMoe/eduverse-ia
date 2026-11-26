using EduVerse.Core.Common;

namespace EduVerse.Core.Catalog;

/// <summary>
/// Demo content for the Duolingo-style exercise kinds (<see cref="ExerciseKind"/>) that are mixed into Practice-mode
/// quizzes alongside classic multiple-choice questions. True/False now covers all 10 subjects (task #72); the other
/// three kinds (fill-the-blank, reorder, match) are filled in incrementally in later passes, starting with the
/// subjects where they fit most naturally (Language, ForeignLanguage — task #71).
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

    private static readonly IReadOnlyList<TrueFalseItem> PhysicsTrueFalse =
    [
        new("physics.boiling", 1,
            Text.Of("Water boils at 100°C at sea level.|El agua hierve a 100°C al nivel del mar.|L'eau bout à 100°C au niveau de la mer.|L'aigua bull a 100°C al nivell del mar."),
            true,
            Text.Of("Yes: at normal atmospheric pressure, water's boiling point is 100°C.|Sí: a la presión atmosférica normal, el punto de ebullición del agua es 100°C.|Oui : à la pression atmosphérique normale, le point d'ébullition de l'eau est 100°C.|Sí: a la pressió atmosfèrica normal, el punt d'ebullició de l'aigua és 100°C.")),
        new("physics.orbit", 1,
            Text.Of("The Earth orbits the Sun.|La Tierra orbita alrededor del Sol.|La Terre tourne autour du Soleil.|La Terra orbita al voltant del Sol."),
            true,
            Text.Of("Yes: the Earth completes one orbit around the Sun roughly every 365 days.|Sí: la Tierra completa una vuelta alrededor del Sol cada 365 días aproximadamente.|Oui : la Terre fait un tour complet autour du Soleil en environ 365 jours.|Sí: la Terra completa una volta al voltant del Sol cada 365 dies aproximadament.")),
        new("physics.light_sound", 2,
            Text.Of("Light travels faster than sound.|La luz viaja más rápido que el sonido.|La lumière voyage plus vite que le son.|La llum viatja més ràpid que el so."),
            true,
            Text.Of("Yes: that's why you see lightning before you hear the thunder.|Sí: por eso se ve el relámpago antes de oír el trueno.|Oui : c'est pour ça qu'on voit l'éclair avant d'entendre le tonnerre.|Sí: per això es veu el llamp abans de sentir el tro.")),
        new("physics.vacuum_fall", 2,
            Text.Of("In a vacuum, a feather and a hammer fall at the same speed.|En el vacío, una pluma y un martillo caen a la misma velocidad.|Dans le vide, une plume et un marteau tombent à la même vitesse.|Al buit, una ploma i un martell cauen a la mateixa velocitat."),
            true,
            Text.Of("Yes: without air resistance, gravity accelerates every object equally, whatever its mass.|Sí: sin resistencia del aire, la gravedad acelera a todos los objetos por igual, sea cual sea su masa.|Oui : sans résistance de l'air, la gravité accélère tous les objets de la même façon, quelle que soit leur masse.|Sí: sense resistència de l'aire, la gravetat accelera tots els objectes per igual, sigui quina sigui la seva massa.")),
        new("physics.energy", 3,
            Text.Of("Energy can be created out of nothing.|La energía se puede crear de la nada.|L'énergie peut être créée à partir de rien.|L'energia es pot crear del no-res."),
            false,
            Text.Of("No: the law of conservation of energy says energy can only change form, never be created or destroyed.|No: la ley de conservación de la energía dice que la energía solo puede cambiar de forma, nunca crearse ni destruirse.|Non : la loi de conservation de l'énergie dit que l'énergie ne peut que changer de forme, jamais être créée ni détruite.|No: la llei de conservació de l'energia diu que l'energia només pot canviar de forma, mai crear-se ni destruir-se.")),
    ];

    private static readonly IReadOnlyList<TrueFalseItem> ChemistryTrueFalse =
    [
        new("chemistry.water", 1,
            Text.Of("Water is made of hydrogen and oxygen.|El agua está formada por hidrógeno y oxígeno.|L'eau est composée d'hydrogène et d'oxygène.|L'aigua està formada per hidrogen i oxigen."),
            true,
            Text.Of("Yes: a water molecule is H₂O — two hydrogen atoms and one oxygen atom.|Sí: una molécula de agua es H₂O, dos átomos de hidrógeno y uno de oxígeno.|Oui : une molécule d'eau est H₂O, deux atomes d'hydrogène et un d'oxygène.|Sí: una molècula d'aigua és H₂O, dos àtoms d'hidrogen i un d'oxigen.")),
        new("chemistry.salt", 1,
            Text.Of("Table salt is made of carbon and oxygen.|La sal de mesa está formada por carbono y oxígeno.|Le sel de table est composé de carbone et d'oxygène.|La sal de taula està formada per carboni i oxigen."),
            false,
            Text.Of("No: table salt (NaCl) is made of sodium and chlorine.|No: la sal de mesa (NaCl) está formada por sodio y cloro.|Non : le sel de table (NaCl) est composé de sodium et de chlore.|No: la sal de taula (NaCl) està formada per sodi i clor.")),
        new("chemistry.ice", 2,
            Text.Of("Ice is denser than liquid water.|El hielo es más denso que el agua líquida.|La glace est plus dense que l'eau liquide.|El gel és més dens que l'aigua líquida."),
            false,
            Text.Of("No: ice is less dense than liquid water, which is exactly why it floats.|No: el hielo es menos denso que el agua líquida, por eso flota.|Non : la glace est moins dense que l'eau liquide, c'est pour ça qu'elle flotte.|No: el gel és menys dens que l'aigua líquida, per això flota.")),
        new("chemistry.ph", 2,
            Text.Of("A pH of 7 is neutral.|Un pH de 7 es neutro.|Un pH de 7 est neutre.|Un pH de 7 és neutre."),
            true,
            Text.Of("Yes: pH 7 is neutral; below 7 is acidic, above 7 is basic.|Sí: el pH 7 es neutro; por debajo de 7 es ácido, por encima de 7 es básico.|Oui : le pH 7 est neutre ; en dessous de 7 c'est acide, au-dessus c'est basique.|Sí: el pH 7 és neutre; per sota de 7 és àcid, per sobre de 7 és bàsic.")),
        new("chemistry.protons", 3,
            Text.Of("Atoms of the same element always have the same number of protons.|Los átomos de un mismo elemento siempre tienen el mismo número de protones.|Les atomes d'un même élément ont toujours le même nombre de protons.|Els àtoms d'un mateix element sempre tenen el mateix nombre de protons."),
            true,
            Text.Of("Yes: the number of protons (the atomic number) is exactly what defines an element.|Sí: el número de protones (el número atómico) es precisamente lo que define a un elemento.|Oui : le nombre de protons (le numéro atomique) est précisément ce qui définit un élément.|Sí: el nombre de protons (el nombre atòmic) és precisament el que defineix un element.")),
    ];

    private static readonly IReadOnlyList<TrueFalseItem> BiologyTrueFalse =
    [
        new("biology.heart", 1,
            Text.Of("Humans have one heart.|Los humanos tenemos un corazón.|Les humains ont un cœur.|Els humans tenim un cor."),
            true,
            Text.Of("Yes: humans have a single heart, with four chambers.|Sí: los humanos tenemos un solo corazón, con cuatro cámaras.|Oui : les humains ont un seul cœur, avec quatre cavités.|Sí: els humans tenim un sol cor, amb quatre cavitats.")),
        new("biology.photosynthesis", 1,
            Text.Of("Plants make their own food using sunlight.|Las plantas fabrican su propio alimento usando la luz del sol.|Les plantes fabriquent leur propre nourriture grâce à la lumière du soleil.|Les plantes fabriquen el seu propi aliment fent servir la llum del sol."),
            true,
            Text.Of("Yes: that process is called photosynthesis.|Sí: ese proceso se llama fotosíntesis.|Oui : ce processus s'appelle la photosynthèse.|Sí: aquest procés s'anomena fotosíntesi.")),
        new("biology.bones", 2,
            Text.Of("Adults have more bones than babies.|Los adultos tienen más huesos que los bebés.|Les adultes ont plus d'os que les bébés.|Els adults tenen més ossos que els nadons."),
            false,
            Text.Of("No: babies are born with more bones, and some fuse together as they grow.|No: los bebés nacen con más huesos, y algunos se fusionan a medida que crecen.|Non : les bébés naissent avec plus d'os, et certains fusionnent en grandissant.|No: els nadons neixen amb més ossos, i alguns es fusionen a mesura que creixen.")),
        new("biology.cells", 2,
            Text.Of("All living things are made of cells.|Todos los seres vivos están formados por células.|Tous les êtres vivants sont constitués de cellules.|Tots els éssers vius estan formats per cèl·lules."),
            true,
            Text.Of("Yes: the cell is the basic unit of every living thing, from bacteria to humans.|Sí: la célula es la unidad básica de todo ser vivo, desde las bacterias hasta los humanos.|Oui : la cellule est l'unité de base de tout être vivant, des bactéries aux humains.|Sí: la cèl·lula és la unitat bàsica de tot ésser viu, des dels bacteris fins als humans.")),
        new("biology.dna", 3,
            Text.Of("DNA carries genetic information in humans.|El ADN contiene la información genética en los humanos.|L'ADN porte l'information génétique chez les humains.|L'ADN conté la informació genètica en els humans."),
            true,
            Text.Of("Yes: DNA stores the instructions that build and run every cell in the body.|Sí: el ADN guarda las instrucciones que construyen y hacen funcionar cada célula del cuerpo.|Oui : l'ADN stocke les instructions qui construisent et font fonctionner chaque cellule du corps.|Sí: l'ADN guarda les instruccions que construeixen i fan funcionar cada cèl·lula del cos.")),
    ];

    private static readonly IReadOnlyList<TrueFalseItem> HistoryTrueFalse =
    [
        new("history.pyramids", 1,
            Text.Of("The pyramids of Egypt were built by the Romans.|Las pirámides de Egipto fueron construidas por los romanos.|Les pyramides d'Égypte ont été construites par les Romains.|Les piràmides d'Egipte van ser construïdes pels romans."),
            false,
            Text.Of("No: the pyramids were built by the ancient Egyptians, long before Rome existed.|No: las pirámides fueron construidas por los antiguos egipcios, mucho antes de que existiera Roma.|Non : les pyramides ont été construites par les anciens Égyptiens, bien avant que Rome n'existe.|No: les piràmides van ser construïdes pels antics egipcis, molt abans que existís Roma.")),
        new("history.ww2", 2,
            Text.Of("World War II ended in 1945.|La Segunda Guerra Mundial terminó en 1945.|La Seconde Guerre mondiale s'est terminée en 1945.|La Segona Guerra Mundial va acabar el 1945."),
            true,
            Text.Of("Yes: the war ended in 1945, with Germany surrendering in May and Japan in September.|Sí: la guerra terminó en 1945, con la rendición de Alemania en mayo y la de Japón en septiembre.|Oui : la guerre s'est terminée en 1945, avec la capitulation de l'Allemagne en mai et celle du Japon en septembre.|Sí: la guerra va acabar el 1945, amb la rendició d'Alemanya al maig i la del Japó al setembre.")),
        new("history.columbus", 2,
            Text.Of("Christopher Columbus was the first person to reach the Americas.|Cristóbal Colón fue la primera persona en llegar a América.|Christophe Colomb a été la première personne à atteindre les Amériques.|Cristòfor Colom va ser la primera persona a arribar a Amèrica."),
            false,
            Text.Of("No: Indigenous peoples had already lived in the Americas for thousands of years before Columbus arrived.|No: los pueblos indígenas ya vivían en América desde hacía miles de años antes de que llegara Colón.|Non : les peuples autochtones vivaient déjà en Amérique depuis des milliers d'années avant l'arrivée de Colomb.|No: els pobles indígenes ja vivien a Amèrica des de feia milers d'anys abans que hi arribés Colom.")),
        new("history.berlin_wall", 2,
            Text.Of("The Berlin Wall fell in 1989.|El Muro de Berlín cayó en 1989.|Le mur de Berlin est tombé en 1989.|El Mur de Berlín va caure el 1989."),
            true,
            Text.Of("Yes: the Berlin Wall fell in November 1989, a key moment in the end of the Cold War.|Sí: el Muro de Berlín cayó en noviembre de 1989, un momento clave en el fin de la Guerra Fría.|Oui : le mur de Berlin est tombé en novembre 1989, un moment clé de la fin de la guerre froide.|Sí: el Mur de Berlín va caure el novembre de 1989, un moment clau en la fi de la Guerra Freda.")),
        new("history.french_revolution", 3,
            Text.Of("The French Revolution began in 1789.|La Revolución Francesa comenzó en 1789.|La Révolution française a commencé en 1789.|La Revolució Francesa va començar el 1789."),
            true,
            Text.Of("Yes: it began in 1789, with the storming of the Bastille in July.|Sí: comenzó en 1789, con la toma de la Bastilla en julio.|Oui : elle a commencé en 1789, avec la prise de la Bastille en juillet.|Sí: va començar el 1789, amb la presa de la Bastilla al juliol.")),
        // Task #97: more items per difficulty tier, so a drill repeats less often across sessions.
        new("history.knights", 1,
            Text.Of("Knights and castles belong to the Middle Ages.|Los caballeros y los castillos pertenecen a la Edad Media.|Les chevaliers et les châteaux forts appartiennent au Moyen Âge.|Els cavallers i els castells pertanyen a l'edat mitjana."),
            true,
            Text.Of("Yes: castles and knights are typical of the Middle Ages, roughly from the 5th to the 15th century.|Sí: los castillos y los caballeros son típicos de la Edad Media, más o menos del siglo V al XV.|Oui : les châteaux forts et les chevaliers sont typiques du Moyen Âge, à peu près du Vᵉ au XVᵉ siècle.|Sí: els castells i els cavallers són típics de l'edat mitjana, més o menys del segle V al XV.")),
        new("history.moon_landing", 2,
            Text.Of("The first Moon landing took place in 1969.|El primer alunizaje tuvo lugar en 1969.|Le premier alunissage a eu lieu en 1969.|El primer allunatge va tenir lloc el 1969."),
            true,
            Text.Of("Yes: Apollo 11 landed on the Moon in July 1969.|Sí: el Apolo 11 se posó en la Luna en julio de 1969.|Oui : Apollo 11 s'est posé sur la Lune en juillet 1969.|Sí: l'Apollo 11 es va posar a la Lluna el juliol de 1969.")),
        new("history.magna_carta", 3,
            Text.Of("The Magna Carta was sealed after the French Revolution.|La Carta Magna se selló después de la Revolución francesa.|La Grande Charte a été scellée après la Révolution française.|La Carta Magna es va segellar després de la Revolució francesa."),
            false,
            Text.Of("No: the Magna Carta dates from 1215, more than five centuries before 1789.|No: la Carta Magna es de 1215, más de cinco siglos antes de 1789.|Non : la Grande Charte date de 1215, plus de cinq siècles avant 1789.|No: la Carta Magna és del 1215, més de cinc segles abans del 1789.")),
        new("history.cold_war", 3,
            Text.Of("During the Cold War, the USA and the USSR fought each other directly in a world war.|Durante la Guerra Fría, EE. UU. y la URSS lucharon directamente en una guerra mundial.|Pendant la guerre froide, les États-Unis et l'URSS se sont affrontés directement dans une guerre mondiale.|Durant la Guerra Freda, els EUA i l'URSS es van enfrontar directament en una guerra mundial."),
            false,
            Text.Of("No: they never fought each other directly; their rivalry played out through an arms race and wars in other countries.|No: nunca se enfrentaron directamente; su rivalidad se dio en la carrera armamentística y en guerras en otros países.|Non : ils ne se sont jamais affrontés directement ; leur rivalité passait par la course aux armements et des guerres dans d'autres pays.|No: mai no es van enfrontar directament; la seva rivalitat es va donar en la cursa armamentística i en guerres en altres països.")),
    ];

    private static readonly IReadOnlyList<TrueFalseItem> GeographyTrueFalse =
    [
        new("geography.paris", 1,
            Text.Of("Paris is the capital of France.|París es la capital de Francia.|Paris est la capitale de la France.|París és la capital de França."),
            true,
            Text.Of("Yes: Paris has been France's capital for centuries.|Sí: París es la capital de Francia desde hace siglos.|Oui : Paris est la capitale de la France depuis des siècles.|Sí: París és la capital de França des de fa segles.")),
        new("geography.continent", 1,
            Text.Of("Africa is the largest continent.|África es el continente más grande.|L'Afrique est le plus grand continent.|L'Àfrica és el continent més gran."),
            false,
            Text.Of("No: Asia is the largest continent; Africa is the second largest.|No: Asia es el continente más grande; África es el segundo más grande.|Non : l'Asie est le plus grand continent ; l'Afrique est le deuxième.|No: Àsia és el continent més gran; l'Àfrica és el segon més gran.")),
        new("geography.amazon", 2,
            Text.Of("The Amazon rainforest is located in South America.|La selva amazónica está ubicada en Sudamérica.|La forêt amazonienne se trouve en Amérique du Sud.|La selva amazònica es troba a Sud-amèrica."),
            true,
            Text.Of("Yes: the Amazon spans several South American countries, mostly Brazil.|Sí: el Amazonas se extiende por varios países sudamericanos, sobre todo Brasil.|Oui : l'Amazonie s'étend sur plusieurs pays d'Amérique du Sud, surtout le Brésil.|Sí: l'Amazones s'estén per diversos països sud-americans, sobretot el Brasil.")),
        new("geography.pacific", 2,
            Text.Of("The Pacific is the largest ocean on Earth.|El Pacífico es el océano más grande de la Tierra.|Le Pacifique est le plus grand océan de la Terre.|El Pacífic és l'oceà més gran de la Terra."),
            true,
            Text.Of("Yes: the Pacific Ocean covers more area than any other ocean.|Sí: el océano Pacífico cubre más superficie que cualquier otro océano.|Oui : l'océan Pacifique couvre plus de surface que tout autre océan.|Sí: l'oceà Pacífic cobreix més superfície que qualsevol altre oceà.")),
        new("geography.everest", 3,
            Text.Of("Mount Everest is the tallest mountain on Earth, measured from sea level.|El Everest es la montaña más alta de la Tierra, medida desde el nivel del mar.|L'Everest est la plus haute montagne de la Terre, mesurée depuis le niveau de la mer.|L'Everest és la muntanya més alta de la Terra, mesurada des del nivell del mar."),
            true,
            Text.Of("Yes: at 8,849 meters above sea level, Everest is the highest point on Earth.|Sí: con 8.849 metros sobre el nivel del mar, el Everest es el punto más alto de la Tierra.|Oui : avec 8 849 mètres au-dessus du niveau de la mer, l'Everest est le point le plus haut de la Terre.|Sí: amb 8.849 metres sobre el nivell del mar, l'Everest és el punt més alt de la Terra.")),
        // Task #97: more items per difficulty tier, so a drill repeats less often across sessions.
        new("geography.sunrise", 1,
            Text.Of("The Sun rises in the east.|El Sol sale por el este.|Le Soleil se lève à l'est.|El Sol surt per l'est."),
            true,
            Text.Of("Yes: because the Earth spins from west to east, the Sun appears in the east every morning.|Sí: como la Tierra gira de oeste a este, el Sol aparece por el este cada mañana.|Oui : comme la Terre tourne d'ouest en est, le Soleil apparaît à l'est chaque matin.|Sí: com que la Terra gira d'oest a est, el Sol apareix per l'est cada matí.")),
        new("geography.iberia", 2,
            Text.Of("Spain and Portugal share the Iberian Peninsula.|España y Portugal comparten la península ibérica.|L'Espagne et le Portugal se partagent la péninsule Ibérique.|Espanya i Portugal comparteixen la península Ibèrica."),
            true,
            Text.Of("Yes: the Iberian Peninsula is shared mainly by Spain and Portugal, plus Andorra and Gibraltar.|Sí: la península ibérica la comparten sobre todo España y Portugal, además de Andorra y Gibraltar.|Oui : la péninsule Ibérique est partagée surtout par l'Espagne et le Portugal, plus Andorre et Gibraltar.|Sí: la península Ibèrica la comparteixen sobretot Espanya i Portugal, a més d'Andorra i Gibraltar.")),
        new("geography.seasons", 3,
            Text.Of("Seasons happen because the Earth gets closer to and farther from the Sun.|Las estaciones se producen porque la Tierra se acerca y se aleja del Sol.|Les saisons existent parce que la Terre se rapproche et s'éloigne du Soleil.|Les estacions es produeixen perquè la Terra s'acosta i s'allunya del Sol."),
            false,
            Text.Of("No: seasons are caused by the tilt of the Earth's axis, which changes how directly sunlight reaches each hemisphere.|No: se deben a la inclinación del eje terrestre, que cambia cómo llega la luz del Sol a cada hemisferio.|Non : elles sont dues à l'inclinaison de l'axe de la Terre, qui change l'angle sous lequel le Soleil éclaire chaque hémisphère.|No: es deuen a la inclinació de l'eix terrestre, que canvia com arriba la llum del Sol a cada hemisferi.")),
        new("geography.hemispheres", 3,
            Text.Of("When it is summer in Spain, it is winter in Argentina.|Cuando es verano en España, es invierno en Argentina.|Quand c'est l'été en Espagne, c'est l'hiver en Argentine.|Quan és estiu a Espanya, és hivern a l'Argentina."),
            true,
            Text.Of("Yes: the two hemispheres have opposite seasons, because of the tilt of the Earth's axis.|Sí: los dos hemisferios tienen estaciones opuestas, por la inclinación del eje terrestre.|Oui : les deux hémisphères ont des saisons opposées, à cause de l'inclinaison de l'axe terrestre.|Sí: els dos hemisferis tenen estacions oposades, per la inclinació de l'eix terrestre.")),
    ];

    private static readonly IReadOnlyList<TrueFalseItem> EconomicsTrueFalse =
    [
        new("economics.scarcity", 1,
            Text.Of("Scarcity means there's an unlimited supply of resources.|La escasez significa que hay una oferta ilimitada de recursos.|La rareté signifie qu'il existe une offre illimitée de ressources.|L'escassetat significa que hi ha una oferta il·limitada de recursos."),
            false,
            Text.Of("No: scarcity means resources are limited compared to what people want.|No: la escasez significa que los recursos son limitados en comparación con lo que la gente quiere.|Non : la rareté signifie que les ressources sont limitées par rapport à ce que les gens veulent.|No: l'escassetat significa que els recursos són limitats en comparació amb el que la gent vol.")),
        new("economics.supply", 2,
            Text.Of("When supply increases and demand stays the same, prices tend to fall.|Cuando la oferta aumenta y la demanda se mantiene igual, los precios tienden a bajar.|Quand l'offre augmente et que la demande reste la même, les prix ont tendance à baisser.|Quan l'oferta augmenta i la demanda es manté igual, els preus tendeixen a baixar."),
            true,
            Text.Of("Yes: more supply with the same demand usually pushes prices down.|Sí: más oferta con la misma demanda suele hacer bajar los precios.|Oui : plus d'offre avec la même demande fait généralement baisser les prix.|Sí: més oferta amb la mateixa demanda sol fer baixar els preus.")),
        new("economics.inflation", 2,
            Text.Of("Inflation means prices are falling over time.|La inflación significa que los precios bajan con el tiempo.|L'inflation signifie que les prix baissent avec le temps.|La inflació significa que els preus baixen amb el temps."),
            false,
            Text.Of("No: inflation means prices are rising over time, so money buys less.|No: la inflación significa que los precios suben con el tiempo, así que el dinero compra menos.|Non : l'inflation signifie que les prix augmentent avec le temps, donc l'argent achète moins.|No: la inflació significa que els preus pugen amb el temps, així que els diners compren menys.")),
        new("economics.central_bank", 2,
            Text.Of("A country's central bank can set interest rates.|El banco central de un país puede fijar los tipos de interés.|La banque centrale d'un pays peut fixer les taux d'intérêt.|El banc central d'un país pot fixar els tipus d'interès."),
            true,
            Text.Of("Yes: setting interest rates is one of a central bank's main tools.|Sí: fijar los tipos de interés es una de las principales herramientas de un banco central.|Oui : fixer les taux d'intérêt est l'un des principaux outils d'une banque centrale.|Sí: fixar els tipus d'interès és una de les principals eines d'un banc central.")),
        new("economics.gdp", 3,
            Text.Of("GDP measures the total value of goods and services produced in a country.|El PIB mide el valor total de los bienes y servicios producidos en un país.|Le PIB mesure la valeur totale des biens et services produits dans un pays.|El PIB mesura el valor total dels béns i serveis produïts en un país."),
            true,
            Text.Of("Yes: Gross Domestic Product adds up the value of everything produced in a country, usually per year.|Sí: el Producto Interior Bruto suma el valor de todo lo producido en un país, normalmente por año.|Oui : le Produit intérieur brut additionne la valeur de tout ce qui est produit dans un pays, en général par an.|Sí: el Producte Interior Brut suma el valor de tot el que es produeix en un país, normalment per any.")),
        // Task #97: more items per difficulty tier, so a drill repeats less often across sessions.
        new("economics.interest", 3,
            Text.Of("If you borrow money, you usually have to pay it back with interest.|Si pides dinero prestado, normalmente tienes que devolverlo con intereses.|Si tu empruntes de l'argent, tu dois généralement le rembourser avec des intérêts.|Si demanes diners prestats, normalment els has de tornar amb interessos."),
            true,
            Text.Of("Yes: interest is the price of borrowing money.|Sí: los intereses son el precio de pedir dinero prestado.|Oui : les intérêts sont le prix de l'argent emprunté.|Sí: els interessos són el preu de demanar diners prestats.")),
        new("economics.unemployment_rate", 3,
            Text.Of("The unemployment rate is the share of the whole population that has no job.|La tasa de paro es el porcentaje de toda la población que no tiene trabajo.|Le taux de chômage est la part de toute la population qui n'a pas d'emploi.|La taxa d'atur és el percentatge de tota la població que no té feina."),
            false,
            Text.Of("No: it is measured over the labour force (people working or looking for work), not the whole population, which includes children and retirees.|No: se calcula sobre la población activa (quien trabaja o busca trabajo), no sobre toda la población, que incluye niños y jubilados.|Non : il se calcule sur la population active (qui travaille ou cherche un emploi), pas sur toute la population, qui inclut enfants et retraités.|No: es calcula sobre la població activa (qui treballa o busca feina), no sobre tota la població, que inclou infants i jubilats.")),
        new("economics.vat", 3,
            Text.Of("VAT is a tax paid by the final consumer when buying goods and services.|El IVA es un impuesto que paga el consumidor final al comprar bienes y servicios.|La TVA est un impôt payé par le consommateur final lorsqu'il achète des biens et des services.|L'IVA és un impost que paga el consumidor final quan compra béns i serveis."),
            true,
            Text.Of("Yes: VAT is included in the price, so the consumer pays it and businesses pass it on to the state.|Sí: el IVA va incluido en el precio, así que lo paga el consumidor y las empresas lo entregan a Hacienda.|Oui : la TVA est incluse dans le prix ; c'est le consommateur qui la paie et les entreprises la reversent à l'État.|Sí: l'IVA va inclòs en el preu, així que el paga el consumidor i les empreses el lliuren a Hisenda.")),
        new("economics.diversification", 3,
            Text.Of("Putting all your savings into one company's shares is the safest way to invest.|Poner todos tus ahorros en acciones de una sola empresa es la forma más segura de invertir.|Placer toutes ses économies dans les actions d'une seule entreprise est la façon la plus sûre d'investir.|Posar tots els estalvis en accions d'una sola empresa és la manera més segura d'invertir."),
            false,
            Text.Of("No: spreading money across many investments (diversifying) lowers the risk of losing a lot at once.|No: repartir el dinero entre muchas inversiones (diversificar) reduce el riesgo de perder mucho de golpe.|Non : répartir son argent entre de nombreux placements (diversifier) réduit le risque de beaucoup perdre d'un coup.|No: repartir els diners entre moltes inversions (diversificar) redueix el risc de perdre'n molts de cop.")),
    ];

    private static readonly IReadOnlyList<TrueFalseItem> ProgrammingTrueFalse =
    [
        new("programming.loop", 1,
            Text.Of("A loop lets code repeat multiple times.|Un bucle permite que el código se repita varias veces.|Une boucle permet au code de se répéter plusieurs fois.|Un bucle permet que el codi es repeteixi diverses vegades."),
            true,
            Text.Of("Yes: loops (like 'for' or 'while') run the same code again without rewriting it each time.|Sí: los bucles (como 'for' o 'while') ejecutan el mismo código otra vez sin tener que reescribirlo cada vez.|Oui : les boucles (comme 'for' ou 'while') exécutent le même code à nouveau sans le réécrire à chaque fois.|Sí: els bucles (com 'for' o 'while') executen el mateix codi una altra vegada sense haver-lo de reescriure cada cop.")),
        new("programming.variable", 1,
            Text.Of("A variable can only ever store one value for the whole life of a program.|Una variable solo puede almacenar un valor durante toda la vida de un programa.|Une variable ne peut stocker qu'une seule valeur pendant toute la vie d'un programme.|Una variable només pot emmagatzemar un valor durant tota la vida d'un programa."),
            false,
            Text.Of("No: a variable's value can be changed (reassigned) as many times as the program needs.|No: el valor de una variable se puede cambiar (reasignar) tantas veces como el programa necesite.|Non : la valeur d'une variable peut être modifiée (réaffectée) autant de fois que le programme en a besoin.|No: el valor d'una variable es pot canviar (reassignar) tantes vegades com el programa necessiti.")),
        new("programming.bug", 2,
            Text.Of("A \"bug\" in programming is a mistake that makes a program behave incorrectly.|Un \"bug\" en programación es un error que hace que un programa se comporte de forma incorrecta.|Un \"bug\" en programmation est une erreur qui fait que le programme se comporte mal.|Un \"bug\" en programació és un error que fa que un programa es comporti de manera incorrecta."),
            true,
            Text.Of("Yes: a bug is any mistake in the code that causes unexpected or wrong behavior.|Sí: un bug es cualquier error en el código que provoca un comportamiento inesperado o incorrecto.|Oui : un bug est toute erreur dans le code qui provoque un comportement inattendu ou incorrect.|Sí: un bug és qualsevol error en el codi que provoca un comportament inesperat o incorrecte.")),
        new("programming.binary", 2,
            Text.Of("Computers store data internally using only 0s and 1s.|Los ordenadores almacenan los datos internamente usando solo 0 y 1.|Les ordinateurs stockent les données en interne en utilisant seulement des 0 et des 1.|Els ordinadors emmagatzemen les dades internament fent servir només 0 i 1."),
            true,
            Text.Of("Yes: that's binary code — every piece of data a computer handles is ultimately 0s and 1s.|Sí: eso es el código binario; todo dato que maneja un ordenador es, en el fondo, 0 y 1.|Oui : c'est le code binaire ; toute donnée qu'un ordinateur traite est, au fond, des 0 et des 1.|Sí: això és el codi binari; tota dada que gestiona un ordinador és, al fons, 0 i 1.")),
        new("programming.html", 3,
            Text.Of("HTML is a programming language.|HTML es un lenguaje de programación.|HTML est un langage de programmation.|HTML és un llenguatge de programació."),
            false,
            Text.Of("No: HTML is a markup language — it describes a page's structure, but it can't do logic like loops or conditions on its own.|No: HTML es un lenguaje de marcado; describe la estructura de una página, pero no puede hacer lógica como bucles o condiciones por sí solo.|Non : HTML est un langage de balisage ; il décrit la structure d'une page, mais ne peut pas faire de logique comme des boucles ou des conditions à lui seul.|No: HTML és un llenguatge de marcatge; descriu l'estructura d'una pàgina, però no pot fer lògica com bucles o condicions per si sol.")),
    ];

    private static readonly Dictionary<Area, IReadOnlyList<TrueFalseItem>> Banks = new()
    {
        [Area.Mathematics] = MathTrueFalse,
        [Area.Physics] = PhysicsTrueFalse,
        [Area.Chemistry] = ChemistryTrueFalse,
        [Area.Biology] = BiologyTrueFalse,
        [Area.History] = HistoryTrueFalse,
        [Area.Geography] = GeographyTrueFalse,
        [Area.Economics] = EconomicsTrueFalse,
        [Area.Programming] = ProgrammingTrueFalse,
    };

    /// <summary>The 3 difficulty tiers every bank is organized into, from a subject's level.</summary>
    private static int DifficultyFor(Level level) => level switch
    {
        Level.EarlyYears or Level.Primary1 or Level.Primary2 => 1,
        Level.Primary3 or Level.Secondary1 => 2,
        _ => 3,
    };

    /// <summary>Drops items already shown this attempt (keyed "<paramref name="kind"/>|Topic", built by the caller
    /// from <c>QuizAttempt.UsedJson</c> — task #95/#97's fix for a drill repeating too soon within one test, which
    /// bit hardest exactly where a tier's pool is still thin). Falls back to the full pool once every item in it
    /// has been seen, rather than ever returning nothing — a repeat late in a long session beats no drill at all.</summary>
    private static IReadOnlyList<T> Unseen<T>(IReadOnlyList<T> pool, Func<T, string> topicOf, string kind, IReadOnlySet<string>? exclude)
    {
        if (exclude is not { Count: > 0 } || pool.Count == 0)
        {
            return pool;
        }

        var unseen = pool.Where(i => !exclude.Contains($"{kind}|{topicOf(i)}")).ToList();
        return unseen.Count > 0 ? unseen : pool;
    }

    /// <summary>A true/false drill for the given subject and level, or null if the bank has none yet
    /// (today: Language and ForeignLanguage, where fill-blank/reorder/match fit more naturally — task #71).</summary>
    public static PracticeExercise? TrueFalse(Area area, Level level, TeachLanguage language, int seed, IReadOnlySet<string>? exclude = null)
    {
        if (!Banks.TryGetValue(area, out var bank))
        {
            return null;
        }

        var pool = Unseen(bank.Where(i => i.Difficulty == DifficultyFor(level)).ToList(), i => i.Topic, "TrueFalse", exclude);
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

    // ---- Language & ForeignLanguage: fill-blank, reorder and match (task #71) ----

    /// <summary>"English" / "Spanish" etc., in the student's own interface language — the only two foreign-language
    /// targets this app teaches (<see cref="QuestionBanks.ForeignTarget"/>).</summary>
    private static string TargetName(TeachLanguage language) =>
        (QuestionBanks.ForeignTarget(language) == TeachLanguage.English
            ? Text.Of("English|inglés|anglais|anglès")
            : Text.Of("Spanish|español|espagnol|castellà")).In(language);

    /// <summary>Fisher-Yates, seeded so the same drill always looks the same way (the student submits by text, not
    /// position, so a different shuffle between two views of the same drill — e.g. a page reload — is harmless).</summary>
    private static List<string> Shuffled(IReadOnlyList<string> items, int seed)
    {
        var copy = items.ToList();
        var rng = new Random(seed);
        for (var i = copy.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (copy[i], copy[j]) = (copy[j], copy[i]);
        }
        return copy;
    }

    private sealed record FillBlankItem(string Topic, int Difficulty, Text Prompt, Text Answer, Text Explanation);

    private static readonly IReadOnlyList<FillBlankItem> LanguageFillBlank =
    [
        new("language.opposites", 1,
            Text.Of("The opposite of \"hot\" is ___.|Lo contrario de \"caliente\" es ___.|Le contraire de « chaud » est ___.|El contrari de \"calent\" és ___."),
            Text.Of("cold|frío|froid|fred"),
            Text.Of("\"Cold\" is the opposite of \"hot\".|\"Frío\" es lo contrario de \"caliente\".|« Froid » est le contraire de « chaud ».|\"Fred\" és el contrari de \"calent\".")),
        new("language.parts_of_speech", 2,
            Text.Of("A word that describes a noun is called an ___.|Una palabra que describe a un sustantivo se llama ___.|Un mot qui décrit un nom s'appelle un ___.|Una paraula que descriu un nom s'anomena ___."),
            Text.Of("adjective|adjetivo|adjectif|adjectiu"),
            Text.Of("Words like \"blue\" or \"tall\" that describe a noun are adjectives.|Palabras como \"azul\" o \"alto\" que describen un sustantivo son adjetivos.|Des mots comme « bleu » ou « grand » qui décrivent un nom sont des adjectifs.|Paraules com \"blau\" o \"alt\" que descriuen un nom són adjectius.")),
        new("language.literary_devices", 3,
            Text.Of("When a writer compares two things using \"like\" or \"as\", it's called a ___.|Cuando un escritor compara dos cosas usando \"como\", se llama ___.|Quand un auteur compare deux choses avec « comme », on appelle ça une ___.|Quan un escriptor compara dues coses fent servir \"com\", se'n diu ___."),
            Text.Of("simile|símil|comparaison|símil"),
            Text.Of("\"As brave as a lion\" compares two things with \"as\": that's a simile.|\"Tan valiente como un león\" compara dos cosas con \"como\": eso es un símil.|« Aussi courageux qu'un lion » compare deux choses avec « comme » : c'est une comparaison.|\"Tan valent com un lleó\" compara dues coses amb \"com\": això és un símil.")),
    ];

    private static readonly IReadOnlyList<FillBlankItem> ForeignFillBlankWords =
    [
        new("foreign.vocabulary", 1, Text.Of("dog|perro|chien|gos"), Text.Of("dog|perro|chien|gos"), Text.Of("")),
        new("foreign.vocabulary", 2, Text.Of("house|casa|maison|casa"), Text.Of("house|casa|maison|casa"), Text.Of("")),
        new("foreign.vocabulary", 3, Text.Of("friend|amigo|ami|amic"), Text.Of("friend|amigo|ami|amic"), Text.Of("")),
    ];

    private static readonly Text ForeignFillBlankPrompt =
        Text.Of("How do you say \"{0}\" in {1}?|¿Cómo se dice \"{0}\" en {1}?|Comment dit-on « {0} » en {1} ?|Com es diu «{0}» en {1}?");

    private static readonly Text ForeignFillBlankExplanation =
        Text.Of("\"{0}\" in {1} is \"{2}\".|\"{0}\" en {1} es \"{2}\".|« {0} » en {1} se dit « {2} ».|\"{0}\" en {1} és \"{2}\".");

    // ---- Fill-blank content for the other 8 subjects (task #73) ----

    private static readonly IReadOnlyList<FillBlankItem> MathFillBlank =
    [
        new("math.arithmetic", 1,
            Text.Of("The result of 5 + 5 is ___.|El resultado de 5 + 5 es ___.|Le résultat de 5 + 5 est ___.|El resultat de 5 + 5 és ___."),
            Text.Of("10|10|10|10"),
            Text.Of("5 + 5 = 10.|5 + 5 = 10.|5 + 5 = 10.|5 + 5 = 10.")),
        new("math.shapes", 2,
            Text.Of("A triangle has ___ sides.|Un triángulo tiene ___ lados.|Un triangle a ___ côtés.|Un triangle té ___ costats."),
            Text.Of("three|tres|trois|tres"),
            Text.Of("A triangle always has three sides.|Un triángulo siempre tiene tres lados.|Un triangle a toujours trois côtés.|Un triangle sempre té tres costats.")),
        new("math.roots", 3,
            Text.Of("The square root of 81 is ___.|La raíz cuadrada de 81 es ___.|La racine carrée de 81 est ___.|L'arrel quadrada de 81 és ___."),
            Text.Of("9|9|9|9"),
            Text.Of("9 × 9 = 81, so the square root of 81 is 9.|9 × 9 = 81, así que la raíz cuadrada de 81 es 9.|9 × 9 = 81, donc la racine carrée de 81 est 9.|9 × 9 = 81, així que l'arrel quadrada de 81 és 9.")),
    ];

    private static readonly IReadOnlyList<FillBlankItem> PhysicsFillBlank =
    [
        new("physics.sun", 1,
            Text.Of("The Sun gives us light and ___.|El Sol nos da luz y ___.|Le Soleil nous donne de la lumière et de la ___.|El Sol ens dona llum i ___."),
            Text.Of("heat|calor|chaleur|calor"),
            Text.Of("The Sun gives off both light and heat.|El Sol emite tanto luz como calor.|Le Soleil émet à la fois de la lumière et de la chaleur.|El Sol emet tant llum com calor.")),
        new("physics.gravity", 2,
            Text.Of("The force that pulls objects toward the Earth is called ___.|La fuerza que atrae los objetos hacia la Tierra se llama ___.|La force qui attire les objets vers la Terre s'appelle la ___.|La força que atrau els objectes cap a la Terra s'anomena ___."),
            Text.Of("gravity|gravedad|gravité|gravetat"),
            Text.Of("Gravity is the force that pulls objects down toward Earth.|La gravedad es la fuerza que atrae los objetos hacia la Tierra.|La gravité est la force qui attire les objets vers la Terre.|La gravetat és la força que atrau els objectes cap a la Terra.")),
        new("physics.units", 3,
            Text.Of("The unit used to measure electric current is the ___.|La unidad usada para medir la corriente eléctrica es el ___.|L'unité utilisée pour mesurer le courant électrique est l'___.|La unitat usada per mesurar el corrent elèctric és l'___."),
            Text.Of("ampere|amperio|ampère|amper"),
            Text.Of("Electric current is measured in amperes (amps).|La corriente eléctrica se mide en amperios.|Le courant électrique se mesure en ampères.|El corrent elèctric es mesura en ampers.")),
    ];

    private static readonly IReadOnlyList<FillBlankItem> ChemistryFillBlank =
    [
        new("chemistry.symbols", 1,
            Text.Of("The chemical symbol for oxygen is ___.|El símbolo químico del oxígeno es ___.|Le symbole chimique de l'oxygène est ___.|El símbol químic de l'oxigen és ___."),
            Text.Of("O|O|O|O"),
            Text.Of("Oxygen's chemical symbol is the single letter O.|El símbolo químico del oxígeno es la letra O.|Le symbole chimique de l'oxygène est la lettre O.|El símbol químic de l'oxigen és la lletra O.")),
        new("chemistry.states", 2,
            Text.Of("When a solid turns directly into a gas, it's called ___.|Cuando un sólido se convierte directamente en gas, se llama ___.|Quand un solide se transforme directement en gaz, on appelle ça la ___.|Quan un sòlid es converteix directament en gas, s'anomena ___."),
            Text.Of("sublimation|sublimación|sublimation|sublimació"),
            Text.Of("Sublimation skips the liquid state entirely, like dry ice turning into gas.|La sublimación se salta por completo el estado líquido, como el hielo seco al convertirse en gas.|La sublimation saute complètement l'état liquide, comme la glace carbonique qui se transforme en gaz.|La sublimació es salta completament l'estat líquid, com el gel sec en convertir-se en gas.")),
        new("chemistry.atom", 3,
            Text.Of("The smallest unit of a chemical element is the ___.|La unidad más pequeña de un elemento químico es el ___.|La plus petite unité d'un élément chimique est l'___.|La unitat més petita d'un element químic és l'___."),
            Text.Of("atom|átomo|atome|àtom"),
            Text.Of("Atoms are the basic building blocks of all matter.|Los átomos son los bloques básicos de toda la materia.|Les atomes sont les éléments de base de toute matière.|Els àtoms són els blocs bàsics de tota la matèria.")),
    ];

    private static readonly IReadOnlyList<FillBlankItem> BiologyFillBlank =
    [
        new("biology.heart", 1,
            Text.Of("The organ that pumps blood around the body is the ___.|El órgano que bombea la sangre por el cuerpo es el ___.|L'organe qui pompe le sang dans le corps est le ___.|L'òrgan que bombeja la sang pel cos és el ___."),
            Text.Of("heart|corazón|cœur|cor"),
            Text.Of("The heart pumps blood through the whole body.|El corazón bombea la sangre por todo el cuerpo.|Le cœur pompe le sang dans tout le corps.|El cor bombeja la sang per tot el cos.")),
        new("biology.photosynthesis", 2,
            Text.Of("Plants take in carbon dioxide and release ___.|Las plantas absorben dióxido de carbono y liberan ___.|Les plantes absorbent du dioxyde de carbone et libèrent de l'___.|Les plantes absorbeixen diòxid de carboni i alliberen ___."),
            Text.Of("oxygen|oxígeno|oxygène|oxigen"),
            Text.Of("During photosynthesis, plants absorb CO2 and release oxygen.|Durante la fotosíntesis, las plantas absorben CO2 y liberan oxígeno.|Pendant la photosynthèse, les plantes absorbent le CO2 et libèrent de l'oxygène.|Durant la fotosíntesi, les plantes absorbeixen CO2 i alliberen oxigen.")),
        new("biology.genetics", 3,
            Text.Of("The basic unit of heredity is called a ___.|La unidad básica de la herencia se llama ___.|L'unité de base de l'hérédité s'appelle un ___.|La unitat bàsica de l'herència s'anomena un ___."),
            Text.Of("gene|gen|gène|gen"),
            Text.Of("Genes are the units of DNA that carry hereditary information.|Los genes son las unidades de ADN que llevan la información hereditaria.|Les gènes sont les unités d'ADN qui portent l'information héréditaire.|Els gens són les unitats d'ADN que porten la informació hereditària.")),
    ];

    private static readonly IReadOnlyList<FillBlankItem> HistoryFillBlank =
    [
        new("history.egypt", 1,
            Text.Of("Ancient Egyptian kings were called ___.|Los reyes del antiguo Egipto se llamaban ___.|Les rois de l'Égypte antique s'appelaient ___.|Els reis de l'antic Egipte s'anomenaven ___."),
            Text.Of("pharaohs|faraones|pharaons|faraons"),
            Text.Of("Pharaohs were the rulers of ancient Egypt.|Los faraones eran los gobernantes del antiguo Egipto.|Les pharaons étaient les dirigeants de l'Égypte antique.|Els faraons eren els governants de l'antic Egipte.")),
        new("history.titanic", 2,
            Text.Of("The ship that sank in 1912 after hitting an iceberg was called the ___.|El barco que se hundió en 1912 tras chocar con un iceberg se llamaba el ___.|Le navire qui a coulé en 1912 après avoir heurté un iceberg s'appelait le ___.|El vaixell que es va enfonsar el 1912 després de xocar amb un iceberg es deia el ___."),
            Text.Of("Titanic|Titanic|Titanic|Titanic"),
            Text.Of("The Titanic sank in 1912 on its first voyage.|El Titanic se hundió en 1912 en su primer viaje.|Le Titanic a coulé en 1912 lors de son premier voyage.|El Titanic es va enfonsar el 1912 en el seu primer viatge.")),
        new("history.declaration", 3,
            Text.Of("The 1776 document that declared the United States independent is called the Declaration of ___.|El documento de 1776 que declaró la independencia de Estados Unidos se llama la Declaración de ___.|Le document de 1776 qui a déclaré l'indépendance des États-Unis s'appelle la déclaration d'___.|El document de 1776 que va declarar la independència dels Estats Units s'anomena la Declaració d'___."),
            Text.Of("Independence|Independencia|Indépendance|Independència"),
            Text.Of("The Declaration of Independence was adopted on July 4, 1776.|La Declaración de Independencia se adoptó el 4 de julio de 1776.|La déclaration d'Indépendance a été adoptée le 4 juillet 1776.|La Declaració d'Independència es va adoptar el 4 de juliol de 1776.")),
        // Task #97: more items per difficulty tier, so a drill repeats less often across sessions.
        new("history.castles", 1,
            Text.Of("In the Middle Ages, lords lived in stone ___.|En la Edad Media, los señores vivían en ___ de piedra.|Au Moyen Âge, les seigneurs vivaient dans des ___ forts en pierre.|A l'edat mitjana, els senyors vivien en ___ de pedra."),
            Text.Of("castles|castillos|châteaux|castells"),
            Text.Of("Castles protected lords and their people from attacks.|Los castillos protegían a los señores y a su gente de los ataques.|Les châteaux forts protégeaient les seigneurs et leurs gens des attaques.|Els castells protegien els senyors i la seva gent dels atacs.")),
        new("history.gutenberg", 2,
            Text.Of("Gutenberg invented a printing press with movable ___.|Gutenberg inventó una imprenta de ___ móviles.|Gutenberg a inventé une presse à ___ mobiles.|Gutenberg va inventar una impremta de ___ mòbils."),
            Text.Of("type|tipos|caractères|tipus"),
            Text.Of("Movable type let printers reuse letters to print many books quickly.|Los tipos móviles permitían reutilizar las letras para imprimir muchos libros rápidamente.|Les caractères mobiles permettaient de réutiliser les lettres pour imprimer vite de nombreux livres.|Els tipus mòbils permetien reutilitzar les lletres per imprimir molts llibres ràpidament.")),
        new("history.industrial", 3,
            Text.Of("The 18th-century change from handwork to machines is called the Industrial ___.|El paso del trabajo manual a las máquinas en el siglo XVIII se llama la Revolución ___.|Le passage du travail manuel aux machines au XVIIIᵉ siècle s'appelle la révolution ___.|El pas del treball manual a les màquines al segle XVIII s'anomena la Revolució ___."),
            Text.Of("Revolution|Industrial|industrielle|Industrial"),
            Text.Of("It began in Britain with steam engines and textile factories.|Empezó en Gran Bretaña con las máquinas de vapor y las fábricas textiles.|Elle a commencé en Grande-Bretagne avec les machines à vapeur et les usines textiles.|Va començar a la Gran Bretanya amb les màquines de vapor i les fàbriques tèxtils.")),
        new("history.vassal", 3,
            Text.Of("In the Middle Ages, someone who received land from a lord in exchange for loyalty was a ___.|En la Edad Media, quien recibía tierras de un señor a cambio de fidelidad era un ___.|Au Moyen Âge, celui qui recevait une terre d'un seigneur en échange de sa fidélité était un ___.|A l'edat mitjana, qui rebia terres d'un senyor a canvi de fidelitat era un ___."),
            Text.Of("vassal|vasallo|vassal|vassall"),
            Text.Of("This bond between lord and vassal was the basis of feudalism.|Este vínculo entre señor y vasallo era la base del feudalismo.|Ce lien entre seigneur et vassal était la base de la féodalité.|Aquest vincle entre senyor i vassall era la base del feudalisme.")),
    ];

    private static readonly IReadOnlyList<FillBlankItem> GeographyFillBlank =
    [
        new("geography.pacific", 1,
            Text.Of("The largest ocean on Earth is the ___.|El océano más grande de la Tierra es el ___.|Le plus grand océan de la Terre est le ___.|L'oceà més gran de la Terra és el ___."),
            Text.Of("Pacific|Pacífico|Pacifique|Pacífic"),
            Text.Of("The Pacific Ocean is the largest and deepest ocean on Earth.|El océano Pacífico es el más grande y profundo de la Tierra.|L'océan Pacifique est le plus grand et le plus profond de la Terre.|L'oceà Pacífic és el més gran i profund de la Terra.")),
        new("geography.nile", 2,
            Text.Of("The longest river in the world is the ___.|El río más largo del mundo es el ___.|Le fleuve le plus long du monde est le ___.|El riu més llarg del món és el ___."),
            Text.Of("Nile|Nilo|Nil|Nil"),
            Text.Of("The Nile River, in Africa, is generally considered the longest river in the world.|El río Nilo, en África, se considera generalmente el más largo del mundo.|Le fleuve Nil, en Afrique, est généralement considéré comme le plus long du monde.|El riu Nil, a l'Àfrica, es considera generalment el més llarg del món.")),
        new("geography.equator", 3,
            Text.Of("The imaginary line around the middle of the Earth is called the ___.|La línea imaginaria alrededor de la mitad de la Tierra se llama el ___.|La ligne imaginaire autour du milieu de la Terre s'appelle l'___.|La línia imaginària al voltant de la meitat de la Terra s'anomena l'___."),
            Text.Of("equator|ecuador|équateur|equador"),
            Text.Of("The equator divides the Earth into the Northern and Southern hemispheres.|El ecuador divide la Tierra en el hemisferio norte y el hemisferio sur.|L'équateur divise la Terre en hémisphère nord et hémisphère sud.|L'equador divideix la Terra en hemisferi nord i hemisferi sud.")),
        // Task #97: more items per difficulty tier, so a drill repeats less often across sessions.
        new("geography.north", 1,
            Text.Of("On most maps, the top points ___.|En la mayoría de los mapas, la parte de arriba señala el ___.|Sur la plupart des cartes, le haut indique le ___.|En la majoria de mapes, la part de dalt assenyala el ___."),
            Text.Of("north|norte|nord|nord"),
            Text.Of("Most maps put north at the top; the compass rose shows the directions.|La mayoría de los mapas ponen el norte arriba; la rosa de los vientos muestra las direcciones.|La plupart des cartes placent le nord en haut ; la rose des vents indique les directions.|La majoria de mapes posen el nord a dalt; la rosa dels vents mostra les direccions.")),
        new("geography.sahara", 2,
            Text.Of("The largest hot desert in the world is the ___.|El mayor desierto cálido del mundo es el ___.|Le plus grand désert chaud du monde est le ___.|El desert càlid més gran del món és el ___."),
            Text.Of("Sahara|Sáhara|Sahara|Sàhara"),
            Text.Of("The Sahara covers much of North Africa.|El Sáhara cubre gran parte del norte de África.|Le Sahara couvre une grande partie de l'Afrique du Nord.|El Sàhara cobreix gran part del nord de l'Àfrica.")),
        new("geography.meridians", 3,
            Text.Of("Lines that run from the North Pole to the South Pole are called ___.|Las líneas que van del Polo Norte al Polo Sur se llaman ___.|Les lignes qui vont du pôle Nord au pôle Sud s'appellent des ___.|Les línies que van del pol Nord al pol Sud s'anomenen ___."),
            Text.Of("meridians|meridianos|méridiens|meridians"),
            Text.Of("Meridians measure longitude; the prime meridian passes through Greenwich.|Los meridianos miden la longitud; el meridiano cero pasa por Greenwich.|Les méridiens mesurent la longitude ; le méridien d'origine passe par Greenwich.|Els meridians mesuren la longitud; el meridià zero passa per Greenwich.")),
        new("geography.delta", 3,
            Text.Of("The fan-shaped area where a river splits before reaching the sea is a ___.|La zona en forma de abanico donde un río se divide antes de llegar al mar es un ___.|La zone en éventail où un fleuve se divise avant d'atteindre la mer est un ___.|La zona en forma de ventall on un riu es divideix abans d'arribar al mar és un ___."),
            Text.Of("delta"),
            Text.Of("The Ebro and the Nile both end in a delta.|El Ebro y el Nilo acaban en un delta.|L'Èbre et le Nil se terminent tous deux par un delta.|L'Ebre i el Nil acaben en un delta.")),
    ];

    private static readonly IReadOnlyList<FillBlankItem> EconomicsFillBlank =
    [
        new("economics.income", 1,
            Text.Of("Money that a person earns from working is called ___.|El dinero que una persona gana trabajando se llama ___.|L'argent qu'une personne gagne en travaillant s'appelle le ___.|Els diners que una persona guanya treballant s'anomenen ___."),
            Text.Of("income|ingresos|revenu|ingressos"),
            Text.Of("Income is the money a person receives, usually from work.|Los ingresos son el dinero que una persona recibe, normalmente del trabajo.|Le revenu est l'argent qu'une personne reçoit, généralement du travail.|Els ingressos són els diners que una persona rep, normalment de la feina.")),
        new("economics.budget", 2,
            Text.Of("A plan for how to spend and save money is called a ___.|Un plan para gastar y ahorrar dinero se llama un ___.|Un plan pour dépenser et épargner de l'argent s'appelle un ___.|Un pla per gastar i estalviar diners s'anomena un ___."),
            Text.Of("budget|presupuesto|budget|pressupost"),
            Text.Of("A budget helps plan income and expenses.|Un presupuesto ayuda a planificar los ingresos y los gastos.|Un budget aide à planifier les revenus et les dépenses.|Un pressupost ajuda a planificar els ingressos i les despeses.")),
        new("economics.monopoly", 3,
            Text.Of("A market where only one company sells a product is called a ___.|Un mercado donde solo una empresa vende un producto se llama un ___.|Un marché où une seule entreprise vend un produit s'appelle un ___.|Un mercat on només una empresa ven un producte s'anomena un ___."),
            Text.Of("monopoly|monopolio|monopole|monopoli"),
            Text.Of("In a monopoly, one company controls the whole market for a product.|En un monopolio, una sola empresa controla todo el mercado de un producto.|Dans un monopole, une seule entreprise contrôle tout le marché d'un produit.|En un monopoli, una sola empresa controla tot el mercat d'un producte.")),
        // Task #97: more items per difficulty tier, so a drill repeats less often across sessions.
        new("economics.inflation_word", 3,
            Text.Of("A general rise in prices over time is called ___.|Una subida general de los precios a lo largo del tiempo se llama ___.|Une hausse générale des prix au fil du temps s'appelle l'___.|Una pujada general dels preus al llarg del temps s'anomena ___."),
            Text.Of("inflation|inflación|inflation|inflació"),
            Text.Of("With inflation, the same money buys fewer things.|Con inflación, el mismo dinero compra menos cosas.|Avec l'inflation, le même argent achète moins de choses.|Amb inflació, els mateixos diners compren menys coses.")),
        new("economics.tax", 3,
            Text.Of("A compulsory payment that people and companies make to the state is a ___.|Un pago obligatorio que personas y empresas hacen al Estado es un ___.|Un paiement obligatoire que les personnes et les entreprises versent à l'État est un ___.|Un pagament obligatori que persones i empreses fan a l'Estat és un ___."),
            Text.Of("tax|impuesto|impôt|impost"),
            Text.Of("Taxes pay for public services such as schools, hospitals and roads.|Los impuestos pagan servicios públicos como escuelas, hospitales y carreteras.|Les impôts financent les services publics comme les écoles, les hôpitaux et les routes.|Els impostos paguen serveis públics com escoles, hospitals i carreteres.")),
        new("economics.exports", 3,
            Text.Of("Goods that a country sells abroad are called ___.|Los bienes que un país vende al extranjero se llaman ___.|Les biens qu'un pays vend à l'étranger s'appellent des ___.|Els béns que un país ven a l'estranger s'anomenen ___."),
            Text.Of("exports|exportaciones|exportations|exportacions"),
            Text.Of("Exports bring money into a country; imports are goods bought from abroad.|Las exportaciones traen dinero al país; las importaciones son bienes comprados fuera.|Les exportations font entrer de l'argent dans le pays ; les importations sont des biens achetés à l'étranger.|Les exportacions porten diners al país; les importacions són béns comprats a fora.")),
        new("economics.entrepreneur", 3,
            Text.Of("A person who starts their own business is an ___.|Una persona que crea su propio negocio es un ___.|Une personne qui crée sa propre entreprise est un ___.|Una persona que crea el seu propi negoci és un ___."),
            Text.Of("entrepreneur|emprendedor|entrepreneur|emprenedor"),
            Text.Of("Entrepreneurs take risks to turn an idea into a business.|Los emprendedores asumen riesgos para convertir una idea en un negocio.|Les entrepreneurs prennent des risques pour transformer une idée en entreprise.|Els emprenedors assumeixen riscos per convertir una idea en un negoci.")),
    ];

    private static readonly IReadOnlyList<FillBlankItem> ProgrammingFillBlank =
    [
        new("programming.algorithm", 1,
            Text.Of("A list of step-by-step instructions for a computer is called an ___.|Una lista de instrucciones paso a paso para un ordenador se llama un ___.|Une liste d'instructions étape par étape pour un ordinateur s'appelle un ___.|Una llista d'instruccions pas a pas per a un ordinador s'anomena un ___."),
            Text.Of("algorithm|algoritmo|algorithme|algorisme"),
            Text.Of("An algorithm is a sequence of steps that solves a problem.|Un algoritmo es una secuencia de pasos que resuelve un problema.|Un algorithme est une suite d'étapes qui résout un problème.|Un algorisme és una seqüència de passos que resol un problema.")),
        new("programming.function", 2,
            Text.Of("A piece of reusable code that performs a task is called a ___.|Un fragmento de código reutilizable que realiza una tarea se llama una ___.|Un morceau de code réutilisable qui effectue une tâche s'appelle une ___.|Un fragment de codi reutilitzable que fa una tasca s'anomena una ___."),
            Text.Of("function|función|fonction|funció"),
            Text.Of("A function groups code so it can be reused without rewriting it.|Una función agrupa código para poder reutilizarlo sin reescribirlo.|Une fonction regroupe du code pour pouvoir le réutiliser sans le réécrire.|Una funció agrupa codi perquè es pugui reutilitzar sense reescriure'l.")),
        new("programming.loop", 3,
            Text.Of("An instruction that repeats only while a condition is true is called a ___ loop.|Una instrucción que se repite solo mientras una condición es verdadera se llama un bucle ___.|Une instruction qui se répète seulement tant qu'une condition est vraie s'appelle une boucle ___.|Una instrucció que es repeteix només mentre una condició és certa s'anomena un bucle ___."),
            Text.Of("while|while|while|while"),
            Text.Of("A \"while\" loop keeps repeating as long as its condition stays true.|Un bucle \"while\" sigue repitiéndose mientras su condición sea verdadera.|Une boucle \"while\" continue de se répéter tant que sa condition reste vraie.|Un bucle \"while\" segueix repetint-se mentre la seva condició sigui certa.")),
    ];

    private static readonly Dictionary<Area, IReadOnlyList<FillBlankItem>> FillBlankBanks = new()
    {
        [Area.Mathematics] = MathFillBlank,
        [Area.Physics] = PhysicsFillBlank,
        [Area.Chemistry] = ChemistryFillBlank,
        [Area.Biology] = BiologyFillBlank,
        [Area.History] = HistoryFillBlank,
        [Area.Geography] = GeographyFillBlank,
        [Area.Economics] = EconomicsFillBlank,
        [Area.Programming] = ProgrammingFillBlank,
    };

    /// <summary>A fill-in-the-blank drill, or null if the bank has none for this subject/level.</summary>
    public static PracticeExercise? FillBlank(Area area, Level level, TeachLanguage language, int seed, IReadOnlySet<string>? exclude = null)
    {
        var difficulty = DifficultyFor(level);
        if (FillBlankBanks.TryGetValue(area, out var genericBank))
        {
            var genericPool = Unseen(genericBank.Where(i => i.Difficulty == difficulty).ToList(), i => i.Topic, "FillBlank", exclude);
            if (genericPool.Count == 0)
            {
                return null;
            }

            var genericItem = genericPool[new Random(seed).Next(genericPool.Count)];
            return new PracticeExercise(genericItem.Topic, genericItem.Difficulty, ExerciseKind.FillBlank, genericItem.Prompt.In(language), genericItem.Explanation.In(language))
            {
                Answer = genericItem.Answer.In(language),
            };
        }

        if (area == Area.Language)
        {
            var pool = Unseen(LanguageFillBlank.Where(i => i.Difficulty == difficulty).ToList(), i => i.Topic, "FillBlank", exclude);
            if (pool.Count == 0)
            {
                return null;
            }

            var item = pool[new Random(seed).Next(pool.Count)];
            return new PracticeExercise(item.Topic, item.Difficulty, ExerciseKind.FillBlank, item.Prompt.In(language), item.Explanation.In(language))
            {
                Answer = item.Answer.In(language),
            };
        }

        if (area == Area.ForeignLanguage)
        {
            var pool = ForeignFillBlankWords.Where(i => i.Difficulty == difficulty).ToList();
            if (pool.Count == 0)
            {
                return null;
            }

            var word = pool[new Random(seed).Next(pool.Count)];
            var target = QuestionBanks.ForeignTarget(language);
            var targetName = TargetName(language);
            var sourceWord = word.Answer.In(language); // the "Answer" field doubles as the word concept here
            var targetWord = word.Answer.In(target);
            return new PracticeExercise(word.Topic, word.Difficulty, ExerciseKind.FillBlank,
                ForeignFillBlankPrompt.Format(language, sourceWord, targetName),
                ForeignFillBlankExplanation.Format(language, sourceWord, targetName, targetWord))
            {
                Answer = targetWord,
            };
        }

        return null;
    }

    // Each language's sentence is kept whole (not word-aligned across languages, since a translation rarely has the
    // same word count) and split into tokens at build time by Reorder() — see TokensOf.
    private sealed record ReorderItem(string Topic, int Difficulty, Text Prompt, Text Sentence, Text Explanation);

    private static string[] TokensOf(Text sentence, TeachLanguage language) =>
        sentence.In(language).Split(' ', StringSplitOptions.RemoveEmptyEntries);

    private static readonly IReadOnlyList<ReorderItem> LanguageReorder =
    [
        new("language.sentences", 1, Text.Of("Put the words in the right order.|Pon las palabras en el orden correcto.|Mets les mots dans le bon ordre.|Posa les paraules en l'ordre correcte."),
            Text.Of("The cat is sleeping|El gato está durmiendo|Le chat dort|El gat dorm"),
            Text.Of("\"The cat is sleeping.\"|\"El gato está durmiendo.\"|« Le chat dort. »|\"El gat dorm.\"")),
        new("language.sentences", 2, Text.Of("Put the words in the right order.|Pon las palabras en el orden correcto.|Mets les mots dans le bon ordre.|Posa les paraules en l'ordre correcte."),
            Text.Of("She quickly closed the door|Ella cerró la puerta rápidamente|Elle a rapidement fermé la porte|Ella va tancar la porta ràpidament"),
            Text.Of("\"She quickly closed the door.\"|\"Ella cerró la puerta rápidamente.\"|« Elle a rapidement fermé la porte. »|\"Ella va tancar la porta ràpidament.\"")),
        new("language.sentences", 3, Text.Of("Put the words in the right order.|Pon las palabras en el orden correcto.|Mets les mots dans le bon ordre.|Posa les paraules en l'ordre correcte."),
            Text.Of("Although it was raining they went outside|Aunque estaba lloviendo salieron afuera|Bien qu'il pleuve ils sont sortis|Encara que plovia van sortir fora"),
            Text.Of("A sentence can start with \"although\" to show a contrast.|Una frase puede empezar con \"aunque\" para mostrar un contraste.|Une phrase peut commencer par « bien que » pour montrer un contraste.|Una frase pot començar amb \"encara que\" per mostrar un contrast.")),
    ];

    private static readonly (string Topic, int Difficulty, string[] English, string[] Spanish, Text Explanation)[] ForeignReorder =
    [
        ("foreign.sentences", 1, ["Good", "morning"], ["Buenos", "días"],
            Text.Of("A simple greeting.|Un saludo sencillo.|Une salutation simple.|Una salutació senzilla.")),
        ("foreign.sentences", 2, ["I", "am", "hungry"], ["Tengo", "mucha", "hambre"],
            Text.Of("A common everyday sentence.|Una frase habitual del día a día.|Une phrase courante du quotidien.|Una frase habitual del dia a dia.")),
        ("foreign.sentences", 3, ["Can", "you", "help", "me", "please"], ["Puedes", "ayudarme", "por", "favor"],
            Text.Of("A polite way to ask for help.|Una forma educada de pedir ayuda.|Une façon polie de demander de l'aide.|Una manera educada de demanar ajuda.")),
    ];

    private static readonly Text ForeignReorderPrompt =
        Text.Of("Put the {0} words in the right order.|Pon en orden las palabras en {0}.|Mets les mots en {0} dans le bon ordre.|Posa en ordre les paraules en {0}.");

    /// <summary>A word-reorder drill, or null if the bank has none for this subject/level.</summary>
    // ---- Reorder content for the other 8 subjects (task #73) ----

    private static readonly Text GenericReorderPrompt =
        Text.Of("Put the words in the right order.|Pon las palabras en el orden correcto.|Mets les mots dans le bon ordre.|Posa les paraules en l'ordre correcte.");

    private static readonly IReadOnlyList<ReorderItem> MathReorder =
    [
        new("math.arithmetic", 1, GenericReorderPrompt,
            Text.Of("Two plus two equals four|Dos más dos es igual a cuatro|Deux plus deux égale quatre|Dos més dos és igual a quatre"),
            Text.Of("2 + 2 = 4.|2 + 2 = 4.|2 + 2 = 4.|2 + 2 = 4.")),
        new("math.shapes", 2, GenericReorderPrompt,
            Text.Of("A square has four equal sides|Un cuadrado tiene cuatro lados iguales|Un carré a quatre côtés égaux|Un quadrat té quatre costats iguals"),
            Text.Of("All four sides of a square are the same length.|Los cuatro lados de un cuadrado tienen la misma longitud.|Les quatre côtés d'un carré ont la même longueur.|Els quatre costats d'un quadrat tenen la mateixa longitud.")),
        new("math.multiplication", 3, GenericReorderPrompt,
            Text.Of("Multiplication is repeated addition|La multiplicación es una suma repetida|La multiplication est une addition répétée|La multiplicació és una suma repetida"),
            Text.Of("3 × 4 means adding 3 four times: 3+3+3+3.|3 × 4 significa sumar 3 cuatro veces: 3+3+3+3.|3 × 4 signifie additionner 3 quatre fois : 3+3+3+3.|3 × 4 significa sumar 3 quatre vegades: 3+3+3+3.")),
    ];

    private static readonly IReadOnlyList<ReorderItem> PhysicsReorder =
    [
        new("physics.sun", 1, GenericReorderPrompt,
            Text.Of("The sun rises in the east|El sol sale por el este|Le soleil se lève à l'est|El sol surt per l'est"),
            Text.Of("Yes, and it sets in the west.|Sí, y se pone por el oeste.|Oui, et il se couche à l'ouest.|Sí, i es pon per l'oest.")),
        new("physics.sound", 2, GenericReorderPrompt,
            Text.Of("Sound cannot travel through empty space|El sonido no puede viajar por el espacio vacío|Le son ne peut pas voyager dans l'espace vide|El so no pot viatjar per l'espai buit"),
            Text.Of("Sound needs a medium like air or water to travel; space is a vacuum.|El sonido necesita un medio como el aire o el agua para viajar; el espacio es un vacío.|Le son a besoin d'un support comme l'air ou l'eau pour voyager ; l'espace est un vide.|El so necessita un mitjà com l'aire o l'aigua per viatjar; l'espai és un buit.")),
        new("physics.magnetism", 3, GenericReorderPrompt,
            Text.Of("A magnet has a north pole and a south pole|Un imán tiene un polo norte y un polo sur|Un aimant a un pôle nord et un pôle sud|Un imant té un pol nord i un pol sud"),
            Text.Of("Every magnet has two poles, north and south.|Todo imán tiene dos polos, norte y sur.|Tout aimant a deux pôles, nord et sud.|Tot imant té dos pols, nord i sud.")),
    ];

    private static readonly IReadOnlyList<ReorderItem> ChemistryReorder =
    [
        new("chemistry.solutions", 1, GenericReorderPrompt,
            Text.Of("Salt dissolves in water|La sal se disuelve en agua|Le sel se dissout dans l'eau|La sal es dissol en aigua"),
            Text.Of("Dissolving means the salt spreads evenly through the water.|Disolverse significa que la sal se reparte de manera uniforme por el agua.|Se dissoudre signifie que le sel se répartit uniformément dans l'eau.|Dissoldre's significa que la sal es reparteix de manera uniforme per l'aigua.")),
        new("chemistry.acids", 2, GenericReorderPrompt,
            Text.Of("Mixing an acid and a base can neutralize both|Mezclar un ácido y una base puede neutralizar a ambos|Mélanger un acide et une base peut neutraliser les deux|Barrejar un àcid i una base pot neutralitzar tots dos"),
            Text.Of("The acid and the base cancel out each other's effects.|El ácido y la base anulan sus efectos mutuamente.|L'acide et la base annulent leurs effets mutuels.|L'àcid i la base anul·len els seus efectes mutus.")),
        new("chemistry.reactions", 3, GenericReorderPrompt,
            Text.Of("Every chemical reaction conserves mass|Toda reacción química conserva la masa|Toute réaction chimique conserve la masse|Tota reacció química conserva la massa"),
            Text.Of("Matter isn't created or destroyed in a chemical reaction, just rearranged.|La materia no se crea ni se destruye en una reacción química, solo se reorganiza.|La matière n'est ni créée ni détruite dans une réaction chimique, elle est juste réorganisée.|La matèria no es crea ni es destrueix en una reacció química, només es reorganitza.")),
    ];

    private static readonly IReadOnlyList<ReorderItem> BiologyReorder =
    [
        new("biology.fish", 1, GenericReorderPrompt,
            Text.Of("Fish breathe using gills|Los peces respiran con branquias|Les poissons respirent avec des branchies|Els peixos respiren amb brànquies"),
            Text.Of("Gills let fish take oxygen from the water.|Las branquias permiten que los peces tomen oxígeno del agua.|Les branchies permettent aux poissons de prendre l'oxygène de l'eau.|Les brànquies permeten que els peixos prenguin oxigen de l'aigua.")),
        new("biology.pollination", 2, GenericReorderPrompt,
            Text.Of("Bees help plants reproduce by carrying pollen|Las abejas ayudan a las plantas a reproducirse llevando polen|Les abeilles aident les plantes à se reproduire en transportant du pollen|Les abelles ajuden les plantes a reproduir-se portant pol·len"),
            Text.Of("This process is called pollination.|Este proceso se llama polinización.|Ce processus s'appelle la pollinisation.|Aquest procés s'anomena pol·linització.")),
        new("biology.cells", 3, GenericReorderPrompt,
            Text.Of("The human body is made of many tiny cells|El cuerpo humano está formado por muchas células diminutas|Le corps humain est composé de nombreuses cellules minuscules|El cos humà està format per moltes cèl·lules diminutes"),
            Text.Of("Cells are the tiny building blocks that make up the whole body.|Las células son los diminutos componentes que forman todo el cuerpo.|Les cellules sont les minuscules éléments qui composent tout le corps.|Les cèl·lules són els diminuts components que formen tot el cos.")),
    ];

    private static readonly IReadOnlyList<ReorderItem> HistoryReorder =
    [
        new("history.rome", 1, GenericReorderPrompt,
            Text.Of("Ancient Rome was ruled by emperors|La antigua Roma estaba gobernada por emperadores|La Rome antique était gouvernée par des empereurs|L'antiga Roma estava governada per emperadors"),
            Text.Of("After the Republic, Rome became an empire ruled by emperors.|Después de la República, Roma se convirtió en un imperio gobernado por emperadores.|Après la République, Rome est devenue un empire gouverné par des empereurs.|Després de la República, Roma es va convertir en un imperi governat per emperadors.")),
        new("history.printing", 2, GenericReorderPrompt,
            Text.Of("The printing press changed how information spread|La imprenta cambió cómo se difundía la información|L'imprimerie a changé la façon dont l'information se diffusait|La impremta va canviar com es difonia la informació"),
            Text.Of("Gutenberg's printing press made books much cheaper and faster to produce.|La imprenta de Gutenberg hizo que los libros fueran mucho más baratos y rápidos de producir.|L'imprimerie de Gutenberg a rendu les livres bien moins chers et plus rapides à produire.|La impremta de Gutenberg va fer que els llibres fossin molt més barats i ràpids de produir.")),
        new("history.rivers", 3, GenericReorderPrompt,
            Text.Of("Many ancient civilizations developed near rivers|Muchas civilizaciones antiguas se desarrollaron cerca de ríos|De nombreuses civilisations anciennes se sont développées près des rivières|Moltes civilitzacions antigues es van desenvolupar a prop de rius"),
            Text.Of("Rivers like the Nile and the Tigris gave water for farming and travel.|Ríos como el Nilo y el Tigris daban agua para la agricultura y el transporte.|Des fleuves comme le Nil et le Tigre fournissaient de l'eau pour l'agriculture et les déplacements.|Rius com el Nil i el Tigris donaven aigua per a l'agricultura i el transport.")),
        // Task #97: more items per difficulty tier, so a drill repeats less often across sessions.
        new("history.vikings", 1, GenericReorderPrompt,
            Text.Of("The Vikings sailed in long wooden ships|Los vikingos navegaban en largos barcos de madera|Les Vikings naviguaient sur de longs bateaux en bois|Els víkings navegaven en llargs vaixells de fusta"),
            Text.Of("Viking longships could cross seas and also sail up rivers.|Los barcos vikingos podían cruzar mares y también remontar ríos.|Les drakkars vikings pouvaient traverser les mers et remonter les fleuves.|Els vaixells víkings podien travessar mars i també remuntar rius.")),
        new("history.columbus_voyage", 2, GenericReorderPrompt,
            Text.Of("Columbus reached America in 1492|Colón llegó a América en 1492|Christophe Colomb a atteint l'Amérique en 1492|Colom va arribar a Amèrica el 1492"),
            Text.Of("His voyage was funded by the Catholic Monarchs.|Su viaje lo financiaron los Reyes Católicos.|Son voyage a été financé par les Rois catholiques.|El seu viatge el van finançar els Reis Catòlics.")),
        new("history.democracy", 3, GenericReorderPrompt,
            Text.Of("Democracy was born in ancient Athens|La democracia nació en la antigua Atenas|La démocratie est née dans l'Athènes antique|La democràcia va néixer a l'antiga Atenes"),
            Text.Of("In the 5th century BC, Athenian citizens voted directly in the assembly.|En el siglo V a. C., los ciudadanos atenienses votaban directamente en la asamblea.|Au Vᵉ siècle av. J.-C., les citoyens athéniens votaient directement à l'assemblée.|Al segle V aC, els ciutadans atenesos votaven directament a l'assemblea.")),
        new("history.reunification", 3, GenericReorderPrompt,
            Text.Of("The fall of the Berlin Wall led to German reunification|La caída del muro de Berlín llevó a la reunificación alemana|La chute du mur de Berlin a mené à la réunification allemande|La caiguda del mur de Berlín va portar a la reunificació alemanya"),
            Text.Of("The Wall fell in 1989 and Germany was reunified in 1990.|El muro cayó en 1989 y Alemania se reunificó en 1990.|Le mur est tombé en 1989 et l'Allemagne a été réunifiée en 1990.|El mur va caure el 1989 i Alemanya es va reunificar el 1990.")),
    ];

    private static readonly IReadOnlyList<ReorderItem> GeographyReorder =
    [
        new("geography.mountains", 1, GenericReorderPrompt,
            Text.Of("Mountains are taller than hills|Las montañas son más altas que las colinas|Les montagnes sont plus hautes que les collines|Les muntanyes són més altes que els turons"),
            Text.Of("Mountains rise higher and more steeply than hills.|Las montañas se elevan más y de forma más abrupta que las colinas.|Les montagnes s'élèvent plus haut et plus abruptement que les collines.|Les muntanyes s'eleven més i de manera més abrupta que els turons.")),
        new("geography.deserts", 2, GenericReorderPrompt,
            Text.Of("A desert receives very little rainfall|Un desierto recibe muy poca lluvia|Un désert reçoit très peu de pluie|Un desert rep molt poca pluja"),
            Text.Of("Deserts are defined by how dry they are, not by heat alone.|Los desiertos se definen por lo secos que son, no solo por el calor.|Les déserts sont définis par leur sécheresse, pas seulement par la chaleur.|Els deserts es defineixen per la seva sequedat, no només per la calor.")),
        new("geography.earthquakes", 3, GenericReorderPrompt,
            Text.Of("Earthquakes happen along tectonic plate boundaries|Los terremotos ocurren a lo largo de los límites de las placas tectónicas|Les tremblements de terre se produisent le long des limites des plaques tectoniques|Els terratrèmols passen al llarg dels límits de les plaques tectòniques"),
            Text.Of("Most earthquakes occur where tectonic plates meet and grind against each other.|La mayoría de los terremotos ocurren donde las placas tectónicas se encuentran y rozan entre sí.|La plupart des tremblements de terre se produisent là où les plaques tectoniques se rencontrent et frottent l'une contre l'autre.|La majoria de terratrèmols passen on les plaques tectòniques es troben i freguen entre si.")),
        // Task #97: more items per difficulty tier, so a drill repeats less often across sessions.
        new("geography.rivers", 1, GenericReorderPrompt,
            Text.Of("Rivers carry fresh water to the sea|Los ríos llevan agua dulce al mar|Les fleuves portent de l'eau douce jusqu'à la mer|Els rius porten aigua dolça al mar"),
            Text.Of("Rain and melting snow feed rivers, which flow down to the sea.|La lluvia y la nieve derretida alimentan los ríos, que bajan hasta el mar.|La pluie et la neige fondue alimentent les fleuves, qui descendent jusqu'à la mer.|La pluja i la neu fosa alimenten els rius, que baixen fins al mar.")),
        new("geography.volcanoes", 2, GenericReorderPrompt,
            Text.Of("A volcano can throw out hot lava|Un volcán puede expulsar lava caliente|Un volcan peut projeter de la lave brûlante|Un volcà pot expulsar lava calenta"),
            Text.Of("Lava is molten rock that rises from deep inside the Earth.|La lava es roca fundida que sube desde el interior de la Tierra.|La lave est de la roche fondue qui remonte des profondeurs de la Terre.|La lava és roca fosa que puja des de l'interior de la Terra.")),
        new("geography.cities", 3, GenericReorderPrompt,
            Text.Of("More than half of humanity lives in cities|Más de la mitad de la humanidad vive en ciudades|Plus de la moitié de l'humanité vit en ville|Més de la meitat de la humanitat viu en ciutats"),
            Text.Of("Since about 2007, the world's urban population has been larger than its rural population.|Desde hacia 2007, la población urbana del mundo supera a la rural.|Depuis 2007 environ, la population urbaine mondiale dépasse la population rurale.|Des de cap al 2007, la població urbana del món supera la rural.")),
        new("geography.tides", 3, GenericReorderPrompt,
            Text.Of("The Moon's gravity causes the ocean tides|La gravedad de la Luna causa las mareas|La gravité de la Lune provoque les marées|La gravetat de la Lluna causa les marees"),
            Text.Of("The Moon (and, less, the Sun) pulls on the oceans, so the sea rises and falls about twice a day.|La Luna (y, en menor medida, el Sol) atrae los océanos, así que el mar sube y baja unas dos veces al día.|La Lune (et, dans une moindre mesure, le Soleil) attire les océans : la mer monte et descend environ deux fois par jour.|La Lluna (i, en menor mesura, el Sol) atreu els oceans, així que el mar puja i baixa unes dues vegades al dia.")),
    ];

    private static readonly IReadOnlyList<ReorderItem> EconomicsReorder =
    [
        new("economics.trade", 1, GenericReorderPrompt,
            Text.Of("People trade goods and services for money|Las personas intercambian bienes y servicios por dinero|Les gens échangent des biens et des services contre de l'argent|Les persones intercanvien béns i serveis per diners"),
            Text.Of("Trade exchanges something of value, usually goods or services, for money.|El comercio intercambia algo de valor, normalmente bienes o servicios, por dinero.|Le commerce échange quelque chose de valeur, généralement des biens ou des services, contre de l'argent.|El comerç intercanvia alguna cosa de valor, normalment béns o serveis, per diners.")),
        new("economics.saving", 2, GenericReorderPrompt,
            Text.Of("Saving money now can help pay for things later|Ahorrar dinero ahora puede ayudar a pagar cosas más tarde|Économiser de l'argent maintenant peut aider à payer des choses plus tard|Estalviar diners ara pot ajudar a pagar coses més tard"),
            Text.Of("Saving sets money aside today for a need or goal in the future.|Ahorrar aparta dinero hoy para una necesidad u objetivo futuro.|Épargner met de côté de l'argent aujourd'hui pour un besoin ou un objectif futur.|Estalviar aparta diners avui per a una necessitat o objectiu futur.")),
        new("economics.competition", 3, GenericReorderPrompt,
            Text.Of("Competition between companies can lower prices|La competencia entre empresas puede bajar los precios|La concurrence entre entreprises peut faire baisser les prix|La competència entre empreses pot abaixar els preus"),
            Text.Of("When companies compete for customers, they often lower prices to attract them.|Cuando las empresas compiten por clientes, suelen bajar los precios para atraerlos.|Quand les entreprises se font concurrence pour des clients, elles baissent souvent les prix pour les attirer.|Quan les empreses competeixen per clients, solen abaixar els preus per atreure'ls.")),
        // Task #97: more items per difficulty tier, so a drill repeats less often across sessions.
        new("economics.law_of_demand", 3, GenericReorderPrompt,
            Text.Of("When prices rise people usually buy less|Cuando suben los precios la gente suele comprar menos|Quand les prix montent les gens achètent généralement moins|Quan pugen els preus la gent sol comprar menys"),
            Text.Of("This is the law of demand: the higher the price, the lower the quantity demanded.|Es la ley de la demanda: a mayor precio, menor cantidad demandada.|C'est la loi de la demande : plus le prix est élevé, plus la quantité demandée baisse.|És la llei de la demanda: com més preu, menys quantitat demandada.")),
        new("economics.public_services", 3, GenericReorderPrompt,
            Text.Of("Taxes help pay for public services|Los impuestos ayudan a pagar los servicios públicos|Les impôts aident à payer les services publics|Els impostos ajuden a pagar els serveis públics"),
            Text.Of("Schools, hospitals and roads are funded with the taxes that citizens and companies pay.|Escuelas, hospitales y carreteras se financian con los impuestos que pagan ciudadanos y empresas.|Les écoles, les hôpitaux et les routes sont financés par les impôts des citoyens et des entreprises.|Escoles, hospitals i carreteres es financen amb els impostos que paguen ciutadans i empreses.")),
        new("economics.purchasing_power", 3, GenericReorderPrompt,
            Text.Of("Inflation reduces what your money can buy|La inflación reduce lo que puede comprar tu dinero|L'inflation réduit ce que ton argent peut acheter|La inflació redueix el que poden comprar els teus diners"),
            Text.Of("If prices rise faster than your income, the same money buys less.|Si los precios suben más deprisa que tus ingresos, el mismo dinero compra menos.|Si les prix augmentent plus vite que tes revenus, le même argent achète moins.|Si els preus pugen més de pressa que els teus ingressos, els mateixos diners compren menys.")),
        new("economics.rates", 3, GenericReorderPrompt,
            Text.Of("The central bank raises interest rates to fight inflation|El banco central sube los tipos de interés para frenar la inflación|La banque centrale relève les taux d'intérêt pour lutter contre l'inflation|El banc central apuja els tipus d'interès per frenar la inflació"),
            Text.Of("Higher rates make borrowing more expensive, which cools spending and prices.|Unos tipos más altos encarecen los préstamos, lo que enfría el gasto y los precios.|Des taux plus élevés rendent l'emprunt plus cher, ce qui freine les dépenses et les prix.|Uns tipus més alts encareixen els préstecs, cosa que refreda la despesa i els preus.")),
    ];

    private static readonly IReadOnlyList<ReorderItem> ProgrammingReorder =
    [
        new("programming.code", 1, GenericReorderPrompt,
            Text.Of("Computers only understand instructions written in code|Los ordenadores solo entienden instrucciones escritas en código|Les ordinateurs ne comprennent que des instructions écrites en code|Els ordinadors només entenen instruccions escrites en codi"),
            Text.Of("Code translates human intentions into instructions a computer can run.|El código traduce las intenciones humanas en instrucciones que un ordenador puede ejecutar.|Le code traduit les intentions humaines en instructions qu'un ordinateur peut exécuter.|El codi tradueix les intencions humanes en instruccions que un ordinador pot executar.")),
        new("programming.testing", 2, GenericReorderPrompt,
            Text.Of("Testing code helps find mistakes before they cause problems|Probar el código ayuda a encontrar errores antes de que causen problemas|Tester le code aide à trouver des erreurs avant qu'elles ne causent des problèmes|Provar el codi ajuda a trobar errors abans que causin problemes"),
            Text.Of("Testing catches bugs early, before the program is actually used.|Probar detecta errores pronto, antes de que el programa se use de verdad.|Tester détecte les bugs tôt, avant que le programme ne soit vraiment utilisé.|Provar detecta errors aviat, abans que el programa s'utilitzi de veritat.")),
        new("programming.functions", 3, GenericReorderPrompt,
            Text.Of("A program can call the same function many times|Un programa puede llamar a la misma función muchas veces|Un programme peut appeler la même fonction plusieurs fois|Un programa pot cridar la mateixa funció moltes vegades"),
            Text.Of("Reusing a function avoids rewriting the same code over and over.|Reutilizar una función evita reescribir el mismo código una y otra vez.|Réutiliser une fonction évite de réécrire le même code encore et encore.|Reutilitzar una funció evita reescriure el mateix codi una vegada i una altra.")),
    ];

    private static readonly Dictionary<Area, IReadOnlyList<ReorderItem>> ReorderBanks = new()
    {
        [Area.Mathematics] = MathReorder,
        [Area.Physics] = PhysicsReorder,
        [Area.Chemistry] = ChemistryReorder,
        [Area.Biology] = BiologyReorder,
        [Area.History] = HistoryReorder,
        [Area.Geography] = GeographyReorder,
        [Area.Economics] = EconomicsReorder,
        [Area.Programming] = ProgrammingReorder,
    };

    public static PracticeExercise? Reorder(Area area, Level level, TeachLanguage language, int seed, IReadOnlySet<string>? exclude = null)
    {
        var difficulty = DifficultyFor(level);
        if (ReorderBanks.TryGetValue(area, out var genericBank))
        {
            var genericPool = Unseen(genericBank.Where(i => i.Difficulty == difficulty).ToList(), i => i.Topic, "Reorder", exclude);
            if (genericPool.Count == 0)
            {
                return null;
            }

            var genericItem = genericPool[new Random(seed).Next(genericPool.Count)];
            var genericOrdered = TokensOf(genericItem.Sentence, language);
            return new PracticeExercise(genericItem.Topic, genericItem.Difficulty, ExerciseKind.Reorder, genericItem.Prompt.In(language), genericItem.Explanation.In(language))
            {
                Tokens = Shuffled(genericOrdered, seed),
                CorrectOrder = genericOrdered,
            };
        }

        if (area == Area.Language)
        {
            var pool = Unseen(LanguageReorder.Where(i => i.Difficulty == difficulty).ToList(), i => i.Topic, "Reorder", exclude);
            if (pool.Count == 0)
            {
                return null;
            }

            var item = pool[new Random(seed).Next(pool.Count)];
            var ordered = TokensOf(item.Sentence, language);
            return new PracticeExercise(item.Topic, item.Difficulty, ExerciseKind.Reorder, item.Prompt.In(language), item.Explanation.In(language))
            {
                Tokens = Shuffled(ordered, seed),
                CorrectOrder = ordered,
            };
        }

        if (area == Area.ForeignLanguage)
        {
            var pool = ForeignReorder.Where(i => i.Difficulty == difficulty).ToList();
            if (pool.Count == 0)
            {
                return null;
            }

            var item = pool[new Random(seed).Next(pool.Count)];
            var target = QuestionBanks.ForeignTarget(language);
            var ordered = (target == TeachLanguage.English ? item.English : item.Spanish).ToList();
            return new PracticeExercise(item.Topic, item.Difficulty, ExerciseKind.Reorder,
                ForeignReorderPrompt.Format(language, TargetName(language)), item.Explanation.In(language))
            {
                Tokens = Shuffled(ordered, seed),
                CorrectOrder = ordered,
            };
        }

        return null;
    }

    private sealed record MatchItem(string Topic, int Difficulty, Text Prompt, (Text Left, Text Right)[] Pairs, Text Explanation);

    private static readonly IReadOnlyList<MatchItem> LanguageMatch =
    [
        new("language.opposites", 1, Text.Of("Match each word with its opposite.|Relaciona cada palabra con su contrario.|Associe chaque mot à son contraire.|Relaciona cada paraula amb el seu contrari."),
            [
                (Text.Of("hot|caliente|chaud|calent"), Text.Of("cold|frío|froid|fred")),
                (Text.Of("big|grande|grand|gran"), Text.Of("small|pequeño|petit|petit")),
                (Text.Of("happy|feliz|heureux|feliç"), Text.Of("sad|triste|triste|trist")),
            ],
            Text.Of("Opposites: hot/cold, big/small, happy/sad.|Contrarios: caliente/frío, grande/pequeño, feliz/triste.|Contraires : chaud/froid, grand/petit, heureux/triste.|Contraris: calent/fred, gran/petit, feliç/trist.")),
        new("language.parts_of_speech", 2, Text.Of("Match each word with its part of speech.|Relaciona cada palabra con su categoría gramatical.|Associe chaque mot à sa catégorie grammaticale.|Relaciona cada paraula amb la seva categoria gramatical."),
            [
                (Text.Of("run|correr|courir|córrer"), Text.Of("verb|verbo|verbe|verb")),
                (Text.Of("blue|azul|bleu|blau"), Text.Of("adjective|adjetivo|adjectif|adjectiu")),
                (Text.Of("quickly|rápidamente|rapidement|ràpidament"), Text.Of("adverb|adverbio|adverbe|adverbi")),
            ],
            Text.Of("\"Run\" is a verb, \"blue\" an adjective, \"quickly\" an adverb.|\"Correr\" es un verbo, \"azul\" un adjetivo, \"rápidamente\" un adverbio.|« Courir » est un verbe, « bleu » un adjectif, « rapidement » un adverbe.|\"Córrer\" és un verb, \"blau\" un adjectiu, \"ràpidament\" un adverbi.")),
        new("language.literary_devices", 3, Text.Of("Match each device with its example.|Relaciona cada recurso con su ejemplo.|Associe chaque procédé à son exemple.|Relaciona cada recurs amb el seu exemple."),
            [
                (Text.Of("metaphor|metáfora|métaphore|metàfora"), Text.Of("Time is a thief.|El tiempo es un ladrón.|Le temps est un voleur.|El temps és un lladre.")),
                (Text.Of("simile|símil|comparaison|símil"), Text.Of("As brave as a lion.|Tan valiente como un león.|Aussi courageux qu'un lion.|Tan valent com un lleó.")),
                (Text.Of("personification|personificación|personnification|personificació"), Text.Of("The wind whispered.|El viento susurraba.|Le vent chuchotait.|El vent xiuxiuejava.")),
            ],
            Text.Of("A metaphor states one thing IS another; a simile compares with \"like\"/\"as\"; personification gives human traits to things.|Una metáfora dice que una cosa ES otra; un símil compara con \"como\"; la personificación da rasgos humanos a las cosas.|Une métaphore dit qu'une chose EST une autre ; une comparaison utilise « comme » ; la personnification donne des traits humains aux choses.|Una metàfora diu que una cosa ÉS una altra; un símil compara amb \"com\"; la personificació dona trets humans a les coses.")),
    ];

    private static readonly Dictionary<int, Text[]> ForeignMatchWords = new()
    {
        [1] = [Text.Of("cat|gato|chat|gat"), Text.Of("apple|manzana|pomme|poma"), Text.Of("sun|sol|soleil|sol")],
        [2] = [Text.Of("water|agua|eau|aigua"), Text.Of("book|libro|livre|llibre"), Text.Of("car|coche|voiture|cotxe")],
        [3] = [Text.Of("school|escuela|école|escola"), Text.Of("flower|flor|fleur|flor"), Text.Of("bread|pan|pain|pa")],
    };

    private static readonly Text ForeignMatchPrompt =
        Text.Of("Match each word with its translation in {0}.|Relaciona cada palabra con su traducción en {0}.|Associe chaque mot à sa traduction en {0}.|Relaciona cada paraula amb la seva traducció en {0}.");

    /// <summary>A matching-pairs drill, or null if the bank has none for this subject/level.</summary>
    // ---- Match content for the other 8 subjects (task #73) ----

    private static readonly Text TermMeaningPrompt =
        Text.Of("Match each term with its meaning.|Relaciona cada término con su significado.|Associe chaque terme à sa signification.|Relaciona cada terme amb el seu significat.");

    private static readonly IReadOnlyList<MatchItem> MathMatch =
    [
        new("math.shapes", 1,
            Text.Of("Match each shape with its number of sides.|Relaciona cada figura con su número de lados.|Associe chaque forme à son nombre de côtés.|Relaciona cada figura amb el seu nombre de costats."),
            [
                (Text.Of("triangle|triángulo|triangle|triangle"), Text.Of("3 sides|3 lados|3 côtés|3 costats")),
                (Text.Of("square|cuadrado|carré|quadrat"), Text.Of("4 sides|4 lados|4 côtés|4 costats")),
                (Text.Of("pentagon|pentágono|pentagone|pentàgon"), Text.Of("5 sides|5 lados|5 côtés|5 costats")),
            ],
            Text.Of("A triangle has 3 sides, a square has 4, and a pentagon has 5.|Un triángulo tiene 3 lados, un cuadrado 4 y un pentágono 5.|Un triangle a 3 côtés, un carré 4 et un pentagone 5.|Un triangle té 3 costats, un quadrat 4 i un pentàgon 5.")),
        new("math.operations", 2,
            Text.Of("Match each operation with its symbol.|Relaciona cada operación con su símbolo.|Associe chaque opération à son symbole.|Relaciona cada operació amb el seu símbol."),
            [
                (Text.Of("addition|suma|addition|suma"), Text.Of("+")),
                (Text.Of("subtraction|resta|soustraction|resta"), Text.Of("−")),
                (Text.Of("multiplication|multiplicación|multiplication|multiplicació"), Text.Of("×")),
            ],
            Text.Of("+, − and × are the symbols for addition, subtraction and multiplication.|+, − y × son los símbolos de la suma, la resta y la multiplicación.|+, − et × sont les symboles de l'addition, de la soustraction et de la multiplication.|+, − i × són els símbols de la suma, la resta i la multiplicació.")),
        new("math.terms", 3,
            TermMeaningPrompt,
            [
                (Text.Of("prime number|número primo|nombre premier|nombre primer"), Text.Of("only divisible by 1 and itself|solo divisible entre 1 y él mismo|seulement divisible par 1 et lui-même|només divisible per 1 i ell mateix")),
                (Text.Of("even number|número par|nombre pair|nombre parell"), Text.Of("divisible by 2|divisible entre 2|divisible par 2|divisible per 2")),
                (Text.Of("fraction|fracción|fraction|fracció"), Text.Of("part of a whole|parte de un todo|partie d'un tout|part d'un tot")),
            ],
            Text.Of("A prime number divides only by 1 and itself, an even number divides by 2, and a fraction is part of a whole.|Un número primo solo se divide entre 1 y él mismo, un número par se divide entre 2, y una fracción es parte de un todo.|Un nombre premier ne se divise que par 1 et lui-même, un nombre pair se divise par 2, et une fraction est une partie d'un tout.|Un nombre primer només es divideix per 1 i ell mateix, un nombre parell es divideix per 2, i una fracció és part d'un tot.")),
    ];

    private static readonly IReadOnlyList<MatchItem> PhysicsMatch =
    [
        new("physics.water_forms", 1,
            Text.Of("Match each form of water with its description.|Relaciona cada forma del agua con su descripción.|Associe chaque forme de l'eau à sa description.|Relaciona cada forma de l'aigua amb la seva descripció."),
            [
                (Text.Of("ice|hielo|glace|gel"), Text.Of("frozen water|agua congelada|eau gelée|aigua congelada")),
                (Text.Of("steam|vapor|vapeur|vapor"), Text.Of("water as a gas|agua en forma de gas|eau sous forme de gaz|aigua en forma de gas")),
                (Text.Of("rain|lluvia|pluie|pluja"), Text.Of("water falling from clouds|agua que cae de las nubes|eau qui tombe des nuages|aigua que cau dels núvols")),
            ],
            Text.Of("Ice, steam and rain are all just water in different forms.|El hielo, el vapor y la lluvia son todos agua en diferentes formas.|La glace, la vapeur et la pluie sont toutes de l'eau sous différentes formes.|El gel, el vapor i la pluja són tots aigua en diferents formes.")),
        new("physics.units", 2,
            Text.Of("Match each unit with what it measures.|Relaciona cada unidad con lo que mide.|Associe chaque unité à ce qu'elle mesure.|Relaciona cada unitat amb el que mesura."),
            [
                (Text.Of("meter|metro|mètre|metre"), Text.Of("distance|distancia|distance|distància")),
                (Text.Of("second|segundo|seconde|segon"), Text.Of("time|tiempo|temps|temps")),
                (Text.Of("kilogram|kilogramo|kilogramme|quilogram"), Text.Of("mass|masa|masse|massa")),
            ],
            Text.Of("Meters measure distance, seconds measure time, and kilograms measure mass.|Los metros miden distancia, los segundos miden tiempo y los kilogramos miden masa.|Les mètres mesurent la distance, les secondes le temps et les kilogrammes la masse.|Els metres mesuren distància, els segons mesuren temps i els quilograms mesuren massa.")),
        new("physics.states", 3,
            Text.Of("Match each state of matter with its property.|Relaciona cada estado de la materia con su propiedad.|Associe chaque état de la matière à sa propriété.|Relaciona cada estat de la matèria amb la seva propietat."),
            [
                (Text.Of("solid|sólido|solide|sòlid"), Text.Of("has a fixed shape|tiene una forma fija|a une forme fixe|té una forma fixa")),
                (Text.Of("liquid|líquido|liquide|líquid"), Text.Of("takes the shape of its container|toma la forma de su recipiente|prend la forme de son récipient|pren la forma del seu recipient")),
                (Text.Of("gas|gas|gaz|gas"), Text.Of("spreads to fill any space|se expande para llenar cualquier espacio|se répand pour remplir n'importe quel espace|s'expandeix per omplir qualsevol espai")),
            ],
            Text.Of("Solids keep their shape, liquids take the shape of their container, and gases spread out to fill the space available.|Los sólidos mantienen su forma, los líquidos toman la forma de su recipiente y los gases se expanden para llenar el espacio disponible.|Les solides gardent leur forme, les liquides prennent la forme de leur récipient et les gaz se répandent pour remplir l'espace disponible.|Els sòlids mantenen la seva forma, els líquids prenen la forma del seu recipient i els gasos s'expandeixen per omplir l'espai disponible.")),
    ];

    private static readonly IReadOnlyList<MatchItem> ChemistryMatch =
    [
        new("chemistry.states", 1,
            Text.Of("Match each substance with its state at room temperature.|Relaciona cada sustancia con su estado a temperatura ambiente.|Associe chaque substance à son état à température ambiante.|Relaciona cada substància amb el seu estat a temperatura ambient."),
            [
                (Text.Of("water|agua|eau|aigua"), Text.Of("liquid|líquido|liquide|líquid")),
                (Text.Of("oxygen|oxígeno|oxygène|oxigen"), Text.Of("gas|gas|gaz|gas")),
                (Text.Of("iron|hierro|fer|ferro"), Text.Of("solid|sólido|solide|sòlid")),
            ],
            Text.Of("At room temperature, water is liquid, oxygen is a gas, and iron is solid.|A temperatura ambiente, el agua es líquida, el oxígeno es gas y el hierro es sólido.|À température ambiante, l'eau est liquide, l'oxygène est un gaz et le fer est solide.|A temperatura ambient, l'aigua és líquida, l'oxigen és gas i el ferro és sòlid.")),
        new("chemistry.symbols", 2,
            Text.Of("Match each element with its chemical symbol.|Relaciona cada elemento con su símbolo químico.|Associe chaque élément à son symbole chimique.|Relaciona cada element amb el seu símbol químic."),
            [
                (Text.Of("hydrogen|hidrógeno|hydrogène|hidrogen"), Text.Of("H")),
                (Text.Of("carbon|carbono|carbone|carboni"), Text.Of("C")),
                (Text.Of("nitrogen|nitrógeno|azote|nitrogen"), Text.Of("N")),
            ],
            Text.Of("H is hydrogen, C is carbon, and N is nitrogen.|H es hidrógeno, C es carbono y N es nitrógeno.|H est l'hydrogène, C le carbone et N l'azote.|H és hidrogen, C és carboni i N és nitrogen.")),
        new("chemistry.terms", 3,
            TermMeaningPrompt,
            [
                (Text.Of("mixture|mezcla|mélange|mescla"), Text.Of("substances not chemically bonded|sustancias no unidas químicamente|substances non liées chimiquement|substàncies no unides químicament")),
                (Text.Of("compound|compuesto|composé|compost"), Text.Of("substances chemically bonded|sustancias unidas químicamente|substances liées chimiquement|substàncies unides químicament")),
                (Text.Of("solution|disolución|solution|dissolució"), Text.Of("a mixture that looks uniform|una mezcla que parece uniforme|un mélange qui paraît uniforme|una mescla que sembla uniforme")),
            ],
            Text.Of("A mixture keeps its substances separate, a compound bonds them chemically, and a solution is a mixture that looks the same throughout.|Una mezcla mantiene sus sustancias separadas, un compuesto las une químicamente y una disolución es una mezcla que parece igual en todas partes.|Un mélange garde ses substances séparées, un composé les lie chimiquement, et une solution est un mélange qui paraît uniforme partout.|Una mescla manté les seves substàncies separades, un compost les uneix químicament, i una dissolució és una mescla que sembla igual arreu.")),
    ];

    private static readonly IReadOnlyList<MatchItem> BiologyMatch =
    [
        new("biology.classes", 1,
            Text.Of("Match each animal with its group.|Relaciona cada animal con su grupo.|Associe chaque animal à son groupe.|Relaciona cada animal amb el seu grup."),
            [
                (Text.Of("dog|perro|chien|gos"), Text.Of("mammal|mamífero|mammifère|mamífer")),
                (Text.Of("eagle|águila|aigle|àguila"), Text.Of("bird|ave|oiseau|ocell")),
                (Text.Of("trout|trucha|truite|truita"), Text.Of("fish|pez|poisson|peix")),
            ],
            Text.Of("A dog is a mammal, an eagle is a bird, and a trout is a fish.|Un perro es un mamífero, un águila es un ave y una trucha es un pez.|Un chien est un mammifère, un aigle est un oiseau et une truite est un poisson.|Un gos és un mamífer, una àguila és un ocell i una truita és un peix.")),
        new("biology.organs", 2,
            Text.Of("Match each organ with its function.|Relaciona cada órgano con su función.|Associe chaque organe à sa fonction.|Relaciona cada òrgan amb la seva funció."),
            [
                (Text.Of("lungs|pulmones|poumons|pulmons"), Text.Of("breathing|respiración|respiration|respiració")),
                (Text.Of("stomach|estómago|estomac|estómac"), Text.Of("digestion|digestión|digestion|digestió")),
                (Text.Of("brain|cerebro|cerveau|cervell"), Text.Of("thinking|pensamiento|la pensée|pensament")),
            ],
            Text.Of("Lungs are for breathing, the stomach for digestion, and the brain for thinking.|Los pulmones son para respirar, el estómago para digerir y el cerebro para pensar.|Les poumons servent à respirer, l'estomac à digérer et le cerveau à penser.|Els pulmons són per respirar, l'estómac per digerir i el cervell per pensar.")),
        new("biology.diets", 3,
            Text.Of("Match each diet type with its description.|Relaciona cada tipo de dieta con su descripción.|Associe chaque type de régime à sa description.|Relaciona cada tipus de dieta amb la seva descripció."),
            [
                (Text.Of("herbivore|herbívoro|herbivore|herbívor"), Text.Of("eats only plants|come solo plantas|mange seulement des plantes|menja només plantes")),
                (Text.Of("carnivore|carnívoro|carnivore|carnívor"), Text.Of("eats only meat|come solo carne|mange seulement de la viande|menja només carn")),
                (Text.Of("omnivore|omnívoro|omnivore|omnívor"), Text.Of("eats both plants and meat|come tanto plantas como carne|mange à la fois des plantes et de la viande|menja tant plantes com carn")),
            ],
            Text.Of("Herbivores eat only plants, carnivores eat only meat, and omnivores eat both.|Los herbívoros comen solo plantas, los carnívoros solo carne, y los omnívoros ambas cosas.|Les herbivores mangent seulement des plantes, les carnivores seulement de la viande, et les omnivores les deux.|Els herbívors mengen només plantes, els carnívors només carn, i els omnívors totes dues coses.")),
    ];

    private static readonly IReadOnlyList<MatchItem> HistoryMatch =
    [
        new("history.landmarks", 1,
            Text.Of("Match each place with what it's known for.|Relaciona cada lugar con aquello por lo que es conocido.|Associe chaque lieu à ce pour quoi il est connu.|Relaciona cada lloc amb allò pel qual és conegut."),
            [
                (Text.Of("Egypt|Egipto|Égypte|Egipte"), Text.Of("the pyramids|las pirámides|les pyramides|les piràmides")),
                (Text.Of("Rome|Roma|Rome|Roma"), Text.Of("the Colosseum|el Coliseo|le Colisée|el Colosseu")),
                (Text.Of("China|China|Chine|Xina"), Text.Of("the Great Wall|la Gran Muralla|la Grande Muraille|la Gran Muralla")),
            ],
            Text.Of("Egypt is known for the pyramids, Rome for the Colosseum, and China for the Great Wall.|Egipto es conocido por las pirámides, Roma por el Coliseo y China por la Gran Muralla.|L'Égypte est connue pour les pyramides, Rome pour le Colisée et la Chine pour la Grande Muraille.|Egipte és conegut per les piràmides, Roma pel Colosseu i la Xina per la Gran Muralla.")),
        new("history.scientists", 2,
            Text.Of("Match each scientist with what they're known for.|Relaciona cada científico con aquello por lo que es conocido.|Associe chaque scientifique à ce pour quoi il est connu.|Relaciona cada científic amb allò pel qual és conegut."),
            [
                (Text.Of("Einstein|Einstein|Einstein|Einstein"), Text.Of("the theory of relativity|la teoría de la relatividad|la théorie de la relativité|la teoria de la relativitat")),
                (Text.Of("Newton|Newton|Newton|Newton"), Text.Of("the laws of motion|las leyes del movimiento|les lois du mouvement|les lleis del moviment")),
                (Text.Of("Curie|Curie|Curie|Curie"), Text.Of("research on radioactivity|la investigación sobre la radiactividad|les recherches sur la radioactivité|la investigació sobre la radioactivitat")),
            ],
            Text.Of("Einstein is known for relativity, Newton for the laws of motion, and Curie for her research on radioactivity.|Einstein es conocido por la relatividad, Newton por las leyes del movimiento y Curie por su investigación sobre la radiactividad.|Einstein est connu pour la relativité, Newton pour les lois du mouvement et Curie pour ses recherches sur la radioactivité.|Einstein és conegut per la relativitat, Newton per les lleis del moviment i Curie per la seva investigació sobre la radioactivitat.")),
        new("history.ages", 3,
            Text.Of("Match each era with its tools.|Relaciona cada era con sus herramientas.|Associe chaque ère à ses outils.|Relaciona cada era amb les seves eines."),
            [
                (Text.Of("Stone Age|Edad de Piedra|âge de pierre|Edat de Pedra"), Text.Of("tools made of stone|herramientas hechas de piedra|outils en pierre|eines fetes de pedra")),
                (Text.Of("Bronze Age|Edad de Bronce|âge du bronze|Edat del Bronze"), Text.Of("tools made of bronze|herramientas hechas de bronce|outils en bronze|eines fetes de bronze")),
                (Text.Of("Iron Age|Edad de Hierro|âge du fer|Edat del Ferro"), Text.Of("tools made of iron|herramientas hechas de hierro|outils en fer|eines fetes de ferro")),
            ],
            Text.Of("Each era is named after the main material used for tools at the time.|Cada era recibe el nombre del material principal usado para las herramientas en esa época.|Chaque ère porte le nom du matériau principal utilisé pour les outils à cette époque.|Cada era rep el nom del material principal utilitzat per a les eines en aquella època.")),
        // Task #97: more items per difficulty tier, so a drill repeats less often across sessions.
        new("history.inventions", 1,
            Text.Of("Match each invention with what it does.|Relaciona cada invento con su utilidad.|Associe chaque invention à ce qu'elle permet de faire.|Relaciona cada invent amb la seva utilitat."),
            [
                (Text.Of("wheel|rueda|roue|roda"), Text.Of("helps carry heavy loads|ayuda a transportar cargas pesadas|aide à transporter des charges lourdes|ajuda a transportar càrregues pesades")),
                (Text.Of("compass|brújula|boussole|brúixola"), Text.Of("shows where north is|indica dónde está el norte|indique où est le nord|indica on és el nord")),
                (Text.Of("calendar|calendario|calendrier|calendari"), Text.Of("counts the days of the year|cuenta los días del año|compte les jours de l'année|compta els dies de l'any")),
            ],
            Text.Of("The wheel helps carry loads, the compass shows north, and the calendar counts the days.|La rueda ayuda a transportar cargas, la brújula indica el norte y el calendario cuenta los días.|La roue aide à transporter des charges, la boussole indique le nord et le calendrier compte les jours.|La roda ajuda a transportar càrregues, la brúixola indica el nord i el calendari compta els dies.")),
        new("history.explorers", 2,
            Text.Of("Match each explorer with their journey.|Relaciona cada explorador con su viaje.|Associe chaque explorateur à son voyage.|Relaciona cada explorador amb el seu viatge."),
            [
                (Text.Of("Columbus|Colón|Christophe Colomb|Colom"), Text.Of("reached America in 1492|llegó a América en 1492|a atteint l'Amérique en 1492|va arribar a Amèrica el 1492")),
                (Text.Of("Magellan|Magallanes|Magellan|Magallanes"), Text.Of("first expedition around the world|primera expedición alrededor del mundo|première expédition autour du monde|primera expedició al voltant del món")),
                (Text.Of("Marco Polo"), Text.Of("travelled to China|viajó a China|a voyagé jusqu'en Chine|va viatjar fins a la Xina")),
            ],
            Text.Of("Columbus crossed the Atlantic, Magellan's expedition sailed around the world, and Marco Polo travelled to China.|Colón cruzó el Atlántico, la expedición de Magallanes dio la vuelta al mundo y Marco Polo viajó a China.|Colomb a traversé l'Atlantique, l'expédition de Magellan a fait le tour du monde et Marco Polo a voyagé jusqu'en Chine.|Colom va travessar l'Atlàntic, l'expedició de Magallanes va fer la volta al món i Marco Polo va viatjar fins a la Xina.")),
        new("history.periods", 3,
            Text.Of("Match each period with its dates.|Relaciona cada época con sus fechas.|Associe chaque période à ses dates.|Relaciona cada època amb les seves dates."),
            [
                (Text.Of("Antiquity|Edad Antigua|Antiquité|Edat Antiga"), Text.Of("until 476|hasta el 476|jusqu'en 476|fins al 476")),
                (Text.Of("Middle Ages|Edad Media|Moyen Âge|Edat Mitjana"), Text.Of("476–1492")),
                (Text.Of("Early modern period|Edad Moderna|Époque moderne|Edat Moderna"), Text.Of("1492–1789")),
            ],
            Text.Of("By the usual convention, Antiquity ends with the fall of Rome (476), the Middle Ages around 1492, and the early modern period lasts until 1789.|Según la convención habitual, la Edad Antigua acaba con la caída de Roma (476), la Edad Media hacia 1492 y la Edad Moderna dura hasta 1789.|Selon la convention habituelle, l'Antiquité s'achève avec la chute de Rome (476), le Moyen Âge vers 1492 et l'époque moderne dure jusqu'en 1789.|Segons la convenció habitual, l'edat antiga acaba amb la caiguda de Roma (476), l'edat mitjana cap al 1492 i l'edat moderna dura fins al 1789.")),
        new("history.revolutions", 3,
            Text.Of("Match each revolution with its year.|Relaciona cada revolución con su año.|Associe chaque révolution à son année.|Relaciona cada revolució amb el seu any."),
            [
                (Text.Of("French Revolution|Revolución francesa|Révolution française|Revolució francesa"), Text.Of("1789")),
                (Text.Of("Russian Revolution|Revolución rusa|Révolution russe|Revolució russa"), Text.Of("1917")),
                (Text.Of("US Declaration of Independence|Independencia de EE. UU.|Indépendance américaine|Independència dels EUA"), Text.Of("1776")),
            ],
            Text.Of("1776: US independence; 1789: the French Revolution; 1917: the Russian Revolution.|1776: independencia de EE. UU.; 1789: Revolución francesa; 1917: Revolución rusa.|1776 : indépendance américaine ; 1789 : Révolution française ; 1917 : révolution russe.|1776: independència dels EUA; 1789: Revolució francesa; 1917: Revolució russa.")),
    ];

    private static readonly IReadOnlyList<MatchItem> GeographyMatch =
    [
        new("geography.continents", 1,
            Text.Of("Match each country with its continent.|Relaciona cada país con su continente.|Associe chaque pays à son continent.|Relaciona cada país amb el seu continent."),
            [
                (Text.Of("Brazil|Brasil|Brésil|Brasil"), Text.Of("South America|Sudamérica|Amérique du Sud|Sud-amèrica")),
                (Text.Of("Japan|Japón|Japon|Japó"), Text.Of("Asia|Asia|Asie|Àsia")),
                (Text.Of("Egypt|Egipto|Égypte|Egipte"), Text.Of("Africa|África|Afrique|Àfrica")),
            ],
            Text.Of("Brazil is in South America, Japan is in Asia, and Egypt is in Africa.|Brasil está en Sudamérica, Japón está en Asia y Egipto está en África.|Le Brésil est en Amérique du Sud, le Japon est en Asie et l'Égypte est en Afrique.|El Brasil és a Sud-amèrica, el Japó és a Àsia i Egipte és a l'Àfrica.")),
        new("geography.landforms", 2,
            Text.Of("Match each landform with its description.|Relaciona cada accidente geográfico con su descripción.|Associe chaque relief à sa description.|Relaciona cada accident geogràfic amb la seva descripció."),
            [
                (Text.Of("island|isla|île|illa"), Text.Of("land surrounded by water|tierra rodeada de agua|terre entourée d'eau|terra envoltada d'aigua")),
                (Text.Of("peninsula|península|péninsule|península"), Text.Of("land surrounded by water on three sides|tierra rodeada de agua por tres lados|terre entourée d'eau sur trois côtés|terra envoltada d'aigua per tres costats")),
                (Text.Of("valley|valle|vallée|vall"), Text.Of("low land between hills or mountains|tierra baja entre colinas o montañas|terre basse entre des collines ou des montagnes|terra baixa entre turons o muntanyes")),
            ],
            Text.Of("An island is surrounded by water, a peninsula by water on three sides, and a valley lies low between hills or mountains.|Una isla está rodeada de agua, una península por agua en tres lados, y un valle es una tierra baja entre colinas o montañas.|Une île est entourée d'eau, une péninsule par de l'eau sur trois côtés, et une vallée est une terre basse entre des collines ou des montagnes.|Una illa està envoltada d'aigua, una península per aigua en tres costats, i una vall és una terra baixa entre turons o muntanyes.")),
        new("geography.climates", 3,
            Text.Of("Match each climate with its description.|Relaciona cada clima con su descripción.|Associe chaque climat à sa description.|Relaciona cada clima amb la seva descripció."),
            [
                (Text.Of("tropical|tropical|tropical|tropical"), Text.Of("hot and humid all year|cálido y húmedo todo el año|chaud et humide toute l'année|càlid i humit tot l'any")),
                (Text.Of("polar|polar|polaire|polar"), Text.Of("cold all year|frío todo el año|froid toute l'année|fred tot l'any")),
                (Text.Of("temperate|templado|tempéré|temperat"), Text.Of("mild, with four seasons|templado, con cuatro estaciones|doux, avec quatre saisons|suau, amb quatre estacions")),
            ],
            Text.Of("Tropical climates are hot and humid, polar climates are cold all year, and temperate climates have four mild seasons.|Los climas tropicales son cálidos y húmedos, los polares son fríos todo el año, y los templados tienen cuatro estaciones suaves.|Les climats tropicaux sont chauds et humides, les climats polaires sont froids toute l'année, et les climats tempérés ont quatre saisons douces.|Els climes tropicals són càlids i humits, els polars són freds tot l'any, i els temperats tenen quatre estacions suaus.")),
        // Task #97: more items per difficulty tier, so a drill repeats less often across sessions.
        new("geography.capitals", 1,
            Text.Of("Match each country with its capital.|Relaciona cada país con su capital.|Associe chaque pays à sa capitale.|Relaciona cada país amb la seva capital."),
            [
                (Text.Of("Spain|España|Espagne|Espanya"), Text.Of("Madrid")),
                (Text.Of("Italy|Italia|Italie|Itàlia"), Text.Of("Rome|Roma|Rome|Roma")),
                (Text.Of("Germany|Alemania|Allemagne|Alemanya"), Text.Of("Berlin|Berlín|Berlin|Berlín")),
            ],
            Text.Of("Madrid is the capital of Spain, Rome of Italy and Berlin of Germany.|Madrid es la capital de España, Roma la de Italia y Berlín la de Alemania.|Madrid est la capitale de l'Espagne, Rome celle de l'Italie et Berlin celle de l'Allemagne.|Madrid és la capital d'Espanya, Roma la d'Itàlia i Berlín la d'Alemanya.")),
        new("geography.river_cities", 2,
            Text.Of("Match each river with a city it flows through.|Relaciona cada río con una ciudad por la que pasa.|Associe chaque fleuve à une ville qu'il traverse.|Relaciona cada riu amb una ciutat per on passa."),
            [
                (Text.Of("Seine|Sena|Seine|Sena"), Text.Of("Paris|París|Paris|París")),
                (Text.Of("Thames|Támesis|Tamise|Tàmesi"), Text.Of("London|Londres|Londres|Londres")),
                (Text.Of("Ebro|Ebro|Èbre|Ebre"), Text.Of("Zaragoza|Zaragoza|Saragosse|Saragossa")),
            ],
            Text.Of("The Seine flows through Paris, the Thames through London and the Ebro through Zaragoza.|El Sena pasa por París, el Támesis por Londres y el Ebro por Zaragoza.|La Seine traverse Paris, la Tamise Londres et l'Èbre Saragosse.|El Sena passa per París, el Tàmesi per Londres i l'Ebre per Saragossa.")),
        new("geography.biomes", 3,
            Text.Of("Match each biome with its description.|Relaciona cada bioma con su descripción.|Associe chaque biome à sa description.|Relaciona cada bioma amb la seva descripció."),
            [
                (Text.Of("savanna|sabana|savane|sabana"), Text.Of("grassland with scattered trees and a dry season|pradera con árboles dispersos y una estación seca|prairie aux arbres épars avec une saison sèche|prada amb arbres dispersos i una estació seca")),
                (Text.Of("taiga|taiga|taïga|taiga"), Text.Of("huge conifer forest in cold regions|enorme bosque de coníferas en regiones frías|immense forêt de conifères des régions froides|enorme bosc de coníferes de regions fredes")),
                (Text.Of("tundra|tundra|toundra|tundra"), Text.Of("cold treeless plain with frozen subsoil|llanura fría sin árboles y con el subsuelo helado|plaine froide sans arbres au sous-sol gelé|plana freda sense arbres i amb el subsòl glaçat")),
            ],
            Text.Of("Savannas are tropical grasslands, the taiga is a cold conifer forest, and the tundra is a treeless plain over permafrost.|La sabana es una pradera tropical, la taiga un bosque frío de coníferas y la tundra una llanura sin árboles sobre permafrost.|La savane est une prairie tropicale, la taïga une forêt froide de conifères et la toundra une plaine sans arbres sur le pergélisol.|La sabana és una prada tropical, la taiga un bosc fred de coníferes i la tundra una plana sense arbres sobre permafrost.")),
        new("geography.map_terms", 3,
            TermMeaningPrompt,
            [
                (Text.Of("latitude|latitud|latitude|latitud"), Text.Of("distance north or south of the equator|distancia al norte o al sur del ecuador|distance au nord ou au sud de l'équateur|distància al nord o al sud de l'equador")),
                (Text.Of("longitude|longitud|longitude|longitud"), Text.Of("distance east or west of the prime meridian|distancia al este o al oeste del meridiano cero|distance à l'est ou à l'ouest du méridien d'origine|distància a l'est o a l'oest del meridià zero")),
                (Text.Of("scale|escala|échelle|escala"), Text.Of("ratio between distances on the map and on the ground|relación entre las distancias del mapa y las reales|rapport entre les distances sur la carte et sur le terrain|relació entre les distàncies del mapa i les reals")),
            ],
            Text.Of("Latitude measures north–south from the equator, longitude east–west from Greenwich, and the scale converts map distances into real ones.|La latitud mide norte-sur desde el ecuador, la longitud este-oeste desde Greenwich y la escala convierte las distancias del mapa en reales.|La latitude mesure le nord-sud depuis l'équateur, la longitude l'est-ouest depuis Greenwich et l'échelle convertit les distances de la carte en distances réelles.|La latitud mesura nord-sud des de l'equador, la longitud est-oest des de Greenwich i l'escala converteix les distàncies del mapa en reals.")),
    ];

    private static readonly IReadOnlyList<MatchItem> EconomicsMatch =
    [
        new("economics.terms", 1,
            TermMeaningPrompt,
            [
                (Text.Of("buyer|comprador|acheteur|comprador"), Text.Of("pays for a product|paga por un producto|paie pour un produit|paga per un producte")),
                (Text.Of("seller|vendedor|vendeur|venedor"), Text.Of("offers a product for sale|ofrece un producto a la venta|propose un produit à la vente|ofereix un producte a la venda")),
                (Text.Of("price|precio|prix|preu"), Text.Of("the amount paid for something|la cantidad pagada por algo|le montant payé pour quelque chose|la quantitat pagada per alguna cosa")),
            ],
            Text.Of("A buyer pays, a seller offers something for sale, and the price is what's paid.|Un comprador paga, un vendedor ofrece algo a la venta y el precio es lo que se paga.|Un acheteur paie, un vendeur propose quelque chose à la vente, et le prix est ce qui est payé.|Un comprador paga, un venedor ofereix alguna cosa a la venda i el preu és el que es paga.")),
        new("economics.market", 2,
            TermMeaningPrompt,
            [
                (Text.Of("supply|oferta|offre|oferta"), Text.Of("how much of something is available|cuánto hay disponible de algo|la quantité disponible de quelque chose|quant hi ha disponible d'alguna cosa")),
                (Text.Of("demand|demanda|demande|demanda"), Text.Of("how much people want something|cuánto quiere la gente algo|à quel point les gens veulent quelque chose|quant vol la gent alguna cosa")),
                (Text.Of("trade|comercio|commerce|comerç"), Text.Of("exchanging goods or services|intercambiar bienes o servicios|échanger des biens ou des services|intercanviar béns o serveis")),
            ],
            Text.Of("Supply is what's available, demand is what people want, and trade is exchanging goods or services.|La oferta es lo que hay disponible, la demanda es lo que la gente quiere, y el comercio es intercambiar bienes o servicios.|L'offre est ce qui est disponible, la demande est ce que les gens veulent, et le commerce consiste à échanger des biens ou des services.|L'oferta és el que hi ha disponible, la demanda és el que la gent vol, i el comerç és intercanviar béns o serveis.")),
        new("economics.finance", 3,
            TermMeaningPrompt,
            [
                (Text.Of("tax|impuesto|impôt|impost"), Text.Of("money paid to the government|dinero pagado al gobierno|argent payé au gouvernement|diners pagats al govern")),
                (Text.Of("debt|deuda|dette|deute"), Text.Of("money that is owed|dinero que se debe|argent qui est dû|diners que es deuen")),
                (Text.Of("investment|inversión|investissement|inversió"), Text.Of("money put in to earn more later|dinero invertido para ganar más después|argent investi pour en gagner plus tard|diners invertits per guanyar-ne més després")),
            ],
            Text.Of("A tax goes to the government, a debt is money owed, and an investment is money put in to grow.|Un impuesto va al gobierno, una deuda es dinero que se debe, y una inversión es dinero puesto para crecer.|Un impôt va au gouvernement, une dette est de l'argent dû, et un investissement est de l'argent placé pour qu'il fructifie.|Un impost va al govern, un deute és diners que es deuen, i una inversió és diners posats perquè creixin.")),
        // Task #97: more items per difficulty tier, so a drill repeats less often across sessions.
        new("economics.sectors", 3,
            Text.Of("Match each economic sector with an example.|Relaciona cada sector económico con un ejemplo.|Associe chaque secteur économique à un exemple.|Relaciona cada sector econòmic amb un exemple."),
            [
                (Text.Of("primary sector|sector primario|secteur primaire|sector primari"), Text.Of("farming|agricultura|agriculture|agricultura")),
                (Text.Of("secondary sector|sector secundario|secteur secondaire|sector secundari"), Text.Of("a car factory|una fábrica de coches|une usine automobile|una fàbrica de cotxes")),
                (Text.Of("tertiary sector|sector terciario|secteur tertiaire|sector terciari"), Text.Of("a hospital|un hospital|un hôpital|un hospital")),
            ],
            Text.Of("The primary sector takes resources from nature, the secondary transforms them in factories, and the tertiary provides services.|El sector primario obtiene recursos de la naturaleza, el secundario los transforma en fábricas y el terciario ofrece servicios.|Le secteur primaire tire ses ressources de la nature, le secondaire les transforme en usine et le tertiaire fournit des services.|El sector primari obté recursos de la natura, el secundari els transforma en fàbriques i el terciari ofereix serveis.")),
        new("economics.institutions", 3,
            Text.Of("Match each institution with its role.|Relaciona cada institución con su función.|Associe chaque institution à son rôle.|Relaciona cada institució amb la seva funció."),
            [
                (Text.Of("ECB|BCE|BCE|BCE"), Text.Of("sets interest rates in the euro area|fija los tipos de interés de la zona euro|fixe les taux d'intérêt de la zone euro|fixa els tipus d'interès de la zona euro")),
                (Text.Of("IMF|FMI|FMI|FMI"), Text.Of("lends to countries in financial trouble|presta a países con problemas financieros|prête aux pays en difficulté financière|presta a països amb problemes financers")),
                (Text.Of("WTO|OMC|OMC|OMC"), Text.Of("sets the rules of world trade|establece las normas del comercio mundial|fixe les règles du commerce mondial|estableix les normes del comerç mundial")),
            ],
            Text.Of("The European Central Bank steers the euro, the International Monetary Fund helps countries in crisis, and the World Trade Organization oversees trade rules.|El Banco Central Europeo gestiona el euro, el Fondo Monetario Internacional ayuda a países en crisis y la Organización Mundial del Comercio vigila las normas del comercio.|La Banque centrale européenne pilote l'euro, le Fonds monétaire international aide les pays en crise et l'Organisation mondiale du commerce veille aux règles du commerce.|El Banc Central Europeu gestiona l'euro, el Fons Monetari Internacional ajuda països en crisi i l'Organització Mundial del Comerç vetlla per les normes del comerç.")),
        new("economics.money_terms", 3,
            TermMeaningPrompt,
            [
                (Text.Of("salary|salario|salaire|salari"), Text.Of("money paid for work|dinero que se cobra por trabajar|argent reçu pour un travail|diners que es cobren per treballar")),
                (Text.Of("savings|ahorro|épargne|estalvi"), Text.Of("money kept for later|dinero guardado para más adelante|argent mis de côté pour plus tard|diners guardats per a més endavant")),
                (Text.Of("loan|préstamo|prêt|préstec"), Text.Of("money borrowed that must be repaid|dinero prestado que hay que devolver|argent emprunté qu'il faut rembourser|diners prestats que s'han de tornar")),
            ],
            Text.Of("A salary is earned by working, savings are kept for later, and a loan is borrowed money you must pay back.|El salario se cobra por trabajar, el ahorro se guarda para más adelante y un préstamo es dinero que hay que devolver.|Le salaire se gagne en travaillant, l'épargne est mise de côté pour plus tard et un prêt est de l'argent qu'il faut rembourser.|El salari es cobra per treballar, l'estalvi es guarda per a més endavant i un préstec són diners que s'han de tornar.")),
        new("economics.market_types", 3,
            TermMeaningPrompt,
            [
                (Text.Of("monopoly|monopolio|monopole|monopoli"), Text.Of("a single seller|un solo vendedor|un seul vendeur|un sol venedor")),
                (Text.Of("oligopoly|oligopolio|oligopole|oligopoli"), Text.Of("a few large sellers|unos pocos grandes vendedores|quelques grands vendeurs|uns quants grans venedors")),
                (Text.Of("perfect competition|competencia perfecta|concurrence parfaite|competència perfecta"), Text.Of("many small sellers of the same product|muchos pequeños vendedores del mismo producto|de nombreux petits vendeurs du même produit|molts petits venedors del mateix producte")),
            ],
            Text.Of("The fewer the sellers, the more power each one has over the price.|Cuantos menos vendedores, más poder tiene cada uno sobre el precio.|Moins il y a de vendeurs, plus chacun a de pouvoir sur le prix.|Com menys venedors, més poder té cadascun sobre el preu.")),
    ];

    private static readonly IReadOnlyList<MatchItem> ProgrammingMatch =
    [
        new("programming.terms", 1,
            TermMeaningPrompt,
            [
                (Text.Of("variable|variable|variable|variable"), Text.Of("a named place that stores a value|un lugar con nombre que guarda un valor|un endroit nommé qui stocke une valeur|un lloc amb nom que guarda un valor")),
                (Text.Of("loop|bucle|boucle|bucle"), Text.Of("code that repeats|código que se repite|du code qui se répète|codi que es repeteix")),
                (Text.Of("function|función|fonction|funció"), Text.Of("a reusable block of code|un bloque de código reutilizable|un bloc de code réutilisable|un bloc de codi reutilitzable")),
            ],
            Text.Of("A variable stores a value, a loop repeats code, and a function is a reusable block of code.|Una variable guarda un valor, un bucle repite código y una función es un bloque de código reutilizable.|Une variable stocke une valeur, une boucle répète du code, et une fonction est un bloc de code réutilisable.|Una variable guarda un valor, un bucle repeteix codi i una funció és un bloc de codi reutilitzable.")),
        new("programming.types", 2,
            Text.Of("Match each data type with an example.|Relaciona cada tipo de dato con un ejemplo.|Associe chaque type de donnée à un exemple.|Relaciona cada tipus de dada amb un exemple."),
            [
                (Text.Of("text|texto|texte|text"), Text.Of("\"hello\"")),
                (Text.Of("number|número|nombre|número"), Text.Of("42")),
                (Text.Of("boolean|booleano|booléen|booleà"), Text.Of("true or false|verdadero o falso|vrai ou faux|cert o fals")),
            ],
            Text.Of("Text holds words, numbers hold digits, and a boolean is either true or false.|El texto guarda palabras, los números guardan cifras y un booleano es verdadero o falso.|Le texte contient des mots, les nombres des chiffres, et un booléen est vrai ou faux.|El text guarda paraules, els números guarden xifres i un booleà és cert o fals.")),
        new("programming.terms2", 3,
            TermMeaningPrompt,
            [
                (Text.Of("debugging|depuración|débogage|depuració"), Text.Of("finding and fixing errors|encontrar y corregir errores|trouver et corriger des erreurs|trobar i corregir errors")),
                (Text.Of("compiling|compilación|compilation|compilació"), Text.Of("translating code into a form a computer runs|traducir el código a una forma que un ordenador ejecuta|traduire le code dans une forme qu'un ordinateur exécute|traduir el codi a una forma que un ordinador executa")),
                (Text.Of("syntax|sintaxis|syntaxe|sintaxi"), Text.Of("the rules for how code must be written|las reglas de cómo debe escribirse el código|les règles de la façon dont le code doit être écrit|les regles de com s'ha d'escriure el codi")),
            ],
            Text.Of("Debugging finds errors, compiling translates code to run it, and syntax is the rules for writing code correctly.|La depuración encuentra errores, la compilación traduce el código para ejecutarlo, y la sintaxis son las reglas para escribir código correctamente.|Le débogage trouve les erreurs, la compilation traduit le code pour l'exécuter, et la syntaxe est l'ensemble des règles pour écrire le code correctement.|La depuració troba errors, la compilació tradueix el codi per executar-lo, i la sintaxi són les regles per escriure codi correctament.")),
    ];

    private static readonly Dictionary<Area, IReadOnlyList<MatchItem>> MatchBanks = new()
    {
        [Area.Mathematics] = MathMatch,
        [Area.Physics] = PhysicsMatch,
        [Area.Chemistry] = ChemistryMatch,
        [Area.Biology] = BiologyMatch,
        [Area.History] = HistoryMatch,
        [Area.Geography] = GeographyMatch,
        [Area.Economics] = EconomicsMatch,
        [Area.Programming] = ProgrammingMatch,
    };

    public static PracticeExercise? Match(Area area, Level level, TeachLanguage language, int seed, IReadOnlySet<string>? exclude = null)
    {
        var difficulty = DifficultyFor(level);
        if (MatchBanks.TryGetValue(area, out var genericBank))
        {
            var genericPool = Unseen(genericBank.Where(i => i.Difficulty == difficulty).ToList(), i => i.Topic, "Match", exclude);
            if (genericPool.Count == 0)
            {
                return null;
            }

            var genericItem = genericPool[new Random(seed).Next(genericPool.Count)];
            return new PracticeExercise(genericItem.Topic, genericItem.Difficulty, ExerciseKind.Match, genericItem.Prompt.In(language), genericItem.Explanation.In(language))
            {
                Pairs = genericItem.Pairs.Select(p => new MatchPair(p.Left.In(language), p.Right.In(language))).ToList(),
            };
        }

        if (area == Area.Language)
        {
            var pool = Unseen(LanguageMatch.Where(i => i.Difficulty == difficulty).ToList(), i => i.Topic, "Match", exclude);
            if (pool.Count == 0)
            {
                return null;
            }

            var item = pool[new Random(seed).Next(pool.Count)];
            return new PracticeExercise(item.Topic, item.Difficulty, ExerciseKind.Match, item.Prompt.In(language), item.Explanation.In(language))
            {
                Pairs = item.Pairs.Select(p => new MatchPair(p.Left.In(language), p.Right.In(language))).ToList(),
            };
        }

        if (area == Area.ForeignLanguage)
        {
            if (!ForeignMatchWords.TryGetValue(difficulty, out var words))
            {
                return null;
            }

            var target = QuestionBanks.ForeignTarget(language);
            var explanation = Text.Of("");
            return new PracticeExercise("foreign.vocabulary", difficulty, ExerciseKind.Match,
                ForeignMatchPrompt.Format(language, TargetName(language)), explanation.In(language))
            {
                Pairs = words.Select(w => new MatchPair(w.In(language), w.In(target))).ToList(),
            };
        }

        return null;
    }

    /// <summary>Picks among fill-blank/reorder/match for Language/ForeignLanguage (the subjects without true/false
    /// content — task #71); null for any other subject or if this difficulty tier has no content of any of the 3
    /// kinds yet.</summary>
    private static readonly int[] DrillKindOrder = [0, 1, 2];

    public static PracticeExercise? LanguageDrill(Area area, Level level, TeachLanguage language, int seed)
    {
        if (area is not (Area.Language or Area.ForeignLanguage))
        {
            return null;
        }

        var rng = new Random(seed);
        var order = DrillKindOrder.OrderBy(_ => rng.Next()).ToArray();
        foreach (var pick in order)
        {
            var exercise = pick switch
            {
                0 => FillBlank(area, level, language, seed),
                1 => Reorder(area, level, language, seed),
                _ => Match(area, level, language, seed),
            };
            if (exercise is not null)
            {
                return exercise;
            }
        }

        return null;
    }

    // ---- MultiSelect: "select all that apply" (task #96), all 8 non-language subjects ----

    private sealed record MultiSelectItem(string Topic, int Difficulty, Text Prompt, IReadOnlyList<Text> Options, IReadOnlyList<int> Correct, Text Explanation);

    private static readonly IReadOnlyList<MultiSelectItem> MathMultiSelect =
    [
        new("math.multiselect.even", 1,
            Text.Of("Select all the even numbers.|Selecciona todos los números pares.|Sélectionne tous les nombres pairs.|Selecciona tots els nombres parells."),
            [Text.Of("2|2|2|2"), Text.Of("3|3|3|3"), Text.Of("4|4|4|4"), Text.Of("7|7|7|7")], [0, 2],
            Text.Of("2 and 4 are even (divisible by 2); 3 and 7 are odd.|2 y 4 son pares (divisibles entre 2); 3 y 7 son impares.|2 et 4 sont pairs (divisibles par 2) ; 3 et 7 sont impairs.|2 i 4 són parells (divisibles per 2); 3 i 7 són senars.")),
        new("math.multiselect.fractions_half", 2,
            Text.Of("Select all the fractions equal to one half.|Selecciona todas las fracciones iguales a un medio.|Sélectionne toutes les fractions égales à un demi.|Selecciona totes les fraccions iguals a un mig."),
            [Text.Of("2/4|2/4|2/4|2/4"), Text.Of("3/6|3/6|3/6|3/6"), Text.Of("1/3|1/3|1/3|1/3"), Text.Of("2/5|2/5|2/5|2/5")], [0, 1],
            Text.Of("2/4 and 3/6 both simplify to 1/2; 1/3 and 2/5 don't.|2/4 y 3/6 se simplifican a 1/2; 1/3 y 2/5 no.|2/4 et 3/6 se simplifient tous deux en 1/2 ; 1/3 et 2/5 non.|2/4 i 3/6 es simplifiquen a 1/2; 1/3 i 2/5 no.")),
        new("math.multiselect.primes", 3,
            Text.Of("Select all the prime numbers.|Selecciona todos los números primos.|Sélectionne tous les nombres premiers.|Selecciona tots els nombres primers."),
            [Text.Of("9|9|9|9"), Text.Of("11|11|11|11"), Text.Of("15|15|15|15"), Text.Of("17|17|17|17")], [1, 3],
            Text.Of("11 and 17 have no divisors other than 1 and themselves; 9 = 3×3 and 15 = 3×5 don't.|11 y 17 no tienen más divisores que 1 y ellos mismos; 9 = 3×3 y 15 = 3×5 sí.|11 et 17 n'ont pas d'autres diviseurs qu'eux-mêmes et 1 ; 9 = 3×3 et 15 = 3×5 en ont.|11 i 17 no tenen més divisors que 1 i ells mateixos; 9 = 3×3 i 15 = 3×5 sí.")),
    ];

    private static readonly IReadOnlyList<MultiSelectItem> PhysicsMultiSelect =
    [
        new("physics.multiselect.energy_sources", 1,
            Text.Of("Select all the sources of light.|Selecciona todas las fuentes de luz.|Sélectionne toutes les sources de lumière.|Selecciona totes les fonts de llum."),
            [Text.Of("The Sun|El Sol|Le Soleil|El Sol"), Text.Of("A rock|Una piedra|Une pierre|Una pedra"), Text.Of("A light bulb|Una bombilla|Une ampoule|Una bombeta"), Text.Of("A chair|Una silla|Une chaise|Una cadira")], [0, 2],
            Text.Of("The Sun and a light bulb give off their own light; a rock and a chair don't.|El Sol y una bombilla emiten luz propia; una piedra y una silla no.|Le Soleil et une ampoule émettent leur propre lumière ; une pierre et une chaise non.|El Sol i una bombeta emeten llum pròpia; una pedra i una cadira no.")),
        new("physics.multiselect.states", 2,
            Text.Of("Select all the states of matter water can be in.|Selecciona todos los estados en los que puede estar el agua.|Sélectionne tous les états dans lesquels l'eau peut se trouver.|Selecciona tots els estats en què pot estar l'aigua."),
            [Text.Of("Solid (ice)|Sólido (hielo)|Solide (glace)|Sòlid (gel)"), Text.Of("Plasma|Plasma|Plasma|Plasma"), Text.Of("Liquid|Líquido|Liquide|Líquid"), Text.Of("Gas (vapour)|Gas (vapor)|Gaz (vapeur)|Gas (vapor)")], [0, 2, 3],
            Text.Of("Water is commonly solid, liquid or gas; it only becomes plasma at extremely high temperatures, far beyond everyday conditions.|El agua suele ser sólida, líquida o gaseosa; solo se vuelve plasma a temperaturas extremadamente altas, muy por encima de lo habitual.|L'eau est généralement solide, liquide ou gazeuse ; elle ne devient plasma qu'à des températures extrêmement élevées, bien au-delà du quotidien.|L'aigua sol ser sòlida, líquida o gasosa; només es torna plasma a temperatures extremadament altes, molt per sobre de l'habitual.")),
        new("physics.multiselect.forces", 3,
            Text.Of("Select all the examples of a force.|Selecciona todos los ejemplos de una fuerza.|Sélectionne tous les exemples de force.|Selecciona tots els exemples d'una força."),
            [Text.Of("The colour of a wall|El color de una pared|La couleur d'un mur|El color d'una paret"), Text.Of("Gravity pulling an apple down|La gravedad tirando de una manzana hacia abajo|La gravité qui attire une pomme vers le bas|La gravetat estirant una poma cap avall"), Text.Of("A magnet pulling a nail|Un imán atrayendo un clavo|Un aimant attirant un clou|Un imant atraient un clau"), Text.Of("The name of a shape|El nombre de una forma|Le nom d'une forme|El nom d'una forma")], [1, 2],
            Text.Of("Gravity and magnetism are forces — pushes or pulls between objects; colour and names aren't.|La gravedad y el magnetismo son fuerzas, empujones o tirones entre objetos; el color y los nombres no.|La gravité et le magnétisme sont des forces, des poussées ou des tractions entre objets ; la couleur et les noms n'en sont pas.|La gravetat i el magnetisme són forces, empentes o estirades entre objectes; el color i els noms no.")),
    ];

    private static readonly IReadOnlyList<MultiSelectItem> ChemistryMultiSelect =
    [
        new("chemistry.multiselect.elements", 1,
            Text.Of("Select all the chemical elements.|Selecciona todos los elementos químicos.|Sélectionne tous les éléments chimiques.|Selecciona tots els elements químics."),
            [Text.Of("Oxygen|Oxígeno|Oxygène|Oxigen"), Text.Of("Water|Agua|Eau|Aigua"), Text.Of("Gold|Oro|Or|Or"), Text.Of("Salt|Sal|Sel|Sal")], [0, 2],
            Text.Of("Oxygen and gold are elements (one kind of atom); water and salt are compounds made of several elements.|El oxígeno y el oro son elementos (un solo tipo de átomo); el agua y la sal son compuestos de varios elementos.|L'oxygène et l'or sont des éléments (un seul type d'atome) ; l'eau et le sel sont des composés de plusieurs éléments.|L'oxigen i l'or són elements (un sol tipus d'àtom); l'aigua i la sal són compostos de diversos elements.")),
        new("chemistry.multiselect.acids", 2,
            Text.Of("Select all the substances that are acidic (pH below 7).|Selecciona todas las sustancias ácidas (pH por debajo de 7).|Sélectionne toutes les substances acides (pH inférieur à 7).|Selecciona totes les substàncies àcides (pH per sota de 7)."),
            [Text.Of("Lemon juice|Zumo de limón|Jus de citron|Suc de llimona"), Text.Of("Vinegar|Vinagre|Vinaigre|Vinagre"), Text.Of("Soap|Jabón|Savon|Sabó"), Text.Of("Baking soda|Bicarbonato|Bicarbonate|Bicarbonat")], [0, 1],
            Text.Of("Lemon juice and vinegar are acidic; soap and baking soda are basic (pH above 7).|El zumo de limón y el vinagre son ácidos; el jabón y el bicarbonato son básicos (pH por encima de 7).|Le jus de citron et le vinaigre sont acides ; le savon et le bicarbonate sont basiques (pH supérieur à 7).|El suc de llimona i el vinagre són àcids; el sabó i el bicarbonat són bàsics (pH per sobre de 7).")),
        new("chemistry.multiselect.mixtures", 3,
            Text.Of("Select all the examples of a mixture (not chemically bonded together).|Selecciona todos los ejemplos de una mezcla (no unidos químicamente).|Sélectionne tous les exemples de mélange (non liés chimiquement).|Selecciona tots els exemples d'una mescla (no units químicament)."),
            [Text.Of("Table salt (NaCl)|Sal de mesa (NaCl)|Sel de table (NaCl)|Sal de taula (NaCl)"), Text.Of("Salt water|Agua con sal|Eau salée|Aigua amb sal"), Text.Of("Sand and gravel|Arena y grava|Sable et gravier|Sorra i grava"), Text.Of("Carbon dioxide (CO₂)|Dióxido de carbono (CO₂)|Dioxyde de carbone (CO₂)|Diòxid de carboni (CO₂)")], [1, 2],
            Text.Of("Salt water and sand-and-gravel are mixtures, easy to separate again; table salt and CO₂ are compounds, chemically bonded.|El agua con sal y la mezcla de arena y grava son mezclas, fáciles de separar; la sal de mesa y el CO₂ son compuestos, unidos químicamente.|L'eau salée et le mélange sable-gravier sont des mélanges, faciles à séparer ; le sel de table et le CO₂ sont des composés, liés chimiquement.|L'aigua amb sal i la mescla de sorra i grava són mescles, fàcils de separar; la sal de taula i el CO₂ són compostos, units químicament.")),
    ];

    private static readonly IReadOnlyList<MultiSelectItem> BiologyMultiSelect =
    [
        new("biology.multiselect.mammals", 1,
            Text.Of("Select all the mammals.|Selecciona todos los mamíferos.|Sélectionne tous les mammifères.|Selecciona tots els mamífers."),
            [Text.Of("Dog|Perro|Chien|Gos"), Text.Of("Frog|Rana|Grenouille|Granota"), Text.Of("Whale|Ballena|Baleine|Balena"), Text.Of("Lizard|Lagarto|Lézard|Llangardaix")], [0, 2],
            Text.Of("Dogs and whales are mammals (they nurse their young with milk); frogs and lizards aren't.|El perro y la ballena son mamíferos (amamantan a sus crías); la rana y el lagarto no.|Le chien et la baleine sont des mammifères (ils allaitent leurs petits) ; la grenouille et le lézard non.|El gos i la balena són mamífers (alleten les seves cries); la granota i el llangardaix no.")),
        new("biology.multiselect.photosynthesis", 2,
            Text.Of("Select all the things a plant needs for photosynthesis.|Selecciona todo lo que una planta necesita para la fotosíntesis.|Sélectionne tout ce dont une plante a besoin pour la photosynthèse.|Selecciona tot el que una planta necessita per a la fotosíntesi."),
            [Text.Of("Sunlight|Luz del sol|Lumière du soleil|Llum del sol"), Text.Of("Moonlight|Luz de la luna|Lumière de la lune|Llum de la lluna"), Text.Of("Water|Agua|Eau|Aigua"), Text.Of("Carbon dioxide|Dióxido de carbono|Dioxyde de carbone|Diòxid de carboni")], [0, 2, 3],
            Text.Of("A plant uses sunlight, water and carbon dioxide to make its own food; moonlight is far too weak for that.|Una planta usa luz solar, agua y dióxido de carbono para fabricar su propio alimento; la luz de la luna es demasiado débil para eso.|Une plante utilise la lumière du soleil, l'eau et le dioxyde de carbone pour fabriquer sa propre nourriture ; la lumière de la lune est bien trop faible pour cela.|Una planta fa servir llum solar, aigua i diòxid de carboni per fabricar el seu propi aliment; la llum de la lluna és massa feble per a això.")),
        new("biology.multiselect.vertebrates", 3,
            Text.Of("Select all the animals with a backbone (vertebrates).|Selecciona todos los animales con columna vertebral (vertebrados).|Sélectionne tous les animaux à colonne vertébrale (vertébrés).|Selecciona tots els animals amb columna vertebral (vertebrats)."),
            [Text.Of("Jellyfish|Medusa|Méduse|Medusa"), Text.Of("Shark|Tiburón|Requin|Tauró"), Text.Of("Eagle|Águila|Aigle|Àguila"), Text.Of("Worm|Gusano|Ver de terre|Cuc")], [1, 2],
            Text.Of("Sharks and eagles have a backbone; jellyfish and worms are invertebrates, with no backbone at all.|El tiburón y el águila tienen columna vertebral; la medusa y el gusano son invertebrados, sin columna.|Le requin et l'aigle ont une colonne vertébrale ; la méduse et le ver de terre sont des invertébrés, sans colonne.|El tauró i l'àguila tenen columna vertebral; la medusa i el cuc són invertebrats, sense columna.")),
    ];

    private static readonly IReadOnlyList<MultiSelectItem> HistoryMultiSelect =
    [
        new("history.multiselect.ancient_civilizations", 1,
            Text.Of("Select all the ancient civilizations.|Selecciona todas las civilizaciones antiguas.|Sélectionne toutes les civilisations antiques.|Selecciona totes les civilitzacions antigues."),
            [Text.Of("Ancient Egypt|El antiguo Egipto|L'Égypte antique|L'antic Egipte"), Text.Of("The internet|Internet|Internet|Internet"), Text.Of("Ancient Rome|La antigua Roma|La Rome antique|L'antiga Roma"), Text.Of("Smartphones|Los móviles|Les smartphones|Els mòbils")], [0, 2],
            Text.Of("Ancient Egypt and Ancient Rome existed thousands of years ago; the internet and smartphones are modern inventions.|El antiguo Egipto y la antigua Roma existieron hace miles de años; internet y los móviles son inventos modernos.|L'Égypte antique et la Rome antique existaient il y a des milliers d'années ; Internet et les smartphones sont des inventions modernes.|L'antic Egipte i l'antiga Roma van existir fa milers d'anys; internet i els mòbils són invents moderns.")),
        new("history.multiselect.ww2_allies", 2,
            Text.Of("Select all the countries that were Allied powers in World War II.|Selecciona todos los países que fueron potencias aliadas en la Segunda Guerra Mundial.|Sélectionne tous les pays qui faisaient partie des Alliés durant la Seconde Guerre mondiale.|Selecciona tots els països que van ser potències aliades a la Segona Guerra Mundial."),
            [Text.Of("The United States|Estados Unidos|Les États-Unis|Els Estats Units"), Text.Of("The United Kingdom|El Reino Unido|Le Royaume-Uni|El Regne Unit"), Text.Of("Germany|Alemania|L'Allemagne|Alemanya"), Text.Of("Japan|Japón|Le Japon|El Japó")], [0, 1],
            Text.Of("The United States and the United Kingdom were Allied powers; Germany and Japan were Axis powers, on the other side.|Estados Unidos y el Reino Unido fueron potencias aliadas; Alemania y Japón fueron potencias del Eje, en el otro bando.|Les États-Unis et le Royaume-Uni faisaient partie des Alliés ; l'Allemagne et le Japon faisaient partie de l'Axe, dans l'autre camp.|Els Estats Units i el Regne Unit van ser potències aliades; Alemanya i el Japó van ser potències de l'Eix, a l'altre bàndol.")),
        new("history.multiselect.renaissance", 3,
            Text.Of("Select all the figures of the Renaissance.|Selecciona todas las figuras del Renacimiento.|Sélectionne toutes les figures de la Renaissance.|Selecciona totes les figures del Renaixement."),
            [Text.Of("Napoleon Bonaparte|Napoleón Bonaparte|Napoléon Bonaparte|Napoleó Bonaparte"), Text.Of("Leonardo da Vinci|Leonardo da Vinci|Léonard de Vinci|Leonardo da Vinci"), Text.Of("Michelangelo|Miguel Ángel|Michel-Ange|Miquel Àngel"), Text.Of("Albert Einstein|Albert Einstein|Albert Einstein|Albert Einstein")], [1, 2],
            Text.Of("Leonardo da Vinci and Michelangelo were Renaissance artists (15th-16th century); Napoleon and Einstein lived centuries later.|Leonardo da Vinci y Miguel Ángel fueron artistas del Renacimiento (siglos XV-XVI); Napoleón y Einstein vivieron siglos después.|Léonard de Vinci et Michel-Ange étaient des artistes de la Renaissance (XVe-XVIe siècle) ; Napoléon et Einstein ont vécu des siècles plus tard.|Leonardo da Vinci i Miquel Àngel van ser artistes del Renaixement (segles XV-XVI); Napoleó i Einstein van viure segles després.")),
        // Task #97: more items per difficulty tier, so a drill repeats less often across sessions.
        new("history.multiselect.ancient_inventions", 1,
            Text.Of("Select all the inventions that already existed in ancient times.|Selecciona todos los inventos que ya existían en la Antigüedad.|Sélectionne toutes les inventions qui existaient déjà dans l'Antiquité.|Selecciona tots els invents que ja existien a l'antiguitat."),
            [Text.Of("The wheel|La rueda|La roue|La roda"), Text.Of("Television|La televisión|La télévision|La televisió"), Text.Of("Boats|Los barcos|Les bateaux|Els vaixells"), Text.Of("Airplanes|Los aviones|Les avions|Els avions")], [0, 2],
            Text.Of("The wheel and boats are thousands of years old; television and airplanes appeared in the 20th century.|La rueda y los barcos tienen miles de años; la televisión y los aviones aparecieron en el siglo XX.|La roue et les bateaux ont des milliers d'années ; la télévision et les avions sont apparus au XXᵉ siècle.|La roda i els vaixells tenen milers d'anys; la televisió i els avions van aparèixer al segle XX.")),
        new("history.multiselect.middle_ages", 2,
            Text.Of("Select everything typical of the Middle Ages.|Selecciona todo lo típico de la Edad Media.|Sélectionne tout ce qui est typique du Moyen Âge.|Selecciona tot el que és típic de l'edat mitjana."),
            [Text.Of("Castles|Los castillos|Les châteaux forts|Els castells"), Text.Of("Knights|Los caballeros|Les chevaliers|Els cavallers"), Text.Of("Electric trains|Los trenes eléctricos|Les trains électriques|Els trens elèctrics"), Text.Of("Radio|La radio|La radio|La ràdio")], [0, 1],
            Text.Of("Castles and knights belong to the Middle Ages; electric trains and radio came with the industrial age.|Los castillos y los caballeros son de la Edad Media; los trenes eléctricos y la radio llegaron con la era industrial.|Les châteaux forts et les chevaliers appartiennent au Moyen Âge ; les trains électriques et la radio sont arrivés avec l'ère industrielle.|Els castells i els cavallers són de l'edat mitjana; els trens elèctrics i la ràdio van arribar amb l'era industrial.")),
        new("history.multiselect.eec_founders", 3,
            Text.Of("Select all the founding members of the EEC (1957).|Selecciona todos los miembros fundadores de la CEE (1957).|Sélectionne tous les membres fondateurs de la CEE (1957).|Selecciona tots els membres fundadors de la CEE (1957)."),
            [Text.Of("France|Francia|La France|França"), Text.Of("Spain|España|L'Espagne|Espanya"), Text.Of("Italy|Italia|L'Italie|Itàlia"), Text.Of("The United Kingdom|El Reino Unido|Le Royaume-Uni|El Regne Unit")], [0, 2],
            Text.Of("France and Italy signed the Treaty of Rome in 1957; the United Kingdom joined in 1973 and Spain in 1986.|Francia e Italia firmaron el Tratado de Roma en 1957; el Reino Unido entró en 1973 y España en 1986.|La France et l'Italie ont signé le traité de Rome en 1957 ; le Royaume-Uni est entré en 1973 et l'Espagne en 1986.|França i Itàlia van signar el Tractat de Roma el 1957; el Regne Unit hi va entrar el 1973 i Espanya el 1986.")),
        new("history.multiselect.warsaw_pact", 3,
            Text.Of("Select all the countries that were in the Warsaw Pact.|Selecciona todos los países que formaron parte del Pacto de Varsovia.|Sélectionne tous les pays qui faisaient partie du pacte de Varsovie.|Selecciona tots els països que van formar part del Pacte de Varsòvia."),
            [Text.Of("Poland|Polonia|La Pologne|Polònia"), Text.Of("France|Francia|La France|França"), Text.Of("Hungary|Hungría|La Hongrie|Hongria"), Text.Of("Portugal|Portugal|Le Portugal|Portugal")], [0, 2],
            Text.Of("Poland and Hungary were in the Soviet-led Warsaw Pact; France and Portugal were founding members of NATO.|Polonia y Hungría formaban parte del Pacto de Varsovia, liderado por la URSS; Francia y Portugal fueron miembros fundadores de la OTAN.|La Pologne et la Hongrie faisaient partie du pacte de Varsovie, dirigé par l'URSS ; la France et le Portugal étaient membres fondateurs de l'OTAN.|Polònia i Hongria formaven part del Pacte de Varsòvia, liderat per l'URSS; França i Portugal van ser membres fundadors de l'OTAN.")),
    ];

    private static readonly IReadOnlyList<MultiSelectItem> GeographyMultiSelect =
    [
        new("geography.multiselect.continents", 1,
            Text.Of("Select all the continents.|Selecciona todos los continentes.|Sélectionne tous les continents.|Selecciona tots els continents."),
            [Text.Of("Africa|África|Afrique|Àfrica"), Text.Of("Paris|París|Paris|París"), Text.Of("Asia|Asia|Asie|Àsia"), Text.Of("The Nile|El Nilo|Le Nil|El Nil")], [0, 2],
            Text.Of("Africa and Asia are continents; Paris is a city and the Nile is a river.|África y Asia son continentes; París es una ciudad y el Nilo es un río.|L'Afrique et l'Asie sont des continents ; Paris est une ville et le Nil est un fleuve.|Àfrica i Àsia són continents; París és una ciutat i el Nil és un riu.")),
        new("geography.multiselect.oceans", 2,
            Text.Of("Select all the oceans.|Selecciona todos los océanos.|Sélectionne tous les océans.|Selecciona tots els oceans."),
            [Text.Of("The Pacific|El Pacífico|Le Pacifique|El Pacífic"), Text.Of("The Atlantic|El Atlántico|L'Atlantique|L'Atlàntic"), Text.Of("The Amazon|El Amazonas|L'Amazone|L'Amazones"), Text.Of("The Sahara|El Sáhara|Le Sahara|El Sàhara")], [0, 1],
            Text.Of("The Pacific and the Atlantic are oceans; the Amazon is a river and the Sahara is a desert.|El Pacífico y el Atlántico son océanos; el Amazonas es un río y el Sáhara es un desierto.|Le Pacifique et l'Atlantique sont des océans ; l'Amazone est un fleuve et le Sahara est un désert.|El Pacífic i l'Atlàntic són oceans; l'Amazones és un riu i el Sàhara és un desert.")),
        new("geography.multiselect.capitals", 3,
            Text.Of("Select all the correct country-capital pairs.|Selecciona todos los pares país-capital correctos.|Sélectionne toutes les paires pays-capitale correctes.|Selecciona tots els parells país-capital correctes."),
            [Text.Of("Spain — Lisbon|España — Lisboa|Espagne — Lisbonne|Espanya — Lisboa"), Text.Of("France — Paris|Francia — París|France — Paris|França — París"), Text.Of("Japan — Tokyo|Japón — Tokio|Japon — Tokyo|Japó — Tòquio"), Text.Of("Italy — Berlin|Italia — Berlín|Italie — Berlin|Itàlia — Berlín")], [1, 2],
            Text.Of("Paris is the capital of France and Tokyo of Japan; Spain's capital is Madrid (not Lisbon) and Italy's is Rome (not Berlin).|París es la capital de Francia y Tokio de Japón; la capital de España es Madrid (no Lisboa) y la de Italia es Roma (no Berlín).|Paris est la capitale de la France et Tokyo celle du Japon ; la capitale de l'Espagne est Madrid (pas Lisbonne) et celle de l'Italie est Rome (pas Berlin).|París és la capital de França i Tòquio del Japó; la capital d'Espanya és Madrid (no Lisboa) i la d'Itàlia és Roma (no Berlín).")),
        // Task #97: more items per difficulty tier, so a drill repeats less often across sessions.
        new("geography.multiselect.islands", 1,
            Text.Of("Select all the islands.|Selecciona todas las islas.|Sélectionne toutes les îles.|Selecciona totes les illes."),
            [Text.Of("Mallorca|Mallorca|Majorque|Mallorca"), Text.Of("Madrid"), Text.Of("Tenerife"), Text.Of("Toledo|Toledo|Tolède|Toledo")], [0, 2],
            Text.Of("Mallorca and Tenerife are islands; Madrid and Toledo are inland cities.|Mallorca y Tenerife son islas; Madrid y Toledo son ciudades del interior.|Majorque et Tenerife sont des îles ; Madrid et Tolède sont des villes de l'intérieur.|Mallorca i Tenerife són illes; Madrid i Toledo són ciutats de l'interior.")),
        new("geography.multiselect.europe", 2,
            Text.Of("Select all the countries in Europe.|Selecciona todos los países de Europa.|Sélectionne tous les pays d'Europe.|Selecciona tots els països d'Europa."),
            [Text.Of("Portugal"), Text.Of("Kenya|Kenia|Le Kenya|Kenya"), Text.Of("Poland|Polonia|La Pologne|Polònia"), Text.Of("Peru|Perú|Le Pérou|Perú")], [0, 2],
            Text.Of("Portugal and Poland are in Europe; Kenya is in Africa and Peru in South America.|Portugal y Polonia están en Europa; Kenia está en África y Perú en América del Sur.|Le Portugal et la Pologne sont en Europe ; le Kenya est en Afrique et le Pérou en Amérique du Sud.|Portugal i Polònia són a Europa; Kenya és a l'Àfrica i el Perú a Amèrica del Sud.")),
        new("geography.multiselect.landlocked", 3,
            Text.Of("Select all the countries with no sea coast.|Selecciona todos los países sin costa.|Sélectionne tous les pays sans accès à la mer.|Selecciona tots els països sense costa."),
            [Text.Of("Switzerland|Suiza|La Suisse|Suïssa"), Text.Of("Austria|Austria|L'Autriche|Àustria"), Text.Of("Italy|Italia|L'Italie|Itàlia"), Text.Of("Portugal|Portugal|Le Portugal|Portugal")], [0, 1],
            Text.Of("Switzerland and Austria have no coast at all; Italy and Portugal have long coastlines.|Suiza y Austria no tienen costa; Italia y Portugal tienen costas muy largas.|La Suisse et l'Autriche n'ont aucun accès à la mer ; l'Italie et le Portugal ont de longues côtes.|Suïssa i Àustria no tenen costa; Itàlia i Portugal tenen costes molt llargues.")),
        new("geography.multiselect.southern_hemisphere", 3,
            Text.Of("Select all the countries in the Southern Hemisphere.|Selecciona todos los países del hemisferio sur.|Sélectionne tous les pays de l'hémisphère Sud.|Selecciona tots els països de l'hemisferi sud."),
            [Text.Of("Argentina|Argentina|L'Argentine|Argentina"), Text.Of("Canada|Canadá|Le Canada|Canadà"), Text.Of("Australia|Australia|L'Australie|Austràlia"), Text.Of("Japan|Japón|Le Japon|Japó")], [0, 2],
            Text.Of("Argentina and Australia lie south of the equator; Canada and Japan are in the Northern Hemisphere.|Argentina y Australia están al sur del ecuador; Canadá y Japón están en el hemisferio norte.|L'Argentine et l'Australie sont au sud de l'équateur ; le Canada et le Japon sont dans l'hémisphère Nord.|L'Argentina i Austràlia són al sud de l'equador; el Canadà i el Japó són a l'hemisferi nord.")),
    ];

    private static readonly IReadOnlyList<MultiSelectItem> EconomicsMultiSelect =
    [
        new("economics.multiselect.needs", 1,
            Text.Of("Select all the basic needs.|Selecciona todas las necesidades básicas.|Sélectionne tous les besoins de base.|Selecciona totes les necessitats bàsiques."),
            [Text.Of("Food|Comida|Nourriture|Menjar"), Text.Of("A video game console|Una consola de videojuegos|Une console de jeux vidéo|Una consola de videojocs"), Text.Of("Shelter|Vivienda|Un logement|Un habitatge"), Text.Of("A luxury car|Un coche de lujo|Une voiture de luxe|Un cotxe de luxe")], [0, 2],
            Text.Of("Food and shelter are basic needs everyone requires; a game console and a luxury car are wants, nice to have but not essential.|La comida y la vivienda son necesidades básicas de todos; una consola y un coche de lujo son deseos, agradables pero no esenciales.|La nourriture et le logement sont des besoins de base pour tous ; une console et une voiture de luxe sont des envies, agréables mais pas essentielles.|El menjar i l'habitatge són necessitats bàsiques de tothom; una consola i un cotxe de luxe són desitjos, agradables però no essencials.")),
        new("economics.multiselect.income", 2,
            Text.Of("Select all the examples of income.|Selecciona todos los ejemplos de ingresos.|Sélectionne tous les exemples de revenu.|Selecciona tots els exemples d'ingressos."),
            [Text.Of("A salary from a job|Un salario del trabajo|Un salaire d'un emploi|Un salari de la feina"), Text.Of("Money from selling vegetables you grew|Dinero por vender verduras que cultivaste|L'argent de la vente de légumes que tu as cultivés|Diners per vendre verdures que has cultivat"), Text.Of("Paying rent|Pagar el alquiler|Payer un loyer|Pagar el lloguer"), Text.Of("Buying groceries|Comprar comida|Faire les courses|Comprar menjar")], [0, 1],
            Text.Of("A salary and money from selling something are income, money coming in; rent and groceries are expenses, money going out.|Un salario y el dinero por vender algo son ingresos, dinero que entra; el alquiler y la compra de comida son gastos, dinero que sale.|Un salaire et l'argent d'une vente sont des revenus, de l'argent qui entre ; le loyer et les courses sont des dépenses, de l'argent qui sort.|Un salari i els diners per vendre alguna cosa són ingressos, diners que entren; el lloguer i la compra de menjar són despeses, diners que surten.")),
        new("economics.multiselect.supply_demand", 3,
            Text.Of("Select all the situations that would make a product's price go up.|Selecciona todas las situaciones que harían subir el precio de un producto.|Sélectionne toutes les situations qui feraient augmenter le prix d'un produit.|Selecciona totes les situacions que farien pujar el preu d'un producte."),
            [Text.Of("A new factory floods the market with it|Una nueva fábrica inunda el mercado con él|Une nouvelle usine en inonde le marché|Una nova fàbrica n'inunda el mercat"), Text.Of("Demand rises while supply stays the same|La demanda sube y la oferta se mantiene|La demande augmente et l'offre reste la même|La demanda puja i l'oferta es manté"), Text.Of("A shortage reduces how much is available|Una escasez reduce cuánto hay disponible|Une pénurie réduit la quantité disponible|Una escassetat redueix la quantitat disponible"), Text.Of("Nobody wants to buy it anymore|Ya nadie quiere comprarlo|Plus personne ne veut l'acheter|Ja ningú el vol comprar")], [1, 2],
            Text.Of("Rising demand or a shrinking supply both push the price up; flooding the market or losing all buyers push it down instead.|Más demanda o menos oferta suben el precio; inundar el mercado o perder a todos los compradores lo bajan.|Une demande en hausse ou une offre réduite font monter le prix ; inonder le marché ou perdre tous les acheteurs le font baisser.|Més demanda o menys oferta pugen el preu; inundar el mercat o perdre tots els compradors el fan baixar.")),
        // Task #97: more items per difficulty tier, so a drill repeats less often across sessions.
        new("economics.multiselect.taxes", 3,
            Text.Of("Select all the taxes.|Selecciona todos los impuestos.|Sélectionne tous les impôts.|Selecciona tots els impostos."),
            [Text.Of("VAT|IVA|TVA|IVA"), Text.Of("Income tax|Impuesto sobre la renta (IRPF)|Impôt sur le revenu|Impost sobre la renda (IRPF)"), Text.Of("A salary|Un salario|Un salaire|Un salari"), Text.Of("A loan|Un préstamo|Un prêt|Un préstec")], [0, 1],
            Text.Of("VAT and income tax are taxes paid to the state; a salary is income from work and a loan is borrowed money.|El IVA y el IRPF son impuestos que se pagan al Estado; un salario es un ingreso por trabajar y un préstamo es dinero prestado.|La TVA et l'impôt sur le revenu sont des impôts payés à l'État ; un salaire est un revenu du travail et un prêt de l'argent emprunté.|L'IVA i l'IRPF són impostos que es paguen a l'Estat; un salari és un ingrés per treballar i un préstec són diners prestats.")),
        new("economics.multiselect.fixed_costs", 3,
            Text.Of("Select the fixed costs of a bakery (they stay the same however much bread it makes).|Selecciona los costes fijos de una panadería (no cambian por mucho pan que haga).|Sélectionne les coûts fixes d'une boulangerie (ils ne changent pas, quelle que soit la quantité de pain).|Selecciona els costos fixos d'un forn de pa (no canvien per molt pa que faci)."),
            [Text.Of("Rent of the shop|Alquiler del local|Loyer du local|Lloguer del local"), Text.Of("Flour|Harina|Farine|Farina"), Text.Of("Insurance|Seguro|Assurance|Assegurança"), Text.Of("Paper bags|Bolsas de papel|Sacs en papier|Bosses de paper")], [0, 2],
            Text.Of("Rent and insurance cost the same however much bread is made; flour and bags grow with production, so they are variable costs.|El alquiler y el seguro cuestan lo mismo se haga el pan que se haga; la harina y las bolsas crecen con la producción, así que son costes variables.|Le loyer et l'assurance coûtent pareil quelle que soit la production ; la farine et les sacs augmentent avec elle, ce sont des coûts variables.|El lloguer i l'assegurança costen el mateix es faci el pa que es faci; la farina i les bosses creixen amb la producció, així que són costos variables.")),
        new("economics.multiselect.inflation_causes", 3,
            Text.Of("Select everything that can push inflation up.|Selecciona todo lo que puede hacer subir la inflación.|Sélectionne tout ce qui peut faire monter l'inflation.|Selecciona tot el que pot fer pujar la inflació."),
            [Text.Of("Demand growing faster than production|Que la demanda crezca más que la producción|Une demande qui augmente plus vite que la production|Que la demanda creixi més que la producció"), Text.Of("A big rise in energy prices|Una gran subida del precio de la energía|Une forte hausse des prix de l'énergie|Una gran pujada del preu de l'energia"), Text.Of("The central bank raising interest rates|Que el banco central suba los tipos de interés|La banque centrale qui relève ses taux|Que el banc central apugi els tipus d'interès"), Text.Of("Wages falling across the economy|Que los salarios bajen en toda la economía|Une baisse générale des salaires|Que els salaris baixin a tota l'economia")], [0, 1],
            Text.Of("Too much demand or more expensive energy push prices up; higher interest rates and falling wages tend to cool them down.|El exceso de demanda o una energía más cara suben los precios; tipos más altos y salarios a la baja tienden a frenarlos.|Trop de demande ou une énergie plus chère font monter les prix ; des taux plus élevés et des salaires en baisse tendent à les freiner.|L'excés de demanda o una energia més cara apugen els preus; tipus més alts i salaris a la baixa tendeixen a frenar-los.")),
        new("economics.multiselect.gdp", 3,
            Text.Of("Select everything that counts in a country's GDP.|Selecciona todo lo que cuenta en el PIB de un país.|Sélectionne tout ce qui compte dans le PIB d'un pays.|Selecciona tot el que compta en el PIB d'un país."),
            [Text.Of("A new car made in a local factory|Un coche nuevo fabricado en una fábrica del país|Une voiture neuve fabriquée dans une usine du pays|Un cotxe nou fabricat en una fàbrica del país"), Text.Of("A haircut at a hairdresser's|Un corte de pelo en una peluquería|Une coupe chez le coiffeur|Un tall de cabell en una perruqueria"), Text.Of("Selling a second-hand bike|Vender una bici de segunda mano|La vente d'un vélo d'occasion|Vendre una bici de segona mà"), Text.Of("Buying shares of a company|Comprar acciones de una empresa|L'achat d'actions d'une entreprise|Comprar accions d'una empresa")], [0, 1],
            Text.Of("GDP counts newly produced goods and services; selling second-hand items or buying shares only changes who owns something that already exists.|El PIB cuenta los bienes y servicios producidos de nuevo; vender objetos usados o comprar acciones solo cambia de manos algo que ya existía.|Le PIB compte les biens et services nouvellement produits ; vendre un objet d'occasion ou acheter des actions ne fait que changer le propriétaire de quelque chose qui existe déjà.|El PIB compta els béns i serveis produïts de nou; vendre objectes de segona mà o comprar accions només canvia de mans una cosa que ja existia.")),
    ];

    private static readonly IReadOnlyList<MultiSelectItem> ProgrammingMultiSelect =
    [
        new("programming.multiselect.data_types", 1,
            Text.Of("Select all the examples of a number.|Selecciona todos los ejemplos de un número.|Sélectionne tous les exemples de nombre.|Selecciona tots els exemples d'un nombre."),
            [Text.Of("42|42|42|42"), Text.Of("\"hello\"|\"hola\"|« bonjour »|\"hola\""), Text.Of("7|7|7|7"), Text.Of("true|true|true|true")], [0, 2],
            Text.Of("42 and 7 are numbers; \"hello\" is text and true is a boolean (yes/no) value.|42 y 7 son números; \"hola\" es texto y true es un valor booleano (sí/no).|42 et 7 sont des nombres ; « bonjour » est du texte et true est une valeur booléenne (oui/non).|42 i 7 són nombres; \"hola\" és text i true és un valor booleà (sí/no).")),
        new("programming.multiselect.loops", 2,
            Text.Of("Select all the situations where a loop is useful.|Selecciona todas las situaciones donde un bucle es útil.|Sélectionne toutes les situations où une boucle est utile.|Selecciona totes les situacions on un bucle és útil."),
            [Text.Of("Printing the numbers 1 to 100|Imprimir los números del 1 al 100|Afficher les nombres de 1 à 100|Imprimir els números de l'1 al 100"), Text.Of("Checking every item in a long list|Revisar cada elemento de una lista larga|Vérifier chaque élément d'une longue liste|Revisar cada element d'una llista larga"), Text.Of("Saying your name once|Decir tu nombre una vez|Dire ton nom une fois|Dir el teu nom una vegada"), Text.Of("Adding two fixed numbers together|Sumar dos números fijos|Additionner deux nombres fixes|Sumar dos números fixos")], [0, 1],
            Text.Of("A loop repeats an action, so it fits printing 100 numbers or checking many items; saying your name once or a single addition needs no repetition.|Un bucle repite una acción, así que sirve para imprimir 100 números o revisar muchos elementos; decir tu nombre una vez o una sola suma no necesita repetición.|Une boucle répète une action, donc elle convient pour afficher 100 nombres ou vérifier plusieurs éléments ; dire son nom une fois ou une seule addition n'a pas besoin de répétition.|Un bucle repeteix una acció, així que serveix per imprimir 100 números o revisar molts elements; dir el teu nom una vegada o una sola suma no necessita repetició.")),
        new("programming.multiselect.bugs", 3,
            Text.Of("Select all the things that can cause a bug in a program.|Selecciona todo lo que puede causar un error (bug) en un programa.|Sélectionne tout ce qui peut causer un bogue dans un programme.|Selecciona tot el que pot causar un error (bug) en un programa."),
            [Text.Of("A well-tested function|Una función bien probada|Une fonction bien testée|Una funció ben provada"), Text.Of("A misspelled variable name|Un nombre de variable mal escrito|Un nom de variable mal orthographié|Un nom de variable mal escrit"), Text.Of("Dividing a number by zero|Dividir un número entre cero|Diviser un nombre par zéro|Dividir un número entre zero"), Text.Of("A clear comment explaining the code|Un comentario claro que explica el código|Un commentaire clair expliquant le code|Un comentari clar que explica el codi")], [1, 2],
            Text.Of("A misspelled name or dividing by zero can crash or break a program; a well-tested function and a clear comment help prevent bugs instead.|Un nombre mal escrito o dividir entre cero pueden romper un programa; una función bien probada y un comentario claro ayudan a evitar errores.|Un nom mal orthographié ou une division par zéro peuvent faire planter un programme ; une fonction bien testée et un commentaire clair aident à éviter les bogues.|Un nom mal escrit o dividir entre zero poden trencar un programa; una funció ben provada i un comentari clar ajuden a evitar errors.")),
    ];

    private static readonly Dictionary<Area, IReadOnlyList<MultiSelectItem>> MultiSelectBanks = new()
    {
        [Area.Mathematics] = MathMultiSelect,
        [Area.Physics] = PhysicsMultiSelect,
        [Area.Chemistry] = ChemistryMultiSelect,
        [Area.Biology] = BiologyMultiSelect,
        [Area.History] = HistoryMultiSelect,
        [Area.Geography] = GeographyMultiSelect,
        [Area.Economics] = EconomicsMultiSelect,
        [Area.Programming] = ProgrammingMultiSelect,
    };

    /// <summary>A "select all that apply" drill, or null if the bank has none for this subject/level
    /// (today: Language and ForeignLanguage, same gap as <see cref="TrueFalse"/> — this kind doesn't fit a
    /// translation/grammar exercise as naturally as fill-blank/reorder/match already do there).</summary>
    public static PracticeExercise? MultiSelect(Area area, Level level, TeachLanguage language, int seed, IReadOnlySet<string>? exclude = null)
    {
        if (!MultiSelectBanks.TryGetValue(area, out var bank))
        {
            return null;
        }

        var pool = Unseen(bank.Where(i => i.Difficulty == DifficultyFor(level)).ToList(), i => i.Topic, "MultiSelect", exclude);
        if (pool.Count == 0)
        {
            return null;
        }

        var item = pool[new Random(seed).Next(pool.Count)];
        return new PracticeExercise(item.Topic, item.Difficulty, ExerciseKind.MultiSelect, item.Prompt.In(language), item.Explanation.In(language))
        {
            Options = item.Options.Select(o => o.In(language)).ToList(),
            CorrectIndices = item.Correct,
        };
    }

    /// <summary>"Listen and answer" (task #114): the student hears the prompt instead of reading it, then picks
    /// from the same 4 options a classic multiple-choice question would show. Reuses <see cref="QuestionBanks.Generate"/>
    /// directly rather than a curated bank, so every subject, level and language already has content — the one drill
    /// kind that never returns null. <paramref name="exclude"/> avoids repeating a topic already heard this attempt,
    /// same as the other kinds, but falls back to a repeat (bounded retries) instead of ever returning nothing,
    /// since unlike the curated banks there's no smaller "full pool" to fall back to.</summary>
    public static PracticeExercise Listen(Area area, Level level, TeachLanguage language, int seed, IReadOnlySet<string>? exclude = null)
    {
        var difficulty = DifficultyFor(level);
        var q = QuestionBanks.Generate(area, level, difficulty, seed, language);
        for (var retry = 1; retry < 20 && exclude is not null && exclude.Contains($"{ExerciseKind.Listen}|{q.Topic}"); retry++)
        {
            q = QuestionBanks.Generate(area, level, difficulty, unchecked(seed + retry * 101), language);
        }

        return new PracticeExercise(q.Topic, q.Difficulty, ExerciseKind.Listen, q.Prompt, q.Explanation)
        {
            Options = q.Options,
            CorrectIndex = q.CorrectIndex,
        };
    }

    private static readonly int[] AnyDrillKindOrder = [0, 1, 2, 3, 4, 5];

    /// <summary>Picks among true/false, fill-blank, reorder, match, multi-select and listen for any subject (task
    /// #96 added multi-select — several toggled picks, not one tap; task #114 added listen — hearing the prompt
    /// instead of reading it, drawn from the ordinary question generator instead of a curated bank); null if none
    /// of the 6 kinds has content for this area/level yet (only possible for the first 5 — Listen always has
    /// content, so a subject with no other drill kind at all will still get Listen drills).
    /// <paramref name="allowTrueFalse"/> lets a caller exclude the 50/50-guessable true/false kind (task #74:
    /// QuizStore excludes it specifically for Placement, where that guess rate would add noise to the binary-search
    /// level calibration) while still mixing in the rest. <paramref name="exclude"/> (task #95/#97) is the set of
    /// "Kind|Topic" keys already shown this attempt (from <c>QuizAttempt.UsedJson</c>): every picker below drops an
    /// item already seen, as long as its tier still has another one left.</summary>
    public static PracticeExercise? AnyDrill(Area area, Level level, TeachLanguage language, int seed, bool allowTrueFalse = true, IReadOnlySet<string>? exclude = null)
    {
        var rng = new Random(seed);
        var order = AnyDrillKindOrder.OrderBy(_ => rng.Next()).ToArray();
        foreach (var pick in order)
        {
            if (pick == 0 && !allowTrueFalse)
            {
                continue;
            }

            var exercise = pick switch
            {
                0 => TrueFalse(area, level, language, seed, exclude),
                1 => FillBlank(area, level, language, seed, exclude),
                2 => Reorder(area, level, language, seed, exclude),
                3 => Match(area, level, language, seed, exclude),
                4 => MultiSelect(area, level, language, seed, exclude),
                _ => Listen(area, level, language, seed, exclude),
            };
            if (exercise is not null)
            {
                return exercise;
            }
        }

        return null;
    }
}
