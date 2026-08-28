using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerStats : MonoBehaviour
{
    [Header("Maximum Resources")]
    [Min(1)]
    public int maxHealth = 100;

    [Min(1)]
    public int maxMana = 75;

    [Min(1)]
    public int maxStamina = 75;

    [Header("Current Resources")]
    [SerializeField] private int currentHealth;
    [SerializeField] private int currentMana;
    [SerializeField] private int currentStamina;

    [Header("Character")]
    [SerializeField] private string characterName;
    [SerializeField] private CharacterGender gender;
    [SerializeField] private PlayerClass playerClass = PlayerClass.Warrior;

    [Header("References")]
    [SerializeField] private PlayerWASD movement;

    [Header("Sprint")]
    [Min(0f)]
    [SerializeField] private float sprintCostPerSecond = 4f;

    [Range(0f, 1f)]
    [SerializeField] private float exhaustedSpeedMultiplier = 0.35f;

    [Header("Regeneration Settings (per second)")]

    [SerializeField]
    private float meleeHealthRegen = 1f;

    [SerializeField]
    private float meleeManaRegen = 0f;

    [SerializeField]
    private float meleeStaminaRegen = 3f;

    [SerializeField]
    private float rangedHealthRegen = 1f;

    [SerializeField]
    private float rangedManaRegen = 2f;

    [SerializeField]
    private float rangedStaminaRegen = 2f;

    [SerializeField]
    private float mageHealthRegen = 1f;

    [SerializeField]
    private float mageManaRegen = 4f;

    [SerializeField]
    private float mageStaminaRegen = 1f;

    [Header("Skill")]
    [Min(0)]
    [SerializeField] private int skillManaCost = 25;

    [SerializeField] private InputActionReference skillAction;

    [Header("Death Visual")]
    [Range(0f, 1f)]
    [SerializeField] private float deadPlayerOpacity = 0.5f;

    // -------------------------------------------------
    // CHARACTER INFORMATION
    // -------------------------------------------------

    public string CharacterName => characterName;
    public CharacterGender Gender => gender;
    public PlayerClass PlayerClass => playerClass;

    // -------------------------------------------------
    // CURRENT RESOURCES
    // -------------------------------------------------

    public int CurrentHealth => currentHealth;
    public int CurrentMana => currentMana;
    public int CurrentStamina => currentStamina;

    public bool IsDead { get; private set; }

    // -------------------------------------------------
    // EVENTS
    // -------------------------------------------------

    public event Action<PlayerStats> Changed;
    public event Action Died;

    // -------------------------------------------------
    // REGENERATION ACCUMULATORS
    // -------------------------------------------------

    private float healthRegenAccumulator;
    private float manaRegenAccumulator;
    private float staminaRegenAccumulator;

    // -------------------------------------------------
    // SPRINT ACCUMULATOR
    // -------------------------------------------------

    private float sprintDrainAccumulator;

    // -------------------------------------------------
    // UNITY
    // -------------------------------------------------

    private void Awake()
    {
        if (movement == null)
            movement = GetComponent<PlayerWASD>();

        ResetToFull();
    }

    private void OnEnable()
    {
        if (skillAction != null)
        {
            skillAction.action.Enable();
            skillAction.action.performed += OnSkillPerformed;
        }
    }

    private void OnDisable()
    {
        if (skillAction != null)
        {
            skillAction.action.performed -= OnSkillPerformed;
            skillAction.action.Disable();
        }
    }

    private void Update()
    {
        if (IsDead)
        {
            DisableMovement();
            return;
        }

        UpdateSprint();
        UpdateMovementMultiplier();
        RegenerateResources();
    }

    // -------------------------------------------------
    // RESET
    // -------------------------------------------------

    /// <summary>
    /// Restores all resources to their configured maximum values.
    /// </summary>
    public void ResetToFull()
    {
        currentHealth = Mathf.Max(1, maxHealth);
        currentMana = Mathf.Max(1, maxMana);
        currentStamina = Mathf.Max(1, maxStamina);

        healthRegenAccumulator = 0f;
        manaRegenAccumulator = 0f;
        staminaRegenAccumulator = 0f;
        sprintDrainAccumulator = 0f;

        IsDead = false;

        if (movement != null)
            movement.SpeedMultiplier = 1f;

        NotifyChanged();
    }

    // -------------------------------------------------
    // HEALTH
    // -------------------------------------------------

    public void TakeDamage(int amount)
    {
        if (amount <= 0 || IsDead || currentHealth <= 0)
            return;

        currentHealth = Mathf.Max(
            0,
            currentHealth - amount
        );

        NotifyChanged();

        if (currentHealth == 0)
        {
            IsDead = true;

            Debug.Log("PLAYER IS DEAD");

            HandlePlayerDied();

            Died?.Invoke();
        }
    }

    public void Heal(int amount)
    {
        if (amount <= 0 || IsDead || currentHealth <= 0)
            return;

        currentHealth = Mathf.Min(
            Mathf.Max(1, maxHealth),
            currentHealth + amount
        );

        NotifyChanged();
    }

    // -------------------------------------------------
    // MANA
    // -------------------------------------------------

    public bool UseMana(int amount)
    {
        if (amount <= 0 || currentMana < amount)
            return false;

        currentMana -= amount;

        NotifyChanged();

        return true;
    }

    public void RestoreMana(int amount)
    {
        if (amount <= 0)
            return;

        currentMana = Mathf.Min(
            Mathf.Max(1, maxMana),
            currentMana + amount
        );

        NotifyChanged();
    }

    // -------------------------------------------------
    // STAMINA
    // -------------------------------------------------

    public bool UseStamina(int amount)
    {
        if (amount <= 0 || currentStamina < amount)
            return false;

        currentStamina -= amount;

        NotifyChanged();

        return true;
    }

    public void RestoreStamina(int amount)
    {
        if (amount <= 0)
            return;

        currentStamina = Mathf.Min(
            Mathf.Max(1, maxStamina),
            currentStamina + amount
        );

        NotifyChanged();
    }

    // -------------------------------------------------
    // CHARACTER
    // -------------------------------------------------

    public void SetCharacterName(string newName)
    {
        characterName = newName.Trim();

        NotifyChanged();
    }

    public void SetCharacterGender(CharacterGender newGender)
    {
        gender = newGender;

        NotifyChanged();
    }

    public void SetPlayerClass(PlayerClass newClass)
    {
        if (playerClass == newClass)
            return;

        playerClass = newClass;

        healthRegenAccumulator = 0f;
        manaRegenAccumulator = 0f;
        staminaRegenAccumulator = 0f;

        NotifyChanged();
    }

    // -------------------------------------------------
    // SPRINT
    // -------------------------------------------------

    private void UpdateSprint()
    {
        if (movement == null)
            return;

        bool isSprinting = movement.IsSprinting;

        if (!isSprinting || currentStamina <= 0)
        {
            sprintDrainAccumulator = 0f;
            return;
        }

        sprintDrainAccumulator +=
            sprintCostPerSecond * Time.deltaTime;

        if (sprintDrainAccumulator >= 1f)
        {
            int drainAmount =
                Mathf.FloorToInt(sprintDrainAccumulator);

            sprintDrainAccumulator -= drainAmount;

            UseStamina(drainAmount);
        }
    }

    // -------------------------------------------------
    // MOVEMENT
    // -------------------------------------------------

    private void UpdateMovementMultiplier()
    {
        if (movement == null)
            return;

        if (currentStamina > 0)
        {
            movement.SpeedMultiplier = 1f;
        }
        else
        {
            movement.SpeedMultiplier =
                exhaustedSpeedMultiplier;
        }
    }

    private void DisableMovement()
    {
        if (movement != null)
            movement.SpeedMultiplier = 0f;
    }

    // -------------------------------------------------
    // REGENERATION
    // -------------------------------------------------

    private void RegenerateResources()
    {
        if (IsDead)
            return;

        GetRegenerationRates(
            out float healthRegen,
            out float manaRegen,
            out float staminaRegen
        );

        bool changed = false;

        // HEALTH
        if (currentHealth < maxHealth && healthRegen > 0f)
        {
            healthRegenAccumulator +=
                healthRegen * Time.deltaTime;

            int restoreAmount =
                Mathf.FloorToInt(healthRegenAccumulator);

            if (restoreAmount > 0)
            {
                healthRegenAccumulator -= restoreAmount;

                int oldHealth = currentHealth;

                currentHealth = Mathf.Min(
                    maxHealth,
                    currentHealth + restoreAmount
                );

                if (currentHealth != oldHealth)
                    changed = true;
            }
        }
        else
        {
            healthRegenAccumulator = 0f;
        }

        // MANA
        if (currentMana < maxMana && manaRegen > 0f)
        {
            manaRegenAccumulator +=
                manaRegen * Time.deltaTime;

            int restoreAmount =
                Mathf.FloorToInt(manaRegenAccumulator);

            if (restoreAmount > 0)
            {
                manaRegenAccumulator -= restoreAmount;

                int oldMana = currentMana;

                currentMana = Mathf.Min(
                    maxMana,
                    currentMana + restoreAmount
                );

                if (currentMana != oldMana)
                    changed = true;
            }
        }
        else
        {
            manaRegenAccumulator = 0f;
        }

        // STAMINA
        if (currentStamina < maxStamina && staminaRegen > 0f)
        {
            staminaRegenAccumulator +=
                staminaRegen * Time.deltaTime;

            int restoreAmount =
                Mathf.FloorToInt(staminaRegenAccumulator);

            if (restoreAmount > 0)
            {
                staminaRegenAccumulator -= restoreAmount;

                int oldStamina = currentStamina;

                currentStamina = Mathf.Min(
                    maxStamina,
                    currentStamina + restoreAmount
                );

                if (currentStamina != oldStamina)
                    changed = true;
            }
        }
        else
        {
            staminaRegenAccumulator = 0f;
        }

        if (changed)
            NotifyChanged();
    }

    private void GetRegenerationRates(
        out float healthRegen,
        out float manaRegen,
        out float staminaRegen
    )
    {
        switch (playerClass)
        {
            case PlayerClass.Warrior:
                healthRegen = meleeHealthRegen;
                manaRegen = meleeManaRegen;
                staminaRegen = meleeStaminaRegen;
                break;

            case PlayerClass.Ranger:
                healthRegen = rangedHealthRegen;
                manaRegen = rangedManaRegen;
                staminaRegen = rangedStaminaRegen;
                break;

            case PlayerClass.Mage:
                healthRegen = mageHealthRegen;
                manaRegen = mageManaRegen;
                staminaRegen = mageStaminaRegen;
                break;

            default:
                healthRegen = 0f;
                manaRegen = 0f;
                staminaRegen = 0f;
                break;
        }
    }

    // -------------------------------------------------
    // SKILL
    // -------------------------------------------------

    private void OnSkillPerformed(
        InputAction.CallbackContext context)
    {
        UseSkill();
    }

    private void UseSkill()
    {
        if (IsDead)
            return;

        if (UseMana(skillManaCost))
        {
            Debug.Log("Skill activated!");

            // Add actual skill behavior here later.
        }
    }

    // -------------------------------------------------
    // PLAYER DEATH
    // -------------------------------------------------

    private void HandlePlayerDied()
    {
        DisableMovement();

        sprintDrainAccumulator = 0f;

        SpriteRenderer[] renderers =
            GetComponentsInChildren<SpriteRenderer>(true);

        foreach (SpriteRenderer renderer in renderers)
        {
            Color color = renderer.color;
            color.a = deadPlayerOpacity;
            renderer.color = color;
        }

        Debug.Log(
            "[PlayerStats] Player has died. Gameplay disabled."
        );
    }

    // -------------------------------------------------
    // EVENTS
    // -------------------------------------------------

    private void NotifyChanged()
    {
        Changed?.Invoke(this);
    }
}