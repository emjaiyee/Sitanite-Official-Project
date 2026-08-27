using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(PlayerAttributesNTraits))]
public class PlayerStats : MonoBehaviour
{
    [Header("Maximum Resources")]
    [Min(1)]
    public float maxHealth = 100f;

    [Min(1)]
    public float maxMana = 75f;

    [Min(1)]
    public float maxStamina = 75f;

    [Header("Effective Maximum Resources (Runtime)")]
    [SerializeField] private float effectiveMaxHealth;
    [SerializeField] private float effectiveMaxMana;
    [SerializeField] private float effectiveMaxStamina;

    [Header("Base Damage")]
    [Min(0)] public float basePierceDamage = 0f;
    [Min(0)] public float baseStabDamage = 0f;
    [Min(0)] public float baseSlashDamage = 0f;
    [Min(0)] public float baseBluntDamage = 0f;
    [Min(0)] public float baseFrostDamage = 0f;
    [Min(0)] public float basePoisonDamage = 0f;
    [Min(0)] public float baseLightningDamage = 0f;
    [Min(0)] public float basePsychicDamage = 0f;
    [Min(0)] public float baseNecrosisDamage = 0f;
    [Min(0)] public float baseWaterDamage = 0f;
    [Min(0)] public float baseEarthDamage = 0f;
    [Min(0)] public float baseFireDamage = 0f;
    [Min(0)] public float baseAirDamage = 0f;
    [Min(0)] public float basePhysicalDamage = 0f;

    [Header("Effective Damage (Runtime)")]
    [SerializeField] private float effectivePierceDamage;
    [SerializeField] private float effectiveStabDamage;
    [SerializeField] private float effectiveSlashDamage;
    [SerializeField] private float effectiveBluntDamage;
    [SerializeField] private float effectiveFrostDamage;
    [SerializeField] private float effectivePoisonDamage;
    [SerializeField] private float effectiveLightningDamage;
    [SerializeField] private float effectivePsychicDamage;
    [SerializeField] private float effectiveNecrosisDamage;
    [SerializeField] private float effectiveWaterDamage;
    [SerializeField] private float effectiveEarthDamage;
    [SerializeField] private float effectiveFireDamage;
    [SerializeField] private float effectiveAirDamage;
    [SerializeField] private float effectivePhysicalDamage;

    [Header("Base Damage Resistance")]
    [Min(0)] public float basePierceResistance = 0f;
    [Min(0)] public float baseStabResistance = 0f;
    [Min(0)] public float baseSlashResistance = 0f;
    [Min(0)] public float baseBluntResistance = 0f;
    [Min(0)] public float baseFrostResistance = 0f;
    [Min(0)] public float basePoisonResistance = 0f;
    [Min(0)] public float baseLightningResistance = 0f;
    [Min(0)] public float basePsychicResistance = 0f;
    [Min(0)] public float baseNecrosisResistance = 0f;
    [Min(0)] public float baseWaterResistance = 0f;
    [Min(0)] public float baseEarthResistance = 0f;
    [Min(0)] public float baseFireResistance = 0f;
    [Min(0)] public float baseAirResistance = 0f;
    [Min(0)] public float basePhysicalResistance = 0f;

    [Header("Effective Damage Resistance (Runtime)")]
    [SerializeField] private float effectivePierceResistance;
    [SerializeField] private float effectiveStabResistance;
    [SerializeField] private float effectiveSlashResistance;
    [SerializeField] private float effectiveBluntResistance;
    [SerializeField] private float effectiveFrostResistance;
    [SerializeField] private float effectivePoisonResistance;
    [SerializeField] private float effectiveLightningResistance;
    [SerializeField] private float effectivePsychicResistance;
    [SerializeField] private float effectiveNecrosisResistance;
    [SerializeField] private float effectiveWaterResistance;
    [SerializeField] private float effectiveEarthResistance;
    [SerializeField] private float effectiveFireResistance;
    [SerializeField] private float effectiveAirResistance;
    [SerializeField] private float effectivePhysicalResistance;

