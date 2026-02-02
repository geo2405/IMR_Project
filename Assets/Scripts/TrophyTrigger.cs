using UnityEngine;

public class TrophyTrigger : MonoBehaviour
{
    [Header("Ce aprindem cand atingi trofeul")]
    public GameObject panelVictorie; // Trage Panelul Aici
    public AudioSource sunetVictorie; // Optional

    private bool jocGata = false;

    void OnTriggerEnter(Collider other)
    {
        // Verificam daca e jucatorul
        if (other.CompareTag("Player") || other.GetComponent<Camera>() != null)
        {
            if (jocGata) return;
            jocGata = true;

            Debug.Log("🏆 AI ATINS TROFEUL!");

            // 1. Aprindem mesajul
            if (panelVictorie != null) panelVictorie.SetActive(true);

            // 2. Sunet
            if (sunetVictorie != null) sunetVictorie.Play();

            // 3. Oprim timpul ca sa citesti mesajul linistit
            Time.timeScale = 0f;
        }
    }

    void Update()
    {
        // Daca am castigat, asteptam SPACE ca sa inchidem aplicatia
        if (jocGata && Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("👋 La revedere, absolventule!");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
        }
    }
}