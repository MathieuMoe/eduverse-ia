using System.Text.Json;
using EduVerse.Core.Abstractions;
using EduVerse.Core.Common;
using EduVerse.Core.Parsing;

namespace EduVerse.Core.Catalog;

public enum Theme
{
    Space,
    Ocean,
    Jungle,
    Castle,
}

/// <summary>Progress of an adventure: doors passed, hearts left, health of the boss.</summary>
public sealed record AdventureState(Theme Theme, int Passed, int Hearts, int BossHp)
{
    public bool InBoss => Passed >= Adventure.Doors;

    public bool Won => BossHp <= 0;

    public bool Lost => Hearts <= 0 && !Won;

    public bool Finished => Won || Lost;
}

/// <summary>
/// An adventure: 5 doors (one question each; a wrong answer costs a heart and the door asks again) then a boss that needs
/// 4 right answers, asked on the student's weakest topics at the hardest difficulty. 3 hearts for the whole adventure.
/// The code decides everything; the AI (or the offline writer) only tells the story.
/// </summary>
public static class Adventure
{
    public const int Doors = 5;
    public const int Hearts = 3;
    public const int BossHp = 4;

    public static AdventureState Start(Theme theme) => new(theme, 0, Hearts, BossHp);

    public static AdventureState Answer(AdventureState state, bool correct)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.Finished)
        {
            return state;
        }

        if (!correct)
        {
            return state with { Hearts = state.Hearts - 1 };
        }

        return state.InBoss ? state with { BossHp = state.BossHp - 1 } : state with { Passed = state.Passed + 1 };
    }

    /// <summary>Stars of an adventure won: one per heart left.</summary>
    public static int Stars(AdventureState state) => state is { Won: true } ? Math.Max(1, state.Hearts) : 0;

    public static string Emoji(Theme theme) => theme switch
    {
        Theme.Space => "🚀",
        Theme.Ocean => "🌊",
        Theme.Jungle => "🌴",
        _ => "🏰",
    };
}

/// <summary>The story of an adventure: a title, an introduction, one line per door, the boss and the two endings.</summary>
public sealed record Story(string Title, string Intro, IReadOnlyList<string> Doors, string Boss, string BossIntro, string Victory, string Defeat);

/// <summary>Story written by the AI. Only the theme, the subject and the level are sent: nothing written by the student.</summary>
public static class StoryPrompt
{
    public const string ToolName = "adventure_story";

    public const string Schema = """
    {
      "type": "object",
      "properties": {
        "title": { "type": "string" },
        "intro": { "type": "string", "description": "2-3 sentences that set the scene and the mission." },
        "doors": { "type": "array", "items": { "type": "string" }, "minItems": 5, "maxItems": 5, "description": "One sentence per step of the journey, before each question." },
        "boss": { "type": "string", "description": "Name of a friendly, not scary, final guardian." },
        "bossIntro": { "type": "string" },
        "victory": { "type": "string" },
        "defeat": { "type": "string", "description": "Encouraging: the hero can try again." }
      },
      "required": ["title", "intro", "doors", "boss", "bossIntro", "victory", "defeat"]
    }
    """;

    public static StructuredPrompt Build(Theme theme, Area area, Level level, TeachLanguage language)
    {
        var system = $"""
            You write a short interactive adventure for a learning game, for {Levels.PromptName(level)}.
            Theme: {theme}. Each step of the story is unlocked by answering a question of {area}; do not write the questions, only the story.
            The student is the hero. Keep it kind, funny and age-appropriate: no violence, no fear, no real people, no brands or known characters.
            The final guardian is a friendly creature who loves riddles. Short sentences (at most 30 words each).
            Write everything in {Lang.Name(language)}. Call the tool {ToolName}.
            """;
        return new StructuredPrompt(system, $"Theme: {theme}. Subject: {area}.", ToolName, "Returns the story.", Schema, 1500);
    }

