using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference attackAction;

    [Header("Attack Recovery")]
    [Min(0f)]
    [SerializeField] private float attackMovementLockDuration = 0.2f;

    private PlayerEquipment equipment;
    private PlayerWASD movement;
    private PlayerDash dash;
    private PlayerStats stats;
    private PlayerAnimationController animationController;

    private bool attackActive;
    private Coroutine attackRecovery;


    private void Awake()
    {
        equipment = GetComponent<PlayerEquipment>();
        movement = GetComponent<PlayerWASD>();
        dash = GetComponent<PlayerDash>();
        stats = GetComponent<PlayerStats>();
        animationController = GetComponent<PlayerAnimationController>();

        if (equipment == null)
        {
            Debug.LogWarning(
                "PlayerAttack could not find " +
                "a PlayerEquipment component."
            );
        }

        if (movement == null)
        {
            Debug.LogWarning(
                "PlayerAttack could not find " +
                "a PlayerWASD component."
            );
        }

        if (dash == null)
        {
            Debug.LogWarning(
                "PlayerAttack could not find " +
                "a PlayerDash component."
            );
        }

        if (animationController == null)
        {
            Debug.LogWarning(
                "PlayerAttack could not find " +
                "a PlayerAnimationController component."
            );
        }
    }


    private void OnEnable()
    {
        if (attackAction == null)
        {
            Debug.LogWarning(
                "PlayerAttack has no Attack InputActionReference assigned."
            );

            return;
        }

        attackAction.action.Enable();
        attackAction.action.performed += OnAttackPerformed;
    }


    private void OnDisable()
    {
        if (attackAction == null)
            return;

        attackAction.action.performed -= OnAttackPerformed;
        attackAction.action.Disable();

        EndAttackMovementLock();
    }


    private void OnAttackPerformed(
        InputAction.CallbackContext context)
    {
        Attack();
    }


    private void Attack()
    {
        if (attackActive)
            return;


        if (equipment == null ||
            equipment.CurrentWeapon == null ||
            equipment.CurrentWeaponData == null ||
            equipment.CurrentWeaponData.EquipmentType != EquipmentType.Weapon)
        {
            Debug.Log("No weapon equipped!");
            return;
        }


        if (!equipment.CurrentWeapon.CanAttack)
            return;


        if (movement == null)
            return;


        ItemData weaponData =
            equipment.CurrentWeaponData;


        if (weaponData.AttackResourceType != ResourceType.None &&
            weaponData.AttackCost > 0 &&
            (stats == null ||
             !stats.UseResource(
                 weaponData.AttackCost,
                 weaponData.AttackResourceType)))
        {
            Debug.Log(
                $"[PlayerAttack] Not enough " +
                $"{weaponData.AttackResourceType} to attack."
            );

            return;
        }


        Vector2 attackDirection =
            GetMouseDirection();


        if (attackDirection.sqrMagnitude <= 0.0001f)
            return;


        // Face player toward mouse
        movement.FaceDirection(attackDirection);


        // Start attack state
        attackActive = true;

        movement.LockMovement();
        movement.LockFacingDirection();


        // Play attack animation
        if (animationController != null)
        {
            animationController.PlayAttack();
        }


        if (dash != null)
            dash.LockDash();


        // Send attack direction to weapon
        equipment.CurrentWeapon.Attack(attackDirection);


        attackRecovery = StartCoroutine(
            EndAttackMovementLockAfterDelay()
        );
    }


    private Vector2 GetMouseDirection()
    {
        if (Mouse.current == null ||
            Camera.main == null)
        {
            return Vector2.zero;
        }


        Vector3 mousePosition =
            Camera.main.ScreenToWorldPoint(
                Mouse.current.position.ReadValue()
            );


        Vector2 direction =
            (Vector2)(mousePosition - transform.position);


        if (direction.sqrMagnitude <= 0.0001f)
            return Vector2.zero;


        return direction.normalized;
    }


    private void EndAttackMovementLock()
    {
        if (!attackActive)
            return;


        attackActive = false;


        if (movement != null)
        {
            movement.UnlockMovement();
            movement.UnlockFacingDirection();
        }


        if (dash != null)
            dash.UnlockDash();
    }


    private IEnumerator EndAttackMovementLockAfterDelay()
    {
        yield return new WaitForSeconds(
            attackMovementLockDuration
        );


        attackRecovery = null;

        EndAttackMovementLock();
    }
}