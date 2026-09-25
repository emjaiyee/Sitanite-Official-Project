using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;


/// <summary>
/// Type of stats that equipments can have.
/// </summary>
public enum StatType
{
    Health,
    Mana,
    Stamina,
    HealthRegen,
    ManaRegen,
    StaminaRegen,
    MoveSpeed,
    BaseDamageResistance,
    DamageResistance,
    Damage,
    AttributeReduction,
    TraitReduction,
    Rejuvenation
}

public enum RejuvenationType
{
    Health,
    Stamina,
    Mana
}

public enum StatCapType
{
    None,
    PrimaryAttribute,
    SecondaryTrait
}

/// <summary>
/// Item rarity used for classification and inventory presentation.
/// </summary>
public enum ItemRarity
{
    Common,
    Uncommon,
    Rare,
    Legendary
}

/// <summary>
/// Determines how the stat calculation applies.
/// </summary>
public enum StatModifierType
{
    Flat,       
    Percent
}

public enum DamageSlot
{
    Primary,
    Secondary,
    Tertiary
}

/// <summary>
/// Individual stat modification entry.
/// </summary>
[Serializable]
public struct EquipmentStat
{
    public StatType statType;
    public PrimaryAttribute reducedAttribute;
    public SecondaryTrait reducedTrait;
    public RejuvenationType rejuvenationType;
    public StatModifierType modifierType;
    public float value;

    [HideInInspector] public DamageType damageType;
    [HideInInspector] public DamageSlot damageSlot;
    [HideInInspector] public bool lingeringDamage;
    [HideInInspector] public float lingeringBaseValue;
}

[Serializable]
public struct WeaponDamage
{
    public DamageType damageType;
    public DamageSlot damageSlot;
    public bool lingeringDamage;
    [Min(0f)] public float lingeringBaseValue;
    public StatModifierType modifierType;
    public float value;
}

[Serializable]
public class WeaponAttackSettings
{
    [SerializeField] public GameObject visualPrefab;
    [SerializeField] public GameObject projectilePrefab;
    [Min(0f)] [SerializeField] public float cooldown = 0.25f;
    [Min(0f)] [SerializeField] public float range = 1f;
    [Min(0f)] [SerializeField] public float projectileSpeed = 12f;
    [Min(0f)] [SerializeField] public float spellProjectileSpeed = 4f;
    [Min(0)] [SerializeField] public int cost;
    [SerializeField] public ResourceType resourceType = ResourceType.None;
    [SerializeField] public List<WeaponDamage> damages = new List<WeaponDamage>();

    public GameObject ProjectilePrefab => projectilePrefab;
    public GameObject VisualPrefab => visualPrefab;
    public float Cooldown => cooldown;
    public float Range => range;
    public float ProjectileSpeed => projectileSpeed;
    public float SpellProjectileSpeed => spellProjectileSpeed;
    public int Cost => cost;
    public ResourceType ResourceType => resourceType;
    public IReadOnlyList<WeaponDamage> Damages => damages;
}

[Serializable]
public class WeaponSkillSettings
{
    [SerializeField] public GameObject visualPrefab;
    [SerializeField] public GameObject projectilePrefab;
    [Min(0f)] [SerializeField] public float radius = 2f;
    [Min(0f)] [SerializeField] public float radiusMultiplier = 1f;
    [Range(1f, 360f)] [SerializeField] public float slashAngle = 90f;
    [Min(0f)] [SerializeField] public float range = 8f;
    [Min(0)] [SerializeField] public int projectileCount = 12;
    [Min(0f)] [SerializeField] public float duration = 0.6f;
    [Min(0.1f)] [SerializeField] public float damageTicksPerSecond = 10f;
    [Min(0f)] [SerializeField] public float visualDuration = 0.8f;
    [Min(0f)] [SerializeField] public float cooldown = 1f;
    [Min(0)] [SerializeField] public int cost = 25;
    [SerializeField] public ResourceType resourceType = ResourceType.Stamina;
    [SerializeField] public WeaponDamage damage;

