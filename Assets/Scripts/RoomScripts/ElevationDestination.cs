using UnityEngine;

public class ElevationDestination : MonoBehaviour
{
    [Header("Destination Settings")]
    [SerializeField] private GatewayDirection direction;
    [SerializeField] private GatewayFlow flow;

    [Header("Elevation")]
    [SerializeField] private int elevationLevel = 0;

    public GatewayDirection Direction => direction;
    public GatewayFlow Flow => flow;
    public int ElevationLevel => elevationLevel;
}