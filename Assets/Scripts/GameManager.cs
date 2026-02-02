using UnityEngine;
using TMPro; // Pt Text
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance; // Accesibil de oriunde
    const string SignatureListKey = "Semnatura_List";

    [Header("UI Referinte")]
    public TMP_Text scoreText; // Trage ScoreText aici in Inspector
    public GameObject confettiEffect; // Optional: Trage particulele aici
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

    void Awake()
    {
        // Ne asiguram ca exista doar un singur GameManager
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
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
            // Reset complet (doar pentru testare)
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
        var chats = FindObjectsOfType<ProfessorChat>(true);
        foreach (var chat in chats)
        {
            if (chat == null) continue;
            var id = chat.professorID;
            if (string.IsNullOrEmpty(id)) continue;
            if (seen.Add(id))
                result.Add(id);
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
            // Daca e particle system, da-i play
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
        var audio = AudioManager.EnsureExists();
        var chosen = clip ?? audio.confettiSfx ?? audio.defaultSfx;
        if (chosen == null) return;
        audio.PlayOneShot(chosen);
    }

    void ConfigureConfetti(ParticleSystem ps, bool isFinal)
    {
        if (ps == null) return;

        var main = ps.main;
        if (forceColorConfetti)
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.98f, 0.75f, 0.2f), 0f),
                    new GradientColorKey(new Color(0.35f, 0.8f, 0.95f), 0.5f),
                    new GradientColorKey(new Color(0.95f, 0.4f, 0.6f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 0.7f),
                    new GradientAlphaKey(1f, 1f)
                }
            );

            main.startColor = new ParticleSystem.MinMaxGradient(gradient);

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = gradient;
        }

        if (forceConfettiBurst)
        {
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            var count = isFinal ? finalBurstCount : normalBurstCount;
            if (count < 1) count = 1;
            if (count > 500) count = 500;
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

        var main = ps.main;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.sortingOrder = confettiSortingOrder;
            if (!string.IsNullOrEmpty(confettiSortingLayer))
                renderer.sortingLayerName = confettiSortingLayer;
        }
    }

    void TriggerAchievement()
    {
        if (achievementTriggered)
            return;

        achievementTriggered = true;
        if (!showAchievementBadge)
            return;

        var badge = AchievementBadgeController.EnsureExists();
        if (badge == null)
            return;

        var subtitle = achievementSubtitle;
        if (string.IsNullOrWhiteSpace(subtitle))
            subtitle = $"Ai colectat {signatures}/{maxSignatures} semnături!";
        subtitle = subtitle.Replace("{current}", signatures.ToString()).Replace("{max}", maxSignatures.ToString());

        badge.Show(achievementTitle, subtitle, achievementIcon, keepAchievementBadge);
    }

    void TriggerBonkFeedback()
    {
        if (!showBonkFeedback)
            return;

        var bonk = BonkFeedbackController.EnsureExists();
        if (bonk == null)
            return;

        bonk.Show(bonkSprite, bonkMessage, bonkSfx);
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
