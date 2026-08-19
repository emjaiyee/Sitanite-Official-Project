using UnityEngine;
using UnityEngine.EventSystems;

public class EquipmentSlotUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private EquipmentType slotType;
    [SerializeField] private RectTransform slotRectTransform;
    [SerializeField] private float slotSize = 64f;

    public EquipmentType SlotType => slotType;
    public InventoryItem EquippedItem => EquipmentManager.Instance != null 
        ? EquipmentManager.Instance.GetEquippedItem(slotType) 
        : null;

    private RectTransform equippedVisual;

    private void Awake()
    {
        if (slotRectTransform == null) slotRectTransform = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        BindEvents();
        SyncVisualFromManager();
    }

    private void OnDisable()
    {
        UnbindEvents();
        ClearVisualOnly();
    }

    private void BindEvents()
    {
        if (EquipmentManager.Instance != null)
        {
            EquipmentManager.Instance.OnEquipmentChanged -= HandleEquipmentChanged;
            EquipmentManager.Instance.OnEquipmentChanged += HandleEquipmentChanged;
        }
    }

    private void UnbindEvents()
    {
        if (EquipmentManager.Instance != null)
        {
            EquipmentManager.Instance.OnEquipmentChanged -= HandleEquipmentChanged;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        DragDropManager dragManager = DragDropManager.Instance;
        EquipmentManager equipManager = EquipmentManager.Instance;
        if (dragManager == null || equipManager == null) return;

        if (dragManager.HeldItem != null)
        {
            if (CanEquip(dragManager.HeldItem))
            {
                EquipHeldItem(dragManager.HeldItem);
            }
        }
        else if (EquippedItem != null)
        {
            UnequipItem();
        }
    }

    public bool CanEquip(InventoryItem item)
    {
        if (item == null || item.Data == null) return false;
        return item.Data.EquipmentType == slotType;
    }

    private void EquipHeldItem(InventoryItem newItem)
    {
        DragDropManager dragManager = DragDropManager.Instance;
        EquipmentManager equipManager = EquipmentManager.Instance;

        RectTransform incomingVisual = dragManager.heldItemVisual;

        UnbindEvents();

        if (!equipManager.Equip(slotType, newItem, out InventoryItem previousItem))
        {
            BindEvents();
            return;
        }

        if (incomingVisual != null)
        {
            equippedVisual = incomingVisual;
            SnapVisualToSlot(equippedVisual, newItem);
        }

        dragManager.ClearHeldState();

        if (previousItem != null)
        {
            RectTransform previousVisual = CreateAndSetupVisual(previousItem);
            dragManager.PickUpItem(previousItem, null, previousVisual, this);
        }

        BindEvents();
    }

    private void UnequipItem()
    {
        EquipmentManager equipManager = EquipmentManager.Instance;
        DragDropManager dragManager = DragDropManager.Instance;

        InventoryItem itemToPickup = EquippedItem;
        RectTransform visualToPickup = equippedVisual;

        equippedVisual = null;

        UnbindEvents();
        equipManager.Unequip(slotType);
        BindEvents();

        if (itemToPickup != null)
        {
            if (visualToPickup == null)
            {
                visualToPickup = CreateAndSetupVisual(itemToPickup);
            }

            dragManager.PickUpItem(itemToPickup, null, visualToPickup, this);
        }
    }

    private void HandleEquipmentChanged(EquipmentType changedType, InventoryItem newItem)
    {
        if (changedType == slotType)
        {
            SyncVisualFromManager();
        }
    }

    public void SyncVisualFromManager()
    {
        ClearVisualOnly();

        if (EquipmentManager.Instance == null) return;

        InventoryItem item = EquipmentManager.Instance.GetEquippedItem(slotType);
        if (item != null)
        {
            equippedVisual = CreateAndSetupVisual(item);
            SnapVisualToSlot(equippedVisual, item);
        }
    }

    private void SnapVisualToSlot(RectTransform visual, InventoryItem item)
    {
        if (visual == null) return;

        visual.SetParent(slotRectTransform, false);
        visual.anchorMin = new Vector2(0.5f, 0.5f);
        visual.anchorMax = new Vector2(0.5f, 0.5f);
        visual.pivot = new Vector2(0.5f, 0.5f);
        visual.anchoredPosition = Vector2.zero;
        visual.localRotation = Quaternion.identity;
        visual.localScale = Vector3.one;

        if (visual.TryGetComponent<ItemUIController>(out var controller))
        {
            controller.SetupForEquipment(item, slotSize);
        }

        if (visual.TryGetComponent<CanvasGroup>(out var canvasGroup))
        {
            canvasGroup.blocksRaycasts = true;
        }
    }

    private RectTransform CreateAndSetupVisual(InventoryItem item)
    {
        if (EquipmentManager.Instance == null || EquipmentManager.Instance.ItemPrefab == null) return null;

        GameObject obj = Instantiate(EquipmentManager.Instance.ItemPrefab, slotRectTransform);
        RectTransform rect = obj.GetComponent<RectTransform>();

        if (obj.TryGetComponent<ItemUIController>(out var controller))
        {
            controller.SetupForEquipment(item, slotSize);
        }

        return rect;
    }

    private void ClearVisualOnly()
    {
        if (equippedVisual != null)
        {
            Destroy(equippedVisual.gameObject);
            equippedVisual = null;
        }

        for (int i = slotRectTransform.childCount - 1; i >= 0; i--)
        {
            Transform child = slotRectTransform.GetChild(i);
            if (child.GetComponent<ItemUIController>() != null)
            {
                Destroy(child.gameObject);
            }
        }
    }
}