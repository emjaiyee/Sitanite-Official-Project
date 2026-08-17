using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(EnemyHealth))]
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyRange : MonoBehaviour
{
    //==========================================================
    // STATES 
    //==========================================================
    public enum EnemyState
    {
        Idle,
        Chase,
        Search,
        Attack,
        Return,
        Death
    }

    [Header("State")]
    [SerializeField] private EnemyState startingState =
            EnemyState.Idle;

    public EnemyState CurrentState { get; private set; }



    // =========================================================
    // DETECTION
    // =========================================================

    [Header("Detection")]
    [SerializeField] private float detectionRange = 12f;

    public float DetectionRange =>
        detectionRange;


    // =========================================================
    // MOVEMENT
    // =========================================================

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;

    public float MoveSpeed =>
        moveSpeed;


    // =========================================================
    // COMBAT
    // =========================================================

    [Header("Combat")]
    [SerializeField] private float attackRange = 10f;

    public float AttackRange =>
        attackRange;


    // =========================================================
    // RETURN
    // =========================================================

    [Header("Return")]
    [SerializeField] private float returnThreshold = 0.1f;

    public float ReturnThreshold =>
        returnThreshold;

    // =========================================================
    // REFERENCES   
    // =========================================================

    private EnemyHealth enemyHealth;
    private NavMeshAgent agent;
    private Transform player;

    private Vector3 spawnPosition;


    public EnemyHealth Health =>
        enemyHealth;

    public NavMeshAgent Agent =>
        agent;

    public Transform Player =>
        player;

    public Vector3 SpawnPosition =>
        spawnPosition;

    // =========================================================
    // STATE INSTANCE
    // =========================================================

    private EnemyRangeState currentState;

    // =========================================================
    // Unity
    // =========================================================

    private void Awake()
    {
        enemyHealth = 
            GetComponent<EnemyHealth>();

        agent = 
            GetComponent<NavMeshAgent>();

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

        // -----------------------------------------------------
        // NAVMESH AGENT
        // -----------------------------------------------------

        ConfigureAgent();
    }



    private void OnEnable()
    {
        if (enemyHealth != null)
        {
            enemyHealth.OnEnemyDied += HandleEnemyDied;   
        }
    }

    private void OnDisable()
    {
        if (enemyHealth != null)
        {
            enemyHealth.OnEnemyDied -= HandleEnemyDied;
        }
    }

    private void Start()
    {
        ChangeState(startingState);
    }

    private void Update()
    {
        if (currentState == null)
            return;

        currentState.Tick();
    }

    // =========================================================
    // STATE MANAGEMENT
    // =========================================================

    public void ChangeState(
        EnemyState newState)
    {
        if (CurrentState == newState)
            return;

        if (currentState != null)
        {
            currentState.Exit();
        }

        CurrentState =
            newState;

        currentState =
            CreateState(newState);

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
        //        return new EnemyMeleeIdleState(this);

            case EnemyState.Chase:
        //        return new EnemyMeleeChaseState(this);

            case EnemyState.Search:
        //        return new EnemyMeleeSearchState(this);

            case EnemyState.Attack:
        //        return new EnemyMeleeAttackState(this);

            case EnemyState.Return:
        //        return new EnemyMeleeReturnState(this);

            case EnemyState.Death:
        //        return new EnemyMeleeDeathState(this);
        default:
                Debug.LogWarning(
                    $"[EnemyMelee] {name} has no state " +
                    $"implementation for {state}."
                );
                break;
        }

        return null;
    }


    // =========================================================
    // ENEMY DEATH
    // =========================================================

    private void HandleEnemyDied(
        GameObject deadEnemy)
    {
        if (CurrentState == EnemyState.Death)
            return;

        ChangeState(
            EnemyState.Death
        );
    }



    // =========================================================
    // NAVMESH CONFIGURATION
    // =========================================================

    private void ConfigureAgent()
    {
        if (agent == null)
            return;

        // We are using this as a 2D NavMesh agent.
        agent.updateRotation = false;
        agent.updateUpAxis = false;

        agent.speed =
            moveSpeed;
    }



    // =========================================================
    // COMMON ENEMY QUERIES
    // =========================================================

    public float DistanceToPlayer()
    {
        if (player == null)
            return float.MaxValue;

        return Vector2.Distance(
            transform.position,
            player.position
        );
    }


    public bool IsPlayerDetected()
    {
        return DistanceToPlayer()
            <= detectionRange;
    }


    public bool IsPlayerInAttackRange()
    {
        return DistanceToPlayer()
            <= attackRange;
    }


    public float DistanceFromSpawn()
    {
        return Vector2.Distance(
            transform.position,
            spawnPosition
        );
    }


    public bool IsAtSpawn()
    {
        return DistanceFromSpawn()
            <= returnThreshold;
    }


    // =========================================================
    // HEALTH
    // =========================================================

    public bool IsDead()
    {
        if (enemyHealth == null)
            return false;

        return enemyHealth.CurrentHealth <= 0;
    }

    // =========================================================
    // MOVEMENT HELPERS
    // =========================================================

    public void StopMoving()
    {
        if (agent == null)
            return;

        agent.isStopped = true;
        agent.ResetPath();
    }


    public void ResumeMoving()
    {
        if (agent == null)
            return;

        agent.isStopped = false;
    }


    public void MoveTo(
        Vector3 destination)
    {
        if (agent == null)
            return;

        if (!agent.isOnNavMesh)
            return;

        agent.isStopped = false;
        agent.SetDestination(destination);
    }


    // =========================================================
    // DEBUG
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        // Detection range
        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(
            transform.position,
            detectionRange
        );


        // Attack range
        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(
            transform.position,
            attackRange
        );


        // Spawn position
        if (Application.isPlaying)
        {
            Gizmos.color = Color.blue;

            Gizmos.DrawWireSphere(
                spawnPosition,
                returnThreshold
            );
        }
    }
}
















