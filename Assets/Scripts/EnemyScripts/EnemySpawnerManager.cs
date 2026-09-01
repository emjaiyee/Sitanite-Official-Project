using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Dimla/Enemy Spawner Manager")]
public class EnemySpawnerManager : MonoBehaviour
{
    public static EnemySpawnerManager Instance { get; private set; }

    [Serializable]
    private class AdditiveModifier
    {
        public float minimum = 0f;
        public float maximum = 0f;

        [Range(0f, 1f)]
        public float chance = 1f;

        public float Roll()
        {
            if (UnityEngine.Random.value > chance)
                return 0f;

            return UnityEngine.Random.Range(
                Mathf.Min(minimum, maximum),
                Mathf.Max(minimum, maximum)
            );
        }
    }

    [Serializable]
    private class SpawnConfiguration
    {
        [Header("Floor Range")]
        [Min(1)] public int minimumFloor = 1;
        [Min(1)] public int maximumFloor = 1;

        [Header("Enemy Sources")]
        public List<GameObject> enemyPrefabs = new List<GameObject>();

        [Header("Active Spawners")]
        [Min(0)] public int minimumActiveSpawners = 1;
        [Min(0)] public int maximumActiveSpawners = 3;
        [Range(0f, 1f)] public float activationChance = 1f;

        [Header("Enemy Modifiers")]
        public AdditiveModifier health = new AdditiveModifier();
        public AdditiveModifier resistance = new AdditiveModifier();
        public AdditiveModifier damage = new AdditiveModifier();
        public AdditiveModifier level = new AdditiveModifier();
        public AdditiveModifier experienceReward = new AdditiveModifier();

        public bool ContainsFloor(int floor)
        {
            return floor >= minimumFloor && floor <= maximumFloor;
        }
    }

    [Header("Floor Spawn Configurations")]
    [SerializeField] private List<SpawnConfiguration> spawnConfigurations =
        new List<SpawnConfiguration>();

    [Header("Spawn Options")]
    [SerializeField] private bool spawnOnStart = true;
    [SerializeField] private float spawnDelay = 0f;
    [SerializeField] private Transform spawnParent;

    [Header("Floor References")]
    [SerializeField] private FloorManager floorManager;

    public event Action<RoomInstance> OnRoomWaveCleared;

    private readonly HashSet<RoomInstance> registeredRooms =
        new HashSet<RoomInstance>();
    private readonly Dictionary<EnemySpawnPoint, RoomInstance> spawnPointRooms =
        new Dictionary<EnemySpawnPoint, RoomInstance>();
    private readonly Dictionary<RoomInstance, List<EnemySpawnPoint>> activeSpawnPoints =
        new Dictionary<RoomInstance, List<EnemySpawnPoint>>();
    private readonly Dictionary<RoomInstance, HashSet<EnemySpawnPoint>> clearedSpawnPoints =
        new Dictionary<RoomInstance, HashSet<EnemySpawnPoint>>();
    private readonly Dictionary<EnemySpawnPoint, Action> clearHandlers = new Dictionary<EnemySpawnPoint, Action>();

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

    public void BeginFloor(FloorManager floor)
    {
        foreach (KeyValuePair<EnemySpawnPoint, Action> entry in clearHandlers)
        {
            if (entry.Key != null)
                entry.Key.OnWaveCleared -= entry.Value;
        }

        registeredRooms.Clear();
        spawnPointRooms.Clear();
        activeSpawnPoints.Clear();
        clearedSpawnPoints.Clear();
        clearHandlers.Clear();
        floorManager = floor;
    }

    public void RegisterRoom(RoomInstance room)
    {
        if (room == null || !registeredRooms.Add(room))
            return;

        if (!spawnOnStart)
            return;

        if (spawnDelay > 0f)
            StartCoroutine(DelayedSpawn(room, spawnDelay));
        else
            SpawnRoomEnemies(room);
    }

    private IEnumerator DelayedSpawn(RoomInstance room, float delay)
    {
        yield return new WaitForSeconds(delay);
        SpawnRoomEnemies(room);
    }

    public void SpawnRoomEnemies(RoomInstance room)
    {
        if (room == null || !registeredRooms.Contains(room))
            return;

        EnemySpawnPoint[] spawnPoints =
            room.GetComponentsInChildren<EnemySpawnPoint>(true);

        if (spawnPoints.Length == 0)
        {
            RaiseRoomWaveCleared(room);
            return;
        }

        SpawnConfiguration configuration = GetCurrentSpawnConfiguration();
        int activeCount = GetActiveSpawnPointCount(configuration, spawnPoints.Length);

        List<EnemySpawnPoint> chosenSpawnPoints = SelectSpawnPoints(
            spawnPoints,
            activeCount,
            configuration
        );

        activeSpawnPoints[room] = chosenSpawnPoints;
        clearedSpawnPoints[room] = new HashSet<EnemySpawnPoint>();

        if (chosenSpawnPoints.Count == 0)
        {
            RaiseRoomWaveCleared(room);
            return;
        }

        foreach (EnemySpawnPoint spawnPoint in chosenSpawnPoints)
        {
            if (spawnPoint == null)
                continue;

            spawnPointRooms[spawnPoint] = room;

            GameObject enemyPrefab = GetRandomEnemyPrefab(configuration);
            if (enemyPrefab == null)
            {
                Debug.LogWarning(
                    $"[EnemySpawnerManager] Room {room.RoomNumber} has no enemy prefab for the current floor."
                );
                HandleSpawnPointCleared(spawnPoint, null);
                continue;
            }

            int enemyLevel = RollEnemyLevel(configuration);

            Action handler = null;
            handler = () => HandleSpawnPointCleared(spawnPoint, handler);
            clearHandlers[spawnPoint] = handler;
            spawnPoint.OnWaveCleared += handler;

            List<GameObject> spawnedEnemies = spawnPoint.SpawnEnemies(
                enemyPrefab,
                enemyLevel,
                spawnParent
            );

            foreach (GameObject enemy in spawnedEnemies)
                ApplyModifiers(enemy, configuration);
        }
    }

