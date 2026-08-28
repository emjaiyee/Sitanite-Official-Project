using UnityEngine;
using UnityEngine.InputSystem;

public class Dagger : MeleeWeapon
{
    [Header("Dagger Skill - Stab")]

    [Tooltip("Maximum distance from the player to find a target.")]
    [SerializeField] private float skillRange = 3f;

    [Tooltip("Damage dealt by the Dagger skill.")]
    [SerializeField] private int skillDamage = 50;

    [Tooltip("How close an enemy must be to the mouse cursor to be selected.")]
    [SerializeField] private float targetRadius = 1f;

    [Header("Skill Visual")]

    [Tooltip("Optional visual effect spawned when the stab is performed.")]
    [SerializeField] private GameObject skillVisualPrefab;

    public override string WeaponId => "Dagger";


    // =========================================================
    // SKILL
    // =========================================================

    public override void UseSkill()
    {
        Transform target = FindTarget();

        if (target == null)
        {
            Debug.Log(
                "[Dagger] No valid target found."
            );

            return;
        }

        PerformStab(target);
    }


    // =========================================================
    // FIND TARGET
    // =========================================================

    private Transform FindTarget()
    {
        if (Mouse.current == null)
        {
            Debug.LogWarning(
                "[Dagger] Mouse input unavailable."
            );

            return null;
        }

        if (Camera.main == null)
        {
            Debug.LogWarning(
                "[Dagger] Main Camera could not be found."
            );

            return null;
        }

        // Get mouse position in world space.
        Vector3 mouseScreenPosition =
            Mouse.current.position.ReadValue();

        Vector3 mouseWorldPosition =
            Camera.main.ScreenToWorldPoint(
                mouseScreenPosition
            );

        mouseWorldPosition.z =
            transform.root.position.z;


        // Find all enemies inside the skill range.
        Collider2D[] hits =
            Physics2D.OverlapCircleAll(
                transform.root.position,
                skillRange,
                hittableLayers
            );


        Transform closestTarget = null;

        float closestDistance =
            float.MaxValue;


        foreach (Collider2D hit in hits)
        {
            if (hit == null)
                continue;

            IDamageable damageable =
                hit.GetComponentInParent<IDamageable>();

            if (damageable == null)
                continue;


            // Distance between enemy and mouse.
            float mouseDistance =
                Vector2.Distance(
                    hit.transform.position,
                    mouseWorldPosition
                );


            // Enemy must be close enough to mouse.
            if (mouseDistance > targetRadius)
                continue;


            // Pick the closest valid target to the mouse.
            if (mouseDistance < closestDistance)
            {
                closestDistance = mouseDistance;
                closestTarget = hit.transform;
            }
        }

        return closestTarget;
    }


    // =========================================================
    // PERFORM STAB
    // =========================================================

    private void PerformStab(
        Transform target)
    {
        if (target == null)
            return;


        IDamageable damageable =
            target.GetComponentInParent<IDamageable>();


        if (damageable == null)
        {
            Debug.LogWarning(
                "[Dagger] Target is not damageable."
            );

            return;
        }


        damageable.TakeDamage(
            skillDamage,
            DamageType.Physical
        );


        Debug.Log(
            $"[Dagger] Stabbed {target.name} " +
            $"for {skillDamage} damage."
        );


        // Optional visual effect.
        if (skillVisualPrefab != null)
        {
            GameObject visual =
                Instantiate(
                    skillVisualPrefab,
                    target.position,
                    Quaternion.identity
                );

            Destroy(
                visual,
                0.25f
            );
        }
    }


    // =========================================================
    // GIZMOS
    // =========================================================

#if UNITY_EDITOR

    private void OnDrawGizmosSelected()
    {
        if (transform.root == null)
            return;

        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(
            transform.root.position,
            skillRange
        );
    }

#endif
}