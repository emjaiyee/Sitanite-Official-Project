using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

public static class ProceduralRoomBuilder
{
    public static readonly Vector3 CellSize = new Vector3(0.32f, 0.16f, 2f);

    public static GameObject Build(ProceduralRoomProfile profile, int seed, bool registerUndo = true, Scene targetScene = default)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before generating editor rooms.");
        ValidateProfile(profile);
        ProceduralRoomLayout layout = ProceduralRoomLayout.Generate(profile, seed);
        System.Random random = new System.Random(seed);
        GameObject root = new GameObject($"ProceduralRoom_{profile.shape}_{seed}");
        if (targetScene.IsValid())
            SceneManager.MoveGameObjectToScene(root, targetScene);
        try
        {
            root.AddComponent<RoomInstance>();
            Grid grid = root.AddComponent<Grid>();
            grid.cellLayout = GridLayout.CellLayout.IsometricZAsY;
            grid.cellSwizzle = GridLayout.CellSwizzle.XYZ;
            grid.cellSize = CellSize;
            grid.cellGap = Vector3.zero;

            Tilemap ground = CreateMap(root, "GroundLevel0", profile, true);
            Tilemap walls = CreateMap(root, "WallVisuals0", profile, true);
            Tilemap walkable = CreateMap(root, "WalkableTiles0", profile, false);
            Tilemap detectable = CreateMap(root, "DetectableTiles", profile, false);
            Tilemap collision = CreateMap(root, "ColliderLevel0", profile, false);
            Vector3 surfaceOffset = Vector3.up * (CellSize.y * profile.surfaceHeightInTiles);
            walkable.transform.localPosition = surfaceOffset;
            detectable.transform.localPosition = surfaceOffset;
            collision.transform.localPosition = surfaceOffset;
            walkable.gameObject.AddComponent<AStarWalkableMap>();
            walkable.gameObject.AddComponent<AStarWalkableGizmo>();
            detectable.gameObject.AddComponent<AStarDetectionMap>();
            detectable.gameObject.AddComponent<AStarDetectionGizmo>();

            PaintVisuals(ground, layout.Ground, profile.groundTiles, profile.groundEdges, layout.Ground, true, random);
            PaintVisuals(walls, layout.VisualWalls, profile.wallTiles, profile.wallEdges, layout.Ground, false, random);
            Paint(collision, layout.Walls, profile.collisionTile);

            Rigidbody2D body = collision.gameObject.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;
            CompositeCollider2D composite = collision.gameObject.AddComponent<CompositeCollider2D>();
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
            RebuildBoundaryCollision(collision);

            AddVisibility(ground, null, null);
            AddVisibility(walls, null, null);
            AddVisibility(collision, null, composite);

            Transform helpers = Child(root.transform, "RoomMarkers").transform;
            GameObject spawn = Child(helpers, "PlayerSpawn");
            spawn.transform.position = walkable.GetCellCenterWorld(layout.PlayerSpawn);
            spawn.AddComponent<RoomSpawnPoint>();
            if (profile.isSecretRoom)
            {
                CreateDestination(helpers, "SecretForwardDestination", walkable, layout.BackwardLanding, profile.secretRoomBackwardDirection, GatewayFlow.SecretForward);
                CreateGateway(helpers, "SecretBackwardGateway", walkable, layout.BackwardGateway, profile.secretRoomBackwardDirection,
                    GatewayFlow.SecretBackward, profile.backwardColliderSize, profile);
            }
            else
            {
                CreateDestination(helpers, "ForwardDestination", walkable, layout.BackwardLanding, profile.backwardDirection, GatewayFlow.Forward);
                CreateDestination(helpers, "BackwardDestination", walkable, layout.ForwardLanding, profile.forwardDirection, GatewayFlow.Backward);
                CreateGateway(helpers, "BackwardGateway", walkable, layout.BackwardGateway, profile.backwardDirection, GatewayFlow.Backward, profile.backwardColliderSize, profile);
                CreateGateway(helpers, "ForwardGateway", walkable, layout.ForwardGateway, profile.forwardDirection, GatewayFlow.Forward, profile.forwardColliderSize, profile);
            }
            if (profile.UsesFloorGateway)
                CreateTrapdoor(root, helpers, walkable, layout, profile);
            if (profile.UsesSecretEntrance)
                CreateSecretEntrance(root, helpers, ground, walls, walkable, layout, profile, random);

            PlaceDecorations(root, walkable, layout, profile, random);
            Paint(walkable, layout.Walkable, profile.navigationTile);
            Paint(detectable, layout.Detectable, profile.navigationTile);
            PlaceLights(root, walkable, layout, profile, random);
            PlaceEnemies(root, walkable, layout, profile, random);
            BuildWallLayers(root, walkable, layout.Ground, profile);
            if (profile.baseZ != 0)
                ProceduralRoomComposer.ShiftSectionCells(root, profile.baseZ);
            if (profile.elevationLevel != 0)
                ProceduralRoomComposer.ShiftSectionLevels(root, profile.elevationLevel);
            foreach (RoomSpawnPoint point in root.GetComponentsInChildren<RoomSpawnPoint>(true))
                Configure(point, serialized => serialized.FindProperty("elevationLevel").intValue = profile.elevationLevel);
            foreach (Gateway gateway in root.GetComponentsInChildren<Gateway>(true))
                Configure(gateway, serialized => serialized.FindProperty("elevationLevel").intValue = profile.elevationLevel);
            foreach (ElevationDestination destination in root.GetComponentsInChildren<ElevationDestination>(true))
                Configure(destination, serialized => serialized.FindProperty("elevationLevel").intValue = profile.elevationLevel);
            composite.GenerateGeometry();
            ValidateRoom(root);
            if (registerUndo)
                Undo.RegisterCreatedObjectUndo(root, "Create Procedural Room");
            return root;
        }
        catch
        {
            Object.DestroyImmediate(root);
            throw;
        }
    }

    public static GameObject SaveNewPrefab(GameObject root, string path)
    {
        if (EditorUtility.IsPersistent(root))
            throw new InvalidOperationException("Bake a scene room, not an existing prefab asset.");
        ValidateRoom(root);
        if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/", StringComparison.Ordinal) ||
            !path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Choose a prefab path under Assets.");
        path = AssetDatabase.GenerateUniqueAssetPath(path);
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path, out bool success);
        if (!success || prefab == null)
            throw new InvalidOperationException("Unity could not save the room prefab.");
        return prefab;
    }

    public static void EnsureUtilityTiles(ProceduralRoomProfile profile)
    {
        if (profile == null || !EditorUtility.IsPersistent(profile))
            throw new InvalidOperationException("Save the profile as an asset before creating utility tiles.");

        if (profile.navigationTile == null)
        {
            Tile tile = ScriptableObject.CreateInstance<Tile>();
            tile.name = "Invisible Navigation";
            tile.colliderType = Tile.ColliderType.None;
            AssetDatabase.AddObjectToAsset(tile, profile);
            profile.navigationTile = tile;
        }
        if (profile.collisionTile == null)
        {
            Tile tile = ScriptableObject.CreateInstance<Tile>();
            tile.name = "Invisible Grid Collision";
            tile.colliderType = Tile.ColliderType.Grid;
            AssetDatabase.AddObjectToAsset(tile, profile);
            profile.collisionTile = tile;
        }
        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssetIfDirty(profile);
    }

    public static void ValidateRoom(GameObject root)
    {
        if (root == null || root.GetComponent<RoomInstance>() == null || root.GetComponent<Grid>() == null)
            throw new InvalidOperationException("Select a generated room root with RoomInstance and Grid.");

        Grid grid = root.GetComponent<Grid>();
        if (grid.cellLayout != GridLayout.CellLayout.IsometricZAsY || grid.cellSwizzle != GridLayout.CellSwizzle.XYZ ||
            (grid.cellSize - CellSize).sqrMagnitude > 0.000001f || grid.cellGap != Vector3.zero ||
            root.transform.localScale != Vector3.one || Quaternion.Angle(root.transform.localRotation, Quaternion.identity) > 0.001f)
            throw new InvalidOperationException("The room Grid must match Isometric Z As Y, XYZ, and cell size (0.32, 0.16, 2).");

        AStarWalkableMap[] maps = root.GetComponentsInChildren<AStarWalkableMap>(true);
        if (maps.Length != 1 || maps[0].ElevationLevel < 0)
            throw new InvalidOperationException("The flat generator validates one walkable map at a nonnegative elevation. Use the Composer for multiple sections.");
        Tilemap map = maps[0].GetComponent<Tilemap>();
        int elevationLevel = maps[0].ElevationLevel;
        ProceduralTilemapPlane plane = map.GetComponent<ProceduralTilemapPlane>();
        int baseZ = plane != null ? plane.baseZ : 0;
        if (map.GetComponent<AStarWalkableGizmo>() == null)
            throw new InvalidOperationException("The walkable map requires AStarWalkableGizmo.");
        HashSet<Vector3Int> cells = GetCells(map);
        RoomSpawnPoint spawn = root.GetComponentInChildren<RoomSpawnPoint>(true);
        if (spawn == null || spawn.ElevationLevel != elevationLevel || !cells.Contains(ProceduralTilemapPlane.WorldToCell(map, spawn.transform.position)))
            throw new InvalidOperationException("PlayerSpawn must be on the walkable map and share its elevation level.");

        HashSet<Vector3Int> reached = new HashSet<Vector3Int>();
        Queue<Vector3Int> queue = new Queue<Vector3Int>();
        Vector3Int start = ProceduralTilemapPlane.WorldToCell(map, spawn.transform.position);
        queue.Enqueue(start);
        reached.Add(start);
        Vector3Int[] offsets = { Vector3Int.right, Vector3Int.up, Vector3Int.left, Vector3Int.down };
        while (queue.Count > 0)
        {
            Vector3Int cell = queue.Dequeue();
            foreach (Vector3Int offset in offsets)
            {
                Vector3Int neighbor = cell + offset;
                if (cells.Contains(neighbor) && reached.Add(neighbor))
                    queue.Enqueue(neighbor);
            }
        }
        if (reached.Count != cells.Count)
            throw new InvalidOperationException("Edited walkable tiles contain disconnected regions.");

        Physics2D.SyncTransforms();
        Gateway[] gateways = root.GetComponentsInChildren<Gateway>(true);
        ElevationDestination[] destinations = root.GetComponentsInChildren<ElevationDestination>(true);
        bool secretRoom = !gateways.Any(gateway => gateway.Flow == GatewayFlow.Forward || gateway.Flow == GatewayFlow.Backward);
        if (secretRoom && (gateways.Length != 1 || gateways[0].Flow != GatewayFlow.SecretBackward ||
            destinations.Length != 1 || destinations[0].Flow != GatewayFlow.SecretForward || destinations[0].Direction != gateways[0].Direction))
            throw new InvalidOperationException("Secret rooms require one SecretBackward gateway and one SecretForward arrival at its inward landing.");
        GatewayFlow[] roomFlows = secretRoom ? Array.Empty<GatewayFlow>() : new[] { GatewayFlow.Forward, GatewayFlow.Backward };
        foreach (GatewayFlow flow in roomFlows)
        {
            Gateway gateway = gateways.FirstOrDefault(candidate => candidate.Flow == flow);
            Gateway landingGateway = gateways.FirstOrDefault(candidate => candidate.Flow == roomFlows.First(other => other != flow));
            if (gateway == null || landingGateway == null || !destinations.Any(destination =>
                destination.Flow == flow && destination.Direction == landingGateway.Direction))
                throw new InvalidOperationException("Forward destinations must follow the backward entrance; backward destinations must follow the forward exit.");
        }
        foreach (Gateway gateway in gateways)
        {
            Collider2D trigger = gateway.GetComponent<Collider2D>();
            if (trigger == null || !trigger.isTrigger || gateway.GetComponent<GatewayVisibility>() == null ||
                gateway.ElevationLevel != elevationLevel || !cells.Contains(ProceduralTilemapPlane.WorldToCell(map, gateway.transform.position)))
                throw new InvalidOperationException("Every gateway needs a trigger, GatewayVisibility, matching elevation, and a reachable walkable cell.");
            Vector3Int gatewayCell = ProceduralTilemapPlane.WorldToCell(map, gateway.transform.position);
            if (gateway.Flow == GatewayFlow.Floor)
            {
                Tilemap trapdoor = root.transform.Find("FloorTrapdoorTiles")?.GetComponent<Tilemap>();
                if (gateway.GetComponent<GatewayFloorVisibility>() == null || trapdoor == null || GetCells(trapdoor).Count != 4)
                    throw new InvalidOperationException("Floor gateways need GatewayFloorVisibility and four trapdoor tiles.");
                continue;
            }
            Vector3Int outward = ProceduralRoomLayout.DirectionOffset(gateway.Direction);
            if (cells.Contains(gatewayCell + outward))
                throw new InvalidOperationException($"{gateway.name} does not lie on its {gateway.Direction} edge.");
        }
        foreach (ElevationDestination destination in destinations)
        {
            Vector3Int destinationCell = ProceduralTilemapPlane.WorldToCell(map, destination.transform.position);
            if (destination.ElevationLevel != elevationLevel || !cells.Contains(destinationCell))
                throw new InvalidOperationException($"{destination.name} at {destination.transform.position} maps to {destinationCell}, which is not a walkable destination at elevation {elevationLevel}.");
            foreach (Gateway gateway in gateways)
            {
                Collider2D trigger = gateway.GetComponent<Collider2D>();
                if (trigger.OverlapPoint(destination.transform.position))
                    throw new InvalidOperationException($"{destination.name} at {destination.transform.position} overlaps {gateway.name} at {gateway.transform.position}; trigger bounds: {trigger.bounds}.");
            }
        }
        foreach (EnemySpawnPoint point in root.GetComponentsInChildren<EnemySpawnPoint>(true))
        {
            if (new SerializedObject(point).FindProperty("elevationLevel").intValue != elevationLevel ||
                !cells.Contains(ProceduralTilemapPlane.WorldToCell(map, point.transform.position)))
                throw new InvalidOperationException("An enemy spawn marker is outside the reachable walkable map or has a different elevation.");
        }

        Tilemap ground = root.transform.Find("GroundLevel0")?.GetComponent<Tilemap>();
        Tilemap collision = root.transform.Find("ColliderLevel0")?.GetComponent<Tilemap>();
        Tilemap walls = root.transform.Find("WallVisuals0")?.GetComponent<Tilemap>();
        if (ground == null || collision == null || root.GetComponentsInChildren<AStarDetectionMap>(true).Length != 1)
            throw new InvalidOperationException("Ground, collision, and detection maps are required.");
        Tilemap detection = root.transform.Find("DetectableTiles")?.GetComponent<Tilemap>();
        if (collision.transform.localPosition != map.transform.localPosition || detection.transform.localPosition != map.transform.localPosition)
            throw new InvalidOperationException("Flat collision and detection maps must share the walkable surface offset.");
        if (GetCells(detection).Count > 0 || detection.GetComponent<AStarDetectionGizmo>() == null)
            throw new InvalidOperationException("Generated detection stays empty and requires AStarDetectionGizmo; author manual stairs after baking.");
        HashSet<Vector3Int> groundCells = GetCells(ground);
        Tilemap secretPath = root.transform.Find("SecretPathGround")?.GetComponent<Tilemap>();
        if (secretPath != null)
        {
            groundCells.UnionWith(GetCells(secretPath));
            Gateway entrance = gateways.FirstOrDefault(gateway => gateway.GetComponent<GatewaySecretVisibility>() != null);
            if (entrance == null || !destinations.Any(destination => destination.Flow == OppositeSecretFlow(entrance.Flow) &&
                destination.Direction == entrance.Direction))
                throw new InvalidOperationException("A secret entrance requires its matching return destination.");
        }
        if (walls != null)
        {
            foreach (Vector3Int cell in GetCells(walls))
            {
                Vector3Int footprint = new Vector3Int(cell.x, cell.y, baseZ);
                bool hasSouthGround = groundCells.Contains(footprint + Vector3Int.left) || groundCells.Contains(footprint + Vector3Int.down) ||
                    groundCells.Contains(footprint + Vector3Int.left + Vector3Int.down);
                if (groundCells.Contains(footprint) || groundCells.Contains(footprint + Vector3Int.right) || groundCells.Contains(footprint + Vector3Int.up) || !hasSouthGround)
                    throw new InvalidOperationException("Visible walls must stay outside the ground on north-facing boundaries, never south-facing edges.");
            }
        }
        foreach (Vector3Int cell in cells)
        {
            if (cell.z != baseZ || !groundCells.Contains(cell) || collision.HasTile(cell))
                throw new InvalidOperationException("Walkable cells must have ground and no boundary collision.");
        }
        foreach (Vector3Int cell in groundCells)
        {
            for (int offsetX = -1; offsetX <= 1; offsetX++)
            {
                for (int offsetY = -1; offsetY <= 1; offsetY++)
                {
                    Vector3Int neighbor = cell + new Vector3Int(offsetX, offsetY, 0);
                    if (!groundCells.Contains(neighbor) &&
                        (!collision.HasTile(neighbor) || collision.GetColliderType(neighbor) != Tile.ColliderType.Grid))
                        throw new InvalidOperationException("Ground boundary has a missing Grid collision tile. Update ColliderLevel0 after editing ground.");
                }
            }
        }
        foreach (Tilemap wallMap in WallMaps(root))
        {
            TilemapRenderer wallRenderer = wallMap.GetComponent<TilemapRenderer>();
            TilemapRenderer groundRenderer = ground.GetComponent<TilemapRenderer>();
            if (wallMap.transform.localPosition != ground.transform.localPosition || wallMap.tileAnchor != ground.tileAnchor ||
                wallMap.orientation != ground.orientation || wallMap.orientationMatrix != ground.orientationMatrix ||
                wallRenderer.sortingLayerID != groundRenderer.sortingLayerID || wallRenderer.sortingOrder != groundRenderer.sortingOrder ||
                wallRenderer.mode != groundRenderer.mode || wallRenderer.sharedMaterial != groundRenderer.sharedMaterial)
                throw new InvalidOperationException("Wall Tilemaps must match ground placement, anchor, orientation and rendering settings. Wall height uses cell Z, not a map offset.");
            if (wallMap.name == "SecretEntranceClosedWall")
                continue;
            WallOcclusionFade fade = wallMap.GetComponent<WallOcclusionFade>();
            if (fade != null && fade.enabled)
                continue;
            foreach (Vector3Int cell in GetCells(wallMap))
            {
                if (WallOverlapsGround(wallMap, cell, map, groundCells))
                    throw new InvalidOperationException($"{wallMap.name} at {cell} projects wall artwork over ground. Move or remove the wall, not the walkable tiles.");
            }
        }
    }

    public static HashSet<Vector3Int> GetCells(Tilemap map)
    {
        HashSet<Vector3Int> cells = new HashSet<Vector3Int>();
        foreach (Vector3Int cell in map.cellBounds.allPositionsWithin)
        {
            if (map.HasTile(cell))
                cells.Add(cell);
        }
        return cells;
    }

    public static void RebuildBoundaryCollision(Tilemap collision)
    {
        if (collision == null || collision.GetComponent<CompositeCollider2D>() == null)
            throw new InvalidOperationException("Select the generated ColliderLevel0 map with its CompositeCollider2D.");
        PolygonCollider2D polygon = collision.GetComponent<PolygonCollider2D>();
        if (polygon == null)
            polygon = collision.gameObject.AddComponent<PolygonCollider2D>();
        CompositeCollider2D composite = collision.GetComponent<CompositeCollider2D>();
        composite.generationType = CompositeCollider2D.GenerationType.Manual;
        polygon.compositeOperation = Collider2D.CompositeOperation.Merge;
        List<Vector3Int> cells = ProceduralRoomLayout.OrderedCells(GetCells(collision));
        polygon.pathCount = cells.Count;
        foreach (Vector3Int cell in cells)
        {
            if (collision.GetColliderType(cell) != Tile.ColliderType.Grid)
                throw new InvalidOperationException("Boundary collision tiles must use Grid collider metadata.");
        }
        for (int index = 0; index < cells.Count; index++)
        {
            Vector3Int cell = cells[index];
            Vector3 centerWorld = collision.GetCellCenterWorld(cell);
            Vector2 center = collision.transform.InverseTransformPoint(centerWorld);
            Vector2 axisX = (Vector2)collision.transform.InverseTransformVector(collision.GetCellCenterWorld(cell + Vector3Int.right) - centerWorld) * 0.5f;
            Vector2 axisY = (Vector2)collision.transform.InverseTransformVector(collision.GetCellCenterWorld(cell + Vector3Int.up) - centerWorld) * 0.5f;
            polygon.SetPath(index, new[] { center + axisX + axisY, center - axisX + axisY,
                center - axisX - axisY, center + axisX - axisY });
        }
        composite.GenerateGeometry();
    }

    private static void ValidateProfile(ProceduralRoomProfile profile)
    {
        if (profile == null || profile.navigationTile == null || profile.collisionTile == null)
            throw new InvalidOperationException("Assign a profile and create its navigation/collision utility tiles.");
        if (profile.elevationLevel < 0 || profile.baseZ < 0)
            throw new InvalidOperationException("Room Elevation Level and Base Z must be nonnegative.");
        if (profile.groundTiles == null || !profile.groundTiles.Any(entry => entry != null && entry.tile != null))
            throw new InvalidOperationException("Assign at least one Ground Tile, or import a ground palette.");
        foreach (ProceduralWeightedTile entry in profile.groundTiles.Concat(profile.wallTiles))
        {
            if (entry == null || entry.tile == null || entry.weight < 1 || entry.weight > 10000 || !EditorUtility.IsPersistent(entry.tile))
                throw new InvalidOperationException("Tile pools need saved TileBase assets with weights between 1 and 10000.");
        }
        foreach (ProceduralEdgeTile entry in profile.groundEdges.Concat(profile.wallEdges))
        {
            if (entry == null || entry.tile == null || !EditorUtility.IsPersistent(entry.tile))
                throw new InvalidOperationException("Each edge rule needs a saved TileBase asset.");
        }
        if (!EditorUtility.IsPersistent(profile.navigationTile) || !EditorUtility.IsPersistent(profile.collisionTile))
            throw new InvalidOperationException("Navigation and collision tiles must be saved assets, not temporary objects.");
        ProceduralRoomLayout.ValidateGatewaySettings(profile);
        if (profile.UsesFloorGateway && new[] { profile.trapdoorNorthEast, profile.trapdoorNorthWest,
            profile.trapdoorSouthEast, profile.trapdoorSouthWest }.Any(tile => tile == null || !EditorUtility.IsPersistent(tile)))
            throw new InvalidOperationException("Assign all four saved flat trapdoor tiles, or disable Include Floor Gateway.");
        if (profile.enemySpawnPoints < 0 || profile.enemySpawnPoints > 24 || profile.decorationCount < 0 || profile.decorationCount > 64 ||
            profile.lightCount < 0 || profile.lightCount > 64 || profile.lightSpacing < 1 ||
            profile.decorationClearance < 0 || profile.decorationClearance > 4 ||
            float.IsNaN(profile.surfaceHeightInTiles) || profile.surfaceHeightInTiles < 0f || profile.surfaceHeightInTiles > 4f ||
            float.IsNaN(profile.southGatewayLightIntensity) || profile.southGatewayLightIntensity < 0f ||
            float.IsNaN(profile.southGatewayLightRadius) || profile.southGatewayLightRadius <= 0f || float.IsInfinity(profile.southGatewayLightRadius))
            throw new InvalidOperationException("Population, cube surface offset, or gateway light settings are outside the profile limits.");
        if (profile.enemySpawnPointPrefab != null && (!PrefabUtility.IsPartOfPrefabAsset(profile.enemySpawnPointPrefab) ||
            profile.enemySpawnPointPrefab.GetComponentsInChildren<EnemySpawnPoint>(true).Length != 1))
            throw new InvalidOperationException("Enemy Spawn Point Prefab must be a prefab containing exactly one EnemySpawnPoint.");
        if (profile.gatewayVisualPrefab != null && (!PrefabUtility.IsPartOfPrefabAsset(profile.gatewayVisualPrefab) ||
            profile.gatewayVisualPrefab.GetComponentsInChildren<Collider2D>(true).Length > 0 ||
            profile.gatewayVisualPrefab.GetComponentsInChildren<Gateway>(true).Length > 0))
            throw new InvalidOperationException("Gateway Visual Prefab must be a visual-only prefab without colliders or Gateway components.");
        if (profile.southGatewayLightPrefab != null && (!PrefabUtility.IsPartOfPrefabAsset(profile.southGatewayLightPrefab) ||
            profile.southGatewayLightPrefab.GetComponentsInChildren<Light2D>(true).Length != 1 ||
            profile.southGatewayLightPrefab.GetComponentsInChildren<Collider2D>(true).Length != 0 ||
            profile.southGatewayLightPrefab.GetComponentsInChildren<Gateway>(true).Length != 0 ||
            profile.southGatewayLightPrefab.GetComponentsInChildren<SpriteRenderer>(true).Length != 0))
            throw new InvalidOperationException("South Gateway Light Prefab must contain one Light2D and no sprites, colliders, or Gateway components.");
        foreach (GameObject prefab in profile.decorationPrefabs)
        {
            if (prefab == null || !PrefabUtility.IsPartOfPrefabAsset(prefab) || prefab.GetComponentInChildren<RoomInstance>(true) != null ||
                prefab.GetComponentInChildren<Gateway>(true) != null || prefab.GetComponentInChildren<EnemySpawnPoint>(true) != null)
                throw new InvalidOperationException("Decoration pools must contain prop prefabs, not rooms, gateways, or enemy markers.");
        }
            foreach (GameObject prefab in profile.lightPrefabs)
            {
                if (prefab == null || !PrefabUtility.IsPartOfPrefabAsset(prefab) || prefab.GetComponentsInChildren<Light2D>(true).Length == 0 ||
                prefab.GetComponentInChildren<RoomInstance>(true) != null || prefab.GetComponentInChildren<Gateway>(true) != null ||
                prefab.GetComponentInChildren<EnemySpawnPoint>(true) != null || prefab.GetComponentsInChildren<Collider2D>(true).Any(collider => !collider.isTrigger))
                throw new InvalidOperationException("Scattered Light Prefabs must contain Light2D components and no solid colliders, rooms, gateways or enemy markers.");
            }
        if (!SortingLayer.layers.Any(layer => layer.name == profile.groundSortingLayer))
            throw new InvalidOperationException("Choose an existing sorting layer for the ground and walls.");
    }

    private static GameObject Child(Transform parent, string name)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent, false);
        return child;
    }

    private static Tilemap CreateMap(GameObject root, string name, ProceduralRoomProfile profile, bool visible)
    {
        GameObject child = Child(root.transform, name);
        Tilemap map = child.AddComponent<Tilemap>();
        map.tileAnchor = new Vector3(0.5f, 0.5f, 0f);
        map.color = visible ? Color.white : new Color(1f, 1f, 1f, 0f);
        TilemapRenderer mapRenderer = child.AddComponent<TilemapRenderer>();
        mapRenderer.enabled = visible;
        mapRenderer.mode = TilemapRenderer.Mode.Individual;
        mapRenderer.sortingLayerName = profile.groundSortingLayer;
        mapRenderer.sortingOrder = profile.groundSortingOrder;
        if (profile.tilemapMaterial != null)
            mapRenderer.sharedMaterial = profile.tilemapMaterial;
        return map;
    }

    private static void Paint(Tilemap map, IEnumerable<Vector3Int> cells, TileBase tile)
    {
        Vector3Int[] positions = ProceduralRoomLayout.OrderedCells(cells).ToArray();
        map.SetTiles(positions, Enumerable.Repeat(tile, positions.Length).ToArray());
        map.CompressBounds();
    }

    private static void PaintVisuals(Tilemap map, HashSet<Vector3Int> cells, List<ProceduralWeightedTile> pool,
        List<ProceduralEdgeTile> edges, HashSet<Vector3Int> ground, bool missing, System.Random random)
    {
        int totalWeight = pool.Sum(entry => entry.weight);
        foreach (Vector3Int cell in ProceduralRoomLayout.OrderedCells(cells))
        {
            ProceduralTileSides mask = ProceduralRoomLayout.NeighborMask(ground, cell, missing);
            ProceduralEdgeTile edge = edges.FirstOrDefault(entry => entry.sides == mask);
            TileBase chosen = edge?.tile;
            if (chosen == null && totalWeight > 0)
            {
                int roll = random.Next(totalWeight);
                foreach (ProceduralWeightedTile entry in pool)
                {
                    roll -= entry.weight;
                    if (roll < 0)
                    {
                        chosen = entry.tile;
                        break;
                    }
                }
            }
            if (chosen != null)
                map.SetTile(cell, chosen);
        }
        map.CompressBounds();
        map.RefreshAllTiles();
    }

    private static void AddVisibility(Tilemap map, TilemapCollider2D collider, CompositeCollider2D composite)
    {
        Configure(map.gameObject.AddComponent<TilemapElevationVisibility>(), serialized =>
        {
            serialized.FindProperty("tilemap").objectReferenceValue = map;
            serialized.FindProperty("tilemapCollider").objectReferenceValue = collider;
            serialized.FindProperty("compositeCollider").objectReferenceValue = composite;
            serialized.FindProperty("tilemapLevel").intValue = 0;
        });
    }

    private static IEnumerable<Tilemap> WallMaps(GameObject root)
    {
        return root.GetComponentsInChildren<Tilemap>(true).Where(map => map.name == "WallVisuals0" ||
            map.name == "SecretPathWalls" || map.name == "SecretEntranceClosedWall");
    }

    private static void BuildWallLayers(GameObject root, Tilemap surface, HashSet<Vector3Int> ground, ProceduralRoomProfile profile)
    {
        foreach (Tilemap source in WallMaps(root))
        {
            source.transform.localPosition = root.transform.Find("GroundLevel0").localPosition;
            Vector3Int[] bases = GetCells(source).ToArray();
            for (int height = 1; height < profile.wallHeightInTiles; height++)
            {
                foreach (Vector3Int cell in bases)
                    source.SetTile(new Vector3Int(cell.x, cell.y, height), source.GetTile(cell));
            }
            source.RefreshAllTiles();
            if (source.name != "SecretEntranceClosedWall" && !profile.fadeObscuringWalls)
                FilterWallColumns(source, surface, ground);
            source.CompressBounds();
        }
        if (profile.fadeObscuringWalls)
            AddWallFading(root, profile);
    }

    public static void AddWallFading(GameObject root, ProceduralRoomProfile profile = null)
    {
        AStarWalkableMap surface = root.GetComponentInChildren<AStarWalkableMap>(true);
        foreach (Tilemap map in WallMaps(root).Where(candidate => candidate.name != "SecretEntranceClosedWall"))
        {
            WallOcclusionFade fade = map.GetComponent<WallOcclusionFade>();
            if (fade == null)
                fade = map.gameObject.AddComponent<WallOcclusionFade>();
            fade.tilemapLevel = surface != null ? surface.ElevationLevel : 0;
            if (profile != null)
            {
                fade.proximityPadding = profile.wallFadePadding;
                fade.fadedAlpha = profile.wallFadedAlpha;
                fade.fadeDuration = profile.wallFadeDuration;
            }
        }
    }

    private static void FilterWallColumns(Tilemap walls, Tilemap surface, HashSet<Vector3Int> ground)
    {
        while (true)
        {
            HashSet<Vector2Int> blocked = new HashSet<Vector2Int>();
            HashSet<Vector3Int> cells = GetCells(walls);
            foreach (Vector3Int cell in cells)
            {
                if (WallOverlapsGround(walls, cell, surface, ground))
                    blocked.Add(new Vector2Int(cell.x, cell.y));
            }
            if (blocked.Count == 0)
                return;
            foreach (Vector3Int cell in cells)
            {
                if (blocked.Contains(new Vector2Int(cell.x, cell.y)))
                    walls.SetTile(cell, null);
            }
            walls.RefreshAllTiles();
        }
    }

    public static bool WallOverlapsGround(Tilemap walls, Vector3Int cell, Tilemap surface, HashSet<Vector3Int> ground)
    {
        TileBase tile = walls.GetTile(cell);
        if (tile is RuleOverrideTile overridden)
            tile = overridden.m_InstanceTile;
        IEnumerable<Sprite> sprites = new[] { walls.GetSprite(cell) };
        if (tile is RuleTile rule)
            sprites = sprites.Concat(new[] { rule.m_DefaultSprite }).Concat(rule.m_TilingRules.SelectMany(entry => entry.m_Sprites ?? Array.Empty<Sprite>()));
        else if (tile is AnimatedTile animated)
            sprites = sprites.Concat(animated.m_AnimatedSprites ?? Array.Empty<Sprite>());

        Vector3 origin = walls.GetCellCenterWorld(cell);
        Matrix4x4 transform = walls.orientationMatrix * walls.GetTransformMatrix(cell);
        Tilemap groundVisual = walls.transform.parent.Find("GroundLevel0")?.GetComponent<Tilemap>();
        TilemapRenderer wallRenderer = walls.GetComponent<TilemapRenderer>();
        TilemapRenderer groundRenderer = groundVisual != null ? groundVisual.GetComponent<TilemapRenderer>() : null;
        bool sharedSorting = groundRenderer != null && wallRenderer != null &&
            UnityEngine.Rendering.GraphicsSettings.transparencySortMode == TransparencySortMode.CustomAxis &&
            wallRenderer.mode == TilemapRenderer.Mode.Individual && groundRenderer.mode == wallRenderer.mode &&
            groundRenderer.sortingLayerID == wallRenderer.sortingLayerID && groundRenderer.sortingOrder == wallRenderer.sortingOrder;
        ProceduralTilemapPlane plane = surface.GetComponent<ProceduralTilemapPlane>();
        int baseZ = plane != null ? plane.baseZ : 0;
        Vector3Int baseCell = new Vector3Int(0, 0, baseZ);
        Vector3 surfaceOrigin = surface.GetCellCenterWorld(baseCell);
        Vector2 halfX = (surface.GetCellCenterWorld(baseCell + Vector3Int.right) - surfaceOrigin) * 0.5f;
        Vector2 halfY = (surface.GetCellCenterWorld(baseCell + Vector3Int.up) - surfaceOrigin) * 0.5f;
        foreach (Sprite sprite in sprites.Where(candidate => candidate != null).Distinct())
        {
            Bounds bounds = sprite.bounds;
            Vector2[] vertices = sprite.vertices.Select(point => (Vector2)(origin + walls.transform.TransformVector(
                transform.MultiplyPoint3x4(new Vector3(point.x, point.y, 0f))))).ToArray();
            ushort[] triangles = sprite.triangles;
            Vector2[] footprint =
            {
                origin + walls.transform.TransformVector(transform.MultiplyPoint3x4(new Vector3(bounds.min.x, bounds.min.y, 0f))),
                origin + walls.transform.TransformVector(transform.MultiplyPoint3x4(new Vector3(bounds.max.x, bounds.min.y, 0f))),
                origin + walls.transform.TransformVector(transform.MultiplyPoint3x4(new Vector3(bounds.max.x, bounds.max.y, 0f))),
                origin + walls.transform.TransformVector(transform.MultiplyPoint3x4(new Vector3(bounds.min.x, bounds.max.y, 0f)))
            };
            Vector3Int[] corners = footprint.Select(point => surface.WorldToCell(new Vector3(point.x, point.y, surfaceOrigin.z))).ToArray();
            for (int cellX = corners.Min(point => point.x) - 1; cellX <= corners.Max(point => point.x) + 1; cellX++)
            {
                for (int cellY = corners.Min(point => point.y) - 1; cellY <= corners.Max(point => point.y) + 1; cellY++)
                {
                    Vector3Int candidate = new Vector3Int(cellX, cellY, baseZ);
                    if (!ground.Contains(candidate))
                        continue;
                    if (sharedSorting && Vector3.Dot(origin - groundVisual.GetCellCenterWorld(candidate),
                        UnityEngine.Rendering.GraphicsSettings.transparencySortAxis) > 0.00001f)
                        continue;
                    Vector2 center = surface.GetCellCenterWorld(candidate);
                    Vector2[] diamond = { center + halfX + halfY, center - halfX + halfY, center - halfX - halfY, center + halfX - halfY };
                    if (!PolygonsOverlap(footprint, diamond))
                        continue;
                    for (int triangle = 0; triangle + 2 < triangles.Length; triangle += 3)
                    {
                        Vector2[] face = { vertices[triangles[triangle]], vertices[triangles[triangle + 1]], vertices[triangles[triangle + 2]] };
                        if (PolygonsOverlap(face, diamond))
                            return true;
                    }
                }
            }
        }
        return false;
    }

    private static bool PolygonsOverlap(Vector2[] first, Vector2[] second)
    {
        foreach (Vector2[] polygon in new[] { first, second })
        {
            for (int index = 0; index < polygon.Length; index++)
            {
                Vector2 edge = polygon[(index + 1) % polygon.Length] - polygon[index];
                Vector2 axis = new Vector2(-edge.y, edge.x).normalized;
                float firstMin = first.Min(point => Vector2.Dot(point, axis));
                float firstMax = first.Max(point => Vector2.Dot(point, axis));
                float secondMin = second.Min(point => Vector2.Dot(point, axis));
                float secondMax = second.Max(point => Vector2.Dot(point, axis));
                if (firstMax <= secondMin + 0.00001f || secondMax <= firstMin + 0.00001f)
                    return false;
            }
        }
        return true;
    }

    private static void CreateDestination(Transform parent, string name, Tilemap map, Vector3Int cell, GatewayDirection direction, GatewayFlow flow)
    {
        GameObject destination = Child(parent, name);
        destination.transform.position = map.GetCellCenterWorld(cell);
        Configure(destination.AddComponent<ElevationDestination>(), serialized =>
        {
            serialized.FindProperty("direction").enumValueIndex = (int)direction;
            serialized.FindProperty("flow").enumValueIndex = (int)flow;
        });
    }

    private static GatewayVisibility CreateGateway(Transform parent, string name, Tilemap map, Vector3Int cell,
        GatewayDirection direction, GatewayFlow flow, Vector2 size, ProceduralRoomProfile profile, bool secretEntrance = false)
    {
        GameObject gateway = Child(parent, name);
        gateway.transform.position = map.GetCellCenterWorld(cell);
        PolygonCollider2D collider = gateway.AddComponent<PolygonCollider2D>();
        collider.isTrigger = true;
        Vector3Int outward = ProceduralRoomLayout.DirectionOffset(direction);
        Vector3Int tangent = outward.x != 0 ? Vector3Int.up : Vector3Int.right;
        Vector2 origin = map.GetCellCenterWorld(cell);
        Vector2 normalAxis = ((Vector2)map.GetCellCenterWorld(cell + outward) - origin) * (size.y * 0.5f);
        Vector2 tangentAxis = ((Vector2)map.GetCellCenterWorld(cell + tangent) - origin) * (size.x * 0.5f);
        collider.points = new[]
        {
            normalAxis + tangentAxis, -normalAxis + tangentAxis,
            -normalAxis - tangentAxis, normalAxis - tangentAxis
        };
        Configure(gateway.AddComponent<Gateway>(), serialized =>
        {
            serialized.FindProperty("direction").enumValueIndex = (int)direction;
            serialized.FindProperty("flow").enumValueIndex = (int)flow;
        });
        SpriteRenderer visual = null;
        Light2D light = null;
        if (direction == GatewayDirection.SouthWest || direction == GatewayDirection.SouthEast)
        {
            if (profile.southGatewayLightPrefab != null)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(profile.southGatewayLightPrefab, gateway.transform);
                instance.transform.localPosition = Vector3.zero;
                light = instance.GetComponentInChildren<Light2D>(true);
            }
            else
            {
                light = Child(gateway.transform, "South Exit Light").AddComponent<Light2D>();
                light.lightType = Light2D.LightType.Point;
                light.color = profile.southGatewayLightColor;
                light.intensity = profile.southGatewayLightIntensity;
                light.pointLightOuterRadius = profile.southGatewayLightRadius;
                light.pointLightInnerRadius = profile.southGatewayLightRadius * 0.25f;
            }
        }
        else if (profile.gatewayVisualPrefab != null)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(profile.gatewayVisualPrefab, gateway.transform);
            instance.transform.localPosition = Vector3.zero;
            visual = instance.GetComponentInChildren<SpriteRenderer>(true);
        }
        GatewayVisibility visibility = secretEntrance ? gateway.AddComponent<GatewaySecretVisibility>() : gateway.AddComponent<GatewayVisibility>();
        Configure(visibility, serialized =>
        {
            serialized.FindProperty("gatewayCollider").objectReferenceValue = collider;
            serialized.FindProperty("spriteRenderer").objectReferenceValue = visual;
            serialized.FindProperty("light2D").objectReferenceValue = light;
        });
        return visibility;
    }

    private static GatewayFlow OppositeSecretFlow(GatewayFlow flow) =>
        flow == GatewayFlow.SecretForward ? GatewayFlow.SecretBackward : GatewayFlow.SecretForward;

    private static PolygonCollider2D CreateCellPolygon(Transform parent, string name, Tilemap map, Vector3 center, Vector2 size)
    {
        GameObject child = Child(parent, name);
        child.transform.position = center;
        PolygonCollider2D polygon = child.AddComponent<PolygonCollider2D>();
        Vector3 origin = map.GetCellCenterWorld(Vector3Int.zero);
        Vector2 axisX = (Vector2)(map.GetCellCenterWorld(Vector3Int.right) - origin) * (size.x * 0.5f);
        Vector2 axisY = (Vector2)(map.GetCellCenterWorld(Vector3Int.up) - origin) * (size.y * 0.5f);
        polygon.points = new[] { axisX + axisY, -axisX + axisY, -axisX - axisY, axisX - axisY };
        return polygon;
    }

    private static void CreateTrapdoor(GameObject root, Transform helpers, Tilemap surface, ProceduralRoomLayout layout, ProceduralRoomProfile profile)
    {
        Tilemap tiles = CreateMap(root, "FloorTrapdoorTiles", profile, true);
        tiles.transform.localPosition = surface.transform.localPosition;
        tiles.GetComponent<TilemapRenderer>().sortingOrder = profile.groundSortingOrder + 1;
        Vector3Int origin = layout.FloorGateway;
        tiles.SetTile(origin, profile.trapdoorSouthWest);
        tiles.SetTile(origin + Vector3Int.right, profile.trapdoorSouthEast);
        tiles.SetTile(origin + Vector3Int.up, profile.trapdoorNorthWest);
        tiles.SetTile(origin + Vector3Int.right + Vector3Int.up, profile.trapdoorNorthEast);
        tiles.CompressBounds();
        Vector3 center = (surface.GetCellCenterWorld(origin) + surface.GetCellCenterWorld(origin + Vector3Int.right + Vector3Int.up)) * 0.5f;
        PolygonCollider2D trigger = CreateCellPolygon(helpers, "FloorGateway", surface, center, new Vector2(2f, 2f));
        trigger.isTrigger = true;
        Configure(trigger.gameObject.AddComponent<Gateway>(), serialized => serialized.FindProperty("flow").enumValueIndex = (int)GatewayFlow.Floor);
        Configure(trigger.gameObject.AddComponent<GatewayFloorVisibility>(), serialized =>
        {
            serialized.FindProperty("tilemap").objectReferenceValue = tiles;
            serialized.FindProperty("tilemapRenderer").objectReferenceValue = tiles.GetComponent<TilemapRenderer>();
            serialized.FindProperty("gatewayCollider").objectReferenceValue = trigger;
        });
    }

    private static void CreateSecretEntrance(GameObject root, Transform helpers, Tilemap ground, Tilemap walls, Tilemap surface,
        ProceduralRoomLayout layout, ProceduralRoomProfile profile, System.Random random)
    {
        Tilemap path = CreateMap(root, "SecretPathGround", profile, true);
        foreach (Vector3Int cell in layout.SecretPath)
        {
            path.SetTile(cell, ground.GetTile(cell));
            ground.SetTile(cell, null);
        }
        Tilemap pathWalls = CreateMap(root, "SecretPathWalls", profile, true);
        foreach (Vector3Int cell in ProceduralRoomBuilder.GetCells(walls).ToArray())
        {
            bool touchesPath = false;
            for (int offsetX = -1; offsetX <= 1; offsetX++)
            {
                for (int offsetY = -1; offsetY <= 1; offsetY++)
                    touchesPath |= layout.SecretPath.Contains(cell + new Vector3Int(offsetX, offsetY, 0));
            }
            if (touchesPath)
            {
                pathWalls.SetTile(cell, walls.GetTile(cell));
                walls.SetTile(cell, null);
            }
        }
        Tilemap closedWall = CreateMap(root, "SecretEntranceClosedWall", profile, true);
        Vector3Int outward = ProceduralRoomLayout.DirectionOffset(profile.secretDirection);
        Vector3Int tangent = outward.x != 0 ? Vector3Int.up : Vector3Int.right;
        if (profile.secretDirection == GatewayDirection.NorthEast || profile.secretDirection == GatewayDirection.NorthWest)
        {
            HashSet<Vector3Int> closedCells = new HashSet<Vector3Int>();
            for (int width = -profile.secretPathHalfWidth; width <= profile.secretPathHalfWidth; width++)
                closedCells.Add(layout.SecretOpening + tangent * width);
            PaintVisuals(closedWall, closedCells, profile.wallTiles, profile.wallEdges, layout.Ground, false, random);
        }
        float span = profile.secretPathHalfWidth * 2f + 1f;
        PolygonCollider2D blocker = CreateCellPolygon(helpers, "SecretEntranceBlocker", surface,
            surface.GetCellCenterWorld(layout.SecretOpening), outward.x != 0 ? new Vector2(1f, span) : new Vector2(span, 1f));
        GatewayVisibility visibility = CreateGateway(helpers, "SecretGateway", surface, layout.SecretGateway, profile.secretDirection,
            GatewayFlow.SecretForward, profile.secretColliderSize, profile, true);
        Configure(visibility, serialized =>
        {
            SerializedProperty tilemaps = serialized.FindProperty("relatedTilemaps");
            tilemaps.arraySize = 3;
            Tilemap[] related = { path, pathWalls, closedWall };
            for (int index = 0; index < related.Length; index++)
            {
                SerializedProperty entry = tilemaps.GetArrayElementAtIndex(index);
                entry.FindPropertyRelative("tilemap").objectReferenceValue = related[index];
                entry.FindPropertyRelative("visibleWhenUnlocked").boolValue = index != 2;
            }
            SerializedProperty colliders = serialized.FindProperty("relatedColliders");
            colliders.arraySize = 1;
            colliders.GetArrayElementAtIndex(0).FindPropertyRelative("collider").objectReferenceValue = blocker;
            colliders.GetArrayElementAtIndex(0).FindPropertyRelative("enabledWhenUnlocked").boolValue = false;
        });
        CreateDestination(helpers, "SecretReturnDestination", surface, layout.SecretLanding, profile.secretDirection, GatewayFlow.SecretBackward);
        ground.CompressBounds();
        path.CompressBounds();
        walls.CompressBounds();
        pathWalls.CompressBounds();
    }

    private static void PlaceDecorations(GameObject root, Tilemap map, ProceduralRoomLayout layout, ProceduralRoomProfile profile, System.Random random)
    {
        if (profile.decorationPrefabs.Count == 0 || profile.decorationCount == 0)
            return;
        Transform parent = Child(root.transform, "Decorations").transform;
        List<Vector3Int> candidates = ProceduralRoomLayout.OrderedCells(layout.Walkable.Except(layout.Reserved));
        Shuffle(candidates, random);
        HashSet<Vector3Int> occupied = new HashSet<Vector3Int>();
        int placed = 0;
        foreach (Vector3Int cell in candidates)
        {
            if (placed >= profile.decorationCount)
                break;
            GameObject prefab = profile.decorationPrefabs[random.Next(profile.decorationPrefabs.Count)];
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.SetActive(true);
            instance.transform.position = map.GetCellCenterWorld(cell);
            Physics2D.SyncTransforms();
            Bounds bounds = new Bounds(instance.transform.position, Vector3.zero);
            foreach (Renderer propRenderer in instance.GetComponentsInChildren<Renderer>(true))
                bounds.Encapsulate(propRenderer.bounds);
            foreach (Collider2D propCollider in instance.GetComponentsInChildren<Collider2D>(true))
            {
                if (propCollider.enabled && !propCollider.isTrigger && propCollider.gameObject.activeInHierarchy)
                    bounds.Encapsulate(propCollider.bounds);
            }
            HashSet<Vector3Int> footprint = BoundsCells(map, bounds, profile.decorationClearance);
            ProceduralDecorationSettings settings = instance.GetComponent<ProceduralDecorationSettings>();
            bool cutsWalkable = settings == null || settings.cutsWalkableTiles;
            HashSet<Vector3Int> removed = cutsWalkable ? new HashSet<Vector3Int>(footprint.Intersect(layout.Walkable)) : new HashSet<Vector3Int>();
            bool fits = footprint.IsSubsetOf(layout.Ground) && !footprint.Overlaps(layout.Reserved) && !footprint.Overlaps(occupied);
            if (fits)
            {
                layout.Walkable.ExceptWith(removed);
                try { layout.Validate(profile.UsesFloorGateway); }
                catch (InvalidOperationException) { fits = false; }
            }
            if (!fits)
            {
                layout.Walkable.UnionWith(removed);
                Object.DestroyImmediate(instance);
                continue;
            }
            occupied.UnionWith(footprint);
            if (cutsWalkable)
                layout.Detectable.ExceptWith(footprint);
            placed++;
        }
        if (placed < profile.decorationCount)
            Debug.LogWarning($"{root.name}: placed {placed}/{profile.decorationCount} props; other footprints would obstruct reserved routes or exceed the room.", root);
    }

    private static void PlaceLights(GameObject root, Tilemap map, ProceduralRoomLayout layout, ProceduralRoomProfile profile, System.Random random)
    {
        if (profile.lightPrefabs.Count == 0 || profile.lightCount == 0)
            return;
        Transform parent = Child(root.transform, "ScatteredLights").transform;
        List<Vector3Int> candidates = ProceduralRoomLayout.OrderedCells(layout.Walkable.Except(layout.Reserved));
        Shuffle(candidates, random);
        List<Vector3Int> placed = new List<Vector3Int>();
        foreach (Vector3Int cell in candidates)
        {
            if (placed.Count >= profile.lightCount)
                break;
            if (placed.Any(previous => (previous - cell).sqrMagnitude < (float)profile.lightSpacing * profile.lightSpacing))
                continue;
            GameObject prefab = profile.lightPrefabs[random.Next(profile.lightPrefabs.Count)];
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.transform.position = map.GetCellCenterWorld(cell);
            instance.SetActive(true);
            foreach (Light2D light in instance.GetComponentsInChildren<Light2D>(true))
            {
                LightElevationVisibility visibility = light.GetComponent<LightElevationVisibility>();
                if (visibility == null)
                    visibility = light.gameObject.AddComponent<LightElevationVisibility>();
                Configure(visibility, serialized => serialized.FindProperty("lightLevel").intValue = 0);
            }
            placed.Add(cell);
        }
        if (placed.Count < profile.lightCount)
            Debug.LogWarning($"{root.name}: placed {placed.Count}/{profile.lightCount} scattered lights with safe spacing.", root);
    }

    private static HashSet<Vector3Int> BoundsCells(Tilemap map, Bounds bounds, int clearance)
    {
        Vector3Int[] corners =
        {
            map.WorldToCell(new Vector3(bounds.min.x, bounds.min.y, 0f)),
            map.WorldToCell(new Vector3(bounds.min.x, bounds.max.y, 0f)),
            map.WorldToCell(new Vector3(bounds.max.x, bounds.min.y, 0f)),
            map.WorldToCell(new Vector3(bounds.max.x, bounds.max.y, 0f))
        };
        HashSet<Vector3Int> cells = new HashSet<Vector3Int>();
        for (int cellX = corners.Min(cell => cell.x) - clearance; cellX <= corners.Max(cell => cell.x) + clearance; cellX++)
        {
            for (int cellY = corners.Min(cell => cell.y) - clearance; cellY <= corners.Max(cell => cell.y) + clearance; cellY++)
                cells.Add(new Vector3Int(cellX, cellY, 0));
        }
        return cells;
    }

    private static void PlaceEnemies(GameObject root, Tilemap map, ProceduralRoomLayout layout, ProceduralRoomProfile profile, System.Random random)
    {
        Transform parent = Child(root.transform, "EnemySpawnPoints").transform;
        List<Vector3Int> candidates = ProceduralRoomLayout.OrderedCells(layout.Walkable.Where(cell =>
            (cell - layout.PlayerSpawn).sqrMagnitude >= 36 && !layout.Reserved.Contains(cell)));
        Shuffle(candidates, random);
        List<Vector3Int> placed = new List<Vector3Int>();
        foreach (Vector3Int cell in candidates)
        {
            if (placed.Count >= profile.enemySpawnPoints)
                break;
            if (placed.Any(previous => (previous - cell).sqrMagnitude < 9))
                continue;
            GameObject instance = profile.enemySpawnPointPrefab != null
                ? (GameObject)PrefabUtility.InstantiatePrefab(profile.enemySpawnPointPrefab, parent)
                : Child(parent, $"EnemySpawnPoint_{placed.Count + 1}");
            EnemySpawnPoint marker = instance.GetComponentInChildren<EnemySpawnPoint>(true);
            if (marker == null)
                marker = instance.AddComponent<EnemySpawnPoint>();
            instance.transform.position = map.GetCellCenterWorld(cell);
            if (marker.transform != instance.transform)
                marker.transform.position = map.GetCellCenterWorld(cell);
            Configure(marker, serialized =>
            {
                serialized.FindProperty("spawnOnStart").boolValue = false;
                serialized.FindProperty("elevationLevel").intValue = 0;
                if (profile.enemySpawnPointPrefab == null)
                {
                    serialized.FindProperty("useRandomCount").boolValue = false;
                    serialized.FindProperty("fixedSpawnCount").intValue = 1;
                    serialized.FindProperty("spawnRadius").floatValue = 0f;
                    serialized.FindProperty("spawnParent").objectReferenceValue = root.transform;
                }
            });
            placed.Add(cell);
        }
        if (placed.Count < profile.enemySpawnPoints)
            Debug.LogWarning($"{root.name}: placed {placed.Count}/{profile.enemySpawnPoints} enemy markers with safe spacing.", root);
    }

    private static void Shuffle<T>(List<T> values, System.Random random)
    {
        for (int index = values.Count - 1; index > 0; index--)
        {
            int other = random.Next(index + 1);
            T temporary = values[index];
            values[index] = values[other];
            values[other] = temporary;
        }
    }

    private static void Configure(Component component, Action<SerializedObject> configure)
    {
        SerializedObject serialized = new SerializedObject(component);
        configure(serialized);
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}

