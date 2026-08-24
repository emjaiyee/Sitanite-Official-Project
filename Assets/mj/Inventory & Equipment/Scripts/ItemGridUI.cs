using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Handles UI rendering, input detection, and spatial grid mapping for items in inventory.
/// Syncs model events with visual component updates and grid alignment.
/// </summary>
public class ItemGridUI : MonoBehaviour
{
    #region Nested Types
    /// <summary>
    /// Struct container for holding the items in the inventory.
    /// </summary>
    [System.Serializable]
    public struct InitialItemEntry
    {
        [Tooltip("Specific inventory ItemData ScriptableObject")]
        public ItemData itemData;

        [Tooltip("Stack size.")]
        public int quantity;
    }
    #endregion

    #region Serialized Fields
    [Header("Grid Configuration")]
    [Tooltip("Reference to InventoryGrid Script.")]
    [SerializeField] private InventoryGrid gridManager;

    [Tooltip("RectTransform of the inventory grid container.")]
    [SerializeField] private RectTransform gridRectTransform;

    [Tooltip("Pixel width and height of individual square grid cell.")]
    [SerializeField] private float cellSize = 64f;

    [Header("Input Handling")]
    [Tooltip("Input action triggering click interaction")]
    [SerializeField] private InputAction clickAction;

    [Header("Item Prefab")]
    [Tooltip("UI Prefab instantiated to represent inventory items visually.")]
    [SerializeField] private GameObject itemPrefab;

    [Tooltip("Initial item inventory list on startup.")]
    [SerializeField] private List<InitialItemEntry> storedItems = new List<InitialItemEntry>();
    #endregion

    #region Private Fields
    private readonly Dictionary<InventoryItem, RectTransform> itemVisualMap = new Dictionary<InventoryItem, RectTransform>();
    private Canvas parentCanvas;
    #endregion

    #region Properties
    /// <summary>Get pixel dimension size individual grid cell.</summary>
    public float CellSize => cellSize;

    /// <summary>Get bound inventory grid data model.</summary>
    public InventoryGrid GridModel => gridManager;
    #endregion

    #region Lifecycle
    private void Awake()
    {
        if (gridRectTransform == null) gridRectTransform = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();
    }

    private void OnEnable()
    {
        clickAction.Enable();

        gridManager.OnItemPlaced += HandleItemPlaced;
        gridManager.OnItemRotated += HandleItemRotated;
        gridManager.OnItemUpdated += HandleItemUpdated;
        
        RebuildGridVisuals();
    }

    private void OnDisable()
    {
        clickAction.Disable();

        gridManager.OnItemPlaced -= HandleItemPlaced;
        gridManager.OnItemRotated -= HandleItemRotated;
        gridManager.OnItemUpdated -= HandleItemUpdated;
    }

    private void Start()
    {
        float totalWidth = gridManager.GridWidth * cellSize;
        float totalHeight = gridManager.GridHeight * cellSize;

        gridRectTransform.sizeDelta = new Vector2(totalWidth, totalHeight);

        InitializeInventoryItems();
    }

    private void Update()
    {
        if (clickAction.WasPressedThisFrame())
        {
            HandleMouseClick();
        }
    }
    #endregion

    #region Public API
    /// <summary>
    /// Clears existing UI visuals and regenerates UI transforms for all items.
    /// </summary>
    public void RebuildGridVisuals()
    {
        foreach (var kvp in itemVisualMap)
        {
            if (kvp.Value != null) Destroy(kvp.Value.gameObject);
        }
        itemVisualMap.Clear();

        if (gridManager == null) return;

        // Prevent duplicate visual instantiation for item spanning multiple grid cells
        HashSet<InventoryItem> processedItems = new HashSet<InventoryItem>();

        for (int x = 0; x < gridManager.GridWidth; x++)
        {
            for (int y = 0; y < gridManager.GridHeight; y++)
            {
                InventoryItem item = gridManager.GetItem(x, y);
                if (item != null && !processedItems.Contains(item))
                {
                    processedItems.Add(item);
                    RectTransform visual = CreateItemVisual(item);
                    SnapVisualToGrid(item, visual);
                }
            }
        }
    }

