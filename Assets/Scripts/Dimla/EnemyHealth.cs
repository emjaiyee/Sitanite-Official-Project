using System;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 10;

    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;

    // Optional event other systems can subscribe to
    public event Action<GameObject> OnEnemyDied;

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    /// <summary>
    /// Initialize or override max health at runtime.
    /// Call before the enemy takes damage if you want to set a custom value.
    /// </summary>
    public void Init(int max)
    {
        maxHealth = Mathf.Max(1, max);
        CurrentHealth = maxHealth;
    }

    public void ApplyDamage(int amount)
    {
        if (amount <= 0) return;
        if (CurrentHealth <= 0) return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);

        if (CurrentHealth == 0)
            Die();
    }

    public void Heal(int amount)
    {
        if (amount <= 0) return;
        if (CurrentHealth <= 0) return; // dead can't be healed
        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
    }

    private void Die()
    {
        // optional: play VFX / SFX here
        Debug.Log($"[EnemyHealth] {gameObject.name} died");
        OnEnemyDied?.Invoke(gameObject);

        // remove the enemy from scene
        Destroy(gameObject);
    }
}