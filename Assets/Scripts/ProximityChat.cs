using UnityEngine;
using TMPro;

public class ProximityChat : MonoBehaviour
{
    [Header("Setãri")]
    public Transform player;
    public GameObject canvasObject;  // Obiectul cu Canvas-ul
    public float distantaActivare = 3.0f;

    [Header("Mesaj Ajutãtor")]
    public TMP_Text statusText;

    private CanvasGroup canvasGroup; // Componenta care controleaza vizibilitatea
    private bool isClose = false;

    void Start()
    {
        // Gasim playerul automat
        if (player == null)
        {
            if (Camera.main != null) player = Camera.main.transform;
            else player = FindObjectOfType<AudioListener>().transform;
        }

        // Pregatim Canvas Group
        if (canvasObject != null)
        {
            canvasGroup = canvasObject.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                // Daca ai uitat sa il pui in Inspector, il punem noi automat
                canvasGroup = canvasObject.AddComponent<CanvasGroup>();
            }
        }

        // Ascundem la start (fara erori)
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
                if (statusText != null && statusText.text == "")
                    statusText.text = "Apasã [SPACE] pentru a vorbi...";
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
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1; // Vizibil
            canvasGroup.interactable = true; // Poti apasa pe el
            canvasGroup.blocksRaycasts = true; // Blocheaza laserul
        }
    }

    void HideCanvas()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0; // Invizibil (dar obiectul EXISTA, deci nu da eroare)
            canvasGroup.interactable = false; // Nu poti apasa din greseala
            canvasGroup.blocksRaycasts = false; // Laserul trece prin el
        }
    }
}