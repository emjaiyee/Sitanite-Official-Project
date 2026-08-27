using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Executes the behavior described by the equipped ItemData asset.
/// Keep one instance on the player's dedicated Weapons object.
/// </summary>
public class WeaponController : MonoBehaviour, IWeapon, IChargeableWeapon
{
    [SerializeField] private Transform attackPoint;
    [SerializeField] private Transform firePoint;

    private ItemData data;
    private PlayerStats playerStats;
    private float chargeStartTime;
    private bool isCharging;
    private GameObject activeChargeVisual;

    public string WeaponId => data == null ? string.Empty : data.WeaponId;

    private void Awake()
    {
        playerStats = GetComponentInParent<PlayerStats>();
    }

    public void Configure(ItemData weaponData)
    {
        StopCharging();
        data = weaponData;
    }

    private void Update()
    {
        if (!isCharging || data == null)
            return;

        float chargePercent = Mathf.Clamp01(
            (Time.time - chargeStartTime) / data.MaxChargeTime
        );

        if (activeChargeVisual != null)
            activeChargeVisual.transform.localScale = Vector3.one * Mathf.Lerp(
                1f,
                data.MaxChargeVisualScale,
                chargePercent
            );
    }

    public void Attack()
    {
        if (!IsConfigured())
            return;

        if (data.WeaponAttackType == WeaponAttackType.Melee)
        {
            AttackMelee();
            return;
        }

        if (data.WeaponAttackType == WeaponAttackType.Ranged)
        {
            FireProjectile(
                data.ProjectilePrefab,
                data.AttackRange,
                GetPrimaryDamage(),
                data.ProjectileSpeed
            );
            return;
        }

        Debug.LogWarning($"{WeaponId}: attack behavior is not implemented.");
    }

    private void AttackMelee()
    {
        if (attackPoint == null)
        {
            Debug.LogWarning($"{WeaponId}: attack point is not assigned.");
            return;
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(
            attackPoint.position,
            data.AttackRange,
            data.HittableLayers
        );

        foreach (Collider2D hit in hits)
        {
            IDamageable target = hit == null ? null : hit.GetComponentInParent<IDamageable>();
            if (target != null)
                target.TakeDamage(GetPrimaryDamage(), data.PrimaryDamageType);
        }
    }

    public void UseSkill()
    {
        if (!IsConfigured())
            return;

        switch (data.WeaponSkillType)
        {
            case WeaponSkillType.AreaDamage:
                UseAreaDamageSkill();
                break;
            case WeaponSkillType.ArrowRain:
                UseArrowRainSkill();
                break;
            case WeaponSkillType.ChargedArrow:
                StartCharging();
                break;
            case WeaponSkillType.Beam:
                StartCharging();
                break;
            default:
                Debug.LogWarning($"{WeaponId}: skill behavior is not implemented.");
                break;
        }
    }

    private void UseAreaDamageSkill()
    {
        Vector3 origin = transform.root.position;
        float radius = data.SkillRadius * data.SkillRadiusMultiplier;
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            origin,
            radius,
            data.SkillHittableLayers
        );

        DamageTargets(hits);
        CreateVisual(data.SkillVisualPrefab, origin, radius * 2f, 0.5f);
    }

    private void UseArrowRainSkill()
    {
        if (!TryGetMousePosition(out Vector3 targetPosition))
            return;

        if (Vector2.Distance(transform.root.position, targetPosition) > data.SkillRange)
        {
            Debug.Log($"{WeaponId}: skill target is out of range.");
            return;
        }

        CreateVisual(
            data.SkillVisualPrefab,
            targetPosition,
            data.SkillRadius * 2f,
            data.SkillVisualDuration
        );
        StartCoroutine(ArrowRain(targetPosition));
    }

    private IEnumerator ArrowRain(Vector3 targetPosition)
    {
        int projectileCount = Mathf.Max(0, data.SkillProjectileCount);
        float delay = projectileCount > 1
            ? data.SkillDuration / (projectileCount - 1)
            : 0f;

        for (int index = 0; index < projectileCount; index++)
        {
            Vector2 randomOffset = Random.insideUnitCircle * data.SkillRadius;
            Vector3 landingPosition = targetPosition + new Vector3(randomOffset.x, randomOffset.y, 0f);

            if (data.SkillProjectilePrefab != null)
            {
                GameObject visual = Instantiate(
                    data.SkillProjectilePrefab,
                    landingPosition + Vector3.up * 2f,
                    Quaternion.Euler(0f, 0f, -90f)
                );
                Destroy(visual, 0.4f);
            }

            Collider2D[] hits = Physics2D.OverlapCircleAll(
                landingPosition,
                0.35f,
                data.HittableLayers
            );
            DamageTargets(hits);

            if (index < projectileCount - 1)
                yield return new WaitForSeconds(delay);
        }
    }

    private void StartCharging()
    {
        if (isCharging)
            return;

        isCharging = true;
        chargeStartTime = Time.time;

        if (data.ChargeVisualPrefab != null)
            activeChargeVisual = Instantiate(
                data.ChargeVisualPrefab,
                transform.root.position,
                Quaternion.identity
            );
    }

