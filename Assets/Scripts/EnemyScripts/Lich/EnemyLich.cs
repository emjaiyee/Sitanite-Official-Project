using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(EnemyLevelXP))]
[RequireComponent(typeof(EnemyHealth))]

public class EnemyLich : MonoBehaviour
{
    #region Stats/ States/ Variables
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
    [SerializeField]
    private EnemyState startingState =
        EnemyState.Idle;

    public EnemyState? CurrentState { get; private set; }

    private EnemyLichState currentState;


    // =========================================================
    // DETECTION
    // =========================================================

    [Header("Detection")]

    [Tooltip(
        "Detection distance measured in A* grid cells. " +
        "The enemy must detect the player before entering Chase."
    )]
    [SerializeField] private int detectionRadius = 12;

    public int DetectionRadius =>
        detectionRadius;


    // =========================================================
    // MOVEMENT
    // =========================================================

    [Header("Movement")]

    [SerializeField] private float moveSpeed = 3f;

    [SerializeField] private int idleWanderRadius = 4;

    public float MoveSpeed =>
        moveSpeed;

    public int IdleWanderRadius =>
        idleWanderRadius;

    [Header("Locate")]
    [Min(0f)][SerializeField] private float locateWaitDuration = 2.5f;

    public float LocateWaitDuration => locateWaitDuration;

    public Vector3? DamageSourcePosition { get; private set; }
    public Vector3? LastKnownPlayerPosition { get; private set; }


    // =========================================================
    // ATTACK
    // =========================================================

    [Header("Attack")]

    [Tooltip(
        "Maximum world-space distance at which the enemy can " +
        "fire a projectile once it is in Chase state."
    )]
    [Min(0f)]
    [SerializeField] private float attackRange = 4f;

    [Header("Projectile")]
    [SerializeField]
    private DamageType projectileDamageType =
        DamageType.Physical;

    [Min(0)]
    [SerializeField] private int projectileDamage = 5;

    [Min(0.01f)]
    [SerializeField] private float attackCooldown = 1.5f;

    [Min(0.01f)]
    [SerializeField] private float projectileSpeed = 7f;

    [Min(0.01f)]
    [SerializeField] private float projectileLifetime = 4f;


    [Header("Missile")]
    [SerializeField]
    private DamageType missileDamageType =
        DamageType.Physical;

    [Min(0)]
    [SerializeField] private int missileDamage = 10;

    [Min(0.01f)]
    [SerializeField] private float missileCooldown = 6.5f;

    [Min(0.01f)]
    [SerializeField] private float missileSpeed = 3f;

    [Min(0.01f)]
    [SerializeField] private float missileLifetime = 5f;


    [Tooltip(
        "The point where the enemy shoots from. " +
        "If left empty, the enemy's transform position is used."
    )]
    [SerializeField] private Transform attackPoint;

    [Header("Projectiles")]

    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private GameObject missileProjectilePrefab;


    private float nextAttackTime;
    private float nextMissileTime;

    private bool useMissileProjectile = false;

    public float AttackRange =>
        attackRange;


    // =========================================================
    // STATS
    // =========================================================

    private bool baseStatsCached;
    private float baseMoveSpeed;
    private int baseProjectileDamage;
    private int baseMissileDamage;

    // =========================================================
    // SUMMON STATS
    // =========================================================
    [Header("Summon")]
    [Min(0)]
    [SerializeField]private int summonLevel = 0;
    [SerializeField]private GameObject summonEnemy;
    [SerializeField]private List<GameObject> summonList;
    [SerializeField]private float summonCooldown = 30f;
    [SerializeField]private bool summonFirst = false;
    [SerializeField]private float spawnRadius = 0.7f;
    [SerializeField]private float nextSummonTime;
    [SerializeField] LayerMask hittableLayers = Physics2D.AllLayers;
    [SerializeField] float checkRadius = 0.5f;

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


    // =========================================================
    // REFERENCES
    // =========================================================

    private EnemyHealth enemyHealth;
    private EnemyElevationLevel enemyElevation;
    private Transform player;
    private PlayerStats playerStats;

