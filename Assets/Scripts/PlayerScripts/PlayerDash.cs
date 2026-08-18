using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerDash : MonoBehaviour
{
    [Header("Dash Settings")]
    [SerializeField] private float dashSpeed = 10f;
    [SerializeField] private float dashDuration = 0.2f;

    [Header("Stamina")]
    [SerializeField] private int dashCost = 20;

    [Header("Input")]
    [SerializeField] private InputActionReference dashAction;

    private Rigidbody2D rb;
    private PlayerWASD movement;
    private PlayerStats stats;

    private bool isDashing;
    private float dashTime;

    // Last valid movement direction.
    private Vector2 lastMoveDirection = Vector2.right;

    // Ramp support
    private bool overrideMovement;
    private Vector2 rampForward = Vector2.right;

    public bool IsDashing => isDashing;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        movement = GetComponent<PlayerWASD>();
        stats = GetComponent<PlayerStats>();

        if (movement == null)
            Debug.LogError("PlayerDash requires a PlayerWASD component.");

        if (stats == null)
            Debug.LogError("PlayerDash requires a PlayerStats component.");
    }

    private void OnEnable()
    {
        if (dashAction == null)
        {
            Debug.LogWarning("PlayerDash has no Dash InputActionReference assigned.");
            return;
        }

        dashAction.action.Enable();
        dashAction.action.performed += OnDashPerformed;
    }

    private void OnDisable()
    {
        if (dashAction == null) return;

        dashAction.action.performed -= OnDashPerformed;
        dashAction.action.Disable();
    }

    private void Update()
    {
        // Track latest movement direction
        if (movement != null)
        {
            Vector2 currentMovement = movement.MoveDirection;
            if (currentMovement.sqrMagnitude > 0.0001f)
                lastMoveDirection = currentMovement.normalized;
        }

        // End dash after duration
        if (isDashing && Time.time >= dashTime + dashDuration)
            EndDash();
    }

    private void OnDashPerformed(InputAction.CallbackContext context)
    {
        StartDash();
    }

    private void StartDash()
    {
        if (isDashing) return;

        // ✅ Stamina check before dash
        if (stats != null && !stats.UseStamina(dashCost))
        {
            Debug.Log("[PlayerDash] Not enough stamina.");
            return;
        }

        Vector2 dashDirection = lastMoveDirection;

        // Ramp support
        if (overrideMovement)
        {
            Vector2 forward = rampForward.normalized;
            float amount = Vector2.Dot(dashDirection, forward);
            dashDirection = forward * amount;
        }

        if (dashDirection.sqrMagnitude <= 0.0001f) return;
        dashDirection.Normalize();

        // ✅ Start dash
        isDashing = true;
        dashTime = Time.time;

        if (movement != null) movement.LockMovement();

        rb.linearVelocity = Vector2.zero; // clear existing movement
        rb.linearVelocity = dashDirection * dashSpeed;

        Debug.Log($"[PlayerDash] Dash triggered! Direction: {dashDirection}");
    }

    private void EndDash()
    {
        isDashing = false;
        rb.linearVelocity = Vector2.zero;

        if (movement != null) movement.UnlockMovement();

        Debug.Log("[PlayerDash] Dash ended.");
    }

    // Ramp methods
    public void EnterRamp(Vector2 forward)
    {
        overrideMovement = true;
        rampForward = forward.normalized;
        Debug.Log($"[PlayerDash] Entered ramp. Forward: {rampForward}");
    }

    public void ExitRamp()
    {
        overrideMovement = false;
        Debug.Log("[PlayerDash] Exited ramp.");
    }
}