[CustomEditor(typeof(ProceduralRoomProfile))]
public class ProceduralRoomProfileEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        bool secret = serializedObject.FindProperty("isSecretRoom").boolValue;
        bool cavern = serializedObject.FindProperty("shape").enumValueIndex == (int)ProceduralRoomShape.Cavern;
        SerializedProperty property = serializedObject.GetIterator();
        bool enterChildren = true;
        while (property.NextVisible(enterChildren))
        {
            enterChildren = false;
            if (property.name == "m_Script")
                continue;
            if ((cavern && property.name == "chamberCount") || (!cavern && property.name.StartsWith("cavern", StringComparison.Ordinal)))
                continue;
            if ((secret && (property.name == "forwardDirection" || property.name == "backwardDirection" || property.name == "forwardColliderSize" ||
                property.name == "includeFloorGateway" || property.name.StartsWith("trapdoor", StringComparison.Ordinal) ||
                property.name == "includeSecretGateway" || property.name == "secretDirection" || property.name == "secretColliderSize" ||
                property.name == "secretPathLength" || property.name == "secretPathHalfWidth")) ||
                (!secret && property.name == "secretRoomBackwardDirection"))
                continue;
            EditorGUILayout.PropertyField(property, secret && property.name == "backwardColliderSize"
                ? new GUIContent("Secret Room Backward Collider Size") : null, true);
        }
        serializedObject.ApplyModifiedProperties();
    }
}

