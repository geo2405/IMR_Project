using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BonkFeedbackController : MonoBehaviour
{
    public static BonkFeedbackController Instance { get; private set; }

    [Header("Layout")]
    public Vector2 panelSize = new Vector2(520f, 360f);
    public Vector2 imageSize = new Vector2(260f, 260f);
    public float textSize = 36f;

    [Header("Colors")]
    public Color backgroundColor = new Color(0f, 0f, 0f, 0.45f);
    public Color panelColor = new Color(0.08f, 0.1f, 0.12f, 0.9f);
    public Color textColor = Color.white;

    [Header("Animation")]
    public float fadeIn = 0.12f;
    public float hold = 0.6f;
    public float fadeOut = 0.18f;
    public float punchScale = 1.08f;

    private CanvasGroup rootGroup;
    private RectTransform panelRect;
    private Image bonkImage;
    private TMP_Text bonkText;
    private Coroutine routine;

    public static BonkFeedbackController EnsureExists()
    {
        if (Instance != null)
            return Instance;

        var go = new GameObject("BonkFeedback");
        return go.AddComponent<BonkFeedbackController>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildUiIfNeeded();
        HideImmediate();
    }

    public void Show(Sprite sprite, string message, AudioClip sfx)
    {
        BuildUiIfNeeded();

        if (bonkImage != null)
        {
            bonkImage.sprite = sprite;
            bonkImage.enabled = sprite != null;
        }

        if (bonkText != null)
            bonkText.text = string.IsNullOrWhiteSpace(message) ? "Nice try" : message;

        if (sfx != null)
            AudioManager.EnsureExists().PlayOneShot(sfx);

        if (routine != null)
            StopCoroutine(routine);
        routine = StartCoroutine(PlaySequence());
    }

    private IEnumerator PlaySequence()
    {
        rootGroup.alpha = 0f;
        panelRect.localScale = Vector3.one * 0.9f;
        gameObject.SetActive(true);

        yield return Fade(0f, 1f, fadeIn);
        yield return PunchScale();
        yield return new WaitForSeconds(hold);
        yield return Fade(1f, 0f, fadeOut);

        HideImmediate();
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        float time = 0f;
        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            var t = duration > 0f ? Mathf.Clamp01(time / duration) : 1f;
            rootGroup.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        rootGroup.alpha = to;
    }

    private IEnumerator PunchScale()
    {
        var time = 0f;
        var duration = 0.18f;
        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            var t = Mathf.Clamp01(time / duration);
            var eased = Mathf.Sin(t * Mathf.PI);
            var scale = Mathf.Lerp(1f, punchScale, eased);
            panelRect.localScale = Vector3.one * scale;
            yield return null;
        }

        panelRect.localScale = Vector3.one;
    }

    private void HideImmediate()
    {
        if (rootGroup != null)
            rootGroup.alpha = 0f;
    }

    private void BuildUiIfNeeded()
    {
        if (rootGroup != null)
            return;

        var canvas = gameObject.GetComponent<Canvas>();
        if (canvas == null)
            canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1600;

        var scaler = gameObject.GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        if (gameObject.GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();

        rootGroup = gameObject.GetComponent<CanvasGroup>();
        if (rootGroup == null)
            rootGroup = gameObject.AddComponent<CanvasGroup>();
        rootGroup.blocksRaycasts = false;
        rootGroup.interactable = false;

        var backdrop = CreateUi<Image>("Backdrop", transform);
        backdrop.color = backgroundColor;
        var backdropRect = backdrop.rectTransform;
        backdropRect.anchorMin = Vector2.zero;
        backdropRect.anchorMax = Vector2.one;
        backdropRect.offsetMin = Vector2.zero;
        backdropRect.offsetMax = Vector2.zero;

        var panel = CreateUi<Image>("Panel", transform);
        panel.color = panelColor;
        panelRect = panel.rectTransform;
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = panelSize;
        panelRect.anchoredPosition = Vector2.zero;

        var outline = panel.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.45f);
        outline.effectDistance = new Vector2(3f, -3f);

        bonkImage = CreateUi<Image>("BonkImage", panel.transform);
        var imageRect = bonkImage.rectTransform;
        imageRect.anchorMin = new Vector2(0.5f, 0.6f);
        imageRect.anchorMax = new Vector2(0.5f, 0.6f);
        imageRect.pivot = new Vector2(0.5f, 0.5f);
        imageRect.sizeDelta = imageSize;
        imageRect.anchoredPosition = Vector2.zero;
        bonkImage.enabled = false;

        bonkText = CreateText("BonkText", panel.transform, (int)textSize, FontStyles.Bold);
        var textRect = bonkText.rectTransform;
        textRect.anchorMin = new Vector2(0.5f, 0.1f);
        textRect.anchorMax = new Vector2(0.5f, 0.1f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = new Vector2(0f, 0f);
        bonkText.alignment = TextAlignmentOptions.Center;
        bonkText.color = textColor;
        bonkText.text = "Nice try";
    }

    private static T CreateUi<T>(string name, Transform parent) where T : Component
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(T));
        go.transform.SetParent(parent, false);
        return go.GetComponent<T>();
    }

    private static TMP_Text CreateText(string name, Transform parent, int size, FontStyles style)
    {
        var text = CreateUi<TextMeshProUGUI>(name, parent);
        text.text = name;
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
        return text;
    }
}
