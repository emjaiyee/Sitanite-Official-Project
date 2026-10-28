using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyHealth), typeof(EnemyLevelXP), typeof(EnemyElevationLevel))]
public class EnemyManggagaway : MonoBehaviour
{
    [Header("Identity and Room")]
    [SerializeField] private string bossName = "Manggagaway";
    [Tooltip("Leave unassigned on prefabs. The spawned parent room takes priority; otherwise resolved through RoomManager.")]
    [SerializeField] private RoomInstance room;
    [SerializeField] private ManggagawaySpriteAnimator spriteAnimator;
    [SerializeField] private Transform attackPoint;

    [Header("Detection and Movement")]
    [Min(1)] [SerializeField] private int detectionRadius = 12;
    [Min(0f)] [SerializeField] private float moveSpeed = 2f;
    [Min(0.1f)] [SerializeField] private float projectileRange = 6f;
    [Tooltip("While waiting on attack cooldowns, the boss keeps walking towards the player until this close.")]
    [Min(0f)] [SerializeField] private float stoppingDistance = 1.5f;
    [Min(0.1f)] [SerializeField] private float pathRefreshInterval = 0.5f;

    [Header("Phase")]
    [Range(0.01f, 0.99f)] [SerializeField] private float phaseThreshold = 0.45f;
    [Tooltip("Used only when transformation sprites have not yet been assigned.")]
    [Min(0.1f)] [SerializeField] private float transformationFallbackDuration = 1.5f;

    [Header("Spell Projectile")]
    [SerializeField] private GameObject projectilePrefab;
    [Min(0.01f)] [SerializeField] private float projectileCooldown = 1.5f;
    [Min(0.1f)] [SerializeField] private float projectileSpeed = 5f;
    [Min(0.1f)] [SerializeField] private float projectileLifetime = 2f;
    [Min(0.01f)] [SerializeField] private float projectileHitRadius = 0.15f;
    [Min(1)] [SerializeField] private int projectileDamage = 5;
    [FormerlySerializedAs("damageType")]
    [SerializeField] private DamageType projectileDamageType = DamageType.Necrosis;

    [Header("Beam")]
    [SerializeField] private GameObject beamPrefab;
    [SerializeField] private GameObject beamChargePrefab;
    [Min(0f)] [SerializeField] private float beamChargeDuration = 1.5f;
    [Min(0.01f)] [SerializeField] private float beamCooldown = 8f;
    [Range(0f, 1f)] [SerializeField] private float beamChance = 0.35f;
    [Min(0.01f)] [SerializeField] private float beamWidth = 0.6f;
    [Min(0.1f)] [SerializeField] private float beamDuration = 1f;
    [Min(1)] [SerializeField] private int beamDamagePerSecond = 10;
    [SerializeField] private DamageType beamDamageType = DamageType.Necrosis;

    [Header("AOE")]
    [SerializeField] private GameObject aoePrefab;
    [Min(0.1f)] [SerializeField] private float aoeRange = 2f;
    [Min(0.01f)] [SerializeField] private float aoeCooldown = 10f;
    [Min(1)] [SerializeField] private int aoeDamage = 10;
    [SerializeField] private DamageType aoeDamageType = DamageType.Necrosis;

    [Header("Summoning")]
    [SerializeField] private GameObject summonPrefab;
    [SerializeField] private List<GameObject> summonPool = new List<GameObject>();
    [Min(0.01f)] [SerializeField] private float summonCooldown = 30f;
    [Min(1)] [SerializeField] private int summonCount = 3;
    [Min(1)] [SerializeField] private int maxLivingSummons = 6;
    [Min(0.1f)] [SerializeField] private float summonRadius = 3f;

    [Header("Heal")]
    [SerializeField] private GameObject healPrefab;
    [Min(0.01f)] [SerializeField] private float healCooldown = 45f;
    [Min(1)] [SerializeField] private int healPerTick = 10;
    [Min(1f)] [SerializeField] private float healDuration = 30f;

