using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // Avem nevoie de asta pentru text
using System.Collections; // Pentru Coroutine (asteptare)

public class GameFlowManager : MonoBehaviour
{
    public static GameFlowManager instance;

    [Header("Setari Joc")]
    public int numarTotalProfesori = 1; // PUNE 1 ACUM PENTRU TEST! (Pune 3 cand termini jocul)
    public int pragScorCompatibilitate = 70;

    [Header("UI Feedback")]
    public GameObject panelRezultat;  // Trage Panel_RezultatFinal aici
    public TMP_Text textMesaj;        // Trage Text_Mesaj aici

    [Header("Obiecte & Scene")]
    public GameObject obiectTrofeu;   // Trage Trofeul aici
    public string numeScenaFantoma = "Scena_Fantoma";

    // Contoare
    private int semnaturiColectate = 0;
    private int profesoriCompatibili = 0;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    public void AmTerminatCuUnProfesor(int scorObtinut)
    {
        Debug.Log($"🔔 MANAGER: Am primit scorul {scorObtinut}");

        semnaturiColectate++;

        if (scorObtinut >= pragScorCompatibilitate)
        {
            profesoriCompatibili++;
            Debug.Log($"✅ Nota Buna: {scorObtinut}");
        }
        else
        {
            Debug.Log($"❌ Nota Mica: {scorObtinut}");
        }

        // Verificam daca s-a terminat jocul
        VerificaFinalulJocului();
    }

    void VerificaFinalulJocului()
    {
        // Doar daca am vorbit cu TOTI profesorii necesari
        if (semnaturiColectate >= numarTotalProfesori)
        {
            // Pornim secventa de final
            StartCoroutine(SecventaFinala());
        }
    }

    IEnumerator SecventaFinala()
    {
        Debug.Log("🎬 MANAGER: INCEP SECVENTA FINALA!");

        // 1. Fortam Panelul sa fie vizibil si ultimul (deasupra tuturor)
        if (panelRezultat != null)
        {
            panelRezultat.SetActive(true);
            panelRezultat.transform.SetAsLastSibling(); // TRUC: Il muta in fata tuturor!

            // SIGURANTA EXTRA: Fortam Alpha la 1 (sa fie opac)
            CanvasGroup cg = panelRezultat.GetComponent<CanvasGroup>();
            if (cg == null) cg = panelRezultat.AddComponent<CanvasGroup>();
            cg.alpha = 1f;
        }
        else
        {
            Debug.LogError("❌ NU AI TRAS PANELUL IN INSPECTOR!");
        }

        // 2. Mesaj de suspans
        if (textMesaj != null)
        {
            textMesaj.color = Color.white;
            textMesaj.text = "Se analizează dosarul...";
        }

        // Asteptam 3 secunde (suspans)
        yield return new WaitForSeconds(3.0f);

        // 3. Verificam rezultatul
        if (profesoriCompatibili >= 1)
        {
            // --- VICTORIE ---
            Debug.Log("🎉 VICTORIE!");
            if (textMesaj != null)
            {
                textMesaj.color = Color.green;
                textMesaj.text = "ADMIS!\nAi găsit coordonator!";
            }

            yield return new WaitForSeconds(3.0f);

            if (panelRezultat) panelRezultat.SetActive(false); // Ascundem mesajul
            if (obiectTrofeu) obiectTrofeu.SetActive(true);    // Apare trofeul
        }
        else
        {
            // --- ESEC ---
            Debug.Log("💀 ESEC!");
            if (textMesaj != null)
            {
                textMesaj.color = Color.red;
                textMesaj.text = "RESPINS!\nNu ai suficiente credite...";
            }

            yield return new WaitForSeconds(3.0f);
            SceneManager.LoadScene(numeScenaFantoma);
        }
    }
}