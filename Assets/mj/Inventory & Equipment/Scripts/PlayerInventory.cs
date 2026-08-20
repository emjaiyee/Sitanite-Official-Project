using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Manages player-bound inventory access, input toggling, UI lifecycle,
/// and interaction with dropped loot.
/// </summary>
public class PlayerInventory : MonoBehaviour
{
    #region Serialized Fields
    [Header("Inventory Components")]
    [Tooltip("Reference the primary inventory grid.")]
    [SerializeField] private InventoryGrid mainBackpack;

    [Header("UI Reference")]
    [Tooltip("Root UI Panel container holding the inventory & equipments")]
    [SerializeField] private GameObject inventoryPanel;

    [Header("Input & Configuration")]
    [Tooltip("Input action assigned to toggle inventory & Equipment")]
    [SerializeField] private InputAction inventoryAction;

    [Tooltip("Should the root UI panel start open or closed")]
    [SerializeField] private bool startOpen = false;
    #endregion

    #region Properties
    /// <summary>Get main backpack grid container.</summary>
    public InventoryGrid MainBackPack => mainBackpack;

    /// <summary>Get whether the inventory UI panel is currently active in hierarchy.</summary>
    public bool IsOpen => inventoryPanel != null && inventoryPanel.activeSelf;
    #endregion

    #region Lifecycle
    private void OnEnable()
    {
        inventoryAction.Enable();
    }

    private void OnDisable()
    {
        inventoryAction.Disable();
    }

    private void Start()
    {
        SetInventoryState(startOpen);
    }

    private void Update()
    {
        if (inventoryAction.WasPressedThisFrame())
        {
            ToggleInventory();
        }
    }
    #endregion

    #region Public API
    /// <summary>
    /// Toggles the active visibility of the root UI panel.
    /// </summary>
    public void ToggleInventory()
    {
        if (inventoryPanel == null) return;

        SetInventoryState(!inventoryPanel.activeSelf);
    }

    /// <summary>
    /// Explicitly sets the active state of the inventory UI panel.
    /// Automatically cancels drag operations if closed while holding an item.
    /// </summary>
    /// <param name="isOpen">Boolean visibility state.</param>
    public void SetInventoryState(bool isOpen)
    {
        if (inventoryPanel == null) return;

        // Returns dragged items to original slot if UI closes mid-drag
        if (!isOpen && DragDropManager.Instance != null && DragDropManager.Instance.HeldItem != null)
        {
            DragDropManager.Instance.CancelDrag();
        }

        inventoryPanel.SetActive(isOpen);
    }

    /// <summary>
    /// Routes loot drop interaction into the primary inventory grid.
    /// </summary>
    /// <param name="item">Loot instance to collect.</param>
    /// <returns>True if pickup logic succeeded; otherwise, false.</returns>
    public bool Pickup(Loot item)
    {
        if (mainBackpack == null || item == null) return false;

        return item.TryPickup(mainBackpack);
    }
    #endregion
}