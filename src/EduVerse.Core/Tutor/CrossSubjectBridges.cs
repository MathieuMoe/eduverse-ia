using EduVerse.Core.Catalog;
using Txt = EduVerse.Core.Catalog.Text;

namespace EduVerse.Core.Tutor;

/// <summary>
/// A curated link between two topics, in different subjects, that are underneath the same idea (a ratio in maths
/// is a scale on a map, density in physics is the same idea as concentration in chemistry). Unlike
/// <see cref="FigureConnections"/>, which only rewards completed content, a bridge is meant to be surfaced the moment
/// it is useful: when the student already masters one side and is currently struggling with the other, so the
/// Tutor can say "you already know this, just with a different name" instead of re-teaching from zero.
/// </summary>
public sealed record TopicBridge(Area AreaA, string TopicA, Area AreaB, string TopicB, Txt Insight);

/// <summary>A bridge that applies right now: the student already knows <see cref="KnownTopic"/> and is weak on
/// <see cref="StruggleArea"/>/<see cref="StruggleTopic"/>, and <see cref="Bridge"/> connects the two.</summary>
public sealed record BridgeMatch(TopicBridge Bridge, Area KnownArea, string KnownTopic, Area StruggleArea, string StruggleTopic);

public static class CrossSubjectBridges
{
    /// <summary>A topic counts as "known" for a bridge once its mastery reaches this (lower than the stricter
    /// concept-level <see cref="EduVerse.Core.Learning.BktParameters.MasteredThreshold"/>: a bridge is a nudge, not a
    /// certificate, so it only needs the topic to be solidly ahead of the one the student is struggling with).</summary>
    public const double KnownThreshold = 0.8;

    private static TopicBridge Link(Area areaA, string topicA, Area areaB, string topicB, string insight) =>
        new(areaA, topicA, areaB, topicB, Txt.Of(insight));

