using UnityEngine;

/// <summary>
/// Handles world item drops, visual representation, and player interaction/pickup logic.
/// Made to be a prefab, for visual representation feedback.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class Loot : MonoBehaviour
{
    #region Serialized Fields
    [Header("Data & Configuration")]
    [Tooltip("The ScriprtableObject Item data that you want this loot to be.")]
    [SerializeField] private ItemData itemData;

    [Tooltip("Set how many items does this loot have?")]
    [SerializeField] private int quantity = 1;

    [Header("References")]
    [Tooltip("Main visible SpriteRender (Auto assigned don't worry)")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    #endregion

    #region Properties
    /// <summary>Get the assigned item data.</summary>
    public ItemData Data => itemData;

    /// <summary>Get current item quantity.</summary>
    public int Quantity => quantity;
    #endregion

    #region Lifecycle
    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (TryGetComponent<Collider2D>(out var col))
            col.isTrigger = true;
    }

    private void Start()
    {
        UpdateVisuals();
    }

    private void OnValidate()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        UpdateVisuals();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && other.TryGetComponent<PlayerInventory>(out var playerInventory))
        {
            playerInventory.Pickup(this);
        }
    }
    #endregion

    #region Public API
    /// <summary>
    /// Initialize the loot instance with item data and initial stack size.
    /// </summary>
    /// <param name="data">ScriptableObject ItemData</param>
    /// <param name="initialQuantity">Stack size (clamped to minimum 1).</param>
    public void Setup(ItemData data, int initialQuantity)
    {
        itemData = data;
        quantity = Mathf.Max(1, initialQuantity);
        UpdateVisuals();
    }

    /// <summary>
    /// Attempts to add item to player inventory.
    /// Handles full pickups (destroys object) and partial pickups (reduces world quantity).
    /// </summary>
    /// <param name="playerInventory">Target inventory system.</param>
    /// <returns>True if at least one item was picked up; otherwise, false.</returns>
    public bool TryPickup(InventoryGrid playerInventory)
    {
        if (playerInventory == null || itemData == null || quantity <= 0)
            return false;

        InventoryItem tempItem = new InventoryItem(itemData, quantity);

        if (playerInventory.TryAddItem(tempItem))
        {
            Destroy(gameObject);
            return true;
        }

        // Handle partial stacks: If the player inventory could only accept part of the stack,
        // update the remaining world quantity and sync visual representations.
        if (tempItem.Quantity < quantity)
        {
            quantity = tempItem.Quantity;
            UpdateVisuals();
            return true;
        }

        return false;
    }
    #endregion

    #region Helpers
    private void UpdateVisuals()
    {
        if (spriteRenderer == null) return;

        if (itemData != null && itemData.inventoryIcon != null)
        {
            spriteRenderer.sprite = itemData.inventoryIcon;
            gameObject.name = $"Loot_{itemData.name} (x{quantity})";
        }
        else
        {
            spriteRenderer.sprite = null;
        }
    }
    #endregion
}