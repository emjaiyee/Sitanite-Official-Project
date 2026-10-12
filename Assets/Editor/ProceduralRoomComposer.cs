using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

public static class ProceduralRoomComposer
{
    public static GameObject Combine(GameObject baseRoom, GameObject sectionRoom, AStarStairLink stair,
        Transform sectionLanding, RampElevationTrigger lowerTrigger, RampElevationTrigger upperTrigger,
        bool attachAtUpperEnd, bool registerUndo = true, int sectionElevation = -1, int sectionBaseZ = 0, int gatewaySeed = 12345)
    {
        GuardEditMode();
        if (baseRoom == null || sectionRoom == null || stair == null || sectionLanding == null || lowerTrigger == null || upperTrigger == null)
            throw new InvalidOperationException("Assign both rooms, the stair link, the section landing, and both elevation triggers.");
        if (baseRoom == sectionRoom || baseRoom.transform.IsChildOf(sectionRoom.transform) || sectionRoom.transform.IsChildOf(baseRoom.transform))
            throw new InvalidOperationException("Use two separate room roots, not ancestors of one another.");
        RequireInside(baseRoom, stair.transform, "Stair link");
        RequireInside(baseRoom, lowerTrigger.transform, "Lower trigger");
        RequireInside(baseRoom, upperTrigger.transform, "Upper trigger");
        RequireInside(sectionRoom, sectionLanding, "Section landing");
        if (lowerTrigger == upperTrigger)
            throw new InvalidOperationException("The two landings require separate elevation triggers.");
        RoomInstance owner = baseRoom.GetComponent<RoomInstance>();
        if (owner == null || baseRoom.GetComponentsInChildren<RoomInstance>(true).Length != 1)
            throw new InvalidOperationException("The base room root must be its only RoomInstance.");
        AStarWalkableMap baseMap = SingleSurface(baseRoom);
        AStarWalkableMap sectionMap = SingleSurface(sectionRoom);
        if (baseRoom.GetComponentsInChildren<AStarStairLink>(true).Length != 1 || sectionRoom.GetComponentsInChildren<AStarStairLink>(true).Length != 0)
            throw new InvalidOperationException("This tool joins two flat sections through one authored stair. Multi-stair routing is not supported.");
        if (sectionRoom.GetComponentsInChildren<GatewaySecretVisibility>(true).Length != 0)
            throw new InvalidOperationException("Use an attached section without a locked secret entrance; its key-room progression belongs to a separate room.");
        ValidateGrids(baseRoom);
        ValidateGrids(sectionRoom);
        int baseLevel = baseMap.ElevationLevel;
        int sectionLevel = sectionElevation == -1 ? baseLevel + (attachAtUpperEnd ? 1 : -1) : sectionElevation;
        if (baseLevel < 0 || sectionLevel < 0)
            throw new InvalidOperationException("Descending from elevation zero is not supported. Set the base map to elevation one or higher first.");
        if ((attachAtUpperEnd && sectionLevel <= baseLevel) || (!attachAtUpperEnd && sectionLevel >= baseLevel))
            throw new InvalidOperationException("The attached tilemap level must be above the base for ascent, or below it for descent.");
        foreach (string property in new[] { "entryAnchor", "exitAnchor" })
        {
            Transform anchor = new SerializedObject(stair).FindProperty(property).objectReferenceValue as Transform;
            if (anchor != null)
                RequireInside(baseRoom, anchor, "Stair anchor");
        }

        GameObject result = new GameObject(baseRoom.name + "_Combined");
        if (!EditorUtility.IsPersistent(baseRoom))
            SceneManager.MoveGameObjectToScene(result, baseRoom.scene);
        result.transform.position = baseRoom.transform.position;
        try
        {
            EditorUtility.CopySerialized(owner, result.AddComponent<RoomInstance>());
            GameObject baseCopy = Clone(baseRoom, result.transform, "BaseGeometry");
            GameObject sectionCopy = Clone(sectionRoom, result.transform, "ElevationSection_" + sectionLevel);
            ValidateGrids(baseCopy);
            ValidateGrids(sectionCopy);
            foreach (RoomInstance duplicate in baseCopy.GetComponentsInChildren<RoomInstance>(true))
                Object.DestroyImmediate(duplicate);
            foreach (Gateway gateway in baseCopy.GetComponentsInChildren<Gateway>(true))
                gateway.SetDestination(null);
            foreach (RoomInstance duplicate in sectionCopy.GetComponentsInChildren<RoomInstance>(true))
                Object.DestroyImmediate(duplicate);
            AStarStairLink copiedStair = Resolve(baseRoom.transform, baseCopy.transform, stair.transform).GetComponent<AStarStairLink>();
            RampElevationTrigger copiedLower = Resolve(baseRoom.transform, baseCopy.transform, lowerTrigger.transform).GetComponent<RampElevationTrigger>();
            RampElevationTrigger copiedUpper = Resolve(baseRoom.transform, baseCopy.transform, upperTrigger.transform).GetComponent<RampElevationTrigger>();
            Transform copiedLanding = Resolve(sectionRoom.transform, sectionCopy.transform, sectionLanding);
            ResolveStairAnchors(copiedStair);
            RequireWalkable(SingleSurface(baseCopy), Anchor(copiedStair, attachAtUpperEnd ? "entryAnchor" : "exitAnchor").position,
                "Base stair landing");
            RequireWalkable(SingleSurface(sectionCopy), copiedLanding.position, "Section landing anchor");
            Vector3 target = Anchor(copiedStair, attachAtUpperEnd ? "exitAnchor" : "entryAnchor").position;
            ShiftSectionCells(sectionCopy, sectionBaseZ);
            sectionCopy.transform.position += target - copiedLanding.position;
            GameObject landing = new GameObject("SectionLandingAnchor");
            landing.transform.SetParent(sectionCopy.transform, false);
            landing.transform.position = target;
            ShiftSectionLevels(sectionCopy, sectionLevel - sectionMap.ElevationLevel);
            RandomizeGateways(baseCopy, sectionCopy, baseLevel, sectionLevel, gatewaySeed);
            ProceduralRoomBuilder.AddWallFading(baseCopy);
            ProceduralRoomBuilder.AddWallFading(sectionCopy);
            RestoreBoundaries(baseCopy);
            RestoreBoundaries(sectionCopy);
            SetInt(copiedLower, "targetLevel", Mathf.Min(baseLevel, sectionLevel));
            SetInt(copiedUpper, "targetLevel", Mathf.Max(baseLevel, sectionLevel));
            Validate(result);
            if (registerUndo)
                Undo.RegisterCreatedObjectUndo(result, "Combine Rooms Into Elevation Section");
            return result;
        }
        catch
        {
            Object.DestroyImmediate(result);
            throw;
        }
    }

