using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using System.Text.RegularExpressions;

public class ProfessorChat : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField inputField;
    public TMP_Text outputText;

    [Header("Server Settings")]
    public string serverIP = "192.168.0.105"; // IP-ul PC-ului
    public int port = 1234;
    public string model = "openai/gpt-oss-20b";
    private float requestTimeout = 20f;

    private string[] endpoints = { "/v1/responses", "/v1/chat/completions" };

    public void SendQuestion()
    {
        string question = inputField.text.Trim();
        if (!string.IsNullOrEmpty(question))
        {
            //outputText.text += $"\n🧑‍🎓 {question}\n";
            StartCoroutine(SendToLLM(question));
            inputField.text = "";
        }
    }

    IEnumerator SendToLLM(string question)
    {
        string baseUrl = $"http://{serverIP}:{port}";
        bool success = false;

        foreach (string endpoint in endpoints)
        {
            string apiUrl = baseUrl + endpoint;
            string jsonBody;

            if (endpoint.Contains("responses"))
            {
                jsonBody = JsonUtility.ToJson(new InputRequest
                {
                    model = model,
                    input = $"Ești un profesor prietenos din facultate. Răspunde foarte scurt, maximum două propoziții. Întrebarea studentului: {question}",
                    max_tokens = 150
                });
            }
            else
            {
                jsonBody = "{\"model\": \"" + model + "\", \"max_tokens\": 150, \"messages\": [" +
                           "{\"role\": \"system\", \"content\": \"Ești un profesor prietenos din facultate. Răspunde foarte scurt, maximum două propoziții.\"}," +
                           "{\"role\": \"user\", \"content\": \"" + question + "\"}]}";
            }

            using (UnityWebRequest www = new UnityWebRequest(apiUrl, "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
                www.uploadHandler = new UploadHandlerRaw(bodyRaw);
                www.downloadHandler = new DownloadHandlerBuffer();
                www.SetRequestHeader("Content-Type", "application/json");
                www.SetRequestHeader("Accept", "application/json");
                www.timeout = Mathf.RoundToInt(requestTimeout);

                Debug.Log($"📡 Trimit către {apiUrl}: {question}");
                yield return www.SendWebRequest();

                Debug.Log($"🔍 Cod răspuns: {www.responseCode}");

                if (www.result == UnityWebRequest.Result.Success)
                {
                    string json = www.downloadHandler.text;
                    Debug.Log($"✅ Răspuns complet:\n{json}");
                    success = true;

                    string answer = ExtractOutputText(json);
                    outputText.text += $"\n👨‍🏫 {answer}\n";
                    break;
                }
                else
                {
                    Debug.LogWarning($"⚠️ Eroare la {apiUrl}: {www.error}");
                }
            }
        }

        if (!success)
            outputText.text += "\n❌ Nu am reușit să obțin un răspuns de la model.\n";
    }

    // ✅ Parsare robustă pentru formatul LM Studio (v1/responses)
    string ExtractOutputText(string json)
{
    try
    {
        // Caută DOAR textul din secțiunea "output_text"
        var match = Regex.Match(json, "\"type\"\\s*:\\s*\"output_text\"\\s*,\\s*\"text\"\\s*:\\s*\"([^\"]+)\"");
        if (match.Success)
            return match.Groups[1].Value;

        // Fallback – OpenAI-style
        var jsonObj = JsonUtility.FromJson<ResponseWrapper>(json);
        if (jsonObj != null && jsonObj.choices != null && jsonObj.choices.Length > 0)
            return jsonObj.choices[0].message.content;

        return "⚠️ Nu am putut interpreta răspunsul.";
    }
    catch
    {
        return "⚠️ Eroare la parsare.";
    }
}

    [System.Serializable]
    public class InputRequest
    {
        public string model;
        public string input;
        public int max_tokens;
    }

    [System.Serializable]
    public class ResponseWrapper
    {
        public Choice[] choices;
    }

    [System.Serializable]
    public class Choice
    {
        public Message message;
    }

    [System.Serializable]
    public class Message
    {
        public string content;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
            SendQuestion();
    }
}
