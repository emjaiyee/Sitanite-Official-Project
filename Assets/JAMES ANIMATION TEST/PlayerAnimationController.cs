using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerAnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerWASD playerMovement;
    [SerializeField] private CharacterSpriteController characterSpriteController;


    [Header("Movement Parameters")]
    [SerializeField] private string speedParameter = "Speed";
    [SerializeField] private string moveXParameter = "MoveX";
    [SerializeField] private string moveYParameter = "MoveY";
    [SerializeField] private string runningParameter = "Running";


    [Header("Combat Parameters")]
    [SerializeField] private string attackParameter = "Attack";
    [SerializeField] private string skillParameter = "Skill";

    // MUST MATCH ANIMATOR PARAMETER NAME
    [SerializeField] private string weaponTypeParameter = "WeaponType";


    [Header("Other Parameters")]
    [SerializeField] private string dashParameter = "Dash";
    [SerializeField] private string deathParameter = "Death";



    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();


        if (playerMovement == null)
            playerMovement = GetComponent<PlayerWASD>();

        if (characterSpriteController == null)
            characterSpriteController = GetComponent<CharacterSpriteController>();
    }





    private void Update()
    {
        UpdateMovementAnimation();
    }





    private void UpdateMovementAnimation()
    {
        if (playerMovement == null)
            return;


        Vector2 movement =
            playerMovement.MoveDirection;


        Vector2 facing =
            playerMovement.FacingDirection;



        animator.SetFloat(
            speedParameter,
            movement.magnitude
        );


        animator.SetFloat(
            moveXParameter,
            facing.x
        );


        animator.SetFloat(
            moveYParameter,
            facing.y
        );

        animator.SetBool(
            runningParameter,
            playerMovement.IsSprinting
        );
    }







    // =====================================
    // COMBAT
    // =====================================


    public void PlayAttack()
    {
        animator.SetTrigger(
            attackParameter
        );

        if (characterSpriteController != null)
            characterSpriteController.PlayAttack();
    }





    public void PlaySkill()
    {
        animator.SetTrigger(
            skillParameter
        );

        if (characterSpriteController != null)
            characterSpriteController.PlaySkill();
    }





    // Weapon switching
    // 0 = Melee
    // 1 = Ranged
    // 2 = Spell

    public void SetWeaponType(
        WeaponAttackType type)
    {
        animator.SetInteger(
            weaponTypeParameter,
            (int)type
        );


        Debug.Log(
            $"[PlayerAnimation] Weapon Type: {type}"
        );
    }







    // =====================================
    // MOVEMENT ACTIONS
    // =====================================


    public void PlayDash()
    {
        animator.SetTrigger(
            dashParameter
        );

        if (characterSpriteController != null)
            characterSpriteController.PlayDash();
    }






    public void PlayDeath()
    {
        animator.SetBool(
            deathParameter,
            true
        );

        if (characterSpriteController != null)
            characterSpriteController.PlayDeath();
    }
}