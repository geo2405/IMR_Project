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

    [Header("Conexiune LLM")]
    public string serverIP = "127.0.0.1";
    public int port = 1234;
    public string model = "local-model";

    private string endpoint = "/v1/chat/completions";

    // Conectare la clasa existenta in proiect
    private ConversationLog conversationLog = new ConversationLog();

    // =========================
    // 1. INPUT 
    // =========================

    public void IntrebareDeLaVoce(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        Debug.Log("🎤 Voce: " + text);
        if (inputField != null) inputField.text = "Student: " + text;

        StartCoroutine(ProcessFlow(text));
    }

    public void SendQuestion()
    {
        if (inputField == null) return;
        string text = inputField.text.Trim();
        if (string.IsNullOrEmpty(text)) return;

        inputField.text = "Student: " + text;
        StartCoroutine(ProcessFlow(text));
    }

    // =========================
    // 2. LOGICA PREMIUM (REPARATĂ: FĂRĂ TIMEOUT)
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
        yield return new WaitForSeconds(5.0f);

        // Dacă serverul a terminat deja (pc rachetă), nu mai scriem "Imediat", afișăm direct
        if (!requestComplete)
        {
            if (outputText != null) outputText.text = "✋ Vă răspund imediat...";
        }

        // 3. Mai așteptăm încă 2 secunde
        yield return new WaitForSeconds(10.0f);

        // Dacă tot nu a terminat...
        if (!requestComplete)
        {
            if (outputText != null) outputText.text = "💡 Așadar...";
        }

        // 4. AȘTEPTARE REALĂ (AICI ERA PROBLEMA!)
        // Acum așteptăm oricât e nevoie. Nu mai există limită de timp artificială.
        while (!requestComplete)
        {
            yield return null; // Stăm aici până termină LM Studio, fie și 2 minute.
        }

        // 5. Afișăm rezultatul
        if (requestError || string.IsNullOrEmpty(finalJson))
        {
            if (outputText != null) outputText.text = "Eroare: LM Studio nu a răspuns sau s-a întrerupt conexiunea.";
        }
        else
        {
            string answer = ExtractOutputText(finalJson);

            if (conversationLog.professorMessages != null) conversationLog.professorMessages.Add(answer);

            StartCoroutine(TypewriterEffect(answer));
        }
    }

    // =========================
    // 3. CONEXIUNEA ROBUSTĂ
    // =========================

    IEnumerator MakeRequest(string url, string question, System.Action<string> callback)
    {
        ProfessorProfile prof = null;
        if (ProfessorManager.Instance != null) prof = ProfessorManager.Instance.activeProfessor;
        if (prof == null) prof = GetComponent<ProfessorProfile>();

        string systemPrompt = prof != null ? prof.systemPrompt : "Ești un profesor.";

        // JSON Body
        string jsonBody = "{ \"model\": \"" + model + "\", \"messages\": [ {\"role\": \"system\", \"content\": \"" + EscapeJson(systemPrompt) + "\"}, {\"role\": \"user\", \"content\": \"" + EscapeJson(question) + "\"} ], \"temperature\": 0.7, \"max_tokens\": 300, \"stream\": false }";

        using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");


            // TIMEOUT MĂRIT LA 5 MINUTE (300 secunde)
            // Local LLM poate fi lent, îi dăm timp să respire.
            www.timeout = 300;

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
    // 4. EFECTE VIZUALE
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

        if (GameManager.Instance != null)
            GameManager.Instance.CollectSignature(professorID);
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
}