using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class DragDropManager : MonoBehaviour
{
    public static DragDropManager Instance { get; private set; }

    [Header("UI Drag Container")]
    [SerializeField] private Canvas mainCanvas;

    [Header("Input Handling")]
    [SerializeField] private InputAction rotateAction;

    public RectTransform heldItemVisual;
    private ItemUIController itemUIController;

    public InventoryItem HeldItem { get; private set; }
    public ItemGridUI SourceGrid { get; private set; }
    public EquipmentSlotUI SourceSlot { get; private set; }

    public RectTransform HeldItemVisual => heldItemVisual;

    public event Action<InventoryItem> OnItemPickedUp;
    public event Action<InventoryItem> OnItemDropped;
    public event Action<InventoryItem> OnItemRotated;

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

    private void Update()
    {
        if (HeldItem == null) return;

        UpdateHeldItemPosition();

        if (rotateAction.WasPressedThisFrame())
        {
            RotateHeldItem();
        }
    }

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

    public void CancelDrag()
    {
        if (HeldItem == null) return;

        if (SourceGrid != null && SourceGrid.GridModel != null)
        {
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

    public void ClearHeldState()
    {
        ClearHeldItem();
    }

    private void ClearHeldItem()
    {
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

    private void OnEnable() => rotateAction.Enable();
    private void OnDisable() => rotateAction.Disable();
}