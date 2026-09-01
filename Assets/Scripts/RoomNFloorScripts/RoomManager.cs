using System.Collections.Generic;
using UnityEngine;

public class RoomManager : MonoBehaviour
{
    [Header("Room Prefabs")]
    [SerializeField] private List<GameObject> smallRoomPrefabs =
        new List<GameObject>();

    [SerializeField] private List<GameObject> mediumRoomPrefabs =
        new List<GameObject>();

    [SerializeField] private List<GameObject> largeRoomPrefabs =
        new List<GameObject>();

    [SerializeField] private List<GameObject> xlRoomPrefabs =
        new List<GameObject>();

    [Header("Room Spacing")]
    [SerializeField] private float roomSpacingX = 30f;

    [Header("Room Progression")]
    [SerializeField] private bool lockNextRoomUntilCleared = true;

    [Header("Special Gateways")]
    [Range(0f, 1f)]
    [SerializeField] private float secretGatewayChance = 0.35f;
    [SerializeField] private FloorManager floorManager;

    [Header("Secret Rooms")]
    [Tooltip("Prefabs used for rooms reached through Secret gateways.")]
    [SerializeField] private List<GameObject> secretRoomPrefabs =
        new List<GameObject>();
    [Tooltip("World position offset used when spawning the secret room.")]
    [SerializeField] private Vector3 secretRoomSpawnOffset =
        new Vector3(0f, -30f, 0f);

    private int currentRoomNumber = 1;

    public int CurrentRoomNumber => currentRoomNumber;
    public bool LockNextRoomUntilCleared => lockNextRoomUntilCleared;

    private readonly List<RoomInstance> generatedRooms =
        new List<RoomInstance>();

    private readonly List<RoomInstance> generatedSecretRooms =
        new List<RoomInstance>();

    public IReadOnlyList<RoomInstance> GeneratedRooms =>
        generatedRooms;
    private readonly HashSet<int> clearedRooms =
    new HashSet<int>();

    private readonly HashSet<Gateway> validFloorGateways =
        new HashSet<Gateway>();

    private Gateway validSecretGateway;
    private RoomInstance secretUnlockRoom;


    // -------------------------------------------------
    // ROOM CLEAR TRACKING
    // -------------------------------------------------

    // Tracks every EnemySpawnPoint belonging to each room.
    private readonly Dictionary<
        RoomInstance,
        List<EnemySpawnPoint>
    > roomSpawnPoints =
        new Dictionary<
            RoomInstance,
            List<EnemySpawnPoint>
        >();

    // Tracks which spawn points have reported themselves cleared.
    private readonly Dictionary<
        RoomInstance,
        HashSet<EnemySpawnPoint>
    > clearedSpawnPoints =
        new Dictionary<
            RoomInstance,
            HashSet<EnemySpawnPoint>
        >();


    private void Start()
    {

    }


    private List<GameObject> GetAllowedRoomPrefabs(
        FloorConfiguration configuration)
    {
        List<GameObject> allowedPrefabs =
            new List<GameObject>();

        if (configuration.allowSmall)
        {
            allowedPrefabs.AddRange(
                smallRoomPrefabs
            );
        }

        if (configuration.allowMedium)
        {
            allowedPrefabs.AddRange(
                mediumRoomPrefabs
            );
        }

        if (configuration.allowLarge)
        {
            allowedPrefabs.AddRange(
                largeRoomPrefabs
            );
        }

        if (configuration.allowXL)
        {
            allowedPrefabs.AddRange(
                xlRoomPrefabs
            );
        }

        return allowedPrefabs;
    }


