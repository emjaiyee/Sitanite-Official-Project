using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public class ProceduralTilemapPlane : MonoBehaviour
{
    public int baseZ;

    public static Vector3Int WorldToCell(Tilemap map, Vector3 position)
    {
        ProceduralTilemapPlane plane = map.GetComponent<ProceduralTilemapPlane>();
        if (plane == null)
            return map.WorldToCell(position);
        position.z = map.GetCellCenterWorld(new Vector3Int(0, 0, plane.baseZ)).z;
        Vector3Int cell = map.WorldToCell(position);
        cell.z = plane.baseZ;
        return cell;
    }

    public static bool ContainsDetection(Tilemap map, Vector3 position)
    {
        if (map.HasTile(map.WorldToCell(position)))
            return true;
        BoundsInt bounds = map.cellBounds;
        for (int cellZ = bounds.zMin; cellZ < bounds.zMax; cellZ++)
        {
            position.z = map.GetCellCenterWorld(new Vector3Int(0, 0, cellZ)).z;
            Vector3Int cell = map.WorldToCell(position);
            cell.z = cellZ;
            if (map.HasTile(cell))
                return true;
        }
        return false;
    }
}