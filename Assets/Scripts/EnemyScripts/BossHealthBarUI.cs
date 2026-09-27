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
    private PlayerRoomTracker roomTracker;
    private float targetAlpha;

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (healthSlider == null)
            healthSlider = GetComponentInChildren<Slider>(true);

        targetAlpha = 0f;
        SetAlphaImmediately(targetAlpha);
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
        UpdateFade();
    }

    private void OnDisable()
    {
        if (spawnerManager != null)
            spawnerManager.OnBossSpawned -= BindBoss;

        UnbindBoss();
        spawnerManager = null;
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
        if (roomTracker == null && Player.Instance != null)
            roomTracker = Player.Instance.GetComponent<PlayerRoomTracker>();

        bool isInBossRoom =
            bossHealth != null &&
            spawnerManager != null &&
            spawnerManager.ActiveBossRoom != null &&
            roomTracker != null &&
            roomTracker.CurrentRoom == spawnerManager.ActiveBossRoom;

        FadeTo(isInBossRoom ? 1f : 0f);
    }

    private void FadeTo(float targetAlpha)
    {
        this.targetAlpha = targetAlpha;
    }

    private void UpdateFade()
    {
        if (canvasGroup == null)
            return;

        float duration = Mathf.Max(0f, fadeDuration);
        if (duration <= 0f)
        {
            SetAlphaImmediately(targetAlpha);
            return;
        }

        canvasGroup.alpha = Mathf.MoveTowards(
            canvasGroup.alpha,
            targetAlpha,
            Time.unscaledDeltaTime / duration
        );
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