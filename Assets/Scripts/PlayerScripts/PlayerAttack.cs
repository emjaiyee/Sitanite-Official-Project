using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference attackAction;

    private PlayerEquipment equipment;
    private PlayerWASD movement;
    private PlayerDash dash;

    private bool attackActive;

    private void Awake()
    {
        equipment = GetComponent<PlayerEquipment>();
        movement = GetComponent<PlayerWASD>();
        dash = GetComponent<PlayerDash>();

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

        Vector2 attackDirection = GetMouseDirection();

        if (attackDirection.sqrMagnitude <= 0.0001f)
            return;

        // Face the player toward the mouse.
        movement.FaceDirection(attackDirection);

        // Lock movement during the attack.
        attackActive = true;
        movement.LockMovement();

        if (dash != null)
            dash.LockDash();

        // Pass the exact same direction to the weapon.
        equipment.CurrentWeapon.Attack(attackDirection);

        // Current attacks are instantaneous.
        EndAttackMovementLock();
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
            movement.UnlockMovement();

        if (dash != null)
            dash.UnlockDash();
    }
}