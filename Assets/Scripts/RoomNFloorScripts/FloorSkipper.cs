using UnityEngine;

// Debug-only helper: starts the Dungeon scene on a chosen floor.
[DefaultExecutionOrder(-1000)]
[AddComponentMenu("Dimla/Debug/Floor Skipper")]
public class FloorSkipper : MonoBehaviour
{
    [SerializeField] private bool skipOnPlay = true;
    [Min(1)] [SerializeField] private int targetFloor = 1;
    [Tooltip("Resolved automatically when not assigned.")]
    [SerializeField] private FloorManager floorManager;

    private void Awake()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (!skipOnPlay)
            return;

        if (floorManager == null)
            floorManager = FindFirstObjectByType<FloorManager>();

        if (floorManager == null)
        {
            Debug.LogWarning("FloorSkipper: no FloorManager found in the scene.", this);
            return;
        }

        if (floorManager.SetStartingFloor(targetFloor))
            Debug.Log($"FloorSkipper: starting on Floor {targetFloor}.", this);
        else
            Debug.LogWarning(
                $"FloorSkipper: Floor {targetFloor} has no floor configuration or Manggagaway boss floor; " +
                $"starting on Floor {floorManager.CurrentFloor}.", this);
#endif
    }
}
