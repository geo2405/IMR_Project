using UnityEngine;
using TMPro;

public class ProximityChat : MonoBehaviour
{
    [Header("Setări Distanță")]
    public Transform player;
    public GameObject canvasObject;  // Chat_Container (tatal)
    public float distantaActivare = 3.5f;

    [Header("Mesaj Ajutător")]
    public TMP_Text statusText;

    [Header("Animatie Proximitate")]
    public bool animateOnProximity = true;

    private CanvasGroup canvasGroup;
    private bool isClose = false;

    // Referinta la scriptul de chat de pe ACEST profesor
    private ProfessorChat myChatScript;

    void Start()
    {
        // 1. Găsim playerul (Studentul)
        if (player == null)
        {
            if (Camera.main != null) player = Camera.main.transform;
            else player = FindObjectOfType<AudioListener>().transform;
        }

        // 2. Pregătim Canvasul (Vizualul)
        if (canvasObject != null)
        {
            canvasGroup = canvasObject.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = canvasObject.AddComponent<CanvasGroup>();
        }

        // 3. CRITIC: Găsim scriptul de Chat de pe acest profesor
        myChatScript = GetComponent<ProfessorChat>();

        if (myChatScript == null)
        {
            Debug.LogError("❌ GRAV: Pe obiectul " + gameObject.name + " lipsește scriptul 'ProfessorChat'!");
        }

        HideCanvas();
    }

    void Update()
    {
        if (player == null || canvasObject == null) return;

        float dist = Vector3.Distance(transform.position, player.position);

        if (dist <= distantaActivare)
        {
            if (!isClose)
            {
                isClose = true;
                ShowCanvas();
            }
        }
        else
        {
            if (isClose)
            {
                isClose = false;
                HideCanvas();
            }
        }
    }

    void ShowCanvas()
    {
        // AICI ERA PROBLEMA! 
        // Acum îi spunem explicit lui Google: "Eu sunt proful activ!"
        if (GoogleVoice.Instance != null && myChatScript != null)
        {
            GoogleVoice.Instance.activeProfessor = myChatScript;
            Debug.Log("✅ CONECTAT: GoogleVoice acum vorbește cu " + myChatScript.professorID);
        }
        else
        {
            Debug.LogWarning("⚠️ Nu am putut conecta GoogleVoice. Verifica daca GoogleVoice exista in scena!");
        }

        // Afișăm vizual
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        if (statusText != null) statusText.text = "Apasă [SPACE] pentru a vorbi...";

        if (animateOnProximity && myChatScript != null)
            myChatScript.SetConversationActive(true);
    }

    void HideCanvas()
    {
        // Ascundem vizual
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        // Când plecăm, îi spunem lui Google că nu mai suntem lângă prof
        if (GoogleVoice.Instance != null && GoogleVoice.Instance.activeProfessor == myChatScript)
        {
            GoogleVoice.Instance.activeProfessor = null;
        }

        if (animateOnProximity && myChatScript != null)
            myChatScript.SetConversationActive(false);
    }
}
