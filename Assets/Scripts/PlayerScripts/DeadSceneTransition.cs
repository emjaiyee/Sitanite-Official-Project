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

    [Header("Death Results")]
    [SerializeField] private DeathResultsPanel deathResults;
    [Range(0f, 1f)] [SerializeField] private float reanimationChance = 0.5f;

    [Header("Dungeon Memory")]
    [SerializeField] private DungeonMemory dungeonMemory;
    [SerializeField] private FloorManager floorManager;

    private PlayerStats playerStats;
    private bool isTransitioning;
    private Player.RunStatistics finalStatistics;
    private string finalCharacterName;
    private bool reanimated;

    private void Awake()
    {
        if (fadeCanvas != null)
            fadeCanvas.alpha = 0f;

        if (deathVignetteImage != null)
            deathVignetteImage.gameObject.SetActive(false);

        if (deathResults == null)
            deathResults = FindFirstObjectByType<DeathResultsPanel>(FindObjectsInactive.Include);
        if (deathResults != null)
            deathResults.Hide();
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

        isTransitioning = true;
        if (Player.Instance != null)
            finalStatistics = Player.Instance.FinishRun();
        finalCharacterName = playerStats != null ? playerStats.CharacterName : null;

        reanimated = Random.value < reanimationChance;

        if (dungeonMemory == null)
            dungeonMemory = FindFirstObjectByType<DungeonMemory>();
        if (floorManager == null)
            floorManager = FindFirstObjectByType<FloorManager>();

        if (dungeonMemory != null && floorManager != null)
        {
            dungeonMemory.RememberDeath(
                Player.Instance, playerStats, floorManager.CurrentFloor,
                reanimated ? DungeonMemory.DeathOutcome.Reanimated : DungeonMemory.DeathOutcome.Dead
            );
        }
        else
            Debug.LogWarning("DeadSceneTransition: Dungeon memory or floor manager is missing; death was not saved.", this);

        PlayerInventory inventory = Player.Instance != null
            ? Player.Instance.GetComponentInChildren<PlayerInventory>(true)
            : null;
        if (inventory != null)
        {
            inventory.SetInventoryState(false);
            inventory.enabled = false;
        }

        PlayerStatsUI statsUI = FindFirstObjectByType<PlayerStatsUI>(FindObjectsInactive.Include);
        if (statsUI != null)
        {
            statsUI.SetStatsWindowState(false);
            statsUI.enabled = false;
        }

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

        if (deathVignetteImage != null)
            deathVignetteImage.gameObject.SetActive(false);

        if (deathResults != null)
            yield return deathResults.Show(finalStatistics, reanimated, fadeCanvas, finalCharacterName);
        else
            Debug.LogWarning("DeadSceneTransition: Death results panel is not assigned.", this);

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