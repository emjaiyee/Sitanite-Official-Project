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

    private bool isDashing;
    private float dashTime;

    // Last valid movement direction.
    private Vector2 lastMoveDirection = Vector2.right;

    // -------------------------------------------------
    // RAMP SUPPORT
    // -------------------------------------------------

    private bool overrideMovement;
    private Vector2 rampForward = Vector2.right;

    public bool IsDashing => isDashing;

    // -------------------------------------------------
    // UNITY
    // -------------------------------------------------

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        movement = GetComponent<PlayerWASD>();
        stats = GetComponent<PlayerStats>();

        if (movement == null)
        {
            Debug.LogError(
                "PlayerDash requires a PlayerWASD component."
            );
        }

        if (stats == null)
        {
            Debug.LogError(
                "PlayerDash requires a PlayerStats component."
            );
        }
    }

    private void OnEnable()
    {
        if (dashAction == null)
        {
            Debug.LogWarning(
                "PlayerDash has no Dash InputActionReference assigned."
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

        if (
            isDashing &&
            Time.time >= dashTime + stats.dashDuration
        )
        {
            EndDash();
        }
    }

    // -------------------------------------------------
    // MOVEMENT DIRECTION
    // -------------------------------------------------

    private void TrackMovementDirection()
    {
        if (movement == null)
            return;

        Vector2 currentMovement =
            movement.MoveDirection;

        if (currentMovement.sqrMagnitude > 0.0001f)
        {
            lastMoveDirection =
                currentMovement.normalized;
        }
    }

    // -------------------------------------------------
    // INPUT
    // -------------------------------------------------

    private void OnDashPerformed(
        InputAction.CallbackContext context)
    {
        StartDash();
    }

    // -------------------------------------------------
    // DASH
    // -------------------------------------------------

    private void StartDash()
    {
        if (isDashing)
            return;

        // Dead players cannot dash.
        if (stats != null && stats.IsDead)
            return;

        // ---------------------------------------------
        // STAMINA
        // ---------------------------------------------

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

        // ---------------------------------------------
        // DIRECTION
        // ---------------------------------------------

        Vector2 dashDirection =
            lastMoveDirection;

        // ---------------------------------------------
        // RAMP SUPPORT
        // ---------------------------------------------

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

        // ---------------------------------------------
        // START DASH
        // ---------------------------------------------

        isDashing = true;
        dashTime = Time.time;

        // Tell PlayerWASD to stop controlling the Rigidbody.
        if (movement != null)
        {
            movement.LockMovement();
        }

        // Clear existing movement.
        rb.linearVelocity = Vector2.zero;

        // Apply dash velocity.
        rb.linearVelocity =
            dashDirection * stats.dashSpeed;

        Debug.Log(
            $"[PlayerDash] Dash triggered! Direction: {dashDirection}"
        );
    }

    private void EndDash()
    {
        if (!isDashing)
            return;

        isDashing = false;

        // Stop dash velocity.
        rb.linearVelocity = Vector2.zero;

        // Give movement control back to PlayerWASD.
        if (movement != null)
        {
            movement.UnlockMovement();
        }

        Debug.Log(
            "[PlayerDash] Dash ended."
        );
    }

    // -------------------------------------------------
    // RAMP METHODS
    // -------------------------------------------------

    public void EnterRamp(Vector2 forward)
    {
        if (forward.sqrMagnitude <= 0.0001f)
            return;

        overrideMovement = true;
        rampForward = forward.normalized;

        Debug.Log(
            $"[PlayerDash] Entered ramp. Forward: {rampForward}"
        );
    }

    public void ExitRamp()
    {
        overrideMovement = false;

        Debug.Log(
            "[PlayerDash] Exited ramp."
        );
    }
}