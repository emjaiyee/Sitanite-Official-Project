using UnityEngine;

public abstract class ProjectileBase : MonoBehaviour, IProjectile
{
    [Header("Damage")]
    [SerializeField] private LayerMask hittableLayers;

    private int damage;
    private DamageType damageType;
    private int secondaryDamage;
    private DamageType secondaryDamageType;
    private int tertiaryDamage;
    private DamageType tertiaryDamageType;
    private float speed;
    private float maxDistance;
    private bool homing;
    private Transform homingTarget;
    private Vector3 startPosition;
    private bool initialized;

    public void Initialize(
        int damage,
        DamageType damageType,
        float speed,
        float maxDistance,
        bool homing,
        LayerMask hittableLayers)
    {
        this.damage = damage;
        this.damageType = damageType;
        this.speed = speed;
        this.maxDistance = maxDistance;
        this.homing = homing;
        this.hittableLayers = hittableLayers;
        startPosition = transform.position;
        if (homing)
            homingTarget = FindNearestTarget();
        initialized = true;
    }

    public void InitializeSkill(
        int primaryDamage,
        DamageType primaryDamageType,
        int secondaryDamage,
        DamageType secondaryDamageType,
        int tertiaryDamage,
        DamageType tertiaryDamageType,
        float speed,
        float maxDistance,
        bool homing,
        LayerMask hittableLayers)
    {
        Initialize(
            primaryDamage,
            primaryDamageType,
            speed,
            maxDistance,
            homing,
            hittableLayers
        );
        this.secondaryDamage = secondaryDamage;
        this.secondaryDamageType = secondaryDamageType;
        this.tertiaryDamage = tertiaryDamage;
        this.tertiaryDamageType = tertiaryDamageType;
    }

    private void Update()
    {
        if (!initialized)
            return;

        if (homingTarget != null)
        {
            Vector2 direction = homingTarget.position - transform.position;
            if (direction.sqrMagnitude > 0.0001f)
                transform.right = direction.normalized;
        }

        transform.position += transform.right * speed * Time.deltaTime;

        if (Vector3.Distance(startPosition, transform.position) >= maxDistance)
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if ((hittableLayers.value & (1 << other.gameObject.layer)) == 0)
            return;

        IDamageable target = other.GetComponentInParent<EnemyHealth>();
        if (target == null)
            return;

        if (!PlayerElevationLevel.CanAffectTarget(
                (target as MonoBehaviour)?.transform))
            return;

        target.TakeDamage(damage, damageType);

        if (secondaryDamage > 0 && secondaryDamageType != DamageType.None)
            target.TakeDamage(secondaryDamage, secondaryDamageType);

        if (tertiaryDamage > 0 && tertiaryDamageType != DamageType.None)
            target.TakeDamage(tertiaryDamage, tertiaryDamageType);

        Destroy(gameObject);
    }

    private Transform FindNearestTarget()
    {
        Collider2D[] candidates = Physics2D.OverlapCircleAll(
            startPosition,
            maxDistance,
            hittableLayers
        );
        Transform nearestTarget = null;
        float nearestDistance = float.PositiveInfinity;

        foreach (Collider2D candidate in candidates)
        {
            IDamageable target = candidate == null
                ? null
                : candidate.GetComponentInParent<EnemyHealth>();
            MonoBehaviour targetBehaviour = target as MonoBehaviour;
            if (targetBehaviour == null)
                continue;

            if (!PlayerElevationLevel.CanAffectTarget(
                    targetBehaviour.transform))
                continue;

            float distance = (targetBehaviour.transform.position - startPosition)
                .sqrMagnitude;
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestTarget = targetBehaviour.transform;
            }
        }

        return nearestTarget;
    }
}