    public static void Validate(GameObject room)
    {
        GuardEditMode();
        if (room != null && EditorUtility.IsPersistent(room))
            throw new InvalidOperationException("Instantiate the combined prefab into a scene before validating its physical traversal.");
        if (room == null || room.GetComponent<RoomInstance>() == null || room.GetComponentsInChildren<RoomInstance>(true).Length != 1)
            throw new InvalidOperationException("A combined root must own exactly one RoomInstance.");
        if (room.GetComponent<Grid>() != null)
            throw new InvalidOperationException("The combined root stays empty of Grid components; keep compatible Grids under its geometry children.");
        ValidateGrids(room);
        AStarWalkableMap[] maps = room.GetComponentsInChildren<AStarWalkableMap>(true);
        AStarStairLink[] links = room.GetComponentsInChildren<AStarStairLink>(true);
        if (maps.Length != 2 || links.Length != 1 || maps[0].ElevationLevel == maps[1].ElevationLevel || maps.Any(map => map.ElevationLevel < 0))
            throw new InvalidOperationException("The composition must have two distinct nonnegative navigation levels and one stair link.");
        foreach (AStarWalkableMap map in maps)
            RequireConnected(map.GetComponent<Tilemap>());
        AStarStairLink link = links[0];
        Transform entry = Anchor(link, "entryAnchor");
        Transform exit = Anchor(link, "exitAnchor");
        RequireInside(room, entry, "Lower stair anchor");
        RequireInside(room, exit, "Upper stair anchor");
        AStarWalkableMap lower = maps.OrderBy(map => map.ElevationLevel).First();
        AStarWalkableMap upper = maps.OrderBy(map => map.ElevationLevel).Last();
        RequireWalkable(lower, entry.position, "Lower stair anchor");
        RequireWalkable(upper, exit.position, "Upper stair anchor");
        Collider2D ramp = link.GetComponent<Collider2D>();
        if (ramp == null || !ramp.enabled || !ramp.isTrigger || !ramp.gameObject.activeInHierarchy || link.GetComponent<RampMovementTrigger>() == null)
            throw new InvalidOperationException("The stair link needs an active trigger and RampMovementTrigger on the same object.");
        if (Vector2.Distance(entry.position, exit.position) < 0.01f)
            throw new InvalidOperationException("Stair anchors cannot occupy the same point.");
        Physics2D.SyncTransforms();
        RampElevationTrigger[] triggers = room.GetComponentsInChildren<RampElevationTrigger>(true);
        RequireElevationTrigger(triggers, entry.position, lower.ElevationLevel, "Lower");
        RequireElevationTrigger(triggers, exit.position, upper.ElevationLevel, "Upper");
        Collider2D[] solids = room.GetComponentsInChildren<Collider2D>(true).Where(collider => collider.enabled &&
            collider.gameObject.activeInHierarchy && !collider.isTrigger && collider.compositeOperation == Collider2D.CompositeOperation.None).ToArray();
        WarnLandingClearance(lower, entry.position, solids);
        WarnLandingClearance(upper, exit.position, solids);
        Vector2 segment = exit.position - entry.position;
        List<RaycastHit2D> hits = new List<RaycastHit2D>();
        room.scene.GetPhysicsScene2D().Raycast(entry.position, segment.normalized, segment.magnitude,
            new ContactFilter2D { useTriggers = false }, hits);
        HashSet<Collider2D> centerlineColliders = new HashSet<Collider2D>(hits.Where(hit => hit.collider != null &&
            hit.collider.transform.IsChildOf(room.transform)).Select(hit => hit.collider));
        Tilemap[] detectable = room.GetComponentsInChildren<AStarDetectionMap>(true).Select(map => map.GetComponent<Tilemap>()).ToArray();
        for (int sample = 1; sample < 10; sample++)
        {
            Vector3 point = Vector3.Lerp(entry.position, exit.position, sample / 10f);
            foreach (Collider2D collider in solids.Where(candidate => candidate.OverlapPoint(point)))
                centerlineColliders.Add(collider);
            if (sample >= 3 && sample <= 7 && (!ramp.OverlapPoint(point) || !detectable.Any(map => ProceduralTilemapPlane.ContainsDetection(map, point))))
                throw new InvalidOperationException("The stair's central traversal needs ramp-trigger and AStarDetectionMap coverage. Paint detectable stair tiles manually.");
        }
        foreach (Collider2D collider in centerlineColliders)
            Debug.LogWarning($"Stair centerline intersects '{collider.name}'. Composition is allowed; verify physical traversal in Play Mode.", collider);
        foreach (RoomSpawnPoint spawn in room.GetComponentsInChildren<RoomSpawnPoint>(true))
            RequireMarker(maps, spawn.transform.position, spawn.ElevationLevel, "Player spawn");
        if (room.GetComponentsInChildren<RoomSpawnPoint>(true).Length != 1)
            throw new InvalidOperationException("The combined room needs exactly one player spawn, from its base room.");
        foreach (EnemySpawnPoint spawn in room.GetComponentsInChildren<EnemySpawnPoint>(true))
            RequireMarker(maps, spawn.transform.position, new SerializedObject(spawn).FindProperty("elevationLevel").intValue, "Enemy spawn");
        foreach (ElevationDestination destination in room.GetComponentsInChildren<ElevationDestination>(true))
            RequireMarker(maps, destination.transform.position, new SerializedObject(destination).FindProperty("elevationLevel").intValue, "Gateway destination");
    }

