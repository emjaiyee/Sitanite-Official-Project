using UnityEngine;

// Basic sword. Attach to a child GameObject under the player (e.g. "Sword").
public class MeleeWeapon : MonoBehaviour, IWeapon
{
    [Header("Sword Settings")]
    [SerializeField] private int damage = 10;
    [SerializeField] private float attackRange = 1f;
    [SerializeField] private LayerMask hittableLayers;

    [Tooltip(
        "Point the swing radius is centered on - " +
        "usually an empty child in front of the player."
    )]
    [SerializeField] private Transform attackPoint;


    public void Attack()
    {
        Debug.Log("Sword swing!");


        if (attackPoint == null)
        {
            Debug.LogWarning(
                "MeleeWeapon: attackPoint not assigned in the Inspector."
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


            // Find the damageable component on the collider
            // or one of its parent objects.
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