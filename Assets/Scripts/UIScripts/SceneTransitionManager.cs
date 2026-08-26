using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string sceneName;

    [Header("Fade")]
    [SerializeField] private CanvasGroup fadeCanvas;
    [SerializeField] private float fadeToBlackDuration = 0.5f;
    [SerializeField] private float fadeToBlackSpeed = 2f;

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

    public void TransitionToScene()
    {
        TransitionToScene(sceneName);
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

    public void QuitGame()
    {
        if (isTransitioning)
            return;

        StartCoroutine(QuitRoutine());
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

    private IEnumerator QuitRoutine()
    {
        isTransitioning = true;

        yield return FadeToBlack();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
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

        if (fadeToBlackDuration <= 0f || fadeToBlackSpeed <= 0f)
        {
            fadeCanvas.alpha = 1f;
            yield break;
        }

        while (elapsed < fadeToBlackDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            fadeCanvas.alpha = Mathf.MoveTowards(
                fadeCanvas.alpha,
                1f,
                fadeToBlackSpeed * Time.unscaledDeltaTime
            );

            if (fadeCanvas.alpha >= 1f)
                break;

            yield return null;
        }

        fadeCanvas.alpha = 1f;
    }
}