using UnityEngine;
using System.Collections;

public class Enemy_Melee_FSM : MonoBehaviour
{

    public enum EnemyState { Idle, Chase, Attack, Return, Death }
    public EnemyState currentState = EnemyState.Idle;

    [Header("Enemy Variable")]
    public float _detectionRange = 3f;
    public float _enemySpeed = 1f;
    public float _enemyZoneRange = 5f;
    public float _playerDistance;

    [SerializeField] private Transform _player;
    [SerializeField] private Rigidbody2D _rb;
    [SerializeField] private Vector2 _movement;
    [SerializeField] private Vector2 _enemySpawnPoint;
    [SerializeField] private SpriteRenderer _color;

    private bool _zoneBase;




    void Start()
    {
        _player = GameObject.FindGameObjectWithTag("Player").transform;
        _rb = GetComponent<Rigidbody2D>();
        _enemySpawnPoint = transform.position;
        _color = GetComponent<SpriteRenderer>();
    }

    void Update()
    {

        float _zoneDistance = Vector2.Distance(transform.position, _enemySpawnPoint);
        _zoneBase = (_zoneDistance != 0);



        switch (currentState)
        { 
            case EnemyState.Idle:
                IdleStateFunction();
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

       
    }

    private void IdleStateFunction() 
    {
        // Wait for player to enter detection range
        //Play Unique Idle Animation "Not moving already starts the idle State".
        if (_zoneBase == false) return;
        StartCoroutine(IdleStateCoroutine());
    }
    private void ChaseStateFunction() { }
    private void AttackStateFunction() { }
    private void ReturnStateFunction() 
    {
        transform.position = Vector2.MoveTowards(transform.position, _enemySpawnPoint, _enemySpeed * Time.deltaTime);
        if (_zoneBase == true) return;

        currentState = EnemyState.Idle;
    }
    private void DeathStateFunction() { }


    #region -------- Idle State Coroutine ----------
    IEnumerator IdleStateCoroutine() 
    {
        Debug.Log("Idle State");

        yield return new WaitForSeconds(5f);

        Debug.Log("Idle State Coroutine Finished");
        currentState = EnemyState.Return;
    }
    #endregion

}