    public GameObject VisualPrefab => visualPrefab;
    public GameObject ProjectilePrefab => projectilePrefab;
    public float Radius => radius;
    public float RadiusMultiplier => radiusMultiplier;
    public float SlashAngle => slashAngle;
    public float Range => range;
    public int ProjectileCount => projectileCount;
    public float Duration => duration;
    public float DamageTicksPerSecond => damageTicksPerSecond;
    public float VisualDuration => visualDuration;
    public float Cooldown => cooldown;
    public int Cost => cost;
    public ResourceType ResourceType => resourceType;
    public WeaponDamage Damage => damage;
    [Header("Crossbow Fire")]
public GameObject FireAreaPrefab;
public float FireDuration = 4f;
public float FireDamageInterval = 0.5f;
public int FireDamage = 10;
public float FireRadius = 2f;

}

[Serializable]
public class WeaponChargedSkillSettings
{
    [SerializeField] public GameObject visualPrefab;
    [SerializeField] public GameObject projectilePrefab;
    [SerializeField] public Sprite[] maxChargeSprites;
    [Min(0f)] [SerializeField] public float maxChargeAnimationSpeed = 12f;
    [Min(0f)] [SerializeField] public float startVisualScale = 1.5f;
    [Min(0f)] [SerializeField] public float endVisualScale = 1f;
    [Min(0f)] [SerializeField] public float maxVisualScale = 2f;
    [Min(0f)] [SerializeField] public float maxChargeTime = 2f;
    [Min(0)] [SerializeField] public int minimumDamage = 30;
    [Min(0)] [SerializeField] public int maximumDamage = 150;
    [Min(0f)] [SerializeField] public float beamDuration = 2f;
    [Min(0f)] [SerializeField] public float beamWidth = 1f;
    [Min(0.1f)] [SerializeField] public float damageTicksPerSecond = 1f;
    [Min(0)] [SerializeField] public int maxCost = 50;
    [SerializeField] public WeaponDamage damage;

    public GameObject VisualPrefab => visualPrefab;
    public GameObject ProjectilePrefab => projectilePrefab;
    public Sprite[] MaxChargeSprites => maxChargeSprites;
    public float MaxChargeAnimationSpeed => maxChargeAnimationSpeed;
    public float StartVisualScale => startVisualScale;
    public float EndVisualScale => endVisualScale;
    public float MaxVisualScale => maxVisualScale;
    public float MaxChargeTime => maxChargeTime;
    public int MinimumDamage => minimumDamage;
    public int MaximumDamage => maximumDamage;
    public float BeamDuration => beamDuration;
    public float BeamWidth => beamWidth;
    public float DamageTicksPerSecond => damageTicksPerSecond;
    public int MaxCost => maxCost;
    public WeaponDamage Damage => damage;
}

[Serializable]
public class WeaponStats
{
    [SerializeField] public string weaponId;
    [SerializeField] public WeaponAttackType attackType = WeaponAttackType.Melee;
    [SerializeField] public WeaponSkillType skillType = WeaponSkillType.None;
    [SerializeField] public bool homing;
    [SerializeField] public LayerMask hittableLayers;
    [SerializeField] public LayerMask skillHittableLayers;
    [SerializeField] public WeaponAttackSettings attack = new WeaponAttackSettings();
    [SerializeField] public WeaponSkillSettings skill = new WeaponSkillSettings();
    [SerializeField] public WeaponChargedSkillSettings chargedSkill = new WeaponChargedSkillSettings();

    public string WeaponId => weaponId;
    public WeaponAttackType AttackType => attackType;
    public WeaponSkillType SkillType => skillType;
    public bool Homing => homing;
    public LayerMask HittableLayers => hittableLayers;
    public LayerMask SkillHittableLayers => skillHittableLayers;
    public WeaponAttackSettings Attack => attack;
    public WeaponSkillSettings Skill => skill;
    public WeaponChargedSkillSettings ChargedSkill => chargedSkill;
}

[System.Serializable]
public enum ResourceType
{
    None,
    Stamina,
    Mana
}

/// <summary>
/// Type of equipments that the player can wear or hold.
/// </summary>
public enum EquipmentType
{
    None,
    Weapon,
    Helmet,
    Chestplate,
    Legging,
    Shield,
    Consumable
}

public enum WeaponAttackType
{
    Melee = 0,
    Ranged = 1,
    Spell = 2
}   

public enum WeaponSkillType
{
    None,
    AreaDamage,
    ArrowRain,
    ChargedArrow,
    Beam,
    Stab,
    CrossbowExplosion,
    Slash,
    SpinAxe,
    Typhoon,
    VeilOfFire,
    FireBalls,
    RapidFireBalls
}

