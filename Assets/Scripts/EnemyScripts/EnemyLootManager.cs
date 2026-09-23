using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Dimla/Enemy Loot Manager")]
public class EnemyLootManager : MonoBehaviour
{
    [Serializable]
    public struct LootDrop
    {
        public ItemData itemData;
        [Min(1)] public int minimumQuantity;
        [Min(1)] public int maximumQuantity;
    }

    [Serializable]
    private class FloorLootConfiguration
    {
        [Min(1)] public int minimumFloor = 1;
        [Min(1)] public int maximumFloor = 1;
        [Range(0f, 1f)] public float lootChance = 1f;
        public List<LootDrop> lootPool = new List<LootDrop>();

        public bool ContainsFloor(int floor)
        {
            return floor >= minimumFloor && floor <= maximumFloor;
        }
    }

    public static EnemyLootManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private FloorManager floorManager;
    [SerializeField] private GameObject lootPrefab;

    [Header("Regular Enemy Floor Loot Configurations")]
    [SerializeField] private List<FloorLootConfiguration> lootConfigurations =
        new List<FloorLootConfiguration>();

    [Header("Boss Floor Loot Configurations")]
    [SerializeField] private List<FloorLootConfiguration> bossLootConfigurations =
        new List<FloorLootConfiguration>();

    [Header("Spawn Placement")]
    [Min(0f)] [SerializeField] private float spawnSearchRadius = 1f;
    [Min(1)] [SerializeField] private int spawnSearchAttempts = 8;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void SpawnLoot(Vector3 origin, EnemyType enemyType)
    {
        List<FloorLootConfiguration> configurations =
            GetCurrentConfigurations(enemyType);

        if (configurations.Count == 0)
            return;

        FloorLootConfiguration configuration = configurations[
            UnityEngine.Random.Range(0, configurations.Count)
        ];

        if (UnityEngine.Random.value > configuration.lootChance)
            return;

        LootDrop? drop = GetRandomDrop(configuration.lootPool);
        if (!drop.HasValue || lootPrefab == null)
            return;

        Vector3? spawnPosition = FindWalkableSpawnPosition(origin);
        if (!spawnPosition.HasValue)
            return;

        LootDrop selectedDrop = drop.Value;
        GameObject lootObject = Instantiate(
            lootPrefab,
            spawnPosition.Value,
            Quaternion.identity
        );

        if (lootObject.TryGetComponent(out Loot loot))
        {
            loot.Setup(
                selectedDrop.itemData,
                UnityEngine.Random.Range(
                    Mathf.Min(
                        selectedDrop.minimumQuantity,
                        selectedDrop.maximumQuantity
                    ),
                    Mathf.Max(
                        selectedDrop.minimumQuantity,
                        selectedDrop.maximumQuantity
                    ) + 1
                )
            );
        }
    }

    private List<FloorLootConfiguration> GetCurrentConfigurations(
        EnemyType enemyType)
    {
        List<FloorLootConfiguration> configurations =
            new List<FloorLootConfiguration>();

        if (floorManager == null)
            floorManager = FindFirstObjectByType<FloorManager>();

        if (floorManager == null)
            return configurations;

        List<FloorLootConfiguration> lootConfigurationList =
            enemyType == EnemyType.Boss
                ? bossLootConfigurations
                : lootConfigurations;

        foreach (FloorLootConfiguration configuration in lootConfigurationList)
        {
            if (configuration != null &&
                configuration.ContainsFloor(floorManager.CurrentFloor))
            {
                configurations.Add(configuration);
            }
        }

        return configurations;
    }

    private static LootDrop? GetRandomDrop(List<LootDrop> lootPool)
    {
        List<LootDrop> validDrops = new List<LootDrop>();

        foreach (LootDrop drop in lootPool)
        {
            if (drop.itemData != null)
                validDrops.Add(drop);
        }

        if (validDrops.Count == 0)
            return null;

        return validDrops[UnityEngine.Random.Range(0, validDrops.Count)];
    }

    private Vector3? FindWalkableSpawnPosition(Vector3 origin)
    {
        if (AStarManager.Instance == null)
            return null;

        Vector3? position = AStarManager.Instance.GetWalkableCellCenter(origin);
        if (position.HasValue)
            return position;

        for (int attempt = 0; attempt < spawnSearchAttempts; attempt++)
        {
            Vector2 offset = UnityEngine.Random.insideUnitCircle * spawnSearchRadius;
            position = AStarManager.Instance.GetWalkableCellCenter(
                origin + new Vector3(offset.x, offset.y, 0f)
            );

            if (position.HasValue)
                return position;
        }

        return null;
    }
}