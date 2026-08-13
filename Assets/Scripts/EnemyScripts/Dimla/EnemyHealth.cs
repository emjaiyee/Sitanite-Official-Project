using System;
using UnityEngine;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 10;

    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;

    public event Action<GameObject> OnEnemyDied;

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    public void Init(int max)
    {
        maxHealth = Mathf.Max(1, max);
        CurrentHealth = maxHealth;
    }

    public void TakeDamage(
        int amount,
        DamageType damageType = DamageType.Physical)
    {
        if (amount <= 0)
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

    public void Heal(int amount)
    {
        if (amount <= 0)
            return;

        if (CurrentHealth <= 0)
            return;

        CurrentHealth =
            Mathf.Min(
                maxHealth,
                CurrentHealth + amount
            );
    }

    private void Die()
    {
        Debug.Log(
            $"[EnemyHealth] {gameObject.name} died"
        );

        OnEnemyDied?.Invoke(gameObject);

        Destroy(gameObject);
    }
}