<<<<<<< HEAD
=======
using System.Collections.Generic;
>>>>>>> main
using UnityEngine;

/// <summary>
/// Handles world item drops, visual representation, and player interaction/pickup logic.
<<<<<<< HEAD
/// Made to be a prefab, for visual representation feedback.
=======
>>>>>>> main
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class Loot : MonoBehaviour
{
    #region Serialized Fields
    [Header("Data & Configuration")]
<<<<<<< HEAD
    [Tooltip("The ScriprtableObject Item data that you want this loot to be.")]
=======
    [Tooltip("The ScriptableObject Item data that you want this loot to be.")]
>>>>>>> main
    [SerializeField] private ItemData itemData;

    [Tooltip("Set how many items does this loot have?")]
    [SerializeField] private int quantity = 1;

    [Header("References")]
<<<<<<< HEAD
    [Tooltip("Main visible SpriteRender (Auto assigned don't worry)")]
=======
    [Tooltip("Main visible SpriteRenderer (Auto assigned don't worry)")]
>>>>>>> main
    [SerializeField] private SpriteRenderer spriteRenderer;
    #endregion

    #region Properties
<<<<<<< HEAD
    /// <summary>Get the assigned item data.</summary>
    public ItemData Data => itemData;

    /// <summary>Get current item quantity.</summary>
=======
    public ItemData Data => itemData;
>>>>>>> main
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
<<<<<<< HEAD
        if (other.CompareTag("Player") && other.TryGetComponent<PlayerInventory>(out var playerInventory))
        {
=======
        if (!other.CompareTag("Player"))
            return;

        if (other.TryGetComponent<PlayerInventory>(out var playerInventory) || 
            other.GetComponentInParent<PlayerInventory>() != null)
        {
            playerInventory = playerInventory ?? other.GetComponentInParent<PlayerInventory>();
>>>>>>> main
            playerInventory.Pickup(this);
        }
    }
    #endregion

    #region Public API
<<<<<<< HEAD
    /// <summary>
    /// Initialize the loot instance with item data and initial stack size.
    /// </summary>
    /// <param name="data">ScriptableObject ItemData</param>
    /// <param name="initialQuantity">Stack size (clamped to minimum 1).</param>
=======
>>>>>>> main
    public void Setup(ItemData data, int initialQuantity)
    {
        itemData = data;
        quantity = Mathf.Max(1, initialQuantity);
        UpdateVisuals();
    }

<<<<<<< HEAD
    /// <summary>
    /// Attempts to add item to player inventory.
    /// Handles full pickups (destroys object) and partial pickups (reduces world quantity).
    /// </summary>
    /// <param name="playerInventory">Target inventory system.</param>
    /// <returns>True if at least one item was picked up; otherwise, false.</returns>
=======
>>>>>>> main
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

<<<<<<< HEAD
        // Handle partial stacks: If the player inventory could only accept part of the stack,
        // update the remaining world quantity and sync visual representations.
=======
>>>>>>> main
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