/// <summary>
/// ScriptableObject asset defining base properties, UI icons, grid dimensions, 
/// and stacking behavior for inventory items.
/// </summary>
[CreateAssetMenu(fileName = "NewItemData", menuName = "Inventory/Item Data")]
public class ItemData : ScriptableObject
{

    #region Serialized Fields
    [Header("Display Settings")]
    [Tooltip("Display name shown in UI tooltips and inspect panels.")]
    public string itemName = "New Item";

    [Tooltip("Description text for inventory inspection.")]
    [TextArea(3, 6)] 
    public string itemDescription = string.Empty;

    [Tooltip("Icon sprite displayed inside inventory grid")]
    public Sprite inventoryIcon;

    [Tooltip("Icon sprite displayed by world loot pickups.")]
    public Sprite lootIcon;

    [Tooltip("Icon sprite displayed inside equipment slot (32px, 32px)")]
    public Sprite equipmentIcon;

    [Header("Classification")]
    [Tooltip("Target equipment slot.")]
    [SerializeField] private EquipmentType equipmentType = EquipmentType.None;

    [Tooltip("Rarity classification used by item data and inventory presentation.")]
    [SerializeField] private ItemRarity rarity = ItemRarity.Common;

    [Tooltip("Character definition applied while this item is equipped.")]
    [SerializeField] private CharacterPartDefinition characterDefinition;

    [Header("Stat Cap")]
    [SerializeField] private StatCapType statCapType = StatCapType.None;
    [SerializeField] private PrimaryAttribute statCapAttribute;
    [SerializeField] private SecondaryTrait statCapTrait;
    [Min(0)] [SerializeField] private int statCapValue;

    [Header("Armor and Consumable Stats")]
    [Tooltip("Attributes added or multiplied by armor, shields, or consumables.")]
    [SerializeField] private List<EquipmentStat> statModifiers = new List<EquipmentStat>();

    [Header("Weapon Stats")]
    [SerializeField] private WeaponStats weaponStats = new WeaponStats();

