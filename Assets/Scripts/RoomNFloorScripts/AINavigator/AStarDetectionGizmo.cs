using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public class AStarDetectionGizmo : MonoBehaviour
{
    [SerializeField] private Color gizmoColor = new Color(0f, 0.5f, 1f, 0.35f);

    private Tilemap tilemap;

    private void Awake()
    {
        tilemap = GetComponent<Tilemap>();
    }

    private void OnDrawGizmos()
    {
        if (tilemap == null)
            tilemap = GetComponent<Tilemap>();

        if (tilemap == null)
            return;

        Gizmos.color = gizmoColor;

        foreach (Vector3Int cell in tilemap.cellBounds.allPositionsWithin)
        {
            if (!tilemap.HasTile(cell))
                continue;

            Vector3 center = tilemap.GetCellCenterWorld(cell);
            Vector3 size = tilemap.cellSize;

            Gizmos.DrawCube(center, size);
        }
    }
}
