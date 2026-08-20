using UnityEngine;

public class BreakablePot : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer potRenderer;
    [SerializeField] private Sprite brokenSprite;

    [Header("Prototype Loot")]
    [SerializeField] private ItemData lootItem;
    [Min(1)] [SerializeField] private int lootQuantity = 1;

    [Header("Interaction")]
    [Min(0.01f)] [SerializeField] private float interactionRange = 1.2f;

    private bool broken;

    public bool IsBroken => broken;
    public float InteractionRange => interactionRange;

    private void Awake()
    {
        if (potRenderer == null)
            potRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnMouseDown()
    {
        PlayerGameplayFeatures player = FindFirstObjectByType<PlayerGameplayFeatures>();
        if (player != null)
            player.TryBreakPot(this);
    }

    /// <summary>Changes the pot to its broken sprite, disables its collider, and adds prototype loot to the inventory.</summary>
    public bool TryBreak()
    {
        if (broken)
            return false;

        broken = true;

        if (potRenderer != null && brokenSprite != null)
            potRenderer.sprite = brokenSprite;

        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
            collider.enabled = false;

        ItemGridUI inventory = FindFirstObjectByType<ItemGridUI>(FindObjectsInactive.Include);
        if (inventory != null && lootItem != null)
            inventory.TryAddItem(lootItem, lootQuantity);

        Debug.Log($"[BreakablePot] {name} broke and dropped {lootQuantity} {lootItem?.itemName ?? "prototype loot"}.");
        return true;
    }
}