    public void GenerateFloor(
        FloorConfiguration configuration)
    {
        if (configuration == null)
        {
            Debug.LogError(
                "RoomManager cannot generate a floor: " +
                "FloorConfiguration is null."
            );

            return;
        }

        ClearGeneratedRooms();

        List<GameObject> prefabPool =
            GetAllowedRoomPrefabs(configuration);

        if (prefabPool.Count == 0)
        {
            Debug.LogError(
                "No room prefabs are available for " +
                "the current FloorConfiguration."
            );

            return;
        }

        int minimumRooms =
            Mathf.Max(
                1,
                configuration.minimumRooms
            );

        int maximumRooms =
            Mathf.Max(
                1,
                configuration.maximumRooms
            );

        if (minimumRooms > maximumRooms)
        {
            maximumRooms = minimumRooms;
        }

        int roomCount = Random.Range(
            minimumRooms,
            maximumRooms + 1
        );

        Debug.Log(
            $"Generating floor with {roomCount} rooms."
        );


        // ---------------------------------------------
        // ROOM 1
        // ---------------------------------------------

        RoomInstance firstRoom =
            GenerateFirstRoom(prefabPool);

        if (firstRoom == null)
        {
            Debug.LogError(
                "Failed to generate Room 1."
            );

            return;
        }

        Gateway firstBackwardGateway =
            FindGateway(
                firstRoom,
                GatewayFlow.Backward
            );

        if (firstBackwardGateway != null)
        {
            GatewayVisibility visibility =
                firstBackwardGateway.GetComponent<GatewayVisibility>();

            if (visibility != null)
            {
                visibility.Lock();
            }
        }


        // ---------------------------------------------
        // ROOMS 2+
        // ---------------------------------------------

        for (
            int roomNumber = 2;
            roomNumber <= roomCount;
            roomNumber++
        )
        {
            RoomInstance previousRoom =
                generatedRooms[
                    generatedRooms.Count - 1
                ];

            Gateway previousForwardGateway =
                FindGateway(
                    previousRoom,
                    GatewayFlow.Forward
                );

            if (previousForwardGateway == null)
            {
                Debug.LogError(
                    $"Room {previousRoom.RoomNumber} " +
                    "does not have a Forward Gateway."
                );

                break;
            }

            GameObject compatiblePrefab =
                FindCompatibleRoomPrefab(
                    previousForwardGateway,
                    prefabPool
                );

            if (compatiblePrefab == null)
            {
                Debug.LogError(
                    $"Could not find a compatible room prefab " +
                    $"for Room {previousRoom.RoomNumber}'s " +
                    $"Forward Gateway."
                );

                break;
            }

            RoomInstance newRoom =
                GenerateRoom(
                    roomNumber,
                    compatiblePrefab
                );

            if (newRoom == null)
            {
                break;
            }

            ConnectRooms(
                previousRoom,
                newRoom
            );
        }

        ConfigureSpecialGateways();

        // ---------------------------------------------
        // ROOM PROGRESSION
        // ---------------------------------------------

        currentRoomNumber = 1;

        if (lockNextRoomUntilCleared)
        {
            // Lock every Forward Gateway first.
            LockAllRoomGateways();

            // Room 1 is immediately accessible only if
            // it has no enemy spawn points.
            bool firstRoomHasEnemies =
                roomSpawnPoints.ContainsKey(firstRoom) &&
                roomSpawnPoints[firstRoom].Count > 0;

            SetRoomGatewayState(
                firstRoom,
                !firstRoomHasEnemies
            );
        }

        // ---------------------------------------------
        // FINAL ROOM
        // ---------------------------------------------

        if (generatedRooms.Count > 0)
        {
            RoomInstance finalRoom =
                generatedRooms[
                    generatedRooms.Count - 1
                ];

            Gateway finalForwardGateway =
                FindGateway(
                    finalRoom,
                    GatewayFlow.Forward
                );

            if (finalForwardGateway != null)
            {
                GatewayVisibility visibility =
                    finalForwardGateway.GetComponent<GatewayVisibility>();

                if (visibility != null)
                {
                    visibility.Lock();
                }
            }

            Debug.Log(
                $"Final room is Room " +
                $"{finalRoom.RoomNumber}."
            );
        }

        // ---------------------------------------------
        // PLAYER
        // ---------------------------------------------

        SpawnPlayerAtFirstRoom();
    }


    private RoomInstance GenerateFirstRoom(
        List<GameObject> prefabPool)
    {
        GameObject selectedPrefab =
            prefabPool[
                Random.Range(
                    0,
                    prefabPool.Count
                )
            ];

        return GenerateRoom(
            1,
            selectedPrefab
        );
    }


    private RoomInstance GenerateRoom(
        int roomNumber,
        GameObject roomPrefab)
    {
        Vector3 spawnPosition =
            new Vector3(
                roomSpacingX *
                (roomNumber - 1),
                0f,
                0f
            );

        GameObject roomObject =
            Instantiate(
                roomPrefab,
                spawnPosition,
                Quaternion.identity,
                transform
            );

        RoomInstance roomInstance =
            roomObject.GetComponent<RoomInstance>();

        if (roomInstance == null)
        {
            Debug.LogError(
                $"Room prefab '{roomPrefab.name}' " +
                "does not have a RoomInstance component."
            );

            Destroy(roomObject);

            return null;
        }

        roomInstance.Initialize(roomNumber);

        generatedRooms.Add(roomInstance);

        // ---------------------------------------------
        // REGISTER ENEMY SPAWN POINTS
        // ---------------------------------------------

        RegisterRoomEnemySpawnPoints(
            roomInstance
        );

        Debug.Log(
            $"Generated Room {roomNumber}: " +
            $"{roomPrefab.name} " +
            $"at X = {spawnPosition.x}"
        );

        return roomInstance;
    }


    // -------------------------------------------------
    // ENEMY / ROOM CLEAR SYSTEM
    // -------------------------------------------------

