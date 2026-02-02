using UnityEngine;
using TMPro;
using UnityEngine.UI; // IMPORTANT: Avem nevoie de asta pentru a bloca butonul

public class EndConversationButton : MonoBehaviour
{
    [Header("References")]
    public LLMCompatibilityEvaluator llmEvaluator;
    public TMP_Text resultText;
    public AudioClip endSfx;

    [Header("Scoring")]
    [Range(0f, 1f)]
    public float alpha = 0.2f;

    public void EndConversation()
    {
        Debug.Log("🛑 BUTON APASAT: Incepem verificarea...");

        // 1. BLOCARE BUTON (Sa nu poti apasa de 2 ori si sa strici logica)
        var btn = GetComponent<Button>();
        if (btn != null) btn.interactable = false;

        PlaySfx(endSfx);

        var manager = ProfessorManager.Instance;

        // 2. Verificari de siguranta
        if (manager == null || manager.activeChat == null)
        {
            Debug.Log("⚡ EROARE: Nu exista chat activ. Fortez iesirea.");
            FortareFinal(0);
            return;
        }

        ConversationLog log = manager.activeChat.GetConversationLog();

        // 3. SCURTCIRCUITUL: Daca nu ai vorbit -> Nota 0 Instant
        if (log == null || log.studentMessages.Count == 0)
        {
            Debug.Log("⚡ LISTA GOALA: Nu ai vorbit. Execut finalul instant!");

            if (resultText) resultText.text = "Tăcerea e de aur... dar nu aici.\nNotă: 0.";

            FortareFinal(0); // Trimitem 0 la GameManager

            manager.activeChat.EndConversation();
            return;
        }

        // ---------------------------------------------------------
        // 4. Daca ai vorbit -> Calculam scorul normal
        Debug.Log("DATA: Ai vorbit. Chem AI-ul...");

        if (resultText) resultText.text = "Evaluez...";

        var keywordResult = CompatibilityScorer.Compute(log, manager.activeProfessor);
        string studentText = string.Join(" ", log.studentMessages);

        if (llmEvaluator != null)
        {
            StartCoroutine(llmEvaluator.Evaluate(studentText, manager.activeProfessor.domain, (llmScore, llmReason) =>
            {
                int finalScore = Mathf.RoundToInt(alpha * keywordResult.score + (1f - alpha) * llmScore);

                if (resultText) resultText.text = $"Scor: {finalScore}/100\n{llmReason}";

                FortareFinal(finalScore); // Trimitem scorul la GameManager

                log.Clear();
                manager.activeChat.EndConversation();
            }));
        }
        else
        {
            // Fallback daca nu merge AI-ul
            FortareFinal(keywordResult.score);
        }
    }

    // --- AICI ERA GRESEALA ---
    // Acum suna la cine trebuie (GameManager)
    void FortareFinal(int scor)
    {
        // VERIFICAM DACA EXISTA GAME MANAGER (CEL BUN)
        if (GameManager.Instance != null)
        {
            Debug.Log($"📞 Sun la GameManager cu scorul: {scor}");
            // Apelam functia din scriptul Hibrid pe care l-am facut adineauri
            GameManager.Instance.AmTerminatCuUnProfesor(scor);
        }
        else
        {
            Debug.LogError("❌ CRITIC: Nu gasesc GameManager in scena! (Nu GameFlowManager, ala e sters!)");
        }
    }

    void PlaySfx(AudioClip clip)
    {
        var audio = AudioManager.EnsureExists();
        if (clip != null) audio.PlayOneShot(clip);
    }
}