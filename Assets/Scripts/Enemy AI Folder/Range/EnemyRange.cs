using NUnit.Framework.Constraints;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(EnemyHealth))]
public class EnemyRange : MonoBehaviour
{
    #region Variables 
    //==========================================================
    // STATES 
    //==========================================================


    public enum EnemyState
    {
        Idle,
        Chase,
        Search, 
        Death
    }


    [Header("State")]
    [SerializeField] private EnemyState startingState =
            EnemyState.Idle;


    public EnemyState? CurrentState { get; private set; }



    // =========================================================
    // STATE INSTANCE
    // =========================================================

    private EnemyRangeState currentState;



    // =========================================================
    // DETECTION
    // =========================================================

    [Header("Detection")]
    [Tooltip("Detection distance measured in A* grid cells.")]
    [SerializeField] private int detectionRadius = 12;

    public int DetectionRadius =>
        detectionRadius;

    [Header("Attack Detection")]
    [Tooltip("Attack Detection distance measured in A* grid cells.")]
    [SerializeField] private int attackDetectionRadius = 10;

    public int AttackDetectionRadius =>
        attackDetectionRadius;


    // =========================================================
    // MOVEMENT
    // =========================================================

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;

    public float MoveSpeed =>
        moveSpeed;


    [SerializeField] private int idleWanderRadius = 4;

    public int IdleWanderRadius =>
        idleWanderRadius;



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
    private Transform player;


    private Vector3 spawnPosition;

    public EnemyHealth Health =>
        enemyHealth;

    public Transform Player =>
        player;

    public Vector3 SpawnPosition =>
        spawnPosition;

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

    #endregion

    #region Unity Methods

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {

        enemyHealth =
            GetComponent<EnemyHealth>();

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
        }
        else
        {
            Debug.LogWarning(
                $"[EnemyMelee] {name} could not find " +
                "a GameObject tagged 'Player'."
            );
        }
    }


    private void OnEnable()
    {
        if (enemyHealth != null)
        {
            enemyHealth.OnEnemyDied += HandleEnemyDied;
        }
    }


    private void Start()
    {
        // -----------------------------------------------------
        // CHECK A* SPAWN TILE
        // -----------------------------------------------------

        if (AStarManager.Instance == null)
        {
            Debug.LogError(
                $"[EnemyMelee] {name} could not find " +
                "an AStarManager."
            );

            return;
        }


        if (!AStarManager.Instance.IsPositionWalkable(
                transform.position))
        {
            Debug.LogError(
                $"[EnemyMelee] {name} spawned on a " +
                $"NON-WALKABLE A* tile at " +
                $"{transform.position}."
            );

            return;
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
        }
    }


    private void Update()
    {
       
        if (currentState == null)
            return;

        currentState.Tick();
    }

    #endregion

    #region State Management

    // =========================================================
    // ENEMY DEATH
    // =========================================================

    private void HandleEnemyDied(
    GameObject deadEnemy)
    {
        ChangeState(
            EnemyState.Death
        );
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

    private EnemyRangeState CreateState(
    EnemyState state)
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
                Debug.LogError(
                    $"[EnemyMelee] {name}: " +
                    $"Unknown state {state}."
                );

                return null;
        }
    }
    #endregion

    #region Functions

    // =========================================================
    // DETECTION
    // =========================================================

    public bool IsPlayerDetected()
    {
        if (player == null)
            return false;


        if (AStarManager.Instance == null)
            return false;


        return AStarManager.Instance
            .IsPositionWithinDetectionRadius(
                transform.position,
                player.position,
                detectionRadius
            );
    }

    public bool IsPlayerWithinAttackRange()
    {
        if (player == null)
            return false;


        if (AStarManager.Instance == null)
            return false;


        return AStarManager.Instance
            .IsPositionWithinDetectionRadius(
                transform.position,
                player.position,
                attackDetectionRadius
            );

    }

    public bool IsPlayerRayCasted()
    {
        if (player == null)
            return false;

        int layerMask = LayerMask.GetMask("Default");

        RaycastHit2D hit =
                       Physics2D.Raycast(
                transform.position,
                player.position - transform.position,
                Mathf.Infinity,
                layerMask
            );

        if (hit.collider.CompareTag("Player"))
        {
            Debug.Log($"[EnemyRayCast] {name} Raycast hit: {hit.collider.name}");
            return true;
        }
        return false;

    }


    // =========================================================
    // MOVEMENT
    // =========================================================

    public void StopMoving()
    {
        currentPath = null;
        currentPathIndex = 0;
        movementPaused = false;
    }


    public void PauseMovement(bool paused)
    {
        movementPaused = paused;
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
            return;

        if (!HasPath)
            return;

        Vector3 target =
            currentPath[currentPathIndex];

        Debug.Log(
            $"[PATH] {name} moving toward " +
            $"index {currentPathIndex}/{currentPath.Count} " +
            $"target={target} " +
            $"current={transform.position}"
        );

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

            Debug.Log(
                $"[PATH] {name} reached waypoint. " +
                $"Next index = {currentPathIndex}"
            );
        }
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

        Debug.DrawRay(transform.position, player.position - transform.position, Color.green);


        Gizmos.color =
            new Color(
                1f,
                1f,
                0f,
                0.25f
            );

        if (IsPlayerRayCasted())
        {
            Gizmos.color =
                new Color(
                    1f,
                    0f,
                    0f,
                    0.25f
                    );
        }


        for (int x = -detectionRadius;
            x <= detectionRadius;
            x++)
        {
            for (int y = -detectionRadius;
                y <= detectionRadius;
                y++)
            {
                int squaredDistance =
                    x * x +
                    y * y;


                int squaredRadius =
                    detectionRadius *
                    detectionRadius;


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
    #endregion


}