    private void RegisterRoomEnemySpawnPoints(
    RoomInstance room)
    {
        if (room == null)
            return;

        EnemySpawnPoint[] spawnPoints =
            room.GetComponentsInChildren<EnemySpawnPoint>(true);

        List<EnemySpawnPoint> roomPoints =
            new List<EnemySpawnPoint>(spawnPoints);

        HashSet<EnemySpawnPoint> clearedPoints =
            new HashSet<EnemySpawnPoint>();

        roomSpawnPoints[room] = roomPoints;
        clearedSpawnPoints[room] = clearedPoints;


        Debug.Log(
            $"Room {room.RoomNumber} has " +
            $"{roomPoints.Count} EnemySpawnPoint(s)."
        );


        // -------------------------------------------------
        // NO ENEMIES
        // -------------------------------------------------

        if (roomPoints.Count == 0)
        {
            Debug.Log(
                $"Room {room.RoomNumber} contains no " +
                "EnemySpawnPoints."
            );

            if (room.RoomNumber == 1)
            {
                SetRoomCleared(
                    room.RoomNumber
                );
            }
            else
            {
                Debug.Log(
                    $"Room {room.RoomNumber} will be cleared " +
                    "when the player enters it."
                );
            }

            return;
        }


        // -------------------------------------------------
        // ROOM HAS ENEMIES
        // -------------------------------------------------

        // Lock this room's Forward Gateway immediately.
        SetRoomGatewayState(
            room,
            false
        );


        // -------------------------------------------------
        // SUBSCRIBE TO SPAWN POINTS
        // -------------------------------------------------

        foreach (
            EnemySpawnPoint spawnPoint
            in roomPoints
        )
        {
            if (spawnPoint == null)
                continue;

            EnemySpawnPoint capturedSpawnPoint =
                spawnPoint;

            spawnPoint.OnWaveCleared +=
                () => HandleSpawnPointCleared(
                    room,
                    capturedSpawnPoint
                );
        }
    }


    private void HandleSpawnPointCleared(
        RoomInstance room,
        EnemySpawnPoint spawnPoint)
    {
        if (room == null || spawnPoint == null)
            return;

        if (!roomSpawnPoints.ContainsKey(room))
            return;

        HashSet<EnemySpawnPoint> clearedPoints =
            clearedSpawnPoints[room];

        // Prevent duplicate notifications.
        if (!clearedPoints.Add(spawnPoint))
            return;

        Debug.Log(
            $"Room {room.RoomNumber}: " +
            $"EnemySpawnPoint '{spawnPoint.name}' cleared. " +
            $"{clearedPoints.Count}/" +
            $"{roomSpawnPoints[room].Count} cleared."
        );


        // ---------------------------------------------
        // CHECK WHETHER THE WHOLE ROOM IS CLEARED
        // ---------------------------------------------

        if (
            clearedPoints.Count >=
            roomSpawnPoints[room].Count
        )
        {
            HandleRoomCleared(room);
        }
    }


    private void HandleRoomCleared(
        RoomInstance room)
    {
        if (room == null)
            return;

        Debug.Log(
            $"Room {room.RoomNumber} has been cleared."
        );

        SetRoomCleared(
            room.RoomNumber
        );

        if (
            validSecretGateway == null ||
            secretUnlockRoom != room
        )
            return;

        SetSecretGatewayVisible(validSecretGateway, true);
        HeadsUpTextManager.Show("The floor shifts. A hidden room has opened.");
        Debug.Log(
            $"Secret Gateway '{validSecretGateway.name}' opened after " +
            $"clearing Room {room.RoomNumber}."
        );

        validSecretGateway = null;
        secretUnlockRoom = null;
    }

    public bool HandleGatewayEntered(Gateway gateway)
    {
        if (gateway == null)
            return false;

        MarkDestinationRoomVisited(gateway);

        if (gateway.Flow != GatewayFlow.Floor)
            return false;

        if (!validFloorGateways.Contains(gateway))
            return true;

        if (floorManager == null)
            floorManager = FindFirstObjectByType<FloorManager>();

        if (floorManager == null)
        {
            Debug.LogError("No FloorManager exists for the Floor Gateway.");
            return true;
        }

        floorManager.EnterNextFloor();
        return true;
    }

    private void MarkDestinationRoomVisited(Gateway gateway)
    {
        if (gateway == null || gateway.Destination == null)
            return;

        RoomInstance destinationRoom =
            gateway.Destination.GetComponentInParent<RoomInstance>();

        if (destinationRoom == null ||
            !generatedRooms.Contains(destinationRoom))
            return;

        if (!roomSpawnPoints.TryGetValue(
                destinationRoom,
                out List<EnemySpawnPoint> spawnPoints))
            return;

        if (spawnPoints.Count > 0)
            return;

        Debug.Log(
            $"Room {destinationRoom.RoomNumber} was entered and " +
            "contains no EnemySpawnPoints. Clearing room."
        );

        HandleRoomCleared(destinationRoom);
    }

