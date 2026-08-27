using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSkill : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference skillAction;

    [Header("Stamina")]
    [SerializeField] private int skillCost = 25;

    private PlayerStats stats;
    private PlayerEquipment equipment;

    private void Awake()
    {
        stats = GetComponent<PlayerStats>();
        equipment = GetComponent<PlayerEquipment>();

        if (stats == null)
        {
            Debug.LogError(
                "PlayerSkill requires PlayerStats."
            );
        }

        if (equipment == null)
        {
            Debug.LogError(
                "PlayerSkill requires PlayerEquipment."
            );
        }
    }

    private void OnEnable()
    {
        if (skillAction == null)
        {
            Debug.LogWarning(
                "PlayerSkill has no Skill InputActionReference."
            );

            return;
        }

        skillAction.action.Enable();

        // Button pressed.
        skillAction.action.started += OnSkillStarted;

        // Button released.
        skillAction.action.canceled += OnSkillCanceled;
    }

    private void OnDisable()
    {
        if (skillAction == null)
            return;

        skillAction.action.started -= OnSkillStarted;
        skillAction.action.canceled -= OnSkillCanceled;

        skillAction.action.Disable();
    }

    // =========================================================
    // PRESS F
    // =========================================================

    private void OnSkillStarted(
        InputAction.CallbackContext context)
    {
        StartWeaponSkill();
    }

    private void StartWeaponSkill()
    {
        if (equipment == null)
            return;

        if (equipment.CurrentWeapon == null)
        {
            Debug.Log(
                "[PlayerSkill] No weapon equipped."
            );

            return;
        }

        // Check stamina when the skill starts.
        if (stats != null &&
            !stats.UseStamina(skillCost))
        {
            Debug.Log(
                "[PlayerSkill] Not enough stamina."
            );

            return;
        }

        equipment.CurrentWeapon.UseSkill();
    }

    // =========================================================
    // RELEASE F
    // =========================================================

    private void OnSkillCanceled(
        InputAction.CallbackContext context)
    {
        ReleaseWeaponSkill();
    }

    private void ReleaseWeaponSkill()
    {
        if (equipment == null)
            return;

        if (equipment.CurrentWeapon == null)
            return;

        // Check if the current weapon supports charging.
        IChargeableWeapon chargeableWeapon =
            equipment.CurrentWeapon as IChargeableWeapon;

        if (chargeableWeapon != null)
        {
            chargeableWeapon.ReleaseSkill();
        }
    }
}