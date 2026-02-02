using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class WelcomeOverlayController : MonoBehaviour
{
    private const string DefaultPrefsKey = "welcome_shown";
    private const string PrefabResourcePath = "Prefabs/WelcomeOverlay";

    [Header("Content")]
    public string title = "Bine ai venit în FIIVerse!";
    [TextArea(2, 6)]
    public List<string> pages = new List<string>();
    public string nextHint = "Apasă N pentru următorul mesaj";
    public string skipHint = "Esc pentru skip/close";

    [Header("Logos")]
    public Texture2D[] logoTextures;

    [Header("Visual")]
    public Sprite panelSprite;
    public Color panelColor = new Color(0.08f, 0.1f, 0.12f, 0.9f);
    public Color backdropColor = new Color(0f, 0f, 0f, 0.45f);
    public Vector2 panelSize = new Vector2(900f, 520f);

    [Header("Animation")]
    public float fadeDuration = 0.2f;
    public float slideDistance = 18f;

    [Header("Audio")]
    public AudioClip openSfx;
    public AudioClip nextSfx;
    public AudioClip closeSfx;

    [Header("Behavior")]
    public string playerPrefsKey = DefaultPrefsKey;
    public bool alwaysShow = false;
    public bool allowSkipWithEscape = true;
    public bool blockGameplayInput = true;
    public bool useUnscaledTime = true;

    [Header("Confetti")]
    public bool playConfettiOnOpen = true;
    public bool playConfettiOnClose = true;
    public float confettiDuration = 1.5f;

    public static bool IsOpen { get; private set; }

    private CanvasGroup rootGroup;
    private CanvasGroup pageGroup;
    private RectTransform pageRect;
    private TMP_Text pageText;
    private TMP_Text hintText;
    private TMP_Text titleText;
    private GameObject logoRow;
    private ParticleSystem confetti;
    private int pageIndex;
    private bool isTransitioning;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoSpawn()
    {
        if (Object.FindObjectOfType<WelcomeOverlayController>() != null)
            return;

        var scene = SceneManager.GetActiveScene();
        if (scene.buildIndex != 0)
            return;

        var prefab = Resources.Load<WelcomeOverlayController>(PrefabResourcePath);
        if (prefab == null)
        {
            Debug.LogWarning("WelcomeOverlay prefab missing at Resources/Prefabs/WelcomeOverlay.");
            return;
        }

        if (!prefab.alwaysShow && PlayerPrefs.GetInt(DefaultPrefsKey, 0) == 1)
            return;

        Object.Instantiate(prefab);
    }

    private void Awake()
    {
        if (string.IsNullOrWhiteSpace(playerPrefsKey))
            playerPrefsKey = DefaultPrefsKey;

        if (!alwaysShow && PlayerPrefs.GetInt(playerPrefsKey, 0) == 1)
        {
            gameObject.SetActive(false);
            return;
        }

        EnsureDefaultPages();
        BuildUiIfNeeded();
        ApplyPage(0, instant: true);

        rootGroup.alpha = 0f;
        rootGroup.blocksRaycasts = blockGameplayInput;
        rootGroup.interactable = blockGameplayInput;
    }

    private void Start()
    {
        if (!gameObject.activeInHierarchy)
            return;

        IsOpen = true;
        StartCoroutine(FadeCanvas(rootGroup, 0f, 1f, fadeDuration));
        PlaySfx(openSfx);

        if (playConfettiOnOpen)
        {
            EnsureConfetti();
            confetti.Play(true);
        }
    }

    private void OnDisable()
    {
        IsOpen = false;
    }

    private void Update()
    {
        if (!gameObject.activeInHierarchy || isTransitioning)
            return;

        if (IsNextPressed())
        {
            AdvancePage();
            return;
        }

        if (allowSkipWithEscape && IsSkipPressed())
            Skip();
    }

    private void EnsureDefaultPages()
    {
        if (pages != null && pages.Count > 0)
            return;

        pages = new List<string>
        {
            "FIIVerse îți oferă un tur rapid: vorbește cu profesorii, colectează semnături și descoperă laboratoarele.",
            "Folosește N pentru a trece prin mesaje. Poți închide oricând cu Esc.",
            "Succes! Explorează și distrează-te în FIIverse."
        };
    }

    private void BuildUiIfNeeded()
    {
        rootGroup = GetComponent<CanvasGroup>();

        var canvas = GetComponent<Canvas>();
        if (canvas == null)
            canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        var scaler = GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        if (GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();

        if (rootGroup == null)
            rootGroup = gameObject.AddComponent<CanvasGroup>();

        var backdrop = CreateUi<Image>("Backdrop", transform);
        var backdropRect = backdrop.rectTransform;
        backdropRect.anchorMin = Vector2.zero;
        backdropRect.anchorMax = Vector2.one;
        backdropRect.offsetMin = Vector2.zero;
        backdropRect.offsetMax = Vector2.zero;
        backdrop.color = backdropColor;

        var panel = CreateUi<Image>("Panel", transform);
        var panelRect = panel.rectTransform;
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = panelSize;
        panelRect.anchoredPosition = Vector2.zero;
        panel.color = panelColor;
        if (panelSprite != null)
        {
            panel.sprite = panelSprite;
            panel.type = Image.Type.Sliced;
        }

        titleText = CreateTmp("Title", panel.transform, 40, FontStyles.Bold);
        var titleRect = titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(panelSize.x - 120f, 70f);
        titleRect.anchoredPosition = new Vector2(0f, -24f);
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.text = title;

        pageText = CreateTmp("Message", panel.transform, 26, FontStyles.Normal);
        pageRect = pageText.rectTransform;
        pageRect.anchorMin = new Vector2(0.5f, 0.5f);
        pageRect.anchorMax = new Vector2(0.5f, 0.5f);
        pageRect.pivot = new Vector2(0.5f, 0.5f);
        pageRect.sizeDelta = new Vector2(panelSize.x - 140f, 200f);
        pageRect.anchoredPosition = new Vector2(0f, 10f);
        pageText.alignment = TextAlignmentOptions.Center;
        pageText.enableWordWrapping = true;
        pageText.text = string.Empty;
        pageGroup = pageText.gameObject.AddComponent<CanvasGroup>();

        logoRow = new GameObject("LogoRow", typeof(RectTransform));
        logoRow.transform.SetParent(panel.transform, false);
        var logoRect = logoRow.GetComponent<RectTransform>();
        logoRect.anchorMin = new Vector2(0.5f, 0f);
        logoRect.anchorMax = new Vector2(0.5f, 0f);
        logoRect.pivot = new Vector2(0.5f, 0f);
        logoRect.sizeDelta = new Vector2(panelSize.x - 160f, 80f);
        logoRect.anchoredPosition = new Vector2(0f, 72f);
        var logoLayout = logoRow.AddComponent<HorizontalLayoutGroup>();
        logoLayout.childAlignment = TextAnchor.MiddleCenter;
        logoLayout.spacing = 18f;
        logoLayout.childControlHeight = true;
        logoLayout.childControlWidth = false;
        logoLayout.childForceExpandHeight = false;
        logoLayout.childForceExpandWidth = false;

        PopulateLogos();

        hintText = CreateTmp("Hint", panel.transform, 18, FontStyles.Normal);
        var hintRect = hintText.rectTransform;
        hintRect.anchorMin = new Vector2(0.5f, 0f);
        hintRect.anchorMax = new Vector2(0.5f, 0f);
        hintRect.pivot = new Vector2(0.5f, 0f);
        hintRect.sizeDelta = new Vector2(panelSize.x - 120f, 40f);
        hintRect.anchoredPosition = new Vector2(0f, 18f);
        hintText.alignment = TextAlignmentOptions.Center;
        hintText.color = new Color(1f, 1f, 1f, 0.75f);
    }

    private void PopulateLogos()
    {
        if (logoTextures == null || logoTextures.Length == 0)
        {
            if (logoRow != null)
                logoRow.SetActive(false);
            return;
        }

        for (int i = 0; i < logoTextures.Length; i++)
        {
            var texture = logoTextures[i];
            if (texture == null)
                continue;

            var logo = new GameObject("Logo_" + i, typeof(RectTransform));
            logo.transform.SetParent(logoRow.transform, false);

            var rawImage = logo.AddComponent<RawImage>();
            rawImage.texture = texture;
            rawImage.color = Color.white;

            var layout = logo.AddComponent<LayoutElement>();
            layout.preferredHeight = 52f;
            layout.preferredWidth = 120f;

            var fitter = logo.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            if (texture.height > 0)
                fitter.aspectRatio = (float)texture.width / texture.height;
        }
    }

    private void EnsureConfetti()
    {
        if (confetti != null)
            return;

        var confettiGo = new GameObject("Confetti", typeof(ParticleSystem));
        confettiGo.transform.SetParent(transform, false);
        confettiGo.transform.localPosition = new Vector3(0f, 160f, 0f);
        confetti = confettiGo.GetComponent<ParticleSystem>();
        var main = confetti.main;
        main.duration = confettiDuration;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 2.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.14f);
        main.gravityModifier = 0.2f;
        main.playOnAwake = false;
        main.maxParticles = 120;

        var emission = confetti.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 60) });

        var shape = confetti.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 25f;
        shape.radius = 0.2f;

        var colorOverLifetime = confetti.colorOverLifetime;
        colorOverLifetime.enabled = true;
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
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;

        var renderer = confetti.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = 999;
    }

    private void AdvancePage()
    {
        if (pages == null || pages.Count == 0)
            return;

        if (pageIndex >= pages.Count - 1)
        {
            Close();
            return;
        }

        PlaySfx(nextSfx);
        StartCoroutine(AnimateToPage(pageIndex + 1));
    }

    private void Skip()
    {
        Close();
    }

    private void Close()
    {
        if (!gameObject.activeInHierarchy)
            return;

        PlaySfx(closeSfx);
        StartCoroutine(FadeOutAndClose());
    }

    private IEnumerator AnimateToPage(int nextIndex)
    {
        if (pageGroup == null || pageRect == null)
        {
            ApplyPage(nextIndex, instant: true);
            yield break;
        }

        isTransitioning = true;
        Vector2 startPos = pageRect.anchoredPosition;
        Vector2 outPos = startPos + new Vector2(-slideDistance, 0f);

        yield return FadeAndMove(pageGroup, pageRect, 1f, 0f, startPos, outPos, fadeDuration);
        ApplyPage(nextIndex, instant: true);

        Vector2 inStart = startPos + new Vector2(slideDistance, 0f);
        pageRect.anchoredPosition = inStart;
        yield return FadeAndMove(pageGroup, pageRect, 0f, 1f, inStart, startPos, fadeDuration);

        isTransitioning = false;
    }

    private void ApplyPage(int index, bool instant)
    {
        if (pages == null || pages.Count == 0)
            return;

        pageIndex = Mathf.Clamp(index, 0, pages.Count - 1);
        pageText.text = pages[pageIndex];
        if (hintText != null)
        {
            string hint = nextHint;
            if (allowSkipWithEscape)
                hint = hint + "  |  " + skipHint;
            hintText.text = hint + $"  ({pageIndex + 1}/{pages.Count})";
        }

        if (instant && pageGroup != null)
            pageGroup.alpha = 1f;
    }

    private IEnumerator FadeOutAndClose()
    {
        isTransitioning = true;
        if (playConfettiOnClose)
        {
            EnsureConfetti();
            confetti.Play(true);
        }

        rootGroup.blocksRaycasts = false;
        rootGroup.interactable = false;
        yield return FadeCanvas(rootGroup, rootGroup.alpha, 0f, fadeDuration);
        if (playConfettiOnClose)
            yield return WaitForSecondsConfetti();
        if (!alwaysShow)
        {
            PlayerPrefs.SetInt(playerPrefsKey, 1);
            PlayerPrefs.Save();
        }
        IsOpen = false;
        Destroy(gameObject);
    }

    private IEnumerator FadeCanvas(CanvasGroup group, float from, float to, float duration)
    {
        if (group == null)
            yield break;

        float t = 0f;
        group.alpha = from;
        if (duration <= 0f)
        {
            group.alpha = to;
            yield break;
        }

        while (t < duration)
        {
            t += DeltaTime();
            group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
            yield return null;
        }

        group.alpha = to;
    }

    private IEnumerator FadeAndMove(CanvasGroup group, RectTransform rect, float from, float to, Vector2 posFrom, Vector2 posTo, float duration)
    {
        if (group == null || rect == null)
            yield break;

        float t = 0f;
        group.alpha = from;
        rect.anchoredPosition = posFrom;
        if (duration <= 0f)
        {
            group.alpha = to;
            rect.anchoredPosition = posTo;
            yield break;
        }

        while (t < duration)
        {
            t += DeltaTime();
            float lerp = Mathf.Clamp01(t / duration);
            group.alpha = Mathf.Lerp(from, to, lerp);
            rect.anchoredPosition = Vector2.Lerp(posFrom, posTo, lerp);
            yield return null;
        }

        group.alpha = to;
        rect.anchoredPosition = posTo;
    }

    private bool IsNextPressed()
    {
        bool pressed = false;
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.nKey.wasPressedThisFrame)
            pressed = true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.N))
            pressed = true;
#endif
        return pressed;
    }

    private bool IsSkipPressed()
    {
        bool pressed = false;
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            pressed = true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Escape))
            pressed = true;
#endif
        return pressed;
    }

    private float DeltaTime()
    {
        return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    }

    private IEnumerator WaitForSecondsConfetti()
    {
        if (confettiDuration <= 0f)
            yield break;

        if (useUnscaledTime)
            yield return new WaitForSecondsRealtime(confettiDuration);
        else
            yield return new WaitForSeconds(confettiDuration);
    }

    private void PlaySfx(AudioClip clip)
    {
        var audio = AudioManager.EnsureExists();
        var chosen = clip ?? audio.defaultSfx;
        if (chosen == null)
            return;

        audio.PlayOneShot(chosen);
    }

    private static T CreateUi<T>(string name, Transform parent) where T : Component
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.AddComponent<T>();
    }

    private static TMP_Text CreateTmp(string name, Transform parent, float size, FontStyles style)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var text = go.AddComponent<TextMeshProUGUI>();
        text.fontSize = size;
        text.fontStyle = style;
        text.color = Color.white;
        text.text = string.Empty;
        return text;
    }
}
