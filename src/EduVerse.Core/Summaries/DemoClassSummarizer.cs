using System.Globalization;
using System.Text.RegularExpressions;
using EduVerse.Core.Common;
using EduVerse.Core.Lessons;
using EduVerse.Core.Text;

namespace EduVerse.Core.Summaries;

/// <summary>Offline summary: the most representative sentences (word frequency), homework, difficulties and next steps found in the notes.</summary>
public sealed class DemoClassSummarizer : IClassSummarizer
{
    public Task<ClassSummary> SummarizeAsync(SummaryRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var l = request.Language;
        string T(string en, string es, string fr, string ca) => Lang.T(l, en, es, fr, ca);
        string F(string f, params object[] a) => string.Format(CultureInfo.InvariantCulture, f, a);

        var homework = NotesScanner.Homework(request.Notes);
        var difficulties = NotesScanner.Difficulties(request.Notes);
        var next = NotesScanner.NextClass(request.Notes);
        var special = homework.Concat(difficulties).Concat(next).ToHashSet();
        var sentences = TextMeasure.Sentences(request.Notes).Select(s => s.TrimStart('-', '•', '*', ' ')).Where(s => !special.Contains(s) && TextMeasure.Words(s).Count >= 4).ToList();

        // Frequency of meaningful words, then sentences scored by the words they contain
        var freq = TextMeasure.Words(request.Notes).Select(TextMeasure.Normalize).Where(w => w.Length > 3 && !TextMeasure.StopWords.Contains(w))
            .GroupBy(w => w).ToDictionary(g => g.Key, g => g.Count());
        double Score(string s)
        {
            var words = TextMeasure.Words(s).Select(TextMeasure.Normalize).ToList();
            return words.Count == 0 ? 0 : words.Sum(w => freq.GetValueOrDefault(w)) / Math.Sqrt(words.Count);
        }

        var top = sentences.Select((s, i) => (s, i, score: Score(s))).OrderByDescending(x => x.score).Take(5).OrderBy(x => x.i).Select(x => x.s).ToList();
        var summary = top.Count == 0 ? request.Notes.Trim() : string.Join(" ", top.Take(4));
        var terms = TextMeasure.KeyTerms(request.Notes, 5);
        var questions = terms.Select(t => (t, s: sentences.FirstOrDefault(x => x.Contains(t, StringComparison.OrdinalIgnoreCase))))
            .Where(x => x.s is not null).Take(4)
            .Select(x => new QuestionAnswer(T("Complete: ", "Completa: ", "Complète : ", "Completa: ") + Regex.Replace(x.s!, Regex.Escape(x.t), "_____", RegexOptions.IgnoreCase), x.t)).ToList();

        var date = request.Date.ToString("d", CultureInfo.GetCultureInfo(l switch { TeachLanguage.Spanish => "es", TeachLanguage.French => "fr", TeachLanguage.Catalan => "ca", _ => "en" }));
        var title = F(T("{0} · {1} · {2}", "{0} · {1} · {2}", "{0} · {1} · {2}", "{0} · {1} · {2}"), request.Subject, request.Group, date);
        var forAbsent = F(T("Read the summary and the key ideas, then do the review questions. {0}Ask your teacher at the start of the next class if something is not clear.",
                            "Lee el resumen y las ideas clave y haz las preguntas de repaso. {0}Pregunta al profesor al inicio de la próxima clase si algo no queda claro.",
                            "Lis le résumé et les idées clés, puis fais les questions de révision. {0}Demande au professeur au début du prochain cours si quelque chose n'est pas clair.",
                            "Llegeix el resum i les idees clau i fes les preguntes de repàs. {0}Pregunta al professor a l'inici de la propera classe si alguna cosa no queda clara."),
            homework.Count > 0 ? T("Don't forget the homework. ", "No olvides los deberes. ", "N'oublie pas les devoirs. ", "No oblidis els deures. ") : "");
        var family = F(T("Today in {0} the group worked on: {1}. {2}", "Hoy en {0} el grupo ha trabajado: {1}. {2}", "Aujourd'hui en {0}, le groupe a travaillé : {1}. {2}", "Avui a {0} el grup ha treballat: {1}. {2}"),
            request.Subject, terms.Count > 0 ? string.Join(", ", terms.Take(3)) : request.Subject,
            homework.Count > 0 ? T("There is homework for the next class.", "Hay deberes para la próxima clase.", "Il y a des devoirs pour le prochain cours.", "Hi ha deures per a la propera classe.") : T("There is no homework today.", "Hoy no hay deberes.", "Pas de devoirs aujourd'hui.", "Avui no hi ha deures."));
        var nextSteps = next.ToList();
        if (difficulties.Count > 0)
        {
            nextSteps.Insert(0, T("Start with a 5-minute review of what was difficult.", "Empezar con un repaso de 5 minutos de lo que costó.", "Commencer par une révision de 5 minutes de ce qui a posé problème.", "Començar amb un repàs de 5 minuts del que va costar."));
        }

        return Task.FromResult(new ClassSummary(title, summary, top.Take(5).ToList(), homework.Select(h => new Homework(h, null)).ToList(),
            forAbsent, family, questions, difficulties, nextSteps, []));
    }
}