    [Header("Current Resources")]
    [SerializeField] private float currentHealth;
    [SerializeField] private float currentMana;
    [SerializeField] private float currentStamina;

    [Header("Character")]
    [SerializeField] private string characterName;
    [SerializeField] private CharacterGender gender;
    [SerializeField] private PlayerClass playerClass = PlayerClass.Warrior;

    [Header("References")]
    [SerializeField] private PlayerWASD movement;

    [SerializeField] private PlayerAttributesNTraits attributes;

    [Header("Movement")]
    [Min(0f)] public float moveSpeed = 1.2f;
    [Min(0f)] public float sprintSpeed = 1.8f;

    [Header("Effective Movement Values (Runtime)")]
    [SerializeField] private float effectiveMoveSpeed;
    [SerializeField] private float effectiveSprintSpeed;

    [Header("Dash")]
    [Min(0f)] public float dashSpeed = 2f;
    [Min(0f)] public float dashDuration = 0.2f;
    [Min(0)] public int dashCost = 20;

    [SerializeField] private float effectiveDashSpeed;

    [Header("Sprint")]
    [Min(0f)]
    [SerializeField] private float sprintCostPerSecond = 4f;

    [Range(0f, 2f)]
    [SerializeField] private float exhaustedSpeedMultiplier = 0.35f;

    [Header("Regeneration Settings (per second)")]

    [Min(0f)]
    [SerializeField] private float regenerationCooldown = 0.5f;

    [SerializeField]
    private float healthRegen = 1f;

    [SerializeField]
    private float manaRegen = 0f;

    [SerializeField]
    private float staminaRegen = 3f;

    [Header("Effective Regeneration (Runtime)")]
    [SerializeField] private float effectiveHealthRegen;
    [SerializeField] private float effectiveManaRegen;
    [SerializeField] private float effectiveStaminaRegen;

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

    public float CurrentHealth => currentHealth;
    public float CurrentMana => currentMana;
    public float CurrentStamina => currentStamina;

    public float MaxHealth => Mathf.Max(1f, ApplyEquipmentModifier(maxHealth + MaxHealthModifier, StatType.Health));
    public float MaxMana => Mathf.Max(1f, ApplyEquipmentModifier(maxMana + MaxManaModifier, StatType.Mana));
    public float MaxStamina => Mathf.Max(1f, ApplyEquipmentModifier(maxStamina + MaxStaminaModifier, StatType.Stamina));
    public float MoveSpeed => Mathf.Max(0f, ApplyEquipmentModifier(moveSpeed + MovementSpeedModifier, StatType.MoveSpeed));
    public float SprintSpeed => Mathf.Max(0f, ApplyEquipmentModifier(sprintSpeed + MovementSpeedModifier, StatType.MoveSpeed));
    public float DashSpeed => Mathf.Max(0f, ApplyEquipmentModifier(dashSpeed + MovementSpeedModifier, StatType.MoveSpeed));
    public float HealthRegen => ApplyEquipmentModifier(healthRegen + HealthRegenModifier, StatType.HealthRegen);
    public float ManaRegen => ApplyEquipmentModifier(manaRegen + ManaRegenModifier, StatType.ManaRegen);
    public float StaminaRegen => ApplyEquipmentModifier(staminaRegen + StaminaRegenModifier, StatType.StaminaRegen);

    // -------------------------------------------------
    // PRE-EQUIPMENT VALUES (base + attribute/trait modifiers).
    // The UI shows these as the main number; the colored
    // suffix is the equipment (ItemData) contribution only.
    // -------------------------------------------------

