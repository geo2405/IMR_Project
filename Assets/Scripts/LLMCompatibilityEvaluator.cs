using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Text.RegularExpressions;

public class LLMCompatibilityEvaluator : MonoBehaviour
{
    [Header("LM Studio Settings")]
    public string serverIP = "127.0.0.1";
    public int port = 1234;
    public string model = "local-model";

    private string endpoint = "/v1/chat/completions";
    private float requestTimeout = 60f;

    // =====================================================
    // PUBLIC ENTRY POINT
    // =====================================================
    public IEnumerator Evaluate(
        string studentText,
        string professorDomain,
        System.Action<int, string> onResult)
    {
        string apiUrl = $"http://{serverIP}:{port}{endpoint}";
        string prompt = BuildPrompt(studentText, professorDomain);

        ChatRequest requestData = new ChatRequest
        {
            model = model,
            messages = new ChatMessage[]
            {
                new ChatMessage
                {
                    role = "system",
                    content = "Ești un evaluator academic strict."
                },
                new ChatMessage
                {
                    role = "user",
                    content = prompt
                }
            },
            temperature = 0.0f,
            max_tokens = 150,
            stream = false
        };

        string jsonBody = JsonUtility.ToJson(requestData);

        using (UnityWebRequest www = new UnityWebRequest(apiUrl, "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(
                System.Text.Encoding.UTF8.GetBytes(jsonBody));
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            www.timeout = Mathf.RoundToInt(requestTimeout);

            Debug.Log("📡 LLM Compatibility Evaluation...");
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("❌ LLM evaluator error: " + www.error);
                onResult?.Invoke(0, "Evaluare semantică indisponibilă.");
                yield break;
            }

            string responseJson = www.downloadHandler.text;
            Debug.Log("✅ LLM evaluator response:\n" + responseJson);

            ParseLLMResponse(responseJson, onResult);
        }
    }

    // =====================================================
    // PROMPT BUILDER
    // =====================================================
    string BuildPrompt(string studentText, string domain)
    {
        return
$@"Pe baza intereselor exprimate de STUDENT mai jos,
evaluează compatibilitatea dintre student și un profesor
din domeniul: {domain}.

Interesele studentului:
""{studentText}""

Răspunde STRICT în format JSON:
{{
  ""score"": un număr între 0 și 100,
  ""reason"": o explicație scurtă (maxim o propoziție)
}}

Dacă NU există compatibilitate, returnează totuși:
{{ ""score"": 0, ""reason"": ""Interesele nu se aliniază domeniului."" }}

NU adăuga alt text.";
    }

    // =====================================================
    // RESPONSE PARSING (ROBUST)
    // =====================================================
    void ParseLLMResponse(string json, System.Action<int, string> onResult)
    {
        string content = ExtractContent(json);
        if (string.IsNullOrEmpty(content))
        {
            onResult?.Invoke(0, "Evaluare semantică indisponibilă.");
            return;
        }

        content = content.Trim();

        // 🔑 CAZ: JSON returnat ca STRING (\"{ ... }\")
        if (content.StartsWith("\"{"))
        {
            content = content.Trim('"');
            content = content.Replace("\\\"", "\"");
            content = content.Replace("\\n", "");
        }

        Debug.Log("LLM JSON PARSAT:\n" + content);

        Match scoreMatch = Regex.Match(content, @"""score""\s*:\s*(\d+)");
        Match reasonMatch = Regex.Match(content, @"""reason""\s*:\s*""([^""]+)""");

        if (!scoreMatch.Success)
        {
            onResult?.Invoke(0, "Nu a fost returnat un scor semantic.");
            return;
        }

        int score = Mathf.Clamp(
            int.Parse(scoreMatch.Groups[1].Value), 0, 100);

        string reason = reasonMatch.Success
            ? reasonMatch.Groups[1].Value
            : "Compatibilitate semantică identificată.";

        onResult?.Invoke(score, reason);
    }

    // =====================================================
    // CHAT COMPLETIONS CONTENT EXTRACTION (CORECT)
    // =====================================================
    string ExtractContent(string json)
    {
        try
        {
            ChatCompletionWrapper wrapper =
                JsonUtility.FromJson<ChatCompletionWrapper>(json);

            return wrapper.choices[0].message.content;
        }
        catch
        {
            return null;
        }
    }

    // =====================================================
    // JSON STRUCTS PENTRU PARSARE
    // =====================================================
    [System.Serializable]
    class ChatCompletionWrapper
    {
        public Choice[] choices;
    }

    [System.Serializable]
    class Choice
    {
        public Message message;
    }

    [System.Serializable]
    class Message
    {
        public string role;
        public string content;
    }

    [System.Serializable]
    class ChatRequest
    {
        public string model;
        public ChatMessage[] messages;
        public float temperature;
        public int max_tokens;
        public bool stream;
    }

    [System.Serializable]
    class ChatMessage
    {
        public string role;
        public string content;
    }
}
