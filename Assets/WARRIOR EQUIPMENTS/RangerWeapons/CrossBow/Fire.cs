using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fire : MonoBehaviour
{
    private int damage;
    private DamageType damageType;

    private float radius;
    private float duration;
    private float tickInterval;

    private LayerMask hittableLayers;

    private bool initialized;

    public void Initialize(
        int damage,
        DamageType damageType,
        float radius,
        float duration,
        float tickInterval,
        LayerMask hittableLayers)
    {
        this.damage = damage;
        this.damageType = damageType;
        this.radius = radius;
        this.duration = duration;
        this.tickInterval = Mathf.Max(0.01f, tickInterval);
        this.hittableLayers = hittableLayers;

        initialized = true;

        // Match the visual size to the damage area.
        transform.localScale = Vector3.one * (radius * 2f);

        StartCoroutine(FireRoutine());
    }

    private IEnumerator FireRoutine()
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            DamageEnemies();

            yield return new WaitForSeconds(tickInterval);

            elapsed += tickInterval;
        }

        Destroy(gameObject);
    }

    private void DamageEnemies()
    {
        if (!initialized)
            return;

        Collider2D[] hits =
            Physics2D.OverlapCircleAll(
                transform.position,
                radius,
                hittableLayers
            );

        // Prevent an enemy with multiple colliders from
        // receiving damage more than once per tick.
        HashSet<IDamageable> damagedTargets =
            new HashSet<IDamageable>();

        foreach (Collider2D hit in hits)
        {
            if (hit == null)
                continue;

            IDamageable target =
                hit.GetComponentInParent<EnemyHealth>();

            if (target == null)
                continue;

            if (damagedTargets.Contains(target))
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

            if (damage <= 0)
                continue;

            if (damageType == DamageType.None)
                continue;

            target.TakeDamage(
                damage,
                damageType,
                transform.position
            );

            damagedTargets.Add(target);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(
            transform.position,
            radius
        );
    }
}