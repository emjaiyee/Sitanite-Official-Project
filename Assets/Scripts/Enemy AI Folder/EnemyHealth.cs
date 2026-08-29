using System;
using UnityEngine;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    #region Variables Part

    [SerializeField] private EnemyStats_Data enemyStats;

    public int CurrentHealth { get; private set; }

    public event Action<GameObject> OnEnemyDied;

    private bool hasDied;

    #endregion

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        CurrentHealth = enemyStats.EnemyMaxHealth;
        hasDied = false;
    }


    // =========================================================
    // DAMAGE
    // =========================================================

    public void TakeDamage(int amount, DamageType damageType = DamageType.Physical)
    {
        if (amount <= 0 && hasDied && CurrentHealth <= 0)
            return;


        CurrentHealth =
            Mathf.Max(
                0,
                CurrentHealth - amount
            );


        Debug.Log(
            $"[EnemyHealth] {gameObject.name} took " +
            $"{amount} {damageType} damage. " +
            $"HP: {CurrentHealth}/{enemyStats.EnemyMaxHealth}"
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
                enemyStats.EnemyMaxHealth,
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