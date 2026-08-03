using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerWASD : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Input")]
    [SerializeField] private InputActionReference moveAction;

    [Header("Default Movement")]
    [SerializeField] private bool useIsometricMovement = true;

    private Rigidbody2D rb;
    private Vector2 input;
    private Vector2 movement;

    private bool overrideMovement = false;
    private Vector2 rampForward = Vector2.right;

    public Vector2 MoveDirection => movement;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    private void OnEnable()
    {
        moveAction.action.Enable();
    }

    private void OnDisable()
    {
        moveAction.action.Disable();
    }

    private void Update()
    {
        input = moveAction.action.ReadValue<Vector2>();
        input = Vector2.ClampMagnitude(input, 1f);

        Vector2 desiredMovement;

        if (useIsometricMovement)
        {
            desiredMovement = new Vector2(
                input.x - input.y,
                (input.x + input.y) * 0.5f
            ).normalized;
        }
        else
        {
            desiredMovement = input;
        }

        if (overrideMovement)
        {
            Vector2 forward = rampForward.normalized;

            // Keep only the movement along the ramp.
            float amount = Vector2.Dot(desiredMovement, forward);

            movement = forward * amount;
        }
        else
        {
            movement = desiredMovement;
        }

        if (movement.sqrMagnitude > 0.0001f)
            movement.Normalize();
    }

    private void FixedUpdate()
    {
        rb.MovePosition(
            rb.position + movement * moveSpeed * Time.fixedDeltaTime
        );
    }

    public void EnterRamp(Vector2 forward)
    {
        overrideMovement = true;
        rampForward = forward.normalized;
    }

    public void ExitRamp()
    {
        overrideMovement = false;
    }
}