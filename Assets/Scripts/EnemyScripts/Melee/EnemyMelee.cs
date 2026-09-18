using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(EnemyLevelXP))]
[RequireComponent(typeof(EnemyHealth))]
public class EnemyMelee : MonoBehaviour
{
    // =========================================================
    // STATE
    // =========================================================


    public enum EnemyState
    {
        Idle,
        Chase,
        Search,
        Locate,
        Death
    }


    [Header("State")]
    [SerializeField] private EnemyState startingState =
        EnemyState.Idle;


    public EnemyState? CurrentState { get; private set; }




    // =========================================================
    // STATE INSTANCE
    // =========================================================


    private EnemyMeleeState currentState;


    // =========================================================
    // DETECTION
    // =========================================================

    [Header("Detection")]

    [Tooltip("Detection distance measured in A* grid cells.")]
    [SerializeField] private int detectionRadius = 6;

    [Min(0f)] [SerializeField] private float alertedDuration = 15f;

    public bool Alerted { get; private set; }

    private float alertedUntilTime;

    public int DetectionRadius =>
        Alerted
            ? Mathf.CeilToInt(detectionRadius * 1.5f)
            : detectionRadius;


    // =========================================================
    // MOVEMENT
    // =========================================================

    [Header("Movement")]

    [SerializeField] private float moveSpeed = 2f;

    public float MoveSpeed =>
        moveSpeed;


    // =========================================================
    // ATTACK
    // =========================================================