    private Vector3 spawnPosition;

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

    public void ApplyDamageModifier(float modifier)
    {
        CacheBaseStats();
        baseProjectileDamage = Mathf.Max(
            0,
            Mathf.RoundToInt(baseProjectileDamage + modifier)
        );
        projectileDamage = Mathf.Max(
            0,
            Mathf.RoundToInt(projectileDamage + modifier)
        );

        baseMissileDamage = Mathf.Max(
           0,
           Mathf.RoundToInt(baseMissileDamage + modifier)
           );
        missileDamage = Mathf.Max(
            0,
            Mathf.RoundToInt(missileDamage + modifier)
            );
         

    }

    #region Path Variables
    // =========================================================
    // PATH
    // =========================================================

    private List<Vector3> currentPath;
    private int currentPathIndex;
    private bool movementPaused;

    public bool IsOnStairLink =>
        AStarManager.Instance != null &&
        AStarManager.Instance.GetStairLinkAtPosition(
            transform.position
        ) != null;

    public bool HasPath =>
        currentPath != null &&
        currentPathIndex < currentPath.Count;
    #endregion

    #region Death Variables
    // =========================================================
    // DEATH
    // =========================================================

    [Header("Death")]

    [Tooltip(
        "How long the enemy remains fully visible " +
        "before the fade begins."
    )]
    [SerializeField] private float deathAnimationDelay = 0.5f;

    [Tooltip(
        "How long the enemy takes to fade out " +
        "before being destroyed."
    )]
    [SerializeField] private float deathFadeDuration = 0.75f;

    public float DeathAnimationDelay =>
        deathAnimationDelay;

    public float DeathFadeDuration =>
        deathFadeDuration;
    #endregion

    #endregion



    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        enemyHealth =
            GetComponent<EnemyHealth>();

        enemyElevation =
            GetComponent<EnemyElevationLevel>();

        animator =
            GetComponent<Animator>();

        spawnPosition =
            transform.position;

        CacheBaseStats();



