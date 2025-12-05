using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using System.Text.RegularExpressions;

public class DiscutieEric : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField inputField;
    public TMP_Text outputText;

    [Header("Server Settings")]
    // AM MODIFICAT AICI: 127.0.0.1 inseamna "Acest Calculator". 
    // Merge la oricine ruleaza LM Studio local.
    public string serverIP = "127.0.0.1";
    public int port = 1234;

    // AM MODIFICAT AICI: "local-model" este universal pentru LM Studio
    public string model = "local-model";

    private float requestTimeout = 60f; // Am marit putin timpul (unele laptopuri sunt mai lente)

    private string[] endpoints = { "/v1/chat/completions", "/v1/responses" }; // Am inversat ordinea (chat e mai comun)

    public void SendQuestion()
    {
        string question = inputField.text.Trim();
        if (!string.IsNullOrEmpty(question))
        {
            // Optional: Afisam intrebarea ta in chat
            outputText.text += $"\nTu: {question}\n";

            StartCoroutine(SendToLLM(question));
            inputField.text = "";
        }
    }

    // Am adaugat o functie publica ca sa o poti apela si din alte scripturi (ex: GoogleVoice)
    public void IntrebareDeLaVoce(string text)
    {
        if (!string.IsNullOrEmpty(text))
        {
            inputField.text = text; // O punem in casuta vizual
            SendQuestion(); // O trimitem
        }
    }

    IEnumerator SendToLLM(string question)
    {
        string baseUrl = $"http://{serverIP}:{port}";
        bool success = false;

        // MARIM TIMPUL DE ASTEPTARE (Critic pentru 500 tokens!)
        int timeOutSecunde = 180;

        foreach (string endpoint in endpoints)
        {
            if (success) break;

            string apiUrl = baseUrl + endpoint;
            string jsonBody;

            // Curatam intrebarea ta de caractere care strica JSON-ul
            string cleanQuestion = question.Replace("\"", "'").Replace("\\", "");

            if (endpoint.Contains("responses"))
            {
                // Format vechi LM Studio
                jsonBody = JsonUtility.ToJson(new InputRequest
                {
                    model = model,
                    input = $"Esti un profesor. Raspunde in romana, clar si simplu. Fara formule, fara simboluri ciudate. Intrebare: {cleanQuestion}",
                    max_tokens = 500
                });
            }
            else
            {
                // Format Chat Completions (Cel mai bun pentru Llama 3)
                jsonBody = "{";
                jsonBody += $"\"model\": \"{model}\",";
                jsonBody += "\"messages\": [";
                // AICI E SECRETUL: Ii interzicem sa foloseasca backslash sau latex
                jsonBody += "{\"role\": \"system\", \"content\": \"Ești un profesor universitar prietenos. Răspunde în limba română. Răspunsul tău trebuie să fie DOAR TEXT SIMPLU. Nu folosi markdown, nu folosi LaTeX, nu folosi backslash (\\\\). Fii concis.\"},";
                jsonBody += $"{{\"role\": \"user\", \"content\": \"{cleanQuestion}\"}}";
                jsonBody += "],";
                jsonBody += "\"temperature\": 0.5,"; // Mai putin creativ = mai putine erori
                jsonBody += "\"max_tokens\": 500,";
                jsonBody += "\"stream\": false"; // Asiguram ca nu e stream
                jsonBody += "}";
            }

            using (UnityWebRequest www = new UnityWebRequest(apiUrl, "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
                www.uploadHandler = new UploadHandlerRaw(bodyRaw);
                www.downloadHandler = new DownloadHandlerBuffer();
                www.SetRequestHeader("Content-Type", "application/json");
                www.timeout = timeOutSecunde; // Setam timeout-ul marit

                Debug.Log($"📡 Trimit către {apiUrl} (Asteapta {timeOutSecunde}s)...");
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    string json = www.downloadHandler.text;
                    Debug.Log($"✅ Răspuns primit:\n{json}");

                    string answer = ExtractOutputText(json);

                    // Curatenie finala: Scoatem orice backslash a mai scapat
                    answer = answer.Replace("\\", "").Trim();

                    // Adaugam raspunsul in chat
                    outputText.text += $"\n👨‍🏫 ERIC: {answer}\n----------------\n";
                    success = true;
                }
                else
                {
                    Debug.LogWarning($"⚠️ Eroare: {www.error}. Detalii: {www.downloadHandler.text}");
                }
            }
        }

        if (!success)
        {
            outputText.text += "\n❌ Eroare: Serverul nu a răspuns la timp sau este oprit.\n";
        }
    }

    // Functie de extragere a textului (Parsare manuala ca sa nu depindem de clase complexe)
    string ExtractOutputText(string json)
    {
        // 1. Incercam formatul Chat Completions (choices -> message -> content)
        string cautaContent = "\"content\": \"";
        int start = json.LastIndexOf(cautaContent); // Cautam ultimul "content" (de obicei e cel al asistentului)

        if (start != -1)
        {
            start += cautaContent.Length;
            int end = json.IndexOf("\"", start);

            // Verificam sa nu fie un "content" fals
            if (end != -1) return json.Substring(start, end - start);
        }

        // 2. Fallback pentru formatul vechi "text"
        // ... (implementare simplificata)

        return "Nu am putut citi raspunsul, dar serverul a raspuns.";
    }

    [System.Serializable]
    public class InputRequest
    {
        public string model;
        public string input;
        public int max_tokens;
    }

    void Update()
    {
        // Trimite la Enter
        if (Input.GetKeyDown(KeyCode.Return))
            SendQuestion();
    }
}