using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(PlayerStats))]
public class PlayerWASD : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference sprintAction;

    [Header("Default Movement")]
    [SerializeField] private bool useIsometricMovement = true;

    [Header("Diagonal Input")]
    [Min(0f)]
    [SerializeField] private float diagonalAxisScale = 0.5f;

    private Rigidbody2D rb;
    private PlayerStats stats;

    private Vector2 input;
    private Vector2 movement;

    private bool overrideMovement;
    private Vector2 rampForward = Vector2.right;

    // PlayerDash can temporarily take control of the Rigidbody.
    private bool movementLocked;

    // Current direction the player is facing.
    private Vector2 facingDirection = Vector2.down;
    private bool facingLocked;

    public Vector2 MoveDirection => movement;

    public Vector2 FacingDirection => facingDirection;

    public float SpeedMultiplier { get; set; } = 1f;

    public bool IsMovementLocked => movementLocked;

    public bool IsSprinting { get; private set; }

    // -------------------------------------------------
    // UNITY
    // -------------------------------------------------

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        stats = GetComponent<PlayerStats>();

        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode =
            CollisionDetectionMode2D.Continuous;
    }

    private void OnEnable()
    {
        if (moveAction != null)
            moveAction.action.Enable();

        if (sprintAction != null)
            sprintAction.action.Enable();
    }

    private void OnDisable()
    {
        if (moveAction != null)
            moveAction.action.Disable();

        if (sprintAction != null)
            sprintAction.action.Disable();

        IsSprinting = false;
    }

    private void Update()
    {
        ReadMovementInput();
        UpdateSprintState();
    }

    // -------------------------------------------------
    // MOVEMENT INPUT
    // -------------------------------------------------

    private void ReadMovementInput()
    {
        if (moveAction == null)
        {
            input = Vector2.zero;
            movement = Vector2.zero;
            return;
        }

        input = moveAction.action.ReadValue<Vector2>();
        input = Vector2.ClampMagnitude(input, 1f);

        bool diagonalInput = HasDiagonalInput(input);

        Vector2 desiredMovement =
            useIsometricMovement && diagonalInput
                ? GetGridDiagonalMovement(input)
                : input;

        Vector2 rampMovement = input;

        if (overrideMovement)
        {
            Vector2 forward =
                rampForward.normalized;

            float amount =
                Vector2.Dot(
                    rampMovement,
                    forward
                );

            movement =
                forward * amount;
        }
        else
        {
            movement = desiredMovement;
        }

        if (movement.sqrMagnitude > 0.0001f)
        {
            movement.Normalize();

            // Store the last direction the player moved.
            if (!facingLocked)
                facingDirection = movement;
        }
    }

    private Vector2 GetGridDiagonalMovement(Vector2 rawInput)
    {
        Vector2 desiredMovement = new Vector2(
            rawInput.x,
            rawInput.y * diagonalAxisScale
        );

        if (desiredMovement.sqrMagnitude > 0.0001f)
        {
            desiredMovement.Normalize();
        }

        return desiredMovement;
    }

    private bool HasDiagonalInput(Vector2 rawInput)
    {
        return Mathf.Abs(rawInput.x) > 0.0001f &&
            Mathf.Abs(rawInput.y) > 0.0001f;
    }

    private Vector2 GetIsometricMovement(Vector2 rawInput)
    {
        Vector2 desiredMovement = new Vector2(
            rawInput.x - rawInput.y,
            (rawInput.x + rawInput.y) * 0.5f
        );

        if (desiredMovement.sqrMagnitude > 0.0001f)
        {
            desiredMovement.Normalize();
        }

        return desiredMovement;
    }

    // -------------------------------------------------
    // SPRINT
    // -------------------------------------------------

    private void UpdateSprintState()
    {
        bool sprintHeld =
            sprintAction != null &&
            sprintAction.action.IsPressed();

        bool isMoving =
            movement.sqrMagnitude > 0.0001f;

        bool hasStamina =
            stats != null &&
            stats.CurrentStamina > 0;

        IsSprinting =
            sprintHeld &&
            isMoving &&
            hasStamina;
    }

    // -------------------------------------------------
    // PHYSICS
    // -------------------------------------------------

    private void FixedUpdate()
    {
        // PlayerDash or another system currently controls
        // the Rigidbody.
        if (movementLocked)
            return;

        float currentSpeed =
            IsSprinting
                ? stats.SprintSpeed
                : stats.MoveSpeed;

        rb.MovePosition(
            rb.position +
            movement *
            currentSpeed *
            SpeedMultiplier *
            Time.fixedDeltaTime
        );
    }

    // -------------------------------------------------
    // MOVEMENT LOCK
    // -------------------------------------------------

    public void LockMovement()
    {
        movementLocked = true;
        IsSprinting = false;

        // Clear active movement so the player doesn't
        // continue moving after the skill starts.
        movement = Vector2.zero;
    }

    public void UnlockMovement()
    {
        movementLocked = false;
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
    }

    public void ExitRamp()
    {
        overrideMovement = false;
    }

    // -------------------------------------------------
    // DIRECTION
    // -------------------------------------------------

    private CharacterDirection lastDirection =
        CharacterDirection.South;

    public CharacterDirection GetCurrentDirection()
    {
        // Use the persistent facing direction rather than
        // the current movement input.
        if (facingDirection.sqrMagnitude <= 0.0001f)
            return lastDirection;

        float angle =
            Mathf.Atan2(
                facingDirection.y,
                facingDirection.x
            ) * Mathf.Rad2Deg;

        if (angle < 0)
        {
            angle += 360;
        }

        if (angle >= 22.5f && angle < 67.5f)
        {
            lastDirection =
                CharacterDirection.NorthEast;
        }
        else if (angle >= 67.5f && angle < 112.5f)
        {
            lastDirection =
                CharacterDirection.North;
        }
        else if (angle >= 112.5f && angle < 157.5f)
        {
            lastDirection =
                CharacterDirection.NorthWest;
        }
        else if (angle >= 157.5f && angle < 202.5f)
        {
            lastDirection =
                CharacterDirection.West;
        }
        else if (angle >= 202.5f && angle < 247.5f)
        {
            lastDirection =
                CharacterDirection.SouthWest;
        }
        else if (angle >= 247.5f && angle < 292.5f)
        {
            lastDirection =
                CharacterDirection.South;
        }
        else if (angle >= 292.5f && angle < 337.5f)
        {
            lastDirection =
                CharacterDirection.SouthEast;
        }
        else
        {
            lastDirection =
                CharacterDirection.East;
        }

        return lastDirection;
    }

    // -------------------------------------------------
    // FORCE FACING DIRECTION
    // -------------------------------------------------

    public void FaceDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude <= 0.0001f)
            return;

        facingDirection = direction.normalized;

        // Update the character's 8-direction facing state.
        GetCurrentDirection();
    }

    public void LockFacingDirection()
    {
        facingLocked = true;
    }

    public void UnlockFacingDirection()
    {
        facingLocked = false;
    }
}