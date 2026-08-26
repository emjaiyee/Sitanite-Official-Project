using UnityEngine;

public class LongSword : MeleeWeapon
{
    [Header("Long Sword Skill")]
    [SerializeField] private float skillRadius = 2f;
    [SerializeField] private int skillDamage = 50;
    [SerializeField] private LayerMask skillHittableLayers;
    [SerializeField] private GameObject skillVisualPrefab;

    public override string WeaponId => "LongSword";

    // =========================================================
    // WEAPON SKILL
    // =========================================================

    public override void UseSkill()
    {
        Collider2D[] hits =
            Physics2D.OverlapCircleAll(
                transform.root.position,
                skillRadius,
                skillHittableLayers
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
                    skillDamage,
                    DamageType.Physical
                );

                Debug.Log(
                    $"[LongSword] Skill hit {hit.name} " +
                    $"for {skillDamage} damage."
                );
            }
        }

        if (skillVisualPrefab != null)
        {
            GameObject visual =
                Instantiate(
                    skillVisualPrefab,
                    transform.root.position,
                    Quaternion.identity
                );

            visual.transform.localScale =
                new Vector3(
                    skillRadius * 2f,
                    skillRadius * 2f,
                    1f
                );

            Destroy(visual, 0.5f);
        }

        Debug.Log("[LongSword] Skill triggered!");
    }

#if UNITY_EDITOR

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(
            transform.root.position,
            skillRadius
        );
    }

#endif
}