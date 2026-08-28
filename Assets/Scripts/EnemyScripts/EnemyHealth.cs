using System;
using UnityEngine;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 10;

    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;

    public event Action<GameObject> OnEnemyDied;

    private bool hasDied;


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        CurrentHealth = maxHealth;
        hasDied = false;
    }


    // =========================================================
    // INITIALIZATION
    // =========================================================

    public void Init(int max)
    {
        maxHealth = Mathf.Max(1, max);
        CurrentHealth = maxHealth;
        hasDied = false;
    }


    // =========================================================
    // DAMAGE
    // =========================================================

    public void TakeDamage(
        int amount,
        DamageType damageType = DamageType.Physical)
    {
        if (amount <= 0)
            return;

        if (hasDied)
            return;

        if (CurrentHealth <= 0)
            return;


        CurrentHealth =
            Mathf.Max(
                0,
                CurrentHealth - amount
            );


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