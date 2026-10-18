using System.Collections;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class DeathResultsPanel : MonoBehaviour
{
    [Header("Scene UI")]
    [SerializeField] private Canvas resultsCanvas;
    [SerializeField] private CanvasGroup panel;
    [SerializeField] private TMP_Text characterNameText;
    [SerializeField] private TMP_Text killsText;
    [SerializeField] private TMP_Text miniBossKillsText;
    [SerializeField] private TMP_Text roomsClearedText;
    [SerializeField] private TMP_Text floorsEnteredText;
    [SerializeField] private TMP_Text outcomeText;

    [Header("Slam Animation")]
    [Min(1f)] [SerializeField] private float startingScale = 2.5f;
    [Range(0.5f, 1f)] [SerializeField] private float impactScale = 0.94f;
    [Min(0f)] [SerializeField] private float slamDuration = 0.22f;
    [Min(0f)] [SerializeField] private float settleDuration = 0.12f;
    [Min(0f)] [SerializeField] private float rowDelay = 0.15f;

    [Header("Presentation Timing")]
    [Min(0f)] [SerializeField] private float resultsHoldDuration = 2.5f;
    [Min(0f)] [SerializeField] private float panelFadeDuration = 0.5f;
    [Min(0f)] [SerializeField] private float outcomeFadeDuration = 0.75f;
    [Min(0f)] [SerializeField] private float outcomeHoldDuration = 2f;

    public void Hide()
    {
        if (resultsCanvas != null)
            resultsCanvas.enabled = false;
    }

    public IEnumerator Show(Player.RunStatistics statistics, bool reanimated, CanvasGroup blackFade, string characterName = null)
    {
        if (resultsCanvas == null || panel == null || outcomeText == null ||
            characterNameText == null || killsText == null || miniBossKillsText == null ||
            roomsClearedText == null || floorsEnteredText == null ||
            outcomeText.transform.IsChildOf(panel.transform))
        {
            Debug.LogError(
                "DeathResultsPanel: Assign the canvas, panel and all six TMP texts. " +
                "The outcome text must be outside the fading panel.", this
            );
            yield break;
        }

        Canvas backgroundCanvas = blackFade != null ? blackFade.GetComponentInParent<Canvas>() : null;
        if (backgroundCanvas == resultsCanvas)
        {
            Debug.LogError("DeathResultsPanel: Results require a separate canvas from the black fade.", this);
            yield break;
        }
        if (backgroundCanvas != null)
        {
            resultsCanvas.overrideSorting = true;
            resultsCanvas.sortingLayerID = backgroundCanvas.sortingLayerID;
            resultsCanvas.sortingOrder = backgroundCanvas.sortingOrder + 1;
        }

        TMP_Text[] rows = { characterNameText, killsText, miniBossKillsText, roomsClearedText, floorsEnteredText };
        string[] values =
        {
            $"Name: {(string.IsNullOrWhiteSpace(characterName) ? "Unknown" : characterName)}",
            $"Kills: {statistics.kills}",
            $"Mini-boss Kills: {statistics.miniBossKills}",
            $"Rooms Cleared: {statistics.roomsCleared}",
            $"Floors Entered: {statistics.floorsEntered}"
        };

        panel.gameObject.SetActive(true);
        panel.alpha = 0f;
        panel.interactable = false;
        panel.blocksRaycasts = false;
        outcomeText.gameObject.SetActive(true);
        outcomeText.alpha = 0f;

        for (int index = 0; index < rows.Length; index++)
        {
            rows[index].gameObject.SetActive(true);
            rows[index].text = values[index];
            rows[index].alpha = 0f;
        }

        resultsCanvas.gameObject.SetActive(true);
        resultsCanvas.enabled = true;
        Canvas.ForceUpdateCanvases();

        panel.alpha = 1f;
        yield return Slam(panel.transform);

        foreach (TMP_Text row in rows)
        {
            yield return new WaitForSecondsRealtime(rowDelay);
            row.alpha = 1f;
            yield return Slam(row.transform);
        }

        yield return new WaitForSecondsRealtime(resultsHoldDuration);
        yield return FadePanel();
        panel.gameObject.SetActive(false);

        outcomeText.text = reanimated ? "You are reanimated" : "You are dead";
        yield return FadeOutcome(0f, 1f);
        yield return new WaitForSecondsRealtime(outcomeHoldDuration);
        yield return FadeOutcome(1f, 0f);

        resultsCanvas.enabled = false;
    }

    private IEnumerator Slam(Transform target)
    {
        Vector3 originalScale = target.localScale;
        yield return Scale(target, originalScale * startingScale, originalScale * impactScale, slamDuration);
        yield return Scale(target, originalScale * impactScale, originalScale, settleDuration);
        target.localScale = originalScale;
    }

    private IEnumerator Scale(Transform target, Vector3 from, Vector3 to, float duration)
    {
        target.localScale = from;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            target.localScale = Vector3.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        target.localScale = to;
    }

    private IEnumerator FadePanel()
    {
        float elapsed = 0f;
        while (elapsed < panelFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            panel.alpha = 1f - Mathf.Clamp01(elapsed / panelFadeDuration);
            yield return null;
        }
        panel.alpha = 0f;
    }

    private IEnumerator FadeOutcome(float from, float to)
    {
        float elapsed = 0f;
        while (elapsed < outcomeFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            outcomeText.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / outcomeFadeDuration));
            yield return null;
        }
        outcomeText.alpha = to;
    }
}