    [HideInInspector] [FormerlySerializedAs("weaponId")] [SerializeField] private string legacyWeaponId;
    [HideInInspector] [FormerlySerializedAs("weaponAttackType")] [SerializeField] private WeaponAttackType legacyWeaponAttackType;
    [HideInInspector] [FormerlySerializedAs("weaponSkillType")] [SerializeField] private WeaponSkillType legacyWeaponSkillType;
    [HideInInspector] [FormerlySerializedAs("attackRange")] [SerializeField] private float legacyAttackRange = 1f;
    [HideInInspector] [FormerlySerializedAs("hittableLayers")] [SerializeField] private LayerMask legacyHittableLayers;
    [HideInInspector] [FormerlySerializedAs("homing")] [SerializeField] private bool legacyHoming;
    [HideInInspector] [FormerlySerializedAs("attackCooldown")] [SerializeField] private float legacyAttackCooldown = 0.25f;
    [HideInInspector] [FormerlySerializedAs("projectilePrefab")] [SerializeField] private GameObject legacyProjectilePrefab;
    [HideInInspector] [FormerlySerializedAs("projectileSpeed")] [SerializeField] private float legacyProjectileSpeed = 12f;
    [HideInInspector] [FormerlySerializedAs("spellProjectileSpeed")] [SerializeField] private float legacySpellProjectileSpeed = 4f;
    [HideInInspector] [FormerlySerializedAs("skillDamage")] [SerializeField] private int legacySkillDamage = 50;
    [HideInInspector] [FormerlySerializedAs("skillRadius")] [SerializeField] private float legacySkillRadius = 2f;
    [HideInInspector] [FormerlySerializedAs("skillRadiusMultiplier")] [SerializeField] private float legacySkillRadiusMultiplier = 1f;
    [HideInInspector] [FormerlySerializedAs("skillHittableLayers")] [SerializeField] private LayerMask legacySkillHittableLayers;
    [HideInInspector] [FormerlySerializedAs("skillCooldown")] [SerializeField] private float legacySkillCooldown = 1f;
    [HideInInspector] [FormerlySerializedAs("skillVisualPrefab")] [SerializeField] private GameObject legacySkillVisualPrefab;
    [HideInInspector] [FormerlySerializedAs("skillRange")] [SerializeField] private float legacySkillRange = 8f;
    [HideInInspector] [FormerlySerializedAs("skillProjectileCount")] [SerializeField] private int legacySkillProjectileCount = 12;
    [HideInInspector] [FormerlySerializedAs("skillDuration")] [SerializeField] private float legacySkillDuration = 0.6f;
    [HideInInspector] [FormerlySerializedAs("skillVisualDuration")] [SerializeField] private float legacySkillVisualDuration = 0.8f;
    [HideInInspector] [FormerlySerializedAs("skillProjectilePrefab")] [SerializeField] private GameObject legacySkillProjectilePrefab;
    [HideInInspector] [FormerlySerializedAs("chargeVisualPrefab")] [SerializeField] private GameObject legacyChargeVisualPrefab;
    [HideInInspector] [FormerlySerializedAs("maxChargeSprites")] [SerializeField] private Sprite[] legacyMaxChargeSprites;
    [HideInInspector] [FormerlySerializedAs("maxChargeAnimationSpeed")] [SerializeField] private float legacyMaxChargeAnimationSpeed = 12f;
    [HideInInspector] [FormerlySerializedAs("startChargeVisualScale")] [SerializeField] private float legacyStartChargeVisualScale = 1.5f;
    [HideInInspector] [FormerlySerializedAs("endChargeVisualScale")] [SerializeField] private float legacyEndChargeVisualScale = 1f;
    [HideInInspector] [FormerlySerializedAs("maxChargeVisualScale")] [SerializeField] private float legacyMaxChargeVisualScale = 2f;
    [HideInInspector] [FormerlySerializedAs("maxChargeTime")] [SerializeField] private float legacyMaxChargeTime = 2f;
    [HideInInspector] [FormerlySerializedAs("minimumSkillDamage")] [SerializeField] private int legacyMinimumSkillDamage = 30;
    [HideInInspector] [FormerlySerializedAs("maximumSkillDamage")] [SerializeField] private int legacyMaximumSkillDamage = 150;
    [HideInInspector] [FormerlySerializedAs("beamDuration")] [SerializeField] private float legacyBeamDuration = 2f;
    [HideInInspector] [FormerlySerializedAs("beamWidth")] [SerializeField] private float legacyBeamWidth = 1f;
    [HideInInspector] [FormerlySerializedAs("damageTicksPerSecond")] [SerializeField] private float legacyDamageTicksPerSecond = 1f;
    [HideInInspector] [FormerlySerializedAs("skillCost")] [SerializeField] private int legacySkillCost = 25;
    [HideInInspector] [FormerlySerializedAs("skillResourceType")] [SerializeField] private ResourceType legacySkillResourceType = ResourceType.Stamina;
    [HideInInspector] [FormerlySerializedAs("maxChargeSkillCost")] [SerializeField] private int legacyMaxChargeSkillCost = 50;
    [HideInInspector] [FormerlySerializedAs("attackCost")] [SerializeField] private int legacyAttackCost;
    [HideInInspector] [FormerlySerializedAs("attackResourceType")] [SerializeField] private ResourceType legacyAttackResourceType;
    [SerializeField] private bool weaponStatsMigrated;

    [Header("Item Grid Layout")]
    [Tooltip("2D footprint dimensions in grid cells (X = Width, Y = Height).")]
    public Vector2Int gridSize = new Vector2Int(1, 1);

    [Tooltip("Width in grid cell")]
    public int gridWidth = 1;

    [Tooltip("Height in grid cell")]
    public int gridHeight = 1;

    [Header("Stacking configuration")]
    [Tooltip("Allows multiple quantities of the item.")]
    public bool isStackable = false;

    [Tooltip("Maximum item quantity allowed per stack.")]
    public int maxStackSize = 1;
    #endregion

    #region Properties
    /// <summary>Get designated equipment</summary>
    public EquipmentType EquipmentType => equipmentType;

    /// <summary>Get the item's rarity classification.</summary>
    public ItemRarity Rarity => rarity;

    /// <summary>Get the character definition applied by this item.</summary>
    public CharacterPartDefinition CharacterDefinition => characterDefinition;
    public StatCapType StatCapType => statCapType;
    public PrimaryAttribute StatCapAttribute => statCapAttribute;
    public SecondaryTrait StatCapTrait => statCapTrait;
    public int StatCapValue => statCapValue;

