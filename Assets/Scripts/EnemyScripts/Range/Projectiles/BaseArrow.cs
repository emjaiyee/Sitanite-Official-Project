using System.Collections.Generic;
using UnityEngine;

public class BaseArrow : MonoBehaviour, IProjectileType
{
    [Header("Collision")]
    [SerializeField] private LayerMask hittableLayers = Physics2D.AllLayers;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private bool logMovement = false;

    private int damage;
    private DamageType damageType;
    private float speed;
    private float lifetime;
    private Vector3 direction;
    private Vector3 startPosition;
    private float destroyTime;
    private bool initialized;
    private bool hasHitPlayer;
    private List<DungeonMemory.DamageStat> enemyHits;
    private int enemyElevation;
    private bool enemyHoming;

    public void LaunchForEnemy(Vector3 direction, IReadOnlyList<DungeonMemory.DamageStat> hits, float speed, float range, bool homing, int elevation)
    {
        enemyHits = new List<DungeonMemory.DamageStat>(hits);
        enemyElevation = elevation;
        enemyHoming = homing;
        hittableLayers = LayerMask.GetMask("Player");
        hasHitPlayer = false;
        Launch(direction, 0, DamageType.None, speed, range / Mathf.Max(0.01f, speed));
    }

    public void Launch(
        Vector3 direction,
        int damage,
        DamageType damageType,
        float speed,
        float lifetime)
    {
        this.direction = direction.sqrMagnitude > 0.0001f
            ? direction.normalized
            : Vector3.right;

        this.damage = Mathf.Max(0, damage);
        this.damageType = damageType;
        this.speed = Mathf.Max(0.01f, speed);
        this.lifetime = Mathf.Max(0.01f, lifetime);

        startPosition = transform.position;
        destroyTime = Time.time + this.lifetime;

        transform.right = this.direction;

        initialized = true;

        if (enableDebugLogs)
        {
            Debug.Log(
                $"[BaseArrow] FIRED | " +
                $"Object: {name} | " +
                $"Position: {transform.position} | " +
                $"Direction: {this.direction} | " +
                $"Speed: {this.speed} | " +
                $"Lifetime: {this.lifetime}",
                this
            );
        }
    }

    private void Update()
    {
        if (!initialized)
            return;

        Vector3 previousPosition = transform.position;

        if (enemyHoming && Player.Instance != null)
        {
            direction = (Player.Instance.transform.position - transform.position).normalized;
            transform.right = direction;
        }

        transform.position +=
            direction * speed * Time.deltaTime;

        if (logMovement)
        {
            Debug.Log(
                $"[BaseArrow] MOVING | " +
                $"{previousPosition} -> {transform.position}",
                this
            );
        }

        if (Time.time >= destroyTime)
        {
            if (enableDebugLogs)
            {
                Debug.Log(
                    $"[BaseArrow] DESTROYED | " +
                    $"Lifetime expired after {lifetime:F2}s | " +
                    $"Final Position: {transform.position}",
                    this
                );
            }

            Destroy(gameObject);
            return;
        }

        if (Vector3.Distance(
                startPosition,
                transform.position) >= lifetime * speed)
        {
            if (enableDebugLogs)
            {
                Debug.Log(
                    $"[BaseArrow] DESTROYED | " +
                    $"Maximum travel distance reached | " +
                    $"Final Position: {transform.position}",
                    this
                );
            }

            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D body)
    {
        if (body == null)
            return;

        if (enemyHits != null)
        {
            PlayerStats player = body.GetComponentInParent<PlayerStats>();
            if (hasHitPlayer || (hittableLayers.value & (1 << body.gameObject.layer)) == 0 ||
                !BossBalance.CanHitPlayer(player, enemyElevation))
                return;

            hasHitPlayer = true;
            BossBalance.HitPlayer(player, enemyHits, enemyElevation);
            Destroy(gameObject);
            return;
        }

        string objectLayer =
            LayerMask.LayerToName(body.gameObject.layer);

        bool isHittable =
            (hittableLayers.value &
             (1 << body.gameObject.layer)) != 0;

        if (enableDebugLogs)
        {
            Debug.Log(
                $"[BaseArrow] COLLISION/TRIGGER | " +
                $"Arrow: {name} | " +
                $"Hit: {body.name} | " +
                $"Tag: {body.tag} | " +
                $"Layer: {objectLayer} | " +
                $"Hittable Layer: {isHittable}",
                this
            );
        }

        if (!isHittable)
        {
            if (enableDebugLogs)
            {
                Debug.Log(
                    $"[BaseArrow] IGNORED COLLISION | " +
                    $"{body.name} is NOT on a hittable layer.",
                    this
                );
            }

            return;
        }

        if (!body.CompareTag("Player"))
        {
            if (enableDebugLogs)
            {
                Debug.Log(
                    $"[BaseArrow] IGNORED COLLISION | " +
                    $"{body.name} is hittable but is NOT tagged Player.",
                    this
                );
            }

            return;
        }

        PlayerStats playerStats =
            body.GetComponentInParent<PlayerStats>();

        if (playerStats == null)
        {
            playerStats =
                body.GetComponentInChildren<PlayerStats>();
        }

        if (playerStats == null)
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning(
                    $"[BaseArrow] PLAYER HIT BUT NO PlayerStats FOUND | " +
                    $"Object: {body.name}",
                    this
                );
            }

            return;
        }

        if (playerStats.IsDead)
        {
            if (enableDebugLogs)
            {
                Debug.Log(
                    $"[BaseArrow] PLAYER HIT BUT PLAYER IS DEAD | " +
                    $"Object: {body.name}",
                    this
                );
            }

            return;
        }

        if (damage <= 0)
        {
            if (enableDebugLogs)
            {
                Debug.Log(
                    $"[BaseArrow] PLAYER HIT BUT DAMAGE IS 0 | " +
                    $"Object: {body.name}",
                    this
                );
            }

            return;
        }

        if (hasHitPlayer)
            return;

        hasHitPlayer = true;

        if (enableDebugLogs)
        {
            Debug.Log(
                $"[BaseArrow] *** PLAYER HIT! *** | " +
                $"Target: {body.name} | " +
                $"Damage: {damage} | " +
                $"Damage Type: {damageType}",
                this
            );
        }

        playerStats.TakeDamage(
            damage,
            damageType);

        if (enableDebugLogs)
        {
            Debug.Log(
                $"[BaseArrow] DESTROYED | " +
                $"Successfully damaged player.",
                this
            );
        }

        Destroy(gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision == null)
            return;

        if (enableDebugLogs)
        {
            Debug.Log(
                $"[BaseArrow] COLLISION ENTER | " +
                $"Arrow: {name} | " +
                $"Hit: {collision.gameObject.name}",
                this
            );
        }

        OnTriggerEnter2D(collision.collider);
    }
}