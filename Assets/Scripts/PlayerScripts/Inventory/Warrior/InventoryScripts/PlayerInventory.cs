using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages player-bound inventory access, input toggling, UI lifecycle,
/// and interaction with dropped loot.
///
/// PlayerInventory is persistent across scene changes, while the
/// InventoryGrid and InventoryPanel exist locally within each scene.
/// Scene-local references are automatically discovered at runtime.
/// </summary>
public class PlayerInventory : MonoBehaviour
{
    [Serializable]
    private class SavedInventoryItem
    {
        public ItemData itemData;
        public int quantity;
        public int rotationIndex;
        public Vector2Int originPosition;
    }
    #region Serialized Fields

    [Header("Inventory Components")]
    [Tooltip("Automatically finds the scene-local InventoryGrid at runtime.")]
    [SerializeField] private InventoryGrid mainBackpack;

    [Header("UI Reference")]
    [Tooltip("Automatically finds the scene-local InventoryPanel at runtime.")]
    [SerializeField] private GameObject inventoryPanel;

    [Header("Input & Configuration")]
    [Tooltip("Input action assigned to toggle inventory & Equipment.")]
    [SerializeField] private InputActionReference inventoryAction;

    [Tooltip("Should the root UI panel start open or closed.")]
    [SerializeField] private bool startOpen = false;

    [Header("UI Suppression")]
    [Tooltip("Scene-local UI elements that are automatically hidden while the inventory is open.")]
    [SerializeField] private UISuppressor[] suppressedUIs;

    private PlayerStatsUI statsUI;

    private readonly List<SavedInventoryItem> savedItems = new List<SavedInventoryItem>();
    private bool hasSavedInventory;
    private InventoryGrid boundGrid;
    private bool suppressGridSnapshot;

    #endregion

    #region Properties

    /// <summary>
    /// Get the currently active scene's inventory grid.
    /// </summary>
    public InventoryGrid MainBackPack => mainBackpack;

    /// <summary>
    /// Get whether the inventory UI panel is currently active in the hierarchy.
    /// </summary>
    public bool IsOpen =>
        inventoryPanel != null &&
        inventoryPanel.activeSelf;

    #endregion

    #region Lifecycle

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        if (inventoryAction != null)
            inventoryAction.action.Enable();

        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    private void OnDisable()
    {
        if (inventoryAction != null)
            inventoryAction.action.Disable();

        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        UnbindGrid();
    }

    private void Start()
    {
        FindSceneReferences();
        SetInventoryState(startOpen);
    }

    private void Update()
    {
        if (inventoryAction != null &&
            inventoryAction.action.WasPressedThisFrame())
        {
            ToggleInventory();
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        suppressGridSnapshot = hasSavedInventory;
        FindSceneReferences();
        SetInventoryState(startOpen);
        StartCoroutine(RestoreInventoryAfterSceneLoad());
    }

    private void OnSceneUnloaded(Scene scene)
    {
        SaveInventory();
    }

    private void OnActiveSceneChanged(Scene oldScene, Scene newScene)
    {
        if (oldScene.IsValid() && oldScene != newScene)
            SaveInventory();
    }

    #endregion

    #region Scene Reference Management

    /// <summary>
    /// Finds the InventoryGrid and InventoryPanel belonging to the
    /// currently loaded scene.
    /// </summary>
    private void FindSceneReferences()
    {
        UnbindGrid();

        // Find the scene-local inventory grid, including inactive objects.
        mainBackpack = FindFirstObjectByType<InventoryGrid>(
            FindObjectsInactive.Include
        );

        BindGrid(mainBackpack);

        if (mainBackpack == null)
        {
            Debug.LogWarning(
                "PlayerInventory: No InventoryGrid was found in the current scene."
            );
        }

        // Find the scene-local inventory panel, including inactive objects.
        InventoryPanel panel = FindFirstObjectByType<InventoryPanel>(
            FindObjectsInactive.Include
        );

        if (panel != null)
        {
            inventoryPanel = panel.gameObject;
        }
        else
        {
            inventoryPanel = null;

            Debug.LogWarning(
                "PlayerInventory: No InventoryPanel was found in the current scene."
            );
        }

        statsUI = FindFirstObjectByType<PlayerStatsUI>(
            FindObjectsInactive.Include
        );

        // Find all scene-local UI elements marked for suppression.
        suppressedUIs = FindObjectsByType<UISuppressor>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );
    }

    #endregion

    #region Public API

