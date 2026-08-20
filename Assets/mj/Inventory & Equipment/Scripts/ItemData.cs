using UnityEngine;

/// <summary>
/// Categorizes equipment slot for wearable or holdable inventory items.
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