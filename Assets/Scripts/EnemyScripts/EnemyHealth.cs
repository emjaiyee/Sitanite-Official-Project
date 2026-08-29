using System;
using UnityEngine;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 10;

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

    [Header("Runtime (Debug)")]
    [Tooltip("Runtime current HP. Read-only display for the Inspector.")]
    [SerializeField] private int runtimeCurrentHealth;

    public event Action<GameObject> OnEnemyDied;
    public event Action<EnemyHealth> OnHealthChanged;
    /// <summary>Fired after damage is applied. Second arg is the damage origin, if known.</summary>
    public event Action<EnemyHealth, Vector3?> OnDamaged;

    private bool hasDied;
    private bool baseStatsCached;
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
        CurrentHealth = maxHealth;
        runtimeCurrentHealth = CurrentHealth;
        hasDied = false;
    }

    public void ApplyLevelScaling(int level)
    {
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


        amount = Mathf.RoundToInt(
            Mathf.Max(0f, amount - GetDamageResistance(damageType))
        );

        if (amount <= 0)
            return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        runtimeCurrentHealth = CurrentHealth;

        // Show the damage number above the enemy, colored by type.
        DamagePopupSpawner.Spawn(transform, amount, damageType, 0.8f);

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

    public float GetDamageResistance(DamageType damageType)
    {
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


        Debug.Log(
            $"[EnemyHealth] {gameObject.name} died"
        );


        // EnemyMelee listens to this and
        // transitions into its Death state.
        OnEnemyDied?.Invoke(gameObject);
    }
}