using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

public class ProfessorChat : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField inputField;
    public TMP_Text outputText;

    [Header("LM Studio Settings")]
    public string serverIP = "127.0.0.1";
    public int port = 1234;
    public string model = "local-model";

    private float requestTimeout = 60f;
    private string endpoint = "/v1/chat/completions";

    private ConversationLog conversationLog = new ConversationLog();

    // =========================
    // UI ENTRY POINTS
    // =========================

    public void SendQuestion()
    {
        string question = inputField.text.Trim();
        if (string.IsNullOrEmpty(question)) return;
        conversationLog.studentMessages.Add(question);
        inputField.text = "";
        StartCoroutine(SendToLLM(question));

        Debug.Log($"[CHAT] {gameObject.name} - mesaj adăugat");
    }

    public void IntrebareDeLaVoce(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        inputField.text = text;
        SendQuestion();
    }

    // =========================
    // LLM REQUEST
    // =========================

    IEnumerator SendToLLM(string question)
    {
        string apiUrl = $"http://{serverIP}:{port}{endpoint}";

        // 1️⃣ Luăm profesorul activ
        ProfessorProfile prof = null;
        if (ProfessorManager.Instance != null)
            prof = ProfessorManager.Instance.activeProfessor;

        string systemPrompt =
            prof != null
            ? prof.systemPrompt
            : "Ești un profesor universitar prietenos.";

        // DEBUG CRITIC
        Debug.Log("===== SYSTEM PROMPT TRIMIS =====");
        Debug.Log(systemPrompt);

        // 2️⃣ Curățăm input-ul userului
        string cleanQuestion = question.Replace("\\", "").Replace("\"", "'");

        // 3️⃣ Construim requestul CORECT (fără JSON manual)
        ChatRequest requestData = new ChatRequest
        {
            model = model,
            messages = new ChatMessage[]
            {
                new ChatMessage
                {
                    role = "system",
                    content = systemPrompt
                },
                new ChatMessage
                {
                    role = "user",
                    content = cleanQuestion
                }
            },
            temperature = 0.2f,
            max_tokens = 150,
            stream = false
        };

        string jsonBody = JsonUtility.ToJson(requestData);

        using (UnityWebRequest www = new UnityWebRequest(apiUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            www.timeout = Mathf.RoundToInt(requestTimeout);

            Debug.Log($"📡 Trimit către {apiUrl}");
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string json = www.downloadHandler.text;
                Debug.Log("✅ Răspuns LLM:\n" + json);

                string answer = ExtractOutputText(json);
                conversationLog.professorMessages.Add(answer);
                outputText.text = answer;
            }
            else
            {
                Debug.LogError("❌ Eroare LLM: " + www.error);
                outputText.text = "Eroare: nu am putut contacta profesorul.";
            }
        }


        
    }

    // =========================
    // RESPONSE PARSING
    // =========================

    string ExtractOutputText(string json)
    {
        string key = "\"content\": \"";
        int start = json.LastIndexOf(key);
        if (start == -1) return "Eroare la citirea răspunsului.";

        start += key.Length;
        int end = json.IndexOf("\"", start);
        if (end == -1) return "Eroare la citirea răspunsului.";

        return json.Substring(start, end - start)
                   .Replace("\\n", "\n")
                   .Trim();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
            SendQuestion();
    }

    // =========================
    // JSON STRUCTS (FOARTE IMPORTANT)
    // =========================

    [System.Serializable]
    public class ChatRequest
    {
        public string model;
        public ChatMessage[] messages;
        public float temperature;
        public int max_tokens;
        public bool stream;
    }

    [System.Serializable]
    public class ChatMessage
    {
        public string role;
        public string content;
    }

    public ConversationLog GetConversationLog()
{
    return conversationLog;
}

public void ResetConversation()
{
    conversationLog.Clear();
}

}
