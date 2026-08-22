using UnityEngine;

// Base class for all melee weapons.
// Handles shared attack and damage logic.
// Specific weapons should inherit from this class and provide their WeaponId.
public abstract class MeleeWeapon : MonoBehaviour, IWeapon
{
    [Header("Attack Settings")]
    [SerializeField] protected int damage = 10;
    [SerializeField] protected float attackRange = 1f;
    [SerializeField] protected LayerMask hittableLayers;

    [Tooltip(
        "Point where the attack radius is centered. " +
        "Usually an empty child positioned in front of the player."
    )]
    [SerializeField] protected Transform attackPoint;

    // Every weapon must have a unique ID.
    public abstract string WeaponId { get; }

    public virtual void Attack()
    {
        Debug.Log($"{WeaponId} swing!");

        if (attackPoint == null)
        {
            Debug.LogWarning(
                $"{WeaponId}: attackPoint not assigned in the Inspector."
            );

            return;
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(
            attackPoint.position,
            attackRange,
            hittableLayers
        );

        foreach (Collider2D hit in hits)
        {
            if (hit == null)
                continue;

            // Look for IDamageable on the collider
            // or one of its parent objects.
            IDamageable target = hit.GetComponentInParent<IDamageable>();

            if (target != null)
            {
                target.TakeDamage(
                    damage,
                    DamageType.Physical
                );

                Debug.Log(
                    $"{WeaponId} hit {hit.name} for {damage} damage."
                );
            }
        }
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