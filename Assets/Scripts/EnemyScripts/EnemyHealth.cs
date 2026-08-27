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


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        CurrentHealth = maxHealth;
        runtimeCurrentHealth = CurrentHealth;
        hasDied = false;
    }


    // =========================================================
    // INITIALIZATION
    // =========================================================

    public void Init(int max)
    {
        maxHealth = Mathf.Max(1, max);
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