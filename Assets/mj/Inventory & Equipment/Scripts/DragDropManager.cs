using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// Singleton controller driving global drag-and-drop lifecycle operations,
/// screen-to-UI space position mapping, rotation inputs, and drop cancellation fallbacks.
/// </summary>
public class DragDropManager : MonoBehaviour
{
    #region Singleton
    /// <summary>Get active global singleton instance of the DragDropManager.</summary>
    public static DragDropManager Instance { get; private set; }
    #endregion

    #region Serialized Fields
    [Header("UI Drag Container")]
    [Tooltip("temporary parent while dragging items across UI panels.")]
    [SerializeField] private Canvas mainCanvas;

    [Header("Input Handling")]
    [Tooltip("Input action triggering 90-degree rotations on currently held item.")]
    [SerializeField] private InputAction rotateAction;
    #endregion

    #region Private Fields
    private ItemUIController itemUIController;
    #endregion

    #region Properties & Public Fields
    /// <summary>Get RectTransform of item visual in drag state.</summary>
    public RectTransform heldItemVisual;

    /// <summary>Gets the active item instance being dragged.</summary>
    public InventoryItem HeldItem { get; private set; }

    /// <summary>Gets the origin inventory UI grid container the item was picked up from.</summary>
    public ItemGridUI SourceGrid { get; private set; }

    /// <summary>Gets the origin equipment slot UI container the item was picked up from.</summary>
    public EquipmentSlotUI SourceSlot { get; private set; }

    /// <summary>Get active held item visual RectTransform reference.</summary>
    public RectTransform HeldItemVisual => heldItemVisual;
    #endregion

    #region Events
    /// <summary>Fired when an item enters drag state.</summary>
    public event Action<InventoryItem> OnItemPickedUp;

    /// <summary>Fired when an item leaves drag state via placement, cancellation, or drop.</summary>
    public event Action<InventoryItem> OnItemDropped;

    /// <summary>Fired when the held item is rotated mid-drag.</summary>
    public event Action<InventoryItem> OnItemRotated;
    #endregion

    #region Lifecycle
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (mainCanvas == null)
        {
            mainCanvas = GetComponentInParent<Canvas>();
            if (mainCanvas == null) mainCanvas = FindAnyObjectByType<Canvas>();
        }
    }

    private void OnEnable() => rotateAction.Enable();
    private void OnDisable() => rotateAction.Disable();

    private void Update()
    {
        if (HeldItem == null) return;

        UpdateHeldItemPosition();

        if (rotateAction.WasPressedThisFrame())
        {
            RotateHeldItem();
        }
    }
    #endregion

    #region Public API
    /// <summary>
    /// Captures an inventory item, reparenting visual element to the main canvas root for unconstrained dragging.
    /// </summary>
    /// <param name="item">Target inventory item data</param>
    /// <param name="sourceGrid">Source grid, if picked up from a grid.</param>
    /// <param name="itemVisual">RectTransform visual element of item.</param>
    /// <param name="sourceSlot">Source equipment slot reference, if picked up from an equipment slot.</param>
    /// <returns>True if pickup succeeded; false if an item is already being held.</returns>
    public bool PickUpItem(InventoryItem item, ItemGridUI sourceGrid, RectTransform itemVisual, EquipmentSlotUI sourceSlot = null)
    {
        if (HeldItem != null) return false;

        HeldItem = item;
        SourceGrid = sourceGrid;
        SourceSlot = sourceSlot;
        heldItemVisual = itemVisual;

        if (heldItemVisual != null && mainCanvas != null)
        {
            heldItemVisual.SetParent(mainCanvas.transform, true);
            heldItemVisual.SetAsLastSibling();

            // Disable raycast blocking so mouse pointer events register grid cells underneath held item
            if (heldItemVisual.TryGetComponent<CanvasGroup>(out var canvasGroup))
            {
                canvasGroup.blocksRaycasts = false;
            }

            itemUIController = heldItemVisual.GetComponent<ItemUIController>();
            UpdateHeldItemPosition();
        }

        OnItemPickedUp?.Invoke(HeldItem);
        return true;
    }

    /// <summary>
    /// Try to insert the active held item into inventory grid at matrix coordinates.
    /// </summary>
    /// <param name="targetGrid">Target grid matrix model.</param>
    /// <param name="x">Target top-left column position.</param>
    /// <param name="y">Target top-left row position.</param>
    /// <returns>True if placement logic succeeded; otherwise, false.</returns>
    public bool PlaceHeldItemIntoGrid(InventoryGrid targetGrid, int x, int y)
    {
        if (HeldItem == null) return false;

        if (targetGrid.PlaceItem(HeldItem, x, y))
        {
            ClearHeldItem();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Rotates the active held item 90 degrees clockwise and update visual layout dimensions.
    /// </summary>
    public void RotateHeldItem()
    {
        if (HeldItem == null) return;

        HeldItem.Rotate();

        if (itemUIController != null)
        {
            float cellSize = SourceGrid != null ? SourceGrid.CellSize : 64f;
            itemUIController.UpdateLayout(HeldItem, cellSize);
        }

        OnItemRotated?.Invoke(HeldItem);
    }

    /// <summary>
    /// Cancels active drag operation, restoring item to its origin grid/equipment slot or finding first open fallback space.
    /// </summary>
    public void CancelDrag()
    {
        if (HeldItem == null) return;

        if (SourceGrid != null && SourceGrid.GridModel != null)
        {
            // Attempt restoration to original origin position; fallback to any available space if blocked
            if (!SourceGrid.GridModel.PlaceItem(HeldItem, HeldItem.OriginPosition.x, HeldItem.OriginPosition.y))
            {
                SourceGrid.GridModel.FindSpaceForItem(HeldItem, out Vector2Int fallbackPos);
                SourceGrid.GridModel.PlaceItem(HeldItem, fallbackPos.x, fallbackPos.y);
            }
        }
        else if (SourceSlot != null && EquipmentManager.Instance != null)
        {
            EquipmentManager.Instance.Equip(SourceSlot.SlotType, HeldItem, out _);
            SourceSlot.SyncVisualFromManager();
        }

        ClearHeldItem();
    }

    /// <summary>
    /// Clear active held item references without execution of placement or return logic.
    /// </summary>
    public void ClearHeldState()
    {
        ClearHeldItem();
    }
    #endregion

    #region Internal Helpers
    private void ClearHeldItem()
    {
        // Re-enable raycasting for drop interactions on target containers
        if (heldItemVisual != null && heldItemVisual.TryGetComponent<CanvasGroup>(out var canvasGroup))
        {
            canvasGroup.blocksRaycasts = true;
        }

        InventoryItem droppedItem = HeldItem;
        HeldItem = null;
        SourceGrid = null;
        SourceSlot = null;
        heldItemVisual = null;
        itemUIController = null;

        OnItemDropped?.Invoke(droppedItem);
    }

    private void UpdateHeldItemPosition()
    {
        if (heldItemVisual == null || mainCanvas == null) return;

        Camera uiCamera = mainCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : mainCanvas.worldCamera;
        Vector2 mousePos = Mouse.current.position.ReadValue();

        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
            mainCanvas.GetComponent<RectTransform>(),
            mousePos,
            uiCamera,
            out Vector3 worldPoint))
        {
            heldItemVisual.position = worldPoint;

            Vector3 localPos = heldItemVisual.localPosition;
            localPos.z = 0f;
            heldItemVisual.localPosition = localPos;
        }
    }
    #endregion
}