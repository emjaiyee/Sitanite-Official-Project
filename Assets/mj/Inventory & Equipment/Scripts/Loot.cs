// Can be changed for 3D
using System.Runtime.CompilerServices;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class Loot : MonoBehaviour
{
    [SerializeField] private ItemData itemData;
    [SerializeField] private int quantity = 1;

    [SerializeField] private SpriteRenderer spriteRenderer;

    public ItemData Data => itemData;
    public int Quantity => quantity;

    private void Awake()
    {
        if (spriteRenderer == null)
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (TryGetComponent<Collider2D>(out var col))
        {
            col.isTrigger = true;
        }
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

    // Initialize loot properties
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

    private void UpdateVisuals()
    {
        if (spriteRenderer == null) return;

        if (itemData != null && itemData.inventoryIcon != null)
        {
            spriteRenderer.sprite =itemData.inventoryIcon;
            gameObject.name = $"Loot_{itemData.name} (x{quantity})";
        }
        else
        {
            spriteRenderer.sprite = null;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
{
    if (other.CompareTag("Player") && other.TryGetComponent<PlayerInventory>(out var playerInventory))
    {
        playerInventory.Pickup(this);
    }
}
    
}
