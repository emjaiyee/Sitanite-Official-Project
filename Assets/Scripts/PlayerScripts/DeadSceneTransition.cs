using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DeadSceneTransition : MonoBehaviour
{
    [Header("Destination")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Timing")]
    [Min(5f)]
    [SerializeField] private float deathDelay = 5f;

    [Min(0f)]
    [SerializeField] private float fadeToBlackDuration = 0.5f;

    [Header("Fade")]
    [SerializeField] private CanvasGroup fadeCanvas;

    [Header("Death Vignette")]
    [SerializeField] private Image deathVignetteImage;

    private PlayerStats playerStats;
    private bool isTransitioning;

    private void Awake()
    {
        if (fadeCanvas != null)
            fadeCanvas.alpha = 0f;

        if (deathVignetteImage != null)
            deathVignetteImage.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        ResolvePlayerStats();

        if (playerStats != null)
            playerStats.Died += HandlePlayerDied;
    }

    private void OnDisable()
    {
        if (playerStats != null)
            playerStats.Died -= HandlePlayerDied;
    }

    private void ResolvePlayerStats()
    {
        if (Player.Instance == null)
        {
            Debug.LogError(
                "DeadSceneTransition: Player.Instance was not found.",
                this
            );
            return;
        }

        playerStats = Player.Instance.GetComponent<PlayerStats>();

        if (playerStats == null)
        {
            Debug.LogError(
                "DeadSceneTransition: PlayerStats was not found on Player.Instance.",
                this
            );
        }
    }

    private void HandlePlayerDied()
    {
        if (isTransitioning)
            return;

        if (deathVignetteImage != null)
            deathVignetteImage.gameObject.SetActive(true);

        StartCoroutine(TransitionToMainMenu());
    }

    private IEnumerator TransitionToMainMenu()
    {
        isTransitioning = true;

        yield return new WaitForSecondsRealtime(
            Mathf.Max(5f, deathDelay)
        );

        if (string.IsNullOrWhiteSpace(mainMenuSceneName))
        {
            Debug.LogError(
                "DeadSceneTransition: Main menu scene name is empty.",
                this
            );
            yield break;
        }

        if (fadeCanvas == null)
        {
            Debug.LogError(
                "DeadSceneTransition: Fade CanvasGroup is not assigned.",
                this
            );
            yield break;
        }

        yield return FadeToBlack();

        if (Player.Instance != null)
        {
            Destroy(Player.Instance.gameObject);
            yield return null;
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }

    private IEnumerator FadeToBlack()
    {
        if (fadeToBlackDuration <= 0f)
        {
            fadeCanvas.alpha = 1f;
            yield break;
        }

        float startAlpha = fadeCanvas.alpha;
        float elapsed = 0f;

        while (elapsed < fadeToBlackDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            fadeCanvas.alpha = Mathf.Lerp(
                startAlpha,
                1f,
                elapsed / fadeToBlackDuration
            );

            yield return null;
        }

        fadeCanvas.alpha = 1f;
    }
}