    [Header("Teleportation")]
    [SerializeField] private GameObject teleportPrefab;
    [Min(0.01f)] [SerializeField] private float teleportCooldown = 30f;
    [Min(1)] [SerializeField] private int overwhelmHitCount = 10;
    [Range(0.01f, 1f)] [SerializeField] private float overwhelmDamageFraction = 0.3f;
    [Tooltip("Consecutive hits reset after this many seconds without damage. Damage is compared with HP at the start of the sequence.")]
    [Min(0.1f)] [SerializeField] private float successionWindow = 5f;
    [Min(0.1f)] [SerializeField] private float teleportMinPlayerDistance = 6f;
    [SerializeField] private LayerMask blockingLayers;
    [Min(0.01f)] [SerializeField] private float clearanceRadius = 0.35f;

    [Header("Death")]
    [Tooltip("Seconds to hold the final Dead frame before fading out.")]
    [Min(0f)] [SerializeField] private float deathLingerDuration = 1f;
    [Min(0f)] [SerializeField] private float deathFadeDuration = 1f;

    public EnemyHealth Health { get; private set; }
    public RoomInstance Room => room;
    public bool IsSecondForm { get; private set; }
    public bool IsTransforming { get; private set; }
    public bool IsHealing { get; private set; }
    public bool IsDead { get; private set; }

    private EnemyElevationLevel elevation;
    private PlayerStats playerStats;
    private Transform player;
    private PlayerRoomTracker playerRoomTracker;
    private Bounds roomBounds;
    private bool hasRoomBounds;
    private bool spottedPlayer;
    private bool playerWasNearby;
    private bool healPending;
    private bool busy;
    private bool overwhelmed;
    private int sequenceHits;
    private int sequenceDamage;
    private int sequenceStartHealth;
    private float lastHitTime = float.NegativeInfinity;
    private float nextProjectile, nextBeam, nextAoe, nextSummon, nextHeal, nextTeleport, nextPath;
    private List<Vector3> path;
    private int pathIndex;
    private Vector2 facing = Vector2.down;
    private GameObject healingVisual;
    private readonly List<GameObject> summons = new List<GameObject>();
    private readonly List<GameObject> ownedEffects = new List<GameObject>();

    private int Elevation => elevation.CurrentLevel;

    private void Awake()
    {
        Health = GetComponent<EnemyHealth>();
        elevation = GetComponent<EnemyElevationLevel>();
        if (spriteAnimator == null)
            spriteAnimator = GetComponentInChildren<ManggagawaySpriteAnimator>();
        Health.SetEnemyName(bossName);
    }

    private void OnEnable()
    {
        Health.OnDamageTaken += HandleDamage;
        Health.OnEnemyDied += HandleDeath;
    }

    private void Start()
    {
        ResolveRoom();
        UpdatePhaseFloor();
        nextSummon = Time.time + Random.Range(0f, summonCooldown);
        ValidatePrefabs();
    }

    private void OnDisable()
    {
        Health.OnDamageTaken -= HandleDamage;
        Health.OnEnemyDied -= HandleDeath;
        StopAllCoroutines();
        ClearEffects();
        busy = false;
        IsHealing = false;
        // Finish an interrupted transformation rather than leaving a permanently protected boss.
        if (IsTransforming)
            CompleteTransformation();
        Health.SetDamageProtection(false, IsSecondForm || IsDead ? 0 : PhaseFloor());
    }