    /// <summary>
    /// Toggles the active visibility of the inventory UI panel.
    /// </summary>
    public void ToggleInventory()
    {
        if (inventoryPanel == null)
        {
            Debug.LogWarning(
                "PlayerInventory: Cannot toggle inventory because no InventoryPanel is currently assigned."
            );

            return;
        }

        SetInventoryState(!inventoryPanel.activeSelf);
    }

    /// <summary>
    /// Explicitly sets the active state of the inventory UI panel.
    /// Automatically cancels drag operations if closed while holding an item.
    /// </summary>
    /// <param name="isOpen">Boolean visibility state.</param>
    public void SetInventoryState(bool isOpen)
    {
        if (inventoryPanel == null)
            return;

        if (isOpen && statsUI != null && statsUI.IsOpen)
            statsUI.SetStatsWindowState(false);

        // Returns dragged items to original slot if UI closes mid-drag.
        if (!isOpen &&
            DragDropManager.Instance != null &&
            DragDropManager.Instance.HeldItem != null)
        {
            DragDropManager.Instance.CancelDrag();
        }

        inventoryPanel.SetActive(isOpen);

        SetSuppressedUIState(isOpen);
    }

    /// <summary>
    /// Routes loot drop interaction into the primary inventory grid.
    /// </summary>
    /// <param name="item">Loot instance to collect.</param>
    /// <returns>
    /// True if pickup logic succeeded; otherwise, false.
    /// </returns>
    public bool Pickup(Loot item)
    {
        if (mainBackpack == null || item == null)
            return false;

        return item.TryPickup(mainBackpack);
    }

    private void SaveInventory()
    {
        if (mainBackpack == null)
            return;

        savedItems.Clear();
        foreach (InventoryItem item in mainBackpack.GetItems())
        {
            savedItems.Add(new SavedInventoryItem
            {
                itemData = item.Data,
                quantity = item.Quantity,
                rotationIndex = item.RotationIndex,
                originPosition = item.OriginPosition
            });
        }

        hasSavedInventory = true;
    }

    private void BindGrid(InventoryGrid grid)
    {
        if (grid == null)
            return;

        boundGrid = grid;
        boundGrid.OnItemPlaced += HandleGridChanged;
        boundGrid.OnItemRemoved += HandleGridChanged;
        boundGrid.OnItemRotated += HandleGridChanged;
        boundGrid.OnItemUpdated += HandleGridChanged;
    }

    private void UnbindGrid()
    {
        if (boundGrid == null)
            return;

        boundGrid.OnItemPlaced -= HandleGridChanged;
        boundGrid.OnItemRemoved -= HandleGridChanged;
        boundGrid.OnItemRotated -= HandleGridChanged;
        boundGrid.OnItemUpdated -= HandleGridChanged;
        boundGrid = null;
    }

    private void HandleGridChanged(InventoryItem item, Vector2Int position)
    {
        if (!suppressGridSnapshot)
            SaveInventory();
    }

    private void HandleGridChanged(InventoryItem item)
    {
        if (!suppressGridSnapshot)
            SaveInventory();
    }

    private IEnumerator RestoreInventoryAfterSceneLoad()
    {
        yield return null;

        if (!hasSavedInventory || mainBackpack == null)
        {
            suppressGridSnapshot = false;
            yield break;
        }

        suppressGridSnapshot = true;
        mainBackpack.Clear();
        foreach (SavedInventoryItem savedItem in savedItems)
        {
            if (savedItem.itemData == null)
                continue;

            InventoryItem item = new InventoryItem(savedItem.itemData, savedItem.quantity);
            for (int rotation = 0; rotation < savedItem.rotationIndex; rotation++)
                item.Rotate();

            bool restored = mainBackpack.CanPlaceItem(
                item,
                savedItem.originPosition.x,
                savedItem.originPosition.y
            ) && mainBackpack.PlaceItem(
                item,
                savedItem.originPosition.x,
                savedItem.originPosition.y
            );

            if (!restored)
                restored = mainBackpack.TryAddItem(item);

            if (!restored)
            {
                Debug.LogWarning(
                    $"PlayerInventory: Could not restore '{savedItem.itemData.itemName}' in the new scene."
                );
            }
        }

        suppressGridSnapshot = false;
        SaveInventory();
    }

    #endregion

    private void SetSuppressedUIState(bool inventoryOpen)
    {
        if (suppressedUIs == null)
            return;

        foreach (UISuppressor suppressor in suppressedUIs)
        {
            if (suppressor == null ||
                suppressor.GetComponent<InventoryPanel>() != null)
                continue;

            if (inventoryOpen)
            {
                suppressor.Suppress();
            }
            else
            {
                suppressor.Restore();
            }
        }
    }
}