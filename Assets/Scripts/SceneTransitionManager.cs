using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    [Header("Fade")]
    [SerializeField] private CanvasGroup fadeCanvas;

    [SerializeField]
    private float fadeToBlackDuration = 0.5f;

    [SerializeField]
    private float fadeFromBlackDuration = 0.5f;

    private bool isTransitioning;

    private void Awake()
    {
        // Every scene starts completely black.
        if (fadeCanvas != null)
        {
            fadeCanvas.alpha = 1f;
        }
    }

    private void Start()
    {
        // Automatically reveal the scene.
        StartCoroutine(
            Fade(
                0f,
                fadeFromBlackDuration
            )
        );
    }

    public void TransitionToScene(string sceneName)
    {
        if (isTransitioning)
            return;

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError(
                "SceneTransitionManager: Scene name is empty.",
                this
            );

            return;
        }

        StartCoroutine(
            TransitionRoutine(sceneName)
        );
    }

    public void TransitionToScene(int sceneBuildIndex)
    {
        if (isTransitioning)
            return;

        StartCoroutine(
            TransitionRoutine(sceneBuildIndex)
        );
    }

    private IEnumerator TransitionRoutine(string sceneName)
    {
        isTransitioning = true;

        // Fade current scene to black.
        yield return Fade(
            1f,
            fadeToBlackDuration
        );

        // Load next scene.
        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator TransitionRoutine(int sceneBuildIndex)
    {
        isTransitioning = true;

        // Fade current scene to black.
        yield return Fade(
            1f,
            fadeToBlackDuration
        );

        // Load next scene.
        SceneManager.LoadScene(sceneBuildIndex);
    }

    private IEnumerator Fade(
        float targetAlpha,
        float duration)
    {
        if (fadeCanvas == null)
        {
            Debug.LogError(
                "SceneTransitionManager: Fade CanvasGroup is not assigned.",
                this
            );

            yield break;
        }

        float startAlpha =
            fadeCanvas.alpha;

        float elapsed = 0f;

        if (duration <= 0f)
        {
            fadeCanvas.alpha =
                targetAlpha;

            yield break;
        }

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / duration
                );

            fadeCanvas.alpha =
                Mathf.Lerp(
                    startAlpha,
                    targetAlpha,
                    t
                );

            yield return null;
        }

        fadeCanvas.alpha =
            targetAlpha;
    }
}