    private void Update()
    {
        if (IsDead)
            return;
        ownedEffects.RemoveAll(effect => effect == null);
        ResolvePlayer();
        ResolveRoom();
        if (!IsSecondForm && !IsTransforming)
            UpdatePhaseFloor();
        bool nearby = IsPlayerNearby();
        if (nearby)
            spottedPlayer = true;
        if (playerWasNearby && !nearby && spottedPlayer && Health.CurrentHealth < Health.MaxHealth)
            healPending = true;
        playerWasNearby = nearby;

        if (IsTransforming || busy)
            return;
        if (overwhelmed && player != null && Time.time >= nextTeleport && TryTeleport())
            return;
        if (healPending && !nearby && !IsHealing && Time.time >= nextHeal && Health.CurrentHealth < Health.MaxHealth)
        {
            healPending = false;
            StartCoroutine(Heal());
        }
        if (!nearby)
        {
            Play(ManggagawayAnimationState.Idle);
            return;
        }

        facing = player.position - transform.position;
        float distance = Vector2.Distance(transform.position, player.position);
        if (distance <= aoeRange && Time.time >= nextAoe && aoePrefab != null)
        {
            nextAoe = Time.time + aoeCooldown;
            SpawnEffect(aoePrefab, transform.position, 1f);
            Damage(aoeDamage, aoeDamageType).HitTargets(Physics2D.OverlapCircleAll(transform.position, aoeRange, LayerMask.GetMask("Player")));
            StartCoroutine(AttackRecovery());
        }
        else if (spottedPlayer && Time.time >= nextSummon && summonPool.Count > 0 && TrySummon())
            StartCoroutine(AttackRecovery());
        else if (distance <= projectileRange)
        {
            if (Time.time >= nextProjectile)
            {
                if (beamPrefab != null && Time.time >= nextBeam && Random.value < beamChance)
                    StartCoroutine(FireBeam());
                else if (projectilePrefab != null)
                    FireProjectile();
            }
            else if (distance > stoppingDistance)
                Chase();
            else
                Play(ManggagawayAnimationState.Idle);
        }
        else
            Chase();
    }

    private void ResolvePlayer()
    {
        if (player != null || Player.Instance == null)
            return;
        player = Player.Instance.transform;
        playerStats = player.GetComponent<PlayerStats>();
        playerRoomTracker = player.GetComponent<PlayerRoomTracker>();
    }

    private void ResolveRoom()
    {
        RoomInstance parentRoom = GetComponentInParent<RoomInstance>();
        if (parentRoom != null && parentRoom != room)
        {
            room = parentRoom;
            hasRoomBounds = false;
        }
        else if (room == null || !room.gameObject.scene.IsValid())
        {
            RoomManager manager = FindFirstObjectByType<RoomManager>();
            room = manager != null ? manager.FindRoomAtPosition(transform.position) : null;
            hasRoomBounds = false;
        }
        if (room != null && !hasRoomBounds)
            hasRoomBounds = TryGetRoomBounds(room, out roomBounds);
    }

    private static bool TryGetRoomBounds(RoomInstance targetRoom, out Bounds bounds)
    {
        bounds = default;
        bool found = false;
        foreach (Renderer roomRenderer in targetRoom.GetComponentsInChildren<Renderer>(true))
        {
            if (roomRenderer == null || roomRenderer.GetComponentInParent<EnemyHealth>() != null)
                continue;
            if (found)
                bounds.Encapsulate(roomRenderer.bounds);
            else
                bounds = roomRenderer.bounds;
            found = true;
        }
        foreach (Collider2D roomCollider in targetRoom.GetComponentsInChildren<Collider2D>(true))
        {
            if (roomCollider == null || roomCollider.GetComponentInParent<EnemyHealth>() != null)
                continue;
            if (found)
                bounds.Encapsulate(roomCollider.bounds);
            else
                bounds = roomCollider.bounds;
            found = true;
        }
        return found;
    }

    private bool IsPlayerNearby()
    {
        if (player == null || !BossBalance.CanHitPlayer(playerStats, Elevation))
            return false;
        if (room != null && playerRoomTracker != null && playerRoomTracker.CurrentRoom != room)
            return false;
        return AStarManager.Instance != null && AStarManager.Instance.IsPositionWithinDetectionRadius(
            transform.position, player.position, detectionRadius, Elevation);
    }

    private int PhaseFloor() => Mathf.Max(1, Mathf.FloorToInt(Health.MaxHealth * phaseThreshold));
    private void UpdatePhaseFloor() => Health.SetDamageProtection(false, PhaseFloor());