    private void ConfigureSpecialGateways()
    {
        List<Gateway> floorGateways = new List<Gateway>();
        List<Gateway> secretGateways = new List<Gateway>();

        foreach (RoomInstance room in generatedRooms)
        {
            Gateway[] gateways = room.GetComponentsInChildren<Gateway>(true);

            foreach (Gateway gateway in gateways)
            {
                if (gateway.Flow == GatewayFlow.Floor)
                    floorGateways.Add(gateway);
                else if (
                    gateway.Flow == GatewayFlow.SecretForward ||
                    gateway.Flow == GatewayFlow.SecretBackward
                )
                    secretGateways.Add(gateway);
            }
        }

        validFloorGateways.Clear();

        foreach (Gateway gateway in floorGateways)
            SetGatewayVisible(gateway, false);

        if (floorGateways.Count > 0)
        {
            Gateway selectedGateway =
                floorGateways[Random.Range(0, floorGateways.Count)];

            validFloorGateways.Add(selectedGateway);

            if (currentRoomNumber > generatedRooms.Count)
                SetGatewayVisible(selectedGateway, true);
        }

        validSecretGateway = null;
        secretUnlockRoom = null;

        List<Gateway> eligibleSecretGateways =
            new List<Gateway>();

        foreach (Gateway gateway in secretGateways)
        {
            SetSecretGatewayVisible(gateway, false);
            eligibleSecretGateways.Add(gateway);
        }

        if (
            eligibleSecretGateways.Count == 0 ||
            secretRoomPrefabs.Count == 0 ||
            Random.value > secretGatewayChance
        )
        {
            if (eligibleSecretGateways.Count == 0)
                Debug.LogWarning(
                    "No eligible secret gateways were found. Check that " +
                    "secret gateways have matching destinations."
                );
            else if (secretRoomPrefabs.Count == 0)
                Debug.LogWarning(
                    "Secret gateway chance succeeded, but no secret room " +
                    "prefabs are assigned."
                );

            return;
        }

        List<Gateway> gatewaysWithSecretRooms =
            new List<Gateway>();

        foreach (Gateway gateway in eligibleSecretGateways)
        {
            if (FindCompatibleSecretRoomPrefab(gateway) != null &&
                GetRoomsAtOrAfter(gateway).Count > 0)
                gatewaysWithSecretRooms.Add(gateway);
        }

        if (gatewaysWithSecretRooms.Count == 0)
        {
            Debug.LogWarning(
                "No secret room prefab is compatible with the " +
                "available Secret gateways, or no selected gateway has a " +
                "regular room at or after it."
            );

            return;
        }

        validSecretGateway =
            gatewaysWithSecretRooms[
                Random.Range(0, gatewaysWithSecretRooms.Count)
            ];

        GameObject secretRoomPrefab =
            FindCompatibleSecretRoomPrefab(validSecretGateway);

        RoomInstance secretRoomInstance =
            GenerateSecretRoom(secretRoomPrefab);

        if (secretRoomInstance == null)
        {
            validSecretGateway = null;
            return;
        }

        ElevationDestination secretDestination =
            FindDestination(
                secretRoomInstance,
                GetMatchingSecretFlow(validSecretGateway.Flow),
                validSecretGateway.Direction.Opposite()
            );

        if (secretDestination == null)
        {
            Debug.LogWarning(
                $"Secret room '{secretRoomPrefab.name}' does not have " +
                "a compatible destination."
            );

            validSecretGateway = null;
            return;
        }

        validSecretGateway.SetDestination(secretDestination.transform);

        List<RoomInstance> unlockRooms =
            GetRoomsAtOrAfter(validSecretGateway);

        if (unlockRooms.Count == 0)
        {
            validSecretGateway = null;
            return;
        }

        Gateway returnGateway = FindGateway(
            secretRoomInstance,
            GetOppositeSecretFlow(validSecretGateway.Flow)
        );

        if (returnGateway == null)
        {
            Debug.LogWarning(
                $"Secret room '{secretRoomPrefab.name}' does not have " +
                "a return Secret gateway."
            );

            validSecretGateway = null;
            return;
        }

        RoomInstance entranceRoom =
            validSecretGateway.GetComponentInParent<RoomInstance>();

        ElevationDestination returnDestination =
            FindSpecialDestination(
                returnGateway,
                entranceRoom,
                GetMatchingSecretFlow(returnGateway.Flow)
            );

        if (returnDestination == null)
        {
            Debug.LogWarning(
                $"Secret gateway '{returnGateway.name}' does not have " +
                "a compatible return destination."
            );

            validSecretGateway = null;
            return;
        }

        returnGateway.SetDestination(returnDestination.transform);

        secretUnlockRoom =
            unlockRooms[Random.Range(0, unlockRooms.Count)];

        if (clearedRooms.Contains(secretUnlockRoom.RoomNumber))
        {
            SetSecretGatewayVisible(validSecretGateway, true);
            validSecretGateway = null;
            secretUnlockRoom = null;
        }
    }

