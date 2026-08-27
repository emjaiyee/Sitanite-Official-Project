using System.Collections.Generic;
using UnityEngine;

public class Beam : MonoBehaviour
{
    private const float TickInterval = 1f;
    private const float AppearDuration = 0.12f;
    private const float PulseFrequency = 8f;
    private const float PulseAmount = 0.12f;

    private readonly Dictionary<IDamageable, float> nextDamageTimes =
        new Dictionary<IDamageable, float>();

    private int primaryDamage;
    private DamageType primaryDamageType;
    private int secondaryDamage;
    private DamageType secondaryDamageType;
    private int tertiaryDamage;
    private DamageType tertiaryDamageType;
    private float range;
    private float width;
    private float duration;
    private LayerMask hittableLayers;
    private float endTime;
    private bool initialized;
    private Transform visualTransform;
    private Vector3 visualScale;
    private float startTime;

    public void Initialize(
        int primaryDamage,
        DamageType primaryDamageType,
        int secondaryDamage,
        DamageType secondaryDamageType,
        int tertiaryDamage,
        DamageType tertiaryDamageType,
        float range,
        float width,
        float duration,
        LayerMask hittableLayers)
    {
        this.primaryDamage = primaryDamage;
        this.primaryDamageType = primaryDamageType;
        this.secondaryDamage = secondaryDamage;
        this.secondaryDamageType = secondaryDamageType;
        this.tertiaryDamage = tertiaryDamage;
        this.tertiaryDamageType = tertiaryDamageType;
        this.range = Mathf.Max(0f, range);
        this.width = Mathf.Max(0.01f, width);
        this.duration = Mathf.Max(0f, duration);
        this.hittableLayers = hittableLayers;
        visualTransform = GetComponentInChildren<SpriteRenderer>()?.transform;
        if (visualTransform != null)
        {
            visualScale = visualTransform.localScale;
            visualTransform.localScale = new Vector3(0f, visualScale.y, visualScale.z);
        }

        startTime = Time.time;
        endTime = Time.time + this.duration;
        initialized = true;
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

        AnimateVisual();

        Collider2D[] hits = Physics2D.OverlapCapsuleAll(
            transform.position + transform.right * (range * 0.5f),
            new Vector2(range, width),
            CapsuleDirection2D.Horizontal,
            transform.eulerAngles.z,
            hittableLayers
        );

        foreach (Collider2D hit in hits)
        {
            IDamageable target = hit == null ? null : hit.GetComponentInParent<IDamageable>();
            if (target == null)
                continue;

            if (!nextDamageTimes.TryGetValue(target, out float nextDamageTime) ||
                Time.time >= nextDamageTime)
            {
                ApplyDamage(target);
                nextDamageTimes[target] = Time.time + TickInterval;
            }
        }
    }

    private void AnimateVisual()
    {
        if (visualTransform == null)
            return;

        float elapsed = Time.time - startTime;
        float appearPercent = Mathf.Clamp01(elapsed / AppearDuration);
        float pulse = 1f + Mathf.Sin(elapsed * PulseFrequency) * PulseAmount;
        visualTransform.localScale = new Vector3(
            visualScale.x * appearPercent,
            visualScale.y * pulse,
            visualScale.z
        );
    }

    private void ApplyDamage(IDamageable target)
    {
        if (primaryDamage > 0 && primaryDamageType != DamageType.None)
            target.TakeDamage(primaryDamage, primaryDamageType);

        if (secondaryDamage > 0 && secondaryDamageType != DamageType.None)
            target.TakeDamage(secondaryDamage, secondaryDamageType);

        if (tertiaryDamage > 0 && tertiaryDamageType != DamageType.None)
            target.TakeDamage(tertiaryDamage, tertiaryDamageType);
    }
}