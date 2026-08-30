using UnityEngine;

public class BaseArrow : MonoBehaviour
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