    public float PreEquipmentMaxHealth => Mathf.Max(1f, maxHealth + MaxHealthModifier);
    public float PreEquipmentMaxMana => Mathf.Max(1f, maxMana + MaxManaModifier);
    public float PreEquipmentMaxStamina => Mathf.Max(1f, maxStamina + MaxStaminaModifier);
    public float PreEquipmentMoveSpeed => Mathf.Max(0f, moveSpeed + MovementSpeedModifier);
    public float PreEquipmentSprintSpeed => Mathf.Max(0f, sprintSpeed + MovementSpeedModifier);
    public float PreEquipmentDashSpeed => Mathf.Max(0f, dashSpeed + MovementSpeedModifier);
    public float PreEquipmentHealthRegen => healthRegen + HealthRegenModifier;
    public float PreEquipmentManaRegen => manaRegen + ManaRegenModifier;
    public float PreEquipmentStaminaRegen => staminaRegen + StaminaRegenModifier;

    public float PierceDamage => EffectiveDamage(basePierceDamage, DamageType.Pierce);
    public float StabDamage => EffectiveDamage(baseStabDamage, DamageType.Stab);
    public float SlashDamage => EffectiveDamage(baseSlashDamage, DamageType.Slash);
    public float BluntDamage => EffectiveDamage(baseBluntDamage, DamageType.Blunt);
    public float FrostDamage => EffectiveDamage(baseFrostDamage, DamageType.Frost);
    public float PoisonDamage => EffectiveDamage(basePoisonDamage, DamageType.Poison);
    public float LightningDamage => EffectiveDamage(baseLightningDamage, DamageType.Lightning);
    public float PsychicDamage => EffectiveDamage(basePsychicDamage, DamageType.Psychic);
    public float NecrosisDamage => EffectiveDamage(baseNecrosisDamage, DamageType.Necrosis);
    public float WaterDamage => EffectiveDamage(baseWaterDamage, DamageType.Water);
    public float EarthDamage => EffectiveDamage(baseEarthDamage, DamageType.Earth);
    public float FireDamage => EffectiveDamage(baseFireDamage, DamageType.Fire);
    public float AirDamage => EffectiveDamage(baseAirDamage, DamageType.Air);
    public float PhysicalDamage => EffectiveDamage(basePhysicalDamage, DamageType.Physical);

    public float PierceResistance => ApplyEquipmentResistances(basePierceResistance + ResistanceModifier(DamageType.Pierce), DamageType.Pierce);
    public float StabResistance => ApplyEquipmentResistances(baseStabResistance + ResistanceModifier(DamageType.Stab), DamageType.Stab);
    public float SlashResistance => ApplyEquipmentResistances(baseSlashResistance + ResistanceModifier(DamageType.Slash), DamageType.Slash);
    public float BluntResistance => ApplyEquipmentResistances(baseBluntResistance + ResistanceModifier(DamageType.Blunt), DamageType.Blunt);
    public float FrostResistance => ApplyEquipmentResistances(baseFrostResistance + ResistanceModifier(DamageType.Frost), DamageType.Frost);
    public float PoisonResistance => ApplyEquipmentResistances(basePoisonResistance + ResistanceModifier(DamageType.Poison), DamageType.Poison);
    public float LightningResistance => ApplyEquipmentResistances(baseLightningResistance + ResistanceModifier(DamageType.Lightning), DamageType.Lightning);
    public float PsychicResistance => ApplyEquipmentResistances(basePsychicResistance + ResistanceModifier(DamageType.Psychic), DamageType.Psychic);
    public float NecrosisResistance => ApplyEquipmentResistances(baseNecrosisResistance + ResistanceModifier(DamageType.Necrosis), DamageType.Necrosis);
    public float WaterResistance => ApplyEquipmentResistances(baseWaterResistance + ResistanceModifier(DamageType.Water), DamageType.Water);
    public float EarthResistance => ApplyEquipmentResistances(baseEarthResistance + ResistanceModifier(DamageType.Earth), DamageType.Earth);
    public float FireResistance => ApplyEquipmentResistances(baseFireResistance + ResistanceModifier(DamageType.Fire), DamageType.Fire);
    public float AirResistance => ApplyEquipmentResistances(baseAirResistance + ResistanceModifier(DamageType.Air), DamageType.Air);
    public float PhysicalResistance => ApplyEquipmentResistances(basePhysicalResistance + ResistanceModifier(DamageType.Physical), DamageType.Physical);

