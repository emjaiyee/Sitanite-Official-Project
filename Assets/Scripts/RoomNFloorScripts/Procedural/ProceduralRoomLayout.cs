using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class ProceduralRoomLayout
{
    private static readonly Vector3Int[] Neighbors =
    {
        Vector3Int.right, Vector3Int.up, Vector3Int.left, Vector3Int.down
    };

    public HashSet<Vector3Int> Ground { get; } = new HashSet<Vector3Int>();
    public HashSet<Vector3Int> Walkable { get; } = new HashSet<Vector3Int>();
    public HashSet<Vector3Int> Walls { get; } = new HashSet<Vector3Int>();
    public HashSet<Vector3Int> VisualWalls { get; } = new HashSet<Vector3Int>();
    public HashSet<Vector3Int> Detectable { get; } = new HashSet<Vector3Int>();
    public HashSet<Vector3Int> Reserved { get; } = new HashSet<Vector3Int>();
    public HashSet<Vector3Int> FloorCells { get; } = new HashSet<Vector3Int>();
    public HashSet<Vector3Int> SecretPath { get; } = new HashSet<Vector3Int>();
    public Vector3Int SecretOpening { get; private set; }
    public Vector3Int SecretGateway { get; private set; }
    public Vector3Int SecretLanding { get; private set; }
    public Vector3Int BackwardGateway { get; private set; }
    public Vector3Int ForwardGateway { get; private set; }
    public Vector3Int FloorGateway { get; private set; }
    public Vector3Int BackwardLanding { get; private set; }
    public Vector3Int ForwardLanding { get; private set; }
    public Vector3Int PlayerSpawn => BackwardLanding;

    public static ProceduralRoomLayout Generate(ProceduralRoomProfile profile, int seed)
    {
        if (profile == null)
            throw new ArgumentNullException(nameof(profile));

        if (profile.width < 16 || profile.width > 128 || profile.height < 16 || profile.height > 128 ||
            profile.corridorHalfWidth < 2 || profile.corridorHalfWidth > 8 ||
            profile.width < profile.corridorHalfWidth * 2 + 8 ||
            profile.height < profile.corridorHalfWidth * 2 + 8 ||
            profile.chamberCount < 3 || profile.chamberCount > 24)
        {
            throw new ArgumentException("Use dimensions 16-128, 3-24 chambers, and corridor half-width 2-8. Dimensions must fit the corridor plus eight cells.");
        }
        ValidateGatewaySettings(profile);

        ProceduralRoomLayout layout = new ProceduralRoomLayout();
        System.Random random = new System.Random(seed);
        int minimumX = -profile.width / 2;
        int minimumY = -profile.height / 2;
        int maximumX = minimumX + profile.width - 1;
        int maximumY = minimumY + profile.height - 1;
        int halfWidth = profile.corridorHalfWidth;

        if (profile.shape == ProceduralRoomShape.Rectangle)
        {
            layout.FillRectangle(minimumX, minimumY, maximumX, maximumY);
        }
        else if (profile.shape == ProceduralRoomShape.Cavern)
        {
            layout.CreateCavern(profile, random, minimumX, minimumY, maximumX, maximumY);
        }
        else
        {
            layout.FillRectangle(minimumX, -halfWidth, maximumX, halfWidth);
            layout.FillRectangle(-halfWidth, minimumY, halfWidth, maximumY);

            for (int index = 0; index < profile.chamberCount; index++)
            {
                int centerX = random.Next(minimumX + halfWidth, maximumX - halfWidth + 1);
                int centerY = random.Next(minimumY + halfWidth, maximumY - halfWidth + 1);
                int radiusX = random.Next(halfWidth + 1, Math.Max(halfWidth + 2, profile.width / 4));
                int radiusY = random.Next(halfWidth + 1, Math.Max(halfWidth + 2, profile.height / 4));
                layout.FillRectangle(centerX - halfWidth, Math.Min(0, centerY), centerX + halfWidth, Math.Max(0, centerY));

                for (int cellX = Math.Max(minimumX, centerX - radiusX); cellX <= Math.Min(maximumX, centerX + radiusX); cellX++)
                {
                    for (int cellY = Math.Max(minimumY, centerY - radiusY); cellY <= Math.Min(maximumY, centerY + radiusY); cellY++)
                    {
                        float normalizedX = (cellX - centerX) / (float)radiusX;
                        float normalizedY = (cellY - centerY) / (float)radiusY;
                        if (profile.shape == ProceduralRoomShape.Chambers ||
                            normalizedX * normalizedX + normalizedY * normalizedY <= 1f)
                        {
                            layout.Ground.Add(new Vector3Int(cellX, cellY, 0));
                        }
                    }
                }
            }
        }

        layout.BackwardGateway = layout.EdgeCell(profile.EntranceDirection, minimumX, minimumY, maximumX, maximumY);
        layout.ForwardGateway = layout.EdgeCell(profile.forwardDirection, minimumX, minimumY, maximumX, maximumY);
        layout.BackwardLanding = layout.BackwardGateway - DirectionOffset(profile.EntranceDirection) * LandingDistance(profile.backwardColliderSize);
        layout.ForwardLanding = layout.ForwardGateway - DirectionOffset(profile.forwardDirection) * LandingDistance(profile.forwardColliderSize);
        if (profile.isSecretRoom)
        {
            layout.ForwardGateway = layout.BackwardGateway;
            layout.ForwardLanding = layout.BackwardLanding;
        }

        if (profile.UsesSecretEntrance)
        {
            Vector3Int edge = layout.EdgeCell(profile.secretDirection, minimumX, minimumY, maximumX, maximumY);
            Vector3Int outward = DirectionOffset(profile.secretDirection);
            Vector3Int tangent = outward.x != 0 ? Vector3Int.up : Vector3Int.right;
            layout.SecretOpening = edge + outward;
            layout.SecretGateway = edge + outward * profile.secretPathLength;
            layout.SecretLanding = edge - outward * LandingDistance(profile.secretColliderSize);
            for (int depth = 1; depth <= profile.secretPathLength; depth++)
            {
                for (int width = -profile.secretPathHalfWidth; width <= profile.secretPathHalfWidth; width++)
                {
                    Vector3Int cell = edge + outward * depth + tangent * width;
                    layout.SecretPath.Add(cell);
                    layout.Ground.Add(cell);
                    layout.Reserved.Add(cell);
                }
            }
        }

        foreach (Vector3Int cell in layout.Ground)
        {
            layout.Walkable.Add(cell);

            for (int offsetX = -1; offsetX <= 1; offsetX++)
            {
                for (int offsetY = -1; offsetY <= 1; offsetY++)
                {
                    Vector3Int neighbor = cell + new Vector3Int(offsetX, offsetY, 0);
                    if (!layout.Ground.Contains(neighbor))
                        layout.Walls.Add(neighbor);
                }
            }
            foreach (Vector3Int offset in new[] { Vector3Int.right, Vector3Int.up, Vector3Int.right + Vector3Int.up })
            {
                Vector3Int neighbor = cell + offset;
                if (!layout.Ground.Contains(neighbor) && !layout.Ground.Contains(neighbor + Vector3Int.right) &&
                    !layout.Ground.Contains(neighbor + Vector3Int.up))
                    layout.VisualWalls.Add(neighbor);
            }
        }

        foreach (Vector3Int cell in layout.Walkable)
        {
            if (Math.Abs(cell.y) <= halfWidth || Math.Abs(cell.x) <= halfWidth)
                layout.Reserved.Add(cell);
        }

        if (profile.UsesFloorGateway)
            layout.PlaceTrapdoor(random);
        layout.Validate(profile.UsesFloorGateway);
        return layout;
    }

    public static Vector3Int DirectionOffset(GatewayDirection direction)
    {
        switch (direction)
        {
            case GatewayDirection.NorthEast: return Vector3Int.right;
            case GatewayDirection.NorthWest: return Vector3Int.up;
            case GatewayDirection.SouthWest: return Vector3Int.left;
            case GatewayDirection.SouthEast: return Vector3Int.down;
            default: throw new ArgumentOutOfRangeException(nameof(direction));
        }
    }

    public static void ValidateGatewaySettings(ProceduralRoomProfile profile)
    {
        if (profile.wallHeightInTiles < 1 || profile.wallHeightInTiles > 8)
            throw new ArgumentException("Use wall height 1-8 tile Z layers.");
        DirectionOffset(profile.forwardDirection);
        DirectionOffset(profile.EntranceDirection);
        if (!profile.isSecretRoom && profile.forwardDirection == profile.backwardDirection)
            throw new ArgumentException("Choose distinct sides for forward and backward gateways.");
        if (profile.UsesSecretEntrance)
        {
            DirectionOffset(profile.secretDirection);
            if (profile.secretDirection == profile.forwardDirection || profile.secretDirection == profile.backwardDirection)
                throw new ArgumentException("Choose an unused side for the secret entrance.");
            if (profile.secretPathLength < 6 || profile.secretPathLength > 48 || profile.secretPathHalfWidth < 2 || profile.secretPathHalfWidth > 12)
                throw new ArgumentException("Secret paths need length 6-48 and half-width 2-12 cells.");
        }
        foreach (Vector2 size in profile.isSecretRoom ? new[] { profile.backwardColliderSize } : profile.UsesSecretEntrance
            ? new[] { profile.forwardColliderSize, profile.backwardColliderSize, profile.secretColliderSize }
            : new[] { profile.forwardColliderSize, profile.backwardColliderSize })
        {
            if (float.IsNaN(size.x) || float.IsNaN(size.y) || size.x < 0.25f || size.x > profile.corridorHalfWidth * 2f || size.y < 0.25f || size.y > 4f)
                throw new ArgumentException("Gateway width must fit the reserved corridor (0.25 to twice Corridor Half Width); depth must be 0.25-4 cells.");
        }
    }

    private static int LandingDistance(Vector2 size) => Mathf.CeilToInt(size.y * 0.5f) + 2;

    private void CreateCavern(ProceduralRoomProfile profile, System.Random random, int minimumX, int minimumY, int maximumX, int maximumY)
    {
        if (float.IsNaN(profile.cavernFloorFill) || profile.cavernFloorFill < 0.4f || profile.cavernFloorFill > 0.7f ||
            profile.cavernSmoothingPasses < 1 || profile.cavernSmoothingPasses > 6)
            throw new ArgumentException("Cavern floor fill must be 0.4-0.7 and smoothing passes 1-6.");

        bool[,] floor = new bool[profile.width, profile.height];
        float radiusX = (profile.width - 2f) * 0.5f;
        float radiusY = (profile.height - 2f) * 0.5f;
        for (int cellX = 1; cellX < profile.width - 1; cellX++)
        {
            for (int cellY = 1; cellY < profile.height - 1; cellY++)
            {
                float normalizedX = (cellX + minimumX) / radiusX;
                float normalizedY = (cellY + minimumY) / radiusY;
                floor[cellX, cellY] = random.NextDouble() < profile.cavernFloorFill &&
                    normalizedX * normalizedX + normalizedY * normalizedY <= 1f;
            }
        }
        for (int pass = 0; pass < profile.cavernSmoothingPasses; pass++)
        {
            bool[,] smoothed = new bool[profile.width, profile.height];
            for (int cellX = 1; cellX < profile.width - 1; cellX++)
            {
                for (int cellY = 1; cellY < profile.height - 1; cellY++)
                {
                    int neighbors = 0;
                    for (int offsetX = -1; offsetX <= 1; offsetX++)
                    {
                        for (int offsetY = -1; offsetY <= 1; offsetY++)
                        {
                            if ((offsetX != 0 || offsetY != 0) && floor[cellX + offsetX, cellY + offsetY])
                                neighbors++;
                        }
                    }
                    smoothed[cellX, cellY] = neighbors >= 5 || (floor[cellX, cellY] && neighbors >= 4);
                }
            }
            floor = smoothed;
        }
        for (int cellX = 0; cellX < profile.width; cellX++)
        {
            for (int cellY = 0; cellY < profile.height; cellY++)
            {
                if (floor[cellX, cellY])
                    Ground.Add(new Vector3Int(cellX + minimumX, cellY + minimumY, 0));
            }
        }
        FillCavernDisc(Vector2.zero, profile.corridorHalfWidth + 2, minimumX, minimumY, maximumX, maximumY, false);
        Ground.IntersectWith(Flood(Ground, Vector3Int.zero));
        CarveCavernApproach(profile.EntranceDirection, profile.backwardColliderSize, profile, random, minimumX, minimumY, maximumX, maximumY);
        if (!profile.isSecretRoom)
            CarveCavernApproach(profile.forwardDirection, profile.forwardColliderSize, profile, random, minimumX, minimumY, maximumX, maximumY);
        if (profile.UsesSecretEntrance)
            CarveCavernApproach(profile.secretDirection, profile.secretColliderSize, profile, random, minimumX, minimumY, maximumX, maximumY);
    }

    private void CarveCavernApproach(GatewayDirection direction, Vector2 size, ProceduralRoomProfile profile,
        System.Random random, int minimumX, int minimumY, int maximumX, int maximumY)
    {
        Vector3Int edge = EdgeCell(direction, minimumX, minimumY, maximumX, maximumY);
        Vector3Int outward = DirectionOffset(direction);
        Vector2 tangent = outward.x != 0 ? Vector2.up : Vector2.right;
        int depth = LandingDistance(size) + 1;
        for (int step = 0; step <= depth; step++)
            FillCavernDisc((Vector2Int)(edge - outward * step), profile.corridorHalfWidth, minimumX, minimumY, maximumX, maximumY, true);
        Vector2 start = (Vector2Int)(edge - outward * depth);
        float span = outward.x != 0 ? profile.height : profile.width;
        Vector2 control = start * 0.5f + tangent * ((float)random.NextDouble() * 2f - 1f) * span * 0.25f;
        int samples = (profile.width + profile.height) * 2;
        for (int sample = 0; sample <= samples; sample++)
        {
            float progress = sample / (float)samples;
            Vector2 center = (1f - progress) * (1f - progress) * start + 2f * (1f - progress) * progress * control;
            FillCavernDisc(center, profile.corridorHalfWidth, minimumX, minimumY, maximumX, maximumY, true);
        }
    }

    private void FillCavernDisc(Vector2 center, int radius, int minimumX, int minimumY, int maximumX, int maximumY, bool reserve)
    {
        for (int cellX = Math.Max(minimumX, Mathf.FloorToInt(center.x) - radius); cellX <= Math.Min(maximumX, Mathf.CeilToInt(center.x) + radius); cellX++)
        {
            for (int cellY = Math.Max(minimumY, Mathf.FloorToInt(center.y) - radius); cellY <= Math.Min(maximumY, Mathf.CeilToInt(center.y) + radius); cellY++)
            {
                if ((new Vector2(cellX, cellY) - center).sqrMagnitude > radius * radius)
                    continue;
                Vector3Int cell = new Vector3Int(cellX, cellY, 0);
                Ground.Add(cell);
                if (reserve)
                    Reserved.Add(cell);
            }
        }
    }

    private void PlaceTrapdoor(System.Random random)
    {
        List<Vector3Int> candidates = new List<Vector3Int>();
        foreach (Vector3Int origin in OrderedCells(Ground))
        {
            bool safe = true;
            for (int offsetX = -1; offsetX <= 2 && safe; offsetX++)
            {
                for (int offsetY = -1; offsetY <= 2; offsetY++)
                {
                    Vector3Int cell = origin + new Vector3Int(offsetX, offsetY, 0);
                    if (!Ground.Contains(cell) || SecretPath.Contains(cell) || cell == SecretLanding ||
                        cell == BackwardLanding || cell == ForwardLanding ||
                        (cell - BackwardGateway).sqrMagnitude < 16 || (cell - ForwardGateway).sqrMagnitude < 16)
                    {
                        safe = false;
                        break;
                    }
                }
            }
            if (safe)
                candidates.Add(origin);
        }
        if (candidates.Count == 0)
            throw new InvalidOperationException("No interior 2x2 trapdoor with clearance fits this room.");
        FloorGateway = candidates[random.Next(candidates.Count)];
        for (int offsetX = -1; offsetX <= 2; offsetX++)
        {
            for (int offsetY = -1; offsetY <= 2; offsetY++)
            {
                Vector3Int cell = FloorGateway + new Vector3Int(offsetX, offsetY, 0);
                Reserved.Add(cell);
                if (offsetX >= 0 && offsetX <= 1 && offsetY >= 0 && offsetY <= 1)
                    FloorCells.Add(cell);
            }
        }
    }

    private Vector3Int EdgeCell(GatewayDirection direction, int minimumX, int minimumY, int maximumX, int maximumY)
    {
        switch (direction)
        {
            case GatewayDirection.NorthEast: return new Vector3Int(maximumX, 0, 0);
            case GatewayDirection.NorthWest: return new Vector3Int(0, maximumY, 0);
            case GatewayDirection.SouthWest: return new Vector3Int(minimumX, 0, 0);
            case GatewayDirection.SouthEast: return new Vector3Int(0, minimumY, 0);
            default: throw new ArgumentOutOfRangeException(nameof(direction));
        }
    }

    public void Validate(bool includeFloorGateway)
    {
        if (!Walkable.Contains(PlayerSpawn) || !Walkable.Contains(BackwardGateway) ||
            !Walkable.Contains(ForwardGateway) || !Walkable.Contains(ForwardLanding) ||
            (includeFloorGateway && (FloorCells.Count != 4 || !FloorCells.IsSubsetOf(Walkable))))
        {
            throw new InvalidOperationException("Spawn points and gateways must lie on safe walkable cells.");
        }

        if (Flood(Walkable, PlayerSpawn).Count != Walkable.Count)
            throw new InvalidOperationException("The room contains disconnected navigation cells.");

        if (SecretPath.Count > 0 && (!SecretPath.IsSubsetOf(Walkable) || !Walkable.Contains(SecretLanding)))
            throw new InvalidOperationException("The secret path and return landing must remain walkable.");

        if (Walls.Overlaps(Ground) || Walls.Overlaps(Walkable) || !Walkable.IsSubsetOf(Ground) ||
            !VisualWalls.IsSubsetOf(Walls) || Detectable.Overlaps(Walkable))
            throw new InvalidOperationException("Room terrain, collision, and navigation disagree.");
    }

    public static ProceduralTileSides NeighborMask(HashSet<Vector3Int> cells, Vector3Int cell, bool missing)
    {
        int mask = 0;
        for (int index = 0; index < Neighbors.Length; index++)
        {
            if (cells.Contains(cell + Neighbors[index]) != missing)
                mask |= 1 << index;
        }
        return (ProceduralTileSides)mask;
    }

    public static List<Vector3Int> OrderedCells(IEnumerable<Vector3Int> cells)
    {
        List<Vector3Int> ordered = new List<Vector3Int>(cells);
        ordered.Sort((first, second) => first.y != second.y ? first.y.CompareTo(second.y) : first.x.CompareTo(second.x));
        return ordered;
    }

    private void FillRectangle(int minimumX, int minimumY, int maximumX, int maximumY)
    {
        for (int cellX = minimumX; cellX <= maximumX; cellX++)
        {
            for (int cellY = minimumY; cellY <= maximumY; cellY++)
                Ground.Add(new Vector3Int(cellX, cellY, 0));
        }
    }

    private static HashSet<Vector3Int> Flood(HashSet<Vector3Int> cells, Vector3Int start)
    {
        HashSet<Vector3Int> reached = new HashSet<Vector3Int>();
        if (!cells.Contains(start))
            return reached;

        Queue<Vector3Int> pending = new Queue<Vector3Int>();
        reached.Add(start);
        pending.Enqueue(start);
        while (pending.Count > 0)
        {
            Vector3Int cell = pending.Dequeue();
            foreach (Vector3Int offset in Neighbors)
            {
                Vector3Int neighbor = cell + offset;
                if (cells.Contains(neighbor) && reached.Add(neighbor))
                    pending.Enqueue(neighbor);
            }
        }
        return reached;
    }
}