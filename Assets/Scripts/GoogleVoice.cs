using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Text;
using TMPro; 
using UnityEngine.UI;

public class GoogleVoice : MonoBehaviour
{
    [Header("Setari Google Cloud")]
    public string apiKey = "CHEIA E IN INSPECTOR ACUM";
    public string languageCode = "ro-RO";

    [Header("Setari Control")]
    public KeyCode tastaActivare = KeyCode.Space;

    [Header("Legatura cu Avatarul")]
    public TMP_InputField casutaTextAvatar; 
    public Button butonTrimite;             

    private AudioClip clipInregistrat;
    private string microfonAles;
    private bool inregistreaza = false;

    void Start()
    {
        if (Microphone.devices.Length > 0)
        {
            microfonAles = Microphone.devices[0];
            Debug.Log("Microfon detectat: " + microfonAles);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(tastaActivare) && !inregistreaza)
        {
            PornesteInregistrarea();
        }

        if (Input.GetKeyUp(tastaActivare) && inregistreaza)
        {
            OpresteSiTrimite();
        }
    }

    public void PornesteInregistrarea()
    {
        if (microfonAles == null) return;
        inregistreaza = true;
        clipInregistrat = Microphone.Start(microfonAles, false, 10, 44100);

        if (casutaTextAvatar != null) casutaTextAvatar.text = "🔴 Te ascult...";

        Debug.Log("🔴 ASCULT...");
    }

    public void OpresteSiTrimite()
    {
        if (!inregistreaza) return;
        inregistreaza = false;
        Microphone.End(microfonAles);

        if (casutaTextAvatar != null) casutaTextAvatar.text = "🟡 Procesez...";
        Debug.Log("🟡 Procesez...");

        StartCoroutine(TrimiteLaGoogle(clipInregistrat));
    }

    IEnumerator TrimiteLaGoogle(AudioClip clip)
    {
        byte[] rawData = ConvertToPCM16(clip);
        string audioBase64 = Convert.ToBase64String(rawData);

        string jsonRequest = "{ " +
            "\"config\": { " +
                "\"encoding\": \"LINEAR16\", " +
                "\"sampleRateHertz\": 44100, " +
                "\"languageCode\": \"" + languageCode + "\"" +
            "}, " +
            "\"audio\": { " +
                "\"content\": \"" + audioBase64 + "\"" +
            "} " +
        "}";

        string url = "https://speech.googleapis.com/v1/speech:recognize?key=" + apiKey;

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonRequest);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("❌ EROARE: " + request.downloadHandler.text);
                if (casutaTextAvatar != null) casutaTextAvatar.text = "Eroare Google.";
            }
            else
            {
                string jsonRaspuns = request.downloadHandler.text;
                string textFinal = ExtrageTextDinJson(jsonRaspuns);

                Debug.Log("🟢 GOOGLE A ZIS: " + textFinal);

                if (casutaTextAvatar != null)
                {
                    casutaTextAvatar.text = textFinal;

                    if (butonTrimite != null && textFinal.Length > 1)
                    {
                        Debug.Log("🤖 Apas butonul de trimitere automat...");
                        butonTrimite.onClick.Invoke();
                    }
                }
            }
        }
    }

    byte[] ConvertToPCM16(AudioClip clip)
    {
        var samples = new float[clip.samples];
        clip.GetData(samples, 0);
        short[] intData = new short[samples.Length];
        byte[] bytesData = new byte[samples.Length * 2];
        for (int i = 0; i < samples.Length; i++)
        {
            intData[i] = (short)(samples[i] * 32767);
            BitConverter.GetBytes(intData[i]).CopyTo(bytesData, i * 2);
        }
        return bytesData;
    }

    string ExtrageTextDinJson(string json)
    {
        string cauta = "\"transcript\": \"";
        int start = json.IndexOf(cauta);
        if (start == -1) return "";
        start += cauta.Length;
        int final = json.IndexOf("\"", start);
        return json.Substring(start, final - start);
    }
}