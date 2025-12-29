using UnityEngine;
using TMPro;

public class EndConversationButton : MonoBehaviour
{
    [Header("References")]
    public LLMCompatibilityEvaluator llmEvaluator;
    public TMP_Text resultText;

    [Header("Scoring")]
    [Range(0f, 1f)]
    public float alpha = 0.2f; // pondere keywords

    public void EndConversation()
    {
        Debug.Log("END CONVERSATION APASAT");

        var manager = ProfessorManager.Instance;

        if (manager == null ||
            manager.activeProfessor == null ||
            manager.activeChat == null)
        {
            resultText.text = "Nu există conversație activă.";
            return;
        }

        if (llmEvaluator == null)
        {
            resultText.text = "Evaluator semantic indisponibil.";
            Debug.LogError("LLMCompatibilityEvaluator NU este setat!");
            return;
        }

        ConversationLog log = manager.activeChat.GetConversationLog();

        if (log.studentMessages.Count == 0)
        {
            resultText.text = "Nu există conversație activă.";
            return;
        }

        // 1️⃣ Scor determinist (keywords)
        var keywordResult =
            CompatibilityScorer.Compute(log, manager.activeProfessor);

        string studentText =
            string.Join(" ", log.studentMessages);

        resultText.text = "Evaluez compatibilitatea...";

        // 2️⃣ Scor semantic (LLM)
        StartCoroutine(
            llmEvaluator.Evaluate(
                studentText,
                manager.activeProfessor.domain,
                (llmScore, llmReason) =>
                {
                    int finalScore = Mathf.RoundToInt(
                        alpha * keywordResult.score +
                        (1f - alpha) * llmScore
                    );

                    resultText.text =
                        $"Compatibilitate: {finalScore}/100\n\n" +
                        $"Analiză interese: {keywordResult.explanation}\n" +
                        $"Analiză semantică: {llmReason}";

                    // 3️⃣ Reset conversație
                    log.Clear();
                }
            )
        );
    }
}
