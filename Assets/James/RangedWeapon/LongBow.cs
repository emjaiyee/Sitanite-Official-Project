using UnityEngine;
using UnityEngine.InputSystem;

public class LongBow : RangedWeapon, IChargeableWeapon
{
    [Header("LongBow Skill - Charged Arrow")]

    [Header("Charge Visual")]
    [Tooltip("Visual shown while the LongBow is charging.")]
    [SerializeField] private GameObject chargeVisualPrefab;

    [Tooltip("Maximum scale of the charge visual.")]
    [SerializeField] private float maxChargeVisualScale = 1.5f;

    [Header("Charged Arrow")]
    [Tooltip("Special arrow prefab used only by the LongBow charged skill.")]
    [SerializeField] private GameObject chargedArrowPrefab;

    [Tooltip("Maximum distance the charged arrow can target.")]
    [SerializeField] private float skillRange = 14f;

    [Tooltip("Maximum amount of time the player can charge.")]
    [SerializeField] private float maxChargeTime = 2f;

    [Tooltip("Damage dealt with no charge.")]
    [SerializeField] private int minimumSkillDamage = 30;

    [Tooltip("Damage dealt at full charge.")]
    [SerializeField] private int maximumSkillDamage = 150;

    private float chargeStartTime;
    private bool isCharging;

    private GameObject activeChargeVisual;

    public override string WeaponId => "LongBow";


    // =========================================================
    // SKILL START
    // =========================================================

    public override void UseSkill()
    {
        if (isCharging)
            return;

        StartCharging();
    }

    private void StartCharging()
    {
        isCharging = true;

        chargeStartTime = Time.time;

        Debug.Log(
            "[LongBow] Started charging."
        );

        CreateChargeVisual();
    }


    // =========================================================
    // CHARGE
    // =========================================================

    private void Update()
    {
        if (!isCharging)
            return;

        float chargeTime =
            Time.time - chargeStartTime;

        float chargePercent =
            Mathf.Clamp01(
                chargeTime / maxChargeTime
            );

        UpdateChargeVisual(chargePercent);

        // Maximum charge is reached after maxChargeTime.
        if (chargeTime >= maxChargeTime)
        {
            Debug.Log(
                "[LongBow] Maximum charge reached."
            );
        }
    }


    // =========================================================
    // RELEASE
    // =========================================================

    public void ReleaseSkill()
    {
        if (!isCharging)
            return;

        float chargeTime =
            Time.time - chargeStartTime;

        float chargePercent =
            Mathf.Clamp01(
                chargeTime / maxChargeTime
            );

        int finalDamage =
            Mathf.RoundToInt(
                Mathf.Lerp(
                    minimumSkillDamage,
                    maximumSkillDamage,
                    chargePercent
                )
            );

        Debug.Log(
            $"[LongBow] Released! " +
            $"Charge: {chargePercent * 100f:F0}% | " +
            $"Damage: {finalDamage}"
        );

        FireChargedArrow(
            finalDamage
        );

        StopCharging();
    }


    // =========================================================
    // FIRE CHARGED ARROW
    // =========================================================

    private void FireChargedArrow(
        int finalDamage)
    {
        // Make sure the SPECIAL charged arrow exists.
        if (chargedArrowPrefab == null)
        {
            Debug.LogWarning(
                "[LongBow] Charged Arrow Prefab is not assigned."
            );

            return;
        }

        if (firePoint == null)
        {
            Debug.LogWarning(
                "[LongBow] Fire Point is not assigned."
            );

            return;
        }

        if (Mouse.current == null)
        {
            Debug.LogWarning(
                "[LongBow] Mouse input is unavailable."
            );

            return;
        }

        if (Camera.main == null)
        {
            Debug.LogWarning(
                "[LongBow] Main Camera could not be found."
            );

            return;
        }


        // =====================================================
        // GET MOUSE POSITION
        // =====================================================

        Vector3 mouseScreenPosition =
            Mouse.current.position.ReadValue();

        Vector3 mouseWorldPosition =
            Camera.main.ScreenToWorldPoint(
                mouseScreenPosition
            );

        mouseWorldPosition.z =
            firePoint.position.z;


        // =====================================================
        // CHECK SKILL RANGE
        // =====================================================

        float distanceToTarget =
            Vector2.Distance(
                transform.root.position,
                mouseWorldPosition
            );

        if (distanceToTarget > skillRange)
        {
            Debug.Log(
                $"[LongBow] Target is out of range. " +
                $"Distance: {distanceToTarget:F2} / " +
                $"Range: {skillRange:F2}"
            );

            return;
        }


        // =====================================================
        // CALCULATE DIRECTION
        // =====================================================

        Vector2 direction =
            (
                mouseWorldPosition -
                firePoint.position
            ).normalized;

        if (direction == Vector2.zero)
            return;


        // =====================================================
        // ROTATE ARROW TOWARD MOUSE
        // =====================================================

        float angle =
            Mathf.Atan2(
                direction.y,
                direction.x
            ) * Mathf.Rad2Deg;

        Quaternion rotation =
            Quaternion.Euler(
                0f,
                0f,
                angle
            );


        // =====================================================
        // SPAWN SPECIAL CHARGED ARROW
        // =====================================================

        GameObject projectile =
            Instantiate(
                chargedArrowPrefab,
                firePoint.position,
                rotation
            );


        // =====================================================
        // GET ARROW COMPONENT
        // =====================================================

        Arrow arrow =
            projectile.GetComponent<Arrow>();

        if (arrow == null)
        {
            Debug.LogError(
                "[LongBow] Charged Arrow prefab does not " +
                "contain an Arrow component."
            );

            Destroy(projectile);

            return;
        }


        // =====================================================
        // CALCULATE CHARGE PERCENT
        // =====================================================

        float chargePercent =
            Mathf.InverseLerp(
                minimumSkillDamage,
                maximumSkillDamage,
                finalDamage
            );


        // =====================================================
        // CHARGED PROJECTILE SPEED
        // =====================================================

        float chargedProjectileSpeed =
            projectileSpeed *
            Mathf.Lerp(
                1f,
                1.75f,
                chargePercent
            );


        // =====================================================
        // INITIALIZE ARROW
        // =====================================================

        arrow.Initialize(
            finalDamage,
            damageType,
            chargedProjectileSpeed,
            skillRange
        );


        Debug.Log(
            $"[LongBow] Charged arrow fired! " +
            $"Damage: {finalDamage} | " +
            $"Charge: {chargePercent * 100f:F0}%"
        );
    }


    // =========================================================
    // CHARGING VISUAL
    // =========================================================

    private void CreateChargeVisual()
    {
        if (chargeVisualPrefab == null)
            return;

        activeChargeVisual =
            Instantiate(
                chargeVisualPrefab,
                transform.root.position,
                Quaternion.identity
            );
    }

    private void UpdateChargeVisual(
        float chargePercent)
    {
        if (activeChargeVisual == null)
            return;

        float scale =
            Mathf.Lerp(
                1f,
                maxChargeVisualScale,
                chargePercent
            );

        activeChargeVisual.transform.localScale =
            Vector3.one * scale;
    }


    // =========================================================
    // STOP CHARGING
    // =========================================================

    private void StopCharging()
    {
        isCharging = false;

        if (activeChargeVisual != null)
        {
            Destroy(
                activeChargeVisual
            );

            activeChargeVisual = null;
        }
    }


    // =========================================================
    // GIZMOS
    // =========================================================

#if UNITY_EDITOR

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        Gizmos.DrawWireSphere(
            transform.root.position,
            skillRange
        );
    }

#endif
}