public static class SummarySamples
{
    /// <summary>Teacher's notes to try the module (fictional class).</summary>
    public static string Notes(TeachLanguage language) => Lang.T(language,
        """
        Started with a quick review of states of matter. Today: the water cycle. Explained evaporation: the sun heats the water of seas and rivers and it turns into water vapour.
        Then condensation: the vapour cools down in the atmosphere and forms clouds. Precipitation: the water falls as rain, snow or hail.
        We did the experiment with the hot water and the cold plate, they loved it. Infiltration and runoff: some water goes into the ground and feeds the aquifers.
        Several students struggled to tell condensation from evaporation, we need to review it.
        Homework: draw the water cycle with the four stages and write one sentence for each, for Thursday.
        Next class: start with a quiz on the stages and then the importance of saving water.
        """,
        """
        Empezamos con un repaso rápido de los estados de la materia. Hoy: el ciclo del agua. Expliqué la evaporación: el sol calienta el agua de mares y ríos y se convierte en vapor de agua.
        Después la condensación: el vapor se enfría en la atmósfera y forma las nubes. La precipitación: el agua cae en forma de lluvia, nieve o granizo.
        Hicimos el experimento del agua caliente y el plato frío, les encantó. Infiltración y escorrentía: parte del agua entra en el suelo y alimenta los acuíferos.
        A varios alumnos les costó distinguir condensación y evaporación, hay que repasarlo.
        Deberes: dibujar el ciclo del agua con las cuatro fases y escribir una frase para cada una, para el jueves.
        Próxima clase: empezar con un test rápido de las fases y luego la importancia de ahorrar agua.
        """,
        """
        Début par une révision rapide des états de la matière. Aujourd'hui : le cycle de l'eau. J'ai expliqué l'évaporation : le soleil chauffe l'eau des mers et des rivières et elle se transforme en vapeur d'eau.
        Puis la condensation : la vapeur se refroidit dans l'atmosphère et forme les nuages. Les précipitations : l'eau tombe sous forme de pluie, de neige ou de grêle.
        Nous avons fait l'expérience de l'eau chaude et de l'assiette froide, ils ont adoré. Infiltration et ruissellement : une partie de l'eau entre dans le sol et alimente les nappes.
        Plusieurs élèves ont eu du mal à distinguer condensation et évaporation, il faut le revoir.
        Devoirs : dessiner le cycle de l'eau avec les quatre étapes et écrire une phrase pour chacune, pour jeudi.
        Prochain cours : commencer par un quiz sur les étapes puis l'importance d'économiser l'eau.
        """,
        """
        Comencem amb un repàs ràpid dels estats de la matèria. Avui: el cicle de l'aigua. Vaig explicar l'evaporació: el sol escalfa l'aigua de mars i rius i es converteix en vapor d'aigua.
        Després la condensació: el vapor es refreda a l'atmosfera i forma els núvols. La precipitació: l'aigua cau en forma de pluja, neu o calamarsa.
        Vam fer l'experiment de l'aigua calenta i el plat fred, els va encantar. Infiltració i escolament: part de l'aigua entra al sòl i alimenta els aqüífers.
        A diversos alumnes els va costar distingir condensació i evaporació, cal repassar-ho.
        Deures: dibuixar el cicle de l'aigua amb les quatre fases i escriure una frase per a cadascuna, per a dijous.
        Propera classe: començar amb un test ràpid de les fases i després la importància d'estalviar aigua.
        """);
}
