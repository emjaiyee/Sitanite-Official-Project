using System.Collections;
using System.Collections.Generic;
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
    /// <summary>
    /// <forsounds>
    private WeaponAudioController weaponAudio;
   
    [Header("Full Charge Indicator")]
    [SerializeField] private GameObject fullyChargedIndicatorPrefab;
    [SerializeField] private float indicatorRotationSpeed = 180f;
    [SerializeField] private float indicatorPulseSpeed = 3f;
    [SerializeField] private float indicatorPulseMin = 0.9f;
    [SerializeField] private float indicatorPulseMax = 1.1f;
    [SerializeField] private bool rotateIndicator = true;
    [SerializeField] private bool pulseIndicator = true;

    private ItemData data;
    private PlayerStats playerStats;

    private float chargeStartTime;
    private bool isCharging;
    private GameObject activeChargeVisual;
    private GameObject activeFullChargeIndicator;
    private SpriteRenderer[] chargeVisualSprites;
    private SpriteRenderer[] indicatorSprites;
    private bool hasReachedFullCharge;
    private bool arrowRainTargeting;
    private GameObject activeArrowRainPreview;
    private Vector3 arrowRainTargetPosition;
    private IDamageable selectedStabTarget;
    private SpriteRenderer[] selectedStabRenderers;
    private Color[] selectedStabOriginalColors;

    [Header("Dagger Targeting")]
    [Min(0.1f)]
    [SerializeField] private float stabTargetingRange = 2f;
    [SerializeField] private Color selectedStabColor = Color.yellow;
    private float fullChargeReachedTime;
    private float indicatorPulseTimer;
    private float nextAttackTime;
    private float nextSkillTime;

    // Direction captured when the skill begins charging.
    private Vector2 skillDirection = Vector2.right;

    public string WeaponId =>
        data == null ? string.Empty : data.WeaponId;

    public bool CanAttack => data != null && Time.time >= nextAttackTime;
    public bool CanUseSkill => data != null && Time.time >= nextSkillTime;
    public bool IsTargetingSkill =>
        arrowRainTargeting ||
        selectedStabTarget != null &&
        (selectedStabTarget as MonoBehaviour) != null;

    public float ChargePercent =>
        isCharging && data != null
            ? Mathf.Clamp01(
                (Time.time - chargeStartTime) /
                Mathf.Max(0.0001f, data.MaxChargeTime)
            )
            : 0f;

    private void Awake()
{
    playerStats = GetComponentInParent<PlayerStats>();

    weaponAudio = GetComponentInParent<WeaponAudioController>();

    if (weaponAudio == null)
    {
        weaponAudio = transform.root.GetComponentInChildren<WeaponAudioController>(true);
    }
}

    public void Configure(ItemData weaponData)
    {
        StopCharging();
        ClearArrowRainPreview();
        ClearStabTarget();
        data = weaponData;
        nextAttackTime = 0f;
        nextSkillTime = 0f;
    }

    private void Update()
    {
        if (!isCharging || data == null)
            return;

        float chargePercent = Mathf.Clamp01(
            (Time.time - chargeStartTime) /
            Mathf.Max(0.0001f, data.MaxChargeTime)
        );

        if (activeChargeVisual != null)
        {
            activeChargeVisual.transform.localScale =
                Vector3.one * Mathf.Lerp(
                    data.StartChargeVisualScale,
                    data.EndChargeVisualScale,
                    chargePercent
                );
        }

        // Spawn the full charge indicator when reaching 100%
        if (chargePercent >= 1f && !hasReachedFullCharge)
        {
            hasReachedFullCharge = true;
            fullChargeReachedTime = Time.time;
            SpawnFullChargeIndicator();
        }

        if (hasReachedFullCharge)
        {
            AnimateMaxChargeVisual();
            AnimateFullChargeIndicator();
        }
    }

    // =========================================================
    // ATTACK
    // =========================================================

 
