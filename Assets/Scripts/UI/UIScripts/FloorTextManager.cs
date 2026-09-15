using System.Collections;
using TMPro;
using UnityEngine;

public class FloorTextManager : MonoBehaviour
{
    [Header("Animation")]
    [Min(0f)]
    [SerializeField] private float transitionDuration = 0.8f;
    [Min(0f)]
    [SerializeField] private float centerHoldDuration = 1.5f;
    [Min(1f)]
    [SerializeField] private float announcementScale = 2f;
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Scene References")]
    [Tooltip("Scene-local view whose authored RectTransform is the resting top-screen position.")]
    [SerializeField] private FloorTextView floorTextView;

    private TextMeshProUGUI floorText;
    private RectTransform textRect;
    private Vector2 restingPosition;
    private Vector3 restingWorldPosition;
    private Vector3 restingScale;
    private Vector2 restingAnchorMin;
    private Vector2 restingAnchorMax;
    private Vector2 restingPivot;
    private Coroutine transitionRoutine;

    public static void ShowFloor(int floorNumber)
    {
        FloorTextManager manager = FindFirstObjectByType<FloorTextManager>();

        if (manager == null)
        {
            Debug.LogWarning(
                "FloorTextManager could not find a scene-local instance."
            );
            return;
        }

        manager.ShowFloorText(floorNumber);
    }

    private void Awake()
    {
        if (floorTextView == null)
            floorTextView = FindFirstObjectByType<FloorTextView>();

        if (floorTextView == null)
        {
            Debug.LogError(
                "FloorTextManager requires a scene-local FloorTextView."
            );
            return;
        }

        floorText = floorTextView.FloorText;
        textRect = floorTextView.TextRect;

        if (floorText == null || textRect == null)
        {
            Debug.LogError(
                "FloorTextView requires a TextMeshProUGUI and RectTransform."
            );
            return;
        }

        restingPosition = textRect.anchoredPosition;
        restingScale = textRect.localScale;
        restingAnchorMin = textRect.anchorMin;
        restingAnchorMax = textRect.anchorMax;
        restingPivot = textRect.pivot;
    }

    public void ShowFloorText(int floorNumber)
    {
        if (floorText == null || textRect == null)
            return;

        floorText.text = $"Floor: {floorNumber}";
        restingWorldPosition = textRect.position;

        if (transitionRoutine != null)
            StopCoroutine(transitionRoutine);

        transitionRoutine = StartCoroutine(TransitionRoutine());
    }

    private IEnumerator TransitionRoutine()
    {
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = Vector2.zero;
        textRect.localScale = restingScale * announcementScale;
        Vector3 announcementPosition = textRect.position;

        yield return WaitFor(centerHoldDuration);

        if (transitionDuration <= 0f)
        {
            RestoreRestingTransform();
            transitionRoutine = null;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / transitionDuration);
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);

            textRect.position = Vector3.Lerp(
                announcementPosition,
                restingWorldPosition,
                easedProgress
            );
            textRect.localScale = Vector3.Lerp(
                restingScale * announcementScale,
                restingScale,
                easedProgress
            );
            yield return null;
        }

        RestoreRestingTransform();
        transitionRoutine = null;
    }

    private IEnumerator WaitFor(float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += useUnscaledTime ?
                Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;
        }
    }

    private void RestoreRestingTransform()
    {
        textRect.anchorMin = restingAnchorMin;
        textRect.anchorMax = restingAnchorMax;
        textRect.pivot = restingPivot;
        textRect.anchoredPosition = restingPosition;
        textRect.localScale = restingScale;
    }
}