    private List<RoomInstance> GetRoomsAtOrAfter(Gateway gateway)
    {
        List<RoomInstance> roomsAtOrAfterGateway =
            new List<RoomInstance>();

        if (gateway == null)
            return roomsAtOrAfterGateway;

        RoomInstance gatewayRoom =
            gateway.GetComponentInParent<RoomInstance>();

        if (gatewayRoom == null)
            return roomsAtOrAfterGateway;

        foreach (RoomInstance room in generatedRooms)
        {
            if (room != null && room.RoomNumber >= gatewayRoom.RoomNumber)
                roomsAtOrAfterGateway.Add(room);
        }

        return roomsAtOrAfterGateway;
    }

    private GatewayFlow GetMatchingSecretFlow(
        GatewayFlow gatewayFlow)
    {
        return gatewayFlow;
    }

    private GatewayFlow GetOppositeSecretFlow(
        GatewayFlow gatewayFlow)
    {
        return gatewayFlow == GatewayFlow.SecretForward
            ? GatewayFlow.SecretBackward
            : GatewayFlow.SecretForward;
    }

    private ElevationDestination FindSpecialDestination(
        Gateway gateway,
        GatewayFlow flow)
    {
        return FindSpecialDestination(
            gateway,
            generatedRooms,
            flow
        );
    }

    private ElevationDestination FindSpecialDestination(
        Gateway gateway,
        IReadOnlyList<RoomInstance> rooms)
    {
        return FindSpecialDestination(
            gateway,
            rooms,
            GetMatchingSecretFlow(gateway.Flow)
        );
    }

    private ElevationDestination FindSpecialDestination(
        Gateway gateway,
        IReadOnlyList<RoomInstance> rooms,
        GatewayFlow flow)
    {
        GatewayDirection requiredDirection = gateway.Direction.Opposite();

        foreach (RoomInstance room in rooms)
        {
            ElevationDestination[] destinations =
                room.GetComponentsInChildren<ElevationDestination>(true);

            foreach (ElevationDestination destination in destinations)
            {
                if (
                    destination.Flow == flow &&
                    destination.Direction == requiredDirection
                )
                {
                    return destination;
                }
            }
        }

        return null;
    }

    private ElevationDestination FindSpecialDestination(
        Gateway gateway,
        RoomInstance room,
        GatewayFlow flow)
    {
        if (gateway == null || room == null)
            return null;

        GatewayDirection requiredDirection = gateway.Direction.Opposite();
        ElevationDestination[] destinations =
            room.GetComponentsInChildren<ElevationDestination>(true);

        foreach (ElevationDestination destination in destinations)
        {
            if (
                destination.Flow == flow &&
                destination.Direction == requiredDirection
            )
            {
                return destination;
            }
        }

        return null;
    }

    private GameObject FindCompatibleSecretRoomPrefab(Gateway gateway)
    {
        GatewayFlow requiredFlow =
            GetMatchingSecretFlow(gateway.Flow);

        GatewayDirection requiredDirection =
            gateway.Direction.Opposite();

        foreach (GameObject prefab in secretRoomPrefabs)
        {
            if (prefab == null)
                continue;

            ElevationDestination[] destinations =
                prefab.GetComponentsInChildren<ElevationDestination>(true);

            foreach (ElevationDestination destination in destinations)
            {
                if (destination.Flow == requiredFlow &&
                    destination.Direction == requiredDirection)
                    return prefab;
            }
        }

        return null;
    }

    private RoomInstance GenerateSecretRoom(GameObject roomPrefab)
    {
        if (roomPrefab == null)
            return null;

        GameObject roomObject = Instantiate(
            roomPrefab,
            secretRoomSpawnOffset,
            Quaternion.identity,
            transform
        );

        RoomInstance roomInstance =
            roomObject.GetComponent<RoomInstance>();

        if (roomInstance == null)
        {
            Debug.LogError(
                $"Secret room prefab '{roomPrefab.name}' does not have " +
                "a RoomInstance component."
            );

            Destroy(roomObject);
            return null;
        }

        roomInstance.Initialize(0);
        generatedSecretRooms.Add(roomInstance);

        Debug.Log(
            $"Generated secret room: {roomPrefab.name} at " +
            $"{secretRoomSpawnOffset}."
        );

        return roomInstance;
    }

    private void SetGatewayVisible(Gateway gateway, bool visible)
    {
        GatewayVisibility visibility =
            gateway.GetComponent<GatewayVisibility>();

        if (visibility != null)
            visibility.SetVisible(visible);
        else
            gateway.enabled = visible;
    }

