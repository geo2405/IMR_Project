using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class WelcomeOverlayController_VR : MonoBehaviour
{
    private const string DefaultPrefsKey = "welcome_shown";
    private const string PrefabResourcePath = "Prefabs/WelcomeOverlay";

    [Header("Content")]
    public string title = "Bine ai venit în FIIVerse!";
    [TextArea(2, 6)]
    public List<string> pages = new();
    public string nextHint = "Next";
    public string skipHint = "Skip";

    [Header("Visual")]
    public Color panelColor = new(0.08f, 0.1f, 0.12f, 0.9f);
    public Color backdropColor = new(0f, 0f, 0f, 0.45f);
    public Vector2 panelSize = new(900, 520);

    [Header("VR Settings")]
    public float vrDistance = 1.5f;
    public float vrScale = 0.001f;

    [Header("Animation")]
    public float fadeDuration = 0.25f;

    [Header("Behavior")]
    public string playerPrefsKey = DefaultPrefsKey;
    public bool alwaysShow = false;

    public static bool IsOpen { get; private set; }

    private Canvas canvas;
    private CanvasGroup rootGroup;
    private TMP_Text titleText;
    private TMP_Text pageText;
    private TMP_Text hintText;

    private int pageIndex;
    private bool isVr;

    // ============================
    // AUTO SPAWN
    // ============================
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoSpawn()
    {
        if (Object.FindObjectOfType<WelcomeOverlayController_VR>() != null)
            return;

        var scene = SceneManager.GetActiveScene();
        if (scene.buildIndex != 0)
            return;

        var prefab = Resources.Load<WelcomeOverlayController_VR>(PrefabResourcePath);
        if (prefab == null)
        {
            Debug.LogWarning("WelcomeOverlay prefab missing at Resources/Prefabs/WelcomeOverlay");
            return;
        }

        if (!prefab.alwaysShow && PlayerPrefs.GetInt(DefaultPrefsKey, 0) == 1)
            return;

        Object.Instantiate(prefab);
    }

    // ============================
    // UNITY LIFECYCLE
    // ============================
    private void Awake()
    {
        DetectMode();
        EnsurePages();
        BuildUI();
        ApplyPage(0);

        rootGroup.alpha = 0f;
    }

    private void Start()
    {
        StartCoroutine(AttachAndShow());
    }

    private void Update()
    {
        if (!IsOpen)
            return;

        if (IsNextPressed())
            Next();

        if (IsSkipPressed())
            Close();
    }

    // ============================
    // MODE DETECTION
    // ============================
    private void DetectMode()
    {
        isVr = UnityEngine.XR.XRSettings.isDeviceActive;
    }

    // ============================
    // UI BUILD
    // ============================
    private void BuildUI()
    {
        canvas = gameObject.AddComponent<Canvas>();
        rootGroup = gameObject.AddComponent<CanvasGroup>();
        gameObject.AddComponent<GraphicRaycaster>();

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        if (isVr)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
        }
        else
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
        }

        var rect = canvas.GetComponent<RectTransform>();
        rect.sizeDelta = panelSize;

        // Backdrop
        var backdrop = Create<Image>("Backdrop", transform);
        backdrop.color = backdropColor;
        Stretch(backdrop.rectTransform);

        // Panel
        var panel = Create<Image>("Panel", transform);
        panel.color = panelColor;
        panel.rectTransform.sizeDelta = panelSize;
        panel.rectTransform.anchoredPosition = Vector2.zero;

        titleText = CreateText("Title", panel.transform, 40, FontStyles.Bold);
        titleText.text = title;
        titleText.alignment = TextAlignmentOptions.Center;

        pageText = CreateText("Page", panel.transform, 26, FontStyles.Normal);
        pageText.alignment = TextAlignmentOptions.Center;

        hintText = CreateText("Hint", panel.transform, 18, FontStyles.Normal);
        hintText.alignment = TextAlignmentOptions.Center;
        hintText.color = new Color(1, 1, 1, 0.7f);
    }

    // ============================
    // ATTACH & SHOW
    // ============================
    private IEnumerator AttachAndShow()
    {
        yield return null; // așteaptă XR init

        if (isVr && Camera.main != null)
        {
            transform.SetParent(Camera.main.transform, false);
            transform.localPosition = new Vector3(0, 0, vrDistance);
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one * vrScale;
        }

        IsOpen = true;
        StartCoroutine(Fade(0, 1));
    }

    // ============================
    // PAGE LOGIC
    // ============================
    private void Next()
    {
        pageIndex++;
        if (pageIndex >= pages.Count)
        {
            Close();
            return;
        }

        ApplyPage(pageIndex);
    }

    private void ApplyPage(int index)
    {
        pageText.text = pages[index];
        hintText.text = $"{nextHint} | {skipHint} ({index + 1}/{pages.Count})";
    }

    private void Close()
    {
        StartCoroutine(CloseRoutine());
    }

    private IEnumerator CloseRoutine()
    {
        yield return Fade(1, 0);

        if (!alwaysShow)
        {
            PlayerPrefs.SetInt(playerPrefsKey, 1);
            PlayerPrefs.Save();
        }

        IsOpen = false;
        Destroy(gameObject);
    }

    // ============================
    // INPUT
    // ============================
   private bool IsNextPressed()
{
#if ENABLE_INPUT_SYSTEM
    // PC
    if (Keyboard.current != null && Keyboard.current.nKey.wasPressedThisFrame)
        return true;

    // VR – Quest (A button)
    if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)
        return true;
#endif
    return false;
}

private bool IsSkipPressed()
{
#if ENABLE_INPUT_SYSTEM
    // PC
    if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        return true;

    // VR – Quest (B button)
    if (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame)
        return true;
#endif
    return false;
}
    // ============================
    // HELPERS
    // ============================
    private IEnumerator Fade(float from, float to)
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            rootGroup.alpha = Mathf.Lerp(from, to, t / fadeDuration);
            yield return null;
        }
        rootGroup.alpha = to;
    }

    private void EnsurePages()
    {
        if (pages.Count > 0)
            return;

        pages.Add("FIIVerse îți oferă un tur rapid.");
        pages.Add("Vorbește cu profesorii și colectează semnături.");
        pages.Add("Succes!");
    }

    private static T Create<T>(string name, Transform parent) where T : Component
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.AddComponent<T>();
    }

    private static TMP_Text CreateText(string name, Transform parent, float size, FontStyles style)
    {
        var text = Create<TextMeshProUGUI>(name, parent);
        text.fontSize = size;
        text.fontStyle = style;
        text.color = Color.white;
        return text;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