public void Attack(Vector2 direction)
{
    if (!IsConfigured() || !CanAttack)
        return;

    if (direction.sqrMagnitude <= 0.0001f)
        return;

    direction.Normalize();

    nextAttackTime = Time.time + GetCooldown(data.AttackCooldown);

    // Play weapon-specific attack SFX
    weaponAudio?.PlayAttackSound();

    Vector3 attackOrigin = attackPoint == null
        ? transform.root.position
        : attackPoint.position;
    Vector3 visualPosition = attackOrigin + (Vector3)(direction * data.AttackRange);

    float visualAngle =
        Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

    CreateVisual(
        data.AttackVisualPrefab,
        visualPosition,
        1f,
        0.5f,
        visualAngle
    );

    switch (data.WeaponAttackType)
    {
        case WeaponAttackType.Melee:

            AttackMelee(direction);

            break;

        case WeaponAttackType.Ranged:
        case WeaponAttackType.Spell:

            float projectileSpeed =
                data.WeaponAttackType == WeaponAttackType.Spell
                    ? data.SpellProjectileSpeed
                    : data.ProjectileSpeed;

            FireProjectile(
                data.ProjectilePrefab,
                data.AttackRange,
                GetPrimaryDamage(),
                projectileSpeed,
                direction
            );

            break;

        default:

            Debug.LogWarning(
                $"{WeaponId}: attack behavior is not implemented."
            );

            break;
    }
}


    private void AttackMelee(Vector2 direction)
    {
        if (attackPoint == null)
        {
            Debug.LogWarning(
                $"{WeaponId}: attack point is not assigned."
            );

            return;
        }

        if (data.HittableLayers.value == 0)
        {
            Debug.LogWarning(
                $"{WeaponId}: hittable layers are not set. " +
                "Melee attacks cannot register on enemy health."
            );
        }

        // Place the melee hit area in front of the player.
        Vector2 attackPosition =
            (Vector2)transform.root.position +
            direction * data.AttackRange;

        Collider2D[] hits = Physics2D.OverlapCircleAll(
            attackPosition,
            data.AttackRange,
            Physics2D.AllLayers
        );

        int primaryDamage = GetPrimaryDamage();
        HashSet<IDamageable> damagedTargets = new HashSet<IDamageable>();

        foreach (Collider2D hit in hits)
        {
            // The collider on the hittable layer belongs to the object
            // holding EnemyHealth (which implements IDamageable).
            IDamageable target =
                hit == null
                    ? null
                    : hit.GetComponentInParent<IDamageable>();

            if (target == null)
                continue;

            if (!damagedTargets.Add(target))
                continue;

            if (!PlayerElevationLevel.CanAffectTarget(
                    (target as MonoBehaviour)?.transform))
                continue;

            ApplyMeleeDamage(target, primaryDamage);
        }
    }

    private void ApplyMeleeDamage(IDamageable target, int primaryDamage)
    {
        Vector3 source = transform.root.position;

        if (primaryDamage > 0 && data.PrimaryDamageType != DamageType.None)
            target.TakeDamage(primaryDamage, data.PrimaryDamageType, source);

        int secondaryDamage = data.GetDamage(DamageSlot.Secondary);
        DamageType secondaryDamageType = data.GetDamageType(DamageSlot.Secondary);
        if (secondaryDamage > 0 && secondaryDamageType != DamageType.None)
            target.TakeDamage(secondaryDamage, secondaryDamageType, source);

        int tertiaryDamage = data.GetDamage(DamageSlot.Tertiary);
        DamageType tertiaryDamageType = data.GetDamageType(DamageSlot.Tertiary);
        if (tertiaryDamage > 0 && tertiaryDamageType != DamageType.None)
            target.TakeDamage(tertiaryDamage, tertiaryDamageType, source);
    }

    // =========================================================
    // SKILL
    // =========================================================

 

