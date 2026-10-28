using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHealth : MonoBehaviour, IDamageable, ICriticalDamageable
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 10;

    [Header("Damage Feedback")]
    [SerializeField] private Color damageFlashColor = new Color(1f, 0.25f, 0.25f, 1f);
    [Min(0.01f)]
    [SerializeField] private float damageFlashDuration = 0.12f;

    [Header("Hit Reaction")]
    [Min(0f)]
    [SerializeField] private float hitBounceDistance = 0.02f;
    [Min(0f)]
    [SerializeField] private float hitBounceDuration = 0.1f;

    [Header("Run Statistics")]
    [SerializeField] private bool isMiniBoss;
    [SerializeField] private string enemyName;

    [Header("Damage Resistance")]
    [Min(0)] [SerializeField] private float basePierceResistance;
    [Min(0)] [SerializeField] private float baseStabResistance;
    [Min(0)] [SerializeField] private float baseSlashResistance;
    [Min(0)] [SerializeField] private float baseBluntResistance;
    [Min(0)] [SerializeField] private float baseFrostResistance;
    [Min(0)] [SerializeField] private float basePoisonResistance;
    [Min(0)] [SerializeField] private float baseLightningResistance;
    [Min(0)] [SerializeField] private float basePsychicResistance;
    [Min(0)] [SerializeField] private float baseNecrosisResistance;
    [Min(0)] [SerializeField] private float baseWaterResistance;
    [Min(0)] [SerializeField] private float baseEarthResistance;
    [Min(0)] [SerializeField] private float baseFireResistance;
    [Min(0)] [SerializeField] private float baseAirResistance;
    [Min(0)] [SerializeField] private float basePhysicalResistance;

    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;
    public bool IsMiniBoss => isMiniBoss;
    public string EnemyName => enemyName;

    [Header("Runtime (Debug)")]
    [Tooltip("Runtime current HP. Read-only display for the Inspector.")]
    [SerializeField] private int runtimeCurrentHealth;

    public event Action<GameObject> OnEnemyDied;
    public event Action<EnemyHealth> OnHealthChanged;
    /// <summary>Fired after damage is applied. Second arg is the damage origin, if known.</summary>
    public event Action<EnemyHealth, Vector3?> OnDamaged;

    private bool hasDied;
    private Dictionary<DamageType, float> configuredResistances;
    private bool playerHit;
    private bool playerHitCritical;
    private WeaponAttackType playerAttackType;
    private DamageType lastDamageType;
    private AdaptiveEnemy adaptation;

    public bool HasConfiguredStats => configuredResistances != null;

    public void SetEnemyName(string name)
    {
        enemyName = name;
    }

    public void ConfigureBossStats(float health, IEnumerable<DungeonMemory.DamageStat> stats)
    {
        configuredResistances = new Dictionary<DamageType, float>();
        foreach (DungeonMemory.DamageStat stat in stats)
            configuredResistances[stat.type] = Mathf.Max(0f, stat.resistance);

        isMiniBoss = true;
        Init(Mathf.CeilToInt(health));
    }

    public void BalanceBoss(int enemyLevel, PlayerStats player)
    {
        List<DungeonMemory.DamageStat> stats = new List<DungeonMemory.DamageStat>();
        foreach (DamageType type in BossBalance.DamageTypes())
        {
            stats.Add(new DungeonMemory.DamageStat
            {
                type = type,
                resistance = BossBalance.AdjustBase(GetDamageResistance(type), enemyLevel, player.Level)
            });
        }
        ConfigureBossStats(BossBalance.Health(MaxHealth, player.MaxHealth), stats);
    }

    private bool baseStatsCached;
    private Coroutine damageFlashRoutine;
    private Coroutine hitBounceRoutine;
    private SpriteRenderer[] damageFeedbackRenderers;
    private Color[] originalRendererColors;
    private int baseMaxHealth;
    private float basePierceResistanceValue;
    private float baseStabResistanceValue;
    private float baseSlashResistanceValue;
    private float baseBluntResistanceValue;
    private float baseFrostResistanceValue;
    private float basePoisonResistanceValue;
    private float baseLightningResistanceValue;
    private float basePsychicResistanceValue;
    private float baseNecrosisResistanceValue;
    private float baseWaterResistanceValue;
    private float baseEarthResistanceValue;
    private float baseFireResistanceValue;
    private float baseAirResistanceValue;
    private float basePhysicalResistanceValue;


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        CacheBaseStats();
        CacheDamageFeedbackRenderers();
        CurrentHealth = maxHealth;
        runtimeCurrentHealth = CurrentHealth;
        hasDied = false;
    }

    private void OnDisable()
    {
        StopDamageFlash(restoreColors: true);
        if (hitBounceRoutine != null)
        {
            StopCoroutine(hitBounceRoutine);
            hitBounceRoutine = null;
        }

        EnemyLich lich = GetComponent<EnemyLich>();
        if (lich != null)
            lich.PauseMovement(false);
    }

    public void ApplyLevelScaling(int level)
    {
        if (HasConfiguredStats)
            return;

        CacheBaseStats();

        level = Mathf.Max(1, level);

        float currentHealthRatio = maxHealth > 0
            ? (float)CurrentHealth / maxHealth
            : 1f;

        maxHealth = Mathf.Max(1, Mathf.CeilToInt(baseMaxHealth + GetScaledBonus(level, 15)));

        basePierceResistance = ScaleResistance(basePierceResistanceValue, level);
        baseStabResistance = ScaleResistance(baseStabResistanceValue, level);
        baseSlashResistance = ScaleResistance(baseSlashResistanceValue, level);
        baseBluntResistance = ScaleResistance(baseBluntResistanceValue, level);
        baseFrostResistance = ScaleResistance(baseFrostResistanceValue, level);
        basePoisonResistance = ScaleResistance(basePoisonResistanceValue, level);
        baseLightningResistance = ScaleResistance(baseLightningResistanceValue, level);
        basePsychicResistance = ScaleResistance(basePsychicResistanceValue, level);
        baseNecrosisResistance = ScaleResistance(baseNecrosisResistanceValue, level);
        baseWaterResistance = ScaleResistance(baseWaterResistanceValue, level);
        baseEarthResistance = ScaleResistance(baseEarthResistanceValue, level);
        baseFireResistance = ScaleResistance(baseFireResistanceValue, level);
        baseAirResistance = ScaleResistance(baseAirResistanceValue, level);
        basePhysicalResistance = ScaleResistance(basePhysicalResistanceValue, level);

        if (CurrentHealth > 0)
        {
            CurrentHealth = Mathf.Clamp(
                Mathf.RoundToInt(currentHealthRatio * maxHealth),
                1,
                maxHealth
            );
        }

        runtimeCurrentHealth = CurrentHealth;
    }


    // =========================================================
    // INITIALIZATION
    // =========================================================

    public void Init(int max)
    {
        maxHealth = Mathf.Max(1, max);
        CacheBaseStats();
        CurrentHealth = maxHealth;
        runtimeCurrentHealth = CurrentHealth;
        hasDied = false;
        OnHealthChanged?.Invoke(this);
    }

    public void ApplyAdditiveModifiers(int healthModifier, float resistanceModifier)
    {
        CacheBaseStats();

        baseMaxHealth = Mathf.Max(1, baseMaxHealth + healthModifier);
        maxHealth = Mathf.Max(1, maxHealth + healthModifier);
        CurrentHealth = Mathf.Clamp(CurrentHealth + healthModifier, 1, maxHealth);
        runtimeCurrentHealth = CurrentHealth;

        if (configuredResistances != null)
        {
            foreach (DamageType type in new List<DamageType>(configuredResistances.Keys))
                configuredResistances[type] = Mathf.Max(0f, configuredResistances[type] + resistanceModifier);
        }

        basePierceResistanceValue = AddToActiveResistance(basePierceResistanceValue, resistanceModifier);
        baseStabResistanceValue = AddToActiveResistance(baseStabResistanceValue, resistanceModifier);
        baseSlashResistanceValue = AddToActiveResistance(baseSlashResistanceValue, resistanceModifier);
        baseBluntResistanceValue = AddToActiveResistance(baseBluntResistanceValue, resistanceModifier);
        baseFrostResistanceValue = AddToActiveResistance(baseFrostResistanceValue, resistanceModifier);
        basePoisonResistanceValue = AddToActiveResistance(basePoisonResistanceValue, resistanceModifier);
        baseLightningResistanceValue = AddToActiveResistance(baseLightningResistanceValue, resistanceModifier);
        basePsychicResistanceValue = AddToActiveResistance(basePsychicResistanceValue, resistanceModifier);
        baseNecrosisResistanceValue = AddToActiveResistance(baseNecrosisResistanceValue, resistanceModifier);
        baseWaterResistanceValue = AddToActiveResistance(baseWaterResistanceValue, resistanceModifier);
        baseEarthResistanceValue = AddToActiveResistance(baseEarthResistanceValue, resistanceModifier);
        baseFireResistanceValue = AddToActiveResistance(baseFireResistanceValue, resistanceModifier);
        baseAirResistanceValue = AddToActiveResistance(baseAirResistanceValue, resistanceModifier);
        basePhysicalResistanceValue = AddToActiveResistance(basePhysicalResistanceValue, resistanceModifier);

        basePierceResistance = AddToActiveResistance(basePierceResistance, resistanceModifier);
        baseStabResistance = AddToActiveResistance(baseStabResistance, resistanceModifier);
        baseSlashResistance = AddToActiveResistance(baseSlashResistance, resistanceModifier);
        baseBluntResistance = AddToActiveResistance(baseBluntResistance, resistanceModifier);
        baseFrostResistance = AddToActiveResistance(baseFrostResistance, resistanceModifier);
        basePoisonResistance = AddToActiveResistance(basePoisonResistance, resistanceModifier);
        baseLightningResistance = AddToActiveResistance(baseLightningResistance, resistanceModifier);
        basePsychicResistance = AddToActiveResistance(basePsychicResistance, resistanceModifier);
        baseNecrosisResistance = AddToActiveResistance(baseNecrosisResistance, resistanceModifier);
        baseWaterResistance = AddToActiveResistance(baseWaterResistance, resistanceModifier);
        baseEarthResistance = AddToActiveResistance(baseEarthResistance, resistanceModifier);
        baseFireResistance = AddToActiveResistance(baseFireResistance, resistanceModifier);
        baseAirResistance = AddToActiveResistance(baseAirResistance, resistanceModifier);
        basePhysicalResistance = AddToActiveResistance(basePhysicalResistance, resistanceModifier);
    }


    // =========================================================
    // DAMAGE
    // =========================================================

    public void TakeDamage(
        int amount,
        DamageType damageType = DamageType.Slash,
        Vector3? damageSource = null)
    {
        if (amount <= 0)
            return;

        if (hasDied)
            return;

        if (CurrentHealth <= 0)
            return;

        lastDamageType = damageType;
        if (adaptation == null)
            adaptation = GetComponent<AdaptiveEnemy>();
        amount = adaptation != null && !isMiniBoss && adaptation.Matches(damageType)
            ? adaptation.Mitigate(amount, damageType)
            : Mathf.RoundToInt(
            Mathf.Max(1f, amount - GetDamageResistance(damageType))
        );

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        runtimeCurrentHealth = CurrentHealth;

        if (CurrentHealth > 0)
        {
            StartDamageFlash();
            if (playerHit)
                StartHitBounce(damageSource);
        }

        // Show the damage number above the enemy, colored by type.
        DamagePopupSpawner.Spawn(
            transform,
            amount,
            damageType,
            0.8f,
            playerHitCritical
        );

        OnHealthChanged?.Invoke(this);
        OnDamaged?.Invoke(this, damageSource);


        Debug.Log(
            $"[EnemyHealth] {gameObject.name} took " +
            $"{amount} {damageType} damage. " +
            $"HP: {CurrentHealth}/{maxHealth}"
        );


        if (CurrentHealth == 0)
        {
            Die();
        }
    }

    private void StartHitBounce(Vector3? damageSource)
    {
        if (hitBounceDistance <= 0f || hitBounceDuration <= 0f)
            return;

        Vector2 direction = damageSource.HasValue
            ? (Vector2)transform.position - (Vector2)damageSource.Value
            : Vector2.zero;

        if (direction.sqrMagnitude <= 0.0001f &&
            Player.Instance != null)
        {
            direction =
                (Vector2)transform.position -
                (Vector2)Player.Instance.transform.position;
        }

        if (direction.sqrMagnitude <= 0.0001f)
            direction = Vector2.right;

        direction.Normalize();

        EnemyMelee melee = GetComponent<EnemyMelee>();
        if (melee != null)
            melee.ApplyStagger(hitBounceDuration);

        EnemyRange ranged = GetComponent<EnemyRange>();
        if (ranged != null)
            ranged.ApplyStagger(hitBounceDuration);

        EnemyLich lich = GetComponent<EnemyLich>();
        if (lich != null)
            lich.PauseMovement(true);

        if (hitBounceRoutine != null)
            StopCoroutine(hitBounceRoutine);

        hitBounceRoutine = StartCoroutine(BounceBack(direction));
    }

    private IEnumerator BounceBack(Vector2 direction)
    {
        Vector3 start = transform.position;
        Vector3 end = start + (Vector3)(direction * hitBounceDistance);
        float elapsed = 0f;

        while (elapsed < hitBounceDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / hitBounceDuration);
            float easedProgress = 1f - (1f - progress) * (1f - progress);
            transform.position = Vector3.Lerp(start, end, easedProgress);
            yield return null;
        }

        transform.position = end;
        EnemyLich lich = GetComponent<EnemyLich>();
        if (lich != null)
            lich.PauseMovement(false);

        hitBounceRoutine = null;
    }

    public void TakePlayerDamage(
        int amount,
        DamageType type,
        Vector3? source,
        WeaponAttackType attackType,
        bool isCriticalHit = false)
    {
        playerHit = true;
        playerAttackType = attackType;
        playerHitCritical = isCriticalHit;
        try
        {
            TakeDamage(amount, type, source);
        }
        finally
        {
            playerHit = false;
            playerHitCritical = false;
        }
    }

    public void TakeCriticalDamage(
        int amount,
        DamageType damageType,
        Vector3? damageSource,
        WeaponAttackType attackType)
    {
        TakePlayerDamage(
            amount,
            damageType,
            damageSource,
            attackType,
            true
        );
    }

    public void PlayCounterHitVisual(GameObject visualPrefab, Vector3 sourcePosition)
    {
        if (visualPrefab == null || hasDied || CurrentHealth <= 0)
            return;

        Vector2 direction = (Vector2)(transform.position - sourcePosition);
        float angle = direction.sqrMagnitude > 0.0001f
            ? Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg
            : 0f;
        GameObject visual = Instantiate(
            visualPrefab,
            transform.position,
            Quaternion.Euler(0f, 0f, angle)
        );

        if (visual.GetComponentInChildren<SpriteArrayVisual>() == null)
            Destroy(visual, 0.5f);
    }

    public float GetDamageResistance(DamageType damageType)
    {
        if (adaptation == null)
            adaptation = GetComponent<AdaptiveEnemy>();
        if (adaptation != null && !isMiniBoss && adaptation.Matches(damageType))
            return adaptation.DamageResistance;
        if (configuredResistances != null)
        {
            foreach (DamageType type in BossBalance.DamageTypes())
            {
                if ((damageType & type) != 0 && configuredResistances.TryGetValue(type, out float value))
                    return value;
            }
            return 0f;
        }

        if ((damageType & DamageType.Pierce) != 0)
            return basePierceResistance;
        if ((damageType & DamageType.Stab) != 0)
            return baseStabResistance;
        if ((damageType & DamageType.Slash) != 0)
            return baseSlashResistance;
        if ((damageType & DamageType.Blunt) != 0)
            return baseBluntResistance;
        if ((damageType & DamageType.Frost) != 0)
            return baseFrostResistance;
        if ((damageType & DamageType.Poison) != 0)
            return basePoisonResistance;
        if ((damageType & DamageType.Lightning) != 0)
            return baseLightningResistance;
        if ((damageType & DamageType.Psychic) != 0)
            return basePsychicResistance;
        if ((damageType & DamageType.Necrosis) != 0)
            return baseNecrosisResistance;
        if ((damageType & DamageType.Water) != 0)
            return baseWaterResistance;
        if ((damageType & DamageType.Earth) != 0)
            return baseEarthResistance;
        if ((damageType & DamageType.Fire) != 0)
            return baseFireResistance;
        if ((damageType & DamageType.Air) != 0)
            return baseAirResistance;
        if ((damageType & DamageType.Physical) != 0)
            return basePhysicalResistance;

        return 0f;
    }

    private void CacheBaseStats()
    {
        if (baseStatsCached)
            return;

        baseStatsCached = true;

        baseMaxHealth = maxHealth;

        basePierceResistanceValue = basePierceResistance;
        baseStabResistanceValue = baseStabResistance;
        baseSlashResistanceValue = baseSlashResistance;
        baseBluntResistanceValue = baseBluntResistance;
        baseFrostResistanceValue = baseFrostResistance;
        basePoisonResistanceValue = basePoisonResistance;
        baseLightningResistanceValue = baseLightningResistance;
        basePsychicResistanceValue = basePsychicResistance;
        baseNecrosisResistanceValue = baseNecrosisResistance;
        baseWaterResistanceValue = baseWaterResistance;
        baseEarthResistanceValue = baseEarthResistance;
        baseFireResistanceValue = baseFireResistance;
        baseAirResistanceValue = baseAirResistance;
        basePhysicalResistanceValue = basePhysicalResistance;
    }

    private void CacheDamageFeedbackRenderers()
    {
        damageFeedbackRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        originalRendererColors = new Color[damageFeedbackRenderers.Length];

        for (int index = 0; index < damageFeedbackRenderers.Length; index++)
            originalRendererColors[index] = damageFeedbackRenderers[index].color;
    }

    private void StartDamageFlash()
    {
        if (damageFeedbackRenderers == null || damageFeedbackRenderers.Length == 0)
            return;

        StopDamageFlash(restoreColors: false);
        damageFlashRoutine = StartCoroutine(PlayDamageFlash());
    }

    private IEnumerator PlayDamageFlash()
    {
        float halfDuration = Mathf.Max(0.01f, damageFlashDuration) * 0.5f;
        float elapsed = 0f;

        while (elapsed < halfDuration * 2f)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed < halfDuration
                ? elapsed / halfDuration
                : 1f - ((elapsed - halfDuration) / halfDuration);
            ApplyDamageFlash(Mathf.Clamp01(progress));
            yield return null;
        }

        RestoreDamageFeedbackColors();
        damageFlashRoutine = null;
    }

    private void ApplyDamageFlash(float amount)
    {
        for (int index = 0; index < damageFeedbackRenderers.Length; index++)
        {
            SpriteRenderer spriteRenderer = damageFeedbackRenderers[index];
            if (spriteRenderer == null)
                continue;

            Color originalColor = originalRendererColors[index];
            Color flashColor = damageFlashColor;
            Color currentColor = spriteRenderer.color;
            flashColor.a = currentColor.a;

            Color result = Color.Lerp(originalColor, flashColor, amount);
            result.a = currentColor.a;
            spriteRenderer.color = result;
        }
    }

    private void StopDamageFlash(bool restoreColors)
    {
        if (damageFlashRoutine != null)
        {
            StopCoroutine(damageFlashRoutine);
            damageFlashRoutine = null;
        }

        if (restoreColors)
            RestoreDamageFeedbackColors();
    }

    private void RestoreDamageFeedbackColors()
    {
        if (damageFeedbackRenderers == null)
            return;

        for (int index = 0; index < damageFeedbackRenderers.Length; index++)
        {
            SpriteRenderer spriteRenderer = damageFeedbackRenderers[index];
            if (spriteRenderer == null)
                continue;

            Color restoredColor = originalRendererColors[index];
            restoredColor.a = spriteRenderer.color.a;
            spriteRenderer.color = restoredColor;
        }
    }

    private static float GetScaledBonus(int level, float perLevelBonus)
    {
        if (level <= 1)
            return 0f;

        float bonus = 0f;
        float specialBonus = perLevelBonus * 2f;
        float followUpBonus = Mathf.Ceil(specialBonus * 0.75f);

        for (int currentLevel = 2; currentLevel <= level; currentLevel++)
        {
            int levelInCycle = currentLevel % 5;

            if (levelInCycle == 0)
            {
                bonus += specialBonus;
                continue;
            }

            if (levelInCycle == 1)
            {
                bonus += followUpBonus;

                specialBonus = followUpBonus * 2f;
                followUpBonus = Mathf.Ceil(specialBonus * 0.75f);
                continue;
            }

            bonus += perLevelBonus;
        }

        return bonus;
    }

    private static float ScaleResistance(float baseValue, int level)
    {
        if (baseValue <= 0f)
            return 0f;

        return baseValue + GetScaledBonus(level, 3f);
    }

    private static float AddToActiveResistance(float value, float modifier)
    {
        return value > 0f ? Mathf.Max(0f, value + modifier) : 0f;
    }



    // =========================================================
    // HEAL
    // =========================================================

    public void Heal(int amount)
    {
        if (amount <= 0)
            return;

        if (hasDied)
            return;

        if (CurrentHealth <= 0)
            return;


        CurrentHealth =
            Mathf.Min(
                maxHealth,
                CurrentHealth + amount
            );

        runtimeCurrentHealth = CurrentHealth;
        OnHealthChanged?.Invoke(this);
    }


    // =========================================================
    // DEATH
    // =========================================================

    private void Die()
    {
        if (hasDied)
            return;

        hasDied = true;

        if (playerHit && Player.Instance != null)
        {
            Player.Instance.RecordAdaptiveKill(lastDamageType, playerAttackType != WeaponAttackType.Melee);
        }
        if (Player.Instance != null)
        {
            Player.Instance.RecordEnemyDefeated(
                isMiniBoss || GetComponentInParent<EnemyLich>() != null
            );
        }

        Debug.Log(
            $"[EnemyHealth] {gameObject.name} died"
        );


        // EnemyMelee listens to this and
        // transitions into its Death state.
        OnEnemyDied?.Invoke(gameObject);
    }
}