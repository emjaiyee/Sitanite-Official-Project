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

    [Header("Outline")]
    [SerializeField] private bool showOutline = true;
    [Min(0f)] [SerializeField] private float outlineWidth = 0.025f;
    [SerializeField] private Color outlineColor = Color.white;

    private SpriteRenderer[] outlineRenderers;
    private bool pickupInProgress;
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

        CreateOutline();

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
        if (pickupInProgress || playerInventory == null ||
            itemData == null || quantity <= 0)
            return false;

        InventoryItem tempItem = new InventoryItem(itemData, quantity);

        if (playerInventory.TryAddItem(tempItem))
        {
            pickupInProgress = true;
            if (TryGetComponent<Collider2D>(out var collider))
                collider.enabled = false;

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

        if (itemData != null &&
            (itemData.lootIcon != null || itemData.inventoryIcon != null))
        {
            spriteRenderer.sprite = itemData.lootIcon != null
                ? itemData.lootIcon
                : itemData.inventoryIcon;
            gameObject.name = $"Loot_{itemData.name} (x{quantity})";
        }
        else
        {
            spriteRenderer.sprite = null;
        }

        UpdateOutline();
    }

    private void CreateOutline()
    {
        if (!showOutline || spriteRenderer == null || outlineRenderers != null)
            return;

        Vector2[] offsets =
        {
            Vector2.up,
            Vector2.down,
            Vector2.left,
            Vector2.right,
            new Vector2(1f, 1f).normalized,
            new Vector2(1f, -1f).normalized,
            new Vector2(-1f, 1f).normalized,
            new Vector2(-1f, -1f).normalized
        };

        outlineRenderers = new SpriteRenderer[offsets.Length];
        for (int index = 0; index < offsets.Length; index++)
        {
            GameObject outline = new GameObject("Loot Outline");
            outline.transform.SetParent(spriteRenderer.transform, false);
            outline.transform.localPosition = offsets[index] * outlineWidth;

            SpriteRenderer outlineRenderer = outline.AddComponent<SpriteRenderer>();
            outlineRenderer.color = outlineColor;
            outlineRenderers[index] = outlineRenderer;
        }

        UpdateOutline();
    }

    private void UpdateOutline()
    {
        if (outlineRenderers == null)
            CreateOutline();

        if (outlineRenderers == null)
            return;

        foreach (SpriteRenderer outlineRenderer in outlineRenderers)
        {
            if (outlineRenderer == null)
                continue;

            outlineRenderer.sprite = spriteRenderer.sprite;
            outlineRenderer.color = outlineColor;
            outlineRenderer.flipX = spriteRenderer.flipX;
            outlineRenderer.flipY = spriteRenderer.flipY;
            outlineRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
            outlineRenderer.sortingOrder = spriteRenderer.sortingOrder - 1;
            outlineRenderer.transform.localScale = Vector3.one;
        }
    }
    #endregion
}