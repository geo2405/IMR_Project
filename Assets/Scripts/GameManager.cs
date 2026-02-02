using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Collections; // ADAUGAT: Pentru Coroutine (Secventa Finala)
using UnityEngine.UI;     // ADAUGAT: Pentru manipulare UI avansata

public class GameManager : MonoBehaviour
{
    public static GameManager Instance; // Accesibil de oriunde
    const string SignatureListKey = "Semnatura_List";

    [Header("--- SETĂRI TESTARE ---")]
    public int numarTotalProfesori = 1; // PUNE 1 PENTRU TEST!
    public int pragScorCompatibilitate = 70;

    [Header("--- UI LOGICĂ NOUĂ ---")]
    public GameObject panelRezultat;  // Trage Panel_RezultatFinal aici
    public TMP_Text textMesaj;        // Trage Text_Mesaj aici
    public GameObject obiectTrofeu;   // Trage Trofeul aici

    [Header("UI Referinte Vechi")]
    public TMP_Text scoreText; // Trage ScoreText aici in Inspector
    public GameObject confettiEffect; // Optional: Trage particulele aici

    [Header("Setari Scena Horror")]
    public string numeScenaFantoma = "Scena_Fantoma";

    [Header("Audio")]
    public AudioClip signatureSfx;
    public AudioClip achievementSfx;

    [Header("VFX")]
    public bool forceColorConfetti = true;
    public bool forceConfettiBurst = true;
    public bool confettiOnScreen = true;
    public Vector3 confettiScreenOffset = new Vector3(0f, 0.25f, 1.5f);
    public int confettiSortingOrder = 5000;
    public string confettiSortingLayer = "UI";
    public int normalBurstCount = 40;
    public int finalBurstCount = 90;

    [Header("Achievement")]
    public bool showAchievementBadge = true;
    public string achievementTitle = "Achievement Deblocat!";
    public string achievementSubtitle = "Ai colectat 3/3 semnături!";
    public Sprite achievementIcon;
    public bool keepAchievementBadge = false;

    [Header("Bonk Feedback")]
    public bool showBonkFeedback = true;
    public Sprite bonkSprite;
    public AudioClip bonkSfx;
    public string bonkMessage = "Nice try";

    [Header("Debug")]
    public bool wipePlayerPrefsOnStart = false;

    private int signatures = 0;
    private int maxSignatures = 3;
    private bool achievementTriggered = false;

    // --- ADĂUGAT PENTRU FANTOMĂ ---
    private int scoruriPeste50 = 0;
    private int profesoriDiscutati = 0;

    void Awake()
    {
        // Ne asiguram ca exista doar un singur GameManager
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // FORTARE PENTRU TEST
        numarTotalProfesori = 1;
    }

    void Start()
    {
        if (scoreText == null)
        {
            var scoreGo = GameObject.Find("ScoreText");
            if (scoreGo != null)
                scoreText = scoreGo.GetComponent<TMP_Text>();
            else
            {
                var texts = FindObjectsOfType<TMP_Text>(true);
                foreach (var txt in texts)
                {
                    if (txt == null) continue;
                    var name = txt.name.ToLowerInvariant();
                    if (name.Contains("score") || txt.text.Contains("Semnături"))
                    {
                        scoreText = txt;
                        break;
                    }
                }
            }
        }

        if (wipePlayerPrefsOnStart)
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Debug.Log("🧹 MEMORIE ȘTEARSĂ! Semnături resetate la 0.");
        }
        else
        {
            ResetSignatureKeys();
        }

