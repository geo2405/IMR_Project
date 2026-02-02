using UnityEngine;
using TMPro; // Pt Text

public class GameManager : MonoBehaviour
{
    public static GameManager Instance; // Accesibil de oriunde

    [Header("UI Referinte")]
    public TMP_Text scoreText; // Trage ScoreText aici in Inspector
    public GameObject confettiEffect; // Optional: Trage particulele aici

    private int signatures = 0;
    private int maxSignatures = 3;

    void Awake()
    {
        // Ne asiguram ca exista doar un singur GameManager
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // ASTA E BOMBA NUCLEARĂ CARE ȘTERGE TOT LA PORNIRE
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        signatures = 0; // Resetam si variabila locala
        UpdateUI();
        Debug.Log("🧹 MEMORIE ȘTEARSĂ! Semnături resetate la 0.");
    }

    // Functia pe care o apeleaza Profesorul cand termini discutia
    public void CollectSignature(string professorID)
    {
        // Verificam daca am luat deja semnatura de la prof-ul asta
        // Cheia va fi ex: "Semnatura_Prof_Popescu"
        string key = "Semnatura_" + professorID;

        if (PlayerPrefs.GetInt(key, 0) == 0)
        {
            // NU am vorbit inca, deci luam semnatura
            PlayerPrefs.SetInt(key, 1); // Salvam in memorie
            PlayerPrefs.Save();

            signatures++;
            UpdateUI();
            PlayReward();

            Debug.Log("✅ Ai primit semnătura de la: " + professorID);
        }
        else
        {
            Debug.Log("⚠️ Ai deja semnătura asta!");
        }
    }

    // Verifica daca am vorbit deja (pt a schimba textul de start)
    public bool HasSpokenTo(string professorID)
    {
        return PlayerPrefs.GetInt("Semnatura_" + professorID, 0) == 1;
    }

    void UpdateUI()
    {
        if (scoreText != null)
            scoreText.text = $"Semnături: {signatures}/{maxSignatures}";
    }

    void PlayReward()
    {
        if (confettiEffect != null)
        {
            confettiEffect.SetActive(true);
            // Daca e particle system, da-i play
            var ps = confettiEffect.GetComponent<ParticleSystem>();
            if (ps != null) ps.Play();
        }
    }

    // Resetare pt teste (poti apela asta cu un buton)
    public void ResetProgress()
    {
        PlayerPrefs.DeleteAll();
        signatures = 0;
        UpdateUI();
        Debug.Log("🔄 Progres resetat!");
    }
}