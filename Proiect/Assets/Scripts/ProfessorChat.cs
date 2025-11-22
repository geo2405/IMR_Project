using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

public class ProfessorChat : MonoBehaviour
{
    public TMP_InputField inputField;
    public TMP_Text outputText;
    private string apiUrl = "http://localhost:1234/v1/chat/completions";

    public void SendQuestion()
    {
        string question = inputField.text;
        if (!string.IsNullOrEmpty(question))
        {
            StartCoroutine(SendToLLM(question));
            inputField.text = ""; // goli câmpul după trimitere
        }
    }

    IEnumerator SendToLLM(string question)
{
    var jsonBody = "{\"model\": \"openai/gpt-oss-20b\", \"max_tokens\": 100, \"messages\": [" +
    "{\"role\": \"system\", \"content\": \"Ești un profesor prietenos din facultate. Răspunde foarte scurt, maximum două propoziții.\"}," +
    "{\"role\": \"user\", \"content\": \"" + question + "\"}]}";

    using (UnityWebRequest www = new UnityWebRequest(apiUrl, "POST"))
    {
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");

        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            // ✅ Parsează corect JSON-ul răspunsului
            string json = www.downloadHandler.text;
            try
            {
                var jsonObj = JsonUtility.FromJson<ResponseWrapper>(json);
                string answer = jsonObj.choices[0].message.content;
                outputText.text += "\n👨‍🏫 " + answer + "\n";
            }
            catch
            {
                outputText.text += "\n⚠️ Nu am putut interpreta răspunsul:\n" + json + "\n";
            }
        }
        else
        {
            outputText.text += "\n⚠️ Eroare: " + www.error + "\n";
        }
    }
}

// Clasa pentru a interpreta JSON-ul
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
        // apasă Enter (sau Return)
        if (Input.GetKeyDown(KeyCode.Return))
        {
            SendQuestion();
        }
    }
}
