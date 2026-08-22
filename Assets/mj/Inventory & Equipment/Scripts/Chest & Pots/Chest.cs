using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Chest : MonoBehaviour
{
    [System.Serializable]
    public struct LootDrop
    {
        public ItemData itemData;

        [Min(1)]
        public int minQuantity;

        [Min(1)]
        public int maxQuantity;

        [Range(0f, 1f)]
        public float dropChance;
    }

    [Header("References")]
    [SerializeField] private SpriteRenderer chestRenderer;
    [SerializeField] private Sprite openSprite;
    [SerializeField] private GameObject promptObject;
    [SerializeField] private Transform spawnPoint;

    [Header("Loot Configuration")]
    [SerializeField] private Loot lootPrefab;
    [SerializeField] private List<LootDrop> lootTable = new List<LootDrop>();

    [Header("Isometric Scatter Settings")]
    [SerializeField] private float scatterForce = 3f;

    [Tooltip(
        "Flattens vertical force to match isometric 2:1 projection aspect ratio."
    )]
    [SerializeField] private float isoVerticalRatio = 0.5f;

    [Header("Interaction")]
    [Min(0.01f)]
    [SerializeField] private float interactionRange = 1.5f;

    private bool opened;

    // PlayerStats replaces the deleted PlayerGameplayFeatures.
    private PlayerStats player;

    public bool IsOpened => opened;

    // -------------------------------------------------
    // UNITY
    // -------------------------------------------------

    private void Awake()
    {
        if (chestRenderer == null)
            chestRenderer = GetComponent<SpriteRenderer>();

        if (promptObject == null)
        {
            Transform promptTransform =
                transform.Find("PRESS (E)");

            if (promptTransform != null)
                promptObject = promptTransform.gameObject;
        }

        SetPromptVisible(false);
    }

    private void Update()
    {
        if (opened)
            return;

        // Find the player using the PlayerStats component.
        if (player == null)
        {
            player =
                FindFirstObjectByType<PlayerStats>(
                    FindObjectsInactive.Include
                );
        }

        bool inRange =
            player != null &&
            IsPlayerInRange();

        SetPromptVisible(inRange);

        if (
            inRange &&
            Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame
        )
        {
            OpenChest();
        }
    }

    // -------------------------------------------------
    // OPEN CHEST
    // -------------------------------------------------

    public bool OpenChest()
    {
        if (
            opened ||
            player == null ||
            !IsPlayerInRange()
        )
        {
            return false;
        }

        opened = true;

        SetPromptVisible(false);

        if (
            chestRenderer != null &&
            openSprite != null
        )
        {
            chestRenderer.sprite = openSprite;
        }

        SpawnAndScatterLoot();

        return true;
    }

    // -------------------------------------------------
    // LOOT
    // -------------------------------------------------

    private void SpawnAndScatterLoot()
    {
        if (lootPrefab == null)
        {
            Debug.LogError(
                $"[Chest] {name} is missing a Loot prefab assignment!"
            );

            return;
        }

        Vector3 origin =
            spawnPoint != null
                ? spawnPoint.position
                : transform.position;

        foreach (LootDrop drop in lootTable)
        {
            if (
                drop.itemData == null ||
                Random.value > drop.dropChance
            )
            {
                continue;
            }

            int totalQuantity = Random.Range(
                drop.minQuantity,
                drop.maxQuantity + 1
            );

            Loot spawnedLoot =
                Instantiate(
                    lootPrefab,
                    origin,
                    Quaternion.identity
                );

            spawnedLoot.Setup(
                drop.itemData,
                totalQuantity
            );

            // Generate a random direction on the 2D ground plane.
            Vector2 randomDirection =
                Random.insideUnitCircle.normalized;

            // Compress Y to match the isometric projection.
            Vector2 isoDirection =
                new Vector2(
                    randomDirection.x,
                    randomDirection.y * isoVerticalRatio
                ).normalized;

            if (
                spawnedLoot.TryGetComponent(
                    out Rigidbody2D rb
                )
            )
            {
                float randomizedForce =
                    scatterForce *
                    Random.Range(0.8f, 1.2f);

                rb.AddForce(
                    isoDirection * randomizedForce,
                    ForceMode2D.Impulse
                );
            }
        }
    }

    // -------------------------------------------------
    // PLAYER RANGE
    // -------------------------------------------------

    private bool IsPlayerInRange()
    {
        if (player == null)
            return false;

        Vector2 offset =
            (Vector2)transform.position -
            (Vector2)player.transform.position;

        return offset.sqrMagnitude <=
               interactionRange * interactionRange;
    }

    // -------------------------------------------------
    // PROMPT
    // -------------------------------------------------

    private void SetPromptVisible(bool visible)
    {
        if (promptObject != null)
        {
            promptObject.SetActive(
                visible && !opened
            );
        }
    }
}