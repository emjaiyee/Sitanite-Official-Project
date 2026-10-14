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
    private PlayerAttributesNTraits playerAttributes;
    private PlayerAnimationController animationController;
    private CharacterRenderer characterRenderer;
    private PlayerSkill playerSkill;
    private PlayerShieldDefense shieldDefense;

    private bool attackActive;
    private Coroutine attackRecovery;


    private void Awake()
    {
        equipment = GetComponent<PlayerEquipment>();
        movement = GetComponent<PlayerWASD>();
        dash = GetComponent<PlayerDash>();
        stats = GetComponent<PlayerStats>();
        playerAttributes = GetComponent<PlayerAttributesNTraits>();
        animationController = GetComponent<PlayerAnimationController>();
        characterRenderer = GetComponent<CharacterRenderer>();
        playerSkill = GetComponent<PlayerSkill>();
        shieldDefense = GetComponent<PlayerShieldDefense>();

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
        if (!CombatInputGate.IsCombatInputAllowed)
            return;

        if (Time.timeScale == 0f ||
            (GameManager.Instance != null && GameManager.Instance.IsPaused) ||
            (PauseMenuUI.Instance != null && PauseMenuUI.Instance.IsOpen))
        {
            return;
        }

        if (playerSkill != null && playerSkill.IsTargetingSkill)
            return;

        bool isCounterAttack =
            shieldDefense != null &&
            shieldDefense.HasCounterOpportunity;

        if (Attack(
                isCounterAttack ? shieldDefense.CounterDamageMultiplier : 1f,
                isCounterAttack) &&
            isCounterAttack)
        {
            shieldDefense.ConsumeCounterOpportunity();
            characterRenderer?.PlayCounterAttackFlash(
                attackMovementLockDuration * 2f
            );
            characterRenderer?.PlayCounterAttackWeaponVisual();
        }
    }


    private bool Attack(float damageMultiplier, bool isCounterAttack)
    {
        if (!CombatInputGate.IsCombatInputAllowed)
            return false;

        if (attackActive)
            return false;


        if (equipment == null ||
            equipment.CurrentWeapon == null ||
            equipment.CurrentWeaponData == null ||
            equipment.CurrentWeaponData.EquipmentType != EquipmentType.Weapon)
        {
            Debug.Log("No weapon equipped!");
            return false;
        }


        if (!equipment.CurrentWeapon.CanAttack)
            return false;


        if (movement == null)
            return false;


        ItemData weaponData =
            equipment.CurrentWeaponData;

        Vector2 attackDirection =
            GetMouseDirection();

        if (attackDirection.sqrMagnitude <= 0.0001f)
            return false;

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

            return false;
        }

        float effectiveDamageMultiplier = damageMultiplier;
        bool isCriticalHit = false;
        if (!isCounterAttack &&
            playerAttributes != null &&
            Random.value < playerAttributes.BasicAttackCriticalChance)
        {
            effectiveDamageMultiplier *= 2f;
            isCriticalHit = true;
        }

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
        equipment.CurrentWeapon.Attack(
            attackDirection,
            effectiveDamageMultiplier,
            isCounterAttack,
            isCriticalHit,
            characterRenderer != null
                ? characterRenderer.CriticalHitVisualPrefab
                : null
        );


        attackRecovery = StartCoroutine(
            EndAttackMovementLockAfterDelay(isCounterAttack)
        );

        return true;
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


    private IEnumerator EndAttackMovementLockAfterDelay(
        bool isCounterAttack)
    {
        yield return new WaitForSeconds(
            attackMovementLockDuration
        );

        if (isCounterAttack && animationController != null)
            animationController.PlayAttack();

        if (isCounterAttack)
        {
            yield return new WaitForSeconds(
                attackMovementLockDuration
            );
        }

        attackRecovery = null;

        EndAttackMovementLock();
    }
}