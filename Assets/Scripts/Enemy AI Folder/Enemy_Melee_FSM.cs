using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Runtime.CompilerServices;

public class Enemy_Melee_FSM : MonoBehaviour
{

    public enum EnemyState { Idle, Chase, Search, Attack, Return, Death }
    public EnemyState currentState = EnemyState.Idle;

    [Header("Enemy Variable")]
    public float _detectionRange = 3f;  // Enemy Detection Range 
    public float _enemySpeed = 2f;      // Enemy Movement Speed
    public float _enemyAttackRange = 1f;      // Enemy Movement Speed
    public float _enemyHealth = 100f;     // Enemy Health 

    [SerializeField] private Transform _player; //Tar 
    [SerializeField] private Rigidbody2D _rb;
    [SerializeField] private Vector2 _movement;
    [SerializeField] private Vector2 _enemySpawnPoint;
    [SerializeField] private NavMeshAgent _agent;
    [SerializeField] private SpriteRenderer _color;

    private float _zoneDistance;
    private float _playerDistance;
    private bool _playerDetected;
    [SerializeField]private bool _atAttackRange;
    private bool _Searching;
    private bool _zoneBase;


    private void Awake()
    {
        
        _player = GameObject.FindGameObjectWithTag("Player").transform;
        _agent = GetComponent<NavMeshAgent>();
        _rb = GetComponent<Rigidbody2D>();
        _color = GetComponent<SpriteRenderer>();

    }

    void Start()
    {
        _enemySpawnPoint = transform.position;
        _agent.updateRotation = false;
        _agent.updateUpAxis = false;
        _agent.speed = _enemySpeed;
    }

    void Update()
    {

        _zoneDistance = Vector2.Distance(transform.position, _enemySpawnPoint);
        _zoneBase = (_zoneDistance < 0.1f);
       



        switch (currentState)
        { 
            case EnemyState.Idle:
                IdleStateFunction();
                break;

            case EnemyState.Search:
                SearchStateFuction();
                break;

            case EnemyState.Chase:
                ChaseStateFunction();
                break;

            case EnemyState.Attack:
                AttackStateFunction();
                break;

            case EnemyState.Return:
                ReturnStateFunction();
                break;

            case EnemyState.Death:
                DeathStateFunction();
                break;
        }

       PlayerDetectionFunction();
    }





    #region ------- Enemy State Functions ----------
    private void IdleStateFunction() 
    {
        // Wait for player to enter detection range
        //Play Unique Idle Animation "Not moving already starts the idle State".
        Debug.Log("Idle State Function");

    }

    private void ChaseStateFunction() 
    {
        Debug.Log("Chase State Function");
        _agent.isStopped = false;
        _agent.SetDestination(_player.position);
        if (_playerDetected == false)
        {
            _agent.isStopped = true;
            currentState = EnemyState.Search;
        }
        if (_atAttackRange) 
        {
            _agent.isStopped = true;
            currentState = EnemyState.Attack; 
        }
    }

    private void AttackStateFunction() 
    {
        //Trigger Attack Animation
        Debug.Log("Attack State Function");
    
        TestAttackFunction();

        if (!_atAttackRange)
        {
            _agent.isStopped = false;
            _color.color = Color.yellow;
            currentState = EnemyState.Chase;
        }
    }

    private void SearchStateFuction()
    {
        Debug.Log("Search State Function");
        if (!_playerDetected && !_zoneBase && !_Searching)
        {
            StartCoroutine(SearchStateCoroutine());
        }
    }

    private void ReturnStateFunction() 
    {
        Debug.Log("Return State Function");

        _agent.isStopped = false;
        _agent.SetDestination(_enemySpawnPoint);
        if (_zoneBase == false) return;

        currentState = EnemyState.Idle;
    }

    private void DeathStateFunction() 
    {
        // Initiate Enemy Death Animation
        // Initiate Loot Drop 
        // Destroy Enemy Object/Sprite   
    }
    #endregion

    private void PlayerDetectionFunction()
    {
        //Detect if Enemy is Dead (Enter Death State)
        if (_enemyHealth <= 0f) 
        { 
            _agent.isStopped = true;
            currentState = EnemyState.Death; 
            return; 
        }
      

        _playerDistance = Vector2.Distance(transform.position, _player.position);

        //Detect if Player is within Attack Range (Attack True)
        _atAttackRange = (_playerDistance <= _enemyAttackRange);
        if (currentState == EnemyState.Attack) return;

        //Detect if Player is within Detection Range (Enter Chase State)
        _playerDetected = (_playerDistance < _detectionRange);
        if (_playerDetected)
        {
            currentState = EnemyState.Chase;
        }
 
    }

    private void TestAttackFunction()
    {
        _color.color = Color.red;
    }

    #region -------- Idle State Coroutine ----------
    IEnumerator SearchStateCoroutine()
    {
        _Searching = true;
        yield return new WaitForSeconds(5f);

        Debug.Log("Idle State Coroutine Finished");
        _Searching = false;
        currentState = EnemyState.Return;
    }
    #endregion

}
