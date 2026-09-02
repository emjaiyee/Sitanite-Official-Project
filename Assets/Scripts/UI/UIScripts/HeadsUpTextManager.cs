using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HeadsUpTextManager : MonoBehaviour, IHeadsUpText
{
    public static IHeadsUpText Current { get; private set; }

    [Header("Timing")]
    [Min(0f)]
    [SerializeField] private float fadeInDuration = 0.25f;
    [Min(0f)]
    [SerializeField] private float holdDuration = 3f;
    [Min(0f)]
    [SerializeField] private float fadeOutDuration = 0.25f;

    [Header("Layout")]
    [SerializeField] private Vector2 anchoredPosition = new Vector2(0f, -90f);
    [SerializeField] private Vector2 padding = new Vector2(36f, 18f);
    [Min(100f)]
    [SerializeField] private float maxWidth = 900f;
    [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.8f);
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private TMP_FontAsset fontAsset;
    [Min(1)]
    [SerializeField] private int fontSize = 32;
    [SerializeField] private int sortingOrder = 2000;
    [SerializeField] private bool useUnscaledTime = true;
    [Tooltip("Optional prefab that contains the actual canvas, background image, and TMP text. Leave empty to build the UI at runtime.")]
    [SerializeField] private HeadsUpTextView headsUpTextPrefab;

    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private Image backgroundImage;
    private TextMeshProUGUI messageText;
    private RectTransform rootRect;
    private RectTransform messageRect;
    private HeadsUpTextView runtimeView;
    private readonly Queue<string> pendingMessages = new Queue<string>();
    private Coroutine playbackRoutine;

    public static void Show(string message)
    {
        EnsureInstance()?.ShowText(message);
    }

    public static void Hide()
    {
        if (Current == null)
            return;

        Current.HideText();
    }

    private static HeadsUpTextManager EnsureInstance()
    {
        if (Current is HeadsUpTextManager manager)
            return manager;

        HeadsUpTextManager existing = FindFirstObjectByType<HeadsUpTextManager>();
        if (existing != null)
            return existing;

        GameObject managerObject = new GameObject(nameof(HeadsUpTextManager));
        return managerObject.AddComponent<HeadsUpTextManager>();
    }

    private void Awake()
    {
        if ((object)Current != null && !ReferenceEquals(Current, this))
        {
            Destroy(gameObject);
            return;
        }

        Current = this;
        DontDestroyOnLoad(gameObject);
        EnsureUi();
        SetVisible(false);
    }

    private void OnDestroy()
    {
        if (ReferenceEquals(Current, this))
            Current = null;
    }

    public void ShowText(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        pendingMessages.Enqueue(message);

        if (playbackRoutine == null)
            playbackRoutine = StartCoroutine(PlaybackRoutine());
    }

    public void HideText()
    {
        pendingMessages.Clear();

        if (playbackRoutine != null)
        {
            StopCoroutine(playbackRoutine);
            playbackRoutine = null;
        }

        SetVisible(false);
    }

    private IEnumerator PlaybackRoutine()
    {
        while (pendingMessages.Count > 0)
        {
            string message = pendingMessages.Dequeue();
            SetMessage(message);

            yield return FadeTo(1f, fadeInDuration);
            yield return WaitFor(holdDuration);
            yield return FadeTo(0f, fadeOutDuration);
        }

        playbackRoutine = null;
    }

    private void EnsureUi()
    {
        if (headsUpTextPrefab != null)
        {
            runtimeView = Instantiate(headsUpTextPrefab, transform);
            runtimeView.transform.localPosition = Vector3.zero;
            runtimeView.transform.localRotation = Quaternion.identity;
            runtimeView.transform.localScale = Vector3.one;

            canvas = runtimeView.GetComponent<Canvas>();
            if (canvas == null)
                canvas = runtimeView.gameObject.AddComponent<Canvas>();

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            CanvasScaler scaler = runtimeView.GetComponent<CanvasScaler>();
            if (scaler == null)
                scaler = runtimeView.gameObject.AddComponent<CanvasScaler>();

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            if (runtimeView.GetComponent<GraphicRaycaster>() == null)
                runtimeView.gameObject.AddComponent<GraphicRaycaster>();

            canvasGroup = runtimeView.CanvasGroup;
            if (canvasGroup == null)
                canvasGroup = runtimeView.gameObject.AddComponent<CanvasGroup>();

            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            backgroundImage = runtimeView.BackgroundImage;
            if (backgroundImage != null)
            {
                backgroundImage.color = backgroundColor;
                backgroundImage.raycastTarget = false;
            }

            rootRect = runtimeView.BackgroundRect;
            messageText = runtimeView.MessageText;

            if (messageText != null)
            {
                messageRect = messageText.rectTransform;
                messageRect.anchorMin = Vector2.zero;
                messageRect.anchorMax = Vector2.one;
                messageRect.offsetMin = padding;
                messageRect.offsetMax = -padding;

                messageText.alignment = TextAlignmentOptions.Center;
                messageText.textWrappingMode = TextWrappingModes.Normal;
                messageText.raycastTarget = false;
                messageText.color = textColor;
                messageText.fontSize = fontSize;

                if (fontAsset != null)
                    messageText.font = fontAsset;
            }

            return;
        }

        canvas = GetComponent<Canvas>();
        if (canvas == null)
            canvas = gameObject.AddComponent<Canvas>();

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scalerFallback = GetComponent<CanvasScaler>();
        if (scalerFallback == null)
            scalerFallback = gameObject.AddComponent<CanvasScaler>();

        scalerFallback.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scalerFallback.referenceResolution = new Vector2(1920f, 1080f);
        scalerFallback.matchWidthOrHeight = 0.5f;

        if (GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        rootRect = GetComponent<RectTransform>();
        if (rootRect == null)
            rootRect = gameObject.AddComponent<RectTransform>();

        rootRect.anchorMin = new Vector2(0.5f, 1f);
        rootRect.anchorMax = new Vector2(0.5f, 1f);
        rootRect.pivot = new Vector2(0.5f, 1f);
        rootRect.anchoredPosition = anchoredPosition;

        backgroundImage = GetComponent<Image>();
        if (backgroundImage == null)
            backgroundImage = gameObject.AddComponent<Image>();

        backgroundImage.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
        backgroundImage.type = Image.Type.Sliced;
        backgroundImage.color = backgroundColor;
        backgroundImage.raycastTarget = false;

        messageText = GetComponentInChildren<TextMeshProUGUI>(true);
        if (messageText == null)
        {
            GameObject textObject = new GameObject("HeadsUpText");
            textObject.transform.SetParent(transform, false);
            messageText = textObject.AddComponent<TextMeshProUGUI>();
        }

        messageRect = messageText.rectTransform;
        messageRect.SetParent(transform, false);
        messageRect.anchorMin = Vector2.zero;
        messageRect.anchorMax = Vector2.one;
        messageRect.offsetMin = padding;
        messageRect.offsetMax = -padding;

        messageText.alignment = TextAlignmentOptions.Center;
        messageText.textWrappingMode = TextWrappingModes.Normal;
        messageText.raycastTarget = false;
        messageText.color = textColor;
        messageText.fontSize = fontSize;

        if (fontAsset != null)
            messageText.font = fontAsset;
    }

    private void SetMessage(string message)
    {
        if (messageText == null || backgroundImage == null)
            return;

        messageText.text = message;
        float contentWidth = Mathf.Max(0f, maxWidth - padding.x * 2f);
        messageRect.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Horizontal,
            contentWidth
        );

        messageText.ForceMeshUpdate();

        float preferredWidth = Mathf.Min(contentWidth, messageText.preferredWidth);
        float preferredHeight = messageText.preferredHeight;

        float width = Mathf.Clamp(
            Mathf.Max(preferredWidth, 240f) + padding.x * 2f,
            240f,
            maxWidth
        );

        float height = Mathf.Max(
            preferredHeight + padding.y * 2f,
            56f
        );

        rootRect.sizeDelta = new Vector2(width, height);
        messageRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, contentWidth);
        messageRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, preferredHeight);
    }

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        if (canvasGroup == null)
            yield break;

        if (duration <= 0f)
        {
            canvasGroup.alpha = targetAlpha;
            yield break;
        }

        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

            float progress = Mathf.Clamp01(elapsed / duration);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, progress);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
    }

    private IEnumerator WaitFor(float duration)
    {
        if (duration <= 0f)
            yield break;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;
        }
    }

    private void SetVisible(bool visible)
    {
        if (canvasGroup != null)
            canvasGroup.alpha = visible ? 1f : 0f;
    }
}