    public float GetBaseResistance(DamageType damageType)
    {
        return damageType switch
        {
            DamageType.Pierce => basePierceResistance,
            DamageType.Stab => baseStabResistance,
            DamageType.Slash => baseSlashResistance,
            DamageType.Blunt => baseBluntResistance,
            DamageType.Frost => baseFrostResistance,
            DamageType.Poison => basePoisonResistance,
            DamageType.Lightning => baseLightningResistance,
            DamageType.Psychic => basePsychicResistance,
            DamageType.Necrosis => baseNecrosisResistance,
            DamageType.Water => baseWaterResistance,
            DamageType.Earth => baseEarthResistance,
            DamageType.Fire => baseFireResistance,
            DamageType.Air => baseAirResistance,
            DamageType.Physical => basePhysicalResistance,
            _ => 0f
        };
    }

    /// <summary>Base resistance + attribute/trait modifier, before equipment.</summary>
    public float GetPreEquipmentResistance(DamageType damageType)
    {
        return GetBaseResistance(damageType) + ResistanceModifier(damageType);
    }

    public float GetEffectiveDamage(DamageType damageType)
    {
        return damageType switch
        {
            DamageType.Pierce => PierceDamage,
            DamageType.Stab => StabDamage,
            DamageType.Slash => SlashDamage,
            DamageType.Blunt => BluntDamage,
            DamageType.Frost => FrostDamage,
            DamageType.Poison => PoisonDamage,
            DamageType.Lightning => LightningDamage,
            DamageType.Psychic => PsychicDamage,
            DamageType.Necrosis => NecrosisDamage,
            DamageType.Water => WaterDamage,
            DamageType.Earth => EarthDamage,
            DamageType.Fire => FireDamage,
            DamageType.Air => AirDamage,
            DamageType.Physical => PhysicalDamage,
            _ => 0f
        };
    }

    public bool UseResource(int amount, ResourceType resourceType)
    {
        if (amount <= 0)
            return true;

        switch (resourceType)
        {
            case ResourceType.Stamina:
                return UseStamina(amount);
            case ResourceType.Mana:
                return UseMana(amount);
            default:
                Debug.LogWarning($"Unsupported resource type: {resourceType}");
                return false;
        }
    }

    public float GetDamageResistance(DamageType damageType)
    {
        if ((damageType & DamageType.Pierce) != 0)
            return PierceResistance;
        if ((damageType & DamageType.Stab) != 0)
            return StabResistance;
        if ((damageType & DamageType.Slash) != 0)
            return SlashResistance;
        if ((damageType & DamageType.Blunt) != 0)
            return BluntResistance;
        if ((damageType & DamageType.Frost) != 0)
            return FrostResistance;
        if ((damageType & DamageType.Poison) != 0)
            return PoisonResistance;
        if ((damageType & DamageType.Lightning) != 0)
            return LightningResistance;
        if ((damageType & DamageType.Psychic) != 0)
            return PsychicResistance;
        if ((damageType & DamageType.Necrosis) != 0)
            return NecrosisResistance;
        if ((damageType & DamageType.Water) != 0)
            return WaterResistance;
        if ((damageType & DamageType.Earth) != 0)
            return EarthResistance;
        if ((damageType & DamageType.Fire) != 0)
            return FireResistance;
        if ((damageType & DamageType.Air) != 0)
            return AirResistance;
        if ((damageType & DamageType.Physical) != 0)
            return PhysicalResistance;

        return 0;
    }

