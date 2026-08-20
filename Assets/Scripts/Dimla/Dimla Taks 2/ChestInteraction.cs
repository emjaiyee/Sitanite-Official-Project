using UnityEngine;
using UnityEngine.InputSystem;

public class ChestInteraction : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer chestRenderer;
    [SerializeField] private Sprite openSprite;
    [SerializeField] private GameObject promptObject;

    [Header("Loot")]
    [SerializeField] private ItemData lootItem;
    [Min(1)] [SerializeField] private int lootQuantity = 1;

    [Header("Interaction")]
    [Min(0.01f)] [SerializeField] private float interactionRange = 1.5f;

    private bool opened;
    private PlayerGameplayFeatures player;

    public bool IsOpened => opened;

    private void Awake()
    {
        if (chestRenderer == null)
            chestRenderer = GetComponent<SpriteRenderer>();

        if (promptObject == null)
        {
            Transform promptTransform = transform.Find("PRESS (E)");
            if (promptTransform != null)
                promptObject = promptTransform.gameObject;
        }

        SetPromptVisible(false);
    }

    private void Update()
    {
        if (opened)
            return;

        if (player == null)
            player = FindFirstObjectByType<PlayerGameplayFeatures>(FindObjectsInactive.Include);

        bool inRange = player != null && IsPlayerInRange();
        SetPromptVisible(inRange);

        if (inRange && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            OpenChest();
    }

    /// <summary>Opens the chest and adds its configured loot to the inventory.</summary>
    public bool OpenChest()
    {
        if (opened || player == null || !IsPlayerInRange())
            return false;

        opened = true;
        SetPromptVisible(false);

        if (chestRenderer != null && openSprite != null)
            chestRenderer.sprite = openSprite;

        ItemGridUI inventory = FindFirstObjectByType<ItemGridUI>(FindObjectsInactive.Include);
        bool lootAdded = inventory != null && lootItem != null && inventory.TryAddItem(lootItem, lootQuantity);

        Debug.Log($"[ChestInteraction] {name} opened. Loot added: {lootAdded}. Item: {lootItem?.itemName ?? "none"}.");
        return true;
    }

    private bool IsPlayerInRange()
    {
        Vector2 offset = (Vector2)transform.position - (Vector2)player.transform.position;
        return offset.sqrMagnitude <= interactionRange * interactionRange;
    }

    private void SetPromptVisible(bool visible)
    {
        if (promptObject != null)
            promptObject.SetActive(visible && !opened);
    }
}
