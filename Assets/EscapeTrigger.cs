using UnityEngine;

public class EscapeTrigger : MonoBehaviour
{
    [Header("UI - Trage Panelul de Castig Aici")]
    public GameObject panelCastig;
    public AudioSource sunetVictorie;

    private bool jocTerminat = false;

    void Start()
    {
        // Ne asiguram ca e stins la inceput
        if (panelCastig != null) panelCastig.SetActive(false);
    }

    void Update()
    {
        // Asteptam tasta SPACE ca sa inchidem jocul
        if (jocTerminat)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Debug.Log("👋 Iesim din joc (Victorie)!");
                #if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
                #else
                    Application.Quit();
                #endif
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Verificam daca a intrat Jucatorul
        if (other.CompareTag("Player") || other.GetComponent<Camera>() != null)
        {
            if (jocTerminat) return; // Evitam declansarea dubla
            jocTerminat = true;

            Debug.Log("🚪 VICTORIE! Apasa SPACE ca sa iesi.");

            // 1. Distrugem fantoma IMEDIAT (sa nu te omoare cat timp citesti)
            var fantoma = FindObjectOfType<GhostAI>();
            if (fantoma != null) Destroy(fantoma.gameObject);

            // 2. Aprindem Panelul (Asigura-te ca e pozitionat bine in scena!)
            if (panelCastig != null)
            {
                panelCastig.SetActive(true);
            }

            // 3. Sunetul
            if (sunetVictorie != null) sunetVictorie.Play();

            // 4. Oprim timpul (Jocul ingheata, poti citi linistit)
            Time.timeScale = 0;
        }
    }
}