    public string WeaponId => string.IsNullOrWhiteSpace(weaponStats.WeaponId) ? itemName : weaponStats.WeaponId;
    public WeaponAttackType WeaponAttackType => weaponStats.AttackType;
    public WeaponSkillType WeaponSkillType => weaponStats.SkillType;
    public float AttackRange => weaponStats.Attack.Range;
    public LayerMask HittableLayers => weaponStats.HittableLayers;
    public bool Homing => weaponStats.Homing;
    public float AttackCooldown => weaponStats.Attack.Cooldown;
    public GameObject ProjectilePrefab => weaponStats.Attack.ProjectilePrefab;
    public GameObject AttackVisualPrefab => weaponStats.Attack.VisualPrefab;
    public float ProjectileSpeed => weaponStats.Attack.ProjectileSpeed;
    public float SpellProjectileSpeed => weaponStats.Attack.SpellProjectileSpeed;
    public int SkillDamage => Mathf.RoundToInt(weaponStats.Skill.Damage.value);
    public float SkillRadius => weaponStats.Skill.Radius;
    public float SkillRadiusMultiplier => weaponStats.Skill.RadiusMultiplier;
    public float SlashAngle => weaponStats.Skill.SlashAngle;
    public LayerMask SkillHittableLayers => weaponStats.SkillHittableLayers;
    public float SkillCooldown => weaponStats.Skill.Cooldown;
    public GameObject SkillVisualPrefab => weaponStats.Skill.VisualPrefab;
    public float SkillRange => weaponStats.Skill.Range;
    public int SkillProjectileCount => weaponStats.Skill.ProjectileCount;
    public float SkillDuration => weaponStats.Skill.Duration;
    public float SkillDamageTicksPerSecond => weaponStats.Skill.DamageTicksPerSecond;
    public float SkillVisualDuration => weaponStats.Skill.VisualDuration;
    public GameObject SkillProjectilePrefab => weaponStats.Skill.ProjectilePrefab;
    public GameObject ChargeVisualPrefab => weaponStats.ChargedSkill.VisualPrefab;
    public GameObject ChargedSkillProjectilePrefab => weaponStats.ChargedSkill.ProjectilePrefab;
    public Sprite[] MaxChargeSprites => weaponStats.ChargedSkill.MaxChargeSprites;
    public float MaxChargeAnimationSpeed => weaponStats.ChargedSkill.MaxChargeAnimationSpeed;
    public float StartChargeVisualScale => weaponStats.ChargedSkill.StartVisualScale;
    public float EndChargeVisualScale => weaponStats.ChargedSkill.EndVisualScale;
    public float MaxChargeVisualScale => weaponStats.ChargedSkill.MaxVisualScale;
    public float MaxChargeTime => weaponStats.ChargedSkill.MaxChargeTime;
    public int MinimumSkillDamage => weaponStats.ChargedSkill.MinimumDamage;
    public int MaximumSkillDamage => weaponStats.ChargedSkill.MaximumDamage;
    public float BeamDuration => weaponStats.ChargedSkill.BeamDuration;
    public float BeamWidth => weaponStats.ChargedSkill.BeamWidth;
    public float DamageTicksPerSecond => weaponStats.ChargedSkill.DamageTicksPerSecond;
    public int SkillCost => weaponStats.Skill.Cost;
public ResourceType SkillResourceType => weaponStats.Skill.ResourceType;

public GameObject FireAreaPrefab =>
    weaponStats.Skill.FireAreaPrefab;

public float FireDuration =>
    weaponStats.Skill.FireDuration;

public float FireDamageInterval =>
    weaponStats.Skill.FireDamageInterval;

public int FireDamage =>
    weaponStats.Skill.FireDamage;

public float FireRadius =>
    weaponStats.Skill.FireRadius;

public int MaxChargeSkillCost =>
    weaponStats.ChargedSkill.MaxCost;
    public int AttackCost => weaponStats.Attack.Cost;
    public ResourceType AttackResourceType => weaponStats.Attack.ResourceType;

    public int PrimaryDamage => GetDamageValue(DamageSlot.Primary, 0);
    public DamageType PrimaryDamageType => GetDamageType(DamageSlot.Primary, DamageType.Physical);

    /// <summary>Gets the configured damage value for the given damage slot.</summary>
    public int GetDamage(DamageSlot slot)
    {
        return GetDamageValue(slot, 0);
    }

