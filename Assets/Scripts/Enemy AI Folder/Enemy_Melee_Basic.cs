using UnityEngine;

public class Enemy_Melee_Basic : MonoBehaviour
{
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

    [Header("Bolean Variables")]
    [SerializeField] private bool _detected = false;
    [SerializeField] private bool _outsideZone = false;
    [SerializeField] private bool _attackable = false;


    void Start()
    {
        _player = GameObject.FindGameObjectWithTag("Player").transform;
        _rb = GetComponent<Rigidbody2D>();
        _enemySpawnPoint = transform.position;
        _color = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        _playerDistance = Vector2.Distance(transform.position, _player.position);
        _detected = (_playerDistance < _detectionRange); //Detect Player Location if Within range to start following 
        AttackDistance(); // Detect Player Location if Within range to start Attacking  
        EnemyZoneManager(); // Detect if Enemy AI is inside his Area/Zone.
        PlayerDirection(); // Always Checks Player location to know and get the direction.

        
    }

    private void FixedUpdate()
    {
        if (_attackable) {Debug.Log("Swing"); _color.color = Color.red; return;}
        if (_detected)
        {
            _rb.MovePosition(_rb.position + _movement * _enemySpeed * Time.fixedDeltaTime);
            _color.color = Color.yellow;
        }
        if (!_detected && _outsideZone)
        {
           transform.position = Vector2.MoveTowards(transform.position, _enemySpawnPoint, _enemySpeed * Time.deltaTime);
        }
    }

    private void AttackDistance()
    {
        if (_playerDistance <= 1)
        { 
            Debug.Log("Attack");
            _attackable = true;
        }
        else
        {
            _attackable = false;
        }



    }

    private void EnemyZoneManager()
    {
        float _ZoneDistance = Vector2.Distance(transform.position, _enemySpawnPoint);
            if (_ZoneDistance > _enemyZoneRange) { _outsideZone = true; }
            if (_ZoneDistance == 0) { _outsideZone = false; }
    }
    private void PlayerDirection()
    {
        Vector2 _playerDirection = (_player.position - transform.position).normalized;
        _movement = _playerDirection;
    }


}