public void UseSkill(Vector2 direction)
{
    if (!IsConfigured() || !CanUseSkill)
        return;

    if (direction.sqrMagnitude <= 0.0001f)
        return;

    direction.Normalize();

    switch (data.WeaponSkillType)
    {
        case WeaponSkillType.AreaDamage:

            nextSkillTime = Time.time + GetCooldown(data.SkillCooldown);
            weaponAudio?.PlaySkillSound();
            UseAreaDamageSkill();

            break;

        case WeaponSkillType.ArrowRain:

            BeginArrowRainTargeting(direction);

            break;

        case WeaponSkillType.ChargedArrow:

            nextSkillTime = Time.time + GetCooldown(data.SkillCooldown);
            StartCharging(direction);

            break;

        case WeaponSkillType.Beam:

            nextSkillTime = Time.time + GetCooldown(data.SkillCooldown);
            StartCharging(direction);

            break;
         case WeaponSkillType.CrossbowExplosion:
            weaponAudio?.PlaySkillSound();
            UseCrossbowExplosionSkill(direction);
            break;
        case WeaponSkillType.Stab:

            if (selectedStabTarget == null)
            {
                nextSkillTime = Time.time + GetCooldown(data.SkillCooldown);
                weaponAudio?.PlaySkillSound();
                UseStabSkill(direction);
            }

            break;
        

        case WeaponSkillType.Slash:

            nextSkillTime = Time.time + GetCooldown(data.SkillCooldown);
            weaponAudio?.PlaySkillSound();
            UseSlashSkill(direction);

            break;

        default:

            Debug.LogWarning(
                $"{WeaponId}: skill behavior is not implemented."
            );

            break;
    }

}

        public bool TrySelectSkillTarget(Vector3 worldPosition)
        {
            if (!IsConfigured() ||
                data.WeaponSkillType != WeaponSkillType.Stab)
                return false;

            Vector2 playerPosition = transform.root.position;
            Collider2D[] hits = Physics2D.OverlapPointAll(
                worldPosition,
                Physics2D.AllLayers
            );

            IDamageable closestTarget = null;
            float closestDistance = float.MaxValue;

            foreach (Collider2D hit in hits)
            {
                IDamageable target =
                    hit == null
                        ? null
                        : hit.GetComponentInParent<IDamageable>();

                if (target == null ||
                    !PlayerElevationLevel.CanAffectTarget(
                        (target as MonoBehaviour)?.transform))
                    continue;

                float playerDistance = Vector2.Distance(
                    playerPosition,
                    (target as MonoBehaviour).transform.position
                );

                if (playerDistance > stabTargetingRange)
                    continue;

                float distance = Vector2.Distance(
                    worldPosition,
                    (target as MonoBehaviour).transform.position
                );

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTarget = target;
                }
            }

            ClearStabTarget();
            selectedStabTarget = closestTarget;
            HighlightStabTarget();
            return selectedStabTarget != null;
        }

        public Vector3 GetSkillTargetPosition()
        {
            return (selectedStabTarget as MonoBehaviour)?.transform.position
                ?? transform.root.position;
        }

        public Vector3 GetSkillApproachPosition()
        {
            Vector3 origin = transform.root.position;
            Vector3 targetPosition = GetSkillTargetPosition();
            Vector2 direction = (targetPosition - origin);

            if (direction.sqrMagnitude <= 0.0001f)
                return origin;

            float approachDistance = Mathf.Max(0.1f, data.SkillRange * 0.75f);
            return targetPosition - (Vector3)direction.normalized * approachDistance;
        }

        public void UpdateSkillTarget(Vector3 targetPosition)
        {
            if (!IsTargetingSkill || data == null)
                return;

            Vector3 origin = transform.root.position;
            Vector2 offset = (Vector2)(targetPosition - origin);
            float range = Mathf.Max(0f, data.SkillRange);

            if (offset.sqrMagnitude > range * range)
                offset = offset.normalized * range;

            arrowRainTargetPosition =
                origin + new Vector3(offset.x, offset.y, 0f);

            if (activeArrowRainPreview != null)
                activeArrowRainPreview.transform.position = arrowRainTargetPosition;
        }

        public void ConfirmSkill()
        {
            if (selectedStabTarget != null &&
                (selectedStabTarget as MonoBehaviour) != null)
            {
                Vector3 targetPosition = GetSkillTargetPosition();
                Vector2 direction = targetPosition - transform.root.position;

                ClearStabTarget();
                nextSkillTime = Time.time + GetCooldown(data.SkillCooldown);
                weaponAudio?.PlaySkillSound();
                UseStabSkill(direction);
                return;
            }

            if (!IsTargetingSkill || data == null)
                return;

            Vector3 arrowRainPosition = arrowRainTargetPosition;
            ClearArrowRainPreview();
            nextSkillTime = Time.time + GetCooldown(data.SkillCooldown);
            weaponAudio?.PlaySkillSound();
            UseArrowRainSkill(arrowRainPosition);
        }

        private void HighlightStabTarget()
        {
            MonoBehaviour targetBehaviour =
                selectedStabTarget as MonoBehaviour;

            if (targetBehaviour == null)
                return;

            selectedStabRenderers =
                targetBehaviour.GetComponentsInChildren<SpriteRenderer>();

            selectedStabOriginalColors =
                new Color[selectedStabRenderers.Length];

            for (int index = 0; index < selectedStabRenderers.Length; index++)
            {
                SpriteRenderer renderer = selectedStabRenderers[index];
                selectedStabOriginalColors[index] = renderer.color;
                renderer.color = selectedStabColor;
            }
        }

        private void ClearStabTarget()
        {
            if (selectedStabRenderers != null &&
                selectedStabOriginalColors != null)
            {
                for (int index = 0;
                     index < selectedStabRenderers.Length &&
                     index < selectedStabOriginalColors.Length;
                     index++)
                {
                    if (selectedStabRenderers[index] != null)
                        selectedStabRenderers[index].color =
                            selectedStabOriginalColors[index];
                }
            }

            selectedStabTarget = null;
            selectedStabRenderers = null;
            selectedStabOriginalColors = null;
        }




    // =========================================================
// STAB
// =========================================================

