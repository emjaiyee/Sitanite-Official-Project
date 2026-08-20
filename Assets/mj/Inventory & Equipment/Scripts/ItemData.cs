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
    Defense,
    Attack
}

/// <summary>
/// Determines how the stat calculation applies.
/// </summary>
public enum StatModifierType
{
    Flat,       
    Percent     
}

/// <summary>
/// Individual stat modification entry.
/// </summary>
[Serializable]
public struct EquipmentStat
{
    public StatType statType;
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

    /// <summary>Read-only collection of stat modifiers</summary>
    public IReadOnlyList<EquipmentStat> StatModifiers => statModifiers;
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
    }
    #endregion
}