using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using EduVerse.Core.Common;
using EduVerse.Core.Text;

namespace EduVerse.Core.Lessons;

/// <summary>
/// Offline adaptation with rules: the basic level cuts long sentences and removes asides, the advanced level adds
/// questions to go further; glossary and questions come from the key terms. An AI engine rewrites the text for real.
/// </summary>
public sealed partial class DemoLessonAdapter : ILessonAdapter
{
    public const int BasicMaxWords = 15;

    [GeneratedRegex(@"\s*\([^)]*\)")]
    private static partial Regex Parentheses();

    // Cut points: ";" or a comma followed by a relative or causal connector (not the commas of an enumeration)
    [GeneratedRegex(@"\s*;\s*|,\s+(?=(?:which|who|because|although|whereas|que|quien|quienes|porque|aunque|mientras|qui|qu'|parce que|bien que|alors que|perquè|encara que|mentre)\b)", RegexOptions.IgnoreCase)]
    private static partial Regex Cuts();

    [GeneratedRegex(@"^(?:which|who|that|que|qui|qu'|quien|quienes)\s*", RegexOptions.IgnoreCase)]
    private static partial Regex Relative();

    private static readonly HashSet<string> Pronouns = new(StringComparer.OrdinalIgnoreCase) { "they", "it", "he", "she", "we", "you", "i", "il", "elle", "ils", "elles", "on", "nous", "vous" };

