using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
<<<<<<< HEAD
public class BreakablePot : MonoBehaviour
=======
public class BreakablePot : MonoBehaviour, IDamageable
>>>>>>> main
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
    [SerializeField] private SpriteRenderer potRenderer;
    [SerializeField] private Sprite brokenSprite;
    [SerializeField] private Transform spawnPoint;

    [Header("Loot Configuration")]
    [SerializeField] private GameObject lootPrefab;
    [SerializeField] private List<LootDrop> lootPool = new List<LootDrop>();

    [Header("Isometric Scatter")]
    [SerializeField] private float scatterForce = 2.5f;
    [SerializeField] private float verticalRatio = 0.5f;

    [Header("Interaction")]
    [Min(0.01f)] [SerializeField] private float interactionRange = 1.2f;

    private bool broken;
    private Collider2D potCollider;

    public bool IsBroken => broken;
    public float InteractionRange => interactionRange;

    private void Awake()
    {
        if (potRenderer == null)
            potRenderer = GetComponent<SpriteRenderer>();

        potCollider = GetComponent<Collider2D>();
    }

<<<<<<< HEAD
=======
    /// <summary>
    /// IDamageable implementation. Allows MeleeWeapon.cs to break the pot via interface.
    /// </summary>
    public void TakeDamage(int amount, DamageType damageType = DamageType.Physical)
    {
        TryBreak();
    }

>>>>>>> main
    /// <summary>Swaps sprite, disables collision, and scatters loot onto the isometric ground plane.</summary>
    public bool TryBreak()
    {
        if (broken) return false;

        broken = true;

        if (potRenderer != null && brokenSprite != null)
            potRenderer.sprite = brokenSprite;

        if (potCollider != null)
            potCollider.enabled = false;

        SpawnAndScatterLoot();
        return true;
    }

    private void SpawnAndScatterLoot()
    {
<<<<<<< HEAD
        if (lootPrefab == null) return;
=======
        if (lootPrefab == null)
        {
            Debug.LogWarning($"[BreakablePot] {name} is missing a assigned lootPrefab!");
            return;
        }
>>>>>>> main

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