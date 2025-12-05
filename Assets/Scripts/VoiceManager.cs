using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.IO;
using System;

public class VoiceManager : MonoBehaviour
{
    [Header("Setari OpenAI")]
    public string apiKey = "sk-proj-"; // Inlocuieste cu cheia ta sk-...

    [Header("Setari Microfon")]
    public int durataMaximaSecunde = 10;
    public KeyCode tastaTest = KeyCode.Space; // Doar pentru test la PC

    private AudioClip clipInregistrat;
    private string microfonAles;
    private bool inregistreaza = false;

    void Start()
    {
        // Gasim microfonul (cel de la casca VR se gaseste automat de obicei)
        if (Microphone.devices.Length > 0)
        {
            microfonAles = Microphone.devices[0];
            Debug.Log("Microfon detectat: " + microfonAles);
        }
        else
        {
            Debug.LogError("Nu am gasit niciun microfon!");
        }
    }

    void Update()
    {
        // Acesta este doar un test pentru cand esti la PC, fara VR
        if (Input.GetKeyDown(tastaTest)) IncepeInregistrarea();
        if (Input.GetKeyUp(tastaTest)) OpresteSiTrimite();
    }

    // Functia pe care o vom apela din VR (cand apesi butonul)
    public void IncepeInregistrarea()
    {
        if (inregistreaza) return;

        Debug.Log("Am inceput inregistrarea...");
        inregistreaza = true;
        // Incepem inregistrarea (ignora bucla, lungime 10 secunde, 44100 rata)
        clipInregistrat = Microphone.Start(microfonAles, false, durataMaximaSecunde, 44100);
    }

    // Functia pe care o vom apela din VR (cand dai drumul la buton)
    public void OpresteSiTrimite()
    {
        if (!inregistreaza) return;

        Debug.Log("Am oprit inregistrarea. Procesez...");
        inregistreaza = false;
        Microphone.End(microfonAles);

        // Trimitem la OpenAI
        StartCoroutine(TrimiteLaWhisper(clipInregistrat));
    }

    IEnumerator TrimiteLaWhisper(AudioClip clip)
    {
        // 1. Convertim AudioClip in WAV (formatul acceptat de OpenAI)
        byte[] wavData = ConvertToWav(clip);

        // 2. Pregatim formularul web
        WWWForm form = new WWWForm();
        form.AddBinaryData("file", wavData, "audio.wav", "audio/wav");
        form.AddField("model", "whisper-1");
        form.AddField("language", "ro"); // Setam limba romana pentru acuratete

        // 3. Cream cererea catre server
        using (UnityWebRequest request = UnityWebRequest.Post("https://api.openai.com/v1/audio/transcriptions", form))
        {
            request.SetRequestHeader("Authorization", "Bearer " + apiKey);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Eroare Whisper: " + request.error);
                Debug.LogError("Mesaj server: " + request.downloadHandler.text);
            }
            else
            {
                // 4. Primim raspunsul (JSON)
                string jsonRaspuns = request.downloadHandler.text;
                Debug.Log("Raspuns brut: " + jsonRaspuns);

                // Extragem textul simplu din JSON
                string textTranscris = ExtrageTextDinJson(jsonRaspuns);
                Debug.Log("TEXT FINAL: " + textTranscris);

                // AICI LEGI CU AVATARUL TAU!
                TrimiteTextulLaAvatar(textTranscris);
            }
        }
    }

    void TrimiteTextulLaAvatar(string text)
    {
        // Aici vei chema functia ta existenta care vorbeste cu Mihai/LLM
        // Exemplu: FindObjectOfType<ChatManager>().TrimiteMesaj(text);
        Debug.Log("Acum trimit avatarului textul: " + text);
    }

    // --- Zona Tehnica (Conversie WAV si JSON) ---

    // Functie simplificata de extragere text din JSON-ul OpenAI
    string ExtrageTextDinJson(string json)
    {
        // Cautam campul "text": "..."
        int indexStart = json.IndexOf("\"text\":");
        if (indexStart == -1) return "Eroare citire JSON";

        indexStart += 8; // sarim peste "text": "
        int indexFinal = json.IndexOf("\"", indexStart);

        return json.Substring(indexStart, indexFinal - indexStart);
    }

    // Functie care transforma AudioClip-ul din Unity in fisier WAV (bytes)
    byte[] ConvertToWav(AudioClip clip)
    {
        var samples = new float[clip.samples];
        clip.GetData(samples, 0);

        Int16[] intData = new Int16[samples.Length];
        Byte[] bytesData = new Byte[samples.Length * 2];
        int rescaleFactor = 32767;

        for (int i = 0; i < samples.Length; i++)
        {
            intData[i] = (short)(samples[i] * rescaleFactor);
            Byte[] byteArr = new Byte[2];
            byteArr = BitConverter.GetBytes(intData[i]);
            byteArr.CopyTo(bytesData, i * 2);
        }

        MemoryStream stream = new MemoryStream();
        BinaryWriter writer = new BinaryWriter(stream);

        int sampleRate = clip.frequency;
        int channels = clip.channels;

        writer.Write(System.Text.Encoding.UTF8.GetBytes("RIFF"));
        writer.Write(36 + bytesData.Length);
        writer.Write(System.Text.Encoding.UTF8.GetBytes("WAVE"));
        writer.Write(System.Text.Encoding.UTF8.GetBytes("fmt "));
        writer.Write(16);
        writer.Write((ushort)1);
        writer.Write((ushort)channels);
        writer.Write(sampleRate);
        writer.Write(sampleRate * channels * 2);
        writer.Write((ushort)(channels * 2));
        writer.Write((ushort)16);
        writer.Write(System.Text.Encoding.UTF8.GetBytes("data"));
        writer.Write(bytesData.Length);
        writer.Write(bytesData);

        return stream.ToArray();
    }
}