private void UseCrossbowExplosionSkill(Vector2 direction)
{
    if (data.SkillProjectilePrefab == null)
        return;

    Vector3 spawnPosition =
        firePoint != null
            ? firePoint.position
            : transform.root.position;

    Vector2 shootDirection = direction.normalized;

    if (shootDirection.sqrMagnitude <= 0.001f)
        shootDirection = Vector2.right;

    GameObject projectile =
        Instantiate(
            data.SkillProjectilePrefab,
            spawnPosition,
            Quaternion.identity
        );

    projectile.transform.right = shootDirection;

    CrossbowExplosiveArrow explosiveArrow =
        projectile.GetComponent<CrossbowExplosiveArrow>();

    if (explosiveArrow == null)
    {
        Debug.LogError(
            "Crossbow Explosion skill requires a CrossbowExplosiveArrow component.",
            projectile
        );

        Destroy(projectile);
        return;
    }

    int primaryDamage =
        data.GetSkillDamage(
            DamageSlot.Primary,
            CalculateSkillDamage(data.SkillDamage)
        );

    DamageType primaryDamageType =
        data.GetDamageType(DamageSlot.Primary);

    int secondaryDamage =
        data.GetSkillDamage(
            DamageSlot.Secondary,
            CalculateSkillDamage(data.SkillDamage)
        );

    DamageType secondaryDamageType =
        data.GetDamageType(DamageSlot.Secondary);

    int tertiaryDamage =
        data.GetSkillDamage(
            DamageSlot.Tertiary,
            CalculateSkillDamage(data.SkillDamage)
        );

    DamageType tertiaryDamageType =
        data.GetDamageType(DamageSlot.Tertiary);

    // DEBUG
    Debug.Log(
        $"CROSSBOW DATA DEBUG | " +
        $"Item: {data.name} | " +
        $"WeaponId: {data.WeaponId} | " +
        $"Skill Type: {data.WeaponSkillType} | " +
        $"Skill Projectile: {data.SkillProjectilePrefab} | " +
        $"Fire Area: {data.FireAreaPrefab} | " +
        $"Fire Duration: {data.FireDuration} | " +
        $"Fire Damage: {data.FireDamage} | " +
        $"Fire Radius: {data.FireRadius}"
    );

    explosiveArrow.InitializeExplosive(
        primaryDamage,
        primaryDamageType,
        secondaryDamage,
        secondaryDamageType,
        tertiaryDamage,
        tertiaryDamageType,
        data.ProjectileSpeed,
        data.SkillRange,
        data.Homing,
        Physics2D.AllLayers,
        data.SkillRadius,
        data.FireAreaPrefab,
        data.FireDuration,
        data.FireDamageInterval,
        data.FireDamage,
        data.FireRadius
    );
}