        signatures = 0;
        UpdateUI();
    }

    // --- FUNCTIA NOUĂ APELATĂ DE BUTON ---
    // Aceasta leaga sistemul vechi (confetti/bonk) de sistemul nou (panel/fantoma)
    public void AmTerminatCuUnProfesor(int scorObtinut)
    {
        profesoriDiscutati++;
        Debug.Log($"📊 GAME MANAGER: Scor primit: {scorObtinut}%");

        // LOGICA DE RECOMPENSĂ VIZUALĂ (CONFETTI vs BONK)
        if (scorObtinut >= pragScorCompatibilitate)
        {
            scoruriPeste50++;
            // Aici refolosim functia ta veche pentru confetti!
            PlayReward(false);
        }
        else
        {
            // Aici refolosim functia ta veche pentru bonk!
            TriggerBonkFeedback();
        }

        // VERIFICARE FINAL JOC (INSTANT)
        if (profesoriDiscutati >= numarTotalProfesori)
        {
            StartCoroutine(SecventaFinala());
        }
    }

    // --- SECVENTA FINALA (PANEL NEGRU -> DECIZIE) ---
    IEnumerator SecventaFinala()
    {
        Debug.Log("🎬 START SECVENTA FINALA!");

        // 1. Aprindem Panelul Negru
        if (panelRezultat != null)
        {
            panelRezultat.SetActive(true);
            panelRezultat.transform.SetAsLastSibling(); // Il punem peste tot

            // Ne asiguram ca e opac
            CanvasGroup cg = panelRezultat.GetComponent<CanvasGroup>();
            if (cg == null) cg = panelRezultat.AddComponent<CanvasGroup>();
            cg.alpha = 1f;
        }

        // 2. Mesaj Suspans
        if (textMesaj != null) textMesaj.text = "Se analizează dosarul...";

        // 3. Asteptam 3 secunde
        yield return new WaitForSeconds(3.0f);

        // 4. Decizia
        if (scoruriPeste50 >= 1)
        {
            // --- VICTORIE ---
            if (textMesaj != null)
            {
                textMesaj.color = Color.green;
                textMesaj.text = "ADMIS!\nAi găsit coordonator!";
            }

            PlayReward(true); // Confetti Finale!

            yield return new WaitForSeconds(2.0f);

            if (panelRezultat) panelRezultat.SetActive(false); // Ascundem mesajul
            if (obiectTrofeu) obiectTrofeu.SetActive(true);    // Apare trofeul
        }
        else
        {
            // --- ESEC ---
            if (textMesaj != null)
            {
                textMesaj.color = Color.red;
                textMesaj.text = "RESPINS!\nNu ai suficiente credite...";
            }

            yield return new WaitForSeconds(2.0f);
            SceneManager.LoadScene(numeScenaFantoma);
        }
    }

    // =========================================================
    // MAI JOS SUNT DOAR FUNCTIILE TALE VECHI (NESCHIMBATE)
    // =========================================================

    public void CollectSignature(string professorID)
    {
        string key = "Semnatura_" + professorID;

        if (PlayerPrefs.GetInt(key, 0) == 0)
        {
            PlayerPrefs.SetInt(key, 1);
            RegisterSignatureKey(professorID);
            PlayerPrefs.Save();

            signatures++;
            UpdateUI();
            var isFinal = signatures >= maxSignatures;
            PlayReward(isFinal);
            PlaySfx(isFinal ? achievementSfx : signatureSfx);
            if (isFinal)
                TriggerAchievement();

            Debug.Log("✅ Ai primit semnătura de la: " + professorID);
        }
        else
        {
            Debug.Log("⚠️ Ai deja semnătura asta!");
            signatures = CountSignaturesFromPrefs();
            UpdateUI();
            TriggerBonkFeedback();
        }
    }

    // Funcția veche - o păstrăm dar nu o mai folosim direct în logica nouă
    public void InregistreazaScor(int scor, string professorID)
    {
        AmTerminatCuUnProfesor(scor);
    }

    public bool HasSpokenTo(string professorID)
    {
        return PlayerPrefs.GetInt("Semnatura_" + professorID, 0) == 1;
    }

    void UpdateUI()
    {
        if (scoreText != null)
            scoreText.text = $"Semnături: {signatures}/{maxSignatures}";
    }

    int CountSignaturesFromPrefs()
    {
        var ids = LoadSignatureIds();
        if (ids.Count == 0)
            ids = GetProfessorIds();

        int count = 0;
        foreach (var id in ids)
        {
            if (PlayerPrefs.GetInt("Semnatura_" + id, 0) == 1)
                count++;
        }

        return count;
    }

    void ResetSignatureKeys()
    {
        var ids = LoadSignatureIds();
        if (ids.Count == 0)
            ids = GetProfessorIds();

        foreach (var id in ids)
        {
            PlayerPrefs.DeleteKey("Semnatura_" + id);
        }
        PlayerPrefs.DeleteKey(SignatureListKey);
        PlayerPrefs.Save();
    }

    List<string> GetProfessorIds()
    {
        var result = new List<string>();
        var seen = new HashSet<string>();
        // Folosim Object[] pentru compatibilitate
        var chats = FindObjectsOfType<MonoBehaviour>();
        foreach (var chat in chats)
        {
            // Reflection simplu ca sa nu depindem de clasa ProfessorChat daca nu e compilata
            if (chat.GetType().Name == "ProfessorChat")
            {
                var field = chat.GetType().GetField("professorID");
                if (field != null)
                {
                    string id = field.GetValue(chat) as string;
                    if (!string.IsNullOrEmpty(id) && seen.Add(id)) result.Add(id);
                }
            }
        }
        return result;
    }

    List<string> LoadSignatureIds()
    {
        var list = new List<string>();
        var raw = PlayerPrefs.GetString(SignatureListKey, "");
        if (string.IsNullOrEmpty(raw))
            return list;

        var parts = raw.Split(';');
        foreach (var part in parts)
        {
            if (string.IsNullOrEmpty(part)) continue;
            if (!list.Contains(part))
                list.Add(part);
        }

        return list;
    }

    void RegisterSignatureKey(string professorID)
    {
        if (string.IsNullOrEmpty(professorID)) return;

        var ids = LoadSignatureIds();
        if (!ids.Contains(professorID))
        {
            ids.Add(professorID);
            PlayerPrefs.SetString(SignatureListKey, string.Join(";", ids));
        }
    }

    void PlayReward(bool isFinal)
    {
        if (confettiEffect != null)
        {
            confettiEffect.SetActive(true);
            var ps = confettiEffect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ConfigureConfetti(ps, isFinal);
                if (confettiOnScreen)
                    PositionConfettiOnScreen(ps);
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.Play();
            }
        }
    }

    void PlaySfx(AudioClip clip)
    {
        // Logica simplificata pentru audio
        if (clip == null) return;
        AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position);
    }

    void ConfigureConfetti(ParticleSystem ps, bool isFinal)
    {
        if (ps == null) return;
        var main = ps.main;
        if (forceColorConfetti)
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(new Color(0.98f, 0.75f, 0.2f), 0f), new GradientColorKey(new Color(0.35f, 0.8f, 0.95f), 0.5f), new GradientColorKey(new Color(0.95f, 0.4f, 0.6f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
            );
            main.startColor = new ParticleSystem.MinMaxGradient(gradient);
        }

        if (forceConfettiBurst)
        {
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            var count = isFinal ? finalBurstCount : normalBurstCount;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
        }
    }

    void PositionConfettiOnScreen(ParticleSystem ps)
    {
        var cam = Camera.main;
        if (cam == null) return;

        var t = confettiEffect.transform;
        t.SetParent(cam.transform, false);
        t.localPosition = confettiScreenOffset;
        t.localRotation = Quaternion.identity;
    }

    void TriggerAchievement()
    {
        if (achievementTriggered) return;
        achievementTriggered = true;
        Debug.Log("🏆 ACHIEVEMENT TRIGGERED: " + achievementTitle);
    }

    void TriggerBonkFeedback()
    {
        if (!showBonkFeedback) return;
        Debug.Log("🔨 BONK!");
    }

    public void ResetProgress()
    {
        PlayerPrefs.DeleteAll();
        signatures = 0;
        UpdateUI();
    }
}