    public static Story Parse(JsonElement json)
    {
        var doors = Json.List(json, "doors", Adventure.Doors, 220);
        var story = new Story(Json.Text(json, "title", 80), Json.Text(json, "intro", 400), doors, Json.Text(json, "boss", 60),
            Json.Text(json, "bossIntro", 300), Json.Text(json, "victory", 300), Json.Text(json, "defeat", 300));
        if (story.Title.Length == 0 || story.Intro.Length == 0 || doors.Count != Adventure.Doors || doors.Any(d => d.Length == 0) || story.Boss.Length == 0
            || story.Victory.Length == 0 || story.Defeat.Length == 0)
        {
            throw new AiGenerationException("The AI did not return a complete story.");
        }

        return story;
    }
}

/// <summary>Stories written in advance, for the offline mode (4 themes × 4 languages).</summary>
public static class DemoStories
{
    public static Story Write(Theme theme, Area area, TeachLanguage language)
    {
        string T(string text) => Text.Of(text).In(language);
        var subject = SubjectName(area, language);
        string F(string text) => string.Format(Levels.Culture(language), T(text), subject);
        return theme switch
        {
            Theme.Space => new Story(
                F("The {0} nebula|La nebulosa de {0}|La nébuleuse {0}|La nebulosa de {0}"),
                F("Your ship, the Curiosity, has lost its map. Five beacons of {0} guide you home: light them up!|Tu nave, la Curiosidad, ha perdido el mapa. Cinco balizas de {0} te guían a casa: ¡enciéndelas!|Ton vaisseau, la Curiosité, a perdu sa carte. Cinq balises de {0} te guident vers la maison : allume-les !|La teva nau, la Curiositat, ha perdut el mapa. Cinc balises de {0} et guien cap a casa: encén-les!"),
                [
                    T("Beacon 1 blinks on a frozen moon. Answer to wake it up.|La baliza 1 parpadea en una luna helada. Responde para despertarla.|La balise 1 clignote sur une lune gelée. Réponds pour la réveiller.|La balisa 1 parpelleja en una lluna gelada. Respon per despertar-la."),
                    T("A comet passes by and drops a riddle in your hold.|Pasa un cometa y deja un acertijo en tu bodega.|Une comète passe et laisse une énigme dans ta soute.|Passa un cometa i deixa un enigma a la bodega."),
                    T("The radar beeps: beacon 3 needs a right answer to open.|El radar pita: la baliza 3 necesita una respuesta correcta para abrirse.|Le radar bipe : la balise 3 a besoin d'une bonne réponse pour s'ouvrir.|El radar xiula: la balisa 3 necessita una resposta correcta per obrir-se."),
                    T("You cross an asteroid field. Steady... and think!|Cruzas un campo de asteroides. Calma... ¡y piensa!|Tu traverses un champ d'astéroïdes. Du calme... et réfléchis !|Travesses un camp d'asteroides. Calma... i pensa!"),
                    T("Last beacon! Home is already shining in the distance.|¡Última baliza! Tu casa ya brilla a lo lejos.|Dernière balise ! La maison brille déjà au loin.|Última balisa! Casa teva ja brilla a la llunyania."),
                ],
                T("Zorb, the Riddle Robot|Zorb, el robot de los acertijos|Zorb, le robot des énigmes|Zorb, el robot dels enigmes"),
                T("Zorb blocks the space gate: \"Beep! Only those who answer my four riddles may pass!\"|Zorb bloquea la puerta espacial: «¡Bip! ¡Solo pasa quien responda mis cuatro acertijos!»|Zorb bloque la porte spatiale : « Bip ! Seuls ceux qui résolvent mes quatre énigmes passeront ! »|En Zorb bloqueja la porta espacial: «Bip! Només passa qui respongui els meus quatre enigmes!»"),
                T("Zorb spins with joy: \"Bzzz! You are a true space explorer!\" The gate opens and you fly home.|Zorb gira de alegría: «¡Bzzz! ¡Exploras el espacio como nadie!» La puerta se abre y vuelas a casa.|Zorb tourne de joie : « Bzzz ! Tu explores l'espace comme personne ! » La porte s'ouvre et tu rentres.|En Zorb gira d'alegria: «Bzzz! Explores l'espai com ningú!» La porta s'obre i tornes a casa."),
                T("Your shields are empty... but Zorb winks: \"Recharge and come back, explorer!\"|Tus escudos se han vaciado... pero Zorb te guiña un ojo: «¡Recarga y vuelve a intentarlo!»|Tes boucliers sont vides... mais Zorb te fait un clin d'œil : « Recharge et reviens vite ! »|Els escuts s'han buidat... però en Zorb et pica l'ullet: «Recarrega i torna-ho a provar!»")),
            Theme.Ocean => new Story(
                F("The pearls of {0}|Las perlas de {0}|Les perles de {0}|Les perles de {0}"),
                F("The coral reef has lost its five magic pearls of {0}. Dive in your submarine and bring them back!|El arrecife de coral ha perdido sus cinco perlas mágicas de {0}. ¡Sumérgete en tu submarino y recupéralas!|Le récif de corail a perdu ses cinq perles magiques de {0}. Plonge dans ton sous-marin et rapporte-les !|L'escull de corall ha perdut les seves cinc perles màgiques de {0}. Submergeix-te amb el submarí i recupera-les!"),
                [
                    T("A shy clownfish shows you the first shell.|Un pez payaso tímido te enseña la primera concha.|Un poisson-clown timide te montre le premier coquillage.|Un peix pallasso tímid t'ensenya la primera petxina."),
                    T("A turtle needs help to read an old map.|Una tortuga necesita ayuda para leer un mapa antiguo.|Une tortue a besoin d'aide pour lire une vieille carte.|Una tortuga necessita ajuda per llegir un mapa antic."),
                    T("Bubbles! The third pearl is inside a giant clam that only opens for clever divers.|¡Burbujas! La tercera perla está en una almeja gigante que solo se abre a buceadores listos.|Des bulles ! La troisième perle est dans une palourde géante qui ne s'ouvre qu'aux plongeurs malins.|Bombolles! La tercera perla és dins d'una cloïssa gegant que només s'obre als bussejadors espavilats."),
                    T("You glide through a forest of seaweed lit by jellyfish.|Te deslizas por un bosque de algas iluminado por medusas.|Tu glisses dans une forêt d'algues éclairée par des méduses.|Llisques per un bosc d'algues il·luminat per meduses."),
                    T("The last pearl shines at the bottom of a deep trench.|La última perla brilla en el fondo de una fosa profunda.|La dernière perle brille au fond d'une fosse profonde.|L'última perla brilla al fons d'una fossa profunda."),
                ],
                T("Octavia, the Puzzle Octopus|Octavia, el pulpo de los enigmas|Octavia, la pieuvre aux énigmes|Octàvia, el pop dels enigmes"),
                T("Octavia hugs the treasure chest with her eight arms: \"Four questions, little diver, and the pearls are yours!\"|Octavia abraza el cofre con sus ocho brazos: «¡Cuatro preguntas y las perlas son tuyas!»|Octavia serre le coffre de ses huit bras : « Quatre questions, et les perles sont à toi ! »|L'Octàvia abraça el cofre amb els seus vuit braços: «Quatre preguntes i les perles són teves!»"),
                T("Octavia claps with all eight arms! The pearls go back to the reef, which shines like a rainbow.|¡Octavia aplaude con sus ocho brazos! Las perlas vuelven al arrecife, que brilla como un arcoíris.|Octavia applaudit de ses huit bras ! Les perles retournent au récif, qui brille comme un arc-en-ciel.|L'Octàvia aplaudeix amb els vuit braços! Les perles tornen a l'escull, que brilla com un arc de Sant Martí."),
                T("Your oxygen is running low. Octavia smiles: \"Come up for air and dive again!\"|Te queda poco oxígeno. Octavia sonríe: «¡Sube a respirar y vuelve a bucear!»|Tu n'as presque plus d'oxygène. Octavia sourit : « Remonte respirer et replonge ! »|Et queda poc oxigen. L'Octàvia somriu: «Puja a respirar i torna a bussejar!»")),
            Theme.Jungle => new Story(
                F("The temple of {0}|El templo de {0}|Le temple de {0}|El temple de {0}"),
                F("Deep in the jungle, a lost temple hides the treasure of {0}. Five stone doors stand on the path.|En lo profundo de la selva, un templo perdido esconde el tesoro de {0}. Cinco puertas de piedra cierran el camino.|Au cœur de la jungle, un temple perdu cache le trésor de {0}. Cinq portes de pierre barrent le chemin.|Al cor de la selva, un temple perdut amaga el tresor de {0}. Cinc portes de pedra tanquen el camí."),
                [
                    T("A toucan lands on your shoulder and points to the first door.|Un tucán se posa en tu hombro y señala la primera puerta.|Un toucan se pose sur ton épaule et montre la première porte.|Un tucà es posa a la teva espatlla i assenyala la primera porta."),
                    T("You cross a rope bridge over a singing river.|Cruzas un puente colgante sobre un río que canta.|Tu traverses un pont de cordes au-dessus d'une rivière qui chante.|Travesses un pont penjant sobre un riu que canta."),
                    T("Monkeys giggle: the third door has a riddle carved in gold.|Los monos se ríen: la tercera puerta tiene un acertijo grabado en oro.|Les singes rigolent : la troisième porte a une énigme gravée dans l'or.|Els micos riuen: la tercera porta té un enigma gravat en or."),
                    T("Glowing butterflies light the way through the vines.|Mariposas luminosas iluminan el camino entre las lianas.|Des papillons lumineux éclairent le chemin entre les lianes.|Papallones lluminoses il·luminen el camí entre les lianes."),
                    T("The last door is covered in moss. You can almost touch the treasure!|La última puerta está cubierta de musgo. ¡Ya casi tocas el tesoro!|La dernière porte est couverte de mousse. Tu touches presque le trésor !|L'última porta és coberta de molsa. Gairebé toques el tresor!"),
                ],
                T("Kiko, the Wise Jaguar|Kiko, el jaguar sabio|Kiko, le jaguar sage|Kiko, el jaguar savi"),
                T("Kiko yawns on the treasure: \"Answer my four riddles and I'll let you pass... after my nap.\"|Kiko bosteza sobre el tesoro: «Responde mis cuatro acertijos y te dejaré pasar... después de mi siesta.»|Kiko bâille sur le trésor : « Réponds à mes quatre énigmes et je te laisserai passer... après ma sieste. »|En Kiko badalla sobre el tresor: «Respon els meus quatre enigmes i et deixaré passar... després de la migdiada.»"),
                T("Kiko purrs: \"Well done, explorer!\" The temple fills with golden light: the treasure is yours.|Kiko ronronea: «¡Bien hecho!» El templo se llena de luz dorada: el tesoro es tuyo.|Kiko ronronne : « Bravo ! » Le temple s'illumine d'or : le trésor est à toi.|En Kiko ronca: «Ben fet!» El temple s'omple de llum daurada: el tresor és teu."),
                T("Night falls on the jungle. Kiko stretches: \"Rest at the camp and come back tomorrow!\"|Cae la noche en la selva. Kiko se estira: «¡Descansa en el campamento y vuelve mañana!»|La nuit tombe sur la jungle. Kiko s'étire : « Repose-toi au camp et reviens demain ! »|Cau la nit a la selva. En Kiko s'estira: «Descansa al campament i torna demà!»")),
            _ => new Story(
                F("The castle of {0}|El castillo de {0}|Le château de {0}|El castell de {0}"),
                F("The kingdom's library has been locked by a spell. Climb the five towers of {0} to break it!|La biblioteca del reino ha quedado cerrada por un hechizo. ¡Sube las cinco torres de {0} para romperlo!|La bibliothèque du royaume a été fermée par un sortilège. Gravis les cinq tours de {0} pour le briser !|La biblioteca del regne ha quedat tancada per un encanteri. Puja les cinc torres de {0} per trencar-lo!"),
                [
                    T("The drawbridge creaks: the guard asks a first question.|El puente levadizo cruje: la guardiana hace una primera pregunta.|Le pont-levis grince : le garde pose une première question.|El pont llevadís cruix: la guarda fa una primera pregunta."),
                    T("A friendly ghost floats by and hums a riddle.|Un fantasma simpático pasa flotando y tararea un acertijo.|Un gentil fantôme passe en flottant et fredonne une énigme.|Un fantasma simpàtic passa flotant i taral·leja un enigma."),
                    T("In the third tower, books fly around like birds.|En la tercera torre, los libros vuelan como pájaros.|Dans la troisième tour, les livres volent comme des oiseaux.|A la tercera torre, els llibres volen com ocells."),
                    T("A spiral staircase... with a question on every step!|Una escalera de caracol... ¡con una pregunta en cada escalón!|Un escalier en colimaçon... avec une question à chaque marche !|Una escala de cargol... amb una pregunta a cada graó!"),
                    T("The last tower! You can hear the books whispering behind the door.|¡La última torre! Se oyen los libros susurrar tras la puerta.|La dernière tour ! On entend les livres chuchoter derrière la porte.|L'última torre! Se senten els llibres xiuxiuejar darrere la porta."),
                ],
                T("Flamel, the Dragon Librarian|Flamel, el dragón bibliotecario|Flamel, le dragon bibliothécaire|Flamel, el drac bibliotecari"),
                T("Flamel adjusts his glasses: \"Four questions, young scholar. Answer them and the spell is broken!\"|Flamel se ajusta las gafas: «Cuatro preguntas. ¡Respóndelas y el hechizo se romperá!»|Flamel ajuste ses lunettes : « Quatre questions. Réponds-y et le sortilège sera brisé ! »|En Flamel s'ajusta les ulleres: «Quatre preguntes. Respon-les i l'encanteri es trencarà!»"),
                T("Flamel blows a cloud of confetti! The library opens and every book thanks you.|¡Flamel sopla una nube de confeti! La biblioteca se abre y todos los libros te dan las gracias.|Flamel souffle un nuage de confettis ! La bibliothèque s'ouvre et tous les livres te remercient.|En Flamel bufa un núvol de confeti! La biblioteca s'obre i tots els llibres et donen les gràcies."),
                T("The candles go out... Flamel smiles kindly: \"Even dragons need several tries. Come back soon!\"|Las velas se apagan... Flamel sonríe con amabilidad: «Hasta los dragones necesitan varios intentos. ¡Vuelve pronto!»|Les bougies s'éteignent... Flamel sourit gentiment : « Même les dragons ont besoin de plusieurs essais. Reviens vite ! »|Les espelmes s'apaguen... En Flamel somriu amablement: «Fins i tot els dracs necessiten diversos intents. Torna aviat!»")),
        };
    }

    private static string SubjectName(Area area, TeachLanguage language) => Text.Of(area switch
    {
        Area.Mathematics => "numbers|los números|des nombres|els nombres",
        Area.Physics => "physics|la física|la physique|la física",
        Area.Chemistry => "chemistry|la química|la chimie|la química",
        Area.Biology => "life|la vida|la vie|la vida",
        Area.Language => "words|las palabras|des mots|les paraules",
        Area.ForeignLanguage => "languages|los idiomas|des langues|els idiomes",
        Area.History => "history|la historia|l'histoire|la història",
        Area.Geography => "the world|el mundo|du monde|el món",
        Area.Economics => "economics|la economía|l'économie|l'economia",
        _ => "code|el código|du code|el codi",
    }).In(language);
}
