using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSkill : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference skillAction;


    [Header("Skill Recovery")]
    [Min(0f)]
    [SerializeField] private float skillMovementLockDuration = 0.2f;



    private PlayerStats stats;
    private PlayerEquipment equipment;
    private PlayerWASD movement;
    private PlayerDash dash;
    private PlayerAnimationController animationController;



    private bool skillActive;

    private IChargeableWeapon activeChargeable;

    private Coroutine skillRecovery;




    private void Awake()
    {
        stats = GetComponent<PlayerStats>();
        equipment = GetComponent<PlayerEquipment>();
        movement = GetComponent<PlayerWASD>();
        dash = GetComponent<PlayerDash>();

        animationController =
            GetComponent<PlayerAnimationController>();


        if(stats == null)
            Debug.LogError(
                "PlayerSkill requires PlayerStats."
            );


        if(equipment == null)
            Debug.LogError(
                "PlayerSkill requires PlayerEquipment."
            );


        if(movement == null)
            Debug.LogError(
                "PlayerSkill requires PlayerWASD."
            );


        if(dash == null)
            Debug.LogError(
                "PlayerSkill requires PlayerDash."
            );


        if(animationController == null)
            Debug.LogWarning(
                "PlayerSkill could not find PlayerAnimationController."
            );
    }





    private void OnEnable()
    {
        if(skillAction == null)
        {
            Debug.LogWarning(
                "PlayerSkill has no Skill InputActionReference."
            );

            return;
        }


        skillAction.action.Enable();

        skillAction.action.started += OnSkillStarted;
        skillAction.action.canceled += OnSkillCanceled;
    }





    private void OnDisable()
    {
        if(skillAction == null)
            return;


        skillAction.action.started -= OnSkillStarted;
        skillAction.action.canceled -= OnSkillCanceled;


        skillAction.action.Disable();


        EndSkillMovementLock();
    }





    private void Update()
    {
        if(!skillActive ||
           activeChargeable == null)
            return;


        Vector2 direction =
            GetMouseDirection();


        if(direction.sqrMagnitude <= 0.0001f)
            return;


        activeChargeable.UpdateSkillDirection(
            direction
        );


        if(movement != null)
            movement.FaceDirection(direction);
    }





    private void OnSkillStarted(
        InputAction.CallbackContext context)
    {
        StartWeaponSkill();
    }





    private void StartWeaponSkill()
    {
        if(skillActive)
            return;


        if(stats == null ||
           equipment == null)
            return;


        if(stats.IsDead)
            return;



        if(equipment.CurrentWeapon == null ||
           equipment.CurrentWeaponData == null ||
           equipment.CurrentWeaponData.EquipmentType != EquipmentType.Weapon)
        {
            Debug.Log(
                "[PlayerSkill] No weapon equipped."
            );

            return;
        }



        if(!equipment.CurrentWeapon.CanUseSkill)
            return;



        ItemData weaponData =
            equipment.CurrentWeaponData;



        if(!stats.UseResource(
            weaponData.SkillCost,
            weaponData.SkillResourceType))
        {
            Debug.Log(
                "[PlayerSkill] Not enough resource."
            );

            return;
        }





        Vector2 skillDirection =
            GetMouseDirection();



        if(skillDirection.sqrMagnitude <= 0.0001f)
            return;





        if(movement != null)
        {
            movement.FaceDirection(
                skillDirection
            );

            movement.LockFacingDirection();
        }





        skillActive = true;



        if(movement != null)
            movement.LockMovement();



        if(dash != null)
            dash.LockDash();





        // PLAY SKILL ANIMATION
        if(animationController != null)
        {
            animationController.PlaySkill();
        }





        activeChargeable =
            IsChargedSkill(weaponData)
            ?
            equipment.CurrentWeapon as IChargeableWeapon
            :
            null;





        equipment.CurrentWeapon.UseSkill(
            skillDirection
        );





        if(activeChargeable == null)
        {
            skillRecovery =
                StartCoroutine(
                    EndSkillMovementLockAfterDelay()
                );
        }
    }







    private void OnSkillCanceled(
        InputAction.CallbackContext context)
    {
        ReleaseWeaponSkill();
    }





    private void ReleaseWeaponSkill()
    {
        if(!skillActive)
            return;


        if(equipment == null)
        {
            EndSkillMovementLock();
            return;
        }



        IChargeableWeapon chargeableWeapon =
            equipment.CurrentWeapon as IChargeableWeapon;



        if(chargeableWeapon != null)
        {
            bool fullyCharged =
                chargeableWeapon.ChargePercent >= 1f;


            if(fullyCharged)
                TryConsumeMaxChargeCost();


            chargeableWeapon.ReleaseSkill(
                fullyCharged
            );
        }



        activeChargeable = null;



        if(skillRecovery == null)
        {
            skillRecovery =
                StartCoroutine(
                    EndSkillMovementLockAfterDelay()
                );
        }
    }






    private bool TryConsumeMaxChargeCost()
    {
        ItemData weaponData =
            equipment.CurrentWeaponData;


        if(weaponData == null)
            return true;



        int extraCost =
            weaponData.MaxChargeSkillCost -
            weaponData.SkillCost;



        if(extraCost <= 0)
            return true;



        return stats.UseResource(
            extraCost,
            weaponData.SkillResourceType
        );
    }





    private void EndSkillMovementLock()
    {
        if(!skillActive)
            return;


        skillActive = false;

        activeChargeable = null;



        if(movement != null)
        {
            movement.UnlockMovement();
            movement.UnlockFacingDirection();
        }



        if(dash != null)
            dash.UnlockDash();
    }





    private IEnumerator EndSkillMovementLockAfterDelay()
    {
        yield return new WaitForSeconds(
            skillMovementLockDuration
        );


        skillRecovery = null;

        EndSkillMovementLock();
    }





    private bool IsChargedSkill(ItemData weaponData)
    {
        return weaponData.WeaponSkillType == WeaponSkillType.ChargedArrow ||
               weaponData.WeaponSkillType == WeaponSkillType.Beam;
    }





    private Vector2 GetMouseDirection()
    {
        if(Mouse.current == null ||
           Camera.main == null)
        {
            return Vector2.zero;
        }



        Vector3 mousePosition =
            Camera.main.ScreenToWorldPoint(
                Mouse.current.position.ReadValue()
            );



        Vector2 direction =
            mousePosition - transform.position;



        if(direction.sqrMagnitude <= 0.0001f)
            return Vector2.zero;



        return direction.normalized;
    }
}