    public void ReleaseSkill()
    {
        if (!isCharging || data == null)
            return;

        float chargePercent = Mathf.Clamp01(
            (Time.time - chargeStartTime) / data.MaxChargeTime
        );
        int damage = Mathf.RoundToInt(Mathf.Lerp(
            data.MinimumSkillDamage,
            data.MaximumSkillDamage,
            chargePercent
        ));

        if (data.WeaponSkillType == WeaponSkillType.Beam)
            FireBeam(damage);
        else
            FireProjectile(
                data.SkillProjectilePrefab,
                data.SkillRange,
                damage,
                data.ProjectileSpeed * Mathf.Lerp(1f, 1.75f, chargePercent),
                true
            );
        StopCharging();
    }

    private void FireBeam(int damage)
    {
        if (data.SkillProjectilePrefab == null || firePoint == null)
        {
            Debug.LogWarning($"{WeaponId}: beam prefab or fire point is not assigned.");
            return;
        }

        if (!TryGetMousePosition(out Vector3 mousePosition))
            return;

        if (Vector2.Distance(transform.root.position, mousePosition) > data.SkillRange)
        {
            Debug.Log($"{WeaponId}: beam target is out of range.");
            return;
        }

        Vector2 direction = (mousePosition - firePoint.position).normalized;
        if (direction == Vector2.zero)
            return;

        GameObject beamObject = Instantiate(
            data.SkillProjectilePrefab,
            firePoint.position,
            Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg)
        );
        Beam beam = beamObject.GetComponent<Beam>();

        if (beam == null)
        {
            Debug.LogError($"{WeaponId}: beam prefab does not contain a Beam component.");
            Destroy(beamObject);
            return;
        }

        beam.Initialize(
            damage,
            data.PrimaryDamageType,
            data.GetSkillDamage(DamageSlot.Secondary, damage),
            data.GetDamageType(DamageSlot.Secondary),
            data.GetSkillDamage(DamageSlot.Tertiary, damage),
            data.GetDamageType(DamageSlot.Tertiary),
            data.SkillRange,
            data.BeamWidth,
            data.BeamDuration,
            data.SkillHittableLayers
        );
    }

    private void FireProjectile(
        GameObject projectilePrefab,
        float range,
        int damage,
        float speed,
        bool isSkill = false)
    {
        if (projectilePrefab == null || firePoint == null)
        {
            Debug.LogWarning($"{WeaponId}: projectile prefab or fire point is not assigned.");
            return;
        }

        if (!TryGetMousePosition(out Vector3 mousePosition))
            return;

        if (Vector2.Distance(transform.root.position, mousePosition) > range)
        {
            Debug.Log($"{WeaponId}: target is out of range.");
            return;
        }

        Vector2 direction = (mousePosition - firePoint.position).normalized;
        if (direction == Vector2.zero)
            return;

        GameObject projectile = Instantiate(
            projectilePrefab,
            firePoint.position,
            Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg)
        );
        Arrow arrow = projectile.GetComponent<Arrow>();

        if (arrow == null)
        {
            Debug.LogError($"{WeaponId}: projectile prefab does not contain an Arrow component.");
            Destroy(projectile);
            return;
        }

        if (isSkill)
        {
            arrow.InitializeSkill(
                damage,
                data.PrimaryDamageType,
                data.GetSkillDamage(DamageSlot.Secondary, damage),
                data.GetDamageType(DamageSlot.Secondary),
                data.GetSkillDamage(DamageSlot.Tertiary, damage),
                data.GetDamageType(DamageSlot.Tertiary),
                speed,
                range
            );
        }
        else
        {
            arrow.Initialize(damage, data.PrimaryDamageType, speed, range);
        }
    }

    private bool TryGetMousePosition(out Vector3 mousePosition)
    {
        mousePosition = default;
        if (Mouse.current == null || Camera.main == null)
            return false;

        mousePosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        mousePosition.z = transform.root.position.z;
        return true;
    }

    private void DamageTargets(Collider2D[] hits)
    {
        foreach (Collider2D hit in hits)
        {
            IDamageable target = hit == null ? null : hit.GetComponentInParent<IDamageable>();
            if (target != null)
            {
                ApplySkillDamage(target, DamageSlot.Primary);
                ApplySkillDamage(target, DamageSlot.Secondary);
                ApplySkillDamage(target, DamageSlot.Tertiary);
            }
        }
    }

    private void ApplySkillDamage(IDamageable target, DamageSlot slot)
    {
        int damage = data.GetSkillDamage(slot);
        DamageType damageType = data.GetDamageType(slot);

        if (damage > 0 && damageType != DamageType.None)
            target.TakeDamage(damage, damageType);
    }

    private int GetPrimaryDamage()
    {
        if (playerStats == null)
            return data.PrimaryDamage;

        return Mathf.RoundToInt(
            playerStats.GetEffectiveDamage(data.PrimaryDamageType)
        );
    }

    private void CreateVisual(GameObject prefab, Vector3 position, float scale, float lifetime)
    {
        if (prefab == null)
            return;

        GameObject visual = Instantiate(prefab, position, Quaternion.identity);
        visual.transform.localScale = Vector3.one * scale;
        Destroy(visual, lifetime);
    }

    private void StopCharging()
    {
        isCharging = false;
        if (activeChargeVisual != null)
            Destroy(activeChargeVisual);
        activeChargeVisual = null;
    }

    private bool IsConfigured()
    {
        if (data != null)
            return true;

        Debug.LogWarning("WeaponController has not been configured with an ItemData asset.");
        return false;
    }
}
