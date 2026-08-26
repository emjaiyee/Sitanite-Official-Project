using UnityEngine;

public class BattleAxe : MeleeWeapon
{
    [Header("Battle Axe Skill")]
    [SerializeField] private float skillRadius = 1.5f;
    [SerializeField] private int skillDamage = 80;
    [SerializeField] private LayerMask skillHittableLayers;
    [SerializeField] private GameObject skillVisualPrefab;

    public override string WeaponId => "BattleAxe";

    // =========================================================
    // WEAPON SKILL
    // =========================================================

    public override void UseSkill()
    {
        float slamRadius =
            skillRadius * 0.75f;

        int slamDamage =
            skillDamage;

        Collider2D[] hits =
            Physics2D.OverlapCircleAll(
                transform.root.position,
                slamRadius,
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
                    slamDamage,
                    DamageType.Physical
                );

                Debug.Log(
                    $"[BattleAxe] Slam hit {hit.name} " +
                    $"for {slamDamage} damage."
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
                    slamRadius * 2f,
                    slamRadius * 2f,
                    1f
                );

            Destroy(visual, 0.5f);
        }

        Debug.Log("[BattleAxe] Skill triggered!");
    }

#if UNITY_EDITOR

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(
            transform.root.position,
            skillRadius * 0.75f
        );
    }

#endif
}