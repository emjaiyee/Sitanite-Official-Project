using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BossHealthBarUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TMP_Text bossNameText;
    [SerializeField, Min(0f)] private float fadeDuration = 0.2f;
    [Tooltip("Use a separate UI instance for main bosses. Leave off on the existing mini-boss bar.")]
    [SerializeField] private bool trackMainBoss;

    private EnemySpawnerManager spawnerManager;
    private EnemyHealth bossHealth;
    private PlayerRoomTracker roomTracker;
    private float targetAlpha;
    private EnemyManggagaway mainBoss;
    private float nextBossSearch;
    private UISuppressor uiSuppressor;

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        uiSuppressor = GetComponent<UISuppressor>();

        if (healthSlider == null)
            healthSlider = GetComponentInChildren<Slider>(true);

        targetAlpha = 0f;
        SetAlphaImmediately(targetAlpha);
        RefreshName();
    }

    private void OnEnable()
    {
        if (!trackMainBoss)
            BindToSpawner();
    }

    private void Update()
    {
        if (trackMainBoss)
        {
            if (roomTracker == null && Player.Instance != null)
                roomTracker = Player.Instance.GetComponent<PlayerRoomTracker>();
            if (Time.unscaledTime >= nextBossSearch)
            {
                nextBossSearch = Time.unscaledTime + 0.5f;
                mainBoss = null;
                foreach (EnemyManggagaway candidate in FindObjectsByType<EnemyManggagaway>(FindObjectsSortMode.None))
                {
                    if (!candidate.IsDead && roomTracker != null && candidate.Room != null &&
                        candidate.Room == roomTracker.CurrentRoom)
                    {
                        mainBoss = candidate;
                        break;
                    }
                }
                BindBoss(mainBoss != null ? mainBoss.Health : null);
            }
            RefreshName();
            UpdateVisibility();
            UpdateFade();
            return;
        }

        if (spawnerManager == null)
            BindToSpawner();

        if (spawnerManager != null &&
            spawnerManager.ActiveBossHealth != bossHealth)
            BindBoss(spawnerManager.ActiveBossHealth);

        RefreshName();
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
        if (health != null && health.IsMainBoss != trackMainBoss)
            health = null;

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
        if (bossHealth != null)
        {
            bossHealth.OnHealthChanged -= Refresh;
            bossHealth.OnEnemyDied -= HandleBossDied;
        }
        bossHealth = null;
        RefreshName();
    }

    private void Refresh(EnemyHealth health)
    {
        RefreshName();
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

    private void RefreshName()
    {
        if (bossNameText == null)
            return;

        if (bossHealth == null || (!bossHealth.IsMiniBoss && !bossHealth.IsMainBoss) || bossHealth.CurrentHealth <= 0 ||
            string.IsNullOrWhiteSpace(bossHealth.EnemyName))
        {
            bossNameText.text = string.Empty;
            return;
        }

        FallenDescenderLevelXP fallenLevel = bossHealth.GetComponent<FallenDescenderLevelXP>();
        EnemyLevelXP enemyLevel = bossHealth.GetComponent<EnemyLevelXP>();
        int level = fallenLevel != null ? fallenLevel.Level : enemyLevel != null ? enemyLevel.Level : 1;
        bossNameText.text = $"{bossHealth.EnemyName} | Lvl {level}";
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

        bool isInBossRoom = trackMainBoss
            ? bossHealth != null && mainBoss != null && !mainBoss.IsDead &&
                roomTracker != null && mainBoss.Room != null && roomTracker.CurrentRoom == mainBoss.Room
            :
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
        if (canvasGroup == null || (uiSuppressor != null && uiSuppressor.IsSuppressed))
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