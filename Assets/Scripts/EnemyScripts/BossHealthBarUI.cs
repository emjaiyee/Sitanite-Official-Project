using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BossHealthBarUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Slider healthSlider;
    [SerializeField, Min(0f)] private float fadeDuration = 0.2f;

    private EnemySpawnerManager spawnerManager;
    private EnemyHealth bossHealth;
    private RoomManager roomManager;
    private Coroutine fadeRoutine;

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (healthSlider == null)
            healthSlider = GetComponentInChildren<Slider>(true);

        SetAlphaImmediately(0f);
    }

    private void OnEnable()
    {
        BindToSpawner();
    }

    private void Update()
    {
        if (spawnerManager == null)
            BindToSpawner();

        if (spawnerManager != null &&
            spawnerManager.ActiveBossHealth != bossHealth)
            BindBoss(spawnerManager.ActiveBossHealth);

        UpdateVisibility();
    }

    private void OnDisable()
    {
        if (spawnerManager != null)
            spawnerManager.OnBossSpawned -= BindBoss;

        UnbindBoss();
        spawnerManager = null;

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }
    }

    private void BindToSpawner()
    {
        EnemySpawnerManager manager = EnemySpawnerManager.Instance;
        if (manager == null || manager == spawnerManager)
            return;

        if (spawnerManager != null)
            spawnerManager.OnBossSpawned -= BindBoss;

        spawnerManager = manager;
        spawnerManager.OnBossSpawned += BindBoss;
        BindBoss(spawnerManager.ActiveBossHealth);
    }

    private void BindBoss(EnemyHealth health)
    {
        if (health == bossHealth)
            return;

        UnbindBoss();
        bossHealth = health;

        if (bossHealth == null)
        {
            FadeTo(0f);
            return;
        }

        bossHealth.OnHealthChanged += Refresh;
        bossHealth.OnEnemyDied += HandleBossDied;
        Refresh(bossHealth);
        UpdateVisibility();
    }

    private void UnbindBoss()
    {
        if (bossHealth == null)
            return;

        bossHealth.OnHealthChanged -= Refresh;
        bossHealth.OnEnemyDied -= HandleBossDied;
        bossHealth = null;
    }

    private void Refresh(EnemyHealth health)
    {
        if (healthSlider == null)
            return;

        healthSlider.minValue = 0f;
        healthSlider.maxValue = 1f;
        healthSlider.SetValueWithoutNotify(
            health.MaxHealth <= 0
                ? 0f
                : Mathf.Clamp01((float)health.CurrentHealth / health.MaxHealth)
        );
    }

    private void HandleBossDied(GameObject deadBoss)
    {
        UnbindBoss();
        FadeTo(0f);
    }

    private void UpdateVisibility()
    {
        if (roomManager == null)
            roomManager = FindFirstObjectByType<RoomManager>();

        bool isInBossRoom =
            bossHealth != null &&
            spawnerManager != null &&
            spawnerManager.ActiveBossRoom != null &&
            roomManager != null &&
            roomManager.CurrentPlayerRoom == spawnerManager.ActiveBossRoom;

        FadeTo(isInBossRoom ? 1f : 0f);
    }

    private void FadeTo(float targetAlpha)
    {
        if (canvasGroup == null ||
            Mathf.Approximately(canvasGroup.alpha, targetAlpha))
            return;

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeRoutine(targetAlpha));
    }

    private IEnumerator FadeRoutine(float targetAlpha)
    {
        float startAlpha = canvasGroup.alpha;
        float duration = Mathf.Max(0f, fadeDuration);

        if (duration <= 0f)
        {
            SetAlphaImmediately(targetAlpha);
            fadeRoutine = null;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(
                startAlpha,
                targetAlpha,
                Mathf.Clamp01(elapsed / duration)
            );
            yield return null;
        }

        SetAlphaImmediately(targetAlpha);
        fadeRoutine = null;
    }

    private void SetAlphaImmediately(float alpha)
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = alpha;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }
}