private void UseStabSkill(Vector2 direction)
{
    if (attackPoint == null)
    {
        Debug.LogWarning(
            $"{WeaponId}: attack point is not assigned."
        );

        return;
    }

    if (direction.sqrMagnitude <= 0.0001f)
        return;

    direction.Normalize();

    Vector2 origin =
        (Vector2)transform.root.position;

    // Short forward reach for the dagger stab.
    Vector2 stabPosition =
        origin + direction * data.SkillRange;

    float stabRadius =
        Mathf.Max(0.1f, data.SkillRadius);

    Collider2D[] hits =
        Physics2D.OverlapCircleAll(
            stabPosition,
            stabRadius,
            Physics2D.AllLayers
        );

    IDamageable closestTarget = null;
    float closestDistance = float.MaxValue;

    foreach (Collider2D hit in hits)
    {
        if (hit == null)
            continue;

        IDamageable target =
            hit.GetComponentInParent<IDamageable>();

        if (target == null)
            continue;

        if (!PlayerElevationLevel.CanAffectTarget(
            (target as MonoBehaviour)?.transform))
        {
            continue;
        }

        float distance =
            Vector2.Distance(
                origin,
                (target as MonoBehaviour).transform.position
            );

        if (distance < closestDistance)
        {
            closestDistance = distance;
            closestTarget = target;
        }
    }

    if (closestTarget != null)
    {
        ApplySkillDamage(
            closestTarget,
            DamageSlot.Primary
        );

        ApplySkillDamage(
            closestTarget,
            DamageSlot.Secondary
        );

        ApplySkillDamage(
            closestTarget,
            DamageSlot.Tertiary
        );
    }

    // Optional stab visual.
    if (data.SkillVisualPrefab != null)
    {
        float angle =
            Mathf.Atan2(
                direction.y,
                direction.x
            ) * Mathf.Rad2Deg;

        GameObject visual =
            Instantiate(
                data.SkillVisualPrefab,
                stabPosition,
                Quaternion.Euler(0f, 0f, angle)
            );

        visual.transform.localScale =
            Vector3.one * data.SkillRadius * 2f;

        Destroy(
            visual,
            data.SkillVisualDuration
        );
    }
}

    // =========================================================
    // SLASH
    // =========================================================

    private void UseSlashSkill(Vector2 direction)
    {
        Vector3 origin = attackPoint == null
            ? transform.root.position
            : attackPoint.position;
        float radius = data.SkillRadius * data.SkillRadiusMultiplier;
        float halfAngle = data.SlashAngle * 0.5f;
        float minimumDot = Mathf.Cos(halfAngle * Mathf.Deg2Rad);
        HashSet<IDamageable> targets = new HashSet<IDamageable>();

        foreach (Collider2D hit in Physics2D.OverlapCircleAll(
                     origin,
                     radius,
                     Physics2D.AllLayers))
        {
            IDamageable target = hit == null
                ? null
                : hit.GetComponentInParent<IDamageable>();

            if (target == null)
                continue;

            Vector2 targetDirection = (Vector2)hit.ClosestPoint(origin) - (Vector2)origin;
            if (targetDirection.sqrMagnitude > 0.0001f &&
                Vector2.Dot(direction, targetDirection.normalized) < minimumDot)
                continue;

            targets.Add(target);
        }

        foreach (IDamageable target in targets)
        {
            ApplySkillDamage(target, DamageSlot.Primary);
            ApplySkillDamage(target, DamageSlot.Secondary);
            ApplySkillDamage(target, DamageSlot.Tertiary);
        }

        CreateVisual(
            data.SkillVisualPrefab,
            origin,
            radius * 2f,
            data.SkillVisualDuration,
            Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg
        );
    }

    // =========================================================
    // AREA DAMAGE
    // =========================================================

    private void UseAreaDamageSkill()
    {
        Vector3 origin = transform.root.position;

        float radius =
            data.SkillRadius *
            data.SkillRadiusMultiplier;

        Collider2D[] hits =
            Physics2D.OverlapCircleAll(
                origin,
                radius,
                Physics2D.AllLayers
            );

        DamageTargets(hits);

        CreateVisual(
            data.SkillVisualPrefab,
            origin,
            radius * 2f,
            0.5f
        );
    }

    // =========================================================
    // ARROW RAIN
    // =========================================================

    private void BeginArrowRainTargeting(Vector2 direction)
    {
        if (arrowRainTargeting)
            return;

        arrowRainTargeting = true;
        Vector3 origin = transform.root.position;
        Vector3 targetPosition =
            origin + (Vector3)(direction.normalized * data.SkillRange);

        if (data.SkillVisualPrefab != null)
        {
            activeArrowRainPreview = Instantiate(
                data.SkillVisualPrefab,
                targetPosition,
                Quaternion.identity
            );
            activeArrowRainPreview.transform.localScale =
                Vector3.one * data.SkillRadius * 2f;
        }

        UpdateSkillTarget(targetPosition);
    }

    private void UseArrowRainSkill(Vector3 targetPosition)
    {
        CreateVisual(
            data.SkillVisualPrefab,
            targetPosition,
            data.SkillRadius * 2f,
            data.SkillVisualDuration
        );

        StartCoroutine(
            ArrowRain(targetPosition)
        );
    }

    private void ClearArrowRainPreview()
    {
        if (activeArrowRainPreview != null)
            Destroy(activeArrowRainPreview);

        activeArrowRainPreview = null;
        arrowRainTargeting = false;
    }

    private IEnumerator ArrowRain(Vector3 targetPosition)
    {
        int projectileCount =
            Mathf.Max(0, data.SkillProjectileCount);

        float delay =
            projectileCount > 1
                ? data.SkillDuration / (projectileCount - 1)
                : 0f;

        for (int index = 0;
             index < projectileCount;
             index++)
        {
            Vector2 randomOffset =
                Random.insideUnitCircle *
                data.SkillRadius;

            Vector3 landingPosition =
                targetPosition +
                new Vector3(
                    randomOffset.x,
                    randomOffset.y,
                    0f
                );

            if (data.SkillProjectilePrefab != null)
            {
                GameObject visual =
                    Instantiate(
                        data.SkillProjectilePrefab,
                        landingPosition + Vector3.up * 2f,
                        Quaternion.Euler(0f, 0f, -90f)
                    );

                Destroy(visual, 0.4f);
            }

            Collider2D[] hits =
                Physics2D.OverlapCircleAll(
                    landingPosition,
                    0.35f,
                    Physics2D.AllLayers
                );

            DamageTargets(hits);

            if (index < projectileCount - 1)
                yield return new WaitForSeconds(delay);
        }
    }

    // =========================================================
    // CHARGING
    // =========================================================

   
