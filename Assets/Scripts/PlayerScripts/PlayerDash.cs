using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerDash : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference dashAction;


    private Rigidbody2D rb;
    private PlayerWASD movement;
    private PlayerStats stats;
    private PlayerAnimationController animationController;


    private bool isDashing;
    private float dashTime;


    // Last valid movement direction
    private Vector2 lastMoveDirection = Vector2.right;


    // Ramp support
    private bool overrideMovement;
    private Vector2 rampForward = Vector2.right;


    // Skill dash lock
    private bool dashLocked;


    public bool IsDashing => isDashing;
    public bool IsDashLocked => dashLocked;



    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        movement = GetComponent<PlayerWASD>();
        stats = GetComponent<PlayerStats>();
        animationController = GetComponent<PlayerAnimationController>();


        if (movement == null)
            Debug.LogError("PlayerDash requires PlayerWASD.");


        if (stats == null)
            Debug.LogError("PlayerDash requires PlayerStats.");


        if (animationController == null)
            Debug.LogWarning(
                "PlayerDash could not find PlayerAnimationController."
            );
    }



    private void OnEnable()
    {
        if (dashAction == null)
        {
            Debug.LogWarning(
                "PlayerDash has no Dash InputActionReference."
            );

            return;
        }


        dashAction.action.Enable();
        dashAction.action.performed += OnDashPerformed;
    }



    private void OnDisable()
    {
        if (dashAction == null)
            return;


        dashAction.action.performed -= OnDashPerformed;
        dashAction.action.Disable();
    }




    private void Update()
    {
        TrackMovementDirection();


        if (isDashing &&
            stats != null &&
            Time.time >= dashTime + stats.dashDuration)
        {
            EndDash();
        }
    }





    private void TrackMovementDirection()
    {
        if (movement == null)
            return;


        Vector2 currentMovement = movement.MoveDirection;


        if (currentMovement.sqrMagnitude > 0.0001f)
        {
            lastMoveDirection =
                currentMovement.normalized;
        }
    }





    private void OnDashPerformed(
        InputAction.CallbackContext context)
    {
        StartDash();
    }






    private void StartDash()
    {
        if (dashLocked)
            return;


        if (isDashing)
            return;


        if (stats != null && stats.IsDead)
            return;




        // Consume stamina
        if (stats != null)
        {
            if (!stats.UseStamina(stats.dashCost))
            {
                Debug.Log(
                    "[PlayerDash] Not enough stamina."
                );

                return;
            }
        }





        Vector2 dashDirection =
            lastMoveDirection;




        // Ramp movement
        if (overrideMovement)
        {
            Vector2 forward =
                rampForward.normalized;


            float amount =
                Vector2.Dot(
                    dashDirection,
                    forward
                );


            dashDirection =
                forward * amount;
        }




        if (dashDirection.sqrMagnitude <= 0.0001f)
            return;



        dashDirection.Normalize();



        // START DASH

        isDashing = true;
        dashTime = Time.time;



        if (movement != null)
        {
            movement.LockMovement();
        }



        rb.linearVelocity = Vector2.zero;



        if (stats != null)
        {
            rb.linearVelocity =
                dashDirection * stats.DashSpeed;
        }



        // Tell animator
        if(animationController != null)
        {
            animationController.PlayDash();
        }
        PlayerSoundHelper sound =
    GetComponent<PlayerSoundHelper>();

if(sound != null)
{
    sound.PlayDash();
}


        Debug.Log(
            $"[PlayerDash] Dash triggered! Direction: {dashDirection}"
        );
    }






    private void EndDash()
    {
        if (!isDashing)
            return;


        isDashing = false;


        rb.linearVelocity = Vector2.zero;



        if (movement != null)
        {
            movement.UnlockMovement();
        }



        Debug.Log(
            "[PlayerDash] Dash ended."
        );
    }





    public void LockDash()
    {
        dashLocked = true;


        if(isDashing)
        {
            EndDash();
        }
    }





    public void UnlockDash()
    {
        dashLocked = false;
    }






    public void EnterRamp(Vector2 forward)
    {
        if(forward.sqrMagnitude <= 0.0001f)
            return;


        overrideMovement = true;
        rampForward = forward.normalized;
    }





    public void ExitRamp()
    {
        overrideMovement = false;
    }
}