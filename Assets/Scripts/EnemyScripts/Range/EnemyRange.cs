using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(EnemyLevelXP))]
[RequireComponent(typeof(EnemyHealth))]
public class EnemyRange : MonoBehaviour
{
    public enum EnemyState
    {
        Idle,
        Chase,
        Search,
        Death
    }

    [Header("State")]
    [SerializeField] private EnemyState startingState = EnemyState.Idle;

    public EnemyState? CurrentState { get; private set; }

    private EnemyRangeState currentState;

    [Header("Detection")]
    [Tooltip("Detection distance measured in A* grid cells.")]
    [SerializeField] private int detectionRadius = 12;

    [Tooltip("Attack detection distance measured in A* grid cells.")]
    [SerializeField] private int attackDetectionRadius = 10;

    public int DetectionRadius => Alerted ? Mathf.CeilToInt(detectionRadius * 1.5f) : detectionRadius;
    public int AttackDetectionRadius => attackDetectionRadius;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private int idleWanderRadius = 4;

    public float MoveSpeed => moveSpeed;
    public int IdleWanderRadius => idleWanderRadius;

    [Header("Attack")]
    [SerializeField] private DamageType projectileDamageType = DamageType.Physical;
    [Min(0)] [SerializeField] private int projectileDamage = 5;
    [Min(0.01f)] [SerializeField] private float attackCooldown = 1.5f;
    [Min(0.01f)] [SerializeField] private float projectileSpeed = 7f;
    [Min(0.01f)] [SerializeField] private float projectileLifetime = 4f;

    [Header("Projectile")]
    [SerializeField] private GameObject projectilePrefab;

    [Header("Death")]
    [SerializeField] private float deathAnimationDelay = 0.5f;
    [SerializeField] private float deathFadeDuration = 0.75f;

    private EnemyHealth enemyHealth;
    private Transform player;
    private PlayerStats playerStats;
    private Vector3 spawnPosition;
    private bool alerted;
    private float alertedUntilTime;
    private float nextAttackTime;
    private bool baseStatsCached;
    private float baseMoveSpeed;
    private int baseProjectileDamage;

    private List<Vector3> currentPath;
    private int currentPathIndex;
    private bool movementPaused;

    public EnemyHealth Health => enemyHealth;
    public Transform Player => player;
    public Vector3 SpawnPosition => spawnPosition;
    public float DeathAnimationDelay => deathAnimationDelay;
    public float DeathFadeDuration => deathFadeDuration;
    public bool Alerted => alerted;

    public bool IsOnStairLink =>
        AStarManager.Instance != null &&
        AStarManager.Instance.GetStairLinkAtPosition(transform.position) != null;

    public bool HasPath =>
        currentPath != null &&
        currentPathIndex < currentPath.Count;

