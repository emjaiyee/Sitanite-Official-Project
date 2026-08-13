using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference attackAction;

    private PlayerEquipment equipment;

    private void Awake()
    {
        equipment =
            GetComponent<PlayerEquipment>();

        if (equipment == null)
        {
            Debug.LogWarning(
                "PlayerAttack could not find " +
                "a PlayerEquipment component."
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
    }

    private void OnAttackPerformed(
        InputAction.CallbackContext context)
    {
        Attack();
    }

    private void Attack()
    {
        if (
            equipment != null &&
            equipment.CurrentWeapon != null
        )
        {
            equipment.CurrentWeapon.Attack();
        }
        else
        {
            Debug.Log("No weapon equipped!");
        }
    }
}