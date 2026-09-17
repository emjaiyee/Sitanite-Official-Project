using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryItemActionMenu : MonoBehaviour
{
    private static InventoryItemActionMenu activeMenu;

    [Header("Actions")]
    [SerializeField] private Button useButton;
    [SerializeField] private Button equipButton;
    [SerializeField] private TMP_Text equipButtonLabel;
    [SerializeField] private Button dropButton;
    [SerializeField] private Button cancelButton;

    public RectTransform RectTransform { get; private set; }
    public static bool IsOpen => activeMenu != null;

    private InventoryItem item;
    private InventoryGrid sourceGrid;
    private EquipmentSlotUI sourceSlot;

    private void Awake()
    {
        RectTransform = GetComponent<RectTransform>();
    }

    public static void Show(GameObject actionMenuPrefab, Canvas canvas, InventoryItem targetItem,
        InventoryGrid grid, EquipmentSlotUI slot, Vector2 screenPosition)
    {
        if (actionMenuPrefab == null || canvas == null || targetItem == null)
            return;

        CloseActive();
        UIHoverTooltip.HideAll();

        GameObject menuObject = Instantiate(actionMenuPrefab, canvas.transform);
        activeMenu = menuObject.GetComponent<InventoryItemActionMenu>();
        if (activeMenu == null)
        {
            Debug.LogError("InventoryItemActionMenu: The action menu prefab requires an InventoryItemActionMenu component.", menuObject);
            Destroy(menuObject);
            return;
        }

        activeMenu.Initialize(targetItem, grid, slot, canvas, screenPosition);
    }

    public static void CloseActive()
    {
        if (activeMenu != null)
            Destroy(activeMenu.gameObject);
    }

    public void Initialize(InventoryItem targetItem, InventoryGrid grid, EquipmentSlotUI slot,
        Canvas canvas, Vector2 screenPosition)
    {
        item = targetItem;
        sourceGrid = grid;
        sourceSlot = slot;

        bool isConsumable = item.Data.EquipmentType == EquipmentType.Consumable;
        bool canEquip = sourceGrid != null && IsEquippable(item) &&
            EquipmentManager.Instance != null &&
            EquipmentManager.Instance.GetEquippedItem(item.Data.EquipmentType) == null &&
            EquipmentManager.Instance.CanEquip(item.Data);
        bool canUnequip = sourceSlot != null && CanUnequipToInventory();

        ConfigureButton(useButton, isConsumable, Use);
        ConfigureButton(equipButton, sourceSlot == null ? canEquip : canUnequip,
            sourceSlot == null ? Equip : Unequip);
        if (equipButtonLabel != null)
            equipButtonLabel.text = sourceSlot == null ? "Equip" : "Unequip";
        ConfigureButton(dropButton, true, Drop);
        ConfigureButton(cancelButton, true, CloseActive);

        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.GetComponent<RectTransform>(), screenPosition, uiCamera, out Vector2 localPosition);
        RectTransform.localPosition = localPosition;
    }

    private static void ConfigureButton(Button button, bool interactable, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        button.interactable = interactable;
        button.onClick.AddListener(action);
    }

    private void Use()
    {
        PlayerStats playerStats = FindFirstObjectByType<PlayerStats>();
        if (sourceGrid != null && item.Data.ApplyRejuvenation(playerStats))
            sourceGrid.TryConsumeItem(item);

        CloseActive();
    }

    private void Equip()
    {
        if (sourceGrid != null && EquipmentManager.Instance.Equip(item.Data.EquipmentType, item, out _))
            sourceGrid.RemoveItem(item);

        CloseActive();
    }

    private void Unequip()
    {
        if (sourceSlot != null && sourceGrid == null)
        {
            InventoryGrid inventory = FindFirstObjectByType<PlayerInventory>()?.MainBackPack;
            if (inventory != null && inventory.FindSpaceForItem(item, out Vector2Int position))
            {
                EquipmentManager.Instance.Unequip(sourceSlot.SlotType);
                inventory.PlaceItem(item, position.x, position.y);
            }
        }

        CloseActive();
    }

    private void Drop()
    {
        if (sourceGrid != null)
            sourceGrid.RemoveItem(item);
        else if (sourceSlot != null)
            EquipmentManager.Instance.Unequip(sourceSlot.SlotType);

        FindFirstObjectByType<PlayerInventory>()?.DropItem(item);
        CloseActive();
    }

    private bool CanUnequipToInventory()
    {
        InventoryGrid inventory = FindFirstObjectByType<PlayerInventory>()?.MainBackPack;
        return inventory != null && inventory.FindSpaceForItem(item, out _);
    }

    private static bool IsEquippable(InventoryItem targetItem)
    {
        return targetItem.Data.EquipmentType != EquipmentType.None &&
            targetItem.Data.EquipmentType != EquipmentType.Consumable;
    }

    private void OnDestroy()
    {
        if (activeMenu == this)
            activeMenu = null;
    }
}
