using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public class AStarDetectionMap : MonoBehaviour
{
    private Tilemap tilemap;

    private void Awake()
    {
        tilemap = GetComponent<Tilemap>();
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

        AStarManager.Instance.RegisterDetectionTilemap(tilemap);
    }

    private void OnDestroy()
    {
        if (AStarManager.Instance != null)
        {
            AStarManager.Instance.UnregisterDetectionTilemap(tilemap);
        }
    }
}
