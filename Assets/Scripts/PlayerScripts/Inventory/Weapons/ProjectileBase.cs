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

    /// <summary>
    /// Time.time when this projectile was initialized.
    /// </summary>
    protected float StartTime { get; private set; }

    /// <summary>
    /// Determines whether this projectile should directly damage
    /// the target it collides with.
    ///
    /// Normal projectiles return true.
    /// Explosive projectiles can override this and return false
    /// so they only deal damage through their explosion.
    /// </summary>
    protected virtual bool ShouldDealDirectDamage => true;

    /// <summary>
    /// Called at the end of Initialize.
    /// Use this to cache visual components or initialize visuals.
    /// </summary>
    protected virtual void OnInitialized()
    {
    }

    /// <summary>
    /// Called every Update after projectile movement,
    /// while the projectile is initialized.
    /// </summary>
    protected virtual void OnUpdate()
    {
    }

    /// <summary>
    /// Called when the projectile successfully hits a valid target.
    /// Specialized projectiles can override this for explosions,
    /// impact effects, etc.
    /// </summary>
    protected virtual void OnImpact(Vector3 impactPosition)
    {
    }

    /// <summary>
    /// Called when the projectile reaches its maximum distance.
    /// Specialized projectiles can override this for explosions,
    /// area effects, etc.
    /// </summary>
    protected virtual void OnMaxRangeReached()
    {
    }

    private void Awake()
    {
    }

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

        StartTime = Time.time;

        if (homing)
            homingTarget = FindNearestTarget();

        initialized = true;

        OnInitialized();
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

        // Homing
        if (homingTarget != null)
        {
            Vector2 direction =
                homingTarget.position - transform.position;

            if (direction.sqrMagnitude > 0.0001f)
            {
                transform.right = direction.normalized;
            }
        }

        // Movement
        transform.position +=
            transform.right * speed * Time.deltaTime;

        // Maximum distance
        if (Vector3.Distance(
                startPosition,
                transform.position) >= maxDistance)
        {
            OnMaxRangeReached();

            Destroy(gameObject);

            return;
        }

        // Custom projectile behavior
        OnUpdate();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check layer
        if ((hittableLayers.value &
             (1 << other.gameObject.layer)) == 0)
        {
            return;
        }

        // Find damageable target
        IDamageable target =
            other.GetComponentInParent<EnemyHealth>();

        if (target == null)
            return;

        // Check elevation
        if (!PlayerElevationLevel.CanAffectTarget(
                (target as MonoBehaviour)?.transform))
        {
            return;
        }

        // Normal projectile damage
        if (ShouldDealDirectDamage)
        {
            target.TakeDamage(
                damage,
                damageType,
                startPosition
            );

            if (secondaryDamage > 0 &&
                secondaryDamageType != DamageType.None)
            {
                target.TakeDamage(
                    secondaryDamage,
                    secondaryDamageType,
                    startPosition
                );
            }

            if (tertiaryDamage > 0 &&
                tertiaryDamageType != DamageType.None)
            {
                target.TakeDamage(
                    tertiaryDamage,
                    tertiaryDamageType,
                    startPosition
                );
            }
        }

        // Specialized projectile behavior
        OnImpact(transform.position);

        Destroy(gameObject);
    }

    /// <summary>
    /// Returns the primary damage configured during initialization.
    /// </summary>
    protected int GetPrimaryDamage()
    {
        return damage;
    }

    /// <summary>
    /// Returns the primary damage type configured during initialization.
    /// </summary>
    protected DamageType GetPrimaryDamageType()
    {
        return damageType;
    }

    /// <summary>
    /// Returns the secondary damage configured during initialization.
    /// </summary>
    protected int GetSecondaryDamage()
    {
        return secondaryDamage;
    }

    /// <summary>
    /// Returns the secondary damage type configured during initialization.
    /// </summary>
    protected DamageType GetSecondaryDamageType()
    {
        return secondaryDamageType;
    }

    /// <summary>
    /// Returns the tertiary damage configured during initialization.
    /// </summary>
    protected int GetTertiaryDamage()
    {
        return tertiaryDamage;
    }

    /// <summary>
    /// Returns the tertiary damage type configured during initialization.
    /// </summary>
    protected DamageType GetTertiaryDamageType()
    {
        return tertiaryDamageType;
    }

    /// <summary>
    /// Returns the layer mask configured during initialization.
    /// </summary>
    protected LayerMask GetHittableLayers()
    {
        return hittableLayers;
    }

    /// <summary>
    /// Finds the nearest valid target within projectile range.
    /// </summary>
    private Transform FindNearestTarget()
    {
        Collider2D[] candidates =
            Physics2D.OverlapCircleAll(
                startPosition,
                maxDistance,
                hittableLayers
            );

        Transform nearestTarget = null;

        float nearestDistance =
            float.PositiveInfinity;

        foreach (Collider2D candidate in candidates)
        {
            IDamageable target =
                candidate == null
                    ? null
                    : candidate.GetComponentInParent<EnemyHealth>();

            MonoBehaviour targetBehaviour =
                target as MonoBehaviour;

            if (targetBehaviour == null)
                continue;

            if (!PlayerElevationLevel.CanAffectTarget(
                    targetBehaviour.transform))
            {
                continue;
            }

            float distance =
                (targetBehaviour.transform.position -
                 startPosition).sqrMagnitude;

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestTarget = targetBehaviour.transform;
            }
        }

        return nearestTarget;
    }
}