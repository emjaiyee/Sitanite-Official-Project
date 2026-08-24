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
    }

    private void OnDisable()
    {
        if (inventoryAction != null)
            inventoryAction.action.Disable();

        SceneManager.sceneLoaded -= OnSceneLoaded;
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
        FindSceneReferences();
        SetInventoryState(startOpen);
    }

    #endregion

    #region Scene Reference Management

    /// <summary>
    /// Finds the InventoryGrid and InventoryPanel belonging to the
    /// currently loaded scene.
    /// </summary>
    private void FindSceneReferences()
    {
        // Find the scene-local inventory grid, including inactive objects.
        mainBackpack = FindFirstObjectByType<InventoryGrid>(
            FindObjectsInactive.Include
        );

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

    #endregion

    private void SetSuppressedUIState(bool inventoryOpen)
    {
        if (suppressedUIs == null)
            return;

        foreach (UISuppressor suppressor in suppressedUIs)
        {
            if (suppressor == null)
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