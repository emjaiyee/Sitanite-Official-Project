using UnityEngine;
using UnityEngine.InputSystem;

public abstract class RangedWeapon : MonoBehaviour, IWeapon
{
    [Header("Attack Settings")]
    [SerializeField] protected int damage = 10;
    [SerializeField] protected float attackRange = 8f;

    [Header("Projectile")]
    [SerializeField] protected GameObject arrowPrefab;
    [SerializeField] protected Transform firePoint;
    [SerializeField] protected float projectileSpeed = 12f;

    [Header("Damage")]
    [SerializeField] protected DamageType damageType = DamageType.Physical;
    [Header("Layers")]
[SerializeField] protected LayerMask hittableLayers;

    public abstract string WeaponId { get; }

    // =========================================================
    // NORMAL ATTACK
    // =========================================================

    public virtual void Attack()
    {
        if (arrowPrefab == null)
        {
            Debug.LogWarning(
                $"{WeaponId}: Arrow prefab is not assigned."
            );

            return;
        }

        if (firePoint == null)
        {
            Debug.LogWarning(
                $"{WeaponId}: Fire Point is not assigned."
            );

            return;
        }

        if (Mouse.current == null)
        {
            Debug.LogWarning(
                $"{WeaponId}: Mouse input is not available."
            );

            return;
        }

        if (Camera.main == null)
        {
            Debug.LogWarning(
                $"{WeaponId}: Main Camera could not be found."
            );

            return;
        }

        // Get mouse position in world space.
        Vector3 mouseScreenPosition =
            Mouse.current.position.ReadValue();

        Vector3 mouseWorldPosition =
            Camera.main.ScreenToWorldPoint(
                mouseScreenPosition
            );

        mouseWorldPosition.z =
            firePoint.position.z;

        // Check weapon range.
        float distanceToMouse =
            Vector2.Distance(
                transform.root.position,
                mouseWorldPosition
            );

        if (distanceToMouse > attackRange)
        {
            Debug.Log(
                $"{WeaponId}: Target is out of range. " +
                $"Distance: {distanceToMouse:F2} / " +
                $"Range: {attackRange:F2}"
            );

            return;
        }

        // Calculate direction from fire point to mouse.
        Vector2 direction =
            (
                mouseWorldPosition -
                firePoint.position
            ).normalized;

        if (direction == Vector2.zero)
            return;

        // Calculate rotation toward mouse.
        float angle =
            Mathf.Atan2(
                direction.y,
                direction.x
            ) * Mathf.Rad2Deg;

        Quaternion rotation =
            Quaternion.Euler(
                0f,
                0f,
                angle
            );

        // Spawn arrow facing the mouse.
        GameObject projectile =
            Instantiate(
                arrowPrefab,
                firePoint.position,
                rotation
            );

        Arrow arrow =
            projectile.GetComponent<Arrow>();

        if (arrow == null)
        {
            Debug.LogError(
                $"{WeaponId}: Arrow prefab does not contain " +
                "an Arrow component."
            );

            Destroy(projectile);
            return;
        }

        arrow.Initialize(
            damage,
            damageType,
            projectileSpeed,
            attackRange
        );

        Debug.Log(
            $"{WeaponId} fired toward mouse."
        );
    }

    // =========================================================
    // WEAPON SKILL
    // =========================================================

    // Temporary default skill.
    // ShortBow and LongBow will override this later.
    public virtual void UseSkill()
    {
        Debug.LogWarning(
            $"{WeaponId}: No ranged skill has been implemented yet."
        );
    }
}