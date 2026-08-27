using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class AStarManager : MonoBehaviour
{
    public static AStarManager Instance { get; private set; }

    private readonly List<Tilemap> walkableTilemaps =
        new List<Tilemap>();


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
        Tilemap tilemap)
    {
        if (tilemap == null)
            return;

        if (walkableTilemaps.Contains(tilemap))
            return;

        walkableTilemaps.Add(tilemap);

        Debug.Log(
            $"[AStarManager] Registered: {tilemap.name}"
        );
    }


    public void UnregisterWalkableTilemap(
        Tilemap tilemap)
    {
        if (tilemap == null)
            return;

        walkableTilemaps.Remove(tilemap);
    }


    // =========================================================
    // FIND TILEMAP
    // =========================================================

    public Tilemap GetTilemapAtPosition(
        Vector3 worldPosition)
    {
        foreach (Tilemap tilemap in walkableTilemaps)
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


    public bool IsPositionWalkable(
        Vector3 worldPosition)
    {
        return GetTilemapAtPosition(
            worldPosition) != null;
    }


    // =========================================================
    // FIND PATH
    // =========================================================

    public List<Vector3> FindPath(
        Vector3 startWorldPosition,
        Vector3 targetWorldPosition)
    {
        Tilemap tilemap =
            GetTilemapAtPosition(startWorldPosition);

        if (tilemap == null)
        {
            Debug.LogWarning(
                $"[AStarManager] Start position " +
                $"{startWorldPosition} is not walkable."
            );

            return null;
        }


        Vector3Int startCell =
            tilemap.WorldToCell(
                startWorldPosition);

        Vector3Int targetCell =
            tilemap.WorldToCell(
                targetWorldPosition);


        if (!tilemap.HasTile(targetCell))
        {
            return null;
        }


        return CalculateAStar(
            tilemap,
            startCell,
            targetCell);
    }

    public Vector3? GetRandomWalkablePositionNear(
    Vector3 originWorldPosition,
    int radius)
    {
        Tilemap tilemap =
            GetTilemapAtPosition(
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

    public bool IsPositionWithinDetectionRadius(
    Vector3 enemyWorldPosition,
    Vector3 playerWorldPosition,
    int radius)
    {
        Tilemap enemyTilemap =
            GetTilemapAtPosition(
                enemyWorldPosition
            );

        if (enemyTilemap == null)
            return false;


        Tilemap playerTilemap =
            GetTilemapAtPosition(
                playerWorldPosition
            );

        if (playerTilemap == null)
            return false;


        if (playerTilemap != enemyTilemap)
            return false;


        Vector3Int enemyCell =
            enemyTilemap.WorldToCell(
                enemyWorldPosition
            );


        Vector3Int playerCell =
            enemyTilemap.WorldToCell(
                playerWorldPosition
            );


        int deltaX =
            playerCell.x -
            enemyCell.x;


        int deltaY =
            playerCell.y -
            enemyCell.y;


        int squaredDistance =
            deltaX * deltaX +
            deltaY * deltaY;


        int squaredRadius =
            radius * radius;


        return squaredDistance <= squaredRadius;
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