    public float GetLingeringDamage(DamageSlot slot, PlayerStats playerStats)
    {
        foreach (WeaponDamage modifier in weaponStats.Attack.Damages)
        {
            if (modifier.damageSlot != slot ||
                !modifier.lingeringDamage)
                continue;

            float baseDamage = playerStats == null
                ? 0f
                : playerStats.GetBaseDamage(modifier.damageType);

            return baseDamage * 0.2f + modifier.lingeringBaseValue;
        }

        return 0f;
    }

    public int GetSkillDamage(DamageSlot slot)
    {
        return GetSkillDamage(slot, SkillDamage);
    }

    public int GetSkillDamage(DamageSlot slot, int baseDamage)
    {
        float multiplier = slot switch
        {
            DamageSlot.Primary => 1f,
            DamageSlot.Secondary => 0.3f,
            DamageSlot.Tertiary => 0.1f,
            _ => 0f
        };

        return Mathf.RoundToInt(baseDamage * multiplier);
    }

    public DamageType GetDamageType(DamageSlot slot)
    {
        return GetDamageType(slot, DamageType.None);
    }

    private int GetDamageValue(DamageSlot slot, int fallback)
    {
        foreach (WeaponDamage modifier in weaponStats.Attack.Damages)
        {
            if (modifier.damageSlot == slot &&
                modifier.damageType != DamageType.None)
                return Mathf.RoundToInt(modifier.value);
        }

        return fallback;
    }

    private DamageType GetDamageType(DamageSlot slot, DamageType fallback)
    {
        foreach (WeaponDamage modifier in weaponStats.Attack.Damages)
        {
            if (modifier.damageSlot == slot)
                return modifier.damageType;
        }

        return fallback;
    }

    /// <summary>Read-only collection of stat modifiers</summary>
    public IReadOnlyList<EquipmentStat> StatModifiers => statModifiers;
    public IReadOnlyList<WeaponDamage> WeaponDamages => weaponStats.Attack.Damages;
    public WeaponDamage SkillDamageEntry => weaponStats.Skill.Damage;
    public WeaponDamage ChargedSkillDamageEntry => weaponStats.ChargedSkill.Damage;

    public bool MeetsStatCap(PlayerAttributesNTraits attributes)
    {
        if (statCapType == StatCapType.None)
            return true;

        if (attributes == null)
            return false;

        return statCapType == StatCapType.PrimaryAttribute
            ? attributes.GetAttributeValue(statCapAttribute) >= statCapValue
            : attributes.GetTraitValue(statCapTrait) >= statCapValue;
    }

    public bool ApplyRejuvenation(PlayerStats playerStats)
    {
        if (equipmentType != EquipmentType.Consumable || playerStats == null)
            return false;

        bool applied = false;
        foreach (EquipmentStat modifier in statModifiers)
        {
            if (modifier.statType != StatType.Rejuvenation || modifier.value <= 0f)
                continue;

            float amount = modifier.modifierType == StatModifierType.Percent
                ? GetMaximumResource(playerStats, modifier.rejuvenationType) * modifier.value / 100f
                : modifier.value;

            switch (modifier.rejuvenationType)
            {
                case RejuvenationType.Health:
                    playerStats.Heal(amount);
                    break;
                case RejuvenationType.Stamina:
                    playerStats.RestoreStamina(amount);
                    break;
                case RejuvenationType.Mana:
                    playerStats.RestoreMana(amount);
                    break;
            }

            applied = true;
        }

        return applied;
    }

    private static float GetMaximumResource(PlayerStats playerStats, RejuvenationType type)
    {
        return type switch
        {
            RejuvenationType.Health => playerStats.MaxHealth,
            RejuvenationType.Stamina => playerStats.MaxStamina,
            RejuvenationType.Mana => playerStats.MaxMana,
            _ => 0f
        };
    }
    #endregion

    #region Lifecycle
    private void OnValidate()
    {
        MigrateWeaponStats();

        // Enforce minimum bounds and sync dimension in Inspector
        gridWidth = Mathf.Max(1, gridWidth);
        gridHeight = Mathf.Max(1, gridHeight);

        if (gridSize.x != gridWidth || gridSize.y != gridHeight)
        {
            gridSize = new Vector2Int(gridWidth, gridHeight);
        }

        if (!isStackable)
        {
            maxStackSize = 1;
        }
        else
        {
            maxStackSize = Mathf.Max(1, maxStackSize);
        }

        if (characterDefinition != null && equipmentType == EquipmentType.Helmet &&
            characterDefinition is not HeadwearDefinition)
        {
            Debug.LogWarning("Helmet items must reference a HeadwearDefinition.", this);
        }

        if (characterDefinition != null && equipmentType == EquipmentType.Weapon &&
            characterDefinition is not WeaponDefinition)
        {
            Debug.LogWarning("Weapon items must reference a WeaponDefinition.", this);
        }
    }

