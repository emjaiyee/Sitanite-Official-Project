using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles world item drops, visual representation, and player interaction/pickup logic.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class Loot : MonoBehaviour
{
    #region Serialized Fields
    [Header("Data & Configuration")]
    [Tooltip("The ScriptableObject Item data that you want this loot to be.")]
    [SerializeField] private ItemData itemData;

    [Tooltip("Set how many items does this loot have?")]
    [SerializeField] private int quantity = 1;

    [Header("References")]
    [Tooltip("Main visible SpriteRenderer (Auto assigned don't worry)")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    #endregion

    #region Properties
    public ItemData Data => itemData;
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
        if (!other.CompareTag("Player"))
            return;

        if (other.TryGetComponent<PlayerInventory>(out var playerInventory) || 
            other.GetComponentInParent<PlayerInventory>() != null)
        {
            playerInventory = playerInventory ?? other.GetComponentInParent<PlayerInventory>();
            playerInventory.Pickup(this);
        }
    }
    #endregion

    #region Public API
    public void Setup(ItemData data, int initialQuantity)
    {
        itemData = data;
        quantity = Mathf.Max(1, initialQuantity);
        UpdateVisuals();
    }

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