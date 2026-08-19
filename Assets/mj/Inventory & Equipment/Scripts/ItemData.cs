using UnityEngine;

public enum EquipmentType
{
    None,
    Weapon,
    Helmet,
    Chestplate,
    Legging,
    Shield
}

[CreateAssetMenu(fileName = "ItemData", menuName = "Inventory/ItemData")]
public class ItemData : ScriptableObject
{

    [Header("Item Type")]
    [SerializeField] private EquipmentType equipmentType = EquipmentType.None;
    public EquipmentType EquipmentType => equipmentType;

    public string itemName;
    [TextArea] public string itemDescription;
    public Vector2Int gridSize = new Vector2Int(1, 1);
    public Sprite inventoryIcon;
    public Sprite equipmentIcon;
    

    [Header("Item Size")]
    public int gridWidth = 1;
    public int gridHeight = 1;

    [Header("Stacking")]
    public bool isStackable = false;
    public int maxStackSize = 1;
}