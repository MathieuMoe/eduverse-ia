using EduVerse.Core.Common;

namespace EduVerse.Core.Tutor;

/// <summary>What the letter from the future is built from (names and descriptions already in the student's language,
/// nothing invented): how many topics the student has mastered across every subject, up to two past struggles now
/// confirmed gone, and — when the student has one — the open "what I wonder about" question the forward-looking
/// line is built around, with the link that lets them act on it today.</summary>
public sealed record FutureSelfFacts(string FirstName, int MasteredCount, IReadOnlyList<string> Overcome, string? Curiosity, string? CuriosityUrl);

public sealed record FutureSelfLetter(string Title, string Opening, IReadOnlyList<string> Body, string NextStep, string? NextStepUrl, string Closing);

/// <summary>
/// A one-off letter (not weekly, so it keeps its impact), written as if sent by the student's own self one year
/// ahead, who has already mastered what the student struggles with today. Reuses <see cref="WeeklyLetter"/>'s
/// pattern — written entirely by code from measured facts, so it works offline too — but looks forward instead of
/// back: when the student has an open <see cref="Curiosity"/> question, the future self "already found the answer"
/// and nudges today's self to take the first step towards it, instead of only celebrating past mastery.
/// </summary>
public static class FutureSelf
{
    /// <summary>Round numbers of topics mastered worth a letter — rare enough that each one keeps its impact.</summary>
    public static readonly IReadOnlyList<int> Milestones = [5, 10, 15, 20, 25, 30, 40, 50, 75, 100];

    /// <summary>How many topics past a milestone the letter still shows (a student who checks back the next day should
    /// still find it, not just on the exact topic that tipped the count over).</summary>
    public const int MilestoneWindow = 2;

    /// <summary>The milestone the student has just reached, or null when they are not freshly at one.</summary>
    public static int? MilestoneFor(int masteredCount) =>
        Milestones.Where(m => m <= masteredCount && masteredCount - m <= MilestoneWindow).Max(m => (int?)m);

    public static bool HasContent(FutureSelfFacts f) => MilestoneFor(f.MasteredCount) is not null;

    public static FutureSelfLetter Build(FutureSelfFacts f, TeachLanguage language)
    {
        ArgumentNullException.ThrowIfNull(f);
        var name = string.IsNullOrWhiteSpace(f.FirstName) ? "" : f.FirstName;
        var milestone = MilestoneFor(f.MasteredCount) ?? f.MasteredCount;
        var title = Lang.T(language,
            "A letter from your future self", "Una carta de tu yo del futuro", "Une lettre de ton futur toi", "Una carta del teu jo del futur");
        var opening = Lang.T(language,
            $"Hi {name}, it's you — one year from now. I still remember reaching topic number {milestone}: it didn't feel like much that day, but looking back, that is exactly how it adds up.",
            $"Hola {name}, soy yo, un año en el futuro. Todavía recuerdo llegar al tema número {milestone}: ese día no pareció gran cosa, pero visto desde aquí, así es exactamente como se suma todo.",
            $"Salut {name}, c'est toi — dans un an. Je me souviens encore d'avoir atteint le sujet numéro {milestone} : ce jour-là, ça ne semblait pas grand-chose, mais avec du recul, c'est exactement comme ça que tout s'additionne.",
            $"Hola {name}, sóc jo, d'aquí un any. Encara recordo arribar al tema número {milestone}: aquell dia no va semblar gran cosa, però vist des d'aquí, és exactament així com se suma tot.");
        var body = f.Overcome.Select(topic => Lang.T(language,
            $"Remember how {topic} used to trip you up? From where I am now, that is nothing — just one more thing that clicked once you kept at it.",
            $"¿Te acuerdas de cómo {topic} te costaba? Desde donde estoy ahora, eso ya no es nada: una cosa más que terminó encajando a base de seguir intentándolo.",
            $"Tu te souviens comme {topic} te donnait du fil à retordre ? D'où je suis maintenant, ce n'est plus rien : juste une chose de plus qui a fini par devenir claire à force de persévérer.",
            $"Et recordes com {topic} et costava? Des d'on soc ara, això ja no és res: una cosa més que va acabar encaixant a força de seguir-ho intentant.")).ToList();
        var (nextStep, nextStepUrl) = f.Curiosity is { Length: > 0 } q
            ? (Lang.T(language,
                $"One more thing: I remember you kept wondering \"{q}\". I already know the answer now — go take the first step towards it today, it's worth it.",
                $"Una cosa más: recuerdo que te preguntabas «{q}». Yo ya sé la respuesta; da hoy el primer paso para descubrirla, merece la pena.",
                $"Encore une chose : je me souviens que tu te demandais « {q} ». Je connais déjà la réponse maintenant ; fais aujourd'hui le premier pas pour la découvrir, ça en vaut la peine.",
                $"Una cosa més: recordo que et preguntaves «{q}». Jo ja sé la resposta; fes avui el primer pas per descobrir-la, val la pena."), f.CuriosityUrl)
            : (Lang.T(language,
                "One more thing: pick something you're curious about and go find out today — future you is already glad you did.",
                "Una cosa más: elige algo que te dé curiosidad y descúbrelo hoy; tu yo del futuro ya se alegra de que lo hayas hecho.",
                "Encore une chose : choisis quelque chose qui t'intrigue et découvre-le aujourd'hui ; ton futur toi est déjà content que tu l'aies fait.",
                "Una cosa més: tria alguna cosa que et doni curiositat i descobreix-la avui; el teu jo del futur ja s'alegra que ho hagis fet."), null);
        var closing = Lang.T(language,
            "See you in a year — keep going, it really does add up.",
            "Nos vemos en un año: sigue así, de verdad que todo suma.",
            "À dans un an — continue, ça compte vraiment, tout ça.",
            "Ens veiem d'aquí un any: segueix així, de debò que tot suma.");
        return new FutureSelfLetter(title, opening, body, nextStep, nextStepUrl, closing);
    }
}
