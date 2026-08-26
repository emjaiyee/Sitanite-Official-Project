using UnityEngine;

public abstract class MeleeWeapon : MonoBehaviour, IWeapon
{
    [Header("Attack Settings")]
    [SerializeField] protected int damage = 10;
    [SerializeField] protected float attackRange = 1f;
    [SerializeField] protected LayerMask hittableLayers;

    [Tooltip("Point where the attack radius is centered.")]
    [SerializeField] protected Transform attackPoint;

    public abstract string WeaponId { get; }

    public virtual void Attack()
    {
        Debug.Log($"{WeaponId} swing!");

        if (attackPoint == null)
        {
            Debug.LogWarning(
                $"{WeaponId}: attackPoint not assigned."
            );

            return;
        }

        Collider2D[] hits =
            Physics2D.OverlapCircleAll(
                attackPoint.position,
                attackRange,
                hittableLayers
            );

        foreach (Collider2D hit in hits)
        {
            if (hit == null)
                continue;

            IDamageable target =
                hit.GetComponentInParent<IDamageable>();

            if (target != null)
            {
                target.TakeDamage(
                    damage,
                    DamageType.Physical
                );
            }
        }
    }

    public virtual void UseSkill()
    {
        Debug.Log(
            $"{WeaponId} does not have a skill yet."
        );
    }

#if UNITY_EDITOR

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null)
            return;

        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(
            attackPoint.position,
            attackRange
        );
    }

#endif
}