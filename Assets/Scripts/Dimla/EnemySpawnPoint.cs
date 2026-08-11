using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[AddComponentMenu("Dimla/Enemy Spawn Point")]
public class EnemySpawnPoint : MonoBehaviour
{
    public enum SizeMode
    {
        UseSpriteNativeSize,
        ScaleToDesiredSize
    }

    [Header("Spawn Source")]
    [Tooltip("Preferred: assign a prefab (drag the enemy prefab here). If left empty, the script will create a GameObject using the assigned sprite.")]
    [SerializeField] private GameObject enemyPrefab;
    [Tooltip("Used only when no prefab is assigned. Drag a Sprite to create a simple enemy GameObject at runtime.")]
    [SerializeField] private Sprite enemySprite;

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

    [Header("Sizing")]
    [SerializeField] private SizeMode sizeMode = SizeMode.UseSpriteNativeSize;
    [Tooltip("When SizeMode is ScaleToDesiredSize: desired world size (units) of spawned enemy. Set to tile size (e.g. 1,1) to match tiles.")]
    [SerializeField] private Vector2 desiredSize = Vector2.one;
    [Tooltip("If true, add or resize a BoxCollider2D to match sprite bounds (applies after scaling).")]
    [SerializeField] private bool ensureBoxCollider = true;
    [Tooltip("If true, force using sprite-native size even for prefabs that contain a SpriteRenderer.")]
    [SerializeField] private bool forceUseSpriteNativeForPrefab = false;

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

    [Header("Enemy health & clear behavior")]
    [Tooltip("Max health assigned to each spawned enemy.")]
    [SerializeField] private int enemyMaxHealth = 10;
    [Tooltip("If true, when all enemies spawned by this spawn point die, mark the level cleared.")]
    [SerializeField] private bool clearWhenAllDead = true;
    [Tooltip("If > 0, this levelId will be sent to GameManager.PlayerSteppedOnLevel(levelId). If 0, GameManager.LevelCleared() will be called.")]
    [SerializeField] private int clearLevelId = 0;

    [Header("Runtime")]
    [SerializeField] private string createdEnemyName = "Enemy";

    // Event invoked when an enemy is spawned.
    public event Action<GameObject> OnEnemySpawned;

    // tracked spawned enemies created by this spawn point
    private readonly List<GameObject> trackedEnemies = new List<GameObject>();

