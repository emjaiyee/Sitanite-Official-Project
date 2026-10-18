using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public class WallOcclusionFade : MonoBehaviour
{
    [Min(0)] public int tilemapLevel;
    [Min(0f)] public float proximityPadding = 0.24f;
    [Range(0f, 1f)] public float fadedAlpha = 0.15f;
    [Min(0.01f)] public float fadeDuration = 0.2f;

    private sealed class WallCell
    {
        public Vector3Int position;
        public Color color;
        public TileFlags flags;
    }

    private sealed class WallColumn
    {
        public readonly List<WallCell> cells = new List<WallCell>();
        public Bounds bounds;
        public float alpha = 1f;
    }

    private readonly Dictionary<Vector2Int, WallColumn> columns = new Dictionary<Vector2Int, WallColumn>();
    private Tilemap map;

    private void OnEnable()
    {
        RebuildCache();
    }

    private void OnDisable()
    {
        RestoreCells();
    }

    private void LateUpdate()
    {
        PlayerElevationLevel player = PlayerElevationLevel.Instance;
        RefreshFade(player != null ? player.transform.position : Vector3.zero,
            player != null ? player.CurrentLevel : int.MaxValue, Time.deltaTime);
    }

    public void RebuildCache()
    {
        RestoreCells();
        map = GetComponent<Tilemap>();
        columns.Clear();
        foreach (Vector3Int cell in map.cellBounds.allPositionsWithin)
        {
            if (!map.HasTile(cell))
                continue;
            Vector2Int key = new Vector2Int(cell.x, cell.y);
            if (!columns.TryGetValue(key, out WallColumn column))
            {
                column = new WallColumn { bounds = new Bounds(Project(map.GetCellCenterWorld(cell)), Vector3.zero) };
                columns.Add(key, column);
            }
            column.cells.Add(new WallCell { position = cell, color = map.GetColor(cell), flags = map.GetTileFlags(cell) });
            map.SetTileFlags(cell, map.GetTileFlags(cell) & ~TileFlags.LockColor);
            Sprite sprite = map.GetSprite(cell);
            Vector3 origin = map.GetCellCenterWorld(cell);
            if (sprite == null)
            {
                column.bounds.Encapsulate(Project(origin));
                continue;
            }
            Matrix4x4 matrix = map.orientationMatrix * map.GetTransformMatrix(cell);
            Bounds bounds = sprite.bounds;
            foreach (Vector3 corner in new[]
            {
                new Vector3(bounds.min.x, bounds.min.y), new Vector3(bounds.max.x, bounds.min.y),
                new Vector3(bounds.max.x, bounds.max.y), new Vector3(bounds.min.x, bounds.max.y)
            })
                column.bounds.Encapsulate(Project(origin + map.transform.TransformVector(matrix.MultiplyPoint3x4(corner))));
        }
    }

    public void RefreshFade(Vector3 playerPosition, int playerLevel, float deltaTime)
    {
        if (map == null)
            RebuildCache();
        Vector3 point = Project(playerPosition);
        float padding = Mathf.Max(0f, proximityPadding);
        foreach (WallColumn column in columns.Values)
        {
            float target = playerLevel <= tilemapLevel && column.bounds.SqrDistance(point) <= padding * padding
                ? Mathf.Clamp01(fadedAlpha) : 1f;
            float alpha = Mathf.MoveTowards(column.alpha, target, Mathf.Max(0f, deltaTime) / Mathf.Max(0.01f, fadeDuration));
            if (Mathf.Approximately(alpha, column.alpha))
                continue;
            column.alpha = alpha;
            foreach (WallCell cell in column.cells)
            {
                Color color = cell.color;
                color.a *= alpha;
                map.SetColor(cell.position, color);
            }
        }
    }

    private void RestoreCells()
    {
        if (map == null)
            return;
        foreach (WallColumn column in columns.Values)
        {
            foreach (WallCell cell in column.cells)
            {
                if (!map.HasTile(cell.position))
                    continue;
                map.SetTileFlags(cell.position, map.GetTileFlags(cell.position) & ~TileFlags.LockColor);
                map.SetColor(cell.position, cell.color);
                map.SetTileFlags(cell.position, cell.flags);
            }
        }
    }

    private static Vector3 Project(Vector3 position) => new Vector3(position.x, position.y, 0f);
}