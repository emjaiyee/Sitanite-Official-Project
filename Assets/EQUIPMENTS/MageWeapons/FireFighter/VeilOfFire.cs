using System.Collections.Generic;
using UnityEngine;

public class VeilOfFire : MonoBehaviour
{
    private readonly Dictionary<IDamageable, float> nextDamageTimes =
        new Dictionary<IDamageable, float>();

    private int primaryDamage;
    private int secondaryDamage;
    private DamageType secondaryDamageType;
    private int tertiaryDamage;
    private DamageType tertiaryDamageType;
    private float radius;
    private float tickInterval;
    private float damageScale;
    private float endTime;
    private LayerMask hittableLayers;
    private bool initialized;

    public void Initialize(
        int primaryDamage,
        int secondaryDamage,
        DamageType secondaryDamageType,
        int tertiaryDamage,
        DamageType tertiaryDamageType,
        float radius,
        float duration,
        float damageTicksPerSecond,
        LayerMask hittableLayers,
        Transform player)
    {
        this.primaryDamage = Mathf.Max(0, primaryDamage);
        this.secondaryDamage = Mathf.Max(0, secondaryDamage);
        this.secondaryDamageType = secondaryDamageType;
        this.tertiaryDamage = Mathf.Max(0, tertiaryDamage);
        this.tertiaryDamageType = tertiaryDamageType;
        this.radius = Mathf.Max(0.1f, radius);
        tickInterval = 1f / Mathf.Max(0.1f, damageTicksPerSecond);
        damageScale = tickInterval;
        this.hittableLayers = hittableLayers;
        endTime = Time.time + Mathf.Max(0f, duration);
        initialized = player != null;

        if (!initialized)
            return;

        transform.SetParent(player, false);
        transform.localPosition = Vector3.zero;
        transform.localScale = Vector3.one * this.radius * 2f;

        foreach (Collider2D collider in GetComponentsInChildren<Collider2D>())
            collider.enabled = false;

        foreach (Rigidbody2D body in GetComponentsInChildren<Rigidbody2D>())
            body.simulated = false;
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

        HashSet<IDamageable> targets = new HashSet<IDamageable>();

        foreach (Collider2D hit in hits)
        {
            IDamageable target =
                hit == null
                    ? null
                    : hit.GetComponentInParent<IDamageable>();

            if (target == null || !targets.Add(target))
                continue;

            Transform targetTransform =
                (target as MonoBehaviour)?.transform;

            if (targetTransform == null ||
                !PlayerElevationLevel.CanAffectTarget(targetTransform))
                continue;

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

        if (scaledPrimaryDamage > 0)
            target.TakeDamage(
                scaledPrimaryDamage,
                DamageType.Fire,
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
