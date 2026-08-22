using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    [Header("Fade")]
    [SerializeField] private CanvasGroup fadeCanvas;
    [SerializeField] private float fadeToBlackDuration = 0.5f;

    private bool isTransitioning;

    private void Awake()
    {
        // This manager is responsible for fading OUT
        // the scene it belongs to.
        if (fadeCanvas != null)
        {
            fadeCanvas.alpha = 0f;
        }
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

        yield return FadeToBlack();

        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator TransitionRoutine(int sceneBuildIndex)
    {
        isTransitioning = true;

        yield return FadeToBlack();

        SceneManager.LoadScene(sceneBuildIndex);
    }

    private IEnumerator FadeToBlack()
    {
        if (fadeCanvas == null)
        {
            Debug.LogError(
                "SceneTransitionManager: Fade CanvasGroup is not assigned.",
                this
            );

            yield break;
        }

        float startAlpha = fadeCanvas.alpha;
        float elapsed = 0f;

        if (fadeToBlackDuration <= 0f)
        {
            fadeCanvas.alpha = 1f;
            yield break;
        }

        while (elapsed < fadeToBlackDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(
                elapsed / fadeToBlackDuration
            );

            fadeCanvas.alpha = Mathf.Lerp(
                startAlpha,
                1f,
                t
            );

            yield return null;
        }

        fadeCanvas.alpha = 1f;
    }
}