private void StartCharging(Vector2 direction)
{
    if (isCharging)
        return;

    isCharging = true;
    hasReachedFullCharge = false;
    indicatorPulseTimer = 0f;

    chargeStartTime = Time.time;

    skillDirection = direction.normalized;

    // Play charge-start SFX
    weaponAudio?.PlayChargeStartSound();

    // Start looping charge SFX
    weaponAudio?.StartChargeLoop();

    if (data.ChargeVisualPrefab != null)
    {
        activeChargeVisual =
            Instantiate(
                data.ChargeVisualPrefab,
                firePoint == null
                    ? transform.root.position
                    : firePoint.position,
                Quaternion.Euler(
                    0f,
                    0f,
                    Mathf.Atan2(
                        direction.y,
                        direction.x
                    ) * Mathf.Rad2Deg
                )
            );

        activeChargeVisual.transform.localScale =
            Vector3.one * data.StartChargeVisualScale;

        chargeVisualSprites =
            activeChargeVisual.GetComponentsInChildren<SpriteRenderer>();
    }
}


    // =========================================================
    // AIM WHILE CHARGING
    // =========================================================

    /// <summary>
    /// Re-aims the charging skill toward a new direction.
    /// Called every frame while the skill button is held so the
    /// projectile/beam fires toward the cursor, not the initial press.
    /// </summary>
    public void UpdateSkillDirection(Vector2 direction)
    {
        if (!isCharging || data == null)
            return;

        if (direction.sqrMagnitude <= 0.0001f)
            return;

        skillDirection = direction.normalized;

        if (activeChargeVisual != null)
        {
            activeChargeVisual.transform.rotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    Mathf.Atan2(
                        skillDirection.y,
                        skillDirection.x
                    ) * Mathf.Rad2Deg
                );
        }

        // Update full charge indicator position if it's parented to root instead of firePoint
        if (activeFullChargeIndicator != null && firePoint == null)
        {
            activeFullChargeIndicator.transform.position = transform.root.position;
        }
    }

    // =========================================================
    // RELEASE CHARGED SKILL
    // =========================================================

   
public void ReleaseSkill(bool fullyCharged)
{
    if (!isCharging || data == null)
        return;

    // A max-charge release that couldn't pay the extra cost
    // fires just below maximum instead.
    float chargePercent =
        fullyCharged
            ? 1f
            : Mathf.Min(ChargePercent, 0.99f);

    int rawDamage =
        Mathf.RoundToInt(
            Mathf.Lerp(
                data.MinimumSkillDamage,
                data.MaximumSkillDamage,
                chargePercent
            )
        );

    int damage = CalculateChargedSkillDamage(rawDamage);

    Debug.Log(
        $"[WeaponController] {WeaponId} skill released: " +
        $"charge={chargePercent:P0} raw={rawDamage} " +
        $"bonus={damage - rawDamage} final={damage} " +
        $"(min={data.MinimumSkillDamage} max={data.MaximumSkillDamage} " +
        $"ticksPerSec={data.DamageTicksPerSecond})"
    );

    // Play release SFX and stop the looping charge sound
    weaponAudio?.PlayChargeReleaseSound();
    weaponAudio?.StopChargeLoop();

    if (data.WeaponSkillType == WeaponSkillType.Beam)
    {
        FireBeam(
            damage,
            skillDirection
        );
    }
    else
    {
        FireProjectile(
            data.ChargedSkillProjectilePrefab,
            data.SkillRange,
            damage,
            data.ProjectileSpeed *
                Mathf.Lerp(
                    1f,
                    1.75f,
                    chargePercent
                ),
            skillDirection,
            true
        );
    }

    StopCharging();
}

    // =========================================================
    // BEAM
    // =========================================================

    private void FireBeam(
        int damage,
        Vector2 direction)
    {
        if (data.ChargedSkillProjectilePrefab == null ||
            firePoint == null)
        {
            Debug.LogWarning(
                $"{WeaponId}: beam prefab or fire point is not assigned."
            );

            return;
        }

        if (direction.sqrMagnitude <= 0.0001f)
            return;

        direction.Normalize();

        float angle =
            Mathf.Atan2(
                direction.y,
                direction.x
            ) * Mathf.Rad2Deg;

        GameObject beamObject =
            Instantiate(
                data.ChargedSkillProjectilePrefab,
                firePoint.position,
                Quaternion.Euler(
                    0f,
                    0f,
                    angle
                )
            );

        Beam beam =
            beamObject.GetComponent<Beam>();

        if (beam == null)
        {
            Debug.LogError(
                $"{WeaponId}: beam prefab does not contain a Beam component."
            );

            Destroy(beamObject);
            return;
        }

        beam.Initialize(
            damage,
            data.PrimaryDamageType,

            data.GetSkillDamage(
                DamageSlot.Secondary,
                damage
            ),

            data.GetDamageType(
                DamageSlot.Secondary
            ),

            data.GetSkillDamage(
                DamageSlot.Tertiary,
                damage
            ),

            data.GetDamageType(
                DamageSlot.Tertiary
            ),

            data.SkillRange,
            data.BeamWidth,
            data.BeamDuration,
            Physics2D.AllLayers,
            data.DamageTicksPerSecond
        );
    }

    // =========================================================
    // PROJECTILE
    // =========================================================

    private void FireProjectile(
        GameObject projectilePrefab,
        float range,
        int damage,
        float speed,
        Vector2 direction,
        bool isSkill = false)
    {
        if (projectilePrefab == null ||
            firePoint == null)
        {
            Debug.LogWarning(
                $"{WeaponId}: projectile prefab or fire point is not assigned."
            );

            return;
        }

        if (direction.sqrMagnitude <= 0.0001f)
            return;

        direction.Normalize();

        float angle =
            Mathf.Atan2(
                direction.y,
                direction.x
            ) * Mathf.Rad2Deg;

        GameObject projectile =
            Instantiate(
                projectilePrefab,
                firePoint.position,
                Quaternion.Euler(
                    0f,
                    0f,
                    angle
                )
            );

        IProjectile projectileComponent =
            projectile.GetComponentInChildren<IProjectile>();

        if (projectileComponent == null)
        {
            Debug.LogError(
                $"{WeaponId}: projectile prefab does not contain an IProjectile component."
            );

            Destroy(projectile);
            return;
        }

        if (isSkill)
        {
            projectileComponent.InitializeSkill(
                damage,
                data.PrimaryDamageType,

                data.GetSkillDamage(
                    DamageSlot.Secondary,
                    damage
                ),

                data.GetDamageType(
                    DamageSlot.Secondary
                ),

                data.GetSkillDamage(
                    DamageSlot.Tertiary,
                    damage
                ),

                data.GetDamageType(
                    DamageSlot.Tertiary
                ),

                speed,
                range,
                data.Homing,
                Physics2D.AllLayers
            );
        }
        else
        {
            projectileComponent.Initialize(
                damage,
                data.PrimaryDamageType,
                speed,
                range,
                data.Homing,
                Physics2D.AllLayers
            );
        }
    }

    // =========================================================
    // DAMAGE
    // =========================================================

    private void DamageTargets(Collider2D[] hits)
    {
        foreach (Collider2D hit in hits)
        {
            IDamageable target =
                hit == null
                    ? null
                    : hit.GetComponentInParent<IDamageable>();

            if (target == null)
                continue;

            if (!PlayerElevationLevel.CanAffectTarget(
                    (target as MonoBehaviour)?.transform))
                continue;

            ApplySkillDamage(
                target,
                DamageSlot.Primary
            );

            ApplySkillDamage(
                target,
                DamageSlot.Secondary
            );

            ApplySkillDamage(
                target,
                DamageSlot.Tertiary
            );
        }
    }

    private void ApplySkillDamage(
        IDamageable target,
        DamageSlot slot)
    {
        int damage =
            data.GetSkillDamage(
                slot,
                CalculateSkillDamage(data.SkillDamage)
            );

        DamageType damageType =
            data.GetDamageType(slot);

        if (damage > 0 &&
            damageType != DamageType.None)
        {
            target.TakeDamage(
                damage,
                damageType,
                transform.root.position
            );
        }
    }

    // =========================================================
    // DAMAGE CALCULATION
    // =========================================================

    /// <summary>
    /// Skill damage = the player's runtime base damage
    ///              + damage modifiers from equipped armor
    ///              + the weapon skill's configured damage value.
    /// </summary>
    private int CalculateSkillDamage(int rawDamage)
    {
        if (playerStats == null)
            return rawDamage;

        float baseDamage = playerStats.GetPreEquipmentDamage(data.PrimaryDamageType);
        if (EquipmentManager.Instance != null)
        {
            baseDamage = EquipmentManager.Instance.GetModifiedStat(
                baseDamage,
                StatType.Damage,
                data.PrimaryDamageType
            );
        }

        return Mathf.RoundToInt(baseDamage) + rawDamage;
    }

    /// <summary>
    /// Charged skill damage = raw charge damage
    ///                      + the weapon's primary damage modifier
    ///                      + 50% of the player's runtime base damage
    ///                        (base + attribute/trait scaling)
    ///                      + damage modifiers from other equipped gear.
    /// The weapon's own modifier is excluded from the gear pass since
    /// it is already added explicitly.
    /// </summary>
    private int CalculateChargedSkillDamage(int rawDamage)
    {
        int damage = rawDamage + data.GetDamage(DamageSlot.Primary);

        if (playerStats == null)
            return damage;

        float runtimeBase =
            playerStats.GetPreEquipmentDamage(data.PrimaryDamageType);

        if (EquipmentManager.Instance != null)
        {
            runtimeBase = EquipmentManager.Instance.GetModifiedStatExcluding(
                runtimeBase,
                StatType.Damage,
                data.PrimaryDamageType,
                data
            );
        }

        return damage + Mathf.RoundToInt(runtimeBase * 0.5f);
    }

    private int GetPrimaryDamage()
    {
        if (playerStats == null)
            return data.PrimaryDamage;

        float damage =
            playerStats.GetEffectiveDamage(
                data.PrimaryDamageType
            );

        // EquipmentManager only contributes this weapon's stat modifiers
        // when the weapon was equipped through it (e.g. from inventory).
        // A default weapon assigned directly bypasses the manager, so its
        // ItemData damage has to be added here.
        if (!IsWeaponRegisteredInEquipment())
            damage += data.PrimaryDamage;

        return Mathf.RoundToInt(damage);
    }

    private bool IsWeaponRegisteredInEquipment()
    {
        if (EquipmentManager.Instance == null)
            return false;

        InventoryItem equipped =
            EquipmentManager.Instance.GetEquippedItem(
                EquipmentType.Weapon
            );

        return equipped != null && equipped.Data == data;
    }

    private float GetCooldown(float baseCooldown)
    {
        float reduction = playerStats == null
            ? 0f
            : playerStats.CooldownReduction;

        return Mathf.Max(0f, baseCooldown - reduction);
    }

    // =========================================================
    // VISUAL
    // =========================================================

    private void CreateVisual(
        GameObject prefab,
        Vector3 position,
        float scale,
        float lifetime,
        float rotation = 0f)
    {
        if (prefab == null)
            return;

        GameObject visual =
            Instantiate(
                prefab,
                position,
                Quaternion.Euler(0f, 0f, rotation)
            );

        visual.transform.localScale =
            Vector3.one * scale;

        if (visual.GetComponent<SpriteArrayVisual>() == null && lifetime > 0f)
            Destroy(visual, lifetime);
    }

    // =========================================================
    // CHARGE CLEANUP
    // =========================================================

    private void AnimateMaxChargeVisual()
    {
        if (activeChargeVisual != null)
        {
            activeChargeVisual.transform.localScale =
                Vector3.one * data.MaxChargeVisualScale;
        }

        if (chargeVisualSprites == null ||
            chargeVisualSprites.Length == 0 ||
            data.MaxChargeSprites == null ||
            data.MaxChargeSprites.Length == 0 ||
            data.MaxChargeAnimationSpeed <= 0f)
        {
            return;
        }

        int frameIndex = Mathf.FloorToInt(
            (Time.time - fullChargeReachedTime) *
            data.MaxChargeAnimationSpeed
        ) % data.MaxChargeSprites.Length;

        Sprite frame = data.MaxChargeSprites[frameIndex];
        foreach (SpriteRenderer spriteRenderer in chargeVisualSprites)
        {
            if (spriteRenderer != null)
                spriteRenderer.sprite = frame;
        }
    }

    private void SpawnFullChargeIndicator()
    {
        if (fullyChargedIndicatorPrefab == null)
            return;

        Vector3 spawnPosition = firePoint == null
            ? transform.root.position
            : firePoint.position;

        activeFullChargeIndicator = Instantiate(
            fullyChargedIndicatorPrefab,
            spawnPosition,
            Quaternion.identity,
            firePoint == null ? transform.root : firePoint
        );

        // Gather all sprite renderers for animation
        indicatorSprites = activeFullChargeIndicator.GetComponentsInChildren<SpriteRenderer>();

        Debug.Log($"[WeaponController] {WeaponId} fully charged! Indicator spawned with {indicatorSprites.Length} sprite renderers.");
    }

    private void AnimateFullChargeIndicator()
    {
        if (activeFullChargeIndicator == null)
            return;

        // Rotation animation
        if (rotateIndicator)
        {
            activeFullChargeIndicator.transform.Rotate(
                0f,
                0f,
                indicatorRotationSpeed * Time.deltaTime
            );
        }

        // Pulse animation
        if (pulseIndicator && indicatorSprites != null && indicatorSprites.Length > 0)
        {
            indicatorPulseTimer += Time.deltaTime * indicatorPulseSpeed;
            float scale = Mathf.Lerp(
                indicatorPulseMin,
                indicatorPulseMax,
                (Mathf.Sin(indicatorPulseTimer) + 1f) * 0.5f
            );

            foreach (SpriteRenderer sprite in indicatorSprites)
            {
                if (sprite != null)
                {
                    sprite.transform.localScale = Vector3.one * scale;
                }
            }
        }
    }

    // =========================================================
    // CHARGE CLEANUP
    // =========================================================

    private void StopCharging()
    {
        isCharging = false;
        hasReachedFullCharge = false;

        if (activeChargeVisual != null)
            Destroy(activeChargeVisual);

        activeChargeVisual = null;

        if (activeFullChargeIndicator != null)
            Destroy(activeFullChargeIndicator);

        activeFullChargeIndicator = null;
        chargeVisualSprites = null;
        indicatorSprites = null;
    }

    // =========================================================
    // CONFIGURATION
    // =========================================================

    private bool IsConfigured()
    {
        if (data != null)
            return true;

        Debug.LogWarning(
            "WeaponController has not been configured with an ItemData asset."
        );

        return false;
    }
}