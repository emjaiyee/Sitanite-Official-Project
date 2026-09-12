using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public class AStarWalkableMap : MonoBehaviour
{
    [Header("Elevation")]
    [SerializeField] private int elevationLevel = 0;

    private Tilemap tilemap;

    public int ElevationLevel => elevationLevel;

    public Tilemap Tilemap => tilemap;

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

        AStarManager.Instance.RegisterWalkableTilemap(this);
    }

    private void OnDestroy()
    {
        if (AStarManager.Instance != null)
        {
            AStarManager.Instance.UnregisterWalkableTilemap(this);
        }
    }
}