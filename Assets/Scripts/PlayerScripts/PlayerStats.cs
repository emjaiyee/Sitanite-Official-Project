using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(PlayerAttributesNTraits))]
public class PlayerStats : MonoBehaviour
{
    [Header("Maximum Resources")]
    [Min(1)]
    public int maxHealth = 100;

    [Min(1)]
    public int maxMana = 75;

    [Min(1)]
    public int maxStamina = 75;

    [Header("Base Damage")]
    [Min(0)] public int basePierceDamage = 0;
    [Min(0)] public int baseStabDamage = 0;
    [Min(0)] public int baseSlashDamage = 0;
    [Min(0)] public int baseBluntDamage = 0;
    [Min(0)] public int baseBurningDamage = 0;
    [Min(0)] public int baseFrostDamage = 0;
    [Min(0)] public int basePoisonDamage = 0;
    [Min(0)] public int baseLightningDamage = 0;
    [Min(0)] public int basePsychicDamage = 0;
    [Min(0)] public int basePhysicalDamage = 0;

    [Header("Base Damage Resistance")]
    [Min(0)] public int basePierceResistance = 0;
    [Min(0)] public int baseStabResistance = 0;
    [Min(0)] public int baseSlashResistance = 0;
    [Min(0)] public int baseBluntResistance = 0;
    [Min(0)] public int baseBurningResistance = 0;
    [Min(0)] public int baseFrostResistance = 0;
    [Min(0)] public int basePoisonResistance = 0;
    [Min(0)] public int baseLightningResistance = 0;
    [Min(0)] public int basePsychicResistance = 0;
    [Min(0)] public int basePhysicalResistance = 0;

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

    [SerializeField] private PlayerAttributesNTraits attributes;

    [Header("Movement")]
    [Min(0f)] public float moveSpeed = 5f;
    [Min(0f)] public float sprintSpeed = 8f;

    [Header("Dash")]
    [Min(0f)] public float dashSpeed = 10f;
    [Min(0f)] public float dashDuration = 0.2f;
    [Min(0)] public int dashCost = 20;

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

    public int MaxHealth => Mathf.Max(1, maxHealth + MaxHealthModifier);
    public int MaxMana => Mathf.Max(1, maxMana + MaxManaModifier);
    public int MaxStamina => Mathf.Max(1, maxStamina + MaxStaminaModifier);

    public int PierceDamage => EffectiveDamage(basePierceDamage, DamageType.Pierce);
    public int StabDamage => EffectiveDamage(baseStabDamage, DamageType.Stab);
    public int SlashDamage => EffectiveDamage(baseSlashDamage, DamageType.Slash);
    public int BluntDamage => EffectiveDamage(baseBluntDamage, DamageType.Blunt);
    public int BurningDamage => EffectiveDamage(baseBurningDamage, DamageType.Burning);
    public int FrostDamage => EffectiveDamage(baseFrostDamage, DamageType.Frost);
    public int PoisonDamage => EffectiveDamage(basePoisonDamage, DamageType.Poison);
    public int LightningDamage => EffectiveDamage(baseLightningDamage, DamageType.Lightning);
    public int PsychicDamage => EffectiveDamage(basePsychicDamage, DamageType.Psychic);
    public int PhysicalDamage => EffectiveDamage(basePhysicalDamage, DamageType.Physical);

    public int PierceResistance => basePierceResistance;
    public int StabResistance => baseStabResistance;
    public int SlashResistance => baseSlashResistance;
    public int BluntResistance => baseBluntResistance;
    public int BurningResistance => baseBurningResistance;
    public int FrostResistance => baseFrostResistance;
    public int PoisonResistance => basePoisonResistance;
    public int LightningResistance => baseLightningResistance;
    public int PsychicResistance => basePsychicResistance;
    public int PhysicalResistance => basePhysicalResistance;

    public int GetDamageResistance(DamageType damageType)
    {
        if ((damageType & DamageType.Pierce) != 0)
            return PierceResistance;
        if ((damageType & DamageType.Stab) != 0)
            return StabResistance;
        if ((damageType & DamageType.Slash) != 0)
            return SlashResistance;
        if ((damageType & DamageType.Blunt) != 0)
            return BluntResistance;
        if ((damageType & DamageType.Burning) != 0)
            return BurningResistance;
        if ((damageType & DamageType.Frost) != 0)
            return FrostResistance;
        if ((damageType & DamageType.Poison) != 0)
            return PoisonResistance;
        if ((damageType & DamageType.Lightning) != 0)
            return LightningResistance;
        if ((damageType & DamageType.Psychic) != 0)
            return PsychicResistance;
        if ((damageType & DamageType.Physical) != 0)
            return PhysicalResistance;

        return 0;
    }

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

    private int MaxHealthModifier => attributes != null ? attributes.MaxHealthModifier : 0;
    private int MaxManaModifier => attributes != null ? attributes.MaxManaModifier : 0;
    private int MaxStaminaModifier => attributes != null ? attributes.MaxStaminaModifier : 0;

    private int GetDamageModifier(DamageType damageType)
    {
        return attributes != null ? attributes.GetDamageModifier(damageType) : 0;
    }

    private int EffectiveDamage(int baseDamage, DamageType damageType)
    {
        return Mathf.Max(0, baseDamage + GetDamageModifier(damageType));
    }

    // -------------------------------------------------
    // UNITY
    // -------------------------------------------------

    private void Awake()
    {
        if (movement == null)
            movement = GetComponent<PlayerWASD>();

        if (attributes == null)
            attributes = GetComponent<PlayerAttributesNTraits>();

        InitializeDamageDefaults();
        ResetToFull();
    }

    private void InitializeDamageDefaults()
    {
        basePierceDamage = 5;
        baseStabDamage = 5;
        baseSlashDamage = 5;
        baseBluntDamage = 5;
        basePhysicalDamage = 5;

        baseBurningDamage = 3;
        baseFrostDamage = 3;
        basePoisonDamage = 3;
        baseLightningDamage = 3;
        basePsychicDamage = 3;

        basePierceResistance = 5;
        baseStabResistance = 5;
        baseSlashResistance = 5;
        baseBluntResistance = 5;
        basePhysicalResistance = 5;

        baseBurningResistance = 3;
        baseFrostResistance = 3;
        basePoisonResistance = 3;
        baseLightningResistance = 3;
        basePsychicResistance = 3;
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
        currentHealth = MaxHealth;
        currentMana = MaxMana;
        currentStamina = MaxStamina;

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

    public void TakeDamage(int amount, DamageType damageType = DamageType.Slash)
    {
        if (amount <= 0 || IsDead || currentHealth <= 0)
            return;

        amount = Mathf.Max(0, amount - GetDamageResistance(damageType));

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
            MaxHealth,
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
            MaxMana,
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
            MaxStamina,
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
        if (currentHealth < MaxHealth && healthRegen > 0f)
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
                    MaxHealth,
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
        if (currentMana < MaxMana && manaRegen > 0f)
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
                    MaxMana,
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
        if (currentStamina < MaxStamina && staminaRegen > 0f)
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
                    MaxStamina,
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