    private SpawnConfiguration GetCurrentSpawnConfiguration()
    {
        if (floorManager == null)
            floorManager = FindFirstObjectByType<FloorManager>();

        if (floorManager == null)
            return null;

        foreach (SpawnConfiguration configuration in spawnConfigurations)
        {
            if (configuration != null &&
                configuration.ContainsFloor(floorManager.CurrentFloor))
                return configuration;
        }

        return null;
    }

    private int GetActiveSpawnPointCount(SpawnConfiguration configuration, int availableSpawnPoints)
    {
        if (availableSpawnPoints <= 0)
            return 0;

        int minimum = 1;
        int maximum = availableSpawnPoints;

        if (configuration != null)
        {
            minimum = Mathf.Max(0, configuration.minimumActiveSpawners);
            maximum = Mathf.Max(minimum, configuration.maximumActiveSpawners);
        }

        minimum = Mathf.Clamp(minimum, 0, availableSpawnPoints);
        maximum = Mathf.Clamp(maximum, minimum, availableSpawnPoints);

        if (minimum == maximum)
            return minimum;

        return UnityEngine.Random.Range(minimum, maximum + 1);
    }

    private int RollEnemyLevel(SpawnConfiguration configuration)
    {
        float levelModifier = configuration != null
            ? configuration.level.Roll()
            : 0f;

        return Mathf.Max(1, 1 + Mathf.RoundToInt(levelModifier));
    }

    private GameObject GetRandomEnemyPrefab(SpawnConfiguration configuration)
    {
        if (configuration == null)
            return null;

        List<GameObject> validPrefabs = new List<GameObject>();

        foreach (GameObject enemyPrefab in configuration.enemyPrefabs)
        {
            if (enemyPrefab != null)
                validPrefabs.Add(enemyPrefab);
        }

        if (validPrefabs.Count == 0)
            return null;

        return validPrefabs[UnityEngine.Random.Range(0, validPrefabs.Count)];
    }

    private void ApplyModifiers(GameObject enemy, SpawnConfiguration configuration)
    {
        if (enemy == null || configuration == null)
            return;

        int healthModifier = Mathf.RoundToInt(configuration.health.Roll());
        float resistanceModifier = configuration.resistance.Roll();
        float damageModifier = configuration.damage.Roll();
        float experienceModifier = configuration.experienceReward.Roll();

        EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
            enemyHealth.ApplyAdditiveModifiers(healthModifier, resistanceModifier);

        EnemyMelee enemyMelee = enemy.GetComponent<EnemyMelee>();
        if (enemyMelee != null)
            enemyMelee.ApplyDamageModifier(damageModifier);

        EnemyRange enemyRange = enemy.GetComponent<EnemyRange>();
        if (enemyRange != null)
            enemyRange.ApplyDamageModifier(damageModifier);

        EnemyLevelXP enemyLevelXp = enemy.GetComponent<EnemyLevelXP>();
        if (enemyLevelXp != null)
            enemyLevelXp.AddExperienceReward(experienceModifier);
    }

    private List<EnemySpawnPoint> SelectSpawnPoints(
        EnemySpawnPoint[] spawnPoints,
        int activeCount,
        SpawnConfiguration configuration)
    {
        List<EnemySpawnPoint> pool = new List<EnemySpawnPoint>();

        foreach (EnemySpawnPoint spawnPoint in spawnPoints)
        {
            if (spawnPoint != null &&
                (configuration == null ||
                 UnityEngine.Random.value <= configuration.activationChance))
                pool.Add(spawnPoint);
        }

        if (pool.Count == 0 || activeCount <= 0)
            return new List<EnemySpawnPoint>();

        activeCount = Mathf.Min(activeCount, pool.Count);

        List<EnemySpawnPoint> selected = new List<EnemySpawnPoint>();

        for (int i = 0; i < activeCount; i++)
        {
            int index = UnityEngine.Random.Range(0, pool.Count);
            selected.Add(pool[index]);
            pool.RemoveAt(index);
        }

        return selected;
    }

    private void HandleSpawnPointCleared(EnemySpawnPoint spawnPoint, Action handler)
    {
        if (spawnPoint == null ||
            !spawnPointRooms.TryGetValue(spawnPoint, out RoomInstance room))
            return;

        if (handler != null)
            spawnPoint.OnWaveCleared -= handler;

        if (!clearedSpawnPoints[room].Add(spawnPoint))
            return;

        if (clearedSpawnPoints[room].Count >= activeSpawnPoints[room].Count)
            RaiseRoomWaveCleared(room);
    }

    private void RaiseRoomWaveCleared(RoomInstance room)
    {
        if (room == null ||
            !clearedSpawnPoints.TryGetValue(room, out HashSet<EnemySpawnPoint> cleared))
        {
            cleared = new HashSet<EnemySpawnPoint>();
            clearedSpawnPoints[room] = cleared;
        }

        if (!cleared.Add(null))
            return;

        OnRoomWaveCleared?.Invoke(room);
    }
}