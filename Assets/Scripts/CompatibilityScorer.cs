using UnityEngine;
using System.Text;
using System.Text.RegularExpressions;
public static class CompatibilityScorer
{
    // Cuvinte de negare simple (extensibile)
    static readonly string[] negations =
    {
        "nu",
        "nu vreau",
        "nu ma intereseaza",
        "fara",
        "evit"
    };

    public static (int score, string explanation) Compute(
        ConversationLog log,
        ProfessorProfile prof)
    {
        // 1️⃣ Analizăm DOAR mesajele studentului
        string studentText =
            string.Join(" ", log.studentMessages).ToLower();

        int score = 0;
        int matchedKeywords = 0;
        StringBuilder explanation = new();

        foreach (string kw in prof.keywords)
        {
            string keyword = kw.ToLower();

            if (ContainsWholeWord(studentText, keyword) &&
            !IsNegated(studentText, keyword))
            {
                matchedKeywords++;
                score += 10;
                explanation.Append($"Interes pentru {kw}. ");
            }
        }

        // 2️⃣ Normalizare scor (0–100)
        score = Mathf.Clamp(score, 0, 100);

        // 3️⃣ Explicație semantică (fără hack-uri)
        if (matchedKeywords == 0)
        {
            explanation.Append(
                $"Interesele tale nu se aliniază cu domeniul {prof.domain}."
            );
        }
        else if (matchedKeywords <= 2)
        {
            explanation.Append("Compatibilitate moderată.");
        }
        else
        {
            explanation.Append("Compatibilitate ridicată.");
        }

        return (score, explanation.ToString());
    }

    // =========================
    // HELPER: detectare negație
    // =========================
    static bool IsNegated(string text, string keyword)
    {
        foreach (string neg in negations)
        {
            if (text.Contains(neg + " " + keyword))
                return true;
        }
        return false;
    }
    static bool ContainsWholeWord(string text, string keyword)
{
    // \b = word boundary
    string pattern = $@"\b{Regex.Escape(keyword)}\b";
    return Regex.IsMatch(text, pattern);
}
}