    /// <summary>
    /// Converts screen space positions to 2D matrix coordinates relative to top-left grid origin.
    /// </summary>
    /// <param name="mousePosition">Screen position input vector.</param>
    /// <returns>Matrix cell coordinates.</returns>
    public Vector2Int GetGridPosition(Vector2 mousePosition)
    {
        Camera uiCamera = (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? parentCanvas.worldCamera
            : null;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            gridRectTransform,
            mousePosition,
            uiCamera,
            out Vector2 localPoint
        );

        float relativeX = localPoint.x - gridRectTransform.rect.xMin;
        float relativeY = gridRectTransform.rect.yMax - localPoint.y;

        int x = Mathf.FloorToInt(relativeX / cellSize);
        int y = Mathf.FloorToInt(relativeY / cellSize);

        return new Vector2Int(x, y);
    }
    #endregion

    #region Event Handlers
    private void HandleMouseClick()
    {
        Vector2Int gridPos = GetGridPosition(Mouse.current.position.ReadValue());
        if (!gridManager.IsWithinBounds(gridPos.x, gridPos.y)) return;

        DragDropManager dragManager = DragDropManager.Instance;

        if (dragManager.HeldItem == null)
        {
            InventoryItem clickedItem = gridManager.GetItem(gridPos.x, gridPos.y);
            if (clickedItem != null)
            {
                itemVisualMap.TryGetValue(clickedItem, out RectTransform visual);
                if (dragManager.PickUpItem(clickedItem, this, visual))
                {
                    gridManager.RemoveItem(clickedItem);
                }
            }
        }
        else
        {
            dragManager.PlaceHeldItemIntoGrid(gridManager, gridPos.x, gridPos.y);
        }
    }

    private void HandleItemUpdated(InventoryItem item)
    {
        if (itemVisualMap.TryGetValue(item, out var visual) && visual != null)
        {
            if (visual.TryGetComponent<ItemUIController>(out var controller))
            {
                controller.UpdateLayout(item, cellSize);
            }
        }
    }

    private void HandleItemPlaced(InventoryItem item, Vector2Int position)
    {
        DragDropManager dragManager = DragDropManager.Instance;

        if (dragManager.HeldItem == item && dragManager.HeldItemVisual != null)
        {
            RectTransform heldVisual = dragManager.HeldItemVisual;

            heldVisual.SetParent(gridRectTransform, false);
            itemVisualMap[item] = heldVisual;

            SnapVisualToGrid(item, heldVisual);
            return;
        }

        if (!itemVisualMap.TryGetValue(item, out var visual) || visual == null)
        {
            visual = CreateItemVisual(item);
        }

        visual.SetParent(gridRectTransform, false);
        SnapVisualToGrid(item, visual);
    }
    private void HandleItemRotated(InventoryItem item)
    {
        if (itemVisualMap.TryGetValue(item, out var visual) && visual != null)
        {
            if (visual.TryGetComponent<ItemUIController>(out var controller))
            {
                controller.UpdateLayout(item, cellSize);
            }
        }
    }
    #endregion

    #region Internal Helpers
    private RectTransform CreateItemVisual(InventoryItem item)
    {
        GameObject obj = Instantiate(itemPrefab, gridRectTransform);
        obj.transform.localScale = Vector3.one;

        RectTransform rectTransform = obj.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0, 1);
        rectTransform.anchorMax = new Vector2(0, 1);
        rectTransform.pivot = new Vector2(0, 1);

        if (obj.TryGetComponent<ItemUIController>(out var controller))
        {
            controller.Setup(item, cellSize);
        }

        itemVisualMap[item] = rectTransform;
        return rectTransform;
    }

    private void SnapVisualToGrid(InventoryItem item, RectTransform visual)
    {
        // Anchor top-left to enable direct Y-axis down-offset
        visual.anchorMin = new Vector2(0, 1);
        visual.anchorMax = new Vector2(0, 1);
        visual.pivot = new Vector2(0, 1);

        float posX = gridRectTransform.rect.xMin + (item.OriginPosition.x * cellSize);
        float posY = gridRectTransform.rect.yMax - (item.OriginPosition.y * cellSize);

        visual.anchoredPosition = new Vector2(posX, posY);
        visual.localEulerAngles = Vector3.zero;
        visual.SetAsLastSibling();

        if (visual.TryGetComponent<ItemUIController>(out var controller))
        {
            controller.UpdateLayout(item, cellSize);
        }
    }

    private void InitializeInventoryItems()
    {
        foreach (var item in storedItems)
        {
            if (item.itemData != null)
            {
                InventoryItem newItem = new InventoryItem(item.itemData, item.quantity);
                if (gridManager.FindSpaceForItem(newItem, out Vector2Int pos))
                {
                    gridManager.PlaceItem(newItem, pos.x, pos.y);
                }
            }
        }
    }
    #endregion
}