    private void SetSecretGatewayVisible(Gateway gateway, bool visible)
    {
        if (gateway == null)
            return;

        GatewaySecretVisibility secretVisibility =
            gateway.GetComponent<GatewaySecretVisibility>();

        if (secretVisibility != null)
        {
            if (visible)
                secretVisibility.UnlockSecretRoom();
            else
                secretVisibility.SetInvisible();

            return;
        }

        SetGatewayVisible(gateway, visible);
    }


    // -------------------------------------------------
    // PLAYER SPAWN
    // -------------------------------------------------

    private void SpawnPlayerAtFirstRoom()
    {
        if (generatedRooms.Count == 0)
        {
            Debug.LogError(
                "Cannot spawn player: no rooms were generated."
            );

            return;
        }

        if (Player.Instance == null)
        {
            Debug.LogError(
                "Cannot spawn player: Player.Instance is null."
            );

            return;
        }

        RoomInstance firstRoom =
            generatedRooms[0];

        RoomSpawnPoint spawnPoint =
            firstRoom.GetComponentInChildren<RoomSpawnPoint>();

        if (spawnPoint == null)
        {
            Debug.LogError(
                $"Room {firstRoom.RoomNumber} " +
                "does not have a RoomSpawnPoint."
            );

            return;
        }

        Player player =
            Player.Instance;

        RoomTransitionManager roomTransitionManager =
            RoomTransitionManager.Instance;

        if (roomTransitionManager == null)
        {
            roomTransitionManager =
                FindFirstObjectByType<RoomTransitionManager>();
        }

        if (roomTransitionManager != null)
        {
            roomTransitionManager.TransitionPlayer(
                player.transform,
                spawnPoint.transform,
                spawnPoint.ElevationLevel
            );
        }
        else
        {
            // Fallback if the fade/transition system is absent.
            player.transform.position =
                spawnPoint.transform.position;

            PlayerElevationLevel elevation =
                player.GetComponent<PlayerElevationLevel>();

            if (elevation != null)
            {
                elevation.SetLevel(
                    spawnPoint.ElevationLevel
                );
            }
            else
            {
                Debug.LogWarning(
                    "Player does not have a " +
                    "PlayerElevationLevel component."
                );
            }
        }

        Debug.Log(
            $"Player spawned in Room " +
            $"{firstRoom.RoomNumber} at " +
            $"{spawnPoint.transform.position} " +
            $"with elevation " +
            $"{spawnPoint.ElevationLevel}."
        );
    }


    // -------------------------------------------------
    // ROOM PREFAB COMPATIBILITY
    // -------------------------------------------------

    private GameObject FindCompatibleRoomPrefab(
        Gateway previousForwardGateway,
        List<GameObject> prefabPool)
    {
        GatewayDirection requiredDirection =
            previousForwardGateway.Direction.Opposite();

        Debug.Log(
            $"PCG CONNECTION REQUIREMENT: " +
            $"Gateway '{previousForwardGateway.name}' " +
            $"is {previousForwardGateway.Direction} / " +
            $"{previousForwardGateway.Flow}. " +
            $"Therefore next room requires a " +
            $"{requiredDirection} / " +
            $"{previousForwardGateway.Flow} Destination."
        );

        GatewayFlow requiredFlow =
            previousForwardGateway.Flow;

        List<GameObject> compatiblePrefabs =
            new List<GameObject>();

        foreach (GameObject prefab in prefabPool)
        {
            if (prefab == null)
            {
                continue;
            }

            ElevationDestination[] destinations =
                prefab.GetComponentsInChildren<
                    ElevationDestination
                >(true);

            foreach (
                ElevationDestination destination
                in destinations
            )
            {
                if (
                    destination.Direction ==
                    requiredDirection &&
                    destination.Flow ==
                    requiredFlow
                )
                {
                    compatiblePrefabs.Add(prefab);

                    break;
                }
            }
        }

        if (compatiblePrefabs.Count == 0)
        {
            Debug.LogError(
                $"No compatible room prefab found.\n" +
                $"Required Direction: " +
                $"{requiredDirection}\n" +
                $"Required Flow: " +
                $"{requiredFlow}"
            );

            return null;
        }

        GameObject selectedPrefab =
            compatiblePrefabs[
                Random.Range(
                    0,
                    compatiblePrefabs.Count
                )
            ];

        Debug.Log(
            $"Selected compatible room: " +
            $"{selectedPrefab.name}\n" +
            $"Required Direction: " +
            $"{requiredDirection}\n" +
            $"Required Flow: " +
            $"{requiredFlow}"
        );

        return selectedPrefab;
    }


    // -------------------------------------------------
    // ROOM CONNECTION
    // -------------------------------------------------

