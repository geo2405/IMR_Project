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
            // outputText.text += $"\n🧑‍🎓 {question}\n";

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

        foreach (string endpoint in endpoints)
        {
            if (success) break; // Daca a mers deja, nu mai incercam alt endpoint

            string apiUrl = baseUrl + endpoint;
            string jsonBody;

            // Construim JSON-ul in functie de endpoint
            if (endpoint.Contains("responses"))
            {
                // Format vechi LM Studio
                jsonBody = JsonUtility.ToJson(new InputRequest
                {
                    model = model,
                    input = $"Ești un profesor prietenos. Răspunde scurt (max 2 fraze). Întrebare: {question}",
                    max_tokens = 150
                });
            }
            else
            {
                // Format standard OpenAI / LM Studio (Chat Completions)
                // Escapam ghilimelele din intrebare ca sa nu strice JSON-ul
                string cleanQuestion = question.Replace("\"", "'");

                jsonBody = "{";
                jsonBody += $"\"model\": \"{model}\",";
                jsonBody += "\"messages\": [";
                jsonBody += "{\"role\": \"system\", \"content\": \"Ești un profesor universitar prietenos. Răspunde scurt, concis, în limba română (maximum 30 de cuvinte).\"},";
                jsonBody += $"{{\"role\": \"user\", \"content\": \"{cleanQuestion}\"}}";
                jsonBody += "],";
                jsonBody += "\"temperature\": 0.7,";
                jsonBody += "\"max_tokens\": 150";
                jsonBody += "}";
            }

            using (UnityWebRequest www = new UnityWebRequest(apiUrl, "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
                www.uploadHandler = new UploadHandlerRaw(bodyRaw);
                www.downloadHandler = new DownloadHandlerBuffer();
                www.SetRequestHeader("Content-Type", "application/json");
                www.timeout = Mathf.RoundToInt(requestTimeout);

                Debug.Log($"📡 Trimit către {apiUrl}...");
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    string json = www.downloadHandler.text;
                    Debug.Log($"✅ Răspuns primit:\n{json}");

                    string answer = ExtractOutputText(json);

                    // Curatam raspunsul de eventuale caractere ciudate
                    answer = answer.Replace("\\n", "\n").Trim();

                    outputText.text = answer; // Inlocuim textul vechi cu raspunsul nou
                    success = true;
                }
                else
                {
                    Debug.LogWarning($"⚠️ Acel endpoint nu a mers: {www.error}. Incerc urmatorul...");
                }
            }
        }

        if (!success)
        {
            outputText.text = "❌ Eroare: Nu am putut contacta serverul AI. Verifica daca LM Studio e pornit pe portul 1234.";
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