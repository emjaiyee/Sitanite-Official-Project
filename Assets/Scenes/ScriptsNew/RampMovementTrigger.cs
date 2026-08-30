using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class RampMovementTrigger : MonoBehaviour
{
    private enum RampForwardPreset
    {
        NorthWest,
        NorthEast
    }

    [Header("Movement")]
    [SerializeField] private bool enableRampMovement = true;

    [SerializeField] private RampForwardPreset rampForwardPreset =
        RampForwardPreset.NorthEast;

    [Tooltip("Direction pointing UP the ramp.")]
    [SerializeField]
    private Vector2 rampForward = new Vector2(1f, 0.5f);

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
        ApplyRampPreset();
    }

    private void OnValidate()
    {
        ApplyRampPreset();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!enableRampMovement)
            return;

        PlayerWASD movement = other.GetComponent<PlayerWASD>();

        if (movement != null)
            movement.EnterRamp(rampForward);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        PlayerWASD movement = other.GetComponent<PlayerWASD>();

        if (movement != null)
            movement.ExitRamp();
    }

    private void ApplyRampPreset()
    {
        switch (rampForwardPreset)
        {
            case RampForwardPreset.NorthWest:
                rampForward = new Vector2(-7f, 12f);
                break;
            case RampForwardPreset.NorthEast:
                rampForward = new Vector2(1f, 1.6f);
                break;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Vector3 origin = transform.position;

        DrawArrow(origin, rampForward.normalized, Color.green);

        Gizmos.color = Color.white;
        Gizmos.DrawSphere(origin, 0.05f);
    }

    private void DrawArrow(Vector3 origin, Vector3 direction, Color color)
    {
        Gizmos.color = color;

        Vector3 end = origin + direction.normalized;

        Gizmos.DrawLine(origin, end);

        Vector3 right =
            Quaternion.Euler(0, 0, 25) * -direction.normalized;

        Vector3 left =
            Quaternion.Euler(0, 0, -25) * -direction.normalized;

        Gizmos.DrawLine(end, end + right * 0.25f);
        Gizmos.DrawLine(end, end + left * 0.25f);
    }
#endif
}