using UnityEngine;

public interface IDamageable
{
    /// <param name="damageSource">World position the damage came from, if known.</param>
    void TakeDamage(int amount, DamageType damageType = DamageType.Slash, Vector3? damageSource = null);
}

public interface ICriticalDamageable
{
    void TakeCriticalDamage(
        int amount,
        DamageType damageType,
        Vector3? damageSource,
        WeaponAttackType attackType);
}

public static class PlayerDamage
{
    public static void PlayCriticalHitVisual(
        IDamageable target,
        GameObject visualPrefab,
        Vector3? source)
    {
        if (target is not Component targetComponent || visualPrefab == null)
            return;

        Transform targetTransform = targetComponent.transform;
        Vector2 direction = source.HasValue
            ? (Vector2)(targetTransform.position - source.Value)
            : Vector2.right;
        float angle = direction.sqrMagnitude > 0.0001f
            ? Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg
            : 0f;
        GameObject visual = Object.Instantiate(
            visualPrefab,
            targetTransform.position,
            Quaternion.Euler(0f, 0f, angle),
            targetTransform
        );

        SpriteArrayVisual spriteArrayVisual =
            visual.GetComponentInChildren<SpriteArrayVisual>();
        if (spriteArrayVisual == null)
        {
            Debug.LogWarning(
                $"{visualPrefab.name} needs a SpriteArrayVisual component.",
                targetComponent
            );
            Object.Destroy(visual);
            return;
        }

        visual.transform.localScale = spriteArrayVisual.CounterEffectScale;

        SpriteRenderer targetRenderer =
            targetTransform.GetComponentInChildren<SpriteRenderer>();
        if (targetRenderer == null)
            return;

        foreach (SpriteRenderer visualRenderer in
                 visual.GetComponentsInChildren<SpriteRenderer>())
        {
            visualRenderer.sortingLayerID = targetRenderer.sortingLayerID;
            visualRenderer.sortingOrder = targetRenderer.sortingOrder + 1;
        }
    }

    public static void Deal(
        IDamageable target,
        int amount,
        DamageType type,
        Vector3? source,
        WeaponAttackType attackType,
        bool isCriticalHit = false)
    {
        if (isCriticalHit && target is ICriticalDamageable criticalTarget)
        {
            criticalTarget.TakeCriticalDamage(
                amount,
                type,
                source,
                attackType
            );
            return;
        }

        if (target is EnemyHealth enemy)
        {
            PlayerSkill playerSkill = Player.Instance != null
                ? Player.Instance.GetComponent<PlayerSkill>()
                : null;

            enemy.TakePlayerDamage(
                amount,
                type,
                source,
                attackType,
                playerSkill != null && playerSkill.IsSkillActive
            );
        }
        else
            target.TakeDamage(amount, type, source);
    }
}
 