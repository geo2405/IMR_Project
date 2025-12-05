using UnityEngine;
using System.Speech.Recognition; // Asta merge doar daca ai facut Pasul 1 cu DLL-ul
using System.Collections.Concurrent; // Pentru a trimite mesaje intre firele de executie
using System.Globalization; // Pentru limba

public class FreeSpeech : MonoBehaviour
{
    private SpeechRecognitionEngine recognizer;
    private bool asculta = false;

    // Coada de mesaje (pentru a aduce textul de pe Thread-ul de voce pe Thread-ul Unity)
    private ConcurrentQueue<string> mesajePrimite = new ConcurrentQueue<string>();

    void Start()
    {
        // Initializam motorul. 
        // ATENTIE: Daca vrei Romana, trebuie sa ai Language Pack instalat in Windows 
        // si sa schimbi "en-US" cu "ro-RO". Altfel, va da eroare si va folosi engleza.
        try
        {
            recognizer = new SpeechRecognitionEngine(new CultureInfo("ro-RO"));
        }
        catch
        {
            Debug.LogWarning("Nu am gasit limba ceruta. Folosesc limba implicita a Windows-ului.");
            recognizer = new SpeechRecognitionEngine();
        }

        Debug.Log(recognizer != null);

        // Incarcam un dictionar "liber" (Dictation) ca sa poti spune orice propozitie
        recognizer.LoadGrammar(new DictationGrammar());

        // Setam intrarea audio (microfonul default)
        recognizer.SetInputToDefaultAudioDevice();

        // Conectam evenimentul: cand aude ceva, apeleaza functia RecunoastereReusita
        recognizer.SpeechRecognized += RecunoastereReusita;

        Debug.Log("Motorul Offline System.Speech este gata!");
    }

    void Update()
    {
        // 1. Controlul Microfonului cu SPACE
        if (Input.GetKeyDown(KeyCode.Space) && !asculta)
        {
            PornesteAscultarea();
        }
        if (Input.GetKeyUp(KeyCode.Space) && asculta)
        {
            OpresteAscultarea();
        }

        // 2. Verificam daca au venit mesaje de la motorul vocal
        // Facem asta aici, in Update, ca sa fim pe Thread-ul principal al Unity
        if (mesajePrimite.TryDequeue(out string textFinal))
        {
            Debug.Log("AM AUZIT: " + textFinal);
            TrimiteLaAvatar(textFinal);
        }
    }

    void PornesteAscultarea()
    {
        asculta = true;
        // RecognizeAsyncMode.Multiple inseamna ca asculta continuu pana il oprim
        recognizer.RecognizeAsync(RecognizeMode.Multiple);
        Debug.Log("Te ascult... (Tine apasat Space)");
    }

    void OpresteAscultarea()
    {
        asculta = false;
        recognizer.RecognizeAsyncCancel(); // Opreste ascultarea
        Debug.Log("M-am oprit.");
    }

    // Aceasta functie ruleaza pe un alt Thread! Nu atinge Unity UI de aici.
    void RecunoastereReusita(object sender, SpeechRecognizedEventArgs e)
    {
        // Verificam cat de sigur e motorul pe ce a auzit (0.0 la 1.0)
        if (e.Result.Confidence > 0.4f)
        {
            // Punem textul in coada ca sa il preia Update-ul
            mesajePrimite.Enqueue(e.Result.Text);
        }
    }

    void TrimiteLaAvatar(string text)
    {
        // AICI CONECTEZI CU SCRIPTUL LUI MIHAI
        Debug.Log("Trimit catre Avatar: " + text);
        // FindObjectOfType<ChatManager>().Intrebare(text);
    }

    // Curatenie obligatorie la iesire
    void OnApplicationQuit()
    {
        if (recognizer != null)
        {
            recognizer.Dispose();
        }
    }
}