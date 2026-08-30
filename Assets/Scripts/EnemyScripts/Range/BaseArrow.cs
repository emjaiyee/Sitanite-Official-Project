using UnityEngine;

public class BaseArrow : MonoBehaviour
{
    [SerializeField] private LayerMask hittableLayers = Physics2D.AllLayers;

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
    }

    private void Update()
    {
        if (!initialized)
            return;

        transform.position += direction * speed * Time.deltaTime;

        if (Time.time >= destroyTime ||
            Vector3.Distance(startPosition, transform.position) >= lifetime * speed)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D body)
    {
        if ((hittableLayers.value & (1 << body.gameObject.layer)) == 0)
            return;

        if (!body.CompareTag("Player"))
            return;

        PlayerStats playerStats = body.GetComponentInParent<PlayerStats>();
        if (playerStats == null)
            playerStats = body.GetComponentInChildren<PlayerStats>();

        if (playerStats == null || playerStats.IsDead || damage <= 0)
            return;

        playerStats.TakeDamage(damage, damageType);
        Destroy(gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        OnTriggerEnter2D(collision.collider);
    }
}