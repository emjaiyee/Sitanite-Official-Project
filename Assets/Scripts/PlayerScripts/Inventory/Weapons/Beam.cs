using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class Beam : MonoBehaviour
{
    private const float AppearDuration = 0.12f;
    private const float PulseFrequency = 8f;
    private const float PulseAmount = 0.12f;

    [Header("Visual Animation")]
    [SerializeField] private Sprite[] beamSprites;
    [Min(1f)] [SerializeField] private float framesPerSecond = 12f;
    [SerializeField] private Light2D beamLight;

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
    private float tickInterval = 1f;
    private float damageScale = 1f;
    private LayerMask hittableLayers;
    private float endTime;
    private bool initialized;
    private Transform visualTransform;
    private SpriteRenderer visualRenderer;
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
        LayerMask hittableLayers,
        float ticksPerSecond = 1f)
    {
        // Damage values passed in are per-second amounts. Each tick deals a
        // fraction of that so total DPS stays the same regardless of tick rate.
        ticksPerSecond = Mathf.Max(0.1f, ticksPerSecond);
        tickInterval = 1f / ticksPerSecond;
        damageScale = tickInterval;

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
        visualRenderer = GetComponentInChildren<SpriteRenderer>();
        if (beamLight == null)
            beamLight = GetComponentInChildren<Light2D>();

        if (beamLight != null)
        {
            beamLight.lightType = Light2D.LightType.Sprite;
        }

        if (visualRenderer != null)
        {
            visualTransform = visualRenderer.transform;
            visualScale = visualTransform.localScale;
            visualTransform.localScale = new Vector3(0f, visualScale.y, visualScale.z);

            if (beamSprites != null && beamSprites.Length > 0)
            {
                visualRenderer.sprite = beamSprites[0];
                this.duration = beamSprites.Length / Mathf.Max(1f, framesPerSecond);
            }
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
            IDamageable target = hit == null
                ? null
                : hit.GetComponentInParent<EnemyHealth>();
            if (target == null)
                continue;

            if (!PlayerElevationLevel.CanAffectTarget(
                    (target as MonoBehaviour)?.transform))
                continue;

            if (!nextDamageTimes.TryGetValue(target, out float nextDamageTime) ||
                Time.time >= nextDamageTime)
            {
                ApplyDamage(target);
                nextDamageTimes[target] = Time.time + tickInterval;
            }
        }
    }

    private void AnimateVisual()
    {
        if (visualTransform == null)
            return;

        float elapsed = Time.time - startTime;
        if (beamSprites != null && beamSprites.Length > 0)
        {
            int frameIndex = Mathf.Min(
                Mathf.FloorToInt(elapsed * framesPerSecond),
                beamSprites.Length - 1
            );
            visualRenderer.sprite = beamSprites[frameIndex];
        }

        if (beamLight != null)
            beamLight.lightCookieSprite = visualRenderer.sprite;

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
        int primary = Mathf.RoundToInt(primaryDamage * damageScale);
        int secondary = Mathf.RoundToInt(secondaryDamage * damageScale);
        int tertiary = Mathf.RoundToInt(tertiaryDamage * damageScale);

        if (primary > 0 && primaryDamageType != DamageType.None)
            target.TakeDamage(primary, primaryDamageType, transform.position);

        if (secondary > 0 && secondaryDamageType != DamageType.None)
            target.TakeDamage(secondary, secondaryDamageType, transform.position);

        if (tertiary > 0 && tertiaryDamageType != DamageType.None)
            target.TakeDamage(tertiary, tertiaryDamageType, transform.position);
    }
}