    public Task<LessonResult> AdaptAsync(LessonRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var l = request.Language;
        string T(string en, string es, string fr, string ca) => Lang.T(l, en, es, fr, ca);
        string F(string f, params object[] a) => string.Format(CultureInfo.InvariantCulture, f, a);
        var text = request.Text.Trim();
        var sentences = TextMeasure.Sentences(text);
        var terms = TextMeasure.KeyTerms(text, 8);

        // ---- Basic: shorter sentences, no asides, two sentences per paragraph
        var basicSentences = sentences.SelectMany(Simplify).ToList();
        var basicText = string.Join("\n\n", basicSentences.Chunk(2).Select(c => string.Join(" ", c)));

        // ---- Standard: the original, cleaned
        var standardText = string.Join("\n\n", text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        // ---- Advanced: the original plus questions to go further
        var deeper = new StringBuilder(standardText);
        deeper.Append("\n\n").Append(T("Going further", "Para profundizar", "Pour aller plus loin", "Per aprofundir")).Append('\n');
        foreach (var term in terms.Take(3))
        {
            deeper.Append("- ").Append(F(T("What would change if {0} did not exist or worked differently?", "¿Qué cambiaría si {0} no existiera o funcionara de otra forma?", "Que se passerait-il si {0} n'existait pas ou fonctionnait autrement ?", "Què canviaria si {0} no existís o funcionés d'una altra manera?"), term)).Append('\n');
        }

        if (terms.Count >= 2)
        {
            deeper.Append("- ").Append(F(T("How are {0} and {1} related? Find an example from everyday life.", "¿Qué relación hay entre {0} y {1}? Busca un ejemplo de la vida cotidiana.", "Quel lien y a-t-il entre {0} et {1} ? Trouve un exemple de la vie quotidienne.", "Quina relació hi ha entre {0} i {1}? Busca un exemple de la vida quotidiana."), terms[0], terms[1]));
        }

        // The few words around the term in the lesson (a real definition needs an AI engine)
        string Context(string term)
        {
            var sentence = sentences.FirstOrDefault(x => x.Contains(term, StringComparison.OrdinalIgnoreCase));
            if (sentence is null)
            {
                return "";
            }

            var words = sentence.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var i = Array.FindIndex(words, w => w.Contains(term, StringComparison.OrdinalIgnoreCase));
            var from = Math.Max(0, i - 6);
            var to = Math.Min(words.Length, i + 7);
            return (from > 0 ? "…" : "") + string.Join(' ', words[from..to]).TrimEnd(',', ';') + (to < words.Length ? "…" : "");
        }
        var glossary = terms.Select(t => new GlossaryEntry(t, T("In the lesson: ", "En la lección: ", "Dans la leçon : ", "A la lliçó: ") + "«" + Context(t) + "»")).ToList();

        // One question per sentence: each term with the first sentence not used yet
        var used = new HashSet<string>();
        var withTerm = new List<(string Term, string? Sentence)>();
        foreach (var t in terms)
        {
            var sentence = sentences.FirstOrDefault(s => s.Contains(t, StringComparison.OrdinalIgnoreCase) && !used.Contains(s));
            if (sentence is not null && used.Add(sentence))
            {
                withTerm.Add((t, sentence));
            }
        }
        var n = request.Questions;
        var basicQuestions = withTerm.Take(n).Select(x => new QuestionAnswer(
            T("Complete: ", "Completa: ", "Complète : ", "Completa: ") + Regex.Replace(x.Sentence!, Regex.Escape(x.Term), "_____", RegexOptions.IgnoreCase), x.Term)).ToList();
        var standardQuestions = withTerm.Take(n).Select(x => new QuestionAnswer(
            F(T("What does the lesson say about {0}?", "¿Qué dice la lección sobre {0}?", "Que dit la leçon sur {0} ?", "Què diu la lliçó sobre {0}?"), x.Term), x.Sentence!)).ToList();
        var advancedQuestions = withTerm.Take(n).Select(x => new QuestionAnswer(
            F(T("Why is {0} important in this topic? Argue with an example.", "¿Por qué es importante {0} en este tema? Argumenta con un ejemplo.", "Pourquoi {0} est-il important dans ce thème ? Argumente avec un exemple.", "Per què és important {0} en aquest tema? Argumenta-ho amb un exemple."), x.Term),
            T("Expected: uses the idea «", "Se espera: usa la idea «", "Attendu : utilise l'idée « ", "S'espera: fa servir la idea «") + x.Sentence + T("» and adds an example of their own.", "» y añade un ejemplo propio.", " » et ajoute un exemple personnel.", "» i hi afegeix un exemple propi."))).ToList();

        var versions = new List<LevelVersion>
        {
            new(LessonLevel.Basic, basicText, glossary, basicQuestions,
                T("In pairs, draw a simple diagram with the key words of the glossary and explain it to your partner in 3 sentences.", "Por parejas, dibujad un esquema sencillo con las palabras del glosario y explicádselo a vuestro compañero en 3 frases.", "En binôme, dessinez un schéma simple avec les mots du glossaire et expliquez-le à votre partenaire en 3 phrases.", "Per parelles, dibuixeu un esquema senzill amb les paraules del glossari i expliqueu-lo al company en 3 frases."),
                TextMeasure.Measure(basicText, l)),
            new(LessonLevel.Standard, standardText, glossary.Take(5).ToList(), standardQuestions,
                T("Write a summary of the lesson in 5 lines using at least three words of the glossary.", "Escribe un resumen de la lección en 5 líneas usando al menos tres palabras del glosario.", "Écris un résumé de la leçon en 5 lignes en utilisant au moins trois mots du glossaire.", "Escriu un resum de la lliçó en 5 línies fent servir almenys tres paraules del glossari."),
                TextMeasure.Measure(standardText, l)),
            new(LessonLevel.Advanced, deeper.ToString(), glossary.Take(4).ToList(), advancedQuestions,
                T("Prepare a 2-minute explanation for the class answering one of the «going further» questions, with a source.", "Prepara una explicación de 2 minutos para la clase respondiendo a una de las preguntas de «para profundizar», con una fuente.", "Prépare une explication de 2 minutes pour la classe en répondant à une question « pour aller plus loin », avec une source.", "Prepara una explicació de 2 minuts per a la classe responent una de les preguntes de «per aprofundir», amb una font."),
                TextMeasure.Measure(deeper.ToString(), l)),
        };

        var objectives = new List<string>();
        if (terms.Count > 0)
        {
            objectives.Add(F(T("Understand what {0} is.", "Comprender qué es {0}.", "Comprendre ce qu'est {0}.", "Comprendre què és {0}."), terms[0]));
        }

        if (terms.Count > 2)
        {
            objectives.Add(F(T("Explain {0} and {1} with your own words.", "Explicar {0} y {1} con sus propias palabras.", "Expliquer {0} et {1} avec ses propres mots.", "Explicar {0} i {1} amb les pròpies paraules."), terms[1], terms[2]));
        }

        objectives.Add(T("Apply the content of the lesson to an example.", "Aplicar el contenido de la lección a un ejemplo.", "Appliquer le contenu de la leçon à un exemple.", "Aplicar el contingut de la lliçó a un exemple."));

        var tips = new List<string>
        {
            T("Start together with the standard text (10 min), then split the class by level for the questions.", "Empezad juntos con el texto estándar (10 min) y después dividid la clase por niveles para las preguntas.", "Commencez ensemble avec le texte standard (10 min), puis répartissez la classe par niveau pour les questions.", "Comenceu junts amb el text estàndard (10 min) i després dividiu la classe per nivells per a les preguntes."),
            T("Pair a basic-level student with a standard-level one for the activity.", "Empareja a un alumno de nivel básico con uno de nivel estándar para la actividad.", "Associez un élève de niveau basique avec un élève de niveau standard pour l'activité.", "Aparella un alumne de nivell bàsic amb un de nivell estàndard per a l'activitat."),
            T("Close with the advanced group explaining their «going further» answer to everyone.", "Cerrad con el grupo avanzado explicando su respuesta de «para profundizar» a todos.", "Terminez avec le groupe avancé qui explique sa réponse « pour aller plus loin » à tous.", "Tanqueu amb el grup avançat explicant la seva resposta de «per aprofundir» a tothom."),
        };

        return Task.FromResult(new LessonResult(objectives, versions, tips, TextMeasure.Measure(text, l), LessonValidator.CheckLevels(versions)));
    }

    /// <summary>Removes asides and cuts a long sentence at ";" and before relative or causal clauses, which become sentences of their own.</summary>
    public static IEnumerable<string> Simplify(string sentence)
    {
        ArgumentNullException.ThrowIfNull(sentence);
        var s = Parentheses().Replace(sentence, "").Trim().TrimEnd('.', ';', ':', '!', '?', '…').Trim();
        if (TextMeasure.Words(s).Count <= BasicMaxWords)
        {
            yield return Finish(s);
            yield break;
        }

        foreach (var part in Cuts().Split(s).Select(p => p.Trim()).Where(p => p.Length > 0))
        {
            yield return Finish(Subject(part));
        }
    }

    /// <summary>"which they store" → "they store"; "which contain" → "These contain"; "que almacenan" → "almacenan" (Spanish and Catalan drop the subject).</summary>
    private static string Subject(string part)
    {
        var m = Relative().Match(part);
        if (!m.Success)
        {
            return part;
        }

        var rest = part[m.Length..].TrimStart();
        var words = TextMeasure.Words(rest);
        var first = words.Count > 0 ? words[0] : "";
        if (Pronouns.Contains(first) || m.Value.Trim().StartsWith("que", StringComparison.OrdinalIgnoreCase) || m.Value.StartsWith("qu'", StringComparison.OrdinalIgnoreCase) && rest.Length > 0)
        {
            return rest;
        }

        return m.Value.Trim().ToLowerInvariant() switch
        {
            "qui" => (first.EndsWith("nt", StringComparison.Ordinal) ? "Ils " : "Il ") + rest,
            "which" or "that" or "who" => (first.EndsWith('s') ? "This " : "These ") + rest,
            _ => rest,
        };
    }

    private static string Finish(string s) => s.Length == 0 ? s : char.ToUpper(s[0], CultureInfo.CurrentCulture) + s[1..] + ".";
}
