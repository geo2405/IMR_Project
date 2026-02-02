using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AchievementBadgeController : MonoBehaviour
{
    public static AchievementBadgeController Instance { get; private set; }

    [Header("Layout")]
    public Vector2 badgeSize = new Vector2(360f, 120f);
    public Vector2 badgeOffset = new Vector2(-40f, -40f);
    public float iconSize = 64f;

    [Header("Colors")]
    public Color backgroundColor = new Color(0f, 0f, 0f, 0.6f);
    public Color badgeColor = new Color(0.12f, 0.14f, 0.18f, 0.95f);
    public Color titleColor = Color.white;
    public Color subtitleColor = new Color(1f, 1f, 1f, 0.85f);

    [Header("Animation")]
    public float fadeInDuration = 0.25f;
    public float holdDuration = 1.8f;
    public float autoHideDelay = 10f;
    public float fadeOutDuration = 0.3f;
    public bool keepBadgeVisible = true;
    public bool useUnscaledTime = true;

    [Header("Fancy")]
    public float slideDistance = 60f;
    public float pulseAmount = 0.02f;
    public float pulseSpeed = 2.2f;
    public Color accentColor = new Color(1f, 0.78f, 0.2f, 1f);

    private CanvasGroup backgroundGroup;
    private CanvasGroup badgeGroup;
    private RectTransform badgeRect;
    private Vector2 badgeTargetPosition;
    private TMP_Text titleText;
    private TMP_Text subtitleText;
    private Image iconImage;
    private Image accentImage;
    private Coroutine currentRoutine;

    public static AchievementBadgeController EnsureExists()
    {
        if (Instance != null)
            return Instance;

        var go = new GameObject("AchievementBadge");
        return go.AddComponent<AchievementBadgeController>();
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

    public void Show(string title, string subtitle, Sprite icon, bool keepBadge)
    {
        BuildUiIfNeeded();
        keepBadgeVisible = keepBadge;

        if (titleText != null)
            titleText.text = title;
        if (subtitleText != null)
            subtitleText.text = subtitle;

        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }

        if (currentRoutine != null)
            StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(ShowSequence());
    }

    private IEnumerator ShowSequence()
    {
        backgroundGroup.alpha = 0f;
        badgeGroup.alpha = 0f;
        badgeRect.localScale = Vector3.one * 0.9f;
        badgeRect.anchoredPosition = badgeTargetPosition + new Vector2(slideDistance, 0f);
        gameObject.SetActive(true);

        yield return FadeGroups(0f, 1f, fadeInDuration, scaleUp: true, moveToTarget: true);

        var waitTime = keepBadgeVisible ? holdDuration : autoHideDelay;
        if (waitTime > 0f)
            yield return HoldWithPulse(waitTime);

        if (keepBadgeVisible)
        {
            yield return FadeBackgroundOnly(1f, 0f, fadeOutDuration);
            badgeGroup.alpha = 1f;
            badgeRect.localScale = Vector3.one;
        }
        else
        {
            yield return FadeGroups(1f, 0f, fadeOutDuration, scaleUp: false, moveToTarget: false);
            HideImmediate();
        }
    }

    private IEnumerator FadeGroups(float from, float to, float duration, bool scaleUp, bool moveToTarget)
    {
        float time = 0f;
        var startPos = badgeRect.anchoredPosition;
        while (time < duration)
        {
            time += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            var t = duration > 0f ? Mathf.Clamp01(time / duration) : 1f;
            var ease = t * t * (3f - 2f * t);
            var alpha = Mathf.Lerp(from, to, t);
            backgroundGroup.alpha = alpha;
            badgeGroup.alpha = alpha;
            badgeRect.localScale = Vector3.one * (scaleUp ? Mathf.Lerp(0.9f, 1f, ease) : Mathf.Lerp(1f, 0.95f, ease));
            if (moveToTarget)
                badgeRect.anchoredPosition = Vector2.Lerp(startPos, badgeTargetPosition, ease);
            yield return null;
        }

        if (moveToTarget)
            badgeRect.anchoredPosition = badgeTargetPosition;
    }

    private IEnumerator FadeBackgroundOnly(float from, float to, float duration)
    {
        float time = 0f;
        while (time < duration)
        {
            time += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            var t = duration > 0f ? Mathf.Clamp01(time / duration) : 1f;
            backgroundGroup.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        backgroundGroup.alpha = to;
    }

    private void HideImmediate()
    {
        if (backgroundGroup != null)
            backgroundGroup.alpha = 0f;
        if (badgeGroup != null)
            badgeGroup.alpha = 0f;
    }

    private IEnumerator HoldWithPulse(float duration)
    {
        float time = 0f;
        while (time < duration)
        {
            time += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            var pulse = 1f + Mathf.Sin(time * pulseSpeed * Mathf.PI * 2f) * pulseAmount;
            badgeRect.localScale = Vector3.one * pulse;
            yield return null;
        }

        badgeRect.localScale = Vector3.one;
    }

    private void BuildUiIfNeeded()
    {
        if (backgroundGroup != null && badgeGroup != null)
            return;

        var canvas = gameObject.GetComponent<Canvas>();
        if (canvas == null)
            canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1500;

        var scaler = gameObject.GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        if (gameObject.GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();

        var background = CreateUi<Image>("AchievementBackground", transform);
        background.color = backgroundColor;
        var backgroundRect = background.rectTransform;
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;

        backgroundGroup = background.gameObject.GetComponent<CanvasGroup>();
        if (backgroundGroup == null)
            backgroundGroup = background.gameObject.AddComponent<CanvasGroup>();
        backgroundGroup.blocksRaycasts = false;
        backgroundGroup.interactable = false;

        var badge = CreateUi<Image>("AchievementBadge", transform);
        badge.color = badgeColor;
        badgeRect = badge.rectTransform;
        badgeRect.anchorMin = new Vector2(1f, 1f);
        badgeRect.anchorMax = new Vector2(1f, 1f);
        badgeRect.pivot = new Vector2(1f, 1f);
        badgeRect.sizeDelta = badgeSize;
        badgeRect.anchoredPosition = badgeOffset;
        badgeTargetPosition = badgeOffset;

        var outline = badge.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.45f);
        outline.effectDistance = new Vector2(2f, -2f);

        var shadow = badge.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.25f);
        shadow.effectDistance = new Vector2(4f, -4f);

        badgeGroup = badge.gameObject.GetComponent<CanvasGroup>();
        if (badgeGroup == null)
            badgeGroup = badge.gameObject.AddComponent<CanvasGroup>();
        badgeGroup.blocksRaycasts = false;
        badgeGroup.interactable = false;

        accentImage = CreateUi<Image>("Accent", badge.transform);
        var accentRect = accentImage.rectTransform;
        accentRect.anchorMin = new Vector2(0f, 0f);
        accentRect.anchorMax = new Vector2(0f, 1f);
        accentRect.pivot = new Vector2(0f, 0.5f);
        accentRect.sizeDelta = new Vector2(6f, 0f);
        accentRect.anchoredPosition = new Vector2(0f, 0f);
        accentImage.color = accentColor;
        accentImage.raycastTarget = false;

        iconImage = CreateUi<Image>("Icon", badge.transform);
        var iconRect = iconImage.rectTransform;
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.sizeDelta = new Vector2(iconSize, iconSize);
        iconRect.anchoredPosition = new Vector2(18f, 0f);
        iconImage.enabled = false;

        titleText = CreateText("Title", badge.transform, 24, FontStyles.Bold);
        var titleRect = titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 0.65f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.offsetMin = new Vector2(16f + iconSize + 12f, -8f);
        titleRect.offsetMax = new Vector2(-16f, -8f);
        titleText.color = titleColor;
        titleText.text = "Achievement Deblocat!";

        subtitleText = CreateText("Subtitle", badge.transform, 18, FontStyles.Normal);
        var subtitleRect = subtitleText.rectTransform;
        subtitleRect.anchorMin = new Vector2(0f, 0f);
        subtitleRect.anchorMax = new Vector2(1f, 0.65f);
        subtitleRect.offsetMin = new Vector2(16f + iconSize + 12f, 12f);
        subtitleRect.offsetMax = new Vector2(-16f, -8f);
        subtitleText.color = subtitleColor;
        subtitleText.text = "Ai colectat 3/3 semnături!";
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
        text.alignment = TextAlignmentOptions.Left;
        text.enableWordWrapping = false;
        return text;
    }
}
