using System.Globalization;
using EduVerse.Core.Common;

namespace EduVerse.Core.Catalog;

/// <summary>
/// Turns a question's one-line <see cref="QuizQuestion.Explanation"/> into something that actually says why the
/// answer was right or wrong: on a miss, the correct answer is named (it never was before); on either outcome, the
/// framing is read for the student's stage (shorter and warmer for primary school, more analytical from high school
/// up). The explanation's own content never changes — code only decides how it is introduced, so this needs no AI
/// and keeps working in offline demo mode.
/// </summary>
public static class AnswerExplainer
{
    public static string Explain(QuizQuestion question, bool correct, Level level, TeachLanguage language)
    {
        ArgumentNullException.ThrowIfNull(question);
        var stage = Levels.ToStage(level);
        var why = Decapitalize(question.Explanation);

        if (correct)
        {
            var lead = stage switch
            {
                Stage.Primary => Text.Of("Yes! {0}|¡Sí! {0}|Oui ! {0}|Sí! {0}"),
                Stage.Secondary or Stage.HighSchool => Text.Of("Right, because {0}|Correcto, porque {0}|Correct, car {0}|Correcte, perquè {0}"),
                _ => Text.Of("Correct. The reasoning: {0}|Correcto. El razonamiento: {0}|Correct. Le raisonnement : {0}|Correcte. El raonament: {0}"),
            };
            return lead.Format(language, why);
        }

        var leadWrong = stage switch
        {
            Stage.Primary => Text.Of("Not this time. The right answer is {0}, because {1}|Esta vez no. La respuesta correcta es {0}, porque {1}|Pas cette fois. La bonne réponse est {0}, car {1}|Aquesta vegada no. La resposta correcta és {0}, perquè {1}"),
            Stage.Secondary or Stage.HighSchool => Text.Of("Not quite. The correct answer is {0}, because {1}|No del todo. La respuesta correcta es {0}, porque {1}|Pas tout à fait. La bonne réponse est {0}, car {1}|No del tot. La resposta correcta és {0}, perquè {1}"),
            _ => Text.Of("Not correct — the right answer is {0}. The reasoning: {1}|No es correcto: la respuesta correcta es {0}. El razonamiento: {1}|Incorrect : la bonne réponse est {0}. Le raisonnement : {1}|No és correcte: la resposta correcta és {0}. El raonament: {1}"),
        };
        return leadWrong.Format(language, question.Answer, why);
    }

    private static string Decapitalize(string text) =>
        text.Length == 0 ? text : char.ToLower(text[0], CultureInfo.InvariantCulture) + text[1..];
}
