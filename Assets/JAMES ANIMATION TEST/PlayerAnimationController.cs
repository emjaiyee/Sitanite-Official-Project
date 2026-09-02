using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerAnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerWASD playerMovement;


    [Header("Movement Parameters")]
    [SerializeField] private string speedParameter = "Speed";
    [SerializeField] private string moveXParameter = "MoveX";
    [SerializeField] private string moveYParameter = "MoveY";


    [Header("Combat Parameters")]
    [SerializeField] private string attackParameter = "Attack";
    [SerializeField] private string skillParameter = "Skill";

    // MUST MATCH ANIMATOR PARAMETER NAME
    [SerializeField] private string weaponTypeParameter = "WeaponT";


    [Header("Other Parameters")]
    [SerializeField] private string dashParameter = "Dash";
    [SerializeField] private string deathParameter = "Death";



    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();


        if (playerMovement == null)
            playerMovement = GetComponent<PlayerWASD>();
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
    }







    // =====================================
    // COMBAT
    // =====================================


    public void PlayAttack()
    {
        animator.SetTrigger(
            attackParameter
        );
    }





    public void PlaySkill()
    {
        animator.SetTrigger(
            skillParameter
        );
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
    }






    public void PlayDeath()
    {
        animator.SetBool(
            deathParameter,
            true
        );
    }
}