    private void ConnectRooms(
        RoomInstance previousRoom,
        RoomInstance newRoom)
    {
        if (previousRoom == null)
        {
            Debug.LogError(
                "ConnectRooms failed: previousRoom is null."
            );

            return;
        }

        if (newRoom == null)
        {
            Debug.LogError(
                "ConnectRooms failed: newRoom is null."
            );

            return;
        }


        // ---------------------------------------------
        // FIND PREVIOUS ROOM'S FORWARD GATEWAY
        // ---------------------------------------------

        Gateway previousForwardGateway =
            FindGateway(
                previousRoom,
                GatewayFlow.Forward
            );

        if (previousForwardGateway == null)
        {
            Debug.LogError(
                $"Room {previousRoom.RoomNumber} " +
                "does not have a Forward Gateway."
            );

            return;
        }


        // ---------------------------------------------
        // FIND NEW ROOM'S BACKWARD GATEWAY
        // ---------------------------------------------

        Gateway newBackwardGateway =
            FindGateway(
                newRoom,
                GatewayFlow.Backward
            );

        if (newBackwardGateway == null)
        {
            Debug.LogError(
                $"Room {newRoom.RoomNumber} " +
                "does not have a Backward Gateway."
            );

            return;
        }


        // ---------------------------------------------
        // DETERMINE REQUIRED DESTINATIONS
        // ---------------------------------------------

        GatewayDirection forwardDestinationDirection =
            previousForwardGateway.Direction.Opposite();

        GatewayDirection backwardDestinationDirection =
            newBackwardGateway.Direction.Opposite();


        // ---------------------------------------------
        // FIND NEW ROOM'S FORWARD DESTINATION
        // ---------------------------------------------

        ElevationDestination newForwardDestination =
            FindDestination(
                newRoom,
                GatewayFlow.Forward,
                forwardDestinationDirection
            );

        if (newForwardDestination == null)
        {
            Debug.LogError(
                $"Room {newRoom.RoomNumber} does not have " +
                $"a Forward Destination with Direction " +
                $"{forwardDestinationDirection}."
            );

            return;
        }


        // ---------------------------------------------
        // FIND PREVIOUS ROOM'S BACKWARD DESTINATION
        // ---------------------------------------------

        ElevationDestination previousBackwardDestination =
            FindDestination(
                previousRoom,
                GatewayFlow.Backward,
                backwardDestinationDirection
            );

        if (previousBackwardDestination == null)
        {
            Debug.LogError(
                $"Room {previousRoom.RoomNumber} does not have " +
                $"a Backward Destination with Direction " +
                $"{backwardDestinationDirection}."
            );

            return;
        }


        // ---------------------------------------------
        // CONNECT FORWARD
        // ---------------------------------------------

        previousForwardGateway.SetDestination(
            newForwardDestination.transform
        );


        // ---------------------------------------------
        // CONNECT BACKWARD
        // ---------------------------------------------

        newBackwardGateway.SetDestination(
            previousBackwardDestination.transform
        );

        Debug.Log(
            $"Successfully connected " +
            $"Room {previousRoom.RoomNumber} ↔ " +
            $"Room {newRoom.RoomNumber}."
        );
    }


    // -------------------------------------------------
    // GATEWAY / DESTINATION LOOKUPS
    // -------------------------------------------------

    private Gateway FindGateway(
        RoomInstance room,
        GatewayFlow flow)
    {
        if (room == null)
        {
            Debug.LogError(
                "FindGateway failed: Room is null."
            );

            return null;
        }

        Gateway[] gateways =
            room.GetComponentsInChildren<Gateway>(true);

        Debug.Log(
            $"Searching Room {room.RoomNumber} " +
            $"for {flow} Gateway. " +
            $"Found {gateways.Length} Gateway component(s)."
        );

        foreach (Gateway gateway in gateways)
        {
            Debug.Log(
                $"Gateway found: '{gateway.name}' | " +
                $"Direction = {gateway.Direction} | " +
                $"Flow = {gateway.Flow}"
            );

            if (gateway.Flow == flow)
            {
                Debug.Log(
                    $"SELECTED Gateway: '{gateway.name}' | " +
                    $"Direction = {gateway.Direction} | " +
                    $"Flow = {gateway.Flow}"
                );

                return gateway;
            }
        }

        Debug.LogError(
            $"Room {room.RoomNumber} does not contain " +
            $"a {flow} Gateway."
        );

        return null;
    }


    private ElevationDestination FindDestination(
        RoomInstance room,
        GatewayFlow flow,
        GatewayDirection direction)
    {
        ElevationDestination[] destinations =
            room.GetComponentsInChildren<
                ElevationDestination
            >(true);

        foreach (
            ElevationDestination destination
            in destinations
        )
        {
            if (
                destination.Flow == flow &&
                destination.Direction == direction
            )
            {
                return destination;
            }
        }

        return null;
    }


    // -------------------------------------------------
    // ROOM PROGRESSION
    // -------------------------------------------------

