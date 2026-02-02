using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using System.Text.RegularExpressions;

public class ProfessorChat : MonoBehaviour
{
    [Header("Identitate Profesor")]
    public string professorID = "Prof_TW";

    [Header("UI References")]
    public TMP_InputField inputField;
    public TMP_Text outputText;

    [Header("Setări Estetice")]
    public float vitezaScriere = 0.04f;

    [Header("Audio")]
    public AudioClip sendSfx;
    public AudioClip receiveSfx;
    public AudioClip errorSfx;

    [Header("Conexiune LLM")]
    public string serverIP = "127.0.0.1";
    public int port = 1234;
    public string model = "local-model";

    private string endpoint = "/v1/chat/completions";

    // Conectare la clasa existenta in proiect
    private ConversationLog conversationLog = new ConversationLog();
    private ProfessorAnimationController animController;

    // --- AICI LIPSEA DECLARAȚIA ---
    private ProfessorProfile currentProfile;
    // -----------------------------

    void Awake()
    {
        ResolveAnimator();
    }

    // --- AM ADĂUGAT START CA SĂ GĂSIM PROFILUL ---
    void Start()
    {
        // Încercăm să găsim profilul pe acest obiect
        currentProfile = GetComponent<ProfessorProfile>();

        // Dacă nu e pe obiect, poate e în manager (fallback)
        if (currentProfile == null && ProfessorManager.Instance != null)
        {
            currentProfile = ProfessorManager.Instance.activeProfessor;
        }
    }
    // ---------------------------------------------

    // =========================
    // 1. INPUT 
    // =========================

    public void IntrebareDeLaVoce(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        Debug.Log("🎤 Voce: " + text);
        if (inputField != null) inputField.text = "Student: " + text;

        SetTalking(true);
        PlaySfx(sendSfx);
        StartCoroutine(ProcessFlow(text));
    }

    public void SendQuestion()
    {
        if (inputField == null) return;
        string text = inputField.text.Trim();
        if (string.IsNullOrEmpty(text)) return;

        inputField.text = "Student: " + text;
        SetTalking(true);
        PlaySfx(sendSfx);
        StartCoroutine(ProcessFlow(text));
    }

    // =========================
    // 2. LOGICA PREMIUM
    // =========================

    IEnumerator ProcessFlow(string question)
    {
        // 0. Salvăm întrebarea
        if (conversationLog.studentMessages != null) conversationLog.studentMessages.Add(question);

        // 1. Status Imediat
        if (outputText != null) outputText.text = "🤔 Mă gândesc...";

        // Lansăm cererea
        string apiUrl = $"http://{serverIP}:{port}{endpoint}";
        string cleanQuestion = question.Replace("\\", "").Replace("\"", "'");
        string finalJson = "";
        bool requestComplete = false;
        bool requestError = false;

        StartCoroutine(MakeRequest(apiUrl, cleanQuestion, (response) => {
            if (response == null) requestError = true;
            else finalJson = response;
            requestComplete = true;
        }));

        // 2. Așteptăm fix 3 secunde (de frumusețe)
        yield return new WaitForSeconds(3.0f);

        if (!requestComplete)
        {
            if (outputText != null) outputText.text = "✋ Vă răspund imediat...";
        }

        yield return new WaitForSeconds(5.0f);

        if (!requestComplete)
        {
            if (outputText != null) outputText.text = "💡 Așadar...";
        }

        // 4. AȘTEPTARE REALĂ
        while (!requestComplete)
        {
            yield return null;
        }

        // 5. Afișăm rezultatul
        if (requestError || string.IsNullOrEmpty(finalJson))
        {
            if (outputText != null) outputText.text = "Eroare: LM Studio nu a răspuns.";
            PlaySfx(errorSfx, true);
        }
        else
        {
            string answer = ExtractOutputText(finalJson);

            if (conversationLog.professorMessages != null) conversationLog.professorMessages.Add(answer);

            PlaySfx(receiveSfx);
            StartCoroutine(TypewriterEffect(answer));
        }
    }

    // =========================
    // 3. CONEXIUNEA ROBUSTĂ
    // =========================

