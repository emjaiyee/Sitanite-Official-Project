using UnityEngine;
using UnityEngine.InputSystem;

public class CameraAim : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Input")]
    [SerializeField] private InputActionReference lookAction;
    [SerializeField] private InputActionReference cameraLookAction;

    [Header("Follow")]
    [SerializeField] private float smoothTime = 0.15f;

    [Header("Aim")]
    [SerializeField] private float maxAimDistance = 5f;

    private Camera cam;
    private Vector3 velocity;

    private void Awake()
    {
        cam = Camera.main;
    }

    private void OnEnable()
    {
        lookAction.action.Enable();
        cameraLookAction.action.Enable();
    }

    private void OnDisable()
    {
        lookAction.action.Disable();
        cameraLookAction.action.Disable();
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 desiredPosition = target.position;

        if (cameraLookAction.action.IsPressed())
        {
            Vector2 mouseScreen = lookAction.action.ReadValue<Vector2>();

            Vector3 mouseWorld = cam.ScreenToWorldPoint(
                new Vector3(mouseScreen.x, mouseScreen.y, -cam.transform.position.z)
            );

            Vector2 offset = mouseWorld - target.position;

            // Prevent the camera from going infinitely far.
            offset = Vector2.ClampMagnitude(offset, maxAimDistance);

            desiredPosition += (Vector3)offset;
        }

        desiredPosition.z = transform.position.z;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref velocity,
            smoothTime
        );
    }
}