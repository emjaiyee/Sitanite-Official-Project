using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// UI view component for each individual equipment slots.
/// Handles user pointer interactions, item swap logic, and visual state sync.
/// </summary>
public class EquipmentSlotUI : MonoBehaviour, IPointerClickHandler, IDropHandler
{
    #region Serialized Fields
    [Header("Slot Configuration")]
    [Tooltip("Type of equipment slot.")]
    [SerializeField] private EquipmentType slotType;

    [Tooltip("RectTransform container holding the item visual within slot.")]
    [SerializeField] private RectTransform slotRectTransform;

    [Tooltip("Target dimensions (in pixels) for item scaling within the slot.")]
    [SerializeField] private float slotSize = 64f;
    #endregion

    #region Private Fields
    private RectTransform equippedVisual;
    private bool dropHandled;
    #endregion

    #region Properties
    /// <summary>Get assigned equipment slot type constraint.</summary>
    public EquipmentType SlotType => slotType;

    /// <summary>Get item currently equipped in this slot from EquipmentManager.</summary>
    public InventoryItem EquippedItem => EquipmentManager.Instance != null 
        ? EquipmentManager.Instance.GetEquippedItem(slotType) 
        : null;
    #endregion

    #region Lifecycle
    private void Awake()
    {
        if (slotRectTransform == null) slotRectTransform = GetComponent<RectTransform>();

        Graphic dropTarget = GetComponent<Graphic>();
        if (dropTarget == null)
        {
            Image raycastTarget = gameObject.AddComponent<Image>();
            raycastTarget.color = Color.clear;
            raycastTarget.raycastTarget = true;
        }
        else
        {
            dropTarget.raycastTarget = true;
        }
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
    #endregion

    #region Input Handling
    /// <summary>
    /// Event handler for pointer click inputs on the equipment slot.
    /// Handles equipping held items, swapping equipment, or unequipping items.
    /// </summary>
    /// <param name="eventData">Pointer event containing button input.</param>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        if (dropHandled)
        {
            dropHandled = false;
            return;
        }

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

    public void OnDrop(PointerEventData eventData)
    {
        DragDropManager dragManager = DragDropManager.Instance;
        if (dragManager == null || dragManager.HeldItem == null)
            return;

        dropHandled = true;

        if (!CanEquip(dragManager.HeldItem))
        {
            dragManager.CancelDrag();
            return;
        }

        EquipHeldItem(dragManager.HeldItem);
    }
    #endregion

    #region Public API
    /// <summary>
    /// Checks if given item matches this slot's designated equipment type.
    /// </summary>
    /// <param name="item">Item instance to evaluate.</param>
    /// <returns>True if item is valid and compatible with this slot type.</returns>
    public bool CanEquip(InventoryItem item)
    {
        if (item == null || item.Data == null) return false;
        return item.Data.EquipmentType == slotType;
    }

    /// <summary>
    /// Re-synchronizes the visual state of the slot with the active equipment data from manager.
    /// </summary>
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
    #endregion

    #region Internal Helpers
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

    private void EquipHeldItem(InventoryItem newItem)
    {
        DragDropManager dragManager = DragDropManager.Instance;
        EquipmentManager equipManager = EquipmentManager.Instance;

        if (dragManager == null || equipManager == null || !CanEquip(newItem))
            return;

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
        else
        {
            equippedVisual = CreateAndSetupVisual(newItem);
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
    #endregion
}