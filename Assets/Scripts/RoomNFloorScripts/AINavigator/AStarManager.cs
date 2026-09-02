using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class AStarManager : MonoBehaviour
{
    public static AStarManager Instance { get; private set; }

    private readonly List<AStarWalkableMap> walkableMaps =
        new List<AStarWalkableMap>();

    private readonly List<Tilemap> detectionTilemaps =
        new List<Tilemap>();

    private readonly List<AStarStairLink> stairLinks =
        new List<AStarStairLink>();


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }


    // =========================================================
    // TILEMAP REGISTRATION
    // =========================================================

    public void RegisterWalkableTilemap(
        AStarWalkableMap walkableMap)
    {
        if (walkableMap == null || walkableMap.Tilemap == null)
            return;

        if (walkableMaps.Contains(walkableMap))
            return;

        walkableMaps.Add(walkableMap);

        Debug.Log(
            $"[AStarManager] Registered: {walkableMap.Tilemap.name} " +
            $"(elevation {walkableMap.ElevationLevel})"
        );
    }


    public void UnregisterWalkableTilemap(
        AStarWalkableMap walkableMap)
    {
        if (walkableMap == null)
            return;

        walkableMaps.Remove(walkableMap);
    }


    public void RegisterDetectionTilemap(
        Tilemap tilemap)
    {
        if (tilemap == null)
            return;

        if (detectionTilemaps.Contains(tilemap))
            return;

        detectionTilemaps.Add(tilemap);

        Debug.Log(
            $"[AStarManager] Registered detection: {tilemap.name}"
        );
    }


    public void UnregisterDetectionTilemap(
        Tilemap tilemap)
    {
        if (tilemap == null)
            return;

        detectionTilemaps.Remove(tilemap);
    }


    public void RegisterStairLink(
        AStarStairLink stairLink)
    {
        if (stairLink == null)
            return;

        if (stairLinks.Contains(stairLink))
            return;

        stairLinks.Add(stairLink);
    }


    public void UnregisterStairLink(
        AStarStairLink stairLink)
    {
        if (stairLink == null)
            return;

        stairLinks.Remove(stairLink);
    }


    // =========================================================
    // FIND TILEMAP
    // =========================================================

    public Tilemap GetTilemapAtPosition(
        Vector3 worldPosition)
    {
        return GetWalkableTilemapAtPosition(
            worldPosition);
    }


    public Tilemap GetWalkableTilemapAtPosition(
        Vector3 worldPosition)
    {
        foreach (AStarWalkableMap walkableMap in walkableMaps)
        {
            if (walkableMap == null || walkableMap.Tilemap == null)
                continue;

            Vector3Int cell =
                walkableMap.Tilemap.WorldToCell(worldPosition);

            if (walkableMap.Tilemap.HasTile(cell))
            {
                return walkableMap.Tilemap;
            }
        }

        return null;
    }


    public Tilemap GetWalkableTilemapAtPosition(
        Vector3 worldPosition,
        int elevationLevel)
    {
        foreach (AStarWalkableMap walkableMap in walkableMaps)
        {
            if (walkableMap == null || walkableMap.Tilemap == null)
                continue;

            if (walkableMap.ElevationLevel != elevationLevel)
                continue;

            Vector3Int cell =
                walkableMap.Tilemap.WorldToCell(worldPosition);

            if (walkableMap.Tilemap.HasTile(cell))
                return walkableMap.Tilemap;
        }

        return null;
    }


    public Tilemap GetDetectionTilemapAtPosition(
        Vector3 worldPosition)
    {
        foreach (Tilemap tilemap in detectionTilemaps)
        {
            if (tilemap == null)
                continue;

            Vector3Int cell =
                tilemap.WorldToCell(worldPosition);

            if (tilemap.HasTile(cell))
            {
                return tilemap;
            }
        }

        return null;
    }


    public AStarStairLink GetStairLinkAtPosition(
        Vector3 worldPosition)
    {
        foreach (AStarStairLink stairLink in stairLinks)
        {
            if (stairLink == null)
                continue;

            if (stairLink.Contains(worldPosition))
                return stairLink;
        }

        return null;
    }


    public AStarStairLink GetNearestStairLink(
        Vector3 worldPosition)
    {
        AStarStairLink bestLink = null;
        float bestDistance = float.MaxValue;

        foreach (AStarStairLink stairLink in stairLinks)
        {
            if (stairLink == null)
                continue;

            float entryDistance =
                Vector3.Distance(
                    worldPosition,
                    stairLink.EntryPosition);

            float exitDistance =
                Vector3.Distance(
                    worldPosition,
                    stairLink.ExitPosition);

            float linkDistance = Mathf.Min(
                entryDistance,
                exitDistance);

            if (linkDistance < bestDistance)
            {
                bestDistance = linkDistance;
                bestLink = stairLink;
            }
        }

        return bestLink;
    }


    public bool IsPositionWalkable(
        Vector3 worldPosition)
    {
        return GetWalkableTilemapAtPosition(
            worldPosition) != null ||
            GetStairLinkAtPosition(worldPosition) != null;
    }


    public bool IsPositionWalkable(
        Vector3 worldPosition,
        int elevationLevel)
    {
        return GetWalkableTilemapAtPosition(
            worldPosition,
            elevationLevel) != null ||
            GetStairLinkAtPosition(worldPosition) != null;
    }


    public Vector3? GetWalkableCellCenter(
        Vector3 worldPosition)
    {
        Tilemap tilemap =
            GetWalkableTilemapAtPosition(worldPosition);

        if (tilemap == null)
            return null;

        return tilemap.GetCellCenterWorld(
            tilemap.WorldToCell(worldPosition)
        );
    }


    public Vector3? GetWalkableCellCenter(
        Vector3 worldPosition,
        int elevationLevel)
    {
        Tilemap tilemap =
            GetWalkableTilemapAtPosition(
                worldPosition,
                elevationLevel);

        if (tilemap == null)
            return null;

        return tilemap.GetCellCenterWorld(
            tilemap.WorldToCell(worldPosition)
        );
    }


    // =========================================================
    // FIND PATH
    // =========================================================

    public List<Vector3> FindPath(
        Vector3 startWorldPosition,
        Vector3 targetWorldPosition)
    {
        Tilemap startTilemap =
            GetWalkableTilemapAtPosition(
                startWorldPosition);

        if (startTilemap == null)
        {
            Debug.LogWarning(
                $"[AStarManager] Start position " +
                $"{startWorldPosition} is not walkable."
            );

            return null;
        }

        return FindPath(
            startWorldPosition,
            targetWorldPosition,
            GetWalkableElevationLevel(startTilemap)
        );
    }


    public List<Vector3> FindPath(
        Vector3 startWorldPosition,
        Vector3 targetWorldPosition,
        int elevationLevel)
    {
        return FindPath(
            startWorldPosition,
            targetWorldPosition,
            elevationLevel,
            elevationLevel
        );
    }


    public List<Vector3> FindPath(
        Vector3 startWorldPosition,
        Vector3 targetWorldPosition,
        int startElevationLevel,
        int targetElevationLevel)
    {
        Vector3 resolvedStartWorldPosition =
            ResolvePathStartPosition(
                startWorldPosition,
                startElevationLevel);

        Tilemap startTilemap =
            GetWalkableTilemapAtPosition(
                resolvedStartWorldPosition,
                startElevationLevel);

        if (startTilemap == null)
        {
            Debug.LogWarning(
                $"[AStarManager] Start position " +
                $"{startWorldPosition} is not walkable."
            );

            return null;
        }


        AStarStairLink targetStairLink =
            GetStairLinkAtPosition(targetWorldPosition);

        if (targetStairLink == null)
        {
            Tilemap targetWalkableTilemap =
                GetWalkableTilemapAtPosition(
                    targetWorldPosition,
                    targetElevationLevel);

            if (targetWalkableTilemap == null)
            {
                return null;
            }
        }

        if (targetStairLink != null)
        {
            return BuildPathToStairLink(
                startTilemap,
                resolvedStartWorldPosition,
                targetStairLink
            );
        }


        Tilemap targetTilemap =
            GetWalkableTilemapAtPosition(
                targetWorldPosition,
                targetElevationLevel);

        if (targetTilemap == null)
        {
            return null;
        }


        if (targetTilemap == startTilemap)
        {
            return BuildPathOnTilemap(
                startTilemap,
                resolvedStartWorldPosition,
                targetWorldPosition
            );
        }


        return BuildCrossMapPath(
            startTilemap,
            resolvedStartWorldPosition,
            targetTilemap,
            targetWorldPosition
        );
    }


    private Vector3 ResolvePathStartPosition(
        Vector3 startWorldPosition,
        int elevationLevel)
    {
        if (GetWalkableTilemapAtPosition(
                startWorldPosition,
                elevationLevel) != null)
            return startWorldPosition;

        AStarStairLink stairLink =
            GetStairLinkAtPosition(startWorldPosition);

        if (stairLink == null)
            stairLink = GetNearestStairLink(startWorldPosition);

        if (stairLink == null)
            return startWorldPosition;


        bool entryMatchesElevation =
            GetWalkableTilemapAtPosition(
                stairLink.EntryPosition,
                elevationLevel) != null;


        bool exitMatchesElevation =
            GetWalkableTilemapAtPosition(
                stairLink.ExitPosition,
                elevationLevel) != null;


        if (entryMatchesElevation && !exitMatchesElevation)
            return stairLink.EntryPosition;


        if (exitMatchesElevation && !entryMatchesElevation)
            return stairLink.ExitPosition;


        return stairLink.IsCloserToEntry(startWorldPosition)
            ? stairLink.EntryPosition
            : stairLink.ExitPosition;
    }


    private int GetWalkableElevationLevel(Tilemap tilemap)
    {
        if (tilemap == null)
            return 0;

        foreach (AStarWalkableMap walkableMap in walkableMaps)
        {
            if (walkableMap == null || walkableMap.Tilemap == null)
                continue;

            if (walkableMap.Tilemap == tilemap)
                return walkableMap.ElevationLevel;
        }

        return 0;
    }


    private List<Vector3> BuildPathOnTilemap(
        Tilemap tilemap,
        Vector3 startWorldPosition,
        Vector3 targetWorldPosition)
    {
        if (tilemap == null)
            return null;

        Vector3Int startCell =
            tilemap.WorldToCell(startWorldPosition);

        Vector3Int targetCell =
            tilemap.WorldToCell(targetWorldPosition);

        if (!tilemap.HasTile(targetCell))
            return null;

        return CalculateAStar(
            tilemap,
            startCell,
            targetCell);
    }


    private List<Vector3> BuildPathToStairLink(
        Tilemap startTilemap,
        Vector3 startWorldPosition,
        AStarStairLink stairLink)
    {
        if (stairLink == null)
            return null;

        bool useEntrySide =
            Vector3.Distance(
                startWorldPosition,
                stairLink.EntryPosition
            ) <=
            Vector3.Distance(
                startWorldPosition,
                stairLink.ExitPosition
            );

        Vector3 approachPoint =
            useEntrySide
                ? stairLink.EntryPosition
                : stairLink.ExitPosition;

        List<Vector3> path =
            BuildPathOnTilemap(
                startTilemap,
                startWorldPosition,
                approachPoint
            );

        if (path == null)
            return null;

        AppendWorldPoints(
            path,
            stairLink.BuildTraversalPoints(
                useEntrySide
            )
        );

        return path;
    }


    private List<Vector3> BuildCrossMapPath(
        Tilemap startTilemap,
        Vector3 startWorldPosition,
        Tilemap targetTilemap,
        Vector3 targetWorldPosition)
    {
        List<Vector3> bestPath = null;
        int bestCost = int.MaxValue;

        foreach (AStarStairLink stairLink in stairLinks)
        {
            TryBuildCrossMapPath(
                startTilemap,
                startWorldPosition,
                targetTilemap,
                targetWorldPosition,
                stairLink,
                true,
                ref bestPath,
                ref bestCost
            );

            TryBuildCrossMapPath(
                startTilemap,
                startWorldPosition,
                targetTilemap,
                targetWorldPosition,
                stairLink,
                false,
                ref bestPath,
                ref bestCost
            );
        }

        return bestPath;
    }


    private void TryBuildCrossMapPath(
        Tilemap startTilemap,
        Vector3 startWorldPosition,
        Tilemap targetTilemap,
        Vector3 targetWorldPosition,
        AStarStairLink stairLink,
        bool fromEntryToExit,
        ref List<Vector3> bestPath,
        ref int bestCost)
    {
        if (stairLink == null)
            return;

        Vector3 approachPoint = fromEntryToExit
            ? stairLink.EntryPosition
            : stairLink.ExitPosition;

        Vector3 arrivalPoint = fromEntryToExit
            ? stairLink.ExitPosition
            : stairLink.EntryPosition;

        List<Vector3> approachSegment =
            BuildPathOnTilemap(
                startTilemap,
                startWorldPosition,
                approachPoint
            );

        if (approachSegment == null)
            return;

        List<Vector3> targetSegment =
            BuildPathOnTilemap(
                targetTilemap,
                arrivalPoint,
                targetWorldPosition
            );

        if (targetSegment == null)
            return;

        List<Vector3> candidatePath =
            new List<Vector3>();

        AppendWorldPoints(candidatePath, approachSegment);
        AppendWorldPoints(
            candidatePath,
            stairLink.BuildTraversalPoints(fromEntryToExit)
        );
        AppendWorldPoints(candidatePath, targetSegment);

        if (candidatePath.Count < bestCost)
        {
            bestCost = candidatePath.Count;
            bestPath = candidatePath;
        }
    }


    private void AppendWorldPoints(
        List<Vector3> path,
        IEnumerable<Vector3> points)
    {
        foreach (Vector3 point in points)
        {
            if (path.Count > 0 &&
                Vector3.Distance(
                    path[path.Count - 1],
                    point
                ) <= 0.001f)
            {
                continue;
            }

            path.Add(point);
        }
    }


    public Vector3? GetRandomWalkablePositionNear(
    Vector3 originWorldPosition,
    int radius)
    {
        Tilemap tilemap =
            GetWalkableTilemapAtPosition(
                originWorldPosition);

        if (tilemap == null)
            return null;


        Vector3Int originCell =
            tilemap.WorldToCell(
                originWorldPosition);


        List<Vector3Int> possibleCells =
            new List<Vector3Int>();


        for (int x = -radius;
            x <= radius;
            x++)
        {
            for (int y = -radius;
                y <= radius;
                y++)
            {
                Vector3Int cell =
                    originCell +
                    new Vector3Int(x, y, 0);


                if (!tilemap.HasTile(cell))
                    continue;


                if (cell == originCell)
                    continue;


                possibleCells.Add(cell);
            }
        }


        if (possibleCells.Count == 0)
            return null;


        Vector3Int chosenCell =
            possibleCells[
                Random.Range(
                    0,
                    possibleCells.Count)
            ];


        return tilemap.GetCellCenterWorld(
            chosenCell);
    }


    public Vector3? GetRandomWalkablePositionNear(
        Vector3 originWorldPosition,
        int radius,
        int elevationLevel)
    {
        Tilemap tilemap =
            GetWalkableTilemapAtPosition(
                originWorldPosition,
                elevationLevel);

        if (tilemap == null)
            return null;

        Vector3Int originCell =
            tilemap.WorldToCell(originWorldPosition);

        List<Vector3Int> possibleCells =
            new List<Vector3Int>();

        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                Vector3Int cell =
                    originCell + new Vector3Int(x, y, 0);

                if (!tilemap.HasTile(cell))
                    continue;

                if (cell == originCell)
                    continue;

                possibleCells.Add(cell);
            }
        }

        if (possibleCells.Count == 0)
            return null;

        Vector3Int chosenCell =
            possibleCells[
                Random.Range(0, possibleCells.Count)
            ];

        return tilemap.GetCellCenterWorld(chosenCell);
    }

    public bool IsPositionWithinDetectionRadius(
        Vector3 enemyWorldPosition,
        Vector3 playerWorldPosition,
        int radius,
        int elevationLevel)
    {
        if (GetWalkableTilemapAtPosition(
                enemyWorldPosition,
                elevationLevel) == null)
        {
            return false;
        }

        if (GetWalkableTilemapAtPosition(playerWorldPosition) == null &&
            GetDetectionTilemapAtPosition(playerWorldPosition) == null)
        {
            return false;
        }

        float worldRadius =
            radius *
            GetApproximateCellSize(enemyWorldPosition);

        return Vector3.Distance(
            enemyWorldPosition,
            playerWorldPosition
        ) <= worldRadius;
    }


    private float GetApproximateCellSize(
        Vector3 worldPosition)
    {
        Tilemap tilemap =
            GetWalkableTilemapAtPosition(worldPosition);

        if (tilemap == null)
        {
            tilemap =
                GetDetectionTilemapAtPosition(
                    worldPosition);
        }

        if (tilemap == null)
            return 1f;

        Vector3Int cell =
            tilemap.WorldToCell(worldPosition);

        Vector3 center =
            tilemap.GetCellCenterWorld(cell);

        Vector3 neighbour =
            tilemap.GetCellCenterWorld(
                cell + Vector3Int.right);

        float size = Vector3.Distance(center, neighbour);

        return size > 0.0001f ? size : 1f;
    }

    // =========================================================
    // A*
    // =========================================================

    private List<Vector3> CalculateAStar(
        Tilemap tilemap,
        Vector3Int startCell,
        Vector3Int targetCell)
    {
        List<Node> openList =
            new List<Node>();

        HashSet<Vector3Int> closedSet =
            new HashSet<Vector3Int>();


        Node startNode =
            new Node(startCell);

        startNode.gCost = 0;

        startNode.hCost =
            GetDistance(
                startCell,
                targetCell);


        openList.Add(startNode);


        while (openList.Count > 0)
        {
            Node currentNode =
                GetLowestCostNode(openList);


            openList.Remove(currentNode);

            closedSet.Add(
                currentNode.cell);


            // -------------------------------------------------
            // TARGET REACHED
            // -------------------------------------------------

            if (currentNode.cell ==
                targetCell)
            {
                return RetracePath(
                    startNode,
                    currentNode,
                    tilemap);
            }


            // -------------------------------------------------
            // CHECK NEIGHBOURS
            // -------------------------------------------------

            foreach (Vector3Int neighbourCell
                     in GetNeighbours(currentNode.cell))
            {

                if (!CanMoveToNeighbour(
                        tilemap,
                        currentNode.cell,
                        neighbourCell))
                {
                    continue;
                }

                if (closedSet.Contains(
                        neighbourCell))
                {
                    continue;
                }


                int newMovementCost =
                    currentNode.gCost +
                    GetDistance(
                        currentNode.cell,
                        neighbourCell);


                Node neighbourNode =
                    FindNode(
                        openList,
                        neighbourCell);


                if (neighbourNode == null)
                {
                    neighbourNode =
                        new Node(neighbourCell);

                    neighbourNode.gCost =
                        newMovementCost;

                    neighbourNode.hCost =
                        GetDistance(
                            neighbourCell,
                            targetCell);

                    neighbourNode.parent =
                        currentNode;

                    openList.Add(
                        neighbourNode);
                }
                else if (newMovementCost <
                         neighbourNode.gCost)
                {
                    neighbourNode.gCost =
                        newMovementCost;

                    neighbourNode.parent =
                        currentNode;
                }
            }
        }


        // No route exists.
        return null;
    }


    // =========================================================
    // NODE
    // =========================================================

    private class Node
    {
        public Vector3Int cell;

        public int gCost;
        public int hCost;

        public Node parent;

        public int fCost =>
            gCost + hCost;


        public Node(
            Vector3Int cell)
        {
            this.cell = cell;
        }
    }


    // =========================================================
    // LOWEST COST NODE
    // =========================================================

    private Node GetLowestCostNode(
        List<Node> nodes)
    {
        Node bestNode =
            nodes[0];

        for (int i = 1;
             i < nodes.Count;
             i++)
        {
            Node node =
                nodes[i];

            if (node.fCost < bestNode.fCost ||
                (node.fCost ==
                 bestNode.fCost &&
                 node.hCost <
                 bestNode.hCost))
            {
                bestNode =
                    node;
            }
        }

        return bestNode;
    }


    // =========================================================
    // NEIGHBOURS
    // =========================================================

    private List<Vector3Int> GetNeighbours(
    Vector3Int cell)
    {
        return new List<Vector3Int>
        {
            // Cardinal
            cell + new Vector3Int(1, 0, 0),
            cell + new Vector3Int(-1, 0, 0),
            cell + new Vector3Int(0, 1, 0),
            cell + new Vector3Int(0, -1, 0),

            // Diagonal
            cell + new Vector3Int(1, 1, 0),
            cell + new Vector3Int(1, -1, 0),
            cell + new Vector3Int(-1, 1, 0),
            cell + new Vector3Int(-1, -1, 0)
        };
    }

    private bool CanMoveToNeighbour(
    Tilemap tilemap,
    Vector3Int current,
    Vector3Int neighbour)
    {
        // Destination itself must be walkable.
        if (!tilemap.HasTile(neighbour))
            return false;

        int dx =
            neighbour.x - current.x;

        int dy =
            neighbour.y - current.y;

        // Cardinal movement.
        if (dx == 0 || dy == 0)
            return true;

        // Diagonal movement.
        Vector3Int horizontal =
            current +
            new Vector3Int(dx, 0, 0);

        Vector3Int vertical =
            current +
            new Vector3Int(0, dy, 0);

        // Prevent diagonal corner-cutting.
        return
            tilemap.HasTile(horizontal) &&
            tilemap.HasTile(vertical);
    }


    // =========================================================
    // DISTANCE
    // =========================================================

    private int GetDistance(
    Vector3Int a,
    Vector3Int b)
    {
        int dx =
            Mathf.Abs(a.x - b.x);

        int dy =
            Mathf.Abs(a.y - b.y);

        int diagonal =
            Mathf.Min(dx, dy);

        int straight =
            Mathf.Abs(dx - dy);

        return
            diagonal * 14 +
            straight * 10;
    }


    // =========================================================
    // FIND NODE
    // =========================================================

    private Node FindNode(
        List<Node> nodes,
        Vector3Int cell)
    {
        foreach (Node node in nodes)
        {
            if (node.cell == cell)
                return node;
        }

        return null;
    }


    // =========================================================
    // RETRACE PATH
    // =========================================================

    private List<Vector3> RetracePath(
        Node startNode,
        Node endNode,
        Tilemap tilemap)
    {
        List<Vector3> path =
            new List<Vector3>();

        Node currentNode =
            endNode;


        while (currentNode != startNode)
        {
            path.Add(
                tilemap.GetCellCenterWorld(
                    currentNode.cell));

            currentNode =
                currentNode.parent;
        }


        // We don't need to add the starting
        // position because the enemy is
        // already standing there.

        path.Reverse();

        return path;
    }
}