    private void HandleDamage(int healthBefore, int damage)
    {
        StopHealing();
        healPending = false;
        if (Time.time - lastHitTime > successionWindow)
        {
            sequenceHits = 0;
            sequenceDamage = 0;
            sequenceStartHealth = healthBefore;
        }
        lastHitTime = Time.time;
        sequenceHits++;
        sequenceDamage += damage;
        float damageFraction = (float)sequenceDamage / Mathf.Max(1, sequenceStartHealth);
        overwhelmed = sequenceHits >= overwhelmHitCount ||
            damageFraction >= overwhelmDamageFraction ||
            Mathf.Approximately(damageFraction, overwhelmDamageFraction);
        if (!IsSecondForm && !IsTransforming && Health.CurrentHealth <= PhaseFloor())
        {
            StopAllCoroutines();
            StartCoroutine(TransformToSecondForm());
        }
    }

    private IEnumerator TransformToSecondForm()
    {
        IsTransforming = true;
        Health.SetDamageProtection(true, PhaseFloor());
        StopHealing();
        ClearEffects();
        path = null;
        busy = false;
        Play(ManggagawayAnimationState.Transformation);
        float duration = spriteAnimator != null ? spriteAnimator.Duration : 0f;
        yield return new WaitForSeconds(duration > 0f ? duration : transformationFallbackDuration);
        CompleteTransformation();
    }

    private void CompleteTransformation()
    {
        IsSecondForm = true;
        IsTransforming = false;
        Health.SetDamageProtection(false);
        Health.Heal(Health.MaxHealth);
        ResetOverwhelm();
        Play(ManggagawayAnimationState.Idle);
    }

    private void FireProjectile()
    {
        GameObject instance = Instantiate(projectilePrefab, Origin, Quaternion.identity);
        instance.GetComponent<ManggagawayProjectile>().Initialize(player, Damage(projectileDamage, projectileDamageType),
            projectileSpeed, projectileLifetime, projectileHitRadius);
        ownedEffects.Add(instance);
        nextProjectile = Time.time + projectileCooldown;
        StartCoroutine(AttackRecovery());
    }

