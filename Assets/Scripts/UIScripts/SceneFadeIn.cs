using System.Collections;
using UnityEngine;

public class SceneFadeIn : MonoBehaviour
{
    [Header("Fade")]
    [SerializeField] private CanvasGroup fadeCanvas;
    [SerializeField] private float fadeDuration = 0.5f;

    private void Awake()
    {
        // The scene begins completely black.
        if (fadeCanvas != null)
        {
            fadeCanvas.alpha = 1f;
        }
    }

    private void Start()
    {
        StartCoroutine(FadeIn());
    }

    private IEnumerator FadeIn()
    {
        if (fadeCanvas == null)
        {
            Debug.LogError(
                "SceneFadeIn: Fade CanvasGroup is not assigned.",
                this
            );

            yield break;
        }

        float elapsed = 0f;

        if (fadeDuration <= 0f)
        {
            fadeCanvas.alpha = 0f;
            yield break;
        }

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(
                elapsed / fadeDuration
            );

            fadeCanvas.alpha = Mathf.Lerp(
                1f,
                0f,
                t
            );

            yield return null;
        }

        fadeCanvas.alpha = 0f;
    }
}