        // -----------------------------------------------------
        // FIND PLAYER
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
                $"[Lich_Miniboss] {name} could not find " +
                "a GameObject tagged 'Player'."
            );
        }
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
        Debug.Log(
                $"[Lich_Miniboss] {name} spawned In map " 
                
            );

        nextAttackTime = Time.time + attackCooldown;
        nextMissileTime = Time.time + missileCooldown;
        nextSummonTime = Time.time + summonCooldown;

        // -----------------------------------------------------
        // CHECK A* SPAWN TILE
        // -----------------------------------------------------

        if (AStarManager.Instance == null)
        {
            Debug.LogWarning(
                $"[Lich_Miniboss] {name} could not find " +
                "an AStarManager. Starting FSM without pathfinding."
            );
        }
        else if (!AStarManager.Instance.IsPositionWalkable(
                     transform.position,
                     ElevationLevel))
        {
            Debug.LogWarning(
                $"[EnemyLich] {name} spawned on a " +
                $"NON-WALKABLE A* tile at {transform.position}. " +
                "Starting FSM anyway."
            );
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
        // -----------------------------------------------------
        // FIND PLAYER IF MISSING
        // -----------------------------------------------------

        if (player == null)
        {
            GameObject playerObject =
                GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
            {
                player =
                    playerObject.transform;

                playerStats =
                    FindPlayerStats(playerObject);
            }
        }


        if (currentState == null)
            return;


        this.SetAnimatorBool(IsAttackingHash, false);



        // -----------------------------------------------------
        // FSM OWNS THE BEHAVIOUR
        // -----------------------------------------------------

        currentState.Tick();
    }


    private void LateUpdate()
    {
        this.UpdateAnimationDirection();
    }


    // =========================================================
    // PLAYER
    // =========================================================

    private PlayerStats FindPlayerStats(
        GameObject playerObject)
    {
        if (playerObject == null)
            return null;


        PlayerStats stats =
            playerObject.GetComponentInParent<PlayerStats>();

        if (stats == null)
        {
            stats =
                playerObject.GetComponentInChildren<PlayerStats>();
        }


        return stats;
    }


    // =========================================================
    // DAMAGE / DEATH
    // =========================================================

    private void HandleEnemyDied(
        GameObject deadEnemy)
    {
        ChangeState(
            EnemyState.Death
        );
    }


    private void HandleDamaged(
        EnemyHealth source,
        Vector3? damageSource)
    {
        NotifyDamaged(damageSource);
    }

    public void NotifyDamaged(Vector3? damageSource)
    {
        if (!damageSource.HasValue ||
            CurrentState == EnemyState.Death ||
            CurrentState == EnemyState.Chase)
        {
            return;
        }

        DamageSourcePosition = damageSource.Value;

        if (CurrentState == EnemyState.Locate)
        {
            EnemyLichLocateState locateState =
                currentState as EnemyLichLocateState;

            if (locateState != null)
                locateState.RefreshDestination();

            return;
        }

        ChangeState(EnemyState.Locate);
    }


    // =========================================================
    // STATE MACHINE
    // =========================================================

    public void ChangeState(
        EnemyState newState)
    {
        if (CurrentState.HasValue &&
            CurrentState.Value == newState &&
            currentState != null)
        {
            return;
        }

        if (currentState != null)
            currentState.Exit();


        takingAim = false;


        nextAttackTime = 0f;

        CurrentState =
            newState;


        currentState =
            CreateState(newState);


        if (currentState != null)
            currentState.Enter();


        Debug.Log(
            $"[Lich_Miniboss] {name} -> {newState}"
        );
    }


    private EnemyLichState CreateState(
        EnemyState state)
    {
        switch (state)
        {
            case EnemyState.Idle:
                return new EnemyLichIdleState(this);

            case EnemyState.Chase:
                return new EnemyLichChaseState(this);

            case EnemyState.Search:
                return new EnemyLichSearchState(this);

            case EnemyState.Locate:
                return new EnemyLichLocateState(this);

            case EnemyState.Death:
                return new EnemyLichDeathState(this);

            default:
                Debug.LogError(
                    $"[Lich_Miniboss] {name}: " +
                    $"Unknown state {state}."
                );

                return null;
        }
    }


    // =========================================================
    // DETECTION
    // =========================================================

    /// <summary>
    /// Checks whether the player is inside the enemy's
    /// A* detection radius.
    ///
    /// This is NOT the attack range.
    ///
    /// DetectionRadius is measured using the A* grid.
    /// </summary>
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


    // =========================================================
    // ATTACK RANGE
    // =========================================================

    /// <summary>
    /// Checks whether the player is inside the ranged
    /// enemy's attack distance.
    ///
    /// This intentionally uses the same world-space
    /// calculation as EnemyMelee.
    ///
    /// IMPORTANT:
    /// This method should only be called by the Chase state.
    /// </summary>
    public bool IsPlayerWithinAttackRange()
    {
        if (player == null)
            return false;


        float range =
            attackRange;


        return (
            (Vector2)player.position -
            (Vector2)transform.position
        ).sqrMagnitude <=
        range * range;
    }

    public void TrySpawnSummon()
    {
        summonList.RemoveAll(mobList => mobList == null);


        if (Time.time < nextSummonTime) // cooldowm timer
            return;

        if (summonList.Count == 3)  // summon limit cap
            return;

        if (CurrentState != EnemyState.Chase ||
           player == null ||
           !IsPlayerDetected() ||
           !IsPlayerWithinAttackRange())
            return;

        summonFirst = true; // prevent attacking when summoning

        this.SetAnimatorBool(IsAttackingHash, true); // same animation used

        //--------------------------------
        // Spawn Around the lich
        //--------------------------------

        Vector2 randomOffset = 
            Random.insideUnitCircle * spawnRadius; // spawn radius
        Vector2 spawnPos = 
            (Vector2)transform.position + randomOffset; // random location

        if (Physics2D.OverlapCircle(spawnPos, checkRadius, hittableLayers) == null)
        {

            //--------------------------------
            // Instantiation
            //--------------------------------

            GameObject mob =
                Instantiate(
                summonEnemy,            //prefab
                spawnPos,               //location
                Quaternion.identity,    //rotation
                transform.parent);      //place to same location of the parent in the hiearchy

            //--------------------------------
            // Elevation
            //--------------------------------

            EnemyElevationLevel mobElev =
                mob.GetComponent<EnemyElevationLevel>();

            mobElev.SetLevel(this.ElevationLevel); // place at same Elevation of Lich 


            //--------------------------------
            // Summon Mobs Cap
            //--------------------------------

            summonList.Add(mob);

            nextSummonTime =
                Time.time +
                summonCooldown;

            Debug.Log("[Lich_Miniboss] Summoned a Skelleton");

        }
        else {
                Debug.Log("[Lich_Miniboss] Summoned Skelleton failed to summon");
        }

            //--------------------------------
            // Cooldown reset
            //--------------------------------

            nextAttackTime =   // prevent to imediately fire attack at next frame
               Time.time +
               attackCooldown;

            nextMissileTime =  // also reset missile cooldown
                Time.time +
                missileCooldown;

       
    }
     

    // =========================================================
    // PROJECTILE ATTACK
    // =========================================================

    /// <summary>
    /// Fires a projectile at the player.
    ///
    /// This method does NOT decide whether the player is
    /// detected or in attack range.
    ///
    /// The Chase state is responsible for that decision.
    /// </summary>
    public bool TryShootProjectile()
    {
        if (CurrentState != EnemyState.Chase ||
            player == null ||
            !IsPlayerDetected() ||
            !IsPlayerWithinAttackRange())
            return false;

        if (summonFirst) // prevent attack after summoning 
        {
            summonFirst = false;
            return false;
        }

        if (projectilePrefab == null)
        {
            Debug.LogWarning(
                $"[Lich_Miniboss] {name}: " +
                "Cannot shoot - projectile prefab is missing."
            );

            return false;
        }

        if (Time.time < nextAttackTime)
            return false;

       
        if(Time.time >= nextMissileTime)
        {
            useMissileProjectile = true;      
        }
        


        this.SetAnimatorBool(IsAttackingHash, true);


        // -----------------------------------------------------
        // ATTACK POINT
        // -----------------------------------------------------

        Vector3 spawnPosition =
            attackPoint != null
                ? attackPoint.position
                : transform.position;


        // -----------------------------------------------------
        // DIRECTION
        // -----------------------------------------------------

        Vector3 direction =
            player.position -
            spawnPosition;


        // -----------------------------------------------------
        // CREATE PROJECTILE
        // -----------------------------------------------------

        GameObject projectileObject = useMissileProjectile
            ? Instantiate(
              missileProjectilePrefab,
              spawnPosition,
              Quaternion.identity
              )
            : Instantiate(
                projectilePrefab,
                spawnPosition,
                Quaternion.identity
            );
        
        IProjectileType projectile =
            projectileObject.GetComponent<IProjectileType>();


        if (projectile == null)
        {
            Debug.LogError(
                $"[Lich_Miniboss] {name}: " +
                $"Projectile prefab '{projectilePrefab.name}' " +
                "does not contain a BaseArrow component!"
            );


            Destroy(projectileObject);

            return false;
        }


        // -----------------------------------------------------
        // LAUNCH
        // -----------------------------------------------------
     
        int damageToUse = useMissileProjectile
            ? missileDamage
            : projectileDamage;

        DamageType damageTypeToUse = useMissileProjectile
            ? missileDamageType
            : projectileDamageType;

        float speedToUse = useMissileProjectile
            ? missileSpeed
            : projectileSpeed;

        float lifetimeToUse = useMissileProjectile
            ? missileLifetime
            : projectileLifetime;

        projectile.Launch(
            direction,
            damageToUse,
            damageTypeToUse,
            speedToUse,
            lifetimeToUse
        );


        // -----------------------------------------------------
        // COOLDOWN
        // -----------------------------------------------------

        nextAttackTime =
            Time.time +
            attackCooldown;

       
        if (useMissileProjectile)
        {
            nextMissileTime =
            Time.time +
            missileCooldown;


            useMissileProjectile = false;
        }

        

        return true;
    }


    // =========================================================
    // MOVEMENT
    // =========================================================

    public void StopMoving()
    {
        currentPath = null;
        currentPathIndex = 0;
        this.SetAnimatorBool(IsMovingHash, false);
    }


    public void PauseMovement(
        bool paused)
    {
        movementPaused = paused;

        if (paused)
            this.SetAnimatorBool(IsMovingHash, false);
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
        if (movementPaused ||
            !HasPath)
        {
            this.SetAnimatorBool(IsMovingHash, false);
            return;
        }


        this.SetAnimatorBool(IsMovingHash, true);


        Vector3 target =
            currentPath[currentPathIndex];


        transform.position =
            Vector3.MoveTowards(
                transform.position,
                target,
                moveSpeed *
                Time.deltaTime
            );


        if (Vector3.Distance(
                transform.position,
                target
            ) <= 0.01f)
        {
            transform.position =
                target;

            currentPathIndex++;
        }
    }


    public void SetTakingAim(bool isAiming)
    {
        takingAim = isAiming;
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
    // STATS
    // =========================================================

    public void ApplyLevelScaling(
        int level)
    {
        CacheBaseStats();

        level =
            Mathf.Max(
                1,
                level
            );

        moveSpeed =
            baseMoveSpeed +
            GetMovementSpeedBonus(
                level,
                0.005f
            );

        projectileDamage =
            baseProjectileDamage +
            Mathf.RoundToInt(
                GetScaledBonus(
                    level,
                    5f
                )
            );
        /*
         missileDamage =             
            baseMissileDamage +
            Mathf.RoundToInt(
                GetScaledBonus(
                    level,
                    5f
                )
            );
         
         
         
         */

    }



    private void CacheBaseStats()
    {
        if (baseStatsCached)
            return;


        baseStatsCached = true;

        baseMoveSpeed =
            moveSpeed;

        baseProjectileDamage =
            projectileDamage;

        baseMissileDamage =
           missileDamage;
    }


    private static float GetScaledBonus(
        int level,
        float perLevelBonus)
    {
        if (level <= 1)
            return 0f;


        float bonus = 0f;

        float specialBonus =
            perLevelBonus * 2f;

        float followUpBonus =
            Mathf.Ceil(
                specialBonus * 0.75f
            );


        for (
            int currentLevel = 2;
            currentLevel <= level;
            currentLevel++)
        {
            int levelInCycle =
                currentLevel % 5;


            if (levelInCycle == 0)
            {
                bonus += specialBonus;

                continue;
            }


            if (levelInCycle == 1)
            {
                bonus += followUpBonus;

                specialBonus =
                    followUpBonus * 2f;

                followUpBonus =
                    Mathf.Ceil(
                        specialBonus * 0.75f
                    );

                continue;
            }


            bonus +=
                perLevelBonus;
        }


        return bonus;
    }

    private static float GetMovementSpeedBonus(
        int level,
        float perLevelBonus)
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


    // =========================================================
    // DEBUG GIZMOS
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        if (AStarManager.Instance == null)
            return;


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


        int effectiveDetectionRadius =
            DetectionRadius;


        Gizmos.color =
            new Color(
                1f,
                1f,
                0f,
                0.25f
            );


        for (
            int x = -effectiveDetectionRadius;
            x <= effectiveDetectionRadius;
            x++)
        {
            for (
                int y = -effectiveDetectionRadius;
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


        // -----------------------------------------------------
        // ATTACK RANGE
        // -----------------------------------------------------

        Gizmos.color =
            new Color(
                1f,
                0f,
                0f,
                0.35f
            );


        Gizmos.DrawWireSphere(
            transform.position,
            attackRange
        );
    }







}