    private float ApplyEquipmentResistances(float baseResistance, DamageType damageType)
    {
        float resistance = ApplyEquipmentModifier(
            baseResistance,
            StatType.BaseDamageResistance,
            damageType
        );

        return ApplyEquipmentModifier(
            resistance,
            StatType.DamageResistance,
            damageType
        );
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
    private float healthRegenCooldown;
    private float manaRegenCooldown;
    private float staminaRegenCooldown;

    // -------------------------------------------------
    // SPRINT ACCUMULATOR
    // -------------------------------------------------

    private float sprintDrainAccumulator;

    private float MaxHealthModifier => attributes != null ? attributes.MaxHealthModifier : 0f;
    private float MaxManaModifier => attributes != null ? attributes.MaxManaModifier : 0f;
    private float MaxStaminaModifier => attributes != null ? attributes.MaxStaminaModifier : 0f;
    private float HealthRegenModifier => attributes != null ? attributes.HealthRegenModifier : 0f;
    private float ManaRegenModifier => attributes != null ? attributes.ManaRegenModifier : 0f;
    private float StaminaRegenModifier => attributes != null ? attributes.StaminaRegenModifier : 0f;
    private float MovementSpeedModifier => attributes != null ? attributes.MovementSpeedModifier : 0f;
    public float CooldownReduction => attributes != null ? attributes.CooldownReduction : 0f;

    private float ApplyEquipmentModifier(float baseValue, StatType statType, DamageType damageType = DamageType.None)
    {
        return EquipmentManager.Instance == null
            ? baseValue
            : EquipmentManager.Instance.GetModifiedStat(baseValue, statType, damageType);
    }

    private float GetDamageModifier(DamageType damageType)
    {
        return attributes != null ? attributes.GetDamageModifier(damageType) : 0;
    }

    private float EffectiveDamage(float baseDamage, DamageType damageType)
    {
        return Mathf.Max(
            0,
            ApplyEquipmentModifier(
                baseDamage + GetDamageModifier(damageType),
                StatType.Damage,
                damageType
            )
        );
    }

    public float GetBaseDamage(DamageType damageType)
    {
        return damageType switch
        {
            DamageType.Pierce => basePierceDamage,
            DamageType.Stab => baseStabDamage,
            DamageType.Slash => baseSlashDamage,
            DamageType.Blunt => baseBluntDamage,
            DamageType.Frost => baseFrostDamage,
            DamageType.Poison => basePoisonDamage,
            DamageType.Lightning => baseLightningDamage,
            DamageType.Psychic => basePsychicDamage,
            DamageType.Necrosis => baseNecrosisDamage,
            DamageType.Water => baseWaterDamage,
            DamageType.Earth => baseEarthDamage,
            DamageType.Fire => baseFireDamage,
            DamageType.Air => baseAirDamage,
            DamageType.Physical => basePhysicalDamage,
            _ => 0f
        };
    }

    /// <summary>Base damage + attribute/trait modifier, before equipment.</summary>
    public float GetPreEquipmentDamage(DamageType damageType)
    {
        return GetBaseDamage(damageType) + GetDamageModifier(damageType);
    }

    private float ResistanceModifier(DamageType damageType)
    {
        if ((damageType & (DamageType.Pierce | DamageType.Stab | DamageType.Slash |
                           DamageType.Blunt | DamageType.Physical)) != 0)
            return attributes != null ? attributes.DamageResistanceModifier : 0f;

        return attributes != null ? attributes.MagicalResistanceModifier : 0f;
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
        UpdateEffectiveRuntimeValues();
        ResetToFull();
    }

    private void InitializeDamageDefaults()
    {
        basePierceDamage = 5;
        baseStabDamage = 5;
        baseSlashDamage = 5;
        baseBluntDamage = 5;
        basePhysicalDamage = 5;

        baseFrostDamage = 3;
        basePoisonDamage = 3;
        baseLightningDamage = 3;
        basePsychicDamage = 3;
        baseNecrosisDamage = 3;
        baseWaterDamage = 3;
        baseEarthDamage = 3;
        baseFireDamage = 3;
        baseAirDamage = 3;

        basePierceResistance = 5;
        baseStabResistance = 5;
        baseSlashResistance = 5;
        baseBluntResistance = 5;
        basePhysicalResistance = 5;

        baseFrostResistance = 3;
        basePoisonResistance = 3;
        baseLightningResistance = 3;
        basePsychicResistance = 3;
        baseNecrosisResistance = 3;
        baseWaterResistance = 3;
        baseEarthResistance = 3;
        baseFireResistance = 3;
        baseAirResistance = 3;
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

    private void OnEnable()
    {
        if (attributes != null)
            attributes.Changed += HandleAttributesChanged;
    }

    private void OnDisable()
    {
        if (attributes != null)
            attributes.Changed -= HandleAttributesChanged;
    }

    private void HandleAttributesChanged(PlayerAttributesNTraits source)
    {
        UpdateEffectiveRuntimeValues();
        NotifyChanged();
    }

    public void NotifyStatsChanged()
    {
        UpdateEffectiveRuntimeValues();
        NotifyChanged();
    }

    private void UpdateEffectiveRuntimeValues()
    {
        effectiveMaxHealth = MaxHealth;
        effectiveMaxMana = MaxMana;
        effectiveMaxStamina = MaxStamina;

        effectiveMoveSpeed = MoveSpeed;
        effectiveSprintSpeed = SprintSpeed;
        effectiveDashSpeed = DashSpeed;

        effectivePierceDamage = PierceDamage;
        effectiveStabDamage = StabDamage;
        effectiveSlashDamage = SlashDamage;
        effectiveBluntDamage = BluntDamage;
        effectiveFrostDamage = FrostDamage;
        effectivePoisonDamage = PoisonDamage;
        effectiveLightningDamage = LightningDamage;
        effectivePsychicDamage = PsychicDamage;
        effectiveNecrosisDamage = NecrosisDamage;
        effectiveWaterDamage = WaterDamage;
        effectiveEarthDamage = EarthDamage;
        effectiveFireDamage = FireDamage;
        effectiveAirDamage = AirDamage;
        effectivePhysicalDamage = PhysicalDamage;

        effectivePierceResistance = PierceResistance;
        effectiveStabResistance = StabResistance;
        effectiveSlashResistance = SlashResistance;
        effectiveBluntResistance = BluntResistance;
        effectiveFrostResistance = FrostResistance;
        effectivePoisonResistance = PoisonResistance;
        effectiveLightningResistance = LightningResistance;
        effectivePsychicResistance = PsychicResistance;
        effectiveNecrosisResistance = NecrosisResistance;
        effectiveWaterResistance = WaterResistance;
        effectiveEarthResistance = EarthResistance;
        effectiveFireResistance = FireResistance;
        effectiveAirResistance = AirResistance;
        effectivePhysicalResistance = PhysicalResistance;

        effectiveHealthRegen = HealthRegen;
        effectiveManaRegen = ManaRegen;
        effectiveStaminaRegen = StaminaRegen;
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
        healthRegenCooldown = 0f;
        manaRegenCooldown = 0f;
        staminaRegenCooldown = 0f;
        sprintDrainAccumulator = 0f;

        IsDead = false;

        if (movement != null)
            movement.SpeedMultiplier = 1f;

        NotifyChanged();
    }

    // -------------------------------------------------
    // HEALTH
    // -------------------------------------------------

    public void TakeDamage(float amount, DamageType damageType = DamageType.Slash)
    {
        if (amount <= 0 || IsDead || currentHealth <= 0)
            return;

        amount = Mathf.Max(0f, amount - GetDamageResistance(damageType));

        if (amount <= 0)
            return;

        currentHealth = Mathf.Max(
            0,
            currentHealth - amount
        );

        // Show the damage number above the player, colored by type.
        DamagePopupSpawner.Spawn(transform, amount, damageType, 1f);

        healthRegenCooldown = regenerationCooldown;
        healthRegenAccumulator = 0f;

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
        Heal((float)amount);
    }

    public void Heal(float amount)
    {
        if (amount <= 0 || IsDead || currentHealth <= 0)
            return;

        currentHealth = Mathf.Min(MaxHealth, currentHealth + amount);

        NotifyChanged();
    }

    // -------------------------------------------------
    // MANA
    // -------------------------------------------------

    public bool UseMana(int amount)
    {
        return UseMana((float)amount);
    }

    public bool UseMana(float amount)
    {
        if (amount <= 0 || currentMana < amount)
            return false;

        currentMana = amount >= currentMana
            ? 0f
            : currentMana - amount;

        manaRegenCooldown = regenerationCooldown;
        manaRegenAccumulator = 0f;

        NotifyChanged();

        return true;
    }

    public void RestoreMana(int amount)
    {
        RestoreMana((float)amount);
    }

    public void RestoreMana(float amount)
    {
        if (amount <= 0)
            return;

        currentMana = Mathf.Min(MaxMana, currentMana + amount);

        NotifyChanged();
    }

    // -------------------------------------------------
    // STAMINA
    // -------------------------------------------------

    public bool UseStamina(int amount)
    {
        return UseStamina((float)amount);
    }

    public bool UseStamina(float amount)
    {
        if (amount <= 0 || currentStamina < amount)
            return false;

        currentStamina = amount >= currentStamina
            ? 0f
            : currentStamina - amount;

        staminaRegenCooldown = regenerationCooldown;
        staminaRegenAccumulator = 0f;

        NotifyChanged();

        return true;
    }

    public void RestoreStamina(int amount)
    {
        RestoreStamina((float)amount);
    }

    public void RestoreStamina(float amount)
    {
        if (amount <= 0)
            return;

        currentStamina = Mathf.Min(MaxStamina, currentStamina + amount);

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

        if (sprintDrainAccumulator > 0f)
        {
            float drainAmount =
                Mathf.Min(sprintDrainAccumulator, currentStamina);

            sprintDrainAccumulator -= drainAmount;

            UseStamina(drainAmount);

            if (currentStamina <= 0f)
                sprintDrainAccumulator = 0f;
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

        float currentHealthRegen = HealthRegen;
        float currentManaRegen = ManaRegen;
        float currentStaminaRegen = StaminaRegen;

        bool changed = false;

        // HEALTH
        if (healthRegenCooldown > 0f)
        {
            healthRegenCooldown = Mathf.Max(
                0f,
                healthRegenCooldown - Time.deltaTime
            );
            healthRegenAccumulator = 0f;
        }
        else if (currentHealth < MaxHealth && currentHealthRegen > 0f)
        {
            healthRegenAccumulator +=
                currentHealthRegen * Time.deltaTime;

            float restoreAmount = healthRegenAccumulator;

            if (restoreAmount > 0)
            {
                healthRegenAccumulator = 0f;

                float oldHealth = currentHealth;

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
        if (manaRegenCooldown > 0f)
        {
            manaRegenCooldown = Mathf.Max(
                0f,
                manaRegenCooldown - Time.deltaTime
            );
            manaRegenAccumulator = 0f;
        }
        else if (currentMana < MaxMana && currentManaRegen > 0f)
        {
            manaRegenAccumulator +=
                currentManaRegen * Time.deltaTime;

            float restoreAmount = manaRegenAccumulator;

            if (restoreAmount > 0)
            {
                manaRegenAccumulator = 0f;

                float oldMana = currentMana;

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
        if (staminaRegenCooldown > 0f)
        {
            staminaRegenCooldown = Mathf.Max(
                0f,
                staminaRegenCooldown - Time.deltaTime
            );
            staminaRegenAccumulator = 0f;
        }
        else if (currentStamina < MaxStamina && currentStaminaRegen > 0f)
        {
            staminaRegenAccumulator +=
                currentStaminaRegen * Time.deltaTime;

            float restoreAmount = staminaRegenAccumulator;

            if (restoreAmount > 0)
            {
                staminaRegenAccumulator = 0f;

                float oldStamina = currentStamina;

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