    public static GameObject SaveNewPrefab(GameObject room, string path)
    {
        if (EditorUtility.IsPersistent(room))
            throw new InvalidOperationException("Bake the new scene composition, not an existing asset.");
        Validate(room);
        if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/", StringComparison.Ordinal) || !path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Choose a prefab path under Assets.");
        path = AssetDatabase.GenerateUniqueAssetPath(path);
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(room, path, out bool success);
        if (!success || prefab == null)
            throw new InvalidOperationException("Could not save the combined prefab.");
        return prefab;
    }

    private static GameObject Clone(GameObject source, Transform parent, string name)
    {
        GameObject copy = Object.Instantiate(source, parent);
        copy.name = name;
        if (PrefabUtility.IsPartOfPrefabInstance(copy))
            PrefabUtility.UnpackPrefabInstance(copy, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        copy.transform.localPosition = Vector3.zero;
        copy.transform.localRotation = Quaternion.identity;
        copy.transform.localScale = Vector3.one;
        copy.SetActive(true);
        foreach (Tilemap sourceMap in source.GetComponentsInChildren<Tilemap>(true))
        {
            Tilemap copiedMap = Resolve(source.transform, copy.transform, sourceMap.transform).GetComponent<Tilemap>();
            foreach (Vector3Int cell in ProceduralRoomBuilder.GetCells(sourceMap))
            {
                copiedMap.SetTileFlags(cell, sourceMap.GetTileFlags(cell));
                copiedMap.SetTile(new TileChangeData(cell, sourceMap.GetTile(cell), sourceMap.GetColor(cell), sourceMap.GetTransformMatrix(cell)), true);
                copiedMap.SetTileFlags(cell, sourceMap.GetTileFlags(cell));
            }
        }
        foreach (Tilemap map in copy.GetComponentsInChildren<Tilemap>(true))
        {
            if (map.transform.parent == null || map.transform.parent.GetComponentInParent<Grid>() == null)
            {
                Grid grid = copy.GetComponent<Grid>();
                if (grid == null)
                    grid = copy.AddComponent<Grid>();
                grid.cellLayout = GridLayout.CellLayout.IsometricZAsY;
                grid.cellSwizzle = GridLayout.CellSwizzle.XYZ;
                grid.cellSize = ProceduralRoomBuilder.CellSize;
                grid.cellGap = Vector3.zero;
                break;
            }
        }
        return copy;
    }

    private sealed class GatewayPair
    {
        public Gateway gateway;
        public ElevationDestination arrival;
    }

    private static void RandomizeGateways(GameObject baseCopy, GameObject sectionCopy, int baseLevel, int sectionLevel, int seed)
    {
        List<GatewayPair> pairs = new List<GatewayPair>();
        foreach (GameObject section in new[] { baseCopy, sectionCopy })
        {
            int level = section == baseCopy ? baseLevel : sectionLevel;
            foreach (ElevationDestination destination in section.GetComponentsInChildren<ElevationDestination>(true))
                SetInt(destination, "elevationLevel", level);
            foreach (Gateway gateway in section.GetComponentsInChildren<Gateway>(true))
            {
                SetInt(gateway, "elevationLevel", level);
                gateway.SetDestination(null);
                if (gateway.Flow != GatewayFlow.Forward && gateway.Flow != GatewayFlow.Backward)
                    continue;
                GatewayFlow arrivalFlow = gateway.Flow == GatewayFlow.Forward ? GatewayFlow.Backward : GatewayFlow.Forward;
                ElevationDestination arrival = section.GetComponentsInChildren<ElevationDestination>(true).FirstOrDefault(candidate =>
                    candidate.Flow == arrivalFlow && candidate.Direction == gateway.Direction);
                if (arrival == null)
                    throw new InvalidOperationException($"{gateway.name} needs a {arrivalFlow} destination on its {gateway.Direction} side before composing.");
                pairs.Add(new GatewayPair { gateway = gateway, arrival = arrival });
            }
        }
        System.Random random = new System.Random(seed);
        List<GatewayPair> forward = pairs.Where(pair => pair.gateway.Flow == GatewayFlow.Forward).ToList();
        List<GatewayPair> backward = pairs.Where(pair => pair.gateway.Flow == GatewayFlow.Backward).ToList();
        HashSet<Gateway> keptGateways = new HashSet<Gateway>();
        HashSet<ElevationDestination> keptDestinations = new HashSet<ElevationDestination>();
        if (forward.Count > 0 && backward.Count > 0)
        {
            var combinations = (from exit in forward from entrance in backward
                where exit.gateway.Direction != entrance.gateway.Direction
                select new { exit, entrance }).ToList();
            if (combinations.Count == 0)
                throw new InvalidOperationException("Regular forward/backward gateway sides must be distinct in the combined room.");
            var chosen = combinations[random.Next(combinations.Count)];
            keptGateways.Add(chosen.exit.gateway);
            keptGateways.Add(chosen.entrance.gateway);
            keptDestinations.Add(chosen.exit.arrival);
            keptDestinations.Add(chosen.entrance.arrival);
        }
        else if (pairs.Count > 0)
        {
            GatewayPair chosen = pairs[random.Next(pairs.Count)];
            keptGateways.Add(chosen.gateway);
            keptDestinations.Add(chosen.arrival);
        }
        Gateway[] floors = baseCopy.GetComponentsInChildren<Gateway>(true).Concat(sectionCopy.GetComponentsInChildren<Gateway>(true))
            .Where(gateway => gateway.Flow == GatewayFlow.Floor).ToArray();
        if (floors.Length > 0)
            keptGateways.Add(floors[random.Next(floors.Length)]);
        StripProgression(baseCopy, keptGateways, keptDestinations, true);
        StripProgression(sectionCopy, keptGateways, keptDestinations, false);
    }

    private static void StripProgression(GameObject section, HashSet<Gateway> keptGateways,
        HashSet<ElevationDestination> keptDestinations, bool preserveBase)
    {
        foreach (Gateway gateway in section.GetComponentsInChildren<Gateway>(true))
        {
            if (keptGateways.Contains(gateway) || (preserveBase && gateway.Flow != GatewayFlow.Forward &&
                gateway.Flow != GatewayFlow.Backward && gateway.Flow != GatewayFlow.Floor))
                continue;
            GatewayFloorVisibility floor = gateway.GetComponent<GatewayFloorVisibility>();
            if (floor != null)
            {
                Tilemap tiles = new SerializedObject(floor).FindProperty("tilemap").objectReferenceValue as Tilemap;
                if (tiles != null && tiles.transform.IsChildOf(section.transform))
                    Object.DestroyImmediate(tiles.gameObject);
            }
            Object.DestroyImmediate(gateway.gameObject);
        }
        foreach (ElevationDestination destination in section.GetComponentsInChildren<ElevationDestination>(true))
        {
            if (!keptDestinations.Contains(destination) && (!preserveBase ||
                destination.Flow == GatewayFlow.Forward || destination.Flow == GatewayFlow.Backward))
                Object.DestroyImmediate(destination.gameObject);
        }
        if (!preserveBase)
        {
            foreach (RoomSpawnPoint spawn in section.GetComponentsInChildren<RoomSpawnPoint>(true))
                Object.DestroyImmediate(spawn.gameObject);
        }
    }

    private static void RestoreBoundaries(GameObject section)
    {
        foreach (Tilemap map in section.GetComponentsInChildren<Tilemap>(true).Where(candidate =>
            candidate.name.StartsWith("ColliderLevel", StringComparison.Ordinal) && candidate.GetComponent<CompositeCollider2D>() != null))
        {
            TilemapElevationVisibility visibility = map.GetComponent<TilemapElevationVisibility>();
            if (visibility != null)
            {
                SerializedObject serialized = new SerializedObject(visibility);
                serialized.FindProperty("affectColliders").boolValue = false;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            CompositeCollider2D composite = map.GetComponent<CompositeCollider2D>();
            composite.enabled = true;
            ProceduralRoomBuilder.RebuildBoundaryCollision(map);
        }
    }

    internal static void ShiftSectionCells(GameObject section, int baseZ)
    {
        Tilemap surface = SingleSurface(section).GetComponent<Tilemap>();
        ProceduralTilemapPlane previousPlane = surface.GetComponent<ProceduralTilemapPlane>();
        int sourceZ = previousPlane != null ? previousPlane.baseZ : 0;
        int delta = baseZ - sourceZ;
        Vector3 displacement = surface.GetCellCenterWorld(new Vector3Int(0, 0, baseZ)) -
            surface.GetCellCenterWorld(new Vector3Int(0, 0, sourceZ));
        foreach (Transform branch in section.GetComponentsInChildren<Transform>(true))
        {
            if (branch == section.transform || branch.GetComponentInChildren<Tilemap>(true) != null ||
                branch.parent.GetComponentInChildren<Tilemap>(true) == null)
                continue;
            branch.position += displacement;
        }
        foreach (Tilemap map in section.GetComponentsInChildren<Tilemap>(true))
        {
            Vector3Int[] cells = ProceduralRoomBuilder.GetCells(map).ToArray();
            TileBase[] tiles = cells.Select(map.GetTile).ToArray();
            Color[] colors = cells.Select(map.GetColor).ToArray();
            Matrix4x4[] transforms = cells.Select(map.GetTransformMatrix).ToArray();
            TileFlags[] flags = cells.Select(map.GetTileFlags).ToArray();
            Tile.ColliderType[] colliderTypes = cells.Select(map.GetColliderType).ToArray();
            map.ClearAllTiles();
            for (int index = 0; index < cells.Length; index++)
            {
                Vector3Int destination = cells[index] + Vector3Int.forward * delta;
                map.SetTile(new TileChangeData(destination, tiles[index], colors[index], transforms[index]), true);
                if (map.GetColliderType(destination) != colliderTypes[index])
                    map.SetColliderType(destination, colliderTypes[index]);
                map.SetTileFlags(destination, flags[index]);
            }
            ProceduralTilemapPlane plane = map.GetComponent<ProceduralTilemapPlane>();
            if (plane == null)
                plane = map.gameObject.AddComponent<ProceduralTilemapPlane>();
            plane.baseZ = baseZ;
            map.CompressBounds();
            if (map.name.StartsWith("ColliderLevel", StringComparison.Ordinal) && map.GetComponent<CompositeCollider2D>() != null)
                ProceduralRoomBuilder.RebuildBoundaryCollision(map);
        }
    }

    internal static void ShiftSectionLevels(GameObject section, int delta)
    {
        Shift<AStarWalkableMap>(section, "elevationLevel", delta);
        Shift<TilemapElevationVisibility>(section, "tilemapLevel", delta);
        Shift<TilemapElevationSorting>(section, "tilemapLevel", delta);
        Shift<LightElevationVisibility>(section, "lightLevel", delta);
        Shift<WallOcclusionFade>(section, "tilemapLevel", delta);
        Shift<ElevationGroup>(section, "elevationLevel", delta);
        Shift<EnemySpawnPoint>(section, "elevationLevel", delta);
        Shift<EnemyElevationLevel>(section, "currentLevel", delta);
        Shift<RampElevationTrigger>(section, "targetLevel", delta);
    }

    private static void Shift<T>(GameObject root, string property, int delta) where T : Component
    {
        foreach (T component in root.GetComponentsInChildren<T>(true))
        {
            int value = new SerializedObject(component).FindProperty(property).intValue + delta;
            if (value < 0)
                throw new InvalidOperationException($"{component.name}: shifting {property} would make its elevation negative.");
            SetInt(component, property, value);
        }
    }

    private static void SetInt(Component component, string property, int value)
    {
        SerializedObject serialized = new SerializedObject(component);
        serialized.FindProperty(property).intValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ResolveStairAnchors(AStarStairLink stair)
    {
        Physics2D.SyncTransforms();
        SerializedObject serialized = new SerializedObject(stair);
        foreach (string property in new[] { "entryAnchor", "exitAnchor" })
        {
            if (serialized.FindProperty(property).objectReferenceValue != null)
                continue;
            RampMovementTrigger movement = stair.GetComponent<RampMovementTrigger>();
            if (movement == null || movement.RampCollider == null || !movement.RampCollider.enabled || !movement.RampCollider.isTrigger)
                throw new InvalidOperationException("Automatic stair endpoints need an enabled RampMovementTrigger trigger collider on the stair-link object.");
            GameObject anchor = new GameObject(property == "entryAnchor" ? "AutomaticLowerLanding" : "AutomaticUpperLanding");
            anchor.transform.SetParent(stair.transform, false);
            anchor.transform.position = property == "entryAnchor" ? stair.EntryPosition : stair.ExitPosition;
            serialized.FindProperty(property).objectReferenceValue = anchor.transform;
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Transform Anchor(AStarStairLink stair, string property)
    {
        Transform anchor = new SerializedObject(stair).FindProperty(property).objectReferenceValue as Transform;
        if (anchor == null)
            throw new InvalidOperationException("Assign explicit entry (lower) and exit (upper) anchors on the authored AStarStairLink.");
        return anchor;
    }

    private static Transform Resolve(Transform originalRoot, Transform copyRoot, Transform original)
    {
        List<int> indices = new List<int>();
        while (original != originalRoot)
        {
            indices.Add(original.GetSiblingIndex());
            original = original.parent;
        }
        for (int index = indices.Count - 1; index >= 0; index--)
            copyRoot = copyRoot.GetChild(indices[index]);
        return copyRoot;
    }

    private static AStarWalkableMap SingleSurface(GameObject room)
    {
        AStarWalkableMap[] maps = room.GetComponentsInChildren<AStarWalkableMap>(true);
        if (maps.Length != 1 || !maps[0].enabled || !maps[0].gameObject.activeSelf)
            throw new InvalidOperationException($"{room.name} must have exactly one active flat AStarWalkableMap.");
        RequireConnected(maps[0].GetComponent<Tilemap>());
        return maps[0];
    }

    private static void ValidateGrids(GameObject room)
    {
        if (Vector3.Distance(room.transform.lossyScale, Vector3.one) > 0.0001f || Quaternion.Angle(room.transform.rotation, Quaternion.identity) > 0.001f)
            throw new InvalidOperationException("Room roots must have unit world scale and no rotation. Alignment is translation only.");
        if (EditorUtility.IsPersistent(room))
            return;
        foreach (Tilemap map in room.GetComponentsInChildren<Tilemap>(true))
        {
            GridLayout grid = map.layoutGrid;
            if (grid == null && map.transform.parent != null)
                grid = map.transform.parent.GetComponentInParent<Grid>();
            if (grid == null || grid.cellLayout != GridLayout.CellLayout.IsometricZAsY || grid.cellSwizzle != GridLayout.CellSwizzle.XYZ ||
                Vector3.Distance(grid.cellSize, ProceduralRoomBuilder.CellSize) > 0.0001f || grid.cellGap != Vector3.zero)
                throw new InvalidOperationException($"{map.name} needs a compatible Isometric Z As Y / XYZ Grid with cell size (0.32, 0.16, 2).");
        }
    }

    private static void RequireInside(GameObject root, Transform child, string label)
    {
        if (child == null || (child != root.transform && !child.IsChildOf(root.transform)))
            throw new InvalidOperationException($"{label} must belong to {root.name}.");
    }

    private static void RequireWalkable(AStarWalkableMap map, Vector3 point, string label)
    {
        Tilemap tiles = map.GetComponent<Tilemap>();
        if (!map.enabled || !map.gameObject.activeInHierarchy || !tiles.HasTile(ProceduralTilemapPlane.WorldToCell(tiles, point)))
            throw new InvalidOperationException($"{label} is outside active walkable tiles at elevation {map.ElevationLevel}.");
    }

    private static void RequireMarker(AStarWalkableMap[] maps, Vector3 point, int level, string label)
    {
        AStarWalkableMap map = maps.SingleOrDefault(candidate => candidate.ElevationLevel == level);
        if (map == null)
            throw new InvalidOperationException($"{label} uses a level with no navigation map.");
        RequireWalkable(map, point, label);
    }

    private static void RequireConnected(Tilemap map)
    {
        HashSet<Vector3Int> cells = ProceduralRoomBuilder.GetCells(map);
        ProceduralTilemapPlane plane = map.GetComponent<ProceduralTilemapPlane>();
        int cellZ = plane != null ? plane.baseZ : 0;
        if (cells.Count == 0 || cells.Any(cell => cell.z != cellZ))
            throw new InvalidOperationException("Each section's navigation must occupy one cell-Z plane matching its ProceduralTilemapPlane base Z.");
        HashSet<Vector3Int> reached = new HashSet<Vector3Int> { cells.First() };
        Queue<Vector3Int> pending = new Queue<Vector3Int>(reached);
        while (pending.Count > 0)
        {
            Vector3Int cell = pending.Dequeue();
            foreach (Vector3Int offset in new[] { Vector3Int.right, Vector3Int.up, Vector3Int.left, Vector3Int.down })
            {
                if (cells.Contains(cell + offset) && reached.Add(cell + offset))
                    pending.Enqueue(cell + offset);
            }
        }
        if (reached.Count != cells.Count)
            throw new InvalidOperationException($"{map.name} contains disconnected navigation.");
    }

    private static void RequireElevationTrigger(RampElevationTrigger[] triggers, Vector3 point, int level, string label)
    {
        if (!triggers.Any(trigger => trigger.enabled && trigger.gameObject.activeInHierarchy &&
            new SerializedObject(trigger).FindProperty("targetLevel").intValue == level &&
            trigger.GetComponent<Collider2D>() != null && trigger.GetComponent<Collider2D>().enabled &&
            trigger.GetComponent<Collider2D>().isTrigger && trigger.GetComponent<Collider2D>().OverlapPoint(point)))
            throw new InvalidOperationException($"{label} stair landing needs an active elevation trigger targeting level {level}.");
    }

    private static void WarnLandingClearance(AStarWalkableMap map, Vector3 point, Collider2D[] solids)
    {
        Tilemap tiles = map.GetComponent<Tilemap>();
        Vector3 origin = tiles.GetCellCenterWorld(Vector3Int.zero);
        Vector3 axisX = (tiles.GetCellCenterWorld(Vector3Int.right) - origin) * 0.45f;
        Vector3 axisY = (tiles.GetCellCenterWorld(Vector3Int.up) - origin) * 0.45f;
        foreach (Vector3 probe in new[] { point, point + axisX, point - axisX, point + axisY, point - axisY })
        {
            Collider2D nearby = solids.FirstOrDefault(collider => collider.OverlapPoint(probe));
            if (nearby != null)
            {
                Debug.LogWarning($"Stair landing at elevation {map.ElevationLevel} touches or is close to '{nearby.name}'. Composition is allowed; verify character clearance in Play Mode.", nearby);
                return;
            }
        }
    }

    private static void GuardEditMode()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before composing rooms.");
    }
}

public class ProceduralRoomComposerWindow : EditorWindow
{
    [SerializeField] private GameObject baseRoom;
    [SerializeField] private GameObject sectionRoom;
    [SerializeField] private AStarStairLink stair;
    [SerializeField] private Transform sectionLanding;
    [SerializeField] private RampElevationTrigger lowerTrigger;
    [SerializeField] private RampElevationTrigger upperTrigger;
    [SerializeField] private bool attachAtUpperEnd = true;
    [SerializeField] private int sectionElevation = 1;
    [SerializeField] private int sectionBaseZ;
    [SerializeField] private int gatewaySeed = 12345;
    [SerializeField] private GameObject combinedRoom;
    private Vector2 scroll;
    private string status;
    private bool error;

    [MenuItem("Tools/Procedural Rooms/Room Composer")]
    public static void Open()
    {
        GetWindow<ProceduralRoomComposerWindow>("Room Composer").minSize = new Vector2(380f, 450f);
    }

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.LabelField("Room Sources", EditorStyles.boldLabel);
        baseRoom = (GameObject)EditorGUILayout.ObjectField("Base Room", baseRoom, typeof(GameObject), true);
        sectionRoom = (GameObject)EditorGUILayout.ObjectField("Attached Room", sectionRoom, typeof(GameObject), true);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Selected As Base"))
                baseRoom = Selection.activeGameObject;
            if (GUILayout.Button("Selected As Attached"))
                sectionRoom = Selection.activeGameObject;
        }
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Authored Stair", EditorStyles.boldLabel);
        stair = (AStarStairLink)EditorGUILayout.ObjectField("Stair Link (In Base)", stair, typeof(AStarStairLink), true);
        lowerTrigger = (RampElevationTrigger)EditorGUILayout.ObjectField("Lower Elevation Trigger", lowerTrigger, typeof(RampElevationTrigger), true);
        upperTrigger = (RampElevationTrigger)EditorGUILayout.ObjectField("Upper Elevation Trigger", upperTrigger, typeof(RampElevationTrigger), true);
        sectionLanding = (Transform)EditorGUILayout.ObjectField("Attached Landing Anchor", sectionLanding, typeof(Transform), true);
        attachAtUpperEnd = GUILayout.Toolbar(attachAtUpperEnd ? 0 : 1, new[] { "Upper End (Ascend)", "Lower End (Descend)" }) == 0;
        sectionElevation = EditorGUILayout.IntField("Attached Tilemap Level", sectionElevation);
        sectionBaseZ = EditorGUILayout.IntField("Attached Base Z", sectionBaseZ);
        using (new EditorGUILayout.HorizontalScope())
        {
            gatewaySeed = EditorGUILayout.IntField("Gateway Seed", gatewaySeed);
            if (GUILayout.Button("New Seed", GUILayout.Width(85f)))
                gatewaySeed = Guid.NewGuid().GetHashCode();
        }
        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
        {
            if (GUILayout.Button("Create Combined Room"))
                Run(() =>
                {
                    combinedRoom = ProceduralRoomComposer.Combine(baseRoom, sectionRoom, stair, sectionLanding, lowerTrigger, upperTrigger,
                        attachAtUpperEnd, sectionElevation: sectionElevation, sectionBaseZ: sectionBaseZ, gatewaySeed: gatewaySeed);
                    Selection.activeGameObject = combinedRoom;
                    status = "Created " + combinedRoom.name + ". Original rooms are unchanged.";
                });
            combinedRoom = (GameObject)EditorGUILayout.ObjectField("Combined Room", combinedRoom, typeof(GameObject), true);
            using (new EditorGUI.DisabledScope(combinedRoom == null))
            {
                if (GUILayout.Button("Validate Combined Room"))
                    Run(() => { ProceduralRoomComposer.Validate(combinedRoom); status = "Combined room validation passed."; });
                if (GUILayout.Button("Bake New Combined Prefab"))
                    Run(() =>
                    {
                        string path = EditorUtility.SaveFilePanelInProject("Bake Combined Room", combinedRoom.name, "prefab", "Choose a new combined prefab path.");
                        if (string.IsNullOrEmpty(path))
                            return;
                        GameObject prefab = ProceduralRoomComposer.SaveNewPrefab(combinedRoom, path);
                        EditorGUIUtility.PingObject(prefab);
                        status = "Saved " + AssetDatabase.GetAssetPath(prefab);
                    });
            }
        }
        if (!string.IsNullOrEmpty(status))
            EditorGUILayout.HelpBox(status, error ? MessageType.Error : MessageType.Info);
        EditorGUILayout.EndScrollView();
    }

    private void Run(Action action)
    {
        try { error = false; action(); }
        catch (Exception exception) { error = true; status = exception.Message; Debug.LogException(exception); }
    }
}