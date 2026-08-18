using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference attackAction;

    private PlayerEquipment equipment;
    private PlayerStats playerStats; // ✅ added reference for future stamina/mana checks

    private void Awake()
    {
        equipment = GetComponent<PlayerEquipment>();
        playerStats = GetComponent<PlayerStats>(); // ✅ grab PlayerStats

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

    private void OnAttackPerformed(InputAction.CallbackContext context)
    {
        Attack();
    }

    private void Attack()
    {
        // ✅ Normal melee attack does NOT consume stamina
        if (equipment != null && equipment.CurrentWeapon != null)
        {
            equipment.CurrentWeapon.Attack();
        }
        else
        {
            Debug.Log("No weapon equipped!");
        }
    }
}