    private void MigrateWeaponStats()
    {
        if (weaponStatsMigrated)
            return;

        weaponStats.weaponId = legacyWeaponId;
        weaponStats.attackType = legacyWeaponAttackType;
        weaponStats.skillType = legacyWeaponSkillType;
        weaponStats.homing = legacyHoming;
        weaponStats.hittableLayers = legacyHittableLayers;
        weaponStats.skillHittableLayers = legacySkillHittableLayers;

        weaponStats.attack.projectilePrefab = legacyProjectilePrefab;
        weaponStats.attack.cooldown = legacyAttackCooldown;
        weaponStats.attack.range = legacyAttackRange;
        weaponStats.attack.projectileSpeed = legacyProjectileSpeed;
        weaponStats.attack.spellProjectileSpeed = legacySpellProjectileSpeed;
        weaponStats.attack.cost = legacyAttackCost;
        weaponStats.attack.resourceType = legacyAttackResourceType;
        weaponStats.attack.damages.Clear();

        foreach (EquipmentStat modifier in statModifiers)
        {
            if (modifier.statType != StatType.Damage)
                continue;

            weaponStats.attack.damages.Add(new WeaponDamage
            {
                damageType = modifier.damageType,
                damageSlot = modifier.damageSlot,
                lingeringDamage = modifier.lingeringDamage,
                lingeringBaseValue = modifier.lingeringBaseValue,
                modifierType = modifier.modifierType,
                value = modifier.value
            });
        }

        weaponStats.skill.visualPrefab = legacySkillVisualPrefab;
        weaponStats.skill.projectilePrefab = legacySkillProjectilePrefab;
        weaponStats.skill.radius = legacySkillRadius;
        weaponStats.skill.radiusMultiplier = legacySkillRadiusMultiplier;
        weaponStats.skill.range = legacySkillRange;
        weaponStats.skill.projectileCount = legacySkillProjectileCount;
        weaponStats.skill.duration = legacySkillDuration;
        weaponStats.skill.visualDuration = legacySkillVisualDuration;
        weaponStats.skill.cooldown = legacySkillCooldown;
        weaponStats.skill.cost = legacySkillCost;
        weaponStats.skill.resourceType = legacySkillResourceType;
        weaponStats.skill.damage = new WeaponDamage
        {
            damageType = GetDamageType(DamageSlot.Primary, DamageType.Physical),
            damageSlot = DamageSlot.Primary,
            modifierType = StatModifierType.Flat,
            value = legacySkillDamage
        };

        weaponStats.chargedSkill.visualPrefab = legacyChargeVisualPrefab;
        weaponStats.chargedSkill.projectilePrefab = legacySkillProjectilePrefab;
        weaponStats.chargedSkill.maxChargeSprites = legacyMaxChargeSprites;
        weaponStats.chargedSkill.maxChargeAnimationSpeed = legacyMaxChargeAnimationSpeed;
        weaponStats.chargedSkill.startVisualScale = legacyStartChargeVisualScale;
        weaponStats.chargedSkill.endVisualScale = legacyEndChargeVisualScale;
        weaponStats.chargedSkill.maxVisualScale = legacyMaxChargeVisualScale;
        weaponStats.chargedSkill.maxChargeTime = legacyMaxChargeTime;
        weaponStats.chargedSkill.minimumDamage = legacyMinimumSkillDamage;
        weaponStats.chargedSkill.maximumDamage = legacyMaximumSkillDamage;
        weaponStats.chargedSkill.beamDuration = legacyBeamDuration;
        weaponStats.chargedSkill.beamWidth = legacyBeamWidth;
        weaponStats.chargedSkill.damageTicksPerSecond = legacyDamageTicksPerSecond;
        weaponStats.chargedSkill.maxCost = legacyMaxChargeSkillCost;
        weaponStats.chargedSkill.damage = weaponStats.skill.damage;

        if (equipmentType == EquipmentType.Weapon)
            statModifiers.Clear();

        weaponStatsMigrated = true;
    }
    #endregion
}