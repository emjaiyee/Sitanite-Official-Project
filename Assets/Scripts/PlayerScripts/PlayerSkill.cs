using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSkill : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference skillAction;

    private PlayerStats stats;
    private PlayerEquipment equipment;
    private PlayerWASD movement;
    private PlayerDash dash;

    private bool skillActive;

    // -------------------------------------------------
    // UNITY
    // -------------------------------------------------

    private void Awake()
    {
        stats = GetComponent<PlayerStats>();
        equipment = GetComponent<PlayerEquipment>();
        movement = GetComponent<PlayerWASD>();
        dash = GetComponent<PlayerDash>();

        if (stats == null)
        {
            Debug.LogError(
                "PlayerSkill requires a PlayerStats component."
            );
        }

        if (equipment == null)
        {
            Debug.LogError(
                "PlayerSkill requires a PlayerEquipment component."
            );
        }

        if (movement == null)
        {
            Debug.LogError(
                "PlayerSkill requires a PlayerWASD component."
            );
        }

        if (dash == null)
        {
            Debug.LogError(
                "PlayerSkill requires a PlayerDash component."
            );
        }
    }

    private void OnEnable()
    {
        if (skillAction == null)
        {
            Debug.LogWarning(
                "PlayerSkill has no Skill InputActionReference assigned."
            );

            return;
        }

        skillAction.action.Enable();

        skillAction.action.started += OnSkillStarted;
        skillAction.action.canceled += OnSkillCanceled;
    }

    private void OnDisable()
    {
        if (skillAction == null)
            return;

        skillAction.action.started -= OnSkillStarted;
        skillAction.action.canceled -= OnSkillCanceled;

        skillAction.action.Disable();

        EndSkillMovementLock();
    }

    // -------------------------------------------------
    // PRESS F
    // -------------------------------------------------

    private void OnSkillStarted(
        InputAction.CallbackContext context)
    {
        StartWeaponSkill();
    }

    private void StartWeaponSkill()
    {
        if (skillActive)
            return;

        if (stats == null ||
            equipment == null)
            return;

        if (stats.IsDead)
            return;

        if (equipment.CurrentWeapon == null ||
            equipment.CurrentWeaponData == null ||
            equipment.CurrentWeaponData.EquipmentType != EquipmentType.Weapon)
        {
            Debug.Log(
                "[PlayerSkill] No weapon equipped."
            );

            return;
        }

        if (!equipment.CurrentWeapon.CanUseSkill)
            return;

        ItemData weaponData =
            equipment.CurrentWeaponData;

        if (!stats.UseResource(
            weaponData.SkillCost,
            weaponData.SkillResourceType))
        {
            Debug.Log(
                $"[PlayerSkill] Not enough " +
                $"{weaponData.SkillResourceType}."
            );

            return;
        }

        // ---------------------------------------------
        // GET MOUSE DIRECTION
        // ---------------------------------------------

        Vector2 skillDirection = GetMouseDirection();

        if (skillDirection.sqrMagnitude <= 0.0001f)
            return;

        // ---------------------------------------------
        // FACE PLAYER
        // ---------------------------------------------

        if (movement != null)
        {
            movement.FaceDirection(skillDirection);
            movement.LockFacingDirection();
        }

        // ---------------------------------------------
        // MOVEMENT LOCK
        // ---------------------------------------------

        skillActive = true;

        if (movement != null)
        {
            movement.LockMovement();
        }

        if (dash != null)
        {
            dash.LockDash();
        }

        // ---------------------------------------------
        // USE SKILL
        // ---------------------------------------------

        equipment.CurrentWeapon.UseSkill(skillDirection);
    }
    // -------------------------------------------------
    // RELEASE F
    // -------------------------------------------------

    private void OnSkillCanceled(
        InputAction.CallbackContext context)
    {
        ReleaseWeaponSkill();
    }

    private void ReleaseWeaponSkill()
    {
        if (!skillActive)
            return;

        if (equipment == null)
        {
            EndSkillMovementLock();
            return;
        }

        if (equipment.CurrentWeapon == null ||
            equipment.CurrentWeaponData == null ||
            equipment.CurrentWeaponData.EquipmentType !=
                EquipmentType.Weapon)
        {
            EndSkillMovementLock();
            return;
        }

        // -------------------------------------------------
        // CHARGEABLE WEAPON
        // -------------------------------------------------

        IChargeableWeapon chargeableWeapon =
            equipment.CurrentWeapon as IChargeableWeapon;

        if (chargeableWeapon != null)
        {
            chargeableWeapon.ReleaseSkill();
        }

        // -------------------------------------------------
        // RESTORE MOVEMENT
        // -------------------------------------------------

        EndSkillMovementLock();
    }

    // -------------------------------------------------
    // MOVEMENT LOCK
    // -------------------------------------------------

    private void EndSkillMovementLock()
    {
        if (!skillActive)
            return;

        skillActive = false;

        if (movement != null)
        {
            movement.UnlockMovement();
            movement.UnlockFacingDirection();
        }

        if (dash != null)
        {
            dash.UnlockDash();
        }
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
}