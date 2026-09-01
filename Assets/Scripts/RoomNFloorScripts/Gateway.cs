using UnityEngine;

public class Gateway : MonoBehaviour
{
    [Header("Gateway Settings")]
    [SerializeField] private GatewayDirection direction;
    [SerializeField] private GatewayFlow flow;

    [Header("Destination")]
    [SerializeField] private Transform destination;

    public GatewayDirection Direction => direction;
    public GatewayFlow Flow => flow;
    public Transform Destination => destination;

    public void SetDestination(Transform newDestination)
    {
        destination = newDestination;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        RoomManager roomManager =
            GetComponentInParent<RoomManager>();

        if (roomManager != null && roomManager.HandleGatewayEntered(this))
            return;

        if (destination == null)
        {
            Debug.LogWarning(
                $"Gateway '{name}' does not have a destination assigned."
            );

            return;
        }

        if (RoomTransitionManager.Instance == null)
        {
            Debug.LogError(
                "No RoomTransitionManager instance exists in the scene."
            );

            return;
        }

        RoomTransitionManager.Instance.TransitionPlayer(
            other.transform,
            destination
        );
    }
}