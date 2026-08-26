using UnityEngine;

public class ShortBow : RangedWeapon
{
    [Header("ShortBow Skill - Arrow Rain")]

    [Tooltip("Maximum distance from the player where Arrow Rain can be placed.")]
    [SerializeField] private float skillRange = 8f;

    [Tooltip("Radius of the Arrow Rain AOE.")]
    [SerializeField] private float skillRadius = 2.5f;

    [Tooltip("Number of arrows that fall during Arrow Rain.")]
    [SerializeField] private int arrowCount = 12;

    [Tooltip("Damage dealt by each arrow.")]
    [SerializeField] private int skillDamage = 15;

    [Tooltip("Time between the first and last arrow falling.")]
    [SerializeField] private float rainDuration = 0.6f;

    [Tooltip("How long the visual AOE remains visible.")]
    [SerializeField] private float visualDuration = 0.8f;

    [Header("Skill Visual")]
    [SerializeField] private GameObject skillVisualPrefab;

    [Header("Skill Projectile")]
    [Tooltip(
        "Optional arrow prefab used for the visual Arrow Rain. " +
        "This should contain an Arrow component."
    )]
    [SerializeField] private GameObject skillArrowPrefab;

    public override string WeaponId => "ShortBow";

    // =========================================================
    // NORMAL ATTACK
    // =========================================================

    // Normal ShortBow attack uses RangedWeapon.Attack().
    // No override needed here.

    // =========================================================
    // ARROW RAIN SKILL
    // =========================================================

    public override void UseSkill()
    {
        if (Camera.main == null)
        {
            Debug.LogWarning(
                "[ShortBow] Main Camera could not be found."
            );

            return;
        }

        // Get mouse position.
        Vector3 mouseScreenPosition =
            UnityEngine.InputSystem.Mouse.current.position.ReadValue();

        Vector3 targetPosition =
            Camera.main.ScreenToWorldPoint(
                mouseScreenPosition
            );

        targetPosition.z =
            transform.root.position.z;

        // Check skill range.
        float distanceFromPlayer =
            Vector2.Distance(
                transform.root.position,
                targetPosition
            );

        if (distanceFromPlayer > skillRange)
        {
            Debug.Log(
                $"[ShortBow] Arrow Rain target is out of range. " +
                $"Distance: {distanceFromPlayer:F2} / " +
                $"Range: {skillRange:F2}"
            );

            return;
        }

        Debug.Log(
            $"[ShortBow] Arrow Rain triggered at " +
            $"{targetPosition}."
        );

        // Create the visual AOE.
        CreateSkillVisual(targetPosition);

        // Start the arrow rain.
        StartCoroutine(
            ArrowRain(targetPosition)
        );
    }

    // =========================================================
    // ARROW RAIN
    // =========================================================

    private System.Collections.IEnumerator ArrowRain(
        Vector3 targetPosition)
    {
        if (arrowCount <= 0)
            yield break;

        float delay = 0f;

        if (arrowCount > 1)
        {
            delay =
                rainDuration /
                (arrowCount - 1);
        }

        for (int i = 0; i < arrowCount; i++)
        {
            SpawnSkillArrow(targetPosition);

            if (i < arrowCount - 1)
                yield return new WaitForSeconds(delay);
        }
    }

    // =========================================================
    // SPAWN SKILL ARROW
    // =========================================================

    private void SpawnSkillArrow(
        Vector3 targetPosition)
    {
        // Random position inside the AOE.
        Vector2 randomOffset =
            Random.insideUnitCircle *
            skillRadius;

        Vector3 arrowPosition =
            targetPosition +
            new Vector3(
                randomOffset.x,
                randomOffset.y,
                0f
            );

        // If no arrow visual is assigned,
        // damage is still applied through the AOE.
        if (skillArrowPrefab != null)
        {
            GameObject arrow =
                Instantiate(
                    skillArrowPrefab,
                    arrowPosition +
                    Vector3.up * 2f,
                    Quaternion.Euler(
                        0f,
                        0f,
                        -90f
                    )
                );

            Destroy(
                arrow,
                0.4f
            );
        }

        // Damage enemies at this arrow's landing position.
        Collider2D[] hits =
            Physics2D.OverlapCircleAll(
                arrowPosition,
                0.35f,
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
                    skillDamage,
                    DamageType.Physical
                );

                Debug.Log(
                    $"[ShortBow] Arrow Rain hit " +
                    $"{hit.name} for {skillDamage} damage."
                );
            }
        }
    }

    // =========================================================
    // VISUAL AOE
    // =========================================================

    private void CreateSkillVisual(
        Vector3 targetPosition)
    {
        if (skillVisualPrefab == null)
            return;

        GameObject visual =
            Instantiate(
                skillVisualPrefab,
                targetPosition,
                Quaternion.identity
            );

        visual.transform.localScale =
            new Vector3(
                skillRadius * 2f,
                skillRadius * 2f,
                1f
            );

        Destroy(
            visual,
            visualDuration
        );
    }

#if UNITY_EDITOR

    private void OnDrawGizmosSelected()
    {
        // Normal attack range.
        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(
            transform.root.position,
            attackRange
        );

        // Arrow Rain skill range.
        Gizmos.color = Color.cyan;

        Gizmos.DrawWireSphere(
            transform.root.position,
            skillRange
        );
    }

#endif
}