    private IEnumerator FireBeam()
    {
        busy = true;
        nextBeam = Time.time + beamCooldown;
        nextProjectile = Time.time + projectileCooldown;
        Play(ManggagawayAnimationState.Attack);
        GameObject charge = SpawnEffect(beamChargePrefab, Origin, beamChargeDuration);
        yield return new WaitForSeconds(beamChargeDuration);
        if (charge != null)
            Destroy(charge);
        if (IsDead || IsTransforming)
        {
            busy = false;
            yield break;
        }
        if (player == null || !IsPlayerNearby())
        {
            busy = false;
            yield break;
        }
        Vector2 direction = ((Vector2)player.position - (Vector2)Origin).normalized;
        GameObject instance = Instantiate(beamPrefab, Origin,
            Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg));
        ownedEffects.Add(instance);
        Beam beam = instance.GetComponent<Beam>();
        beam.SetEnemyDamage(Damage(beamDamagePerSecond, beamDamageType));
        beam.Initialize(0, DamageType.None, 0, DamageType.None, 0, DamageType.None,
            projectileRange, beamWidth, beamDuration, LayerMask.GetMask("Player"));
        yield return new WaitForSeconds(beamDuration);
        busy = false;
    }

    private IEnumerator AttackRecovery()
    {
        busy = true;
        Play(ManggagawayAnimationState.Attack);
        float duration = spriteAnimator != null ? spriteAnimator.Duration : 0f;
        yield return new WaitForSeconds(Mathf.Max(0.1f, duration));
        busy = false;
    }

    private IEnumerator Heal()
    {
        IsHealing = true;
        nextHeal = Time.time + healCooldown;
        healingVisual = SpawnEffect(healPrefab, transform.position, healDuration);
        if (healingVisual != null)
            healingVisual.transform.SetParent(transform, true);
        int ticks = Mathf.FloorToInt(healDuration);
        for (int tick = 0; tick < ticks && IsHealing; tick++)
        {
            yield return new WaitForSeconds(1f);
            if (!IsHealing || IsDead || IsTransforming)
                break;
            Health.Heal(healPerTick);
            if (Health.CurrentHealth >= Health.MaxHealth)
                break;
        }
        StopHealing();
    }

    private void StopHealing()
    {
        IsHealing = false;
        if (healingVisual != null)
            Destroy(healingVisual);
        healingVisual = null;
    }

    private bool TrySummon()
    {
        summons.RemoveAll(instance => instance == null ||
            (instance.TryGetComponent(out EnemyHealth health) && health.CurrentHealth <= 0));
        if (summons.Count >= maxLivingSummons)
            return false;
        int spawned = 0;
        for (int i = 0; i < summonCount && summons.Count < maxLivingSummons; i++)
        {
            GameObject prefab = summonPool[Random.Range(0, summonPool.Count)];
            if (prefab == null || !TryFindPosition(summonRadius, out Vector3 position))
                continue;
            GameObject instance = Instantiate(prefab, position, Quaternion.identity, transform.parent);
            if (instance.TryGetComponent(out EnemyElevationLevel summonElevation))
                summonElevation.SetLevel(Elevation);
            summons.Add(instance);
            SpawnEffect(summonPrefab, position, 1f);
            spawned++;
        }
        nextSummon = Time.time + (spawned > 0 ? summonCooldown : 1f);
        return spawned > 0;
    }

    private bool TryTeleport()
    {
        if (!TryFindTeleportPosition(out Vector3 position))
        {
            nextTeleport = Time.time + 1f;
            Debug.LogWarning("[Manggagaway] No safe same-room teleport destination; retrying in one second.", this);
            return false;
        }
        SpawnEffect(teleportPrefab, transform.position, 1f);
        transform.position = position;
        SpawnEffect(teleportPrefab, position, 1f);
        path = null;
        nextPath = 0f;
        nextTeleport = Time.time + teleportCooldown;
        ResetOverwhelm();
        StartCoroutine(AttackRecovery());
        return true;
    }

    private bool TryFindPosition(float radius, out Vector3 position)
    {
        position = transform.position;
        for (int attempt = 0; attempt < 32; attempt++)
        {
            Vector3 candidate = transform.position + (Vector3)(Random.insideUnitCircle * radius);
            if (TryResolveWalkable(candidate, out position))
                return true;
        }
        return false;
    }

    private bool TryFindTeleportPosition(out Vector3 position)
    {
        position = transform.position;
        if (!hasRoomBounds || player == null)
            return false;
        float currentDistance = Vector2.Distance(transform.position, player.position);
        float bestDistance = -1f;
        bool found = false;
        for (int attempt = 0; attempt < 64; attempt++)
        {
            Vector3 candidate = new Vector3(
                Random.Range(roomBounds.min.x, roomBounds.max.x),
                Random.Range(roomBounds.min.y, roomBounds.max.y),
                transform.position.z);
            if (!TryResolveWalkable(candidate, out Vector3 center))
                continue;
            float distance = Vector2.Distance(center, player.position);
            if (distance < teleportMinPlayerDistance || distance <= currentDistance || distance <= bestDistance)
                continue;
            bestDistance = distance;
            position = center;
            found = true;
        }
        return found;
    }

    private bool TryResolveWalkable(Vector3 candidate, out Vector3 position)
    {
        position = candidate;
        AStarManager navigation = AStarManager.Instance;
        if (navigation == null || room == null)
            return false;
        Vector3? center = navigation.GetWalkableCellCenter(candidate, Elevation);
        if (!center.HasValue ||
            Physics2D.OverlapCircle(center.Value, clearanceRadius, blockingLayers) != null ||
            !room.ContainsWorldPosition(center.Value))
            return false;
        position = new Vector3(center.Value.x, center.Value.y, transform.position.z);
        return true;
    }

    private void Chase()
    {
        AStarManager navigation = AStarManager.Instance;
        if (navigation == null)
            return;
        if (Time.time >= nextPath)
        {
            nextPath = Time.time + pathRefreshInterval;
            path = navigation.FindPath(transform.position, player.position, Elevation);
            pathIndex = 0;
        }
        if (path == null || pathIndex >= path.Count)
        {
            Play(ManggagawayAnimationState.Idle);
            return;
        }
        Vector3 target = path[pathIndex];
        facing = target - transform.position;
        transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
        if (Vector2.Distance(transform.position, target) < 0.05f)
            pathIndex++;
        Play(ManggagawayAnimationState.Walk);
    }

    private Vector3 Origin => attackPoint != null ? attackPoint.position : transform.position;

    private BossBalance.HostileDamage Damage(int amount, DamageType damageType) => new BossBalance.HostileDamage(
        new[] { new DungeonMemory.DamageStat { type = damageType, damage = amount } }, Elevation);

    private GameObject SpawnEffect(GameObject prefab, Vector3 position, float lifetime)
    {
        if (prefab == null)
            return null;
        GameObject instance = Instantiate(prefab, position, Quaternion.identity);
        ownedEffects.Add(instance);
        Destroy(instance, Mathf.Max(0.01f, lifetime));
        return instance;
    }

    private void Play(ManggagawayAnimationState state)
    {
        if (spriteAnimator == null)
            return;
        if (IsSecondForm && (state == ManggagawayAnimationState.Idle ||
            state == ManggagawayAnimationState.Walk || state == ManggagawayAnimationState.Attack))
            state = (ManggagawayAnimationState)((int)state + (int)ManggagawayAnimationState.SecondFormIdle);
        spriteAnimator.Play(state, facing, IsSecondForm);
    }

    private void ResetOverwhelm()
    {
        overwhelmed = false;
        sequenceHits = sequenceDamage = 0;
        lastHitTime = float.NegativeInfinity;
    }

    private void ClearEffects()
    {
        foreach (GameObject effect in ownedEffects)
            if (effect != null)
                Destroy(effect);
        ownedEffects.Clear();
    }

    private void HandleDeath(GameObject dead)
    {
        IsDead = true;
        StopAllCoroutines();
        StopHealing();
        ClearEffects();
        foreach (Collider2D collider in GetComponentsInChildren<Collider2D>())
            collider.enabled = false;
        Play(ManggagawayAnimationState.Dead);
        if (isActiveAndEnabled)
            StartCoroutine(FadeAndDestroy());
    }

    private IEnumerator FadeAndDestroy()
    {
        float deadAnimation = spriteAnimator != null ? spriteAnimator.Duration : 0f;
        yield return new WaitForSeconds(deadAnimation + deathLingerDuration);
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
        Color[] colors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            colors[i] = renderers[i].color;
        for (float elapsed = 0f; elapsed < deathFadeDuration; elapsed += Time.deltaTime)
        {
            float alpha = 1f - elapsed / deathFadeDuration;
            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i] != null)
                    renderers[i].color = new Color(colors[i].r, colors[i].g, colors[i].b, colors[i].a * alpha);
            yield return null;
        }
        Destroy(gameObject);
    }

    private void ValidatePrefabs()
    {
        if (spriteAnimator == null)
            Debug.LogError("[Manggagaway] Assign a ManggagawaySpriteAnimator.", this);
        if (projectilePrefab == null || projectilePrefab.GetComponent<ManggagawayProjectile>() == null)
        {
            Debug.LogError("[Manggagaway] Spell Projectile requires a prefab with ManggagawayProjectile.", this);
            projectilePrefab = null;
        }
        if (beamPrefab == null || beamPrefab.GetComponent<Beam>() == null)
        {
            Debug.LogError("[Manggagaway] Beam requires a prefab with Beam.", this);
            beamPrefab = null;
        }
        if (summonPool.Count == 0)
            Debug.LogWarning("[Manggagaway] Summon Pool is empty; assign creature prefabs to enable summoning.", this);
    }
}