    private void Awake()
    {
        enemyHealth = GetComponent<EnemyHealth>();
        spawnPosition = transform.position;
        CacheBaseStats();

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
            playerStats = FindPlayerStats(playerObject);
        }
        else
        {
            Debug.LogWarning($"[EnemyRange] {name} could not find a GameObject tagged 'Player'.");
        }
    }

    public void ApplyLevelScaling(int level)
    {
        CacheBaseStats();

        level = Mathf.Max(1, level);

        moveSpeed = baseMoveSpeed + GetScaledBonus(level, 0.5f);
        projectileDamage = baseProjectileDamage + Mathf.RoundToInt(GetScaledBonus(level, 5f));
    }

    private void OnEnable()
    {
        if (enemyHealth != null)
        {
            enemyHealth.OnEnemyDied += HandleEnemyDied;
            enemyHealth.OnDamaged += HandleDamaged;
        }
    }

    private void Start()
    {
        EnemyAttackScript legacyContactDamage = GetComponent<EnemyAttackScript>();
        if (legacyContactDamage != null)
            legacyContactDamage.enabled = false;

        if (AStarManager.Instance == null)
        {
            Debug.LogWarning($"[EnemyRange] {name} could not find an AStarManager. Starting FSM without pathfinding.");
        }
        else if (!AStarManager.Instance.IsPositionWalkable(transform.position))
        {
            Debug.LogWarning($"[EnemyRange] {name} spawned on a NON-WALKABLE A* tile at {transform.position}. Starting FSM anyway.");
        }

        ChangeState(startingState);
    }

    private void OnDisable()
    {
        if (enemyHealth != null)
        {
            enemyHealth.OnEnemyDied -= HandleEnemyDied;
            enemyHealth.OnDamaged -= HandleDamaged;
        }
    }

    private void Update()
    {
        if (alerted && Time.time >= alertedUntilTime)
            alerted = false;

        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
                playerStats = FindPlayerStats(playerObject);
            }
        }

        if (currentState == null)
            return;

        if (CurrentState != EnemyState.Death && IsPlayerWithinAttackRange())
        {
            PauseMovement(true);
            TryShootProjectile();
            return;
        }

        PauseMovement(false);
        currentState.Tick();
    }

    private PlayerStats FindPlayerStats(GameObject playerObject)
    {
        if (playerObject == null)
            return null;

        PlayerStats stats = playerObject.GetComponentInParent<PlayerStats>();
        if (stats == null)
            stats = playerObject.GetComponentInChildren<PlayerStats>();

        return stats;
    }

    private void HandleEnemyDied(GameObject deadEnemy)
    {
        ChangeState(EnemyState.Death);
    }

    private void HandleDamaged(EnemyHealth source, Vector3? damageSource)
    {
        alerted = true;
        alertedUntilTime = Time.time + 15f;
    }

    public void ChangeState(EnemyState newState)
    {
        if (CurrentState.HasValue && CurrentState.Value == newState && currentState != null)
            return;

        if (currentState != null)
            currentState.Exit();

        nextAttackTime = 0f;
        CurrentState = newState;
        currentState = CreateState(newState);

        if (currentState != null)
            currentState.Enter();

        Debug.Log($"[EnemyRange] {name} -> {newState}");
    }

    private EnemyRangeState CreateState(EnemyState state)
    {
        switch (state)
        {
            case EnemyState.Idle:
                return new EnemyRangeIdleState(this);
            case EnemyState.Chase:
                return new EnemyRangeChaseState(this);
            case EnemyState.Search:
                return new EnemyRangeSearchState(this);
            case EnemyState.Death:
                return new EnemyRangeDeathState(this);
            default:
                Debug.LogError($"[EnemyRange] {name}: Unknown state {state}.");
                return null;
        }
    }

    public bool IsPlayerDetected()
    {
        if (player == null || AStarManager.Instance == null)
            return false;

        return AStarManager.Instance.IsPositionWithinDetectionRadius(
            transform.position,
            player.position,
            DetectionRadius);
    }

    public bool IsPlayerWithinAttackRange()
    {
        if (player == null || AStarManager.Instance == null)
            return false;

        return AStarManager.Instance.IsPositionWithinDetectionRadius(
            transform.position,
            player.position,
            AttackDetectionRadius);
    }

    public bool IsPlayerRayCasted()
    {
        if (player == null)
            return false;

        int layerMask = LayerMask.GetMask("Player", "Default");
        Vector2 direction = player.position - transform.position;
        float distance = direction.magnitude;

        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            direction.normalized,
            distance,
            layerMask);

        return hit.collider != null && hit.collider.CompareTag("Player");
    }

    public void StopMoving()
    {
        currentPath = null;
        currentPathIndex = 0;
    }

    public void PauseMovement(bool paused)
    {
        movementPaused = paused;
    }

    public bool SetPath(List<Vector3> path)
    {
        if (path == null || path.Count == 0)
        {
            StopMoving();
            return false;
        }

        currentPath = path;
        currentPathIndex = 0;
        return true;
    }

    public void FollowCurrentPath()
    {
        if (movementPaused || !HasPath)
            return;

        Vector3 target = currentPath[currentPathIndex];
        transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target) <= 0.01f)
        {
            transform.position = target;
            currentPathIndex++;
        }
    }

    public bool TryShootProjectile()
    {
        if (projectilePrefab == null)
            return false;

        if (player == null || Time.time < nextAttackTime)
            return false;

        if (playerStats == null)
            playerStats = FindPlayerStats(player.gameObject);

        if (playerStats == null)
            return false;

        if (!IsPlayerRayCasted())
            return false;

        GameObject projectileObject = Instantiate(
            projectilePrefab,
            transform.position,
            Quaternion.identity);

        BaseArrow projectile = projectileObject.GetComponent<BaseArrow>();
        if (projectile != null)
        {
            projectile.Launch(
                player.position - transform.position,
                projectileDamage,
                projectileDamageType,
                projectileSpeed,
                projectileLifetime);
        }

        nextAttackTime = Time.time + attackCooldown;
        return true;
    }

    private void CacheBaseStats()
    {
        if (baseStatsCached)
            return;

        baseStatsCached = true;
        baseMoveSpeed = moveSpeed;
        baseProjectileDamage = projectileDamage;
    }

    private static float GetScaledBonus(int level, float perLevelBonus)
    {
        if (level <= 1)
            return 0f;

        float bonus = 0f;
        float specialBonus = perLevelBonus * 2f;
        float followUpBonus = Mathf.Ceil(specialBonus * 0.75f);

        for (int currentLevel = 2; currentLevel <= level; currentLevel++)
        {
            int levelInCycle = currentLevel % 5;

            if (levelInCycle == 0)
            {
                bonus += specialBonus;
                continue;
            }

            if (levelInCycle == 1)
            {
                bonus += followUpBonus;
                specialBonus = followUpBonus * 2f;
                followUpBonus = Mathf.Ceil(specialBonus * 0.75f);
                continue;
            }

            bonus += perLevelBonus;
        }

        return bonus;
    }
}