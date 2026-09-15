
using UnityEngine;

public class CrossbowExplosiveArrow : ProjectileBase
{
    [Header("Explosion")]
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private float explosionVisualDuration = 0.5f;

    private float explosionRadius;

    private GameObject fireAreaPrefab;
    private float fireDuration;
    private float fireTickInterval;
    private int fireDamage;
    private float fireRadius;

    private bool detonated;

    public void InitializeExplosive(
    int primaryDamage,
    DamageType primaryDamageType,
    int secondaryDamage,
    DamageType secondaryDamageType,
    int tertiaryDamage,
    DamageType tertiaryDamageType,
    float speed,
    float maxDistance,
    bool homing,
    LayerMask hittableLayers,
    float explosionRadius,
    GameObject fireAreaPrefab,
    float fireDuration,
    float fireTickInterval,
    int fireDamage,
    float fireRadius)
{
    Debug.Log(
        $"[CrossbowExplosiveArrow] INITIALIZE RECEIVED | " +
        $"Fire Prefab = {fireAreaPrefab}"
    );

    this.explosionRadius = explosionRadius;

    this.fireAreaPrefab = fireAreaPrefab;
    this.fireDuration = fireDuration;
    this.fireTickInterval = fireTickInterval;
    this.fireDamage = fireDamage;
    this.fireRadius = fireRadius;

    InitializeSkill(
        primaryDamage,
        primaryDamageType,
        secondaryDamage,
        secondaryDamageType,
        tertiaryDamage,
        tertiaryDamageType,
        speed,
        maxDistance,
        homing,
        hittableLayers
    );
}

protected override bool ShouldDealDirectDamage => false;

protected override void OnImpact(Vector3 impactPosition)
{
    Detonate(impactPosition);
}

protected override void OnMaxRangeReached()
{
    Detonate(transform.position);
}

private void Detonate(Vector3 position)
{
    if (detonated)
        return;

    detonated = true;

    // 1. Explosion visual
    CreateExplosionVisual(position);

    // 2. Explosion damage
    DamageExplosion(position);

    // 3. Spawn persistent fire area
    SpawnFire(position);
}

    private void CreateExplosionVisual(Vector3 position)
    {
        if (explosionPrefab == null)
            return;

        GameObject visual =
            Instantiate(
                explosionPrefab,
                position,
                Quaternion.identity
            );

        if (explosionVisualDuration > 0f)
        {
            Destroy(
                visual,
                explosionVisualDuration
            );
        }
    }

    private void DamageExplosion(Vector3 position)
    {
        Collider2D[] hits =
            Physics2D.OverlapCircleAll(
                position,
                explosionRadius,
                HittableLayers
            );

        foreach (Collider2D hit in hits)
        {
            if (hit == null)
                continue;

            IDamageable target =
                hit.GetComponentInParent<IDamageable>();

            if (target == null)
                continue;

            MonoBehaviour targetBehaviour =
                target as MonoBehaviour;

            if (targetBehaviour == null)
                continue;

            if (!PlayerElevationLevel.CanAffectTarget(
                    targetBehaviour.transform))
            {
                continue;
            }

            ApplyDamage(
                target,
                PrimaryDamage,
                PrimaryDamageType,
                position
            );

            ApplyDamage(
                target,
                SecondaryDamage,
                SecondaryDamageType,
                position
            );

            ApplyDamage(
                target,
                TertiaryDamage,
                TertiaryDamageType,
                position
            );
        }
    }

    private void ApplyDamage(
        IDamageable target,
        int damage,
        DamageType damageType,
        Vector3 damageSource)
    {
        if (target == null)
            return;

        if (damage <= 0)
            return;

        if (damageType == DamageType.None)
            return;

        target.TakeDamage(
            damage,
            damageType,
            damageSource
        );
    }

  private void SpawnFire(Vector3 position)
{
    Debug.Log(
        $"[CrossbowExplosiveArrow] SPAWN FIRE | " +
        $"Stored Fire Prefab: {fireAreaPrefab} | " +
        $"Position: {position}"
    );

    if (fireAreaPrefab == null)
    {
        Debug.LogError(
            "[CrossbowExplosiveArrow] Fire Area Prefab is NULL when SpawnFire() is called."
        );

        return;
    }

    GameObject fire =
        Instantiate(
            fireAreaPrefab,
            position,
            Quaternion.identity
        );

    if (fire == null)
    {
        Debug.LogError(
            "[CrossbowExplosiveArrow] Failed to instantiate Fire Area."
        );

        return;
    }

    Fire fireComponent =
        fire.GetComponent<Fire>();

    if (fireComponent == null)
    {
        Debug.LogError(
            "[CrossbowExplosiveArrow] Spawned Fire prefab does not have Fire.cs on its ROOT GameObject.",
            fire
        );

        if (fireDuration > 0f)
            Destroy(fire, fireDuration);

        return;
    }

    fireComponent.Initialize(
        fireDamage,
        PrimaryDamageType,
        fireRadius,
        fireDuration,
        fireTickInterval,
        HittableLayers
    );

    Debug.Log(
        $"[CrossbowExplosiveArrow] FIRE SPAWNED SUCCESSFULLY | " +
        $"Damage: {fireDamage} | " +
        $"Radius: {fireRadius} | " +
        $"Duration: {fireDuration} | " +
        $"Tick: {fireTickInterval}"
    );
}

    private int PrimaryDamage =>
        GetPrimaryDamage();

    private DamageType PrimaryDamageType =>
        GetPrimaryDamageType();

    private int SecondaryDamage =>
        GetSecondaryDamage();

    private DamageType SecondaryDamageType =>
        GetSecondaryDamageType();

    private int TertiaryDamage =>
        GetTertiaryDamage();

    private DamageType TertiaryDamageType =>
        GetTertiaryDamageType();

    private LayerMask HittableLayers =>
        GetHittableLayers();

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(
            transform.position,
            explosionRadius
        );
    }
}

