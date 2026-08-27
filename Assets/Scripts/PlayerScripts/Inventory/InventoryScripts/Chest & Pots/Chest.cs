using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Chest : MonoBehaviour, IDamageable
{
    [System.Serializable]
    public struct LootDrop
    {
        public ItemData itemData;
        [Min(1)] public int minQuantity;
        [Min(1)] public int maxQuantity;
        [Range(0f, 1f)] public float dropChance;
    }

    [Header("References")]
    [SerializeField] private SpriteRenderer chestRenderer;
    [SerializeField] private Sprite openSprite;
    [SerializeField] private Transform spawnPoint;

    [Header("Loot Configuration")]
    [SerializeField] private GameObject lootPrefab;
    [SerializeField] private List<LootDrop> lootPool = new List<LootDrop>();

    [Header("Isometric Scatter")]
    [SerializeField] private float scatterForce = 10f;
    [SerializeField] private float verticalRatio = 0.5f;

    [Header("Damage")]
    [SerializeField] private DamageType openableDamageTypes =
        DamageType.Physical | DamageType.Blunt;

    private bool opened;
    private Collider2D chestCollider;

    public bool IsOpened => opened;

    private void Awake()
    {
        if (chestRenderer == null)
            chestRenderer = GetComponent<SpriteRenderer>();

        chestCollider = GetComponent<Collider2D>();
    }

    /// <summary>
    /// IDamageable implementation. Allows weapon hits to trigger the chest open state.
    /// </summary>
    public void TakeDamage(int amount, DamageType damageType = DamageType.Slash)
    {
        if ((damageType & openableDamageTypes) == 0)
            return;

        OpenChest();
    }

    /// <summary>Swaps sprite and scatters configured loot onto the isometric ground plane.</summary>
    public bool OpenChest()
    {
        if (opened) return false;

        opened = true;

        if (chestRenderer != null && openSprite != null)
            chestRenderer.sprite = openSprite;

        SpawnAndScatterLoot();
        return true;
    }

    private void SpawnAndScatterLoot()
    {
        if (lootPrefab == null)
        {
            Debug.LogWarning($"[Chest] {name} is missing an assigned lootPrefab!");
            return;
        }

        Vector3 origin = spawnPoint != null ? spawnPoint.position : transform.position;

        foreach (LootDrop drop in lootPool)
        {
            if (drop.itemData == null || Random.value > drop.dropChance)
                continue;

            int totalQuantity = Random.Range(drop.minQuantity, drop.maxQuantity + 1);

            GameObject spawnedGo = Instantiate(lootPrefab, origin, Quaternion.identity);

            if (spawnedGo.TryGetComponent(out Loot spawnedLoot))
            {
                spawnedLoot.Setup(drop.itemData, totalQuantity);
            }

            Vector2 randomDirection = Random.insideUnitCircle.normalized;
            Vector2 isoDirection = new Vector2(randomDirection.x, randomDirection.y * verticalRatio).normalized;

            if (spawnedGo.TryGetComponent(out Rigidbody2D rb))
            {
                float randomizedForce = scatterForce * Random.Range(0.8f, 1.2f);
                rb.AddForce(isoDirection * randomizedForce, ForceMode2D.Impulse);
            }
        }
    }
}