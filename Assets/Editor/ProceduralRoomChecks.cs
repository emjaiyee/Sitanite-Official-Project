using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class ProceduralRoomChecks
{
    private static readonly string RequestPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/ProceduralRoomChecks.request"));
    private static readonly string ResultPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/ProceduralRoomChecks.result"));

    static ProceduralRoomChecks()
    {
        if (File.Exists(RequestPath))
            EditorApplication.delayCall += RunRequested;
    }

    private static void RunRequested()
    {
        if (!File.Exists(RequestPath))
            return;
        string request = File.ReadAllText(RequestPath).Trim();
        File.Delete(RequestPath);
        if (request == "preview")
            RunPreviewChecks();
        else if (request == "composition")
            RunCompositionChecks();
        else if (request == "generation")
            RunGenerationChecks();
        else
            Run();
    }

    [MenuItem("Tools/Procedural Rooms/Run Preview Checks")]
    public static void RunPreviewChecks()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            File.WriteAllText(ResultPath, "BLOCKED: Exit Play Mode before running preview checks.");
            return;
        }
        ProceduralRoomProfile profile = ScriptableObject.CreateInstance<ProceduralRoomProfile>();
        ProceduralRoomWindow window = ScriptableObject.CreateInstance<ProceduralRoomWindow>();
        Object[] previousSelection = Selection.objects;
        try
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Scripts/RoomNFloorScripts/RoomPrefabs/RoomSmall1.prefab");
            Require(source != null, "RoomSmall1 prefab was not found for the selection check.");
            Selection.activeGameObject = source;
            typeof(ProceduralRoomWindow).GetMethod("UseSelectedRoom", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(window, null);
            Tilemap selectedMap = (Tilemap)typeof(ProceduralRoomWindow).GetField("sourceGround", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(window);
            Require(selectedMap != null && selectedMap.name.IndexOf("Ground", StringComparison.OrdinalIgnoreCase) >= 0,
                "Use Selected Room did not find the authored ground map.");

            int count = 0;
            foreach (ProceduralRoomShape shape in Enum.GetValues(typeof(ProceduralRoomShape)))
            {
                profile.shape = shape;
                foreach (int size in new[] { 16, 36, 128 })
                {
                    profile.width = size;
                    profile.height = size;
                    ProceduralRoomLayout layout = ProceduralRoomLayout.Generate(profile, 12345);
                    Texture2D first = ProceduralRoomPreview.CreateTexture(layout, true);
                    Texture2D second = ProceduralRoomPreview.CreateTexture(layout, true);
                    try
                    {
                        Color32[] pixels = first.GetPixels32();
                        Require(first.width == 768 && first.height == 384 && first.mipmapCount == 1, "Preview texture dimensions or mipmaps changed.");
                        Require(pixels.SequenceEqual(second.GetPixels32()), "Preview pixels are not deterministic.");
                        Require(pixels.Count(pixel => pixel.Equals(ProceduralRoomPreview.Ground)) > 100, "Preview ground is blank.");
                        Require(pixels.Any(pixel => pixel.Equals(ProceduralRoomPreview.Wall)) &&
                            pixels.Any(pixel => pixel.Equals(ProceduralRoomPreview.Spawn)) &&
                            pixels.Any(pixel => pixel.Equals(ProceduralRoomPreview.Gateway)), "Preview lost walls, spawn, or gateways.");
                        Require((first.hideFlags & HideFlags.DontSave) != 0, "Preview texture would be saved into the scene.");
                        count++;
                    }
                    finally
                    {
                        Object.DestroyImmediate(first);
                        Object.DestroyImmediate(second);
                    }
                }
            }
            string result = $"PASS: Authored RoomSmall1 selection; {count} deterministic nonblank bitmap previews including maximum size; walls/spawn/gateway pixels; bounded texture dimensions; no saved preview assets. Interactive repaint validation remains a manual check.";
            File.WriteAllText(ResultPath, result);
            Debug.Log(result);
        }
        catch (Exception exception)
        {
            File.WriteAllText(ResultPath, "FAIL: " + exception);
            Debug.LogException(exception);
        }
        finally
        {
            Selection.objects = previousSelection;
            Object.DestroyImmediate(window);
            Object.DestroyImmediate(profile);
        }
    }

    [MenuItem("Tools/Procedural Rooms/Run Checks")]
    public static void Run()
    {
        RunChecks(false);
    }

    [MenuItem("Tools/Procedural Rooms/Run Composer Checks")]
    public static void RunCompositionChecks()
    {
        RunChecks(true);
    }

    [MenuItem("Tools/Procedural Rooms/Run Generator Checks")]
    public static void RunGenerationChecks()
    {
        RunChecks(false, true);
    }

    private static void RunChecks(bool compositionOnly, bool generationOnly = false)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            File.WriteAllText(ResultPath, "BLOCKED: Exit Play Mode before running room checks.");
            return;
        }
        UnityEngine.Random.State previousRandom = UnityEngine.Random.state;
        string folder = "Assets/__ProceduralRoomChecks_" + Guid.NewGuid().ToString("N");
        var scene = EditorSceneManager.NewPreviewScene();
        ProceduralRoomProfile profile = null;
        try
        {
            AssetDatabase.CreateFolder("Assets", Path.GetFileName(folder));
            profile = ScriptableObject.CreateInstance<ProceduralRoomProfile>();
            AssetDatabase.CreateAsset(profile, folder + "/Profile.asset");
            ProceduralRoomBuilder.EnsureUtilityTiles(profile);
            Tile ground = ScriptableObject.CreateInstance<Tile>();
            ground.name = "Check Ground";
            ground.colliderType = Tile.ColliderType.None;
            AssetDatabase.AddObjectToAsset(ground, profile);
            profile.groundTiles.Add(new ProceduralWeightedTile { tile = ground });
            profile.wallTiles.Add(new ProceduralWeightedTile { tile = ground });
            Tile[] trapdoorTiles = new Tile[4];
            for (int index = 0; index < trapdoorTiles.Length; index++)
            {
                trapdoorTiles[index] = ScriptableObject.CreateInstance<Tile>();
                trapdoorTiles[index].name = "Trapdoor Part " + index;
                AssetDatabase.AddObjectToAsset(trapdoorTiles[index], profile);
            }
            profile.trapdoorNorthEast = trapdoorTiles[0];
            profile.trapdoorNorthWest = trapdoorTiles[1];
            profile.trapdoorSouthEast = trapdoorTiles[2];
            profile.trapdoorSouthWest = trapdoorTiles[3];
            profile.enemySpawnPoints = 4;
            AssetDatabase.SaveAssetIfDirty(profile);

            if (generationOnly)
            {
                CheckGeneratorUpgrades(profile, scene, folder);
                CheckPopulation(profile, scene, folder);
                CheckGeneratedPlanes(profile, scene, folder);
                Require(JsonUtility.ToJson(previousRandom) == JsonUtility.ToJson(UnityEngine.Random.state), "Generator plane checks altered Unity's random state.");
                string generationResult = "PASS: Four new shapes across seeds/dimensions/gateway directions; ring holes and connected passages; direction-specific wall-mounted gateway prefabs; inland enemy markers including template spawn radius; supplied NotWalkableTile boundaries; automatic paint/erase collider updates; independent elevation/base Z 0/5; regular and secret rooms; wall layers; shifted markers/props/lights; matching elevation metadata; flat-Z actor projection; prefab round-trips; invalid input rejection; unchanged Unity random state.";
                File.WriteAllText(ResultPath, generationResult);
                Debug.Log(generationResult);
                return;
            }

            if (compositionOnly)
            {
                CheckComposition(profile, scene, folder);
                CheckWallFading(profile, scene);
                CheckPopulation(profile, scene, folder);
                string compositionResult = "PASS: Composer 0-to-5 levels and base cell Z 5; retained active boundary geometry through visibility changes; advisory landing/centerline collision warnings; seeded gateway pairs from both sections with matching destination elevations; optional collider-derived stair endpoints; source preservation; tile state/alignment; A* both ways; prefab persistence; Undo; invalid-input rejection; per-column fading; decoration cut opt-outs and seeded non-blocking scattered lights.";
                File.WriteAllText(ResultPath, compositionResult);
                Debug.Log(compositionResult);
                return;
            }

            int layoutCount = 0;
            foreach (ProceduralRoomShape shape in Enum.GetValues(typeof(ProceduralRoomShape)))
            {
                profile.shape = shape;
                foreach (int dimension in new[] { 16, 36, 64, 128 })
                {
                    profile.width = dimension;
                    profile.height = dimension;
                    for (int seed = 0; seed < 12; seed++)
                    {
                        ProceduralRoomLayout first = ProceduralRoomLayout.Generate(profile, seed);
                        ProceduralRoomLayout second = ProceduralRoomLayout.Generate(profile, seed);
                        Require(first.Ground.SetEquals(second.Ground) && first.Walkable.SetEquals(second.Walkable), "A seed produced different layouts.");
                        Require(first.FloorCells.SetEquals(second.FloorCells) && first.FloorCells.Count == 4 &&
                            first.FloorCells.IsSubsetOf(first.Reserved), "Trapdoor footprint is not deterministic, 2x2, and reserved.");
                        first.Validate(true);
                        Require(!first.Walkable.Overlaps(first.Detectable), "Detectable-only tiles overlap navigation.");
                        Require(first.Reserved.Contains(first.PlayerSpawn) && first.Reserved.Contains(first.ForwardGateway), "Gateway routes were not reserved.");
                        Require(first.Ground.All(cell => cell.z == 0), "Single-elevation generation wrote nonzero cell Z.");
                        Require(first.Walkable.SetEquals(first.Ground), "Navigation excluded ground edge cells.");
                        Require(first.Detectable.Count == 0, "Ground edges were incorrectly classified as detectable-only stairs.");
                        Require(first.VisualWalls.All(cell => !first.Ground.Contains(cell + Vector3Int.right) &&
                            !first.Ground.Contains(cell + Vector3Int.up)), "A south-side visual wall would cover ground.");
                        layoutCount++;
                    }
                }
            }
            profile.width = 36;
            profile.height = 28;
            foreach (ProceduralRoomShape shape in Enum.GetValues(typeof(ProceduralRoomShape)))
            {
                profile.shape = shape;
                GameObject room = ProceduralRoomBuilder.Build(profile, -12345, false, scene);
                try
                {
                    ProceduralRoomBuilder.ValidateRoom(room);
                    Gateway[] gateways = room.GetComponentsInChildren<Gateway>();
                    Gateway forward = gateways.First(gateway => gateway.Flow == GatewayFlow.Forward);
                    Gateway backward = gateways.First(gateway => gateway.Flow == GatewayFlow.Backward);
                    Require(Vector3.Distance(forward.transform.position, backward.transform.position) > 1f, "Cell-to-world conversion collapsed distinct gateway cells.");
                    Tilemap collision = room.transform.Find("ColliderLevel0").GetComponent<Tilemap>();
                    CompositeCollider2D composite = collision.GetComponent<CompositeCollider2D>();
                    Require(composite.pathCount > 0, "Invisible Grid tiles did not produce boundary geometry.");
                    Vector3Int wall = ProceduralRoomBuilder.GetCells(collision).First();
                    Require(composite.OverlapPoint(collision.GetCellCenterWorld(wall)), "A boundary tile has no physical collision.");
                    Require(collision.GetComponent<Rigidbody2D>().bodyType == RigidbodyType2D.Static, "Boundary body is not static.");
                    AStarWalkableMap walkable = room.GetComponentInChildren<AStarWalkableMap>();
                    Tilemap map = walkable.GetComponent<Tilemap>();
                    Tilemap groundMap = room.transform.Find("GroundLevel0").GetComponent<Tilemap>();
                    Require(map.transform.localPosition == Vector3.up * (0.16f * profile.surfaceHeightInTiles) &&
                        collision.transform.localPosition == map.transform.localPosition, "Navigation and collision do not align with the cube surface.");
                    Require(ProceduralRoomBuilder.GetCells(map).SetEquals(ProceduralRoomBuilder.GetCells(groundMap)), "Navigation is missing surface edges.");
                    CheckSurfaceBoundary(map, collision);
                    CheckTrapdoor(room, profile, -12345);
                    Require(!map.GetComponent<TilemapRenderer>().enabled && map.color.a == 0f, "Navigation tiles are visible.");
                    foreach (EnemySpawnPoint marker in room.GetComponentsInChildren<EnemySpawnPoint>())
                    {
                        SerializedObject serialized = new SerializedObject(marker);
                        Require(!serialized.FindProperty("spawnOnStart").boolValue && serialized.FindProperty("elevationLevel").intValue == 0, "Enemy marker bypasses the scene spawner or has the wrong elevation.");
                    }
                    GameObject firstPrefab = ProceduralRoomBuilder.SaveNewPrefab(room, folder + "/Room.prefab");
                    string firstPath = AssetDatabase.GetAssetPath(firstPrefab);
                    byte[] before = File.ReadAllBytes(firstPath);
                    GameObject secondPrefab = ProceduralRoomBuilder.SaveNewPrefab(room, folder + "/Room.prefab");
                    Require(firstPath != AssetDatabase.GetAssetPath(secondPrefab) && before.SequenceEqual(File.ReadAllBytes(firstPath)), "Bake overwrote an existing prefab.");
                    Require(firstPrefab.GetComponentInChildren<RoomSpawnPoint>() != null && firstPrefab.GetComponentsInChildren<Gateway>().Length == 3, "Prefab did not retain room markers.");
                    Require(firstPrefab.GetComponentInChildren<AStarWalkableMap>().GetComponent<Tilemap>().GetUsedTilesCount() > 0, "Prefab lost navigation tiles.");
                    Require(new SerializedObject(firstPrefab.GetComponentInChildren<GatewayFloorVisibility>()).FindProperty("tilemap").objectReferenceValue != null,
                        "Baking lost the trapdoor tilemap reference.");
                    foreach (Vector3Int cell in ProceduralRoomBuilder.GetCells(map).Where(cell => cell.x == 0).ToArray())
                        map.SetTile(cell, null);
                    RequireThrows(() => ProceduralRoomBuilder.ValidateRoom(room), "Disconnected edited navigation passed validation.");
                }
                finally { Object.DestroyImmediate(room); }
            }
            int directionalCount = 0;
            profile.shape = ProceduralRoomShape.Rectangle;
            profile.enemySpawnPoints = 0;
            foreach (GatewayDirection forward in Enum.GetValues(typeof(GatewayDirection)))
            {
                foreach (GatewayDirection backward in Enum.GetValues(typeof(GatewayDirection)))
                {
                    if (forward == backward)
                        continue;
                    profile.forwardDirection = forward;
                    profile.backwardDirection = backward;
                    profile.forwardColliderSize = new Vector2(4f, 2f);
                    profile.backwardColliderSize = new Vector2(1.5f, 0.75f);
                    GameObject room = ProceduralRoomBuilder.Build(profile, 54321, false, scene);
                    try
                    {
                        foreach (Gateway gateway in room.GetComponentsInChildren<Gateway>())
                        {
                            if (gateway.Flow == GatewayFlow.Floor)
                                continue;
                            Vector2 expectedSize = gateway.Flow == GatewayFlow.Forward ? profile.forwardColliderSize :
                                profile.backwardColliderSize;
                            PolygonCollider2D trigger = gateway.GetComponent<PolygonCollider2D>();
                            Tilemap map = room.GetComponentInChildren<AStarWalkableMap>().GetComponent<Tilemap>();
                            Vector3Int cell = map.WorldToCell(gateway.transform.position);
                            Vector3Int outward = ProceduralRoomLayout.DirectionOffset(gateway.Direction);
                            Require(!map.HasTile(cell + outward), "Gateway direction does not match its boundary.");
                            Vector3Int tangent = outward.x != 0 ? Vector3Int.up : Vector3Int.right;
                            Vector2 edge = (Vector2)(map.GetCellCenterWorld(cell + tangent) - map.GetCellCenterWorld(cell)) * expectedSize.x;
                            Require(Vector2.Distance(trigger.points[0], trigger.points[3]) > 0f &&
                                Mathf.Abs(Vector2.Distance(trigger.points[0], trigger.points[3]) - edge.magnitude) < 0.0001f,
                                "Gateway polygon width does not follow the flow-specific cell size.");
                            Vector2 depth = (Vector2)(map.GetCellCenterWorld(cell + outward) - map.GetCellCenterWorld(cell)) * expectedSize.y;
                            Require(Mathf.Abs(Vector2.Distance(trigger.points[0], trigger.points[1]) - depth.magnitude) < 0.0001f,
                                "Gateway polygon depth does not follow the flow-specific cell size.");
                            bool south = gateway.Direction == GatewayDirection.SouthEast || gateway.Direction == GatewayDirection.SouthWest;
                            Light2D light = gateway.GetComponentInChildren<Light2D>();
                            Require(south == (light != null), "South/north gateway representation does not match its direction.");
                            if (south)
                            {
                                GatewayVisibility visibility = gateway.GetComponent<GatewayVisibility>();
                                SerializedObject serialized = new SerializedObject(visibility);
                                serialized.FindProperty("smoothFade").boolValue = false;
                                serialized.ApplyModifiedPropertiesWithoutUndo();
                                typeof(GatewayVisibility).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(visibility, null);
                                float intensity = light.intensity;
                                visibility.Lock();
                                Require(light.intensity == 0f && !trigger.enabled, "Locked south gateway retains its light or trigger.");
                                visibility.Unlock();
                                Require(Mathf.Approximately(light.intensity, intensity) && trigger.enabled, "Unlocked south gateway does not restore its light and trigger.");
                            }
                        }
                        ElevationDestination[] destinations = room.GetComponentsInChildren<ElevationDestination>();
                        Require(destinations.Length == 2, "Room should retain its two corresponding destination markers.");
                        Tilemap surface = room.GetComponentInChildren<AStarWalkableMap>().GetComponent<Tilemap>();
                        ProceduralRoomLayout logical = ProceduralRoomLayout.Generate(profile, 54321);
                        Require(destinations.Any(destination => destination.Flow == GatewayFlow.Forward && destination.Direction == backward &&
                            surface.WorldToCell(destination.transform.position) == logical.BackwardLanding), "Forward arrival does not follow the backward entrance.");
                        Require(destinations.Any(destination => destination.Flow == GatewayFlow.Backward && destination.Direction == forward &&
                            surface.WorldToCell(destination.transform.position) == logical.ForwardLanding), "Return destination does not follow the forward exit.");
                        ProceduralRoomBuilder.ValidateRoom(room);
                        directionalCount++;
                    }
                    finally { Object.DestroyImmediate(room); }
                }
            }
            profile.forwardDirection = GatewayDirection.NorthEast;
            profile.backwardDirection = GatewayDirection.SouthWest;
            CheckSecretRooms(profile, scene, folder);
            profile.shape = ProceduralRoomShape.Cavern;
            CheckSecretRooms(profile, scene, folder);
            CheckCavernSettings(profile);
            CheckWalls(profile, scene, folder);
            CheckWallFading(profile, scene);
            CheckComposition(profile, scene, folder);
            CheckPopulation(profile, scene, folder);
            CheckGeneratedPlanes(profile, scene, folder);
            CheckGeneratorUpgrades(profile, scene, folder);
            profile.backwardDirection = profile.forwardDirection;
            RequireThrows(() => ProceduralRoomLayout.Generate(profile, 1), "Overlapping gateway sides passed validation.");
            profile.backwardDirection = GatewayDirection.SouthWest;
            profile.corridorHalfWidth = 1;
            RequireThrows(() => ProceduralRoomLayout.Generate(profile, 1), "Invalid clearance configuration passed generation.");
            Require(JsonUtility.ToJson(previousRandom) == JsonUtility.ToJson(UnityEngine.Random.state), "Room generation altered Unity's global random state.");
            string result = $"PASS: {layoutCount} seeded flat layouts; {directionalCount} regular gateway configurations; trapdoors; secret/key-room progression; cavern settings; wall height/fading; composition levels 0-to-5/base Z 5; retained collider geometry/visibility; advisory collision warnings; seeded cross-section gateway pairs/elevation destinations; automatic stair endpoints; tile state/alignment; A* both ways; source/prefab/Undo preservation; decoration cut opt-outs; seeded scattered lights without navigation cuts; generator independent elevation/base Z and prefab round-trips; bitmap previews; unchanged Unity random state.";
            File.WriteAllText(ResultPath, result);
            Debug.Log(result);
        }
        catch (Exception exception)
        {
            File.WriteAllText(ResultPath, "FAIL: " + exception);
            Debug.LogException(exception);
        }
        finally
        {
            UnityEngine.Random.state = previousRandom;
            EditorSceneManager.ClosePreviewScene(scene);
            if (AssetDatabase.IsValidFolder(folder))
                AssetDatabase.DeleteAsset(folder);
        }
    }

    private static void InitializeVisibility(GatewayVisibility visibility)
    {
        SerializedObject serialized = new SerializedObject(visibility);
        serialized.FindProperty("smoothFade").boolValue = false;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        visibility.GetType().GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(visibility, null);
    }

    private static void CheckTrapdoor(GameObject room, ProceduralRoomProfile profile, int seed)
    {
        Tilemap tiles = room.transform.Find("FloorTrapdoorTiles").GetComponent<Tilemap>();
        Tilemap surface = room.GetComponentInChildren<AStarWalkableMap>().GetComponent<Tilemap>();
        ProceduralRoomLayout logical = ProceduralRoomLayout.Generate(profile, seed);
        Vector3Int origin = logical.FloorGateway;
        Require(ProceduralRoomBuilder.GetCells(tiles).SetEquals(logical.FloorCells), "Trapdoor tiles do not match the reserved interior patch.");
        Require(tiles.GetTile(origin) == profile.trapdoorSouthWest && tiles.GetTile(origin + Vector3Int.right) == profile.trapdoorSouthEast &&
            tiles.GetTile(origin + Vector3Int.up) == profile.trapdoorNorthWest &&
            tiles.GetTile(origin + Vector3Int.right + Vector3Int.up) == profile.trapdoorNorthEast, "Trapdoor tile quadrants are reversed.");
        GatewayFloorVisibility visibility = room.GetComponentInChildren<GatewayFloorVisibility>();
        Gateway gateway = visibility.GetComponent<Gateway>();
        PolygonCollider2D trigger = gateway.GetComponent<PolygonCollider2D>();
        Require(gateway.Flow == GatewayFlow.Floor && gateway.GetComponentInChildren<Light2D>() == null &&
            gateway.GetComponentInChildren<SpriteRenderer>() == null, "Trapdoor incorrectly uses directional gateway artwork.");
        InitializeVisibility(visibility);
        Require(!trigger.enabled && tiles.color.a == 0f, "Floor trapdoor did not start hidden and unusable.");
        visibility.UnlockFloorProgression();
        Physics2D.SyncTransforms();
        Require(trigger.enabled && tiles.color.a == 1f, "Floor progression did not reveal and enable the trapdoor.");
        Vector2 axisX = surface.GetCellCenterWorld(origin + Vector3Int.right) - surface.GetCellCenterWorld(origin);
        Vector2 axisY = surface.GetCellCenterWorld(origin + Vector3Int.up) - surface.GetCellCenterWorld(origin);
        Require(Mathf.Abs(Vector2.Distance(trigger.points[0], trigger.points[1]) - axisX.magnitude * 2f) < 0.0001f &&
            Mathf.Abs(Vector2.Distance(trigger.points[1], trigger.points[2]) - axisY.magnitude * 2f) < 0.0001f,
            "Trapdoor trigger is not exactly 2x2 grid cells.");
        foreach (Vector3Int cell in logical.FloorCells)
        {
            Vector2 center = surface.GetCellCenterWorld(cell);
            Require(trigger.OverlapPoint(center), "Trapdoor trigger misses a tile center.");
            foreach (Vector3Int offset in new[] { Vector3Int.right, Vector3Int.up, Vector3Int.left, Vector3Int.down })
            {
                Require(surface.HasTile(cell + offset), "Trapdoor touches the room's outer edge.");
                if (!logical.FloorCells.Contains(cell + offset))
                {
                    Vector2 outside = surface.GetCellCenterWorld(cell + offset);
                    Require(trigger.OverlapPoint(Vector2.Lerp(center, outside, 0.45f)) &&
                        !trigger.OverlapPoint(Vector2.Lerp(center, outside, 0.55f)), "Trapdoor collider does not cover exactly its tile footprint.");
                }
            }
        }
        visibility.SetInvisible();
    }

    private static void CheckSecretRooms(ProceduralRoomProfile profile, UnityEngine.SceneManagement.Scene scene, string folder)
    {
        UnityEngine.Random.State state = UnityEngine.Random.state;
        try
        {
            int caseNumber = 0;
            foreach (GatewayDirection direction in Enum.GetValues(typeof(GatewayDirection)))
            {
                foreach (GatewayFlow entranceFlow in new[] { GatewayFlow.SecretForward })
                {
                    profile.isSecretRoom = false;
                    profile.includeSecretGateway = true;
                    profile.secretDirection = direction;
                    GatewayDirection[] others = ((GatewayDirection[])Enum.GetValues(typeof(GatewayDirection))).Where(side => side != direction).ToArray();
                    profile.forwardDirection = others[0];
                    profile.backwardDirection = others[1];
                    GameObject entranceRoom = ProceduralRoomBuilder.Build(profile, 1234, false, scene);
                    GameObject secretRoom = null;
                    GameObject managerObject = new GameObject("Special Gateway Check");
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(managerObject, scene);
                    try
                    {
                        Tilemap surface = entranceRoom.GetComponentInChildren<AStarWalkableMap>().GetComponent<Tilemap>();
                        CheckSurfaceBoundary(surface, entranceRoom.transform.Find("ColliderLevel0").GetComponent<Tilemap>());
                        foreach (GatewayVisibility visibility in entranceRoom.GetComponentsInChildren<GatewayVisibility>())
                            InitializeVisibility(visibility);
                        GatewaySecretVisibility entranceVisibility = entranceRoom.GetComponentInChildren<GatewaySecretVisibility>();
                        Gateway entrance = entranceVisibility.GetComponent<Gateway>();
                        Collider2D blocker = entranceRoom.transform.Find("RoomMarkers/SecretEntranceBlocker").GetComponent<Collider2D>();
                        Tilemap path = entranceRoom.transform.Find("SecretPathGround").GetComponent<Tilemap>();
                        Tilemap closedWall = entranceRoom.transform.Find("SecretEntranceClosedWall").GetComponent<Tilemap>();
                        ProceduralRoomLayout logical = ProceduralRoomLayout.Generate(profile, 1234);
                        Tilemap visibleGround = entranceRoom.transform.Find("GroundLevel0").GetComponent<Tilemap>();
                        Require(ProceduralRoomBuilder.GetCells(path).Count == profile.secretPathLength * (profile.secretPathHalfWidth * 2 + 1) &&
                            !logical.SecretPath.Any(cell => visibleGround.HasTile(cell)), "Hidden branch is undersized or still has visible ground underneath.");
                        Physics2D.SyncTransforms();
                        Vector3Int outward = ProceduralRoomLayout.DirectionOffset(direction);
                        Vector3Int tangent = outward.x != 0 ? Vector3Int.up : Vector3Int.right;
                        for (int width = -profile.secretPathHalfWidth; width <= profile.secretPathHalfWidth; width++)
                            Require(blocker.OverlapPoint(surface.GetCellCenterWorld(logical.SecretOpening + tangent * width)), "Locked entrance has an unblocked lane.");
                        Require(path.color.a == 0f && blocker.enabled && !entrance.GetComponent<Collider2D>().enabled,
                            "Secret entrance did not start hidden, blocked, and unusable.");
                        Require(closedWall.color.a == 1f, "Locked entrance lost its closed wall state.");
                        profile.isSecretRoom = true;
                        profile.secretRoomBackwardDirection = direction.Opposite();
                        secretRoom = ProceduralRoomBuilder.Build(profile, 9876, false, scene);
                        Gateway[] secretGateways = secretRoom.GetComponentsInChildren<Gateway>();
                        Require(secretGateways.Length == 1 && secretGateways[0].Flow == GatewayFlow.SecretBackward &&
                            secretGateways[0].Direction == direction.Opposite(), "Secret-room mode must contain only its configured backward gateway.");
                        Require(secretRoom.GetComponentInChildren<GatewaySecretVisibility>() == null && secretRoom.transform.Find("FloorTrapdoorTiles") == null,
                            "Secret-room mode incorrectly includes locked entrances or floor trapdoors.");
                        GameObject prefab = ProceduralRoomBuilder.SaveNewPrefab(secretRoom, folder + "/Secret" + caseNumber + ".prefab");
                        RoomManager manager = managerObject.AddComponent<RoomManager>();
                        SerializedObject serializedManager = new SerializedObject(manager);
                        serializedManager.FindProperty("secretGatewayChance").floatValue = 1f;
                        serializedManager.FindProperty("secretRoomPrefabs").arraySize = 1;
                        serializedManager.FindProperty("secretRoomPrefabs").GetArrayElementAtIndex(0).objectReferenceValue = prefab;
                        serializedManager.ApplyModifiedPropertiesWithoutUndo();
                        BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                        Require((GameObject)typeof(RoomManager).GetMethod("FindCompatibleSecretRoomPrefab", flags).Invoke(manager, new object[] { entrance }) == prefab,
                            "RoomManager rejects the generated secret-room destination contract.");
                        RoomInstance before = new GameObject("Earlier Room").AddComponent<RoomInstance>();
                        before.transform.SetParent(manager.transform, false);
                        RoomInstance after = new GameObject("Later Room").AddComponent<RoomInstance>();
                        after.transform.SetParent(manager.transform, false);
                        RoomInstance regular = entranceRoom.GetComponent<RoomInstance>();
                        before.Initialize(1);
                        regular.Initialize(2);
                        after.Initialize(3);
                        List<RoomInstance> rooms = (List<RoomInstance>)typeof(RoomManager).GetField("generatedRooms", flags).GetValue(manager);
                        rooms.AddRange(new[] { before, regular, after });
                        List<RoomInstance> eligible = (List<RoomInstance>)typeof(RoomManager).GetMethod("GetRoomsAtOrAfter", flags).Invoke(manager, new object[] { entrance });
                        Require(eligible.SequenceEqual(new[] { regular, after }), "Secret key eligibility includes an earlier room or omits the entrance/later room.");
                        typeof(RoomManager).GetMethod("ConfigureSpecialGateways", flags).Invoke(manager, null);
                        RoomInstance key = (RoomInstance)typeof(RoomManager).GetField("secretUnlockRoom", flags).GetValue(manager);
                        Require(eligible.Contains(key) && entrance.Destination != null, "Manager did not link the entrance and select an eligible key room.");
                        List<RoomInstance> spawnedSecrets = (List<RoomInstance>)typeof(RoomManager).GetField("generatedSecretRooms", flags).GetValue(manager);
                        GatewayFlow returnFlow = entranceFlow == GatewayFlow.SecretForward ? GatewayFlow.SecretBackward : GatewayFlow.SecretForward;
                        Gateway returnGateway = spawnedSecrets.Single().GetComponentsInChildren<Gateway>().Single(gateway => gateway.Flow == returnFlow);
                        Require(returnGateway.Destination != null && returnGateway.Destination.GetComponent<ElevationDestination>().Flow == returnFlow &&
                            returnGateway.Destination.GetComponent<ElevationDestination>().Direction == returnGateway.Direction.Opposite(), "Secret return gateway is not linked back to the regular room.");
                        Require(Player.Instance == null, "Exit gameplay before running isolated key-room checks.");
                        typeof(RoomManager).GetMethod("HandleRoomCleared", flags).Invoke(manager, new object[] { before });
                        Require(!entranceVisibility.IsVisible && blocker.enabled, "Clearing an earlier room unlocked the secret entrance.");
                        typeof(RoomManager).GetMethod("HandleRoomCleared", flags).Invoke(manager, new object[] { key });
                        Require(entranceVisibility.IsVisible && path.color.a == 1f && !blocker.enabled &&
                            entrance.GetComponent<Collider2D>().enabled && closedWall.color.a == 0f, "Clearing the key room did not reveal the secret path and unlock its colliders.");
                        entranceVisibility.SetInvisible();
                        Require(path.color.a == 0f && blocker.enabled && closedWall.color.a == 1f, "Relocking did not restore the closed secret path.");
                        ProceduralRoomBuilder.ValidateRoom(entranceRoom);
                        GameObject entrancePrefab = ProceduralRoomBuilder.SaveNewPrefab(entranceRoom, folder + "/Entrance" + caseNumber + ".prefab");
                        SerializedObject savedVisibility = new SerializedObject(entrancePrefab.GetComponentInChildren<GatewaySecretVisibility>());
                        Require(savedVisibility.FindProperty("relatedTilemaps").arraySize == 3 && savedVisibility.FindProperty("relatedColliders").arraySize == 1 &&
                            savedVisibility.FindProperty("relatedColliders").GetArrayElementAtIndex(0).FindPropertyRelative("collider").objectReferenceValue != null,
                            "Baking lost the secret path and blocker wiring.");
                        profile.isSecretRoom = false;
                        profile.forwardDirection = others[0];
                        profile.backwardDirection = others[1];
                        Texture2D preview = ProceduralRoomPreview.CreateTexture(ProceduralRoomLayout.Generate(profile, 1234), true);
                        try { Require(preview.GetPixels32().Any(pixel => pixel.Equals(ProceduralRoomPreview.SecretPath)), "Secret branch is absent from the safe bitmap preview."); }
                        finally { Object.DestroyImmediate(preview); }
                        caseNumber++;
                    }
                    finally
                    {
                        Object.DestroyImmediate(entranceRoom);
                        if (secretRoom != null)
                            Object.DestroyImmediate(secretRoom);
                        Object.DestroyImmediate(managerObject);
                    }
                }
            }
            profile.isSecretRoom = false;
            profile.includeSecretGateway = false;
            profile.forwardDirection = GatewayDirection.NorthEast;
            profile.backwardDirection = GatewayDirection.SouthWest;
        }
        finally { UnityEngine.Random.state = state; }
    }

    private static void CheckCavernSettings(ProceduralRoomProfile template)
    {
        ProceduralRoomProfile profile = Object.Instantiate(template);
        try
        {
            Require((int)ProceduralRoomShape.Organic == 2 && (int)ProceduralRoomShape.Cavern == 3,
                "New shapes changed existing serialized shape values.");
            profile.shape = ProceduralRoomShape.Cavern;
            profile.includeFloorGateway = true;
            foreach (Vector2Int dimensions in new[] { new Vector2Int(16, 16), new Vector2Int(36, 28), new Vector2Int(16, 128), new Vector2Int(128, 16) })
            {
                profile.width = dimensions.x;
                profile.height = dimensions.y;
                foreach (float fill in new[] { 0.4f, 0.7f })
                {
                    profile.cavernFloorFill = fill;
                    foreach (int passes in new[] { 1, 6 })
                    {
                        profile.cavernSmoothingPasses = passes;
                        for (int direction = 0; direction < 4; direction++)
                        {
                            profile.backwardDirection = profile.secretRoomBackwardDirection = (GatewayDirection)direction;
                            profile.forwardDirection = (GatewayDirection)((direction + 1) % 4);
                            profile.secretDirection = (GatewayDirection)((direction + 2) % 4);
                            foreach (bool secretRoom in new[] { false, true })
                            {
                                profile.isSecretRoom = secretRoom;
                                profile.includeSecretGateway = !secretRoom;
                                ProceduralRoomLayout layout = ProceduralRoomLayout.Generate(profile, direction + passes * 100);
                                ProceduralRoomLayout repeat = ProceduralRoomLayout.Generate(profile, direction + passes * 100);
                                Require(layout.Ground.SetEquals(repeat.Ground) && layout.Walkable.SetEquals(layout.Ground),
                                    "Cavern tuning lost determinism or walkable floor.");
                                Require(layout.Ground.Except(layout.SecretPath).All(cell => cell.x >= -profile.width / 2 &&
                                    cell.x < -profile.width / 2 + profile.width && cell.y >= -profile.height / 2 && cell.y < -profile.height / 2 + profile.height),
                                    "Cavern carving exceeded the room dimensions.");
                                layout.Validate(profile.UsesFloorGateway);
                            }
                        }
                    }
                }
            }
            profile.width = 36;
            profile.height = 28;
            profile.cavernFloorFill = 0.58f;
            profile.cavernSmoothingPasses = 4;
            profile.isSecretRoom = false;
            profile.includeSecretGateway = false;
            profile.forwardDirection = GatewayDirection.NorthEast;
            profile.backwardDirection = GatewayDirection.SouthWest;
            ProceduralRoomLayout first = ProceduralRoomLayout.Generate(profile, 2026);
            ProceduralRoomLayout second = ProceduralRoomLayout.Generate(profile, 2027);
            Require(!first.Ground.SetEquals(second.Ground), "Different cave seeds produce identical floor.");
            Require(!Enumerable.Range(-profile.height / 2, profile.height).All(cellY => first.Ground.Contains(new Vector3Int(0, cellY, 0))),
                "Cavern still forces a straight cross-corridor to unused sides.");
            Texture2D preview = ProceduralRoomPreview.CreateTexture(first, true);
            try
            {
                Require(preview.GetPixels32().Any(pixel => pixel.Equals(ProceduralRoomPreview.Ground)), "Cavern bitmap preview is blank.");
                File.WriteAllBytes("Temp/ProceduralRoomCavernPreview.png", preview.EncodeToPNG());
            }
            finally { Object.DestroyImmediate(preview); }
            profile.cavernFloorFill = float.NaN;
            RequireThrows(() => ProceduralRoomLayout.Generate(profile, 1), "Invalid cave density was accepted.");
            profile.cavernFloorFill = 0.58f;
            profile.cavernSmoothingPasses = 0;
            RequireThrows(() => ProceduralRoomLayout.Generate(profile, 1), "Invalid cave smoothing was accepted.");
        }
        finally { Object.DestroyImmediate(profile); }
    }

    private static void CheckWalls(ProceduralRoomProfile profile, UnityEngine.SceneManagement.Scene scene, string folder)
    {
        Texture2D texture = new Texture2D(8, 96);
        texture.name = "Wall Footprint Texture";
        texture.SetPixels(Enumerable.Repeat(Color.white, 8 * 96).ToArray());
        texture.Apply();
        AssetDatabase.AddObjectToAsset(texture, profile);
        Sprite tall = Sprite.Create(texture, new Rect(0f, 0f, 8f, 96f), new Vector2(0.5f, 0f), 100f);
        Sprite small = Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 100f);
        Sprite cube = Sprite.Create(texture, new Rect(0f, 0f, 8f, 8f), new Vector2(0.5f, 0.5f), 25f);
        tall.name = "Tall Wall Sprite";
        small.name = "Small Wall Sprite";
        AssetDatabase.AddObjectToAsset(tall, profile);
        AssetDatabase.AddObjectToAsset(small, profile);
        AssetDatabase.AddObjectToAsset(cube, profile);
        Tile tallTile = ScriptableObject.CreateInstance<Tile>();
        Tile smallTile = ScriptableObject.CreateInstance<Tile>();
        Tile cubeTile = ScriptableObject.CreateInstance<Tile>();
        tallTile.sprite = tall;
        smallTile.sprite = small;
        cubeTile.sprite = cube;
        AssetDatabase.AddObjectToAsset(tallTile, profile);
        AssetDatabase.AddObjectToAsset(smallTile, profile);
        AssetDatabase.AddObjectToAsset(cubeTile, profile);
        AssetDatabase.SaveAssetIfDirty(profile);

        GameObject probe = new GameObject("Wall Footprint Regression");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(probe, scene);
        try
        {
            probe.transform.position = new Vector3(6f, -2f, 0f);
            Grid grid = probe.AddComponent<Grid>();
            grid.cellLayout = GridLayout.CellLayout.IsometricZAsY;
            grid.cellSwizzle = GridLayout.CellSwizzle.XYZ;
            grid.cellSize = ProceduralRoomBuilder.CellSize;
            GameObject wallObject = new GameObject("Walls");
            GameObject surfaceObject = new GameObject("Surface");
            wallObject.transform.SetParent(probe.transform, false);
            surfaceObject.transform.SetParent(probe.transform, false);
            wallObject.transform.localPosition = surfaceObject.transform.localPosition = Vector3.up * 0.16f;
            Tilemap walls = wallObject.AddComponent<Tilemap>();
            Tilemap surface = surfaceObject.AddComponent<Tilemap>();
            walls.tileAnchor = surface.tileAnchor = new Vector3(0.5f, 0.5f, 0f);
            Vector3 step = walls.GetCellCenterWorld(Vector3Int.forward) - walls.GetCellCenterWorld(Vector3Int.zero);
            Require(Vector3.Distance(step, new Vector3(0f, 0.16f, 2f)) < 0.0001f, "Native wall Z spacing changed on the dungeon Grid.");
            walls.SetTile(Vector3Int.zero, smallTile);
            Require(!ProceduralRoomBuilder.WallOverlapsGround(walls, Vector3Int.zero, surface,
                new HashSet<Vector3Int> { new Vector3Int(-1, 0, 0), new Vector3Int(0, -1, 0) }),
                "Normal ground contact at the wall base was rejected.");
            HashSet<Vector3Int> distantGround = new HashSet<Vector3Int> { new Vector3Int(-1, 0, 0), new Vector3Int(5, 5, 0) };
            walls.SetTile(Vector3Int.zero, tallTile);
            walls.SetTile(new Vector3Int(50, 0, 0), tallTile);
            Require(ProceduralRoomBuilder.WallOverlapsGround(walls, Vector3Int.zero, surface, distantGround), "Tall wall did not detect distant ground at a concave corner.");
            MethodInfo filter = typeof(ProceduralRoomBuilder).GetMethod("FilterWallColumns", BindingFlags.Static | BindingFlags.NonPublic);
            filter.Invoke(null, new object[] { walls, surface, distantGround });
            Require(!walls.HasTile(Vector3Int.zero) && walls.HasTile(new Vector3Int(50, 0, 0)), "Filtering did not remove only the unsafe wall.");
            walls.ClearAllTiles();
            for (int height = 0; height <= 4; height++)
                walls.SetTile(new Vector3Int(0, 0, height), smallTile);
            distantGround = new HashSet<Vector3Int> { new Vector3Int(4, 4, 0) };
            Require(!ProceduralRoomBuilder.WallOverlapsGround(walls, Vector3Int.zero, surface, distantGround) &&
                ProceduralRoomBuilder.WallOverlapsGround(walls, new Vector3Int(0, 0, 4), surface, distantGround), "Upper-layer projection was confused with base ground coordinates.");
            filter.Invoke(null, new object[] { walls, surface, distantGround });
            Require(ProceduralRoomBuilder.GetCells(walls).Count == 0, "Unsafe upper layer left an incomplete wall column.");
            GameObject groundObject = new GameObject("GroundLevel0");
            groundObject.transform.SetParent(probe.transform, false);
            Tilemap groundVisual = groundObject.AddComponent<Tilemap>();
            groundVisual.tileAnchor = walls.tileAnchor;
            walls.transform.localPosition = Vector3.zero;
            TilemapRenderer wallRenderer = wallObject.AddComponent<TilemapRenderer>();
            TilemapRenderer groundRenderer = groundObject.AddComponent<TilemapRenderer>();
            wallRenderer.mode = groundRenderer.mode = TilemapRenderer.Mode.Individual;
            walls.SetTile(Vector3Int.zero, cubeTile);
            HashSet<Vector3Int> contact = new HashSet<Vector3Int> { new Vector3Int(-1, 0, 0) };
            groundVisual.SetTile(new Vector3Int(-1, 0, 0), cubeTile);
            Require(!ProceduralRoomBuilder.WallOverlapsGround(walls, Vector3Int.zero, surface, contact),
                "Ground-sorted cube contact was rejected.");
            wallRenderer.sortingOrder = groundRenderer.sortingOrder + 20;
            Require(ProceduralRoomBuilder.WallOverlapsGround(walls, Vector3Int.zero, surface, contact),
                "Foreground wall rendering incorrectly bypassed obstruction filtering.");
        }
        finally { Object.DestroyImmediate(probe); }

        List<ProceduralWeightedTile> previousPool = profile.wallTiles;
        profile.wallTiles = new List<ProceduralWeightedTile> { new ProceduralWeightedTile { tile = smallTile } };
        profile.forwardDirection = GatewayDirection.NorthEast;
        profile.backwardDirection = GatewayDirection.SouthWest;
        profile.includeSecretGateway = true;
        profile.secretDirection = GatewayDirection.NorthWest;
        try
        {
            foreach (ProceduralRoomShape shape in Enum.GetValues(typeof(ProceduralRoomShape)))
            {
                profile.shape = shape;
                foreach (int height in new[] { 1, 4, 8 })
                {
                    profile.wallHeightInTiles = height;
                    GameObject room = ProceduralRoomBuilder.Build(profile, 2026, false, scene);
                    try
                    {
                        Tilemap wallMap = room.transform.Find("WallVisuals0").GetComponent<Tilemap>();
                        Tilemap groundMap = room.transform.Find("GroundLevel0").GetComponent<Tilemap>();
                        TilemapRenderer wallRenderer = wallMap.GetComponent<TilemapRenderer>();
                        TilemapRenderer groundRenderer = groundMap.GetComponent<TilemapRenderer>();
                        Require(wallMap.tileAnchor == groundMap.tileAnchor && wallMap.transform.localPosition == groundMap.transform.localPosition &&
                            wallMap.orientation == groundMap.orientation && wallMap.orientationMatrix == groundMap.orientationMatrix &&
                            wallRenderer.sortingLayerID == groundRenderer.sortingLayerID && wallRenderer.sortingOrder == groundRenderer.sortingOrder &&
                            wallRenderer.mode == groundRenderer.mode && wallRenderer.sharedMaterial == groundRenderer.sharedMaterial,
                            "Wall bases must share the ground anchor, transform, orientation, material and rendering settings.");
                        HashSet<Vector3Int> cells = ProceduralRoomBuilder.GetCells(wallMap);
                        Require(cells.Count > 0 && cells.Max(cell => cell.z) == height - 1, "Same-map wall Z layers are missing.");
                        Require(!room.GetComponentsInChildren<Tilemap>().Any(map => map.name.Contains("_Height")) &&
                            room.GetComponentsInChildren<AStarStairLink>().Length == 0 && room.GetComponentsInChildren<AStarWalkableMap>().Length == 1,
                            "Flat generator created extra height maps or automatic stairs.");
                        ProceduralRoomLayout layout = ProceduralRoomLayout.Generate(profile, 2026);
                        Tilemap surface = room.GetComponentInChildren<AStarWalkableMap>().GetComponent<Tilemap>();
                        Require(ProceduralRoomBuilder.GetCells(surface).SetEquals(layout.Ground), "Wall filtering removed walkable ground.");
                        CheckSurfaceBoundary(surface, room.transform.Find("ColliderLevel0").GetComponent<Tilemap>());
                        GatewaySecretVisibility visibility = room.GetComponentInChildren<GatewaySecretVisibility>();
                        Require(new SerializedObject(visibility).FindProperty("relatedTilemaps").arraySize == 3, "Wall layers added redundant secret visibility maps.");
                        InitializeVisibility(visibility);
                        visibility.UnlockSecretRoom();
                        Require(room.transform.Find("SecretPathWalls").GetComponent<Tilemap>().color.a == 1f &&
                            room.transform.Find("SecretEntranceClosedWall").GetComponent<Tilemap>().color.a == 0f, "Wall Z layers do not follow secret unlock state.");
                        visibility.SetInvisible();
                        GameObject prefab = ProceduralRoomBuilder.SaveNewPrefab(room, folder + "/WallZ.prefab");
                        Require(ProceduralRoomBuilder.GetCells(prefab.transform.Find("WallVisuals0").GetComponent<Tilemap>()).SetEquals(cells), "Prefab lost wall Z cells.");
                    }
                    finally { Object.DestroyImmediate(room); }
                }
            }
        }
        finally
        {
            profile.wallTiles = previousPool;
            profile.wallHeightInTiles = 1;
            profile.includeSecretGateway = false;
        }

        ProceduralRoomProfile authored = AssetDatabase.LoadAssetAtPath<ProceduralRoomProfile>("Assets/RoomGenerationProfile.asset");
        if (authored == null)
            return;
        ProceduralRoomProfile copy = Object.Instantiate(authored);
        try
        {
            foreach (ProceduralRoomShape shape in Enum.GetValues(typeof(ProceduralRoomShape)))
            {
                copy.shape = shape;
                GameObject room = ProceduralRoomBuilder.Build(copy, copy.seed, false, scene);
                try
                {
                    int count = ProceduralRoomBuilder.GetCells(room.transform.Find("WallVisuals0").GetComponent<Tilemap>()).Count;
                    Require(count > 0, "Sprite-footprint filtering removed all walls from the current authored profile.");
                    if (shape == ProceduralRoomShape.Rectangle)
                        Require(count >= (copy.width + copy.height) * copy.wallHeightInTiles / 2,
                            "Ground-aligned walls retained only isolated columns rather than the north perimeter.");
                    Debug.Log($"Wall Z check, current profile {shape}: {count} retained visual tiles, height {copy.wallHeightInTiles}.");
                }
                finally { Object.DestroyImmediate(room); }
            }
        }
        finally { Object.DestroyImmediate(copy); }
    }

    private static void CheckCombinedGatewayPairs(GameObject room)
    {
        Gateway[] gateways = room.GetComponentsInChildren<Gateway>();
        ElevationDestination[] destinations = room.GetComponentsInChildren<ElevationDestination>();
        Require(gateways.Count(gateway => gateway.Flow == GatewayFlow.Forward) == 1 &&
            gateways.Count(gateway => gateway.Flow == GatewayFlow.Backward) == 1 && destinations.Length == 2,
            "Composition duplicated or lost regular gateways/destinations.");
        foreach (Gateway gateway in gateways)
        {
            if (gateway.Flow == GatewayFlow.Floor)
                continue;
            GatewayFlow arrivalFlow = gateway.Flow == GatewayFlow.Forward ? GatewayFlow.Backward : GatewayFlow.Forward;
            ElevationDestination arrival = destinations.SingleOrDefault(destination => destination.Flow == arrivalFlow && destination.Direction == gateway.Direction);
            Transform section = gateway.transform;
            while (section.parent != room.transform)
                section = section.parent;
            AStarWalkableMap surface = section.GetComponentInChildren<AStarWalkableMap>();
            Require(arrival != null && arrival.transform.IsChildOf(section) && arrival.ElevationLevel == surface.ElevationLevel &&
                gateway.ElevationLevel == surface.ElevationLevel && gateway.Destination == null,
                "Gateway and arrival destination lost source-section ownership, elevation or cleared runtime links.");
        }
    }

    private static void CheckPopulation(ProceduralRoomProfile template, UnityEngine.SceneManagement.Scene scene, string folder)
    {
        ProceduralRoomProfile profile = Object.Instantiate(template);
        GameObject propSource = new GameObject("NonBlocking Decoration");
        GameObject lightSource = new GameObject("Scatter Light");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(propSource, scene);
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lightSource, scene);
        GameObject first = null;
        GameObject repeat = null;
        GameObject cut = null;
        try
        {
            propSource.AddComponent<BoxCollider2D>().size = new Vector2(0.04f, 0.04f);
            ProceduralDecorationSettings propSettings = propSource.AddComponent<ProceduralDecorationSettings>();
            propSettings.cutsWalkableTiles = false;
            GameObject nonBlocking = PrefabUtility.SaveAsPrefabAsset(propSource, folder + "/NonBlockingDecoration.prefab");
            propSettings.cutsWalkableTiles = true;
            GameObject blocking = PrefabUtility.SaveAsPrefabAsset(propSource, folder + "/BlockingDecoration.prefab");
            Light2D light = lightSource.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.intensity = 3.5f;
            light.color = new Color(0.8f, 0.3f, 0.6f);
            GameObject lightPrefab = PrefabUtility.SaveAsPrefabAsset(lightSource, folder + "/ScatteredLight.prefab");
            profile.shape = ProceduralRoomShape.Rectangle;
            profile.width = profile.height = 36;
            profile.forwardDirection = GatewayDirection.NorthEast;
            profile.backwardDirection = GatewayDirection.SouthWest;
            profile.isSecretRoom = false;
            profile.includeFloorGateway = false;
            profile.includeSecretGateway = false;
            profile.enemySpawnPoints = 0;
            profile.decorationCount = 4;
            profile.decorationClearance = 0;
            profile.decorationPrefabs = new List<GameObject> { nonBlocking };
            profile.lightPrefabs = new List<GameObject> { lightPrefab };
            profile.lightCount = 6;
            profile.lightSpacing = 4;
            ProceduralRoomLayout layout = ProceduralRoomLayout.Generate(profile, 2026);
            first = ProceduralRoomBuilder.Build(profile, 2026, false, scene);
            repeat = ProceduralRoomBuilder.Build(profile, 2026, false, scene);
            Tilemap map = first.GetComponentInChildren<AStarWalkableMap>().GetComponent<Tilemap>();
            Require(first.GetComponentsInChildren<ProceduralDecorationSettings>().Length == 4 &&
                ProceduralRoomBuilder.GetCells(map).SetEquals(layout.Ground), "Non-cutting decorations or scattered lights removed walkable cells.");
            Transform lights = first.transform.Find("ScatteredLights");
            Transform repeatedLights = repeat.transform.Find("ScatteredLights");
            Require(lights.childCount == 6 && repeatedLights.childCount == 6, "Scattered light count was not honored.");
            List<Vector3Int> placed = new List<Vector3Int>();
            for (int index = 0; index < lights.childCount; index++)
            {
                Light2D placedLight = lights.GetChild(index).GetComponentInChildren<Light2D>();
                Vector3Int cell = map.WorldToCell(lights.GetChild(index).position);
                Require(lights.GetChild(index).position == repeatedLights.GetChild(index).position && map.HasTile(cell) &&
                    !layout.Reserved.Contains(cell) && placed.All(previous => (previous - cell).sqrMagnitude >= 16),
                    "Light scattering lost determinism, clearance or cell spacing.");
                Require(placedLight.intensity == 3.5f && placedLight.color == light.color &&
                    new SerializedObject(placedLight.GetComponent<LightElevationVisibility>()).FindProperty("lightLevel").intValue == 0,
                    "Scattered lights lost authored settings or elevation metadata.");
                placed.Add(cell);
            }
            profile.decorationPrefabs = new List<GameObject> { blocking };
            cut = ProceduralRoomBuilder.Build(profile, 2026, false, scene);
            Require(cut.GetComponentsInChildren<ProceduralDecorationSettings>().Length == 4 &&
                ProceduralRoomBuilder.GetCells(cut.GetComponentInChildren<AStarWalkableMap>().GetComponent<Tilemap>()).Count < layout.Ground.Count,
                "Cutting decorations no longer removed their navigation footprints.");
            ProceduralRoomBuilder.ValidateRoom(cut);
        }
        finally
        {
            if (first != null) Object.DestroyImmediate(first);
            if (repeat != null) Object.DestroyImmediate(repeat);
            if (cut != null) Object.DestroyImmediate(cut);
            Object.DestroyImmediate(profile);
            Object.DestroyImmediate(propSource);
            Object.DestroyImmediate(lightSource);
        }
    }

    private static void CheckGeneratorUpgrades(ProceduralRoomProfile template, UnityEngine.SceneManagement.Scene scene, string folder)
    {
        ProceduralRoomProfile profile = Object.Instantiate(template);
        GameObject visualSource = new GameObject("North Wall Visual");
        GameObject markerSource = new GameObject("Radius Spawn Marker");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(visualSource, scene);
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(markerSource, scene);
        try
        {
            visualSource.AddComponent<SpriteRenderer>();
            profile.northEastGatewayVisualPrefab = PrefabUtility.SaveAsPrefabAsset(visualSource, folder + "/NorthEastVisual.prefab");
            profile.northWestGatewayVisualPrefab = PrefabUtility.SaveAsPrefabAsset(visualSource, folder + "/NorthWestVisual.prefab");
            profile.forwardDirection = GatewayDirection.NorthEast;
            profile.backwardDirection = GatewayDirection.NorthWest;
            profile.isSecretRoom = false;
            profile.includeSecretGateway = false;
            profile.includeFloorGateway = true;
            profile.elevationLevel = profile.baseZ = 5;
            profile.enemySpawnPoints = 4;
            profile.enemyBoundaryClearance = 3;
            foreach (ProceduralRoomShape shape in new[] { ProceduralRoomShape.Donut, ProceduralRoomShape.HallwayCaves,
                ProceduralRoomShape.TwinDonuts, ProceduralRoomShape.WindingPassage })
            {
                profile.shape = shape;
                foreach (int dimension in new[] { 16, 36, 64, 128 })
                {
                    profile.width = profile.height = dimension;
                    for (int seed = 0; seed < 12; seed++)
                    {
                        ProceduralRoomLayout layout = ProceduralRoomLayout.Generate(profile, seed);
                        Require(layout.Ground.SetEquals(ProceduralRoomLayout.Generate(profile, seed).Ground), "New shape is not seeded.");
                        layout.Validate(true);
                        if (shape == ProceduralRoomShape.Donut)
                            Require(!layout.Ground.Contains(Vector3Int.zero), "Donut lost its central hole.");
                        if (shape == ProceduralRoomShape.TwinDonuts)
                            Require(!layout.Ground.Contains(new Vector3Int(Mathf.RoundToInt(-0.5f - dimension * 0.225f), 0, 0)) &&
                                !layout.Ground.Contains(new Vector3Int(Mathf.RoundToInt(-0.5f + dimension * 0.225f), 0, 0)), "Twin Donuts lost an interior hole.");
                    }
                }
                profile.width = profile.height = 36;
                GameObject room = ProceduralRoomBuilder.Build(profile, 12345, false, scene);
                try
                {
                    Tilemap surface = room.GetComponentInChildren<AStarWalkableMap>().GetComponent<Tilemap>();
                    Tilemap collision = room.transform.Find("ColliderLevel0").GetComponent<Tilemap>();
                    Require(collision.GetComponent<PolygonCollider2D>() == null && collision.GetComponent<TilemapCollider2D>() != null &&
                        collision.GetComponent<CompositeCollider2D>().generationType == CompositeCollider2D.GenerationType.Synchronous,
                        "Generated boundary still depends on manually rebuilt polygons.");
                    TileBase provided = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Sprites/Tiles/TilemapPalette/NotWalkableTile.asset");
                    Require(ProceduralRoomBuilder.GetCells(collision).All(cell => collision.GetTile(cell) == provided), "Boundary does not use the provided NotWalkableTile.");
                    CheckSurfaceBoundary(surface, collision);
                    foreach (Gateway gateway in room.GetComponentsInChildren<Gateway>())
                    {
                        if (gateway.Flow == GatewayFlow.Floor)
                            continue;
                        Transform visual = gateway.transform.GetChild(0);
                        GameObject expected = gateway.Direction == GatewayDirection.NorthEast ? profile.northEastGatewayVisualPrefab : profile.northWestGatewayVisualPrefab;
                        Vector3Int cell = ProceduralTilemapPlane.WorldToCell(surface, gateway.transform.position);
                        Vector3 wallContact = (surface.GetCellCenterWorld(cell) + surface.GetCellCenterWorld(cell + ProceduralRoomLayout.DirectionOffset(gateway.Direction))) * 0.5f;
                        Require(PrefabUtility.GetCorrespondingObjectFromSource(visual.gameObject) == expected && Vector3.Distance(visual.position, wallContact) < 0.0001f,
                            "Northern gateway used the wrong prefab or floated away from its wall face.");
                    }
                    CheckEnemyClearance(room, profile.enemyBoundaryClearance);
                    Vector3Int paintCell = new Vector3Int(200, 200, profile.baseZ);
                    TilemapCollider2D tiles = collision.GetComponent<TilemapCollider2D>();
                    CompositeCollider2D composite = collision.GetComponent<CompositeCollider2D>();
                    collision.SetTile(paintCell, provided);
                    tiles.ProcessTilemapChanges();
                    Physics2D.SyncTransforms();
                    Require(composite.OverlapPoint(collision.GetCellCenterWorld(paintCell)), "Painting a boundary tile did not update collision automatically.");
                    collision.SetTile(paintCell, null);
                    tiles.ProcessTilemapChanges();
                    Physics2D.SyncTransforms();
                    Require(!composite.OverlapPoint(collision.GetCellCenterWorld(paintCell)), "Erasing a boundary tile left stale collision.");
                }
                finally { Object.DestroyImmediate(room); }
            }
            EnemySpawnPoint marker = markerSource.AddComponent<EnemySpawnPoint>();
            SerializedObject markerSettings = new SerializedObject(marker);
            markerSettings.FindProperty("spawnRadius").floatValue = 0.2f;
            markerSettings.ApplyModifiedPropertiesWithoutUndo();
            profile.enemySpawnPointPrefab = PrefabUtility.SaveAsPrefabAsset(markerSource, folder + "/RadiusMarker.prefab");
            profile.shape = ProceduralRoomShape.Rectangle;
            profile.width = profile.height = 48;
            GameObject radiusRoom = ProceduralRoomBuilder.Build(profile, 12345, false, scene);
            try
            {
                Require(radiusRoom.GetComponentsInChildren<EnemySpawnPoint>().Length == 4, "Inland placement failed to populate a spacious room.");
                CheckEnemyClearance(radiusRoom, 4);
            }
            finally { Object.DestroyImmediate(radiusRoom); }
        }
        finally
        {
            Object.DestroyImmediate(visualSource);
            Object.DestroyImmediate(markerSource);
            Object.DestroyImmediate(profile);
        }
    }

    private static void CheckEnemyClearance(GameObject room, int clearance)
    {
        Tilemap surface = room.GetComponentInChildren<AStarWalkableMap>().GetComponent<Tilemap>();
        foreach (EnemySpawnPoint marker in room.GetComponentsInChildren<EnemySpawnPoint>())
        {
            Vector3Int cell = ProceduralTilemapPlane.WorldToCell(surface, marker.transform.position);
            for (int offsetX = -clearance; offsetX <= clearance; offsetX++)
            for (int offsetY = -clearance; offsetY <= clearance; offsetY++)
                Require(surface.HasTile(cell + new Vector3Int(offsetX, offsetY, 0)), "Enemy marker is too close to a boundary or cut decoration.");
        }
    }

    private static void CheckGeneratedPlanes(ProceduralRoomProfile template, UnityEngine.SceneManagement.Scene scene, string folder)
    {
        ProceduralRoomProfile profile = Object.Instantiate(template);
        try
        {
            profile.shape = ProceduralRoomShape.Rectangle;
            profile.width = profile.height = 36;
            profile.forwardDirection = GatewayDirection.NorthEast;
            profile.backwardDirection = GatewayDirection.SouthWest;
            profile.secretDirection = GatewayDirection.NorthWest;
            profile.wallHeightInTiles = 3;
            profile.includeFloorGateway = true;
            profile.includeSecretGateway = true;
            profile.enemySpawnPoints = 2;
            profile.decorationCount = 2;
            profile.decorationClearance = 0;
            profile.decorationPrefabs = new List<GameObject> { AssetDatabase.LoadAssetAtPath<GameObject>(folder + "/NonBlockingDecoration.prefab") };
            profile.lightPrefabs = new List<GameObject> { AssetDatabase.LoadAssetAtPath<GameObject>(folder + "/ScatteredLight.prefab") };
            profile.lightCount = 3;
            foreach (bool secret in new[] { false, true })
            {
                profile.isSecretRoom = secret;
                profile.baseZ = profile.elevationLevel = 0;
                GameObject original = ProceduralRoomBuilder.Build(profile, 2026, false, scene);
                try
                {
                    foreach (int elevation in new[] { 0, 5 })
                    foreach (int baseZ in new[] { 0, 5 })
                    {
                        profile.elevationLevel = elevation;
                        profile.baseZ = baseZ;
                        GameObject room = ProceduralRoomBuilder.Build(profile, 2026, false, scene);
                        GameObject restored = null;
                        try
                        {
                            AStarWalkableMap navigation = room.GetComponentInChildren<AStarWalkableMap>();
                            Tilemap surface = navigation.GetComponent<Tilemap>();
                            Vector3 displacement = surface.GetCellCenterWorld(new Vector3Int(0, 0, baseZ)) - surface.GetCellCenterWorld(Vector3Int.zero);
                            Require(navigation.ElevationLevel == elevation, "Generated navigation has the wrong gameplay elevation.");
                            foreach (Tilemap map in room.GetComponentsInChildren<Tilemap>(true))
                            {
                                Tilemap source = original.transform.Find(map.name).GetComponent<Tilemap>();
                                Require(ProceduralRoomBuilder.GetCells(map).SetEquals(ProceduralRoomBuilder.GetCells(source).Select(cell => cell + Vector3Int.forward * baseZ)),
                                    map.name + " lost cells or relative wall height when shifting base Z.");
                                if (baseZ != 0)
                                    Require(map.GetComponent<ProceduralTilemapPlane>().baseZ == baseZ, "Generated map is missing its base-Z metadata.");
                            }
                            Require(ProceduralRoomBuilder.GetCells(room.transform.Find("WallVisuals0").GetComponent<Tilemap>())
                                .Select(cell => cell.z).Distinct().OrderBy(value => value).SequenceEqual(new[] { baseZ, baseZ + 1, baseZ + 2 }),
                                "Generated wall layers do not start at Base Z.");
                            foreach (Transform marker in room.GetComponentsInChildren<Transform>(true).Where(candidate =>
                                candidate.GetComponent<RoomSpawnPoint>() != null || candidate.GetComponent<Gateway>() != null ||
                                candidate.GetComponent<ElevationDestination>() != null || candidate.GetComponent<EnemySpawnPoint>() != null ||
                                candidate.GetComponent<ProceduralDecorationSettings>() != null || candidate.GetComponent<Light2D>() != null))
                            {
                                Stack<int> siblingPath = new Stack<int>();
                                for (Transform ancestor = marker; ancestor != room.transform; ancestor = ancestor.parent)
                                    siblingPath.Push(ancestor.GetSiblingIndex());
                                Transform sourceMarker = original.transform;
                                foreach (int siblingIndex in siblingPath)
                                    sourceMarker = sourceMarker.GetChild(siblingIndex);
                                Require(Vector3.Distance(marker.position, sourceMarker.position + displacement) < 0.0001f,
                                    marker.name + " did not follow the native base-Z displacement.");
                            }
                            Require(room.GetComponentInChildren<RoomSpawnPoint>().ElevationLevel == elevation &&
                                room.GetComponentsInChildren<Gateway>(true).All(gateway => gateway.ElevationLevel == elevation) &&
                                room.GetComponentsInChildren<ElevationDestination>(true).All(destination => destination.ElevationLevel == elevation),
                                "Generated spawn/gateway/destination elevations differ from the profile.");
                            foreach (TilemapElevationVisibility visibility in room.GetComponentsInChildren<TilemapElevationVisibility>(true))
                                Require(new SerializedObject(visibility).FindProperty("tilemapLevel").intValue == elevation, "Generated tilemap visibility has the wrong level.");
                            foreach (EnemySpawnPoint enemy in room.GetComponentsInChildren<EnemySpawnPoint>(true))
                                Require(new SerializedObject(enemy).FindProperty("elevationLevel").intValue == elevation, "Generated enemy spawn has the wrong level.");
                            foreach (LightElevationVisibility light in room.GetComponentsInChildren<LightElevationVisibility>(true))
                                Require(new SerializedObject(light).FindProperty("lightLevel").intValue == elevation, "Generated light has the wrong level.");
                            foreach (WallOcclusionFade fade in room.GetComponentsInChildren<WallOcclusionFade>(true))
                                Require(fade.tilemapLevel == elevation, "Generated wall fading has the wrong level.");
                            Vector3 actor = room.GetComponentInChildren<RoomSpawnPoint>().transform.position;
                            actor.z = 0f;
                            Require(surface.HasTile(ProceduralTilemapPlane.WorldToCell(surface, actor)), "Flat-Z actor cannot find the generated navigation plane.");
                            Physics2D.SyncTransforms();
                            Tilemap boundary = room.transform.Find("ColliderLevel0").GetComponent<Tilemap>();
                            CompositeCollider2D composite = boundary.GetComponent<CompositeCollider2D>();
                            Vector3Int boundaryCell = ProceduralRoomBuilder.GetCells(boundary).First();
                            Require(composite.enabled && composite.pathCount > 0 && composite.OverlapPoint(boundary.GetCellCenterWorld(boundaryCell)),
                                "Generated shifted boundary lost physical collision.");
                            ProceduralRoomBuilder.ValidateRoom(room);
                            GameObject prefab = ProceduralRoomBuilder.SaveNewPrefab(room, $"{folder}/Generator_{secret}_{elevation}_{baseZ}.prefab");
                            restored = Object.Instantiate(prefab);
                            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(restored, scene);
                            ProceduralRoomBuilder.ValidateRoom(restored);
                            Require(restored.GetComponentInChildren<AStarWalkableMap>().ElevationLevel == elevation &&
                                ProceduralRoomBuilder.GetCells(restored.transform.Find("GroundLevel0").GetComponent<Tilemap>()).All(cell => cell.z == baseZ),
                                "Generated prefab lost elevation or base Z.");
                        }
                        finally
                        {
                            if (restored != null) Object.DestroyImmediate(restored);
                            Object.DestroyImmediate(room);
                        }
                    }
                }
                finally { Object.DestroyImmediate(original); }
            }
            profile.baseZ = -1;
            RequireThrows(() => ProceduralRoomBuilder.Build(profile, 2026, false, scene), "Generator accepted negative Base Z.");
            profile.baseZ = 0;
            profile.elevationLevel = -1;
            RequireThrows(() => ProceduralRoomBuilder.Build(profile, 2026, false, scene), "Generator accepted negative elevation.");
        }
        finally { Object.DestroyImmediate(profile); }
    }

    private static void CheckWallFading(ProceduralRoomProfile profile, UnityEngine.SceneManagement.Scene scene)
    {
        Texture2D texture = new Texture2D(16, 32);
        texture.SetPixels(Enumerable.Repeat(Color.white, 16 * 32).ToArray());
        texture.Apply();
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 16f, 32f), new Vector2(0.5f, 0.5f), 100f);
        Tile tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = sprite;
        GameObject root = new GameObject("Wall Fade Regression");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
        try
        {
            Grid grid = root.AddComponent<Grid>();
            grid.cellLayout = GridLayout.CellLayout.IsometricZAsY;
            grid.cellSwizzle = GridLayout.CellSwizzle.XYZ;
            grid.cellSize = ProceduralRoomBuilder.CellSize;
            Tilemap map = FixtureChild(root, "WallVisuals0").AddComponent<Tilemap>();
            map.tileAnchor = new Vector3(0.5f, 0.5f, 0f);
            foreach (int cellX in new[] { 0, 20 })
            {
                for (int cellZ = 5; cellZ <= 7; cellZ++)
                {
                    Vector3Int cell = new Vector3Int(cellX, 0, cellZ);
                    map.SetTile(cell, tile);
                    map.SetTileFlags(cell, TileFlags.None);
                    map.SetColor(cell, new Color(0.4f, 0.6f, 0.8f, 0.7f));
                    map.SetTileFlags(cell, TileFlags.LockColor);
                }
            }
            BoxCollider2D solid = map.gameObject.AddComponent<BoxCollider2D>();
            WallOcclusionFade fade = map.gameObject.AddComponent<WallOcclusionFade>();
            fade.tilemapLevel = 5;
            fade.RebuildCache();
            Vector3Int baseCell = new Vector3Int(0, 0, 5);
            Vector3 near = map.GetCellCenterWorld(baseCell) + Vector3.right * 0.16f;
            fade.RefreshFade(near, 5, fade.fadeDuration * 0.5f);
            Require(map.GetColor(baseCell).a < 0.7f && map.GetColor(baseCell).a > 0.7f * fade.fadedAlpha,
                "Wall fading did not transition smoothly near its projected footprint.");
            fade.RefreshFade(near, 5, fade.fadeDuration);
            for (int cellZ = 5; cellZ <= 7; cellZ++)
                Require(Mathf.Abs(map.GetColor(new Vector3Int(0, 0, cellZ)).a - 0.7f * fade.fadedAlpha) < 0.0001f,
                    "Wall fading did not cover the entire cell-Z column.");
            Require(Mathf.Abs(map.GetColor(new Vector3Int(20, 0, 5)).a - 0.7f) < 0.0001f && solid.enabled && !solid.isTrigger,
                "Fading changed distant walls or physical collision.");
            Color hidden = map.color;
            hidden.a = 0f;
            map.color = hidden;
            fade.RefreshFade(near, 5, fade.fadeDuration);
            Require(map.color.a == 0f, "Wall fading overrode elevation/secret map visibility.");
            fade.RefreshFade(near, 6, fade.fadeDuration);
            Require(Mathf.Abs(map.GetColor(baseCell).a - 0.7f) < 0.0001f, "A player above the wall section kept lower walls faded.");
            fade.RefreshFade(near, 0, fade.fadeDuration);
            Require(map.GetColor(baseCell).a < 0.7f, "Upper walls did not fade over a lower-level player.");
            fade.RefreshFade(near + Vector3.right * 100f, 5, fade.fadeDuration);
            Require(map.GetColor(baseCell) == new Color(0.4f, 0.6f, 0.8f, 0.7f), "Wall color did not restore after leaving.");
            typeof(WallOcclusionFade).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(fade, null);
            Require(map.GetTileFlags(baseCell) == TileFlags.LockColor, "Disabling wall fade did not restore authored tile flags.");
            Tilemap detection = FixtureChild(root, "MultiZStairDetection").AddComponent<Tilemap>();
            detection.SetTile(new Vector3Int(0, 0, 5), profile.navigationTile);
            Vector3 detectionPoint = detection.GetCellCenterWorld(new Vector3Int(0, 0, 5));
            detectionPoint.z = 0f;
            Require(ProceduralTilemapPlane.ContainsDetection(detection, detectionPoint), "Palette-Z stair detection was lost for a 2D actor.");
        }
        finally
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(tile);
            Object.DestroyImmediate(sprite);
            Object.DestroyImmediate(texture);
        }
    }

    private static void CheckComposition(ProceduralRoomProfile profile, UnityEngine.SceneManagement.Scene scene, string folder)
    {
        foreach (int levelGap in new[] { 1, 5 })
        foreach (bool ascend in new[] { true, false })
        {
            int baseLevel = ascend ? 0 : levelGap;
            int sectionLevel = ascend ? levelGap : 0;
            int sectionBaseZ = levelGap == 5 ? 5 : 0;
            int baseSide = ascend ? -1 : 1;
            GameObject baseRoom = CompositionFixture("Composer Base", profile, scene, baseSide, baseLevel);
            GameObject section = CompositionFixture("Composer Section", profile, scene, -baseSide, 0);
            section.transform.position = new Vector3(8f, -3f, 0f);
            GameObject combined = null;
            GameObject managerObject = new GameObject("Composer Navigation Check");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(managerObject, scene);
            try
            {
                Tilemap baseMap = baseRoom.GetComponentInChildren<AStarWalkableMap>().GetComponent<Tilemap>();
                Vector3 baseAnchor = baseMap.GetCellCenterWorld(Vector3Int.zero);
                Vector3 lower = ascend ? baseAnchor : baseAnchor - Vector3.up * 0.8f;
                Vector3 upper = ascend ? baseAnchor + Vector3.up * 0.8f : baseAnchor;
                Transform entry = FixtureChild(baseRoom, "LowerAnchor").transform;
                Transform exit = FixtureChild(baseRoom, "UpperAnchor").transform;
                entry.position = lower;
                exit.position = upper;
                GameObject rampObject = FixtureChild(baseRoom, "AuthoredRamp");
                rampObject.transform.position = (lower + upper) * 0.5f;
                BoxCollider2D ramp = rampObject.AddComponent<BoxCollider2D>();
                ramp.isTrigger = true;
                ramp.size = new Vector2(0.16f, 0.7f);
                rampObject.AddComponent<RampMovementTrigger>();
                AStarStairLink stair = rampObject.AddComponent<AStarStairLink>();
                SerializedObject serializedStair = new SerializedObject(stair);
                serializedStair.FindProperty("entryAnchor").objectReferenceValue = entry;
                serializedStair.FindProperty("exitAnchor").objectReferenceValue = exit;
                serializedStair.ApplyModifiedPropertiesWithoutUndo();
                RampElevationTrigger lowerTrigger = FixtureElevationTrigger(baseRoom, "LowerTrigger", lower);
                RampElevationTrigger upperTrigger = FixtureElevationTrigger(baseRoom, "UpperTrigger", upper);
                Transform landing = section.GetComponentInChildren<RoomSpawnPoint>().transform;
                GameObject detectionObject = FixtureChild(baseRoom, "AuthoredStairDetection");
                detectionObject.transform.position = (lower + upper) * 0.5f;
                detectionObject.transform.localScale = new Vector3(3f, 6f, 1f);
                Tilemap detection = detectionObject.AddComponent<Tilemap>();
                detection.tileAnchor = new Vector3(0.5f, 0.5f, 0f);
                detection.SetTile(Vector3Int.zero, profile.navigationTile);
                detection.transform.position += (lower + upper) * 0.5f - detection.GetCellCenterWorld(Vector3Int.zero);
                detectionObject.AddComponent<AStarDetectionMap>();
                detectionObject.AddComponent<AStarDetectionGizmo>();
                string beforeBase = EditorJsonUtility.ToJson(baseRoom.GetComponent<RoomInstance>());
                Vector3 beforePosition = section.transform.position;

                var previousActiveScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                var undoScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                try
                {
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(baseRoom, undoScene);
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(section, undoScene);
                    Undo.IncrementCurrentGroup();
                    Undo.SetCurrentGroupName("Composition Regression");
                    int undoGroup = Undo.GetCurrentGroup();
                    GameObject undoRoom = ProceduralRoomComposer.Combine(baseRoom, section, stair, landing, lowerTrigger, upperTrigger, ascend,
                        sectionElevation: sectionLevel, sectionBaseZ: sectionBaseZ);
                    Undo.FlushUndoRecordObjects();
                    Undo.RevertAllDownToGroup(undoGroup);
                    Require(undoRoom == null && baseRoom != null && section != null,
                        $"Undo did not remove only the new composition: combined exists {undoRoom != null}, base exists {baseRoom != null}, section exists {section != null}, group {undoGroup}, current group {Undo.GetCurrentGroup()}.");
                }
                finally
                {
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(baseRoom, scene);
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(section, scene);
                    EditorSceneManager.CloseScene(undoScene, true);
                    UnityEngine.SceneManagement.SceneManager.SetActiveScene(previousActiveScene);
                }

                combined = ProceduralRoomComposer.Combine(baseRoom, section, stair, landing, lowerTrigger, upperTrigger, ascend, false, sectionLevel, sectionBaseZ);
                Require(combined.GetComponent<Grid>() == null && combined.GetComponentsInChildren<Grid>().Length == 2 &&
                    combined.GetComponentsInChildren<RoomInstance>().Length == 1, "Composition did not preserve child Grids with one empty progression root.");
                Require(EditorJsonUtility.ToJson(baseRoom.GetComponent<RoomInstance>()) == beforeBase && section.transform.position == beforePosition &&
                    section.GetComponentsInChildren<Gateway>().Length == 2 && section.GetComponent<RoomInstance>() != null && landing != null,
                    "Composition changed a source room or removed its landing helper.");
                Transform sectionCopy = combined.transform.Find("ElevationSection_" + sectionLevel);
                Tilemap shiftedGround = sectionCopy.Find("GroundLevel0").GetComponent<Tilemap>();
                Tilemap shiftedWalls = sectionCopy.Find("WallVisuals0").GetComponent<Tilemap>();
                Tilemap shiftedCollision = sectionCopy.Find("ColliderLevel0").GetComponent<Tilemap>();
                Require(ProceduralRoomBuilder.GetCells(shiftedGround).All(cell => cell.z == sectionBaseZ) &&
                    ProceduralRoomBuilder.GetCells(shiftedWalls).Select(cell => cell.z).Distinct().OrderBy(value => value)
                        .SequenceEqual(new[] { sectionBaseZ, sectionBaseZ + 1, sectionBaseZ + 2 }) &&
                    ProceduralRoomBuilder.GetCells(shiftedCollision).All(cell => cell.z == sectionBaseZ),
                    "Attached ground, wall layers or collision did not start at the requested base Z.");
                Vector3Int painted = new Vector3Int(0, 0, sectionBaseZ);
                Require(shiftedGround.GetColor(painted) == section.transform.Find("GroundLevel0").GetComponent<Tilemap>().GetColor(Vector3Int.zero) &&
                    shiftedGround.GetTransformMatrix(painted) == section.transform.Find("GroundLevel0").GetComponent<Tilemap>().GetTransformMatrix(Vector3Int.zero) &&
                    shiftedGround.GetTileFlags(painted) == section.transform.Find("GroundLevel0").GetComponent<Tilemap>().GetTileFlags(Vector3Int.zero),
                    $"Cell-Z shifting lost authored tile state: actual color {shiftedGround.GetColor(painted)}, flags {shiftedGround.GetTileFlags(painted)}, matrix {shiftedGround.GetTransformMatrix(painted)}; source color {section.transform.Find("GroundLevel0").GetComponent<Tilemap>().GetColor(Vector3Int.zero)}, flags {section.transform.Find("GroundLevel0").GetComponent<Tilemap>().GetTileFlags(Vector3Int.zero)}, matrix {section.transform.Find("GroundLevel0").GetComponent<Tilemap>().GetTransformMatrix(Vector3Int.zero)}.");
                Require(Vector3.Distance(sectionCopy.Find("SectionLandingAnchor").position, ascend ? upper : lower) < 0.0001f &&
                    sectionCopy.GetComponentsInChildren<RoomSpawnPoint>().Length == 0 && combined.GetComponentsInChildren<Gateway>().Length == 2,
                    "Section alignment or duplicate progression cleanup failed.");
                CheckCombinedGatewayPairs(combined);
                CompositeCollider2D boundary = shiftedCollision.GetComponent<CompositeCollider2D>();
                Require(boundary.enabled && boundary.pathCount > 0 && boundary.OverlapPoint(shiftedCollision.GetCellCenterWorld(new Vector3Int(-baseSide * 5, -baseSide * 5, sectionBaseZ))),
                    "Attached collision lost its aligned composite geometry.");
                TilemapElevationVisibility boundaryVisibility = shiftedCollision.GetComponent<TilemapElevationVisibility>();
                typeof(TilemapElevationVisibility).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(boundaryVisibility, null);
                boundaryVisibility.Refresh(sectionLevel + 10);
                Require(boundary.enabled && boundary.pathCount > 0, "Elevation visibility disabled the composed section's physical boundary.");
                boundaryVisibility.Refresh(sectionLevel);
                Require(sectionCopy.GetComponentInChildren<AStarWalkableMap>().ElevationLevel == sectionLevel &&
                    new SerializedObject(sectionCopy.GetComponentInChildren<EnemySpawnPoint>()).FindProperty("elevationLevel").intValue == sectionLevel &&
                    new SerializedObject(sectionCopy.GetComponentInChildren<TilemapElevationVisibility>()).FindProperty("tilemapLevel").intValue == sectionLevel &&
                    new SerializedObject(sectionCopy.GetComponentInChildren<TilemapElevationSorting>()).FindProperty("tilemapLevel").intValue == sectionLevel &&
                    new SerializedObject(sectionCopy.GetComponentInChildren<LightElevationVisibility>()).FindProperty("lightLevel").intValue == sectionLevel &&
                    new SerializedObject(sectionCopy.GetComponentInChildren<ElevationGroup>()).FindProperty("elevationLevel").intValue == sectionLevel,
                    "Section elevation metadata was not reassigned.");
                AStarStairLink copiedStair = combined.GetComponentInChildren<AStarStairLink>();
                Transform copiedEntry = new SerializedObject(copiedStair).FindProperty("entryAnchor").objectReferenceValue as Transform;
                Require(copiedEntry != entry && copiedEntry.IsChildOf(combined.transform), "The cloned stair still points into the source room.");
                BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                AStarManager manager = managerObject.AddComponent<AStarManager>();
                foreach (AStarWalkableMap map in combined.GetComponentsInChildren<AStarWalkableMap>())
                {
                    typeof(AStarWalkableMap).GetMethod("Awake", flags).Invoke(map, null);
                    manager.RegisterWalkableTilemap(map);
                }
                typeof(AStarStairLink).GetMethod("Awake", flags).Invoke(copiedStair, null);
                manager.RegisterStairLink(copiedStair);
                foreach (AStarDetectionMap map in combined.GetComponentsInChildren<AStarDetectionMap>())
                    manager.RegisterDetectionTilemap(map.GetComponent<Tilemap>());
                Tilemap copiedBaseMap = combined.transform.Find("BaseGeometry").GetComponentInChildren<AStarWalkableMap>().Tilemap;
                Tilemap copiedSectionMap = sectionCopy.GetComponentInChildren<AStarWalkableMap>().Tilemap;
                Vector3 start = copiedBaseMap.GetCellCenterWorld(new Vector3Int(baseSide * 4, baseSide * 4, 0));
                Vector3 target = copiedSectionMap.GetCellCenterWorld(new Vector3Int(-baseSide * 4, -baseSide * 4, sectionBaseZ));
                Vector3 flatTarget = target;
                flatTarget.z = 0f;
                Require(manager.GetWalkableTilemapAtPosition(flatTarget, sectionLevel) == copiedSectionMap,
                    "Shifted navigation required the 2D actor to use the Tilemap's world Z.");
                List<Vector3> forward = manager.FindPath(start, target, baseLevel, sectionLevel);
                List<Vector3> reverse = manager.FindPath(target, start, sectionLevel, baseLevel);
                Require(forward != null && reverse != null && Vector3.Distance(forward.Last(), target) < 0.0001f &&
                    Vector3.Distance(reverse.Last(), start) < 0.0001f, "Original authored stair A* cannot traverse the composition both ways.");

                GameObject firstPrefab = ProceduralRoomComposer.SaveNewPrefab(combined, folder + "/Combined.prefab");
                string firstPath = AssetDatabase.GetAssetPath(firstPrefab);
                byte[] before = File.ReadAllBytes(firstPath);
                GameObject secondPrefab = ProceduralRoomComposer.SaveNewPrefab(combined, folder + "/Combined.prefab");
                Require(firstPath != AssetDatabase.GetAssetPath(secondPrefab) && before.SequenceEqual(File.ReadAllBytes(firstPath)), "Composition bake overwrote a prefab.");
                GameObject fromPrefab = Object.Instantiate(firstPrefab);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(fromPrefab, scene);
                try { ProceduralRoomComposer.Validate(fromPrefab); }
                finally { Object.DestroyImmediate(fromPrefab); }

                GameObject basePrefab = PrefabUtility.SaveAsPrefabAsset(baseRoom, folder + "/ComposerBase" + baseLevel + ".prefab");
                GameObject sectionPrefab = PrefabUtility.SaveAsPrefabAsset(section, folder + "/ComposerSection" + sectionLevel + ".prefab");
                AStarStairLink prefabStair = basePrefab.GetComponentInChildren<AStarStairLink>(true);
                GameObject prefabComposition = ProceduralRoomComposer.Combine(basePrefab, sectionPrefab, prefabStair,
                    sectionPrefab.GetComponentInChildren<RoomSpawnPoint>(true).transform,
                    basePrefab.transform.Find("LowerTrigger").GetComponent<RampElevationTrigger>(),
                    basePrefab.transform.Find("UpperTrigger").GetComponent<RampElevationTrigger>(), ascend, false, sectionLevel, sectionBaseZ);
                try { ProceduralRoomComposer.Validate(prefabComposition); }
                finally { Object.DestroyImmediate(prefabComposition); }

                GameObject gridParent = new GameObject("Inherited Grid Source");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(gridParent, scene);
                Grid inherited = gridParent.AddComponent<Grid>();
                inherited.cellLayout = GridLayout.CellLayout.IsometricZAsY;
                inherited.cellSwizzle = GridLayout.CellSwizzle.XYZ;
                inherited.cellSize = ProceduralRoomBuilder.CellSize;
                section.transform.SetParent(gridParent.transform, true);
                Object.DestroyImmediate(section.GetComponent<Grid>());
                try
                {
                    GameObject inheritedComposition = ProceduralRoomComposer.Combine(baseRoom, section, stair, landing, lowerTrigger, upperTrigger, ascend, false, sectionLevel, sectionBaseZ);
                    try
                    {
                        Require(inheritedComposition.transform.Find("ElevationSection_" + sectionLevel).GetComponent<Grid>() != null,
                            "Inherited-Grid source did not receive a self-contained Grid in the output.");
                        ProceduralRoomComposer.Validate(inheritedComposition);
                    }
                    finally { Object.DestroyImmediate(inheritedComposition); }
                }
                finally
                {
                    section.transform.SetParent(null, true);
                    Grid restored = section.AddComponent<Grid>();
                    restored.cellLayout = inherited.cellLayout;
                    restored.cellSwizzle = inherited.cellSwizzle;
                    restored.cellSize = inherited.cellSize;
                    Object.DestroyImmediate(gridParent);
                }

                Vector3 landingOffset = (baseMap.GetCellCenterWorld(Vector3Int.right) - baseMap.GetCellCenterWorld(Vector3Int.zero)) * 0.45f;
                BoxCollider2D lowerRail = FixtureChild(baseRoom, "LowerLandingSideRail").AddComponent<BoxCollider2D>();
                lowerRail.transform.position = lower + landingOffset;
                lowerRail.size = new Vector2(0.02f, 0.02f);
                BoxCollider2D upperRail = FixtureChild(baseRoom, "UpperLandingSideRail").AddComponent<BoxCollider2D>();
                upperRail.transform.position = upper + landingOffset;
                upperRail.size = new Vector2(0.02f, 0.02f);
                List<string> clearanceWarnings = new List<string>();
                Application.LogCallback captureWarnings = (message, stackTrace, type) =>
                {
                    if (type == LogType.Warning && message.StartsWith("Stair landing at elevation ", StringComparison.Ordinal))
                        clearanceWarnings.Add(message);
                };
                GameObject railComposition = null;
                Application.logMessageReceived += captureWarnings;
                try
                {
                    railComposition = ProceduralRoomComposer.Combine(baseRoom, section, stair, landing, lowerTrigger, upperTrigger, ascend, false, sectionLevel, sectionBaseZ);
                    Require(clearanceWarnings.Any(message => message.Contains(lowerRail.name)) &&
                        clearanceWarnings.Any(message => message.Contains(upperRail.name)),
                        "Landing side-rail overlaps did not produce named, non-blocking clearance warnings.");
                    ProceduralRoomComposer.Validate(railComposition);
                    Require(railComposition.GetComponentsInChildren<BoxCollider2D>().Count(collider =>
                        collider.name.EndsWith("LandingSideRail", StringComparison.Ordinal) && !collider.isTrigger) == 2,
                        "Composition removed or disabled solid stair-side rails.");
                }
                finally
                {
                    Application.logMessageReceived -= captureWarnings;
                    if (railComposition != null) Object.DestroyImmediate(railComposition);
                    Object.DestroyImmediate(lowerRail.gameObject);
                    Object.DestroyImmediate(upperRail.gameObject);
                }

                BoxCollider2D blocked = FixtureChild(baseRoom, "BlockedRamp").AddComponent<BoxCollider2D>();
                blocked.transform.position = (lower + upper) * 0.5f;
                blocked.size = new Vector2(0.1f, 0.1f);
                int rootCount = scene.rootCount;
                List<string> centerWarnings = new List<string>();
                Application.LogCallback captureCenterWarnings = (message, stackTrace, type) =>
                {
                    if (type == LogType.Warning && message.StartsWith("Stair centerline intersects ", StringComparison.Ordinal))
                        centerWarnings.Add(message);
                };
                Application.logMessageReceived += captureCenterWarnings;
                GameObject advisoryRoom = null;
                try
                {
                    advisoryRoom = ProceduralRoomComposer.Combine(baseRoom, section, stair, landing, lowerTrigger, upperTrigger, ascend, false, sectionLevel, sectionBaseZ);
                    Require(centerWarnings.Any(message => message.Contains("BlockedRamp")), "Centerline obstruction was not reported as a named advisory warning.");
                    ProceduralRoomComposer.SaveNewPrefab(advisoryRoom, folder + "/AdvisoryCollision.prefab");
                }
                finally
                {
                    Application.logMessageReceived -= captureCenterWarnings;
                    if (advisoryRoom != null) Object.DestroyImmediate(advisoryRoom);
                }
                Require(scene.rootCount == rootCount, "Advisory composition cleanup leaked a scene root.");
                Object.DestroyImmediate(blocked.gameObject);
                HashSet<int> forwardLevels = new HashSet<int>();
                HashSet<int> backwardLevels = new HashSet<int>();
                HashSet<int> floorLevels = new HashSet<int>();
                foreach (GameObject source in new[] { baseRoom, section })
                {
                    Tilemap floorTiles = FixtureChild(source, "FloorTrapdoorTiles").AddComponent<Tilemap>();
                    floorTiles.SetTile(Vector3Int.zero, profile.navigationTile);
                    Gateway floor = FixtureChild(source, "FloorGateway").AddComponent<Gateway>();
                    SerializedObject floorSettings = new SerializedObject(floor);
                    floorSettings.FindProperty("flow").enumValueIndex = (int)GatewayFlow.Floor;
                    floorSettings.ApplyModifiedPropertiesWithoutUndo();
                    GatewayFloorVisibility floorVisibility = floor.gameObject.AddComponent<GatewayFloorVisibility>();
                    SerializedObject floorVisualSettings = new SerializedObject(floorVisibility);
                    floorVisualSettings.FindProperty("tilemap").objectReferenceValue = floorTiles;
                    floorVisualSettings.ApplyModifiedPropertiesWithoutUndo();
                }
                for (int seed = 0; seed < 12; seed++)
                {
                    GameObject seeded = ProceduralRoomComposer.Combine(baseRoom, section, stair, landing, lowerTrigger, upperTrigger, ascend, false,
                        sectionLevel, sectionBaseZ, seed);
                    try
                    {
                        CheckCombinedGatewayPairs(seeded);
                        forwardLevels.Add(seeded.GetComponentsInChildren<Gateway>().Single(gateway => gateway.Flow == GatewayFlow.Forward).ElevationLevel);
                        backwardLevels.Add(seeded.GetComponentsInChildren<Gateway>().Single(gateway => gateway.Flow == GatewayFlow.Backward).ElevationLevel);
                        floorLevels.Add(seeded.GetComponentsInChildren<Gateway>().Single(gateway => gateway.Flow == GatewayFlow.Floor).ElevationLevel);
                        Require(seeded.GetComponentsInChildren<Tilemap>().Count(map => map.name == "FloorTrapdoorTiles") == 1,
                            "Composition left duplicate trapdoor artwork behind.");
                    }
                    finally { Object.DestroyImmediate(seeded); }
                }
                Require(forwardLevels.SetEquals(new[] { baseLevel, sectionLevel }) && backwardLevels.SetEquals(new[] { baseLevel, sectionLevel }) &&
                    floorLevels.SetEquals(new[] { baseLevel, sectionLevel }),
                    "Gateway randomization only used the base section.");
                RequireThrows(() => ProceduralRoomComposer.Combine(baseRoom, section, stair, landing, lowerTrigger, upperTrigger, ascend, false, baseLevel, sectionBaseZ),
                    "Composer allowed both sections to use the same gameplay level.");
                RequireThrows(() => ProceduralRoomComposer.Combine(baseRoom, baseRoom, stair, landing, lowerTrigger, upperTrigger, ascend, false), "Composer accepted the same room twice.");
                if (ascend)
                    RequireThrows(() => ProceduralRoomComposer.Combine(baseRoom, section, stair, landing, lowerTrigger, upperTrigger, false, false), "Composer allowed descent below elevation zero.");
                serializedStair.Update();
                serializedStair.FindProperty("entryAnchor").objectReferenceValue = null;
                serializedStair.FindProperty("exitAnchor").objectReferenceValue = null;
                serializedStair.ApplyModifiedPropertiesWithoutUndo();
                Physics2D.SyncTransforms();
                rampObject.transform.position += ascend ? lower - stair.EntryPosition : upper - stair.ExitPosition;
                Physics2D.SyncTransforms();
                lowerTrigger.transform.position = stair.EntryPosition;
                upperTrigger.transform.position = stair.ExitPosition;
                Vector3 automaticMiddle = (stair.EntryPosition + stair.ExitPosition) * 0.5f;
                detection.transform.position += automaticMiddle - detection.GetCellCenterWorld(Vector3Int.zero);
                GameObject automatic = ProceduralRoomComposer.Combine(baseRoom, section, stair, landing, lowerTrigger, upperTrigger, ascend, false, sectionLevel, sectionBaseZ);
                try
                {
                    AStarStairLink automaticStair = automatic.GetComponentInChildren<AStarStairLink>();
                    Require(Vector3.Distance(automaticStair.EntryPosition, stair.EntryPosition) < 0.0001f &&
                        Vector3.Distance(automaticStair.ExitPosition, stair.ExitPosition) < 0.0001f,
                        "Composer automatic endpoints diverged from the runtime ramp-collider fallback.");
                    Require(new SerializedObject(stair).FindProperty("entryAnchor").objectReferenceValue == null &&
                        new SerializedObject(stair).FindProperty("exitAnchor").objectReferenceValue == null,
                        "Automatic endpoints mutated the source stair.");
                }
                finally { Object.DestroyImmediate(automatic); }
            }
            finally
            {
                if (combined != null) Object.DestroyImmediate(combined);
                Object.DestroyImmediate(baseRoom);
                Object.DestroyImmediate(section);
                Object.DestroyImmediate(managerObject);
            }
        }
    }

    private static GameObject CompositionFixture(string name, ProceduralRoomProfile profile, UnityEngine.SceneManagement.Scene scene, int side, int level)
    {
        GameObject room = new GameObject(name);
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(room, scene);
        room.AddComponent<RoomInstance>();
        Grid grid = room.AddComponent<Grid>();
        grid.cellLayout = GridLayout.CellLayout.IsometricZAsY;
        grid.cellSwizzle = GridLayout.CellSwizzle.XYZ;
        grid.cellSize = ProceduralRoomBuilder.CellSize;
        GameObject mapObject = FixtureChild(room, "Surface");
        mapObject.transform.localPosition = Vector3.up * 0.16f;
        Tilemap map = mapObject.AddComponent<Tilemap>();
        map.tileAnchor = new Vector3(0.5f, 0.5f, 0f);
        for (int cellX = 0; cellX <= 4; cellX++)
        {
            for (int cellY = 0; cellY <= 4; cellY++)
                map.SetTile(new Vector3Int(cellX * side, cellY * side, 0), profile.navigationTile);
        }
        AStarWalkableMap walkable = mapObject.AddComponent<AStarWalkableMap>();
        SerializedObject serialized = new SerializedObject(walkable);
        serialized.FindProperty("elevationLevel").intValue = level;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        mapObject.AddComponent<AStarWalkableGizmo>();
        mapObject.AddComponent<TilemapElevationVisibility>();
        mapObject.AddComponent<TilemapElevationSorting>();
        mapObject.AddComponent<ElevationGroup>();
        Tilemap ground = FixtureChild(room, "GroundLevel0").AddComponent<Tilemap>();
        ground.tileAnchor = map.tileAnchor;
        ground.SetTile(Vector3Int.zero, profile.navigationTile);
        ground.SetTileFlags(Vector3Int.zero, TileFlags.None);
        ground.SetColor(Vector3Int.zero, new Color(0.4f, 0.7f, 0.8f, 0.6f));
        ground.SetTransformMatrix(Vector3Int.zero, Matrix4x4.Scale(new Vector3(0.8f, 0.9f, 1f)));
        ground.SetTileFlags(Vector3Int.zero, TileFlags.LockColor | TileFlags.LockTransform);
        Tilemap walls = FixtureChild(room, "WallVisuals0").AddComponent<Tilemap>();
        walls.tileAnchor = map.tileAnchor;
        for (int height = 0; height < 3; height++)
            walls.SetTile(new Vector3Int(side * 5, side * 5, height), profile.navigationTile);
        Tilemap collision = FixtureChild(room, "ColliderLevel0").AddComponent<Tilemap>();
        collision.tileAnchor = map.tileAnchor;
        collision.transform.localPosition = map.transform.localPosition;
        collision.SetTile(new Vector3Int(side * 5, side * 5, 0), profile.collisionTile);
        collision.gameObject.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
        collision.gameObject.AddComponent<CompositeCollider2D>().geometryType = CompositeCollider2D.GeometryType.Polygons;
        ProceduralRoomBuilder.RebuildBoundaryCollision(collision);
        TilemapElevationVisibility collisionVisibility = collision.gameObject.AddComponent<TilemapElevationVisibility>();
        SerializedObject visibilitySettings = new SerializedObject(collisionVisibility);
        visibilitySettings.FindProperty("tilemapLevel").intValue = level;
        visibilitySettings.FindProperty("smoothFade").boolValue = false;
        visibilitySettings.ApplyModifiedPropertiesWithoutUndo();
        GameObject light = FixtureChild(room, "Light");
        light.AddComponent<Light2D>();
        light.AddComponent<LightElevationVisibility>();
        GameObject spawn = FixtureChild(room, "PlayerSpawn");
        spawn.transform.position = map.GetCellCenterWorld(Vector3Int.zero);
        RoomSpawnPoint playerSpawn = spawn.AddComponent<RoomSpawnPoint>();
        serialized = new SerializedObject(playerSpawn);
        serialized.FindProperty("elevationLevel").intValue = level;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        GameObject enemy = FixtureChild(room, "EnemySpawn");
        enemy.transform.position = map.GetCellCenterWorld(new Vector3Int(2 * side, 2 * side, 0));
        EnemySpawnPoint enemySpawn = enemy.AddComponent<EnemySpawnPoint>();
        serialized = new SerializedObject(enemySpawn);
        serialized.FindProperty("elevationLevel").intValue = level;
        serialized.FindProperty("spawnOnStart").boolValue = false;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        for (int index = 0; index < 2; index++)
        {
            Gateway gateway = FixtureChild(room, "Gateway" + index).AddComponent<Gateway>();
            GatewayFlow flow = index == 0 ? GatewayFlow.Forward : GatewayFlow.Backward;
            GatewayDirection direction = index == 0 ? GatewayDirection.NorthEast : GatewayDirection.SouthWest;
            serialized = new SerializedObject(gateway);
            serialized.FindProperty("flow").enumValueIndex = (int)flow;
            serialized.FindProperty("direction").enumValueIndex = (int)direction;
            serialized.FindProperty("elevationLevel").intValue = level;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            GameObject destination = FixtureChild(room, "Destination" + index);
            destination.transform.position = map.GetCellCenterWorld(new Vector3Int(3 * side, 3 * side, 0));
            ElevationDestination marker = destination.AddComponent<ElevationDestination>();
            serialized = new SerializedObject(marker);
            serialized.FindProperty("flow").enumValueIndex = (int)(flow == GatewayFlow.Forward ? GatewayFlow.Backward : GatewayFlow.Forward);
            serialized.FindProperty("direction").enumValueIndex = (int)direction;
            serialized.FindProperty("elevationLevel").intValue = level;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        return room;
    }

    private static GameObject FixtureChild(GameObject root, string name)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(root.transform, false);
        return child;
    }

    private static RampElevationTrigger FixtureElevationTrigger(GameObject room, string name, Vector3 position)
    {
        GameObject child = FixtureChild(room, name);
        child.transform.position = position;
        BoxCollider2D collider = child.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(0.1f, 0.1f);
        return child.AddComponent<RampElevationTrigger>();
    }

    private static void CheckSurfaceBoundary(Tilemap map, Tilemap collision)
    {
        CompositeCollider2D collider = collision.GetComponent<CompositeCollider2D>();
        foreach (Vector3Int cell in ProceduralRoomBuilder.GetCells(map))
        {
            Vector2 center = map.GetCellCenterWorld(cell);
            Require(!collider.OverlapPoint(center), "Boundary collision covers a walkable surface center.");
            foreach (Vector3Int offset in new[] { Vector3Int.right, Vector3Int.up, Vector3Int.left, Vector3Int.down })
            {
                if (map.HasTile(cell + offset))
                    continue;
                Vector2 outside = map.GetCellCenterWorld(cell + offset);
                Vector2 edgeProbe = Vector2.Lerp(center, outside, 0.6f);
                Require(collider.OverlapPoint(edgeProbe), $"Collision leaves an exposed cube-top edge open: cell {cell}, direction {offset}, center {center.ToString("F5")}, outside {outside.ToString("F5")}, probe {edgeProbe.ToString("F5")}, closest {collider.ClosestPoint(edgeProbe).ToString("F5")}, map anchor {map.tileAnchor}, collision anchor {collision.tileAnchor}.");
                Require(!collider.OverlapPoint(Vector2.Lerp(center, outside, 0.4f)), "Collision cuts into the walkable cube-top interior.");
            }
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private static void RequireThrows(Action action, string message)
    {
        try { action(); }
        catch (ArgumentException) { return; }
        catch (InvalidOperationException) { return; }
        throw new InvalidOperationException(message);
    }
}