public class ProceduralRoomWindow : EditorWindow
{
    [SerializeField] private ProceduralRoomProfile profile;
    [SerializeField] private Tilemap sourceGround;
    [SerializeField] private Tilemap sourceWalls;
    [SerializeField] private GameObject editableRoom;
    private Editor profileEditor;
    private Vector2 scroll;
    private ProceduralRoomLayout preview;
    private ProceduralRoomLayout renderedPreview;
    private Texture2D previewTexture;
    private bool renderedFloorGateway;
    private string status;
    private MessageType statusType;
    private bool showProfile = true;
    [SerializeField] private int batchCount = 4;

    [MenuItem("Tools/Procedural Rooms/Room Generator")]
    public static void Open()
    {
        GetWindow<ProceduralRoomWindow>("Room Generator").minSize = new Vector2(400f, 520f);
    }

    private void OnDisable()
    {
        if (profileEditor != null)
            DestroyImmediate(profileEditor);
        if (previewTexture != null)
            DestroyImmediate(previewTexture);
        renderedPreview = null;
    }

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUI.BeginChangeCheck();
        profile = (ProceduralRoomProfile)EditorGUILayout.ObjectField("Profile", profile, typeof(ProceduralRoomProfile), false);
        if (EditorGUI.EndChangeCheck())
            preview = null;
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("New Profile"))
                Perform(CreateProfile);
            using (new EditorGUI.DisabledScope(profile == null))
            {
                if (GUILayout.Button("Create Utility Tiles"))
                    Perform(() => ProceduralRoomBuilder.EnsureUtilityTiles(profile));
            }
        }
        EditorGUILayout.Space();
        sourceGround = (Tilemap)EditorGUILayout.ObjectField("Source Ground Map", sourceGround, typeof(Tilemap), true);
        sourceWalls = (Tilemap)EditorGUILayout.ObjectField("Source Wall Map", sourceWalls, typeof(Tilemap), true);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Use Selected Room"))
                Perform(UseSelectedRoom);
            using (new EditorGUI.DisabledScope(profile == null || sourceGround == null))
            {
                if (GUILayout.Button("Import Palettes"))
                    Perform(ImportPalettes);
            }
        }
        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(profile == null || EditorApplication.isPlayingOrWillChangePlaymode))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Preview Layout"))
                    Perform(() => preview = ProceduralRoomLayout.Generate(profile, profile.seed));
                if (GUILayout.Button("New Seed"))
                    Perform(() =>
                    {
                        Undo.RecordObject(profile, "Change Room Seed");
                        profile.seed = Guid.NewGuid().GetHashCode();
                        EditorUtility.SetDirty(profile);
                        preview = ProceduralRoomLayout.Generate(profile, profile.seed);
                    });
                if (GUILayout.Button("Create Editable Room"))
                    Perform(() =>
                    {
                        editableRoom = ProceduralRoomBuilder.Build(profile, profile.seed);
                        Selection.activeGameObject = editableRoom;
                        SceneView.lastActiveSceneView?.FrameSelected();
                        status = $"Created {editableRoom.name}. Existing rooms were not replaced.";
                    });
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                batchCount = EditorGUILayout.IntSlider("Seed Batch Size", batchCount, 1, 32);
                if (GUILayout.Button("Bake Seed Batch", GUILayout.Width(130f)))
                    Perform(BakeBatch);
            }
        }
        if (preview != null)
            DrawPreview();
        editableRoom = (GameObject)EditorGUILayout.ObjectField("Edited Room", editableRoom, typeof(GameObject), true);
        using (new EditorGUI.DisabledScope(editableRoom == null || EditorUtility.IsPersistent(editableRoom) || EditorApplication.isPlayingOrWillChangePlaymode))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Rebuild Boundary"))
                    Perform(() =>
                    {
                        Tilemap[] collisions = editableRoom.GetComponentsInChildren<Tilemap>().Where(map =>
                            map.name.StartsWith("ColliderLevel", StringComparison.Ordinal) && map.GetComponent<CompositeCollider2D>() != null).ToArray();
                        if (collisions.Length == 0)
                            throw new InvalidOperationException("Edited Room has no generated level collision maps.");
                        int undoGroup = Undo.GetCurrentGroup();
                        Undo.SetCurrentGroupName("Rebuild Room Boundary");
                        foreach (Tilemap collision in collisions)
                        {
                            PolygonCollider2D polygon = collision.GetComponent<PolygonCollider2D>();
                            if (polygon == null)
                                Undo.AddComponent<PolygonCollider2D>(collision.gameObject);
                            Undo.RecordObjects(new Object[] { collision.GetComponent<PolygonCollider2D>(), collision.GetComponent<CompositeCollider2D>() }, "Rebuild Room Boundary");
                            ProceduralRoomBuilder.RebuildBoundaryCollision(collision);
                        }
                        Undo.CollapseUndoOperations(undoGroup);
                        status = $"Rebuilt exact surface-aligned boundary polygons on {collisions.Length} elevation maps.";
                    });
                if (GUILayout.Button("Validate Edited Room"))
                    Perform(() => ProceduralRoomBuilder.ValidateRoom(editableRoom));
                if (GUILayout.Button("Bake New Prefab"))
                    Perform(Bake);
            }
        }
        if (!string.IsNullOrEmpty(status))
            EditorGUILayout.HelpBox(status, statusType);
        if (profile != null)
        {
            showProfile = EditorGUILayout.Foldout(showProfile, "Generation Profile", true);
            if (showProfile)
            {
                Editor.CreateCachedEditor(profile, null, ref profileEditor);
                EditorGUI.BeginChangeCheck();
                profileEditor.OnInspectorGUI();
                if (EditorGUI.EndChangeCheck())
                    preview = null;
            }
        }
        EditorGUILayout.EndScrollView();
    }

    private void Perform(Action action)
    {
        try
        {
            status = "Complete.";
            statusType = MessageType.Info;
            action();
        }
        catch (Exception exception)
        {
            status = exception.Message;
            statusType = MessageType.Error;
            Debug.LogException(exception);
        }
    }

    private void CreateProfile()
    {
        string path = EditorUtility.SaveFilePanelInProject("New Room Profile", "RoomGenerationProfile", "asset", "Choose the profile asset location.");
        if (string.IsNullOrEmpty(path))
            return;
        profile = CreateInstance<ProceduralRoomProfile>();
        AssetDatabase.CreateAsset(profile, AssetDatabase.GenerateUniqueAssetPath(path));
        ProceduralRoomBuilder.EnsureUtilityTiles(profile);
        preview = null;
        EditorGUIUtility.PingObject(profile);
    }

    private void UseSelectedRoom()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
            throw new InvalidOperationException("Select a room prefab or a room object in the Hierarchy.");
        RoomInstance room = selected.GetComponentInParent<RoomInstance>();
        GameObject root = room != null ? room.gameObject : selected;
        sourceGround = root.GetComponentsInChildren<Tilemap>(true)
            .Where(map => map.name.IndexOf("Ground", StringComparison.OrdinalIgnoreCase) >= 0 && map.GetComponent<AStarWalkableMap>() == null)
            .OrderByDescending(map => ProceduralRoomBuilder.GetCells(map).Count).FirstOrDefault();
        if (sourceGround == null)
            throw new InvalidOperationException("No Ground-named tilemap found. Assign Source Ground Map directly.");
        sourceWalls = null;
        status = "Ground source selected. Assign a visible wall map separately when available.";
    }

    private void ImportPalettes()
    {
        Undo.RecordObject(profile, "Import Room Palettes");
        profile.groundTiles = ReadPalette(sourceGround);
        TilemapRenderer groundRenderer = sourceGround.GetComponent<TilemapRenderer>();
        if (groundRenderer != null)
        {
            profile.tilemapMaterial = groundRenderer.sharedMaterial;
            profile.groundSortingLayer = groundRenderer.sortingLayerName;
            profile.groundSortingOrder = groundRenderer.sortingOrder;
        }
        if (sourceWalls != null)
        {
            profile.wallTiles = ReadPalette(sourceWalls);
        }
        EditorUtility.SetDirty(profile);
        preview = null;
        status = $"Imported {profile.groundTiles.Count} ground tile variants. Review the pools and edge rules before building.";
    }

    private static List<ProceduralWeightedTile> ReadPalette(Tilemap map)
    {
        Dictionary<TileBase, int> counts = new Dictionary<TileBase, int>();
        foreach (Vector3Int cell in ProceduralRoomLayout.OrderedCells(ProceduralRoomBuilder.GetCells(map)))
        {
            TileBase tile = map.GetTile(cell);
            if (!EditorUtility.IsPersistent(tile))
                throw new InvalidOperationException("Palette import requires saved tile assets.");
            counts.TryGetValue(tile, out int count);
            counts[tile] = count + 1;
        }
        if (counts.Count == 0)
            throw new InvalidOperationException("The source tilemap is empty.");
        return counts.Select(entry => new ProceduralWeightedTile { tile = entry.Key, weight = Math.Min(10000, entry.Value) }).ToList();
    }

    private void Bake()
    {
        ProceduralRoomBuilder.ValidateRoom(editableRoom);
        string path = EditorUtility.SaveFilePanelInProject("Bake New Room Prefab", editableRoom.name, "prefab", "Choose a new prefab asset location.");
        if (string.IsNullOrEmpty(path))
            return;
        GameObject prefab = ProceduralRoomBuilder.SaveNewPrefab(editableRoom, path);
        EditorGUIUtility.PingObject(prefab);
        status = $"Saved {AssetDatabase.GetAssetPath(prefab)}. Add this prefab to the appropriate RoomManager room pool.";
    }

    private void BakeBatch()
    {
        string folder = EditorUtility.OpenFolderPanel("Room Prefab Output Folder", Application.dataPath, "");
        if (string.IsNullOrEmpty(folder))
            return;
        folder = FileUtil.GetProjectRelativePath(folder).Replace('\\', '/');
        if (!AssetDatabase.IsValidFolder(folder) || (folder != "Assets" && !folder.StartsWith("Assets/", StringComparison.Ordinal)))
            throw new InvalidOperationException("Choose an existing output folder under Assets.");
        UnityEngine.SceneManagement.Scene scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
        int saved = 0;
        try
        {
            for (int index = 0; index < batchCount; index++)
            {
                int seed = unchecked(profile.seed + index);
                EditorUtility.DisplayProgressBar("Bake Room Prefabs", $"Seed {seed}", index / (float)batchCount);
                GameObject room = ProceduralRoomBuilder.Build(profile, seed, false, scene);
                try
                {
                    ProceduralRoomBuilder.SaveNewPrefab(room, $"{folder}/{room.name}.prefab");
                    saved++;
                }
                finally { DestroyImmediate(room); }
            }
            status = $"Saved {saved} new prefabs in {folder}. Existing prefabs were not overwritten.";
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException($"Batch stopped after {saved} saved prefabs: {exception.Message}", exception);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    private void DrawPreview()
    {
        Rect rect = GUILayoutUtility.GetRect(100f, 260f, GUILayout.ExpandWidth(true));
        if (Event.current.type == EventType.Repaint)
        {
            if (previewTexture == null || renderedPreview != preview || renderedFloorGateway != profile.UsesFloorGateway)
            {
                if (previewTexture != null)
                    DestroyImmediate(previewTexture);
                previewTexture = ProceduralRoomPreview.CreateTexture(preview, profile.UsesFloorGateway);
                renderedPreview = preview;
                renderedFloorGateway = profile.UsesFloorGateway;
            }
            EditorGUI.DrawRect(rect, ProceduralRoomPreview.Background);
            GUI.DrawTexture(rect, previewTexture, ScaleMode.ScaleToFit, false);
        }
        EditorGUILayout.LabelField($"Ground: {preview.Ground.Count}   Walkable: {preview.Walkable.Count}   Boundary: {preview.Walls.Count}", EditorStyles.miniLabel);
    }
}

internal static class ProceduralRoomPreview
{
    public static readonly Color32 Background = new Color32(31, 31, 31, 255);
    public static readonly Color32 Ground = new Color32(56, 148, 107, 255);
    public static readonly Color32 Fringe = new Color32(77, 102, 115, 255);
    public static readonly Color32 Wall = new Color32(128, 133, 140, 255);
    public static readonly Color32 Boundary = new Color32(55, 62, 66, 255);
    public static readonly Color32 Spawn = new Color32(255, 217, 77, 255);
    public static readonly Color32 Gateway = new Color32(77, 191, 255, 255);
    public static readonly Color32 SecretPath = new Color32(196, 121, 66, 255);

    public static Texture2D CreateTexture(ProceduralRoomLayout layout, bool includeFloorGateway)
    {
        if (layout == null || layout.Ground.Count == 0)
            throw new ArgumentException("A nonempty layout is required for the room preview.");

        const int width = 768;
        const int height = 384;
        Color32[] pixels = Enumerable.Repeat(Background, width * height).ToArray();
        List<Vector3Int> cells = ProceduralRoomLayout.OrderedCells(layout.Ground.Concat(layout.Walls));
        int minimumX = cells.Min(cell => cell.x - cell.y);
        int maximumX = cells.Max(cell => cell.x - cell.y);
        int minimumY = cells.Min(cell => cell.x + cell.y);
        int maximumY = cells.Max(cell => cell.x + cell.y);
        float scale = Math.Min((width - 8f) / (maximumX - minimumX + 2f),
            (height - 8f) * 2f / (maximumY - minimumY + 2f));
        float centerX = (minimumX + maximumX) * 0.5f;
        float centerY = (minimumY + maximumY) * 0.5f;
        foreach (Vector3Int cell in cells)
        {
            Color32 color = layout.VisualWalls.Contains(cell) ? Wall : layout.Walls.Contains(cell) ? Boundary : layout.Walkable.Contains(cell) ? Ground : Fringe;
            if (layout.SecretPath.Contains(cell))
                color = SecretPath;
            if (cell == layout.PlayerSpawn)
                color = Spawn;
            if (cell == layout.BackwardGateway || cell == layout.ForwardGateway ||
                (layout.SecretPath.Count > 0 && cell == layout.SecretGateway) || (includeFloorGateway && layout.FloorCells.Contains(cell)))
                color = Gateway;
            float positionX = width * 0.5f + (cell.x - cell.y - centerX) * scale;
            float positionY = height * 0.5f + (cell.x + cell.y - centerY) * scale * 0.5f;
            int bottom = Math.Max(0, Mathf.CeilToInt(positionY - scale * 0.5f - 0.5f));
            int top = Math.Min(height - 1, Mathf.FloorToInt(positionY + scale * 0.5f - 0.5f));
            for (int pixelY = bottom; pixelY <= top; pixelY++)
            {
                float halfWidth = scale - Math.Abs(pixelY + 0.5f - positionY) * 2f;
                int left = Math.Max(0, Mathf.CeilToInt(positionX - halfWidth - 0.5f));
                int right = Math.Min(width - 1, Mathf.FloorToInt(positionX + halfWidth - 0.5f));
                for (int pixelX = left; pixelX <= right; pixelX++)
                    pixels[pixelY * width + pixelX] = color;
            }
        }
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            name = "Procedural Room Preview",
            hideFlags = HideFlags.HideAndDontSave,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        texture.SetPixels32(pixels);
        texture.Apply(false, false);
        return texture;
    }
}