    public void SetRoomCleared(int roomNumber)
    {
        if (roomNumber < 1 || roomNumber > generatedRooms.Count)
        {
            Debug.LogWarning(
                $"SetRoomCleared received invalid room number: {roomNumber}."
            );

            return;
        }

        // Prevent duplicate clear notifications.
        if (!clearedRooms.Add(roomNumber))
        {
            Debug.Log(
                $"Room {roomNumber} was already marked as cleared."
            );

            return;
        }

        Debug.Log(
            $"Room {roomNumber} cleared."
        );


        // -------------------------------------------------
        // UNLOCK THIS ROOM'S FORWARD GATEWAY
        // -------------------------------------------------

        RoomInstance clearedRoom =
            generatedRooms[roomNumber - 1];

        SetRoomGatewayState(
            clearedRoom,
            true
        );


        // -------------------------------------------------
        // UPDATE CURRENT ROOM PROGRESSION
        // -------------------------------------------------

        // Move currentRoomNumber forward while the next
        // room in sequence has already been cleared.

        while (
            currentRoomNumber <= generatedRooms.Count &&
            clearedRooms.Contains(currentRoomNumber)
        )
        {
            currentRoomNumber++;
        }


        // -------------------------------------------------
        // FLOOR COMPLETE
        // -------------------------------------------------

        if (currentRoomNumber > generatedRooms.Count)
        {
            Debug.Log(
                "All rooms on this floor have been cleared."
            );

            HeadsUpTextManager.Show(
                "The floor has been cleared. The gateway to the depths has opened."
            );

            foreach (Gateway gateway in validFloorGateways)
                SetGatewayVisible(gateway, true);

            if (GameManager.Instance != null)
            {
                // Replace 1 with your actual floor ID later.
                GameManager.Instance.FloorCleared(1);
            }

            return;
        }


        Debug.Log(
            $"Room progression advanced. " +
            $"Current room: {currentRoomNumber}."
        );
    }


    private void LockAllRoomGateways()
    {
        foreach (RoomInstance room in generatedRooms)
        {
            Gateway forwardGateway =
                FindGateway(
                    room,
                    GatewayFlow.Forward
                );

            if (forwardGateway == null)
                continue;

            GatewayVisibility visibility =
                forwardGateway.GetComponent<
                    GatewayVisibility
                >();

            if (visibility != null)
            {
                visibility.SetVisible(false);
            }
        }
    }


    // -------------------------------------------------
    // CLEANUP
    // -------------------------------------------------

    private void ClearGeneratedRooms()
    {
        // Remove event subscriptions before destroying rooms.
        foreach (
            KeyValuePair<
                RoomInstance,
                List<EnemySpawnPoint>
            > entry
            in roomSpawnPoints
        )
        {
            RoomInstance room =
                entry.Key;

            List<EnemySpawnPoint> spawnPoints =
                entry.Value;

            if (spawnPoints == null)
                continue;

            foreach (
                EnemySpawnPoint spawnPoint
                in spawnPoints
            )
            {
                if (spawnPoint == null)
                    continue;

                // We subscribed using a lambda, so the
                // subscription will disappear when the
                // spawn point is destroyed.
                //
                // The room itself is being destroyed below.
            }
        }

        roomSpawnPoints.Clear();
        clearedSpawnPoints.Clear();
        clearedRooms.Clear();
        validFloorGateways.Clear();
        validSecretGateway = null;
        secretUnlockRoom = null;


        foreach (RoomInstance room in generatedRooms)
        {
            if (room != null)
            {
                Destroy(room.gameObject);
            }
        }

        generatedRooms.Clear();

        foreach (RoomInstance room in generatedSecretRooms)
        {
            if (room != null)
                Destroy(room.gameObject);
        }

        generatedSecretRooms.Clear();
    }
    private void SetRoomGatewayState(
    RoomInstance room,
    bool unlocked)
    {
        if (room == null)
            return;


        Gateway forwardGateway =
            FindGateway(
                room,
                GatewayFlow.Forward
            );

        if (forwardGateway == null)
        {
            Debug.LogWarning(
                $"Room {room.RoomNumber} has no " +
                "Forward Gateway."
            );

            return;
        }


        GatewayVisibility visibility =
            forwardGateway.GetComponent<
                GatewayVisibility
            >();

        if (visibility == null)
        {
            Debug.LogWarning(
                $"Forward Gateway '{forwardGateway.name}' " +
                $"in Room {room.RoomNumber} has no " +
                "GatewayVisibility component."
            );

            return;
        }


        if (unlocked)
        {
            visibility.Unlock();
        }
        else
        {
            visibility.Lock();
        }


        Debug.Log(
            $"Room {room.RoomNumber} Forward Gateway " +
            $"{(unlocked ? "UNLOCKED" : "LOCKED")}."
        );
    }
}