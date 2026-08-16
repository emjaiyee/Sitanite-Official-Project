// TODO: Refactor snap item visual to grid, try to detect sprite size and use that as base.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ItemGridUI : MonoBehaviour
{
    [Header("Grid Configuration")]
    [SerializeField] private InventoryGrid gridManager;
    [SerializeField] private RectTransform gridRectTransform;
    [SerializeField] private float cellSize = 64f;

    [Header("Item Prefab")]
    [SerializeField] private GameObject itemPrefab;
    [SerializeField] private List<InitialItemEntry> storedItems = new List<InitialItemEntry>();

    [Header("Input Handling")]
    [SerializeField] private InputAction clickAction;

    public float CellSize => cellSize;
    public InventoryGrid GridModel => gridManager;

    private readonly Dictionary<InventoryItem, RectTransform> itemVisualMap = new Dictionary<InventoryItem, RectTransform>();
    private Canvas parentCanvas;

    [System.Serializable]
    public struct InitialItemEntry
    {
        public ItemData itemData;
        public int quantity;
    }

    private void Awake()
    {
        if (gridRectTransform == null) gridRectTransform = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();

    }

    private void OnEnable()
    {
        clickAction.Enable();

        gridManager.OnItemPlaced += HandleItemPlaced;
        gridManager.OnItemRemoved += HandleItemRemoved;
        gridManager.OnItemRotated += HandleItemRotated;
    }

    private void OnDisable()
    {
        clickAction.Enable();

        gridManager.OnItemPlaced -= HandleItemPlaced;
        gridManager.OnItemRemoved -= HandleItemRemoved;
        gridManager.OnItemRotated -= HandleItemRotated;
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

    #region Model Callbacks

    private void HandleItemPlaced(InventoryItem item, Vector2Int position)
    {
        if (!itemVisualMap.TryGetValue(item, out var visual) || visual == null)
        {
            visual = CreateItemVisual(item);
        }

        visual.SetParent(gridRectTransform, false);
        SnapVisualToGrid(item, visual);
    }

    private void HandleItemRemoved(InventoryItem item, Vector2Int previousPosition)
    {
        if (DragDropManager.Instance.HeldItem != item && itemVisualMap.TryGetValue(item, out var visual))
        {
            if (visual != null)
            {
                Destroy(visual.gameObject);
            }
            itemVisualMap.Remove(item);
        }
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

    #region Visual Helpers

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