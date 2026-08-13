using System;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("Maximum Resources")]
    [Min(1)] public int maxHealth = 100;
    [Min(1)] public int maxMana = 75;
    [Min(1)] public int maxStamina = 75;

    [Header("Current Resources")]
    [SerializeField] private int currentHealth;
    [SerializeField] private int currentMana;
    [SerializeField] private int currentStamina;

    public int CurrentHealth => currentHealth;
    public int CurrentMana => currentMana;
    public int CurrentStamina => currentStamina;

    public bool IsDead { get; private set; }
    public event Action<PlayerStats> Changed;
    public event Action Died;

    private void Awake()
    {
        ResetToFull();
    }

    /// <summary>Restores all resources to their configured maximum values.</summary>
    public void ResetToFull()
    {
        currentHealth = Mathf.Max(1, maxHealth);
        currentMana = Mathf.Max(1, maxMana);
        currentStamina = Mathf.Max(1, maxStamina);
        IsDead = false;
        NotifyChanged();
    }

    /// <summary>Reduces health and triggers the death event when it reaches zero.</summary>
    public void TakeDamage(int amount)
    {
        if (amount <= 0 || IsDead || currentHealth <= 0) return;
        currentHealth = Mathf.Max(0, currentHealth - amount);
        NotifyChanged();
        if (currentHealth == 0)
        {
            IsDead = true;
            Debug.Log("PLAYER IS DEAD");
            Died?.Invoke();
        }
    }

    /// <summary>Restores health without exceeding the configured maximum.</summary>
    public void Heal(int amount)
    {
        if (amount <= 0 || IsDead || currentHealth <= 0) return;
        currentHealth = Mathf.Min(Mathf.Max(1, maxHealth), currentHealth + amount);
        NotifyChanged();
    }

    /// <summary>Consumes mana when enough mana is available.</summary>
    public bool UseMana(int amount)
    {
        if (amount <= 0 || currentMana < amount) return false;
        currentMana -= amount;
        NotifyChanged();
        return true;
    }

    /// <summary>Restores mana without exceeding the configured maximum.</summary>
    public void RestoreMana(int amount)
    {
        if (amount <= 0) return;
        currentMana = Mathf.Min(Mathf.Max(1, maxMana), currentMana + amount);
        NotifyChanged();
    }

    /// <summary>Consumes stamina when enough stamina is available.</summary>
    public bool UseStamina(int amount)
    {
        if (amount <= 0 || currentStamina < amount) return false;
        currentStamina -= amount;
        NotifyChanged();
        return true;
    }

    /// <summary>Restores stamina without exceeding the configured maximum.</summary>
    public void RestoreStamina(int amount)
    {
        if (amount <= 0) return;
        currentStamina = Mathf.Min(Mathf.Max(1, maxStamina), currentStamina + amount);
        NotifyChanged();
    }

    private void NotifyChanged()
    {
        Changed?.Invoke(this);
    }
}
