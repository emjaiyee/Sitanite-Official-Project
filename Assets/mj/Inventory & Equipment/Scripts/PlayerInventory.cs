using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInventory : MonoBehaviour
{
    [SerializeField] private InventoryGrid mainBackpack;

    public InventoryGrid MainBackPack => mainBackpack;

    [Header("UI Reference")]
    [SerializeField] private GameObject inventoryPanel;

    [Header("Settings")]
    [SerializeField] private InputAction inventoryAction;

    [SerializeField] private bool startOpen = false;
    public bool IsOpen => inventoryPanel != null && inventoryPanel.activeSelf;

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

    public void ToggleInventory()
    {
        if (inventoryPanel == null) return;

        SetInventoryState(!inventoryPanel.activeSelf);
    }

    public void SetInventoryState(bool isOpen)
    {
        if (inventoryPanel == null) return;

        if (!isOpen && DragDropManager.Instance != null && DragDropManager.Instance.HeldItem != null)
        {
            DragDropManager.Instance.CancelDrag();
        }

        inventoryPanel.SetActive(isOpen);
    }

    private void OnEnable() 
    {
        inventoryAction.Enable();
    }

    private void OnDisable()
    {
        inventoryAction.Disable();
    }

    public bool Pickup(Loot item)
    {
        if (mainBackpack == null || item == null) return false;

        return item.TryPickup(mainBackpack);
    }
}