    private static readonly IReadOnlyList<TopicBridge> BridgeList = [
        Link(Area.Mathematics, "math.proportion", Area.Economics, "economics.percentages",
            "A proportion is exactly what a percentage is: a part compared to a whole. If you can solve one, you can solve the other.|Una proporción es justo lo mismo que un porcentaje: una parte comparada con un todo. Si sabes resolver una, sabes resolver la otra.|Une proportion, c'est exactement ce qu'est un pourcentage : une partie comparée à un tout. Si tu sais résoudre l'une, tu sais résoudre l'autre.|Una proporció és exactament el mateix que un percentatge: una part comparada amb un tot. Si saps resoldre'n una, saps resoldre l'altra."),
        Link(Area.Mathematics, "math.fraction", Area.Economics, "economics.percentages",
            "A percentage is just a fraction out of 100. 25 % is the same idea as 25/100, or 1/4.|Un porcentaje no es más que una fracción sobre 100. El 25 % es la misma idea que 25/100, o que 1/4.|Un pourcentage n'est qu'une fraction sur 100. 25 % est la même idée que 25/100, ou que 1/4.|Un percentatge no és més que una fracció sobre 100. El 25 % és la mateixa idea que 25/100, o que 1/4."),
        Link(Area.Physics, "physics.density", Area.Chemistry, "chemistry.matter",
            "Density is how physics measures how tightly packed matter is — the same property chemistry uses to tell substances apart.|La densidad es como la física mide lo compacta que está la materia: la misma propiedad que la química usa para distinguir sustancias.|La densité, c'est la façon dont la physique mesure à quel point la matière est compacte — la même propriété que la chimie utilise pour distinguer les substances.|La densitat és com la física mesura com de compacta és la matèria: la mateixa propietat que la química fa servir per distingir substàncies."),
        Link(Area.Mathematics, "math.probability", Area.Biology, "biology.genetics",
            "The probabilities you calculate in maths are exactly how genetics predicts which traits a child will inherit.|Las probabilidades que calculas en matemáticas son justo cómo la genética predice qué rasgos heredará un hijo.|Les probabilités que tu calcules en maths, c'est exactement comment la génétique prédit les traits qu'un enfant héritera.|Les probabilitats que calcules a matemàtiques són justament com la genètica prediu quins trets heretarà un fill."),
        Link(Area.Mathematics, "math.slope", Area.Physics, "physics.motion",
            "The slope of a line is the same idea as speed: both are just \"how much one thing changes for each step of another\".|La pendiente de una recta es la misma idea que la velocidad: ambas son \"cuánto cambia una cosa por cada paso de la otra\".|La pente d'une droite, c'est la même idée que la vitesse : les deux ne sont que « combien une chose change pour chaque pas de l'autre ».|El pendent d'una recta és la mateixa idea que la velocitat: totes dues són \"quant canvia una cosa per cada pas de l'altra\"."),
        Link(Area.Mathematics, "math.equations", Area.Physics, "physics.forces",
            "Newton's second law, force equals mass times acceleration, is just an equation — the same kind you already solve in maths.|La segunda ley de Newton, fuerza igual a masa por aceleración, es solo una ecuación: del mismo tipo que ya resuelves en matemáticas.|La deuxième loi de Newton, force égale masse fois accélération, n'est qu'une équation — du même genre que tu résous déjà en maths.|La segona llei de Newton, força igual a massa per acceleració, és només una equació: del mateix tipus que ja resols a matemàtiques."),
        Link(Area.Physics, "physics.sun", Area.Geography, "geography.climate",
            "How much sunlight a place receives, which you study in physics, is the main reason its climate is what it is.|Cuánta luz solar recibe un lugar, lo que estudias en física, es la razón principal de que su clima sea como es.|La quantité de lumière solaire que reçoit un lieu, que tu étudies en physique, est la principale raison pour laquelle son climat est ce qu'il est.|Quanta llum solar rep un lloc, cosa que estudies a física, és la raó principal que el seu clima sigui com és."),
        Link(Area.Programming, "programming.loop", Area.Mathematics, "math.sequences",
            "A loop that repeats a step again and again is a sequence: in maths you already know how to describe exactly that.|Un bucle que repite un paso una y otra vez es una sucesión: en matemáticas ya sabes describir justo eso.|Une boucle qui répète une étape encore et encore est une suite : en maths, tu sais déjà décrire exactement ça.|Un bucle que repeteix un pas una vegada i una altra és una successió: a matemàtiques ja saps descriure exactament això."),
        Link(Area.Mathematics, "math.derivatives", Area.Physics, "physics.motion",
            "Speed is the derivative of position: the rate-of-change idea you learn in maths is literally how physics defines motion.|La velocidad es la derivada de la posición: la idea de \"ritmo de cambio\" que aprendes en matemáticas es, literalmente, cómo la física define el movimiento.|La vitesse est la dérivée de la position : l'idée de taux de variation que tu apprends en maths, c'est littéralement comment la physique définit le mouvement.|La velocitat és la derivada de la posició: la idea de \"ritme de canvi\" que aprens a matemàtiques és, literalment, com la física defineix el moviment."),
        Link(Area.Chemistry, "chemistry.reactions", Area.Biology, "biology.photosynthesis",
            "Photosynthesis is a chemical reaction: the same kind of before-and-after you balance in chemistry, just happening inside a leaf.|La fotosíntesis es una reacción química: el mismo tipo de antes-y-después que ajustas en química, solo que ocurre dentro de una hoja.|La photosynthèse est une réaction chimique : le même genre d'avant-après que tu équilibres en chimie, sauf qu'elle se passe dans une feuille.|La fotosíntesi és una reacció química: el mateix tipus d'abans-i-després que ajustes en química, només que passa dins d'una fulla."),
        Link(Area.Language, "language.grammar", Area.ForeignLanguage, "foreign.grammar",
            "Grammar is grammar: the subject-verb-object habits you already know in your own language are the shortcut to learning them in another one.|La gramática es gramática: los hábitos de sujeto-verbo-objeto que ya conoces en tu idioma son el atajo para aprenderlos en otro.|La grammaire, c'est la grammaire : les habitudes sujet-verbe-objet que tu connais déjà dans ta langue sont le raccourci pour les apprendre dans une autre.|La gramàtica és gramàtica: els hàbits de subjecte-verb-objecte que ja coneixes en el teu idioma són la drecera per aprendre'ls en un altre."),
        Link(Area.Mathematics, "math.powers", Area.Biology, "biology.populations",
            "A population that doubles again and again grows by powers, exactly the fast-growing numbers you already compute in maths.|Una población que se duplica una y otra vez crece en potencias, justo los números que crecen rápido que ya calculas en matemáticas.|Une population qui double encore et encore croît par puissances, exactement les nombres à croissance rapide que tu calcules déjà en maths.|Una població que es duplica una vegada i una altra creix en potències, justament els nombres que creixen ràpid que ja calcules a matemàtiques."),
    ];

    /// <summary>All curated bridges, for tools and tests.</summary>
    public static IReadOnlyList<TopicBridge> All() => BridgeList;

    /// <summary>The best bridge for a topic the student is currently struggling with, given the topics they already
    /// master (curated order is the priority order; the first bridge whose other side is mastered wins). Null when
    /// no curated bridge connects this topic to anything the student already knows.</summary>
    public static BridgeMatch? FindFor(Area struggleArea, string struggleTopic, IReadOnlyCollection<string> masteredTopics)
    {
        ArgumentNullException.ThrowIfNull(masteredTopics);
        if (masteredTopics.Count == 0)
        {
            return null;
        }

        foreach (var b in BridgeList)
        {
            if (b.TopicA == struggleTopic && masteredTopics.Contains(b.TopicB))
            {
                return new BridgeMatch(b, b.AreaB, b.TopicB, struggleArea, struggleTopic);
            }

            if (b.TopicB == struggleTopic && masteredTopics.Contains(b.TopicA))
            {
                return new BridgeMatch(b, b.AreaA, b.TopicA, struggleArea, struggleTopic);
            }
        }

        return null;
    }
}
