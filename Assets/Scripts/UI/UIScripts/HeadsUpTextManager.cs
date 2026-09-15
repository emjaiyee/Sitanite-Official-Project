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

    [Header("Appearance")]
    [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.8f);
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private TMP_FontAsset fontAsset;
    [Min(1)]
    [SerializeField] private int fontSize = 32;
    [SerializeField] private bool useUnscaledTime = true;
    [Header("Scene References")]
    [Tooltip("Scene-local view whose RectTransform controls the notification position and size.")]
    [SerializeField] private HeadsUpTextView headsUpTextView;

    private CanvasGroup canvasGroup;
    private Image backgroundImage;
    private TextMeshProUGUI messageText;
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

        Debug.LogWarning(
            "HeadsUpTextManager could not find a scene-local instance. " +
            "Add one to the active scene and assign its HeadsUpTextView."
        );
        return null;
    }

    private void Awake()
    {
        if ((object)Current != null && !ReferenceEquals(Current, this))
        {
            Destroy(gameObject);
            return;
        }

        Current = this;
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
        if (headsUpTextView == null)
            headsUpTextView = FindFirstObjectByType<HeadsUpTextView>();

        if (headsUpTextView == null)
        {
            Debug.LogError(
                "HeadsUpTextManager requires a scene-local HeadsUpTextView."
            );
            return;
        }

        canvasGroup = headsUpTextView.CanvasGroup;
        backgroundImage = headsUpTextView.BackgroundImage;
        messageText = headsUpTextView.MessageText;

        if (canvasGroup == null || backgroundImage == null ||
            messageText == null)
        {
            Debug.LogError(
                "HeadsUpTextView is missing a CanvasGroup, Image, or " +
                "TextMeshProUGUI reference."
            );
            return;
        }

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        backgroundImage.color = backgroundColor;
        backgroundImage.raycastTarget = false;

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
        if (messageText == null)
            return;

        messageText.text = message;
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