using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class AStarStairLink : MonoBehaviour
{
    [Header("Endpoints")]
    [Tooltip("Optional walkable point at the bottom of the ramp. Leave empty to use the RampMovementTrigger collider.")]
    [SerializeField] private Transform entryAnchor;

    [Tooltip("Optional walkable point at the top of the ramp. Leave empty to use the RampMovementTrigger collider.")]
    [SerializeField] private Transform exitAnchor;

    [Header("Traversal")]
    [Min(2)]
    [SerializeField] private int traversalWaypointCount = 4;

    private Collider2D stairCollider;
    private RampMovementTrigger rampMovement;

    public Vector3 EntryPosition =>
        entryAnchor != null
            ? entryAnchor.position
            : GetColliderEndpoint(false);

    public Vector3 ExitPosition =>
        exitAnchor != null
            ? exitAnchor.position
            : GetColliderEndpoint(true);

    private void Reset()
    {
        stairCollider = GetComponent<Collider2D>();
        stairCollider.isTrigger = true;
    }

    private void Awake()
    {
        stairCollider = GetComponent<Collider2D>();
        rampMovement = GetComponent<RampMovementTrigger>();
    }

    private void Start()
    {
        if (AStarManager.Instance == null)
        {
            Debug.LogWarning(
                $"No AStarManager found for {gameObject.name}."
            );

            return;
        }

        AStarManager.Instance.RegisterStairLink(this);
    }

    private void OnDestroy()
    {
        if (AStarManager.Instance != null)
        {
            AStarManager.Instance.UnregisterStairLink(this);
        }
    }

    public bool Contains(Vector3 worldPosition)
    {
        if (stairCollider != null)
        {
            if (stairCollider.OverlapPoint(worldPosition))
                return true;

            if (stairCollider.bounds.Contains(worldPosition))
                return true;
        }

        float threshold = 0.5f;

        if (DistanceToSegment(worldPosition, EntryPosition, ExitPosition) <= threshold)
            return true;

        return Vector2.Distance(transform.position, worldPosition) <= threshold;
    }

    public bool IsConfigured()
    {
        return (entryAnchor != null && exitAnchor != null) ||
            rampMovement != null;
    }

    public bool IsCloserToEntry(Vector3 worldPosition)
    {
        return Vector3.Distance(worldPosition, EntryPosition) <=
            Vector3.Distance(worldPosition, ExitPosition);
    }

    public List<Vector3> BuildTraversalPoints(bool fromEntryToExit)
    {
        List<Vector3> points = new List<Vector3>();

        Vector3 start = fromEntryToExit ? EntryPosition : ExitPosition;
        Vector3 end = fromEntryToExit ? ExitPosition : EntryPosition;

        int waypointCount = Mathf.Max(2, traversalWaypointCount);

        for (int i = 1; i <= waypointCount; i++)
        {
            float t = i / (float)(waypointCount + 1);
            points.Add(Vector3.Lerp(start, end, t));
        }

        points.Add(end);

        return points;
    }

    private Vector3 GetColliderEndpoint(bool upperEnd)
    {
        if (rampMovement == null ||
            rampMovement.RampCollider == null ||
            rampMovement.RampForward.sqrMagnitude <= 0.0001f)
        {
            return transform.position;
        }

        Vector2 direction = rampMovement.RampForward;
        Bounds bounds = rampMovement.RampCollider.bounds;
        float distance = bounds.extents.magnitude * 2f + 0.1f;
        Vector2 probe = (Vector2)bounds.center +
            direction * (upperEnd ? distance : -distance);

        Vector2 edge = rampMovement.RampCollider.ClosestPoint(probe);

        return edge + direction * (upperEnd ? 0.05f : -0.05f);
    }


    private static float DistanceToSegment(
        Vector3 point,
        Vector3 segmentStart,
        Vector3 segmentEnd)
    {
        Vector3 segment = segmentEnd - segmentStart;
        float segmentLengthSquared = segment.sqrMagnitude;

        if (segmentLengthSquared <= 0.0001f)
            return Vector3.Distance(point, segmentStart);

        float t = Vector3.Dot(point - segmentStart, segment) / segmentLengthSquared;
        t = Mathf.Clamp01(t);

        Vector3 projection = segmentStart + segment * t;
        return Vector3.Distance(point, projection);
    }
}