    [Header("Attack")]
    [SerializeField] private float attackRange = 0.8f;
    [SerializeField] private DamageType attackDamageType = DamageType.Slash;
    [Min(0f)] [SerializeField] private float damage = 5f;
    [Min(0.01f)] [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private bool useChargedAttack;

    [Header("Charged Attack")]
    [Min(0f)] [SerializeField] private float chargedAttackRange = 1.2f;
    [SerializeField] private DamageType chargedAttackDamageType = DamageType.Blunt;
    [Min(0f)] [SerializeField] private float chargedDamage = 10f;
    [Min(0.01f)] [SerializeField] private float chargedAttackCooldown = 2.5f;
    [Min(0f)] [SerializeField] private float chargedAttackTime = 0.75f;
    [Min(0f)] [SerializeField] private float chargedAttackMultiplier = 2f;

    private float nextAttackTime;
    private float chargedAttackTimer;
    private bool chargingAttack;
    private bool baseStatsCached;
    private float baseMoveSpeed;
    private float baseDamage;
    private float baseChargedDamage;


    // =========================================================
    // ANIMATION
    // =========================================================

    private static readonly int IsMovingHash =
        Animator.StringToHash("IsMoving");

    private static readonly int IsAttackingHash =
        Animator.StringToHash("IsAttacking");

    private static readonly int MoveXHash =
        Animator.StringToHash("MoveX");

    private static readonly int MoveYHash =
        Animator.StringToHash("MoveY");

    private Animator animator;

    public bool takingAim;

    public float AttackRange => useChargedAttack ? chargedAttackRange : attackRange;
    public float AttackCooldown => attackCooldown;
    public bool UseChargedAttack => useChargedAttack;


    [SerializeField] private int idleWanderRadius = 4;

    public int IdleWanderRadius =>
        idleWanderRadius;

    // =========================================================
    // LOCATE (damage origin)
    // =========================================================

    [Header("Locate")]
    [Tooltip("How long the enemy lingers at the damage origin before giving up.")]
    [Min(0f)] [SerializeField] private float locateWaitDuration = 2.5f;

    public float LocateWaitDuration => locateWaitDuration;

    // Last known position that damage came from.
    public Vector3? DamageSourcePosition { get; private set; }
    public Vector3? LastKnownPlayerPosition { get; private set; }

    /// <summary>
    /// Called by EnemyHealth when this enemy takes damage.
    /// Moves the FSM to Locate unless already chasing or dead.
    /// </summary>
    public void NotifyDamaged(Vector3? damageSource)
    {
        if (!damageSource.HasValue)
            return;

        if (CurrentState == EnemyState.Death)
            return;

        // Already chasing the player: no need to investigate.
        if (CurrentState == EnemyState.Chase)
            return;

        DamageSourcePosition = damageSource.Value;

        if (CurrentState == EnemyState.Locate)
        {
            EnemyMeleeLocateState locateState =
                currentState as EnemyMeleeLocateState;

            if (locateState != null)
                locateState.RefreshDestination();

            return;
        }

        ChangeState(EnemyState.Locate);
    }


    // =========================================================
    // DEATH
    // =========================================================

    [Header("Death")]

    [Tooltip(
        "How long the enemy remains fully visible " +
        "before the fade begins."
    )]
    [SerializeField] private float deathAnimationDelay = 0.5f;

    public float DeathAnimationDelay =>
        deathAnimationDelay;


    [Tooltip(
        "How long the enemy takes to fade out " +
        "before being destroyed."
    )]
    [SerializeField] private float deathFadeDuration = 0.75f;

    public float DeathFadeDuration =>
        deathFadeDuration;


    // =========================================================
    // REFERENCES
    // =========================================================

    private EnemyHealth enemyHealth;
    private EnemyElevationLevel enemyElevation;
    private EnemySFX enemySFX;
    private Transform player;
    private PlayerStats playerStats;

    private Vector3 spawnPosition;
    private float nextFootstepTime;

    [SerializeField] private float footstepInterval = 0.42f;


    public EnemyHealth Health =>
        enemyHealth;

    public Transform Player =>
        player;

    public Vector3 SpawnPosition =>
        spawnPosition;

    public int ElevationLevel =>
        enemyElevation != null ? enemyElevation.CurrentLevel : 0;

    public void SetIdleOrigin(Vector3 position)
    {
        spawnPosition = position;
    }


    // =========================================================
    // PATH
    // =========================================================

    private List<Vector3> currentPath;
    private int currentPathIndex;
    private bool movementPaused;

    public bool IsOnStairLink =>
        AStarManager.Instance != null &&
        AStarManager.Instance.GetStairLinkAtPosition(
            transform.position) != null;


    public bool HasPath =>
        currentPath != null &&
        currentPathIndex < currentPath.Count;


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        enemyHealth =
            GetComponent<EnemyHealth>();

        enemyElevation =
            GetComponent<EnemyElevationLevel>();

        enemySFX =
            GetComponent<EnemySFX>();

        animator =
            GetComponent<Animator>();

        CacheBaseStats();

        EnemyAttackScript legacyContactDamage =
            GetComponent<EnemyAttackScript>();
        if (legacyContactDamage != null)
            legacyContactDamage.enabled = false;

        spawnPosition =
            transform.position;


        // -----------------------------------------------------
        // PLAYER
        // -----------------------------------------------------

        GameObject playerObject =
            GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            player =
                playerObject.transform;
            playerStats =
                FindPlayerStats(playerObject);
        }
        else
        {
            Debug.LogWarning(
                $"[EnemyMelee] {name} could not find " +
                "a GameObject tagged 'Player'."
            );
        }
    }

    public void ApplyLevelScaling(int level)
    {
        CacheBaseStats();

        level = Mathf.Max(1, level);

        moveSpeed = baseMoveSpeed + GetMovementSpeedBonus(level, 0.005f);
        damage = baseDamage + GetScaledBonus(level, 5f);
        chargedDamage = baseChargedDamage + GetScaledBonus(level, 5f);
    }

    public void ApplyDamageModifier(float modifier)
    {
        CacheBaseStats();
        baseDamage = Mathf.Max(0f, baseDamage + modifier);
        baseChargedDamage = Mathf.Max(0f, baseChargedDamage + modifier);
        damage = Mathf.Max(0f, damage + modifier);
        chargedDamage = Mathf.Max(0f, chargedDamage + modifier);
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
        EnemyAttackScript legacyContactDamage =
            GetComponent<EnemyAttackScript>();
        if (legacyContactDamage != null)
            legacyContactDamage.enabled = false;

        nextAttackTime = Time.time + attackCooldown;

        // -----------------------------------------------------
        // CHECK A* SPAWN TILE
        // -----------------------------------------------------

        if (AStarManager.Instance == null)
        {
            Debug.LogWarning(
                $"[EnemyMelee] {name} could not find " +
                "an AStarManager. Starting FSM without pathfinding."
            );
        }

        if (AStarManager.Instance != null &&
            !AStarManager.Instance.IsPositionWalkable(
                transform.position,
                ElevationLevel))
        {
            Debug.LogWarning(
                $"[EnemyMelee] {name} spawned on a " +
                $"NON-WALKABLE A* tile at {transform.position}. " +
                "Starting FSM anyway."
            );
        }


        Debug.Log(
            $"[EnemyMelee] {name} spawned on " +
            "a valid A* tile."
        );


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
        if (Time.timeScale == 0f ||
            (GameManager.Instance != null && GameManager.Instance.IsPaused) ||
            (PauseMenuUI.Instance != null && PauseMenuUI.Instance.IsOpen))
        {
            return;
        }

        if (Alerted && Time.time >= alertedUntilTime)
            Alerted = false;

        if (player == null)
        {
            GameObject playerObject =
                GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
            {
                player = playerObject.transform;
                playerStats = FindPlayerStats(playerObject);
            }
        }

        Diagnose();

        if (currentState == null)
            return;


        SetAnimatorBool(IsAttackingHash, false);

        if (chargingAttack)
        {
            chargedAttackTimer += Time.deltaTime;
            if (chargedAttackTimer >= chargedAttackTime)
                CompleteAttack();

            return;
        }

        if (CurrentState != EnemyState.Death &&
            IsPlayerWithinAttackRange())
        {
            PauseMovement(true);
            SetTakingAim(CurrentState == EnemyState.Chase);
            TryAttack();
            return;
        }

        PauseMovement(false);
        SetTakingAim(false);

        currentState.Tick();
    }


    private void LateUpdate()
    {
        if (Time.timeScale == 0f ||
            (GameManager.Instance != null && GameManager.Instance.IsPaused) ||
            (PauseMenuUI.Instance != null && PauseMenuUI.Instance.IsOpen))
        {
            return;
        }

        UpdateAnimationDirection();
    }


    // =========================================================
    // PLAYER STATS LOOKUP
    // =========================================================

    /// <summary>
    /// Finds PlayerStats whether it sits on the tagged root,
    /// a parent, or a child object.
    /// </summary>
    private PlayerStats FindPlayerStats(GameObject playerObject)
    {
        if (playerObject == null)
            return null;

        PlayerStats stats =
            playerObject.GetComponentInParent<PlayerStats>();

        if (stats == null)
            stats = playerObject.GetComponentInChildren<PlayerStats>();

        return stats;
    }

    // =========================================================
    // TEMP DIAGNOSTIC — remove after fixing
    // =========================================================

    private float nextDiagTime;

    private void Diagnose()
    {
        if (Time.time < nextDiagTime)
            return;

        nextDiagTime = Time.time + 2f;

    }


    // =========================================================
    // ENEMY DEATH
    // =========================================================

    private void HandleEnemyDied(
    GameObject deadEnemy)
    {
    if (enemySFX != null)
        enemySFX.PlayDeathSFX();

    ChangeState(
        EnemyState.Death
    );
    }

    private void HandleDamaged(
        EnemyHealth source,
        Vector3? damageSource)
    {
        Alerted = true;
        alertedUntilTime = Time.time + alertedDuration;
        NotifyDamaged(damageSource);
    }


    public void ChangeState(
    EnemyState newState)
    {
        // -----------------------------------------------------
        // Ignore the request only if we ALREADY have an
        // actual state instance of this state.
        // -----------------------------------------------------

        if (CurrentState.HasValue &&
            CurrentState.Value == newState &&
            currentState != null)
        {
            return;
        }


        // -----------------------------------------------------
        // EXIT OLD STATE
        // -----------------------------------------------------

        if (currentState != null)
        {
            currentState.Exit();
        }

        takingAim = false;

        // Cancel any pending charged attack when the state changes
        // (e.g. the enemy dies mid-charge).
        chargingAttack = false;
        chargedAttackTimer = 0f;

        // -----------------------------------------------------
        // SET NEW STATE
        // -----------------------------------------------------

        CurrentState =
            newState;


        // -----------------------------------------------------
        // CREATE STATE INSTANCE
        // -----------------------------------------------------

        currentState =
            CreateState(newState);


        // -----------------------------------------------------
        // ENTER NEW STATE
        // -----------------------------------------------------

        if (currentState != null)
        {
            currentState.Enter();
        }


        Debug.Log(
            $"[EnemyMelee] {name} -> {newState}"
        );
    }

    private EnemyMeleeState CreateState(
    EnemyState state)
    {
        switch (state)
        {
            case EnemyState.Idle:
                return new EnemyMeleeIdleState(this);

            case EnemyState.Chase:
                return new EnemyMeleeChaseState(this);

            case EnemyState.Search:
                return new EnemyMeleeSearchState(this);

            case EnemyState.Locate:
                return new EnemyMeleeLocateState(this);

            case EnemyState.Death:
                return new EnemyMeleeDeathState(this);

            default:
                Debug.LogError(
                    $"[EnemyMelee] {name}: " +
                    $"Unknown state {state}."
                );

                return null;
        }
    }
    
    // =========================================================
    // DETECTION
    // =========================================================

    public bool IsPlayerDetected()
    {
        if (player == null)
            return false;


        if (AStarManager.Instance == null)
            return false;


        bool detected = AStarManager.Instance
            .IsPositionWithinDetectionRadius(
                transform.position,
                player.position,
                DetectionRadius,
                ElevationLevel
            );

        if (detected)
            LastKnownPlayerPosition = player.position;

        return detected;
    }

    public bool IsPlayerWithinAttackRange()
    {
        if (player == null)
            return false;

        float range = useChargedAttack ? chargedAttackRange : attackRange;
        return ((Vector2)player.position - (Vector2)transform.position)
            .sqrMagnitude <= range * range;
    }

    public void TryAttack()
    {
        if (CurrentState != EnemyState.Chase ||
            player == null ||
            !IsPlayerDetected() ||
            !IsPlayerWithinAttackRange() ||
            Time.time < nextAttackTime)
            return;

        if (playerStats == null)
            playerStats = FindPlayerStats(player.gameObject);

        if (playerStats == null)
        {
            Debug.LogWarning(
                $"[EnemyMelee] {name}: PlayerStats NOT FOUND on player " +
                "hierarchy. Enemy cannot deal damage. " +
                "Put PlayerStats on the object tagged 'Player' or a child."
            );

            // Don't spam: push the next attempt out by the cooldown.
            nextAttackTime = Time.time + attackCooldown;
            return;
        }

        if (useChargedAttack)
        {
            chargingAttack = true;
            chargedAttackTimer = 0f;
            StopMoving();
            return;
        }

        CompleteAttack();
    }

    private void CompleteAttack()
    {
        chargingAttack = false;
        chargedAttackTimer = 0f;

        if (player == null || !IsPlayerWithinAttackRange())
            return;

        if (playerStats == null)
            playerStats = FindPlayerStats(player.gameObject);

        if (playerStats == null)
        {
            Debug.LogWarning(
                $"[EnemyMelee] {name} could not find PlayerStats " +
                "on the player. Attack deals no damage."
            );
            return;
        }

        DamageType damageType = useChargedAttack
            ? chargedAttackDamageType
            : attackDamageType;
        float damageAmount = useChargedAttack
            ? chargedDamage
            : damage;
        if (useChargedAttack)
            damageAmount *= chargedAttackMultiplier;

        if (damageAmount <= 0f)
            return;

        float cooldown = useChargedAttack
            ? chargedAttackCooldown
            : attackCooldown;
        nextAttackTime = Time.time + cooldown;
        SetAnimatorBool(IsAttackingHash, true);

        if (enemySFX != null)
            enemySFX.PlayAttackSFX();

        playerStats.TakeDamage(
            damageAmount,
            damageType
        );
    }


    // =========================================================
    // MOVEMENT
    // =========================================================

    public void StopMoving()
    {
        currentPath = null;
        currentPathIndex = 0;
        movementPaused = false;
        SetAnimatorBool(IsMovingHash, false);
    }


    public void PauseMovement(bool paused)
    {
        movementPaused = paused;

        if (paused)
            SetAnimatorBool(IsMovingHash, false);
    }


    public bool SetPath(
        List<Vector3> path)
    {
        if (path == null ||
            path.Count == 0)
        {
            StopMoving();

            return false;
        }


        currentPath =
            path;

        currentPathIndex = 0;

        return true;
    }


    public void FollowCurrentPath()
    {
        if (movementPaused)
        {
            SetAnimatorBool(IsMovingHash, false);
            return;
        }

        if (!HasPath)
        {
            SetAnimatorBool(IsMovingHash, false);
            return;
        }

        SetAnimatorBool(IsMovingHash, true);

        if (enemySFX != null && Time.time >= nextFootstepTime)
        {
            enemySFX.PlayWalkSFX();
            nextFootstepTime = Time.time + footstepInterval;
        }

        Vector3 target =
            currentPath[currentPathIndex];

        transform.position =
            Vector3.MoveTowards(
                transform.position,
                target,
                moveSpeed * Time.deltaTime
            );

        if (Vector3.Distance(
                transform.position,
                target) <= 0.01f)
        {
            transform.position = target;
            currentPathIndex++;
        }
    }


    public void SetTakingAim(bool aiming)
    {
        takingAim = aiming;
    }


    private void UpdateAnimationDirection()
    {
        if (animator == null)
            return;


        Vector2 direction;

        if (takingAim && player != null)
        {
            direction =
                (Vector2)player.position -
                (Vector2)transform.position;
        }
        else if (HasPath && !movementPaused)
        {
            direction =
                (Vector2)currentPath[currentPathIndex] -
                (Vector2)transform.position;
        }
        else
        {
            return;
        }


        if (direction.sqrMagnitude <= 0.0001f)
            return;


        direction.Normalize();

        animator.SetFloat(MoveXHash, Mathf.Round(direction.x));
        animator.SetFloat(MoveYHash, Mathf.Round(direction.y));
    }


    private void SetAnimatorBool(int parameterHash, bool value)
    {
        if (animator != null)
            animator.SetBool(parameterHash, value);
    }


    // =========================================================
    // DEBUG GIZMOS
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        if (AStarManager.Instance == null)
            return;


        // -----------------------------------------------------
        // FIND ENEMY'S A* TILEMAP
        // -----------------------------------------------------

        UnityEngine.Tilemaps.Tilemap tilemap =
            AStarManager.Instance
                .GetTilemapAtPosition(
                    transform.position
                );


        if (tilemap == null)
            return;


        Vector3Int enemyCell =
            tilemap.WorldToCell(
                transform.position
            );


        // -----------------------------------------------------
        // DETECTION CELLS
        // -----------------------------------------------------

        int effectiveDetectionRadius =
            DetectionRadius;

        Gizmos.color =
            new Color(
                1f,
                1f,
                0f,
                0.25f
            );


        for (int x = -effectiveDetectionRadius;
            x <= effectiveDetectionRadius;
            x++)
        {
            for (int y = -effectiveDetectionRadius;
                y <= effectiveDetectionRadius;
                y++)
            {
                int squaredDistance =
                    x * x +
                    y * y;


                int squaredRadius =
                    effectiveDetectionRadius *
                    effectiveDetectionRadius;


                if (squaredDistance >
                    squaredRadius)
                {
                    continue;
                }


                Vector3Int cell =
                    enemyCell +
                    new Vector3Int(
                        x,
                        y,
                        0
                    );


                if (!tilemap.HasTile(cell))
                    continue;


                Vector3 center =
                    tilemap.GetCellCenterWorld(
                        cell
                    );


                Gizmos.DrawCube(
                    center,
                    tilemap.cellSize
                );
            }
        }
    }

    private void CacheBaseStats()
    {
        if (baseStatsCached)
            return;

        baseStatsCached = true;
    baseMoveSpeed = moveSpeed;
        baseDamage = damage;
        baseChargedDamage = chargedDamage;
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

    private static float GetMovementSpeedBonus(int level, float perLevelBonus)
    {
        if (level <= 1)
            return 0f;

        float bonus = 0f;
        float specialBonus = perLevelBonus * 2f;
        float followUpBonus = specialBonus * 0.75f;

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
                followUpBonus = specialBonus * 0.75f;
                continue;
            }

            bonus += perLevelBonus;
        }

        return bonus;
    }

}