    private void Start()
    {
        if (!spawnOnStart) return;

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
    /// Returns list of spawned GameObjects.
    /// </summary>
    public List<GameObject> SpawnEnemies()
    {
        int count = fixedSpawnCount;
        if (useRandomCount)
        {
            int min = Mathf.Max(0, minSpawn);
            int max = Mathf.Max(min, maxSpawn);
            count = UnityEngine.Random.Range(min, max + 1);
        }

        var spawned = new List<GameObject>();
        if (count <= 0)
        {
            Debug.Log($"[EnemySpawnPoint] Spawn count is {count} at '{name}'");
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
                    float jitterRad = Mathf.Deg2Rad * UnityEngine.Random.Range(-angleJitterDegrees, angleJitterDegrees);
                    float angle = baseAngle + jitterRad;
                    float rMin = baseRadius * (1f - radialJitter);
                    float r = UnityEngine.Random.Range(rMin, baseRadius);
                    Vector2 pos = (Vector2)transform.position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r;

                    if (IsPositionAcceptable(pos, spawned))
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
                        Vector2 pos = (Vector2)transform.position + UnityEngine.Random.insideUnitCircle * spawnRadius;
                        if (IsPositionAcceptable(pos, spawned))
                        {
                            chosenPos = pos;
                            found = true;
                            break;
                        }
                    }
                }

                if (found)
                {
                    var go = SpawnEnemyAt((Vector3)chosenPos);
                    if (go != null) spawned.Add(go);
                }
                else
                {
                    Debug.LogWarning($"[EnemySpawnPoint] Could not find free spot for evenly distributed enemy slot {i+1} at '{name}'.");
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
                    pos2D = (Vector2)transform.position + UnityEngine.Random.insideUnitCircle * spawnRadius;
                    if (IsPositionAcceptable(pos2D, spawned))
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
                    if (IsPositionAcceptable(centerPos, spawned))
                    {
                        pos2D = centerPos;
                        found = true;
                    }
                }

                if (found)
                {
                    var go = SpawnEnemyAt((Vector3)pos2D);
                    if (go != null) spawned.Add(go);
                }
                else
                {
                    Debug.LogWarning($"[EnemySpawnPoint] Could not find free spot for enemy {i+1} at '{name}' after {maxPositionAttempts} attempts.");
                }

                if (attemptsTotal > 1000) break;
            }
        }

        Debug.Log($"[EnemySpawnPoint] Spawned {spawned.Count} enemy(ies) at '{name}'");
        return spawned;
    }

    /// <summary>
    /// Spawns a single enemy at the spawn point position (no offset).
    /// </summary>
    public GameObject SpawnSingle()
    {
        return SpawnEnemyAt(transform.position);
    }

    /// <summary>
    /// Spawns a single enemy at the world position provided.
    /// </summary>
    public GameObject SpawnEnemyAt(Vector3 worldPosition)
    {
        GameObject instance = null;

        if (enemyPrefab != null)
        {
            instance = Instantiate(enemyPrefab, worldPosition, transform.rotation, spawnParent);
            ApplySizeAndCollider(instance);
            ApplyRenderingSettings(instance);
        }
        else if (enemySprite != null)
        {
            GameObject go = new GameObject(createdEnemyName);
            go.transform.position = worldPosition;
            go.transform.rotation = transform.rotation;
            if (spawnParent != null) go.transform.SetParent(spawnParent, true);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = enemySprite;

            if (sizeMode == SizeMode.UseSpriteNativeSize)
            {
                go.transform.localScale = Vector3.one;
                if (ensureBoxCollider) EnsureCollider(sr);
            }
            else if (sizeMode == SizeMode.ScaleToDesiredSize && desiredSize != Vector2.zero)
            {
                ApplyDesiredSize(go.transform, sr, desiredSize, ensureBoxCollider);
            }

            ApplyRenderingSettings(go);
            instance = go;
        }
        else
        {
            Debug.LogWarning("[EnemySpawnPoint] No enemyPrefab or enemySprite assigned on " + name);
            return null;
        }

        // ensure enemy has health component and set HP
        bool addedNewHealth = false;
        var eh = instance.GetComponent<EnemyHealth>();
        if (eh == null)
        {
            eh = instance.AddComponent<EnemyHealth>();
            addedNewHealth = true;
        }

        // Only initialize health when the component was just added.
        // This lets a prefab's EnemyHealth inspector value remain in effect.
        if (addedNewHealth)
            eh.Init(Mathf.Max(1, enemyMaxHealth));

        // subscribe to the death event
        eh.OnEnemyDied += HandleTrackedEnemyDied;

        trackedEnemies.Add(instance);

        OnEnemySpawned?.Invoke(instance);
        return instance;
    }

    private void HandleTrackedEnemyDied(GameObject enemy)
    {
        // Unsubscribe if possible
        var eh = enemy != null ? enemy.GetComponent<EnemyHealth>() : null;
        if (eh != null)
            eh.OnEnemyDied -= HandleTrackedEnemyDied;

        // Remove nulls and this enemy
        trackedEnemies.RemoveAll(g => g == null || g == enemy);

        if (clearWhenAllDead && trackedEnemies.Count == 0)
        {
                if (GameManager.Instance != null)
                {
                    if (clearLevelId > 0)
                        GameManager.Instance.PlayerSteppedOnLevel(clearLevelId);
                    else
                        GameManager.Instance.LevelCleared();
                }
            else
            {
                Debug.Log("[EnemySpawnPoint] Level Cleared (no GameManager present)");
            }
        }
    }

    private bool IsPositionAcceptable(Vector2 pos, List<GameObject> alreadySpawned)
    {
        // compute approximate collision radius from prefab/sprite and combine with minSeparation
        float approxRadius = GetApproximateRadius();
        float required = Mathf.Max(minSeparation, approxRadius);

        // Reject if any world collider overlaps here (tilemap, obstacles, other enemies etc.)
        Collider2D hit = Physics2D.OverlapCircle(pos, required, spawnCheckMask);
        if (hit != null)
        {
            return false;
        }

        // also ensure separation from already spawned (use their positions and their approximate radii)
        foreach (var go in alreadySpawned)
        {
            if (go == null) continue;
            float otherRadius = GetApproximateRadiusForGameObject(go);
            float dist = Vector2.Distance(pos, go.transform.position);
            if (dist < (required + otherRadius))
                return false;
        }

        return true;
    }

    private float GetApproximateRadius()
    {
        // approximate radius (world units) for prefab or sprite before spawn
        if (enemyPrefab != null)
        {
            var sr = enemyPrefab.GetComponentInChildren<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                // sprite bounds are in local (world) units when scale == 1
                return sr.sprite.bounds.extents.magnitude * Mathf.Max(1f, sr.transform.lossyScale.x);
            }
        }

        if (enemySprite != null)
        {
            return enemySprite.bounds.extents.magnitude;
        }

        // fallback to half of minSeparation
        return minSeparation * 0.5f;
    }

    private float GetApproximateRadiusForGameObject(GameObject go)
    {
        if (go == null) return minSeparation * 0.5f;
        var sr = go.GetComponentInChildren<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            // take transform scale into account
            Vector3 lossy = sr.transform.lossyScale;
            float maxScale = Mathf.Max(lossy.x, lossy.y);
            return sr.sprite.bounds.extents.magnitude * maxScale;
        }

        return minSeparation * 0.5f;
    }

    private void ApplySizeAndCollider(GameObject instance)
    {
        var sr = instance.GetComponentInChildren<SpriteRenderer>();
        if (sr == null) return;

        if (sizeMode == SizeMode.UseSpriteNativeSize && forceUseSpriteNativeForPrefab)
        {
            sr.transform.localScale = Vector3.one;
            if (ensureBoxCollider) EnsureCollider(sr);
        }
        else if (sizeMode == SizeMode.ScaleToDesiredSize && desiredSize != Vector2.zero)
        {
            ApplyDesiredSize(instance.transform, sr, desiredSize, ensureBoxCollider);
        }
    }

    private void ApplyRenderingSettings(GameObject root)
    {
        if (root == null) return;

        var srs = root.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var sr in srs)
        {
            if (!string.IsNullOrEmpty(sortingLayer))
                sr.sortingLayerName = sortingLayer;
            sr.sortingOrder = sortingOrder;
        }

        if (addSortingGroupIfMultipleRenderers && srs.Length > 1)
        {
            var sg = root.GetComponent<SortingGroup>();
            if (sg == null) sg = root.AddComponent<SortingGroup>();
            sg.sortingLayerName = sortingLayer;
            sg.sortingOrder = sortingOrder;
        }

        if (Mathf.Abs(zOffset) > 0f)
        {
            var p = root.transform.position;
            root.transform.position = new Vector3(p.x, p.y, p.z + zOffset);
        }
    }

    private static void ApplyDesiredSize(Transform root, SpriteRenderer sr, Vector2 desiredSize, bool ensureCollider)
    {
        if (sr == null || sr.sprite == null) return;

        Vector2 spriteWorldSize = sr.sprite.bounds.size;
        if (spriteWorldSize.x <= 0 || spriteWorldSize.y <= 0) return;

        float scaleX = desiredSize.x / spriteWorldSize.x;
        float scaleY = desiredSize.y / spriteWorldSize.y;
        root.localScale = new Vector3(scaleX, scaleY, root.localScale.z);

        if (ensureCollider) EnsureCollider(sr);
    }

    private static void EnsureCollider(SpriteRenderer sr)
    {
        if (sr == null || sr.sprite == null) return;

        var go = sr.gameObject;
        var collider = go.GetComponent<BoxCollider2D>();
        if (collider == null) collider = go.AddComponent<BoxCollider2D>();

        collider.size = sr.sprite.bounds.size;
        collider.offset = sr.sprite.bounds.center;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, Mathf.Max(0.01f, spawnRadius));
        Gizmos.color = new Color(1, 0, 1, 0.1f);
        Gizmos.DrawSphere(transform.position, 0.02f);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, minSeparation);
    }
#endif
} //