using System;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Type of stats that equipments can have.
/// </summary>
public enum StatType
{
    Health,
    Mana,
    MoveSpeed,
    BaseDamageResistance,
    DamageResistance,
    Damage,
    AttributeReduction,
    TraitReduction
}

public enum StatCapType
{
    None,
    PrimaryAttribute,
    SecondaryTrait
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
    public DamageType damageType;
    public DamageSlot damageSlot;
    public PrimaryAttribute reducedAttribute;
    public SecondaryTrait reducedTrait;
    public StatModifierType modifierType;
    public float value;
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
    Shield
}

public enum WeaponAttackType
{
    None,
    Melee,
    Ranged
}

public enum WeaponSkillType
{
    None,
    AreaDamage,
    ArrowRain,
    ChargedArrow,
    Beam
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

    [Tooltip("Icon sprite displayed inside equipment slot (32px, 32px)")]
    public Sprite equipmentIcon;

    [Header("Classification")]
    [Tooltip("Target equipment slot.")]
    [SerializeField] private EquipmentType equipmentType = EquipmentType.None;

    [Tooltip("Character definition applied while this item is equipped.")]
    [SerializeField] private CharacterPartDefinition characterDefinition;

    [Header("Stat Cap")]
    [SerializeField] private StatCapType statCapType = StatCapType.None;
    [SerializeField] private PrimaryAttribute statCapAttribute;
    [SerializeField] private SecondaryTrait statCapTrait;
    [Min(0)] [SerializeField] private int statCapValue;

    [Header("Weapon Settings")]
    [Tooltip("Stable identifier used by gameplay systems.")]
    [SerializeField] private string weaponId;

    [SerializeField] private WeaponAttackType weaponAttackType = WeaponAttackType.None;
    [SerializeField] private WeaponSkillType weaponSkillType = WeaponSkillType.None;
    [SerializeField] private float attackRange = 1f;
    [SerializeField] private LayerMask hittableLayers;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed = 12f;
    [SerializeField] private int skillDamage = 50;
    [SerializeField] private float skillRadius = 2f;
    [SerializeField] private float skillRadiusMultiplier = 1f;
    [SerializeField] private LayerMask skillHittableLayers;
    [SerializeField] private GameObject skillVisualPrefab;
    [SerializeField] private float skillRange = 8f;
    [SerializeField] private int skillProjectileCount = 12;
    [SerializeField] private float skillDuration = 0.6f;
    [SerializeField] private float skillVisualDuration = 0.8f;
    [SerializeField] private GameObject skillProjectilePrefab;
    [SerializeField] private GameObject chargeVisualPrefab;
    [SerializeField] private float maxChargeVisualScale = 1.5f;
    [SerializeField] private float maxChargeTime = 2f;
    [SerializeField] private int minimumSkillDamage = 30;
    [SerializeField] private int maximumSkillDamage = 150;
    [SerializeField] private float beamDuration = 2f;
    [SerializeField] private float beamWidth = 1f;

    [Header("Stat Modifiers")]
    [Tooltip("Attributes added or multiplied when this item is equipped.")]
    [SerializeField] private List<EquipmentStat> statModifiers = new List<EquipmentStat>();

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

    /// <summary>Get the character definition applied by this item.</summary>
    public CharacterPartDefinition CharacterDefinition => characterDefinition;
    public StatCapType StatCapType => statCapType;
    public PrimaryAttribute StatCapAttribute => statCapAttribute;
    public SecondaryTrait StatCapTrait => statCapTrait;
    public int StatCapValue => statCapValue;

    public string WeaponId => string.IsNullOrWhiteSpace(weaponId) ? itemName : weaponId;
    public WeaponAttackType WeaponAttackType => weaponAttackType;
    public WeaponSkillType WeaponSkillType => weaponSkillType;
    public float AttackRange => attackRange;
    public LayerMask HittableLayers => hittableLayers;
    public GameObject ProjectilePrefab => projectilePrefab;
    public float ProjectileSpeed => projectileSpeed;
    public int SkillDamage => skillDamage;
    public float SkillRadius => skillRadius;
    public float SkillRadiusMultiplier => skillRadiusMultiplier;
    public LayerMask SkillHittableLayers => skillHittableLayers;
    public GameObject SkillVisualPrefab => skillVisualPrefab;
    public float SkillRange => skillRange;
    public int SkillProjectileCount => skillProjectileCount;
    public float SkillDuration => skillDuration;
    public float SkillVisualDuration => skillVisualDuration;
    public GameObject SkillProjectilePrefab => skillProjectilePrefab;
    public GameObject ChargeVisualPrefab => chargeVisualPrefab;
    public float MaxChargeVisualScale => maxChargeVisualScale;
    public float MaxChargeTime => maxChargeTime;
    public int MinimumSkillDamage => minimumSkillDamage;
    public int MaximumSkillDamage => maximumSkillDamage;
    public float BeamDuration => beamDuration;
    public float BeamWidth => beamWidth;

    public int PrimaryDamage => GetDamageValue(DamageSlot.Primary, 0);
    public DamageType PrimaryDamageType => GetDamageType(DamageSlot.Primary, DamageType.Physical);

    public int GetSkillDamage(DamageSlot slot)
    {
        return GetSkillDamage(slot, skillDamage);
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
        foreach (EquipmentStat modifier in statModifiers)
        {
            if (modifier.statType == StatType.Damage &&
                modifier.damageSlot == slot &&
                modifier.damageType != DamageType.None)
                return Mathf.RoundToInt(modifier.value);
        }

        return fallback;
    }

    private DamageType GetDamageType(DamageSlot slot, DamageType fallback)
    {
        foreach (EquipmentStat modifier in statModifiers)
        {
            if (modifier.statType == StatType.Damage && modifier.damageSlot == slot)
                return modifier.damageType;
        }

        return fallback;
    }

    /// <summary>Read-only collection of stat modifiers</summary>
    public IReadOnlyList<EquipmentStat> StatModifiers => statModifiers;

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
    #endregion

    #region Lifecycle
    private void OnValidate()
    {
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

        int damageModifierCount = 0;
        bool hasPrimaryDamage = false;
        foreach (EquipmentStat modifier in statModifiers)
        {
            if (modifier.statType != StatType.Damage)
                continue;

            damageModifierCount++;
            if (modifier.damageSlot == DamageSlot.Primary && modifier.damageType != DamageType.None)
                hasPrimaryDamage = true;
        }

        if (damageModifierCount > 3)
            Debug.LogWarning("An item can have at most three Damage modifiers.", this);

        if (equipmentType == EquipmentType.Weapon && !hasPrimaryDamage)
        {
            statModifiers.Add(new EquipmentStat
            {
                statType = StatType.Damage,
                damageType = DamageType.Physical,
                damageSlot = DamageSlot.Primary,
                modifierType = StatModifierType.Flat,
                value = 0f
            });
        }

        if (characterDefinition != null && equipmentType == EquipmentType.Helmet &&
            characterDefinition is not HeadwearDefinition)
        {
            Debug.LogWarning("Helmet items must reference a HeadwearDefinition.", this);
        }
    }
    #endregion
}