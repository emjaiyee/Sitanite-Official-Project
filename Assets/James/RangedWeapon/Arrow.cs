using UnityEngine;

public class Arrow : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private LayerMask hittableLayers;

    private int damage;
    private DamageType damageType;
    private float speed;
    private float maxDistance;

    private Vector3 startPosition;
    private bool initialized;

    public void Initialize(
        int damage,
        DamageType damageType,
        float speed,
        float maxDistance)
    {
        this.damage = damage;
        this.damageType = damageType;
        this.speed = speed;
        this.maxDistance = maxDistance;

        startPosition = transform.position;
        initialized = true;
    }

    private void Update()
    {
        if (!initialized)
            return;

        transform.position +=
            transform.right * speed * Time.deltaTime;

        float distanceTravelled =
            Vector3.Distance(
                startPosition,
                transform.position
            );

        if (distanceTravelled >= maxDistance)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check whether the object is on a valid hittable layer.
        if ((hittableLayers.value & (1 << other.gameObject.layer)) == 0)
        {
            return;
        }

        IDamageable target =
            other.GetComponentInParent<IDamageable>();

        if (target == null)
        {
            Debug.LogWarning(
                $"[Arrow] Hit {other.name}, but it has no IDamageable."
            );

            return;
        }

        target.TakeDamage(
            damage,
            damageType
        );

        Debug.Log(
            $"[Arrow] Hit {other.name} for " +
            $"{damage} {damageType} damage."
        );

        Destroy(gameObject);
    }
}