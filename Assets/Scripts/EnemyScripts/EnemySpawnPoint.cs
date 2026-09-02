using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[AddComponentMenu("Dimla/Enemy Spawn Point")]
public class EnemySpawnPoint : MonoBehaviour
{
    [Header("Spawn Count (random)")]
    [Tooltip("Spawn a random number of enemies between Min and Max (inclusive).")]
    [SerializeField] private bool useRandomCount = true;
    [SerializeField] private int minSpawn = 2;
    [SerializeField] private int maxSpawn = 4;
    [Tooltip("If not using random count, this exact number will spawn.")]
    [SerializeField] private int fixedSpawnCount = 1;

    [Header("Spawn Distribution")]
    [Tooltip("Radius around the spawn point where enemies are placed.")]
    [SerializeField] private float spawnRadius = 0.5f;

    [Tooltip("Prevent spawned enemies from stacking exactly on the spawn point (keeps them slightly apart).")]
    [SerializeField] private float minSeparation = 0.3f;

    [Tooltip("If true, evenly distribute enemies around the spawn point (ring). If false, use random positions inside circle.")]
    [SerializeField] private bool evenDistribution = true;

    [Tooltip("Maximum angular jitter (degrees) applied to each evenly distributed spawn position.")]
    [SerializeField] private float angleJitterDegrees = 12f;

    [Tooltip("Radial jitter fraction (0..1) applied when using even distribution; 0 = exact radius, 1 = range [0, spawnRadius].")]
    [Range(0f, 1f)]
    [SerializeField] private float radialJitter = 0.25f;

    [Header("Spawn Options")]
    [SerializeField] private bool spawnOnStart = true;
    [SerializeField] private float spawnDelay = 0f;
    [SerializeField] private Transform spawnParent;

    [Header("Elevation")]
    [Tooltip("Elevation level assigned to each enemy spawned by this point.")]
    [Min(0)]
    [SerializeField] private int elevationLevel;

    [Header("Rendering (ensure visibility)")]
    [Tooltip("Sorting layer name to apply to spawned enemies.")]
    [SerializeField] private string sortingLayer = "Default";

    [Tooltip("Sorting order to apply to spawned enemies.")]
    [SerializeField] private int sortingOrder = 50;

    [Tooltip("Small Z offset applied after spawn to avoid Z-fighting.")]
    [SerializeField] private float zOffset = 0f;

    [SerializeField] private bool addSortingGroupIfMultipleRenderers = true;

    [Header("Collision checks")]
    [Tooltip("Layers considered when checking whether a spawn position is blocked (enemies, obstacles, tilemap etc.).")]
    [SerializeField] private LayerMask spawnCheckMask = Physics2D.AllLayers;

    [Tooltip("How many attempts to find a free position per enemy.")]
    [SerializeField] private int maxPositionAttempts = 12;

    [Header("Enemy clear behavior")]
    [Tooltip("If true, this spawn point reports itself cleared when all enemies spawned by it have died.")]
    [SerializeField] private bool clearWhenAllDead = true;


    // Event invoked whenever an enemy is successfully spawned.
    public event Action<GameObject> OnEnemySpawned;

    // Event invoked once when every enemy spawned by this point has died.
    // This also fires immediately if this spawn point spawns zero enemies.
    public event Action OnWaveCleared;


    // Tracked enemies created by this spawn point.
    private readonly List<GameObject> trackedEnemies = new List<GameObject>();

    // Prevents OnWaveCleared from being invoked more than once.
    private bool waveClearedRaised;


    private void Start()
    {
        if (!spawnOnStart)
            return;

        if (spawnDelay > 0f)
            StartCoroutine(DelayedSpawn(spawnDelay));
        else
            SpawnEnemies();
    }


    private IEnumerator DelayedSpawn(float delay)
    {
        yield return new WaitForSeconds(delay);
        SpawnEnemies();
    }


    /// <summary>
    /// Spawns multiple enemies according to configuration (random or fixed count).
    /// Returns the list of successfully spawned GameObjects.
    /// </summary>
    public List<GameObject> SpawnEnemies()
    {
        Debug.LogWarning(
            $"[EnemySpawnPoint] '{name}' no longer owns the prefab source. Use EnemySpawnerManager to drive spawning."
        );

        return new List<GameObject>();
    }


    /// <summary>
    /// Spawns multiple enemies using a prefab supplied by the room-level manager.
    /// </summary>
    public List<GameObject> SpawnEnemies(
        GameObject managedEnemyPrefab,
        int enemyLevel,
        Transform parentOverride)
    {
        if (managedEnemyPrefab == null)
        {
            Debug.LogWarning(
                "[EnemySpawnPoint] No enemy prefab was supplied by the spawner manager on " + name
            );

            return new List<GameObject>();
        }

        int count = fixedSpawnCount;

        if (useRandomCount)
        {
            int min = Mathf.Max(0, minSpawn);
            int max = Mathf.Max(min, maxSpawn);

            count = UnityEngine.Random.Range(min, max + 1);
        }

        List<GameObject> spawned = new List<GameObject>();

        if (count <= 0)
        {
            Debug.Log($"[EnemySpawnPoint] Spawn count is {count} at '{name}'. Spawn point is already clear.");
            RaiseWaveCleared();
            return spawned;
        }

        if (evenDistribution && count > 1)
        {
            float baseRadius = Mathf.Max(0.01f, spawnRadius);

            for (int i = 0; i < count; i++)
            {
                bool found = false;
                Vector2 chosenPos = (Vector2)transform.position;

                float baseAngle = (2f * Mathf.PI) * (i / (float)count);

                for (int attempt = 0; attempt < maxPositionAttempts; attempt++)
                {
                    float jitterRad =
                        Mathf.Deg2Rad *
                        UnityEngine.Random.Range(-angleJitterDegrees, angleJitterDegrees);

                    float angle = baseAngle + jitterRad;

                    float rMin = baseRadius * (1f - radialJitter);
                    float r = UnityEngine.Random.Range(rMin, baseRadius);

                    Vector2 pos =
                        (Vector2)transform.position +
                        new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r;

                    if (IsPositionAcceptable(pos, spawned, managedEnemyPrefab))
                    {
                        chosenPos = pos;
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    for (int fallback = 0; fallback < maxPositionAttempts; fallback++)
                    {
                        Vector2 pos =
                            (Vector2)transform.position +
                            UnityEngine.Random.insideUnitCircle * spawnRadius;

                        if (IsPositionAcceptable(pos, spawned, managedEnemyPrefab))
                        {
                            chosenPos = pos;
                            found = true;
                            break;
                        }
                    }
                }

                if (found)
                {
                    GameObject go = SpawnEnemyAt(
                        (Vector3)chosenPos,
                        managedEnemyPrefab,
                        enemyLevel,
                        parentOverride
                    );

                    if (go != null)
                        spawned.Add(go);
                }
                else
                {
                    Debug.LogWarning(
                        $"[EnemySpawnPoint] Could not find free spot for evenly distributed enemy slot {i + 1} at '{name}'."
                    );
                }
            }
        }
        else
        {
            int attemptsTotal = 0;

            for (int i = 0; i < count; i++)
            {
                Vector2 pos2D = Vector2.zero;
                bool found = false;
                int tries = 0;

                while (!found && tries < maxPositionAttempts)
                {
                    pos2D =
                        (Vector2)transform.position +
                        UnityEngine.Random.insideUnitCircle * spawnRadius;

                    if (IsPositionAcceptable(pos2D, spawned, managedEnemyPrefab))
                    {
                        found = true;
                        break;
                    }

                    tries++;
                    attemptsTotal++;
                }

                if (!found)
                {
                    Vector2 centerPos = transform.position;

                    if (IsPositionAcceptable(centerPos, spawned, managedEnemyPrefab))
                    {
                        pos2D = centerPos;
                        found = true;
                    }
                }

                if (found)
                {
                    GameObject go = SpawnEnemyAt(
                        (Vector3)pos2D,
                        managedEnemyPrefab,
                        enemyLevel,
                        parentOverride
                    );

                    if (go != null)
                        spawned.Add(go);
                }
                else
                {
                    Debug.LogWarning(
                        $"[EnemySpawnPoint] Could not find free spot for enemy {i + 1} at '{name}' after {maxPositionAttempts} attempts."
                    );
                }

                if (attemptsTotal > 1000)
                    break;
            }
        }

        Debug.Log($"[EnemySpawnPoint] Spawned {spawned.Count} enemy(ies) at '{name}'");

        if (spawned.Count == 0)
            RaiseWaveCleared();

        return spawned;
    }


    /// <summary>
    /// Spawns a single enemy at the spawn point position (no offset).
    /// </summary>
    public GameObject SpawnSingle()
    {
        Debug.LogWarning(
            $"[EnemySpawnPoint] '{name}' no longer owns the prefab source. Use EnemySpawnerManager to drive spawning."
        );

        return null;
    }


    /// <summary>
    /// Spawns a single enemy at the world position provided.
    /// </summary>
    public GameObject SpawnEnemyAt(Vector3 worldPosition)
    {
        Debug.LogWarning(
            $"[EnemySpawnPoint] '{name}' no longer owns the prefab source. Use EnemySpawnerManager to drive spawning."
        );

        return null;
    }


    /// <summary>
    /// Spawns a single enemy at the provided world position using a prefab supplied by the manager.
    /// </summary>
    public GameObject SpawnEnemyAt(
        Vector3 worldPosition,
        GameObject managedEnemyPrefab,
        int enemyLevel,
        Transform parentOverride = null)
    {
        if (managedEnemyPrefab == null)
        {
            Debug.LogWarning(
                "[EnemySpawnPoint] No enemy prefab was supplied by the spawner manager on " + name
            );

            return null;
        }

        GameObject instance =
            Instantiate(
                managedEnemyPrefab,
                worldPosition,
                transform.rotation,
                parentOverride != null ? parentOverride : spawnParent
            );

        ApplyRenderingSettings(instance);

        EnemyElevationLevel enemyElevation =
            instance.GetComponent<EnemyElevationLevel>();

        if (enemyElevation == null)
            enemyElevation = instance.AddComponent<EnemyElevationLevel>();

        enemyElevation.SetLevel(elevationLevel);

        EnemyLevelXP enemyLevelXp =
            instance.GetComponent<EnemyLevelXP>();

        if (enemyLevelXp == null)
            enemyLevelXp = instance.AddComponent<EnemyLevelXP>();

        enemyLevelXp.SetLevel(enemyLevel);

        EnemyHealth enemyHealth = instance.GetComponent<EnemyHealth>();

        if (enemyHealth == null)
            enemyHealth = instance.AddComponent<EnemyHealth>();

        EnsureEnemyPhysics(instance);

        enemyHealth.OnEnemyDied += HandleTrackedEnemyDied;
        trackedEnemies.Add(instance);

        OnEnemySpawned?.Invoke(instance);

        return instance;
    }


    /// <summary>
    /// Called when one of the enemies spawned by this spawn point dies.
    /// </summary>
    private void HandleTrackedEnemyDied(GameObject enemy)
    {
        // Unsubscribe if possible.
        var eh =
            enemy != null
                ? enemy.GetComponent<EnemyHealth>()
                : null;

        if (eh != null)
            eh.OnEnemyDied -= HandleTrackedEnemyDied;


        // Remove the dead enemy and any destroyed references.
        trackedEnemies.RemoveAll(
            g => g == null || g == enemy
        );


        // If no enemies spawned by this point remain,
        // report this spawn point as cleared.
        if (clearWhenAllDead &&
            trackedEnemies.Count == 0 &&
            !waveClearedRaised)
        {
            RaiseWaveCleared();
        }
    }


    /// <summary>
    /// Raises the wave-cleared event once.
    /// RoomManager will listen to this event later.
    /// </summary>
    private void RaiseWaveCleared()
    {
        if (waveClearedRaised)
            return;

        waveClearedRaised = true;

        Debug.Log(
            $"[EnemySpawnPoint] Wave cleared at '{name}'."
        );

        OnWaveCleared?.Invoke();
    }


    private bool IsPositionAcceptable(
        Vector2 pos,
        List<GameObject> alreadySpawned,
        GameObject prefab)
    {
        // Compute approximate collision radius from prefab/sprite
        // and combine with minSeparation.
        float approxRadius = GetApproximateRadius(prefab);
        float required = Mathf.Max(minSeparation, approxRadius);


        // Reject if any world collider overlaps here.
        Collider2D hit =
            Physics2D.OverlapCircle(
                pos,
                required,
                spawnCheckMask
            );

        if (hit != null)
            return false;


        // Also ensure separation from already spawned enemies.
        foreach (var go in alreadySpawned)
        {
            if (go == null)
                continue;

            float otherRadius =
                GetApproximateRadiusForGameObject(go);

            float dist =
                Vector2.Distance(
                    pos,
                    go.transform.position
                );

            if (dist < (required + otherRadius))
                return false;
        }


        return true;
    }


    private float GetApproximateRadius()
    {
        return minSeparation * 0.5f;
    }


    private float GetApproximateRadius(GameObject prefab)
    {
        if (prefab != null)
        {
            var collider =
                prefab.GetComponentInChildren<Collider2D>(true);

            if (collider != null)
                return collider.bounds.extents.magnitude;
        }

        return minSeparation * 0.5f;
    }


    private float GetApproximateRadiusForGameObject(GameObject go)
    {
        if (go == null)
            return minSeparation * 0.5f;

        var collider =
            go.GetComponentInChildren<Collider2D>(true);

        if (collider != null)
            return collider.bounds.extents.magnitude;

        return minSeparation * 0.5f;
    }


    private void ApplyRenderingSettings(GameObject root)
    {
        if (root == null)
            return;


        var srs =
            root.GetComponentsInChildren<SpriteRenderer>(true);

        foreach (var sr in srs)
        {
            if (!string.IsNullOrEmpty(sortingLayer))
                sr.sortingLayerName = sortingLayer;

            sr.sortingOrder = sortingOrder;
        }


        if (addSortingGroupIfMultipleRenderers &&
            srs.Length > 1)
        {
            var sg =
                root.GetComponent<SortingGroup>();

            if (sg == null)
                sg = root.AddComponent<SortingGroup>();

            sg.sortingLayerName = sortingLayer;
            sg.sortingOrder = sortingOrder;
        }

        if (Mathf.Abs(zOffset) > 0f)
        {
            var p = root.transform.position;

            root.transform.position =
                new Vector3(
                    p.x,
                    p.y,
                    p.z + zOffset
                );
        }
    }


    private static void EnsureEnemyPhysics(GameObject instance)
    {
        if (instance == null)
            return;


        Rigidbody2D body =
            instance.GetComponent<Rigidbody2D>();

        if (body == null)
            body = instance.AddComponent<Rigidbody2D>();


        body.bodyType = RigidbodyType2D.Dynamic;
        body.simulated = true;
        body.gravityScale = 0f;
        body.constraints = RigidbodyConstraints2D.FreezeAll;
        body.collisionDetectionMode =
            CollisionDetectionMode2D.Continuous;


        Collider2D[] colliders =
            instance.GetComponentsInChildren<Collider2D>(true);


        if (colliders.Length == 0)
        {
            BoxCollider2D fallbackCollider =
                instance.AddComponent<BoxCollider2D>();

            fallbackCollider.size =
                new Vector2(0.5f, 0.5f);

            colliders =
                new Collider2D[] { fallbackCollider };
        }


        foreach (Collider2D collider in colliders)
        {
            collider.enabled = true;
            collider.isTrigger = false;
        }
    }


#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;

        Gizmos.DrawWireSphere(
            transform.position,
            Mathf.Max(0.01f, spawnRadius)
        );


        Gizmos.color =
            new Color(1, 0, 1, 0.1f);

        Gizmos.DrawSphere(
            transform.position,
            0.02f
        );


        Gizmos.color = Color.cyan;

        Gizmos.DrawWireSphere(
            transform.position,
            minSeparation
        );
    }
#endif
}