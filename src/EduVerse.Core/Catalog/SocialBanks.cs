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

        // Task #97: more events per level, so years, centuries and chronology stop cycling through the same handful.
        (1903, Level.Primary2, Text.Of("the Wright brothers make the first powered flight|los hermanos Wright realizan el primer vuelo a motor|les frères Wright réalisent le premier vol motorisé|els germans Wright fan el primer vol amb motor")),
        (1912, Level.Primary2, Text.Of("the Titanic sinks|se hunde el Titanic|le Titanic fait naufrage|s'enfonsa el Titanic")),
        (1961, Level.Primary2, Text.Of("Yuri Gagarin becomes the first person in space|Yuri Gagarin se convierte en el primer ser humano en el espacio|Youri Gagarine devient le premier humain dans l'espace|Iuri Gagarin esdevé el primer ésser humà a l'espai")),
        (1928, Level.Primary2, Text.Of("Alexander Fleming discovers penicillin|Alexander Fleming descubre la penicilina|Alexander Fleming découvre la pénicilline|Alexander Fleming descobreix la penicil·lina")),
        (1886, Level.Primary2, Text.Of("Karl Benz patents the first motor car|Karl Benz patenta el primer automóvil|Karl Benz dépose le brevet de la première automobile|Karl Benz patenta el primer automòbil")),
        (1896, Level.Primary2, Text.Of("the first modern Olympic Games are held in Athens|se celebran en Atenas los primeros Juegos Olímpicos modernos|les premiers Jeux olympiques modernes ont lieu à Athènes|se celebren a Atenes els primers Jocs Olímpics moderns")),
        (1992, Level.Primary2, Text.Of("Barcelona hosts the Olympic Games|Barcelona acoge los Juegos Olímpicos|Barcelone accueille les Jeux olympiques|Barcelona acull els Jocs Olímpics")),
        (1953, Level.Primary2, Text.Of("Hillary and Tenzing are the first to reach the top of Everest|Hillary y Tenzing son los primeros en llegar a la cima del Everest|Hillary et Tenzing sont les premiers à atteindre le sommet de l'Everest|Hillary i Tenzing són els primers a arribar al cim de l'Everest")),
        (711, Level.Primary3, Text.Of("Muslim armies cross into the Iberian Peninsula|los ejércitos musulmanes entran en la península ibérica|les armées musulmanes entrent dans la péninsule Ibérique|els exèrcits musulmans entren a la península Ibèrica")),
        (1347, Level.Primary3, Text.Of("the Black Death reaches Europe|la peste negra llega a Europa|la peste noire atteint l'Europe|la pesta negra arriba a Europa")),
        (1519, Level.Primary3, Text.Of("the Magellan–Elcano expedition sets sail around the world|zarpa la expedición de Magallanes y Elcano alrededor del mundo|l'expédition de Magellan et Elcano part faire le tour du monde|salpa l'expedició de Magallanes i Elcano al voltant del món")),
        (1588, Level.Primary3, Text.Of("the Spanish Armada sails against England|la Gran Armada navega contra Inglaterra|l'Invincible Armada fait voile contre l'Angleterre|la Gran Armada navega contra Anglaterra")),
        (1605, Level.Primary3, Text.Of("the first part of Don Quixote is published|se publica la primera parte del Quijote|la première partie de Don Quichotte est publiée|es publica la primera part del Quixot")),
        (1939, Level.Primary3, Text.Of("the Second World War begins|empieza la Segunda Guerra Mundial|la Seconde Guerre mondiale commence|comença la Segona Guerra Mundial")),
        (1095, Level.Secondary1, Text.Of("Pope Urban II calls the First Crusade|el papa Urbano II convoca la Primera Cruzada|le pape Urbain II appelle à la première croisade|el papa Urbà II convoca la Primera Croada")),
        (1494, Level.Secondary1, Text.Of("Castile and Portugal sign the Treaty of Tordesillas|Castilla y Portugal firman el Tratado de Tordesillas|la Castille et le Portugal signent le traité de Tordesillas|Castella i Portugal signen el Tractat de Tordesillas")),
        (1517, Level.Secondary1, Text.Of("Martin Luther publishes his 95 theses|Martín Lutero publica sus 95 tesis|Martin Luther publie ses 95 thèses|Martí Luter publica les seves 95 tesis")),
        (1543, Level.Secondary1, Text.Of("Copernicus publishes his heliocentric theory|Copérnico publica su teoría heliocéntrica|Copernic publie sa théorie héliocentrique|Copèrnic publica la seva teoria heliocèntrica")),
        (1610, Level.Secondary1, Text.Of("Galileo observes the moons of Jupiter with a telescope|Galileo observa con un telescopio las lunas de Júpiter|Galilée observe les lunes de Jupiter au télescope|Galileu observa amb un telescopi les llunes de Júpiter")),
        (1769, Level.Secondary1, Text.Of("James Watt patents his improved steam engine|James Watt patenta su máquina de vapor mejorada|James Watt fait breveter sa machine à vapeur améliorée|James Watt patenta la seva màquina de vapor millorada")),
        (1808, Level.Secondary2, Text.Of("the Spanish War of Independence against Napoleon begins|empieza la guerra de la Independencia española contra Napoleón|la guerre d'indépendance espagnole contre Napoléon commence|comença la guerra del Francès contra Napoleó")),
        (1812, Level.Secondary2, Text.Of("the Cortes of Cádiz approve Spain's first constitution|las Cortes de Cádiz aprueban la primera constitución española|les Cortes de Cadix adoptent la première Constitution espagnole|les Corts de Cadis aproven la primera constitució espanyola")),
        (1898, Level.Secondary2, Text.Of("Spain loses Cuba, Puerto Rico and the Philippines|España pierde Cuba, Puerto Rico y Filipinas|l'Espagne perd Cuba, Porto Rico et les Philippines|Espanya perd Cuba, Puerto Rico i les Filipines")),
        (1929, Level.Secondary2, Text.Of("the Wall Street Crash|el crac de Wall Street|le krach de Wall Street|el crac de Wall Street")),
        (1948, Level.Secondary2, Text.Of("the UN adopts the Universal Declaration of Human Rights|la ONU aprueba la Declaración Universal de los Derechos Humanos|l'ONU adopte la Déclaration universelle des droits de l'homme|l'ONU aprova la Declaració Universal dels Drets Humans")),
        (1986, Level.Secondary2, Text.Of("Spain and Portugal join the EEC|España y Portugal entran en la CEE|l'Espagne et le Portugal entrent dans la CEE|Espanya i Portugal entren a la CEE")),
        (1848, Level.HighSchool, Text.Of("Marx and Engels publish the Communist Manifesto|Marx y Engels publican el Manifiesto comunista|Marx et Engels publient le Manifeste du parti communiste|Marx i Engels publiquen el Manifest comunista")),
        (1868, Level.HighSchool, Text.Of("the Meiji Restoration begins in Japan|empieza la Restauración Meiji en Japón|la restauration de Meiji commence au Japon|comença la Restauració Meiji al Japó")),
        (1871, Level.HighSchool, Text.Of("the German Empire is proclaimed at Versailles|se proclama el Imperio alemán en Versalles|l'Empire allemand est proclamé à Versailles|es proclama l'Imperi alemany a Versalles")),
        (1947, Level.HighSchool, Text.Of("India becomes independent|la India se independiza|l'Inde devient indépendante|l'Índia s'independitza")),
        (1955, Level.HighSchool, Text.Of("the Warsaw Pact is founded|se funda el Pacto de Varsovia|le pacte de Varsovie est fondé|es funda el Pacte de Varsòvia")),
        (1973, Level.HighSchool, Text.Of("the oil crisis begins|empieza la crisis del petróleo|le premier choc pétrolier commence|comença la crisi del petroli")),
        (1991, Level.HighSchool, Text.Of("the Soviet Union is dissolved|se disuelve la Unión Soviética|l'Union soviétique est dissoute|es dissol la Unió Soviètica")),
        (1993, Level.HighSchool, Text.Of("the Maastricht Treaty comes into force and creates the EU|entra en vigor el Tratado de Maastricht, que crea la UE|le traité de Maastricht entre en vigueur et crée l'UE|entra en vigor el Tractat de Maastricht, que crea la UE")),
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

        // Task #97: people for the first levels (Primary2 had none) and more historians for Bachelor–Doctorate.
        Fact.Of(Level.Primary2, "history.people", "person", "Who painted the Mona Lisa?|¿Quién pintó la Gioconda?|Qui a peint La Joconde ?|Qui va pintar la Gioconda?", "Leonardo da Vinci|Leonardo da Vinci|Léonard de Vinci|Leonardo da Vinci"),
        Fact.Of(Level.Primary2, "history.people", "person", "Who was the first person to walk on the Moon?|¿Quién fue la primera persona en pisar la Luna?|Qui fut la première personne à marcher sur la Lune ?|Qui va ser la primera persona a trepitjar la Lluna?", "Neil Armstrong"),
        Fact.Of(Level.Primary2, "history.people", "person", "Who wrote Don Quixote?|¿Quién escribió el Quijote?|Qui a écrit Don Quichotte ?|Qui va escriure el Quixot?", "Miguel de Cervantes"),
        Fact.Of(Level.Primary2, "history.people", "person", "Which young pharaoh's tomb was found almost intact in 1922?|¿De qué joven faraón se encontró la tumba casi intacta en 1922?|De quel jeune pharaon a-t-on trouvé la tombe presque intacte en 1922 ?|De quin jove faraó es va trobar la tomba gairebé intacta el 1922?", "Tutankhamun|Tutankamón|Toutânkhamon|Tutankamon"),
        Fact.Of(Level.Primary3, "history.people", "person", "Which Carthaginian general crossed the Alps with elephants?|¿Qué general cartaginés cruzó los Alpes con elefantes?|Quel général carthaginois a traversé les Alpes avec des éléphants ?|Quin general cartaginès va travessar els Alps amb elefants?", "Hannibal|Aníbal|Hannibal|Anníbal"),
        Fact.Of(Level.Primary3, "history.people", "person", "Which Macedonian king built an empire that reached India?|¿Qué rey macedonio creó un imperio que llegó hasta la India?|Quel roi macédonien a bâti un empire allant jusqu'en Inde ?|Quin rei macedoni va crear un imperi que arribava fins a l'Índia?", "Alexander the Great|Alejandro Magno|Alexandre le Grand|Alexandre el Gran"),
        Fact.Of(Level.Primary3, "history.people", "person", "Which young French heroine led troops in the Hundred Years' War?|¿Qué joven heroína francesa dirigió tropas en la guerra de los Cien Años?|Quelle jeune héroïne française a mené des troupes pendant la guerre de Cent Ans ?|Quina jove heroïna francesa va dirigir tropes a la guerra dels Cent Anys?", "Joan of Arc|Juana de Arco|Jeanne d'Arc|Joana d'Arc"),
        Fact.Of(Level.Primary3, "history.people", "person", "Who was the first president of the United States?|¿Quién fue el primer presidente de Estados Unidos?|Qui fut le premier président des États-Unis ?|Qui va ser el primer president dels Estats Units?", "George Washington"),
        Fact.Of(Level.Secondary1, "history.people", "person", "Who led the Spanish conquest of the Aztec Empire?|¿Quién dirigió la conquista española del Imperio azteca?|Qui a mené la conquête espagnole de l'Empire aztèque ?|Qui va dirigir la conquesta espanyola de l'Imperi asteca?", "Hernán Cortés"),
        Fact.Of(Level.Secondary1, "history.people", "person", "Which Mongol leader founded the largest land empire in history?|¿Qué líder mongol fundó el mayor imperio terrestre de la historia?|Quel chef mongol a fondé le plus grand empire terrestre de l'histoire ?|Quin líder mongol va fundar el major imperi terrestre de la història?", "Genghis Khan|Gengis Kan|Gengis Khan|Genguis Khan"),
        Fact.Of(Level.Secondary1, "history.people", "person", "Who completed the first voyage around the world after Magellan died?|¿Quién completó la primera vuelta al mundo tras la muerte de Magallanes?|Qui a achevé le premier tour du monde après la mort de Magellan ?|Qui va completar la primera volta al món després de la mort de Magallanes?", "Juan Sebastián Elcano|Juan Sebastián Elcano|Juan Sebastián Elcano|Joan Sebastià Elcano"),
        Fact.Of(Level.Secondary1, "history.people", "person", "Who started the Protestant Reformation?|¿Quién inició la Reforma protestante?|Qui a lancé la Réforme protestante ?|Qui va iniciar la Reforma protestant?", "Martin Luther|Martín Lutero|Martin Luther|Martí Luter"),
        Fact.Of(Level.Secondary2, "history.people", "person", "Who led India's independence movement through non-violence?|¿Quién dirigió el movimiento de independencia de la India mediante la no violencia?|Qui a dirigé le mouvement d'indépendance de l'Inde par la non-violence ?|Qui va dirigir el moviment d'independència de l'Índia mitjançant la no-violència?", "Mahatma Gandhi"),
        Fact.Of(Level.Secondary2, "history.people", "person", "Which leader of South American independence is called \"the Liberator\"?|¿A qué líder de la independencia sudamericana se le llama «el Libertador»?|Quel chef de l'indépendance sud-américaine est surnommé « le Libertador » ?|A quin líder de la independència sud-americana se l'anomena «el Llibertador»?", "Simón Bolívar"),
        Fact.Of(Level.Secondary2, "history.people", "person", "Which Soviet leader launched the reforms called perestroika?|¿Qué dirigente soviético impulsó las reformas llamadas perestroika?|Quel dirigeant soviétique a lancé les réformes appelées perestroïka ?|Quin dirigent soviètic va impulsar les reformes anomenades perestroika?", "Mikhail Gorbachev|Mijaíl Gorbachov|Mikhaïl Gorbatchev|Mikhaïl Gorbatxov"),
        Fact.Of(Level.HighSchool, "history.people", "person", "Which Prussian chancellor unified Germany in 1871?|¿Qué canciller prusiano unificó Alemania en 1871?|Quel chancelier prussien a unifié l'Allemagne en 1871 ?|Quin canceller prussià va unificar Alemanya el 1871?", "Otto von Bismarck"),
        Fact.Of(Level.HighSchool, "history.people", "person", "Which Ottoman sultan conquered Constantinople in 1453?|¿Qué sultán otomano conquistó Constantinopla en 1453?|Quel sultan ottoman a conquis Constantinople en 1453 ?|Quin soldà otomà va conquerir Constantinoble el 1453?", "Mehmed II"),
        Fact.Of(Level.HighSchool, "history.historiography", "historian", "Which Greek writer is called the \"father of history\"?|¿Qué autor griego es llamado el «padre de la historia»?|Quel auteur grec est appelé le « père de l'histoire » ?|Quin autor grec és anomenat el «pare de la història»?", "Herodotus|Heródoto|Hérodote|Heròdot"),
        Fact.Of(Level.Bachelor, "history.people", "person", "Who wrote \"The Prince\" (1513)?|¿Quién escribió «El príncipe» (1513)?|Qui a écrit « Le Prince » (1513) ?|Qui va escriure «El príncep» (1513)?", "Niccolò Machiavelli|Nicolás Maquiavelo|Nicolas Machiavel|Nicolau Maquiavel"),
        Fact.Of(Level.Bachelor, "history.historiography", "historian", "Which Athenian wrote the History of the Peloponnesian War?|¿Qué ateniense escribió la Historia de la guerra del Peloponeso?|Quel Athénien a écrit l'Histoire de la guerre du Péloponnèse ?|Quin atenès va escriure la Història de la guerra del Peloponès?", "Thucydides|Tucídides|Thucydide|Tucídides"),
        Fact.Of(Level.Bachelor, "history.historiography", "historian", "Who wrote \"The History of the Decline and Fall of the Roman Empire\"?|¿Quién escribió la «Historia de la decadencia y caída del Imperio romano»?|Qui a écrit l'« Histoire de la décadence et de la chute de l'Empire romain » ?|Qui va escriure la «Història de la decadència i caiguda de l'Imperi romà»?", "Edward Gibbon"),
        Fact.Of(Level.Master, "history.historiography", "historian", "Who introduced the idea of \"realms of memory\" (lieux de mémoire)?|¿Quién introdujo la idea de los «lugares de memoria»?|Qui a introduit la notion de « lieux de mémoire » ?|Qui va introduir la idea dels «llocs de memòria»?", "Pierre Nora"),
        Fact.Of(Level.Master, "history.historiography", "historian", "Who wrote \"The Return of Martin Guerre\" (1983)?|¿Quién escribió «El regreso de Martin Guerre» (1983)?|Qui a écrit « Le Retour de Martin Guerre » (1983) ?|Qui va escriure «El retorn de Martin Guerre» (1983)?", "Natalie Zemon Davis"),
        Fact.Of(Level.Master, "history.historiography", "historian", "Who wrote \"The Great Transformation\" (1944) on the rise of market society?|¿Quién escribió «La gran transformación» (1944) sobre el auge de la sociedad de mercado?|Qui a écrit « La Grande Transformation » (1944) sur l'essor de la société de marché ?|Qui va escriure «La gran transformació» (1944) sobre l'auge de la societat de mercat?", "Karl Polanyi"),
        Fact.Of(Level.Master, "history.historiography", "historian", "Who wrote \"Orientalism\" (1978)?|¿Quién escribió «Orientalismo» (1978)?|Qui a écrit « L'Orientalisme » (1978) ?|Qui va escriure «Orientalisme» (1978)?", "Edward Said"),
        Fact.Of(Level.Doctorate, "history.historiography", "historian", "Who developed conceptual history (Begriffsgeschichte)?|¿Quién desarrolló la historia conceptual (Begriffsgeschichte)?|Qui a développé l'histoire des concepts (Begriffsgeschichte) ?|Qui va desenvolupar la història conceptual (Begriffsgeschichte)?", "Reinhart Koselleck"),
        Fact.Of(Level.Doctorate, "history.historiography", "historian", "Who wrote \"Metahistory\" (1973) on the narrative forms of historical writing?|¿Quién escribió «Metahistoria» (1973) sobre las formas narrativas de la escritura histórica?|Qui a écrit « Metahistory » (1973) sur les formes narratives de l'écriture historique ?|Qui va escriure «Metahistòria» (1973) sobre les formes narratives de l'escriptura històrica?", "Hayden White"),
        Fact.Of(Level.Doctorate, "history.historiography", "historian", "Who wrote \"Discipline and Punish\" (1975), a history of the prison?|¿Quién escribió «Vigilar y castigar» (1975), una historia de la prisión?|Qui a écrit « Surveiller et punir » (1975), une histoire de la prison ?|Qui va escriure «Vigilar i castigar» (1975), una història de la presó?", "Michel Foucault"),
    ];

    /// <summary>Task #97: who built what — ancient and medieval civilizations, for the primary and early secondary levels.</summary>
    private static readonly Fact[] Civilizations =
    [
        Fact.Of(Level.Primary2, "history.civilizations", "civilization", "Who built the pyramids of Giza?|¿Quién construyó las pirámides de Guiza?|Qui a construit les pyramides de Gizeh ?|Qui va construir les piràmides de Gizeh?", "the ancient Egyptians|los antiguos egipcios|les Égyptiens de l'Antiquité|els antics egipcis", null, "🏜️"),
        Fact.Of(Level.Primary2, "history.civilizations", "civilization", "Who held the first Olympic Games, in Olympia?|¿Quién celebró los primeros Juegos Olímpicos, en Olimpia?|Qui a organisé les premiers Jeux olympiques, à Olympie ?|Qui va celebrar els primers Jocs Olímpics, a Olímpia?", "the ancient Greeks|los antiguos griegos|les Grecs de l'Antiquité|els antics grecs", null, "🏛️"),
        Fact.Of(Level.Primary2, "history.civilizations", "civilization", "Who built the Great Wall to protect their northern border?|¿Quién construyó la Gran Muralla para proteger su frontera norte?|Qui a construit la Grande Muraille pour protéger sa frontière nord ?|Qui va construir la Gran Muralla per protegir la seva frontera nord?", "the Chinese|los chinos|les Chinois|els xinesos", null, "🧱"),
        Fact.Of(Level.Primary3, "history.civilizations", "civilization", "Who built Machu Picchu in the Andes?|¿Quién construyó Machu Picchu en los Andes?|Qui a construit le Machu Picchu dans les Andes ?|Qui va construir Machu Picchu als Andes?", "the Incas|los incas|les Incas|els inques", null, "⛰️"),
        Fact.Of(Level.Primary3, "history.civilizations", "civilization", "Who founded Tenochtitlan, where Mexico City stands today?|¿Quién fundó Tenochtitlan, donde hoy está Ciudad de México?|Qui a fondé Tenochtitlan, là où se trouve aujourd'hui Mexico ?|Qui va fundar Tenochtitlan, on avui hi ha Ciutat de Mèxic?", "the Aztecs|los aztecas|les Aztèques|els asteques"),
        Fact.Of(Level.Primary3, "history.civilizations", "civilization", "Who built the aqueduct of Segovia?|¿Quién construyó el acueducto de Segovia?|Qui a construit l'aqueduc de Ségovie ?|Qui va construir l'aqüeducte de Segòvia?", "the Romans|los romanos|les Romains|els romans"),
        Fact.Of(Level.Primary3, "history.civilizations", "civilization", "Which Scandinavian seafarers reached North America around the year 1000?|¿Qué navegantes escandinavos llegaron a América del Norte hacia el año 1000?|Quels navigateurs scandinaves ont atteint l'Amérique du Nord vers l'an 1000 ?|Quins navegants escandinaus van arribar a Amèrica del Nord cap a l'any 1000?", "the Vikings|los vikingos|les Vikings|els víkings", null, "⛵"),
        Fact.Of(Level.Secondary1, "history.civilizations", "civilization", "Who built Chichén Itzá in the Yucatán?|¿Quién construyó Chichén Itzá, en Yucatán?|Qui a construit Chichén Itzá, dans le Yucatán ?|Qui va construir Chichén Itzá, al Yucatán?", "the Maya|los mayas|les Mayas|els maies"),
        Fact.Of(Level.Secondary1, "history.civilizations", "civilization", "Which seafaring people founded Carthage and spread the alphabet?|¿Qué pueblo navegante fundó Cartago y difundió el alfabeto?|Quel peuple de navigateurs a fondé Carthage et diffusé l'alphabet ?|Quin poble navegant va fundar Cartago i va difondre l'alfabet?", "the Phoenicians|los fenicios|les Phéniciens|els fenicis"),
        Fact.Of(Level.Secondary1, "history.civilizations", "civilization", "Who developed cuneiform, one of the first writing systems?|¿Quién desarrolló la escritura cuneiforme, una de las primeras?|Qui a développé l'écriture cunéiforme, l'une des premières ?|Qui va desenvolupar l'escriptura cuneïforme, una de les primeres?", "the Sumerians|los sumerios|les Sumériens|els sumeris"),
    ];

    /// <summary>Task #97: periods, movements and systems, for secondary school up to university.</summary>
    private static readonly Fact[] Concepts =
    [
        Fact.Of(Level.Secondary2, "history.concepts", "concept", "Rebirth of classical art and learning that began in 15th-century Italy?|¿Renacer del arte y el saber clásicos que empezó en la Italia del siglo XV?|Renouveau de l'art et du savoir antiques né dans l'Italie du XVᵉ siècle ?|Renaixement de l'art i el saber clàssics que va començar a la Itàlia del segle XV?", "the Renaissance|el Renacimiento|la Renaissance|el Renaixement"),
        Fact.Of(Level.Secondary2, "history.concepts", "concept", "Shift from handwork to machines and factories that began in 18th-century Britain?|¿Paso del trabajo manual a las máquinas y fábricas que empezó en la Gran Bretaña del siglo XVIII?|Passage du travail manuel aux machines et aux usines, commencé dans la Grande-Bretagne du XVIIIᵉ siècle ?|Pas del treball manual a les màquines i fàbriques que va començar a la Gran Bretanya del segle XVIII?", "the Industrial Revolution|la Revolución Industrial|la révolution industrielle|la Revolució Industrial"),
        Fact.Of(Level.Secondary2, "history.concepts", "concept", "Rivalry between the USA and the USSR without direct war (1947–1991)?|¿Rivalidad entre EE. UU. y la URSS sin guerra directa (1947-1991)?|Rivalité entre les États-Unis et l'URSS sans guerre directe (1947-1991) ?|Rivalitat entre els EUA i l'URSS sense guerra directa (1947-1991)?", "the Cold War|la Guerra Fría|la guerre froide|la Guerra Freda"),
        Fact.Of(Level.HighSchool, "history.concepts", "concept", "Medieval system in which vassals received land from a lord in exchange for loyalty?|¿Sistema medieval en el que los vasallos recibían tierras de un señor a cambio de fidelidad?|Système médiéval où les vassaux recevaient une terre d'un seigneur en échange de leur fidélité ?|Sistema medieval en què els vassalls rebien terres d'un senyor a canvi de fidelitat?", "feudalism|feudalismo|féodalité|feudalisme"),
        Fact.Of(Level.HighSchool, "history.concepts", "concept", "18th-century movement that trusted reason over tradition and absolute power?|¿Movimiento del siglo XVIII que confiaba en la razón frente a la tradición y el poder absoluto?|Mouvement du XVIIIᵉ siècle qui faisait confiance à la raison contre la tradition et le pouvoir absolu ?|Moviment del segle XVIII que confiava en la raó davant la tradició i el poder absolut?", "the Enlightenment|la Ilustración|les Lumières|la Il·lustració"),
        Fact.Of(Level.HighSchool, "history.concepts", "concept", "Form of government in which the king holds unlimited power?|¿Forma de gobierno en la que el rey tiene un poder ilimitado?|Forme de gouvernement où le roi détient un pouvoir illimité ?|Forma de govern en què el rei té un poder il·limitat?", "absolutism|absolutismo|absolutisme|absolutisme"),
        Fact.Of(Level.HighSchool, "history.concepts", "concept", "Process by which colonies became independent states, mostly after 1945?|¿Proceso por el que las colonias se convirtieron en estados independientes, sobre todo después de 1945?|Processus par lequel les colonies sont devenues des États indépendants, surtout après 1945 ?|Procés pel qual les colònies es van convertir en estats independents, sobretot després de 1945?", "decolonization|descolonización|décolonisation|descolonització"),
        Fact.Of(Level.Bachelor, "history.concepts", "concept", "Early modern policy of piling up gold and silver and limiting imports?|¿Política de la Edad Moderna de acumular oro y plata y limitar las importaciones?|Politique de l'époque moderne consistant à accumuler or et argent et à limiter les importations ?|Política de l'edat moderna d'acumular or i plata i limitar les importacions?", "mercantilism|mercantilismo|mercantilisme|mercantilisme"),
        Fact.Of(Level.Bachelor, "history.concepts", "concept", "Expansion of European powers that led to the \"Scramble for Africa\"?|¿Expansión de las potencias europeas que llevó al «reparto de África»?|Expansion des puissances européennes qui a mené au « partage de l'Afrique » ?|Expansió de les potències europees que va portar al «repartiment de l'Àfrica»?", "imperialism|imperialismo|impérialisme|imperialisme"),
    ];

    public static QuizQuestion Generate(Mc q) => q.Level switch
    {
        <= Level.Primary3 => q.Topic(Chronology, Year, f => Facts.Ask(f, Civilizations), f => Facts.Ask(f, People))(q),
        Level.Secondary1 => q.Topic(Year, Chronology, Century, f => Facts.Ask(f, Civilizations), f => Facts.Ask(f, People))(q),
        <= Level.HighSchool => q.Topic(Year, Chronology, Century, f => Facts.Ask(f, Concepts), f => Facts.Ask(f, People))(q),
        Level.Bachelor => q.Topic(Year, Century, f => Facts.Ask(f, Concepts), f => Facts.Ask(f, People))(q),
        _ => q.Topic(f => Facts.Ask(f, People), Year, f => Facts.Ask(f, People))(q),
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

        // Task #97: more countries at every level, so flags, continents and capitals stop repeating so soon.
        (Text.Of("Netherlands|Países Bajos|Pays-Bas|Països Baixos"), Text.Of("Amsterdam|Ámsterdam|Amsterdam|Amsterdam"), 0, "🇳🇱", Level.Primary2),
        (Text.Of("Ireland|Irlanda|Irlande|Irlanda"), Text.Of("Dublin|Dublín|Dublin|Dublín"), 0, "🇮🇪", Level.Primary2),
        (Text.Of("Colombia|Colombia|Colombie|Colòmbia"), Text.Of("Bogotá|Bogotá|Bogota|Bogotà"), 4, "🇨🇴", Level.Primary2),
        (Text.Of("Chile|Chile|Chili|Xile"), Text.Of("Santiago|Santiago de Chile|Santiago|Santiago de Xile"), 4, "🇨🇱", Level.Primary2),
        (Text.Of("Belgium|Bélgica|Belgique|Bèlgica"), Text.Of("Brussels|Bruselas|Bruxelles|Brussel·les"), 0, "🇧🇪", Level.Primary3),
        (Text.Of("Norway|Noruega|Norvège|Noruega"), Text.Of("Oslo"), 0, "🇳🇴", Level.Primary3),
        (Text.Of("Cuba"), Text.Of("Havana|La Habana|La Havane|l'Havana"), 3, "🇨🇺", Level.Primary3),
        (Text.Of("Thailand|Tailandia|Thaïlande|Tailàndia"), Text.Of("Bangkok"), 1, "🇹🇭", Level.Primary3),
        (Text.Of("Poland|Polonia|Pologne|Polònia"), Text.Of("Warsaw|Varsovia|Varsovie|Varsòvia"), 0, "🇵🇱", Level.Secondary1),
        (Text.Of("Austria|Austria|Autriche|Àustria"), Text.Of("Vienna|Viena|Vienne|Viena"), 0, "🇦🇹", Level.Secondary1),
        (Text.Of("Venezuela|Venezuela|Venezuela|Veneçuela"), Text.Of("Caracas"), 4, "🇻🇪", Level.Secondary1),
        (Text.Of("Vietnam|Vietnam|Viêt Nam|Vietnam"), Text.Of("Hanoi|Hanói|Hanoï|Hanoi"), 1, "🇻🇳", Level.Secondary1),
        (Text.Of("Ethiopia|Etiopía|Éthiopie|Etiòpia"), Text.Of("Addis Ababa|Adís Abeba|Addis-Abeba|Addis Abeba"), 2, "🇪🇹", Level.Secondary1),
        (Text.Of("Saudi Arabia|Arabia Saudí|Arabie saoudite|Aràbia Saudita"), Text.Of("Riyadh|Riad|Riyad|Riad"), 1, "🇸🇦", Level.Secondary2),
        (Text.Of("Ghana"), Text.Of("Accra"), 2, "🇬🇭", Level.Secondary2),
        (Text.Of("Uruguay"), Text.Of("Montevideo"), 4, "🇺🇾", Level.Secondary2),
        (Text.Of("Finland|Finlandia|Finlande|Finlàndia"), Text.Of("Helsinki|Helsinki|Helsinki|Hèlsinki"), 0, "🇫🇮", Level.Secondary2),
        (Text.Of("Hungary|Hungría|Hongrie|Hongria"), Text.Of("Budapest"), 0, "🇭🇺", Level.HighSchool),
        (Text.Of("Pakistan|Pakistán|Pakistan|Pakistan"), Text.Of("Islamabad"), 1, "🇵🇰", Level.HighSchool),
        (Text.Of("Senegal|Senegal|Sénégal|Senegal"), Text.Of("Dakar"), 2, "🇸🇳", Level.HighSchool),
        (Text.Of("Kazakhstan|Kazajistán|Kazakhstan|Kazakhstan"), Text.Of("Astana"), 1, "🇰🇿", Level.HighSchool),
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

        // Task #97: physical geography for Primary3 (oceans, deserts, rivers, ranges), lakes and climates for later levels.
        Fact.Of(Level.Primary3, "geography.physical", "ocean", "Largest ocean on Earth?|¿Océano más grande de la Tierra?|Plus grand océan de la Terre ?|Oceà més gran de la Terra?", "Pacific|Pacífico|Pacifique|Pacífic", null, "🌊"),
        Fact.Of(Level.Primary3, "geography.physical", "ocean", "Ocean between Europe and Africa on one side and the Americas on the other?|¿Océano entre Europa y África, a un lado, y América, al otro?|Océan entre l'Europe et l'Afrique d'un côté et les Amériques de l'autre ?|Oceà entre Europa i l'Àfrica, d'una banda, i Amèrica, de l'altra?", "Atlantic|Atlántico|Atlantique|Atlàntic", null, "🌊"),
        Fact.Of(Level.Secondary1, "geography.physical", "ocean", "Ocean to the south of India?|¿Océano al sur de la India?|Océan au sud de l'Inde ?|Oceà al sud de l'Índia?", "Indian|Índico|Indien|Índic"),
        Fact.Of(Level.Secondary1, "geography.physical", "ocean", "Ocean around the North Pole?|¿Océano que rodea el Polo Norte?|Océan autour du pôle Nord ?|Oceà que envolta el pol Nord?", "Arctic|Ártico|Arctique|Àrtic"),
        Fact.Of(Level.Primary3, "geography.physical", "desert", "Largest hot desert in the world, in North Africa?|¿Mayor desierto cálido del mundo, en el norte de África?|Plus grand désert chaud du monde, en Afrique du Nord ?|Desert càlid més gran del món, al nord de l'Àfrica?", "Sahara|Sáhara|Sahara|Sàhara", null, "🐪"),
        Fact.Of(Level.Secondary1, "geography.physical", "desert", "Desert in northern Chile, one of the driest places on Earth?|¿Desierto del norte de Chile, uno de los lugares más secos de la Tierra?|Désert du nord du Chili, l'un des endroits les plus secs de la Terre ?|Desert del nord de Xile, un dels llocs més secs de la Terra?", "Atacama"),
        Fact.Of(Level.Secondary2, "geography.physical", "desert", "Large cold desert in Mongolia and northern China?|¿Gran desierto frío de Mongolia y el norte de China?|Grand désert froid de Mongolie et du nord de la Chine ?|Gran desert fred de Mongòlia i el nord de la Xina?", "Gobi"),
        Fact.Of(Level.HighSchool, "geography.physical", "desert", "Semi-arid desert covering much of Botswana?|¿Desierto semiárido que cubre gran parte de Botsuana?|Désert semi-aride couvrant une grande partie du Botswana ?|Desert semiàrid que cobreix gran part de Botswana?", "Kalahari|Kalahari|Kalahari|Kalahari"),
        Fact.Of(Level.Primary3, "geography.physical", "river", "Longest river in Africa?|¿Río más largo de África?|Plus long fleuve d'Afrique ?|Riu més llarg de l'Àfrica?", "Nile|Nilo|Nil|Nil"),
        Fact.Of(Level.Primary3, "geography.physical", "river", "River that crosses the largest rainforest in South America?|¿Río que atraviesa la mayor selva de América del Sur?|Fleuve qui traverse la plus grande forêt tropicale d'Amérique du Sud ?|Riu que travessa la selva més gran d'Amèrica del Sud?", "Amazon|Amazonas|Amazone|Amazones"),
        Fact.Of(Level.Secondary2, "geography.physical", "river", "Which river flows through Seville?|¿Qué río pasa por Sevilla?|Quel fleuve traverse Séville ?|Quin riu passa per Sevilla?", "Guadalquivir"),
        Fact.Of(Level.Secondary2, "geography.physical", "river", "Which river flows through Rome?|¿Qué río pasa por Roma?|Quel fleuve traverse Rome ?|Quin riu passa per Roma?", "Tiber|Tíber|Tibre|Tíber"),
        Fact.Of(Level.Secondary2, "geography.physical", "river", "Which river flows through Vienna and Budapest?|¿Qué río pasa por Viena y Budapest?|Quel fleuve traverse Vienne et Budapest ?|Quin riu passa per Viena i Budapest?", "Danube|Danubio|Danube|Danubi"),
        Fact.Of(Level.Primary3, "geography.physical", "range", "Mountain range between Spain and France?|¿Cordillera entre España y Francia?|Chaîne de montagnes entre l'Espagne et la France ?|Serralada entre Espanya i França?", "Pyrenees|Pirineos|Pyrénées|Pirineus", null, "⛰️"),
        Fact.Of(Level.Primary3, "geography.physical", "range", "Mountain range that crosses Switzerland and Austria?|¿Cordillera que atraviesa Suiza y Austria?|Chaîne de montagnes qui traverse la Suisse et l'Autriche ?|Serralada que travessa Suïssa i Àustria?", "Alps|Alpes|Alpes|Alps", null, "🏔️"),
        Fact.Of(Level.Secondary1, "geography.physical", "range", "Long mountain range along the west of South America?|¿Larga cordillera a lo largo del oeste de América del Sur?|Longue chaîne de montagnes le long de l'ouest de l'Amérique du Sud ?|Llarga serralada al llarg de l'oest d'Amèrica del Sud?", "Andes|Andes|Andes|Andes"),
        Fact.Of(Level.Secondary1, "geography.physical", "range", "Mountain range where Everest stands?|¿Cordillera donde se encuentra el Everest?|Chaîne de montagnes où se trouve l'Everest ?|Serralada on hi ha l'Everest?", "Himalayas|Himalaya|Himalaya|Himàlaia"),
        Fact.Of(Level.Secondary2, "geography.physical", "range", "Mountain range traditionally seen as the border between Europe and Asia?|¿Cordillera considerada tradicionalmente la frontera entre Europa y Asia?|Chaîne de montagnes traditionnellement vue comme la limite entre l'Europe et l'Asie ?|Serralada considerada tradicionalment la frontera entre Europa i Àsia?", "Urals|Urales|Oural|Urals"),
        Fact.Of(Level.HighSchool, "geography.physical", "lake", "Largest lake on Earth by surface, often called a sea?|¿Mayor lago de la Tierra por superficie, a menudo llamado mar?|Plus grand lac de la Terre par sa superficie, souvent appelé mer ?|Llac més gran de la Terra per superfície, sovint anomenat mar?", "Caspian Sea|mar Caspio|mer Caspienne|mar Càspia"),
        Fact.Of(Level.HighSchool, "geography.physical", "lake", "Deepest lake in the world, in Siberia?|¿Lago más profundo del mundo, en Siberia?|Lac le plus profond du monde, en Sibérie ?|Llac més profund del món, a Sibèria?", "Lake Baikal|lago Baikal|lac Baïkal|llac Baikal"),
        Fact.Of(Level.HighSchool, "geography.physical", "lake", "Largest lake in Africa, a source of the Nile?|¿Mayor lago de África, una de las fuentes del Nilo?|Plus grand lac d'Afrique, l'une des sources du Nil ?|Llac més gran de l'Àfrica, una de les fonts del Nil?", "Lake Victoria|lago Victoria|lac Victoria|llac Victòria"),
        Fact.Of(Level.HighSchool, "geography.physical", "lake", "Large high-altitude lake between Peru and Bolivia?|¿Gran lago de altura entre Perú y Bolivia?|Grand lac d'altitude entre le Pérou et la Bolivie ?|Gran llac d'altura entre el Perú i Bolívia?", "Lake Titicaca|lago Titicaca|lac Titicaca|llac Titicaca"),
        Fact.Of(Level.HighSchool, "geography.climate", "climate", "Climate with very little rain and big temperature differences between day and night?|¿Clima con muy pocas lluvias y grandes diferencias de temperatura entre el día y la noche?|Climat avec très peu de pluie et de grands écarts de température entre le jour et la nuit ?|Clima amb molt poques pluges i grans diferències de temperatura entre el dia i la nit?", "desert|desértico|désertique|desèrtic"),
        Fact.Of(Level.HighSchool, "geography.climate", "climate", "Mild climate with rain all year, shaped by the nearby ocean, as in Galicia or Brittany?|¿Clima suave con lluvias todo el año, marcado por la cercanía del océano, como en Galicia o Bretaña?|Climat doux et pluvieux toute l'année, marqué par la proximité de l'océan, comme en Galice ou en Bretagne ?|Clima suau amb pluges tot l'any, marcat per la proximitat de l'oceà, com a Galícia o la Bretanya?", "oceanic|oceánico|océanique|oceànic"),
        Fact.Of(Level.HighSchool, "geography.climate", "climate", "Climate far from the sea, with very cold winters and warm summers, as in central Russia?|¿Clima lejos del mar, con inviernos muy fríos y veranos cálidos, como en el centro de Rusia?|Climat loin de la mer, aux hivers très froids et aux étés chauds, comme au centre de la Russie ?|Clima lluny del mar, amb hiverns molt freds i estius càlids, com al centre de Rússia?", "continental"),
        Fact.Of(Level.HighSchool, "geography.climate", "climate", "Climate in which temperatures fall as altitude rises?|¿Clima en el que la temperatura baja al aumentar la altitud?|Climat où la température baisse quand l'altitude augmente ?|Clima en què la temperatura baixa en augmentar l'altitud?", "mountain|de montaña|montagnard|de muntanya"),

        // Task #97: population vocabulary (also asked as a computed density question, see Density).
        Fact.Of(Level.Secondary2, "geography.population", "demography", "Number of births per 1,000 inhabitants in a year?|¿Número de nacimientos por cada 1000 habitantes en un año?|Nombre de naissances pour 1 000 habitants en un an ?|Nombre de naixements per cada 1.000 habitants en un any?", "birth rate|tasa de natalidad|taux de natalité|taxa de natalitat"),
        Fact.Of(Level.Secondary2, "geography.population", "demography", "Number of deaths per 1,000 inhabitants in a year?|¿Número de defunciones por cada 1000 habitantes en un año?|Nombre de décès pour 1 000 habitants en un an ?|Nombre de defuncions per cada 1.000 habitants en un any?", "death rate|tasa de mortalidad|taux de mortalité|taxa de mortalitat"),
        Fact.Of(Level.Secondary2, "geography.population", "demography", "Graph of a population by age and sex?|¿Gráfico de una población por edad y sexo?|Graphique d'une population par âge et par sexe ?|Gràfic d'una població per edat i sexe?", "population pyramid|pirámide de población|pyramide des âges|piràmide de població"),
        Fact.Of(Level.Secondary2, "geography.population", "demography", "Average number of years a newborn is expected to live?|¿Número medio de años que se espera que viva un recién nacido?|Nombre moyen d'années qu'un nouveau-né peut espérer vivre ?|Nombre mitjà d'anys que s'espera que visqui un nadó?", "life expectancy|esperanza de vida|espérance de vie|esperança de vida"),
        Fact.Of(Level.HighSchool, "geography.population", "demography", "Difference between the people who arrive in a country and those who leave it?|¿Diferencia entre las personas que llegan a un país y las que se van?|Différence entre les personnes qui arrivent dans un pays et celles qui le quittent ?|Diferència entre les persones que arriben a un país i les que se'n van?", "net migration|saldo migratorio|solde migratoire|saldo migratori"),
        Fact.Of(Level.HighSchool, "geography.population", "demography", "Average number of children per woman?|¿Número medio de hijos por mujer?|Nombre moyen d'enfants par femme ?|Nombre mitjà de fills per dona?", "fertility rate|tasa de fecundidad|indice de fécondité|taxa de fecunditat"),
        Fact.Of(Level.HighSchool, "geography.population", "demography", "Share of the population that lives in towns and cities?|¿Porcentaje de la población que vive en ciudades?|Part de la population qui vit en ville ?|Percentatge de la població que viu en ciutats?", "urbanization rate|tasa de urbanización|taux d'urbanisation|taxa d'urbanització"),

        // Task #97: more geographers, so Bachelor–Doctorate (facts only) stop cycling through two questions per level.
        Fact.Of(Level.Bachelor, "geography.theory", "geographer", "Who first described the demographic transition model (1929)?|¿Quién describió por primera vez el modelo de transición demográfica (1929)?|Qui a décrit le premier le modèle de transition démographique (1929) ?|Qui va descriure per primer cop el model de transició demogràfica (1929)?", "Warren Thompson"),
        Fact.Of(Level.Bachelor, "geography.theory", "geographer", "Who coined \"megalopolis\" for the urban corridor from Boston to Washington?|¿Quién acuñó «megalópolis» para el corredor urbano de Boston a Washington?|Qui a forgé le terme « mégalopole » pour le corridor urbain de Boston à Washington ?|Qui va encunyar «megalòpolis» per al corredor urbà de Boston a Washington?", "Jean Gottmann"),
        Fact.Of(Level.Bachelor, "geography.theory", "geographer", "Who coined the term \"gentrification\" (1964)?|¿Quién acuñó el término «gentrificación» (1964)?|Qui a forgé le terme « gentrification » (1964) ?|Qui va encunyar el terme «gentrificació» (1964)?", "Ruth Glass"),
        Fact.Of(Level.Bachelor, "geography.theory", "geographer", "Who drew the concentric zone model of the city, based on Chicago?|¿Quién trazó el modelo de zonas concéntricas de la ciudad, a partir de Chicago?|Qui a conçu le modèle des zones concentriques de la ville, d'après Chicago ?|Qui va traçar el model de zones concèntriques de la ciutat, a partir de Chicago?", "Ernest Burgess"),
        Fact.Of(Level.Bachelor, "geography.theory", "geographer", "Who proposed the sector model of urban land use (1939)?|¿Quién propuso el modelo sectorial de usos del suelo urbano (1939)?|Qui a proposé le modèle sectoriel d'occupation du sol urbain (1939) ?|Qui va proposar el model sectorial d'usos del sòl urbà (1939)?", "Homer Hoyt"),
        Fact.Of(Level.Bachelor, "geography.theory", "geographer", "Whose \"laws of migration\" say that most migrants move only a short distance?|¿Qué «leyes de la migración» dicen que la mayoría de los migrantes se desplazan poca distancia?|Quelles « lois de la migration » disent que la plupart des migrants ne se déplacent que sur une courte distance ?|Quines «lleis de la migració» diuen que la majoria dels migrants es desplacen poca distància?", "Ernest Ravenstein"),
        Fact.Of(Level.Master, "geography.theory", "geographer", "Who wrote \"Social Justice and the City\" (1973)?|¿Quién escribió «Urbanismo y desigualdad social» (Social Justice and the City, 1973)?|Qui a écrit « Social Justice and the City » (1973) ?|Qui va escriure «Social Justice and the City» (1973)?", "David Harvey"),
        Fact.Of(Level.Master, "geography.theory", "geographer", "Who explained industrial location with the location triangle (1909)?|¿Quién explicó la localización industrial con el triángulo de localización (1909)?|Qui a expliqué la localisation industrielle par le triangle de localisation (1909) ?|Qui va explicar la localització industrial amb el triangle de localització (1909)?", "Alfred Weber"),
        Fact.Of(Level.Master, "geography.theory", "geographer", "Who wrote \"Topophilia\" (1974), a founding work of humanistic geography?|¿Quién escribió «Topofilia» (1974), obra fundacional de la geografía humanista?|Qui a écrit « Topophilia » (1974), œuvre fondatrice de la géographie humaniste ?|Qui va escriure «Topofília» (1974), obra fundacional de la geografia humanista?", "Yi-Fu Tuan"),
        Fact.Of(Level.Master, "geography.theory", "geographer", "Who founded the French school of regional geography (possibilism)?|¿Quién fundó la escuela francesa de geografía regional (posibilismo)?|Qui a fondé l'école française de géographie régionale (possibilisme) ?|Qui va fundar l'escola francesa de geografia regional (possibilisme)?", "Paul Vidal de la Blache"),
        Fact.Of(Level.Doctorate, "geography.theory", "geographer", "Who wrote \"Postmodern Geographies\" (1989)?|¿Quién escribió «Geografías posmodernas» (1989)?|Qui a écrit « Postmodern Geographies » (1989) ?|Qui va escriure «Geografies postmodernes» (1989)?", "Edward Soja"),
        Fact.Of(Level.Doctorate, "geography.theory", "geographer", "Who wrote \"The Global City\" (1991) on New York, London and Tokyo?|¿Quién escribió «La ciudad global» (1991) sobre Nueva York, Londres y Tokio?|Qui a écrit « La Ville globale » (1991) sur New York, Londres et Tokyo ?|Qui va escriure «La ciutat global» (1991) sobre Nova York, Londres i Tòquio?", "Saskia Sassen"),
        Fact.Of(Level.Doctorate, "geography.theory", "geographer", "Who wrote \"For Space\" (2005), on space as a product of relations?|¿Quién escribió «For Space» (2005), sobre el espacio como producto de relaciones?|Qui a écrit « For Space » (2005), sur l'espace comme produit de relations ?|Qui va escriure «For Space» (2005), sobre l'espai com a producte de relacions?", "Doreen Massey"),
    ];

    public static QuizQuestion Generate(Mc q) => q.Level switch
    {
        Level.Primary1 => q.Topic(Flag, Continent)(q),
        Level.Primary2 => q.Topic(Flag, Continent, Capital)(q),
        Level.Primary3 => q.Topic(Continent, Capital, Flag, f => Facts.Ask(f, FactList))(q),
        Level.Secondary1 => q.Topic(Capital, Scale, f => Facts.Ask(f, FactList))(q),
        Level.Secondary2 => q.Topic(Capital, Scale, Density, f => Facts.Ask(f, FactList))(q),
        Level.HighSchool => q.Topic(Capital, Scale, TimeZone, Density, f => Facts.Ask(f, FactList))(q),
        Level.Bachelor => q.Topic(f => Facts.Ask(f, FactList), f => Facts.Ask(f, FactList), Density, TimeZone)(q),
        _ => q.Topic(f => Facts.Ask(f, FactList), f => Facts.Ask(f, FactList), Density)(q),
    };

    /// <summary>Task #97: map scale, computed in code (cm on the map → km on the ground).</summary>
    private static QuizQuestion Scale(Mc q)
    {
        var scale = q.Pick([10000, 25000, 50000, 100000, 200000]);
        var cm = q.Between(2, 12);
        var km = cm * scale / 100000.0;
        return q.Number("geography.maps", q.F("On a 1:{0} map, two towns are {1} cm apart. Real distance?|En un mapa a escala 1:{0}, dos pueblos están a {1} cm. ¿Distancia real?|Sur une carte au 1/{0}, deux villages sont à {1} cm. Distance réelle ?|En un mapa a escala 1:{0}, dos pobles són a {1} cm. Distància real?", scale, cm),
            km, [cm * scale / 1000.0, cm * scale / 10000.0, cm * scale / 1000000.0], q.F("{0} cm × {1} = {2} cm = {3} km.|{0} cm × {1} = {2} cm = {3} km.|{0} cm × {1} = {2} cm = {3} km.|{0} cm × {1} = {2} cm = {3} km.", cm, scale, cm * scale, q.N(km)), unit: " km");
    }

    /// <summary>Task #97: population density, computed in code.</summary>
    private static QuizQuestion Density(Mc q)
    {
        var area = q.Between(1, 9) * 10000;
        var density = q.Pick([10, 20, 25, 40, 50, 80, 100, 120, 150, 200, 250, 300]);
        var people = area * density;
        return q.Number("geography.population", q.F("A region of {0} km² has {1} inhabitants. Population density?|Una región de {0} km² tiene {1} habitantes. ¿Densidad de población?|Une région de {0} km² compte {1} habitants. Densité de population ?|Una regió de {0} km² té {1} habitants. Densitat de població?", area, people),
            density, [density * 10, density / 10.0, area / (double)people * 1000], q.F("{0} ÷ {1} km² = {2} inhab./km².|{0} ÷ {1} km² = {2} hab./km².|{0} ÷ {1} km² = {2} hab./km².|{0} ÷ {1} km² = {2} hab./km².", people, area, density), unit: " /km²", decimals: 1);
    }

    /// <summary>Task #97: time zones from longitude (15° = 1 hour), computed in code.</summary>
    private static QuizQuestion TimeZone(Mc q)
    {
        var start = q.Between(6, 14);
        var hours = q.Between(1, 11);
        var east = q.Rng.Next(2) == 0;
        static string Clock(int h) => $"{(h % 24 + 24) % 24:00}:00";
        var answer = Clock(start + (east ? hours : -hours));
        return q.Choice("geography.maps", q.F("Each 15° of longitude is one hour of difference. When it is {0} at longitude 0°, what time is it at {1}° {2}?|Cada 15° de longitud son una hora de diferencia. Cuando son las {0} en la longitud 0°, ¿qué hora es a {1}° {2}?|Chaque 15° de longitude font une heure d'écart. Quand il est {0} à la longitude 0°, quelle heure est-il à {1}° {2} ?|Cada 15° de longitud són una hora de diferència. Quan són les {0} a la longitud 0°, quina hora és a {1}° {2}?",
                Clock(start), hours * 15, q.T(east ? "east|este|est|est" : "west|oeste|ouest|oest")),
            answer, [Clock(start + (east ? -hours : hours)), Clock(start + (east ? hours + 1 : -hours - 1)), Clock(start + (east ? hours - 1 : 1 - hours)), Clock(start), Clock(start + (east ? hours + 2 : -hours - 2))],
            q.F("{0}° ÷ 15 = {1} h; {2} → {3}.|{0}° ÷ 15 = {1} h; {2} → {3}.|{0}° ÷ 15 = {1} h ; {2} → {3}.|{0}° ÷ 15 = {1} h; {2} → {3}.", hours * 15, hours,
                q.T(east ? "to the east, clocks are ahead|hacia el este, los relojes van adelantados|vers l'est, les horloges sont en avance|cap a l'est, els rellotges van avançats" : "to the west, clocks are behind|hacia el oeste, los relojes van atrasados|vers l'ouest, les horloges sont en retard|cap a l'oest, els rellotges van endarrerits"), answer));
    }

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

        // Task #97: more basic concepts for Secondary2 (the first level of the subject) and HighSchool.
        Fact.Of(Level.Secondary2, "economics.concepts", "concept", "General and sustained fall in prices?|¿Bajada general y sostenida de los precios?|Baisse générale et durable des prix ?|Baixada general i sostinguda dels preus?", "deflation|deflación|déflation|deflació"),
        Fact.Of(Level.Secondary2, "economics.concepts", "concept", "Price of borrowing money, as a percentage per year?|¿Precio de pedir dinero prestado, en porcentaje anual?|Prix de l'argent emprunté, en pourcentage par an ?|Preu de demanar diners prestats, en percentatge anual?", "interest rate|tipo de interés|taux d'intérêt|tipus d'interès"),
        Fact.Of(Level.Secondary2, "economics.concepts", "concept", "Situation in which resources are not enough to satisfy all wants?|¿Situación en la que los recursos no bastan para satisfacer todas las necesidades?|Situation où les ressources ne suffisent pas à satisfaire tous les besoins ?|Situació en què els recursos no són suficients per satisfer totes les necessitats?", "scarcity|escasez|rareté|escassetat"),
        Fact.Of(Level.Secondary2, "economics.concepts", "concept", "Plan of expected income and spending?|¿Plan de ingresos y gastos previstos?|Plan des recettes et des dépenses prévues ?|Pla d'ingressos i despeses previstos?", "budget|presupuesto|budget|pressupost"),
        Fact.Of(Level.Secondary2, "economics.concepts", "concept", "Tax added to the price of most goods and services?|¿Impuesto que se añade al precio de la mayoría de bienes y servicios?|Impôt ajouté au prix de la plupart des biens et services ?|Impost que s'afegeix al preu de la majoria de béns i serveis?", "VAT|IVA|TVA|IVA"),
        Fact.Of(Level.Secondary2, "economics.concepts", "concept", "Market with only a few large sellers?|¿Mercado con solo unos pocos grandes vendedores?|Marché avec seulement quelques grands vendeurs ?|Mercat amb només uns quants grans venedors?", "oligopoly|oligopolio|oligopole|oligopoli"),
        Fact.Of(Level.Secondary2, "economics.concepts", "concept", "Situation of people who want to work and look for a job but cannot find one?|¿Situación de las personas que quieren trabajar y buscan empleo pero no lo encuentran?|Situation des personnes qui veulent travailler et cherchent un emploi sans en trouver ?|Situació de les persones que volen treballar i busquen feina però no en troben?", "unemployment|desempleo|chômage|atur"),
        Fact.Of(Level.Secondary2, "economics.concepts", "concept", "Goods and services a country sells to other countries?|¿Bienes y servicios que un país vende a otros países?|Biens et services qu'un pays vend à d'autres pays ?|Béns i serveis que un país ven a altres països?", "exports|exportaciones|exportations|exportacions"),
        Fact.Of(Level.Secondary2, "economics.concepts", "concept", "Small part of a company that a person can buy?|¿Pequeña parte de una empresa que una persona puede comprar?|Petite part d'une entreprise qu'une personne peut acheter ?|Petita part d'una empresa que una persona pot comprar?", "share|acción|action|acció"),
        Fact.Of(Level.HighSchool, "economics.concepts", "concept", "Two consecutive quarters of falling GDP?|¿Dos trimestres seguidos de caída del PIB?|Deux trimestres consécutifs de baisse du PIB ?|Dos trimestres seguits de caiguda del PIB?", "recession|recesión|récession|recessió"),
        Fact.Of(Level.HighSchool, "economics.concepts", "concept", "Government's use of public spending and taxes to steer the economy?|¿Uso del gasto público y los impuestos por el Gobierno para orientar la economía?|Utilisation des dépenses publiques et des impôts par l'État pour orienter l'économie ?|Ús de la despesa pública i els impostos per part del Govern per orientar l'economia?", "fiscal policy|política fiscal|politique budgétaire|política fiscal"),
        Fact.Of(Level.HighSchool, "economics.concepts", "concept", "Central bank's use of interest rates and the money supply?|¿Uso de los tipos de interés y la cantidad de dinero por el banco central?|Utilisation des taux d'intérêt et de la masse monétaire par la banque centrale ?|Ús dels tipus d'interès i la quantitat de diner per part del banc central?", "monetary policy|política monetaria|politique monétaire|política monetària"),
        Fact.Of(Level.HighSchool, "economics.concepts", "concept", "Value of a country's exports minus the value of its imports?|¿Valor de las exportaciones de un país menos el de sus importaciones?|Valeur des exportations d'un pays moins celle de ses importations ?|Valor de les exportacions d'un país menys el de les importacions?", "trade balance|balanza comercial|balance commerciale|balança comercial"),
        Fact.Of(Level.HighSchool, "economics.concepts", "concept", "Cost or benefit that falls on people outside a transaction, like pollution?|¿Coste o beneficio que recae en personas ajenas a una transacción, como la contaminación?|Coût ou bénéfice qui retombe sur des personnes extérieures à une transaction, comme la pollution ?|Cost o benefici que recau en persones alienes a una transacció, com la contaminació?", "externality|externalidad|externalité|externalitat"),
        Fact.Of(Level.HighSchool, "economics.concepts", "concept", "Good that nobody can be excluded from and whose use does not reduce it for others, like street lighting?|¿Bien del que no se puede excluir a nadie y cuyo uso no lo reduce para los demás, como el alumbrado público?|Bien dont personne ne peut être exclu et dont l'usage ne le réduit pas pour les autres, comme l'éclairage public ?|Bé del qual no es pot excloure ningú i l'ús del qual no el redueix per als altres, com l'enllumenat públic?", "public good|bien público|bien public|bé públic"),
        Fact.Of(Level.HighSchool, "economics.concepts", "concept", "Situation in which a government spends more than it collects in a year?|¿Situación en la que un Gobierno gasta más de lo que ingresa en un año?|Situation où un État dépense plus qu'il ne perçoit en un an ?|Situació en què un Govern gasta més del que ingressa en un any?", "public deficit|déficit público|déficit public|dèficit públic"),
        Fact.Of(Level.Bachelor, "economics.concepts", "concept", "Extra cost of producing one more unit?|¿Coste adicional de producir una unidad más?|Coût supplémentaire de la production d'une unité de plus ?|Cost addicional de produir una unitat més?", "marginal cost|coste marginal|coût marginal|cost marginal"),
        Fact.Of(Level.Bachelor, "economics.concepts", "concept", "Allocation in which nobody can be made better off without making someone worse off?|¿Asignación en la que nadie puede mejorar sin que otro empeore?|Allocation où personne ne peut être mieux loti sans qu'un autre le soit moins ?|Assignació en què ningú no pot millorar sense que un altre empitjori?", "Pareto efficiency|eficiencia de Pareto|optimum de Pareto|eficiència de Pareto"),
        Fact.Of(Level.Bachelor, "economics.concepts", "concept", "Market failure in which one side of a deal knows more than the other?|¿Fallo de mercado en el que una parte de un trato sabe más que la otra?|Défaillance du marché où une partie à l'échange en sait plus que l'autre ?|Fallada de mercat en què una part d'un tracte en sap més que l'altra?", "asymmetric information|información asimétrica|asymétrie d'information|informació asimètrica"),
        Fact.Of(Level.Bachelor, "economics.concepts", "concept", "Curve of the short-run trade-off between inflation and unemployment?|¿Curva de la relación a corto plazo entre inflación y desempleo?|Courbe de l'arbitrage à court terme entre inflation et chômage ?|Corba de la relació a curt termini entre inflació i atur?", "Phillips curve|curva de Phillips|courbe de Phillips|corba de Phillips"),
        Fact.Of(Level.Bachelor, "economics.concepts", "concept", "Curve of the maximum combinations of two goods an economy can produce?|¿Curva de las combinaciones máximas de dos bienes que puede producir una economía?|Courbe des combinaisons maximales de deux biens qu'une économie peut produire ?|Corba de les combinacions màximes de dos béns que pot produir una economia?", "production possibility frontier|frontera de posibilidades de producción|frontière des possibilités de production|frontera de possibilitats de producció"),

        // Task #97: more economists, so Master/Doctorate stop cycling through two questions per level.
        Fact.Of(Level.HighSchool, "economics.thinkers", "economist", "Who wrote \"Das Kapital\"?|¿Quién escribió «El capital»?|Qui a écrit « Le Capital » ?|Qui va escriure «El capital»?", "Karl Marx"),
        Fact.Of(Level.HighSchool, "economics.thinkers", "economist", "Who warned that population tends to grow faster than food supply?|¿Quién advirtió que la población tiende a crecer más deprisa que los alimentos?|Qui a averti que la population tend à croître plus vite que la nourriture ?|Qui va advertir que la població tendeix a créixer més de pressa que els aliments?", "Thomas Malthus"),
        Fact.Of(Level.Bachelor, "economics.thinkers", "economist", "Who described capitalism's \"creative destruction\"?|¿Quién describió la «destrucción creativa» del capitalismo?|Qui a décrit la « destruction créatrice » du capitalisme ?|Qui va descriure la «destrucció creativa» del capitalisme?", "Joseph Schumpeter"),
        Fact.Of(Level.Bachelor, "economics.thinkers", "economist", "Which Chicago economist led monetarism?|¿Qué economista de Chicago lideró el monetarismo?|Quel économiste de Chicago a mené le monétarisme ?|Quin economista de Chicago va liderar el monetarisme?", "Milton Friedman"),
        Fact.Of(Level.Bachelor, "economics.thinkers", "economist", "Who wrote \"The Road to Serfdom\" (1944)?|¿Quién escribió «Camino de servidumbre» (1944)?|Qui a écrit « La Route de la servitude » (1944) ?|Qui va escriure «Camí de servitud» (1944)?", "Friedrich Hayek"),
        Fact.Of(Level.Bachelor, "economics.thinkers", "economist", "Who explained why firms exist with transaction costs (1937)?|¿Quién explicó la existencia de las empresas con los costes de transacción (1937)?|Qui a expliqué l'existence des entreprises par les coûts de transaction (1937) ?|Qui va explicar l'existència de les empreses amb els costos de transacció (1937)?", "Ronald Coase"),
        Fact.Of(Level.Bachelor, "economics.thinkers", "economist", "Who coined \"conspicuous consumption\"?|¿Quién acuñó el «consumo ostentoso»?|Qui a forgé l'expression « consommation ostentatoire » ?|Qui va encunyar el «consum ostentós»?", "Thorstein Veblen"),
        Fact.Of(Level.Master, "economics.thinkers", "economist", "Who showed how communities can manage common resources without state or market?|¿Quién mostró cómo las comunidades pueden gestionar recursos comunes sin Estado ni mercado?|Qui a montré comment des communautés peuvent gérer des ressources communes sans État ni marché ?|Qui va mostrar com les comunitats poden gestionar recursos comuns sense Estat ni mercat?", "Elinor Ostrom"),
        Fact.Of(Level.Master, "economics.thinkers", "economist", "Who wrote \"The Market for Lemons\" on asymmetric information?|¿Quién escribió «The Market for Lemons» sobre la información asimétrica?|Qui a écrit « The Market for Lemons » sur l'asymétrie d'information ?|Qui va escriure «The Market for Lemons» sobre la informació asimètrica?", "George Akerlof"),
        Fact.Of(Level.Master, "economics.thinkers", "economist", "Who developed prospect theory with Amos Tversky?|¿Quién desarrolló la teoría de las perspectivas con Amos Tversky?|Qui a développé la théorie des perspectives avec Amos Tversky ?|Qui va desenvolupar la teoria de les perspectives amb Amos Tversky?", "Daniel Kahneman"),
        Fact.Of(Level.Master, "economics.thinkers", "economist", "Who developed the capability approach to development?|¿Quién desarrolló el enfoque de las capacidades sobre el desarrollo?|Qui a développé l'approche par les capabilités du développement ?|Qui va desenvolupar l'enfocament de les capacitats sobre el desenvolupament?", "Amartya Sen"),
        Fact.Of(Level.Master, "economics.thinkers", "economist", "Who wrote \"Capital in the Twenty-First Century\" (2013)?|¿Quién escribió «El capital en el siglo XXI» (2013)?|Qui a écrit « Le Capital au XXIᵉ siècle » (2013) ?|Qui va escriure «El capital al segle XXI» (2013)?", "Thomas Piketty"),
        Fact.Of(Level.Doctorate, "economics.thinkers", "economist", "Who extended economic analysis to the family, crime and human capital?|¿Quién extendió el análisis económico a la familia, el delito y el capital humano?|Qui a étendu l'analyse économique à la famille, au crime et au capital humain ?|Qui va estendre l'anàlisi econòmica a la família, el delicte i el capital humà?", "Gary Becker"),
        Fact.Of(Level.Doctorate, "economics.thinkers", "economist", "Who laid the foundations of mechanism design theory?|¿Quién sentó las bases de la teoría del diseño de mecanismos?|Qui a posé les fondements de la théorie de la conception des mécanismes ?|Qui va establir les bases de la teoria del disseny de mecanismes?", "Leonid Hurwicz"),
        Fact.Of(Level.Doctorate, "economics.thinkers", "economist", "Who first formulated rational expectations (1961)?|¿Quién formuló por primera vez las expectativas racionales (1961)?|Qui a formulé le premier les anticipations rationnelles (1961) ?|Qui va formular per primer cop les expectatives racionals (1961)?", "John Muth"),
        Fact.Of(Level.Doctorate, "economics.thinkers", "economist", "Who, with Edward Prescott, founded real business cycle theory?|¿Quién fundó con Edward Prescott la teoría del ciclo económico real?|Qui a fondé avec Edward Prescott la théorie des cycles réels ?|Qui va fundar amb Edward Prescott la teoria del cicle econòmic real?", "Finn Kydland"),
    ];

    public static QuizQuestion Generate(Mc q) => q.Level switch
    {
        <= Level.Secondary2 => q.Topic(PercentChange, Discount, UnitPrice, Savings, f => Facts.Ask(f, FactList), GdpPerCapita, f => Facts.Ask(f, FactList))(q),
        Level.HighSchool => q.Topic(Interest, Equilibrium, Unemployment, RealInterest, GdpPerCapita, f => Facts.Ask(f, FactList), f => Facts.Ask(f, FactList))(q),
        Level.Bachelor => q.Topic(Elasticity, PresentValue, f => Facts.Ask(f, FactList), Unemployment, Equilibrium)(q),
        _ => q.Topic(f => Facts.Ask(f, FactList), PresentValue, f => Facts.Ask(f, FactList), RealInterest, Elasticity)(q),
    };

    /// <summary>Task #97: price per kilo, computed in code.</summary>
    private static QuizQuestion UnitPrice(Mc q)
    {
        var grams = q.Pick([200, 250, 400, 500, 750]);
        var perKilo = q.Pick([2, 4, 5, 6, 8, 10, 12, 20]);
        var price = perKilo * grams / 1000.0;
        return q.Number("economics.budget", q.F("A {0} g pack costs {1} €. Price per kilo?|Un paquete de {0} g cuesta {1} €. ¿Precio por kilo?|Un paquet de {0} g coûte {1} €. Prix au kilo ?|Un paquet de {0} g costa {1} €. Preu per quilo?", grams, q.N(price)),
            perKilo, [price * grams / 1000.0, price / grams * 100, price * 10], q.F("{0} € ÷ {1} g × 1000 g = {2} €/kg.|{0} € ÷ {1} g × 1000 g = {2} €/kg.|{0} € ÷ {1} g × 1000 g = {2} €/kg.|{0} € ÷ {1} g × 1000 g = {2} €/kg.", q.N(price), grams, perKilo), unit: " €");
    }

    /// <summary>Task #97: saving up for a purchase, computed in code.</summary>
    private static QuizQuestion Savings(Mc q)
    {
        var perWeek = q.Pick([5, 10, 15, 20, 25]);
        var weeks = q.Between(4, 20);
        var goal = perWeek * weeks;
        return q.Number("economics.budget", q.F("You save {0} € a week to buy something that costs {1} €. How many weeks do you need?|Ahorras {0} € a la semana para comprar algo que cuesta {1} €. ¿Cuántas semanas necesitas?|Tu économises {0} € par semaine pour acheter un objet à {1} €. Combien de semaines te faut-il ?|Estalvies {0} € a la setmana per comprar una cosa que costa {1} €. Quantes setmanes necessites?", perWeek, goal),
            weeks, [weeks + 1, weeks - 1, goal - perWeek], q.F("{0} € ÷ {1} € = {2}.|{0} € ÷ {1} € = {2}.|{0} € ÷ {1} € = {2}.|{0} € ÷ {1} € = {2}.", goal, perWeek, weeks), decimals: 0);
    }

    /// <summary>Task #97: GDP per capita, computed in code (billions and millions, to keep the numbers readable).</summary>
    private static QuizQuestion GdpPerCapita(Mc q)
    {
        var gdp = q.Between(1, 9) * 100;
        var people = q.Pick([2, 4, 5, 8, 10, 20, 25, 40, 50]);
        var perHead = gdp * 1000.0 / people;
        return q.Number("economics.indicators", q.F("A country has a GDP of {0} billion € and {1} million inhabitants. GDP per capita?|Un país tiene un PIB de {0}000 millones de € y {1} millones de habitantes. ¿PIB per cápita?|Un pays a un PIB de {0} milliards d'€ et {1} millions d'habitants. PIB par habitant ?|Un país té un PIB de {0}.000 milions d'€ i {1} milions d'habitants. PIB per càpita?", gdp, people),
            perHead, [gdp / (double)people, gdp * 100.0 / people, gdp * 10000.0 / people], q.F("{0} 000 000 000 € ÷ {1} 000 000 = {2} €.|{0} 000 000 000 € ÷ {1} 000 000 = {2} €.|{0} 000 000 000 € ÷ {1} 000 000 = {2} €.|{0} 000 000 000 € ÷ {1} 000 000 = {2} €.", gdp, people, q.N(perHead)), unit: " €", decimals: 0);
    }

    /// <summary>Task #97: unemployment rate, computed in code.</summary>
    private static QuizQuestion Unemployment(Mc q)
    {
        var active = q.Between(2, 8) * 1000;
        var rate = q.Pick([4, 5, 8, 10, 12, 15, 20, 25]);
        var unemployed = active * rate / 100;
        return q.Number("economics.indicators", q.F("Of {0} people in the labour force, {1} are unemployed. Unemployment rate?|De {0} personas activas, {1} están en paro. ¿Tasa de paro?|Sur {0} actifs, {1} sont au chômage. Taux de chômage ?|De {0} persones actives, {1} són a l'atur. Taxa d'atur?", active, unemployed),
            rate, [unemployed * 100.0 / (active - unemployed), 100 - rate, rate * 10], q.F("{0} ÷ {1} × 100 = {2} % (unemployed ÷ labour force, not ÷ employed).|{0} ÷ {1} × 100 = {2} % (parados ÷ activos, no ÷ ocupados).|{0} ÷ {1} × 100 = {2} % (chômeurs ÷ actifs, pas ÷ actifs occupés).|{0} ÷ {1} × 100 = {2} % (aturats ÷ actius, no ÷ ocupats).", unemployed, active, rate), unit: " %", decimals: 1);
    }

    /// <summary>Task #97: real interest rate (Fisher approximation), computed in code.</summary>
    private static QuizQuestion RealInterest(Mc q)
    {
        var nominal = q.Between(1, 8);
        var inflation = q.Between(1, 6);
        var real = nominal - inflation;
        return q.Number("economics.indicators", q.F("A deposit pays {0} % a year and inflation is {1} %. Approximate real interest rate?|Un depósito paga un {0} % anual y la inflación es del {1} %. ¿Tipo de interés real aproximado?|Un dépôt rapporte {0} % par an et l'inflation est de {1} %. Taux d'intérêt réel approximatif ?|Un dipòsit paga un {0} % anual i la inflació és del {1} %. Tipus d'interès real aproximat?", nominal, inflation),
            real, [nominal + inflation, inflation - nominal, nominal], q.F("r ≈ {0} % − {1} % = {2} %.|r ≈ {0} % − {1} % = {2} %.|r ≈ {0} % − {1} % = {2} %.|r ≈ {0} % − {1} % = {2} %.", nominal, inflation, real), unit: " %", decimals: 0);
    }

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