    IEnumerator MakeRequest(string url, string question, System.Action<string> callback)
    {
        // Folosim variabila cached currentProfile
        string systemPrompt = currentProfile != null ? currentProfile.systemPrompt : "Ești un profesor.";

        // JSON Body
        string jsonBody = "{ \"model\": \"" + model + "\", \"messages\": [ {\"role\": \"system\", \"content\": \"" + EscapeJson(systemPrompt) + "\"}, {\"role\": \"user\", \"content\": \"" + EscapeJson(question) + "\"} ], \"temperature\": 0.7, \"max_tokens\": 300, \"stream\": false }";

        using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            www.timeout = 300; // 5 minute

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                callback(www.downloadHandler.text);
            }
            else
            {
                Debug.LogError("Eroare Web: " + www.error);
                callback(null);
            }
        }
    }

    // =========================
    // 4. EFECTE VIZUALE & FINALIZARE
    // =========================

    IEnumerator TypewriterEffect(string fullText)
    {
        if (outputText == null) yield break;

        outputText.text = "";

        foreach (char letter in fullText)
        {
            outputText.text += letter;
            yield return new WaitForSeconds(vitezaScriere);
        }
    }

    public void EndConversation()
    {
        if (outputText != null) outputText.text = "";
        if (inputField != null) inputField.text = "";

        // Verificăm dacă avem profil pentru calcul
        if (currentProfile != null)
        {
            // 1. Calculăm scorul folosind scriptul static (CompatibilityScorer)
            // Acesta returnează un tuplu: (int score, string explanation)
            var rezultat = CompatibilityScorer.Compute(conversationLog, currentProfile);

            int scorFinal = rezultat.score;
            string explicatieFinala = rezultat.explanation;

            Debug.Log($"📊 RAPORT FINAL: {scorFinal}% - {explicatieFinala}");

            // 2. Salvăm în GameManager (pentru finalul jocului/fantomă)
            if (GameManager.Instance != null)
            {
                GameManager.Instance.InregistreazaScor(scorFinal, professorID);
                GameManager.Instance.CollectSignature(professorID);
            }

            // 3. AFIȘĂM PANOUL GALBEN (Dacă există)
            if (ScoreDisplay.Instance != null)
            {
                // Aici trimitem tot pachetul
                ScoreDisplay.Instance.ArataScorComplet(scorFinal, explicatieFinala);
            }
            else
            {
                Debug.LogError("❌ NU GASESC ScoreDisplay în scenă! Asigură-te că Canvas_Scor este activ.");
            }
        }
        else
        {
            Debug.LogWarning("⚠️ Nu am găsit profilul profesorului, nu pot calcula compatibilitatea.");
            // Fallback: dăm doar semnătura
            if (GameManager.Instance != null) GameManager.Instance.CollectSignature(professorID);
        }

        SetTalking(false);
        AudioManager.EnsureExists().ResumeAmbient();
    }

    // =========================
    // 5. COMPATIBILITATE
    // =========================

    public ConversationLog GetConversationLog()
    {
        return conversationLog;
    }

    public void ResetConversation()
    {
        if (conversationLog != null) conversationLog.Clear();
        SetTalking(false);
    }

    public void SetConversationActive(bool active)
    {
        SetTalking(active);
    }

    // =========================
    // UTILITARE
    // =========================

    string EscapeJson(string txt) { return txt.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", ""); }

    string ExtractOutputText(string json)
    {
        if (string.IsNullOrEmpty(json)) return "";
        string key = "\"content\": \"";
        int start = json.LastIndexOf(key);
        if (start == -1) return "";
        start += key.Length;
        int end = start;
        bool inEscape = false;
        for (int i = start; i < json.Length; i++)
        {
            if (inEscape) { inEscape = false; continue; }
            if (json[i] == '\\') { inEscape = true; continue; }
            if (json[i] == '"') { end = i; break; }
        }
        string raw = json.Substring(start, end - start);
        string unescaped = Regex.Unescape(raw);
        return Regex.Replace(unescaped, @"[^\u0000-\uFFFF]", "");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return)) SendQuestion();
    }

    void PlaySfx(AudioClip clip, bool isError = false)
    {
        var audio = AudioManager.EnsureExists();
        var fallback = isError ? audio.errorSfx : audio.defaultSfx;
        var chosen = clip ?? fallback;
        if (chosen == null) return;
        audio.PlayOneShot(chosen);
    }

    void ResolveAnimator()
    {
        if (animController != null) return;

        animController = GetComponentInParent<ProfessorAnimationController>();
        if (animController == null) animController = GetComponentInChildren<ProfessorAnimationController>();
        if (animController == null) animController = GetComponent<ProfessorAnimationController>();

        if (animController == null)
        {
            var animator = GetComponentInChildren<Animator>();
            if (animator != null)
            {
                animController = animator.GetComponent<ProfessorAnimationController>();
                if (animController == null)
                    animController = animator.gameObject.AddComponent<ProfessorAnimationController>();
                animController.SetAnimator(animator);
            }
        }
    }

    void SetTalking(bool talking)
    {
        ResolveAnimator();
        if (animController != null) animController.SetTalking(talking);
    }
}