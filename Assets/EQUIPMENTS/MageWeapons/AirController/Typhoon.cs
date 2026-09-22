using System.Collections.Generic;
using UnityEngine;

public class Typhoon : MonoBehaviour
{
    private readonly Dictionary<IDamageable, float> nextDamageTimes =
        new Dictionary<IDamageable, float>();

    private int primaryDamage;
    private DamageType primaryDamageType;
    private int secondaryDamage;
    private DamageType secondaryDamageType;
    private int tertiaryDamage;
    private DamageType tertiaryDamageType;
    private float radius;
    private float duration;
    private float tickInterval;
    private float damageScale;
    private float pullSpeed;
    private LayerMask hittableLayers;
    private float endTime;
    private bool initialized;

    [Header("Typhoon Behavior")]
    [Min(0f)]
    [SerializeField] private float pullSpeedMultiplier = 3f;

    public void Initialize(
        int primaryDamage,
        DamageType primaryDamageType,
        int secondaryDamage,
        DamageType secondaryDamageType,
        int tertiaryDamage,
        DamageType tertiaryDamageType,
        float radius,
        float duration,
        float damageTicksPerSecond,
        LayerMask hittableLayers)
    {
        this.primaryDamage = primaryDamage;
        this.primaryDamageType = primaryDamageType;
        this.secondaryDamage = secondaryDamage;
        this.secondaryDamageType = secondaryDamageType;
        this.tertiaryDamage = tertiaryDamage;
        this.tertiaryDamageType = tertiaryDamageType;
        this.radius = Mathf.Max(0.1f, radius);
        this.duration = Mathf.Max(0f, duration);
        tickInterval = 1f / Mathf.Max(0.1f, damageTicksPerSecond);
        damageScale = tickInterval;
        pullSpeed = this.radius * pullSpeedMultiplier;
        this.hittableLayers = hittableLayers;
        endTime = Time.time + this.duration;
        initialized = true;

        transform.localScale = Vector3.one * this.radius * 2f;
    }

    private void Update()
    {
        if (!initialized)
            return;

        if (Time.time >= endTime)
        {
            Destroy(gameObject);
            return;
        }

        Collider2D[] hits =
            Physics2D.OverlapCircleAll(
                transform.position,
                radius,
                hittableLayers
            );

        foreach (Collider2D hit in hits)
        {
            IDamageable target =
                hit == null
                    ? null
                    : hit.GetComponentInParent<IDamageable>();

            if (target == null)
                continue;

            Transform targetTransform =
                (target as MonoBehaviour)?.transform;

            if (targetTransform == null ||
                !PlayerElevationLevel.CanAffectTarget(targetTransform))
                continue;

            Vector2 toCenter =
                (Vector2)transform.position - (Vector2)targetTransform.position;

            if (toCenter.sqrMagnitude > 0.0001f)
            {
                targetTransform.position =
                    Vector2.MoveTowards(
                        targetTransform.position,
                        transform.position,
                        pullSpeed * Time.deltaTime
                    );
            }

            if (!nextDamageTimes.TryGetValue(target, out float nextDamageTime) ||
                Time.time >= nextDamageTime)
            {
                ApplyDamage(target);
                nextDamageTimes[target] = Time.time + tickInterval;
            }
        }
    }

    private void ApplyDamage(IDamageable target)
    {
        int scaledPrimaryDamage =
            Mathf.RoundToInt(primaryDamage * damageScale);
        int scaledSecondaryDamage =
            Mathf.RoundToInt(secondaryDamage * damageScale);
        int scaledTertiaryDamage =
            Mathf.RoundToInt(tertiaryDamage * damageScale);

        if (scaledPrimaryDamage > 0 && primaryDamageType != DamageType.None)
            target.TakeDamage(
                scaledPrimaryDamage,
                primaryDamageType,
                transform.position
            );

        if (scaledSecondaryDamage > 0 && secondaryDamageType != DamageType.None)
            target.TakeDamage(
                scaledSecondaryDamage,
                secondaryDamageType,
                transform.position
            );

        if (scaledTertiaryDamage > 0 && tertiaryDamageType != DamageType.None)
            target.TakeDamage(
                scaledTertiaryDamage,
                tertiaryDamageType,
                transform.position
            );
    }
}
