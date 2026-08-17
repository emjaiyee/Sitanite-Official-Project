using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public class AStarWalkableMap : MonoBehaviour
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

        AStarManager.Instance.RegisterWalkableTilemap(tilemap);
    }

    private void OnDestroy()
    {
        if (AStarManager.Instance != null)
        {
            AStarManager.Instance.UnregisterWalkableTilemap(tilemap);
        }
    }
}