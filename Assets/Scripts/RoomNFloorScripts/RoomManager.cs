using System.Collections.Generic;
using UnityEngine;

public class RoomManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private RoomManager roomManager;

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

    private int currentRoomNumber = 1;

    public int CurrentRoomNumber => currentRoomNumber;
    public bool LockNextRoomUntilCleared => lockNextRoomUntilCleared;

    private readonly List<RoomInstance> generatedRooms =
        new List<RoomInstance>();

    public IReadOnlyList<RoomInstance> GeneratedRooms =>
        generatedRooms;

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
            firstBackwardGateway.SetGatewayEnabled(false);
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
                finalForwardGateway.SetGatewayEnabled(false);
            }

            Debug.Log(
                $"Final room is Room " +
                $"{finalRoom.RoomNumber}."
            );
        }

        // ---------------------------------------------
        // ROOM PROGRESSION
        // ---------------------------------------------

        currentRoomNumber = 1;

        if (lockNextRoomUntilCleared)
        {
            LockAllRoomGateways();
            UnlockCurrentRoom();
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

        Debug.Log(
            $"Generated Room {roomNumber}: " +
            $"{roomPrefab.name} " +
            $"at X = {spawnPosition.x}"
        );

        return roomInstance;
    }

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

        // Position player
        player.transform.position =
            spawnPoint.transform.position;

        // Set elevation
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

        Debug.Log(
            $"Player spawned in Room " +
            $"{firstRoom.RoomNumber} at " +
            $"{spawnPoint.transform.position} " +
            $"with elevation " +
            $"{spawnPoint.ElevationLevel}."
        );
    }


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
    public void SetRoomCleared(int roomNumber)
    {
        if (roomNumber != currentRoomNumber)
        {
            return;
        }

        Debug.Log($"Room {roomNumber} cleared.");

        currentRoomNumber++;

        UnlockCurrentRoom();
    }

    private void ClearGeneratedRooms()
    {
        foreach (RoomInstance room in generatedRooms)
        {
            if (room != null)
            {
                Destroy(room.gameObject);
            }
        }

        generatedRooms.Clear();
    }
    private void UnlockCurrentRoom()
    {
        if (currentRoomNumber > generatedRooms.Count)
        {
            Debug.Log("Floor cleared!");

            // For future references, to whoever does the GameManager's floor clearing:
            // GameManager.Instance.FloorCleared();

            return;
        }

        RoomInstance currentRoom =
            generatedRooms[currentRoomNumber - 1];

        Gateway forwardGateway =
            FindGateway(
                currentRoom,
                GatewayFlow.Forward
            );

        if (forwardGateway == null)
        {
            Debug.LogWarning(
                $"Room {currentRoomNumber} " +
                "has no Forward Gateway."
            );

            return;
        }

        GatewayVisibility visibility =
            forwardGateway.GetComponent<GatewayVisibility>();

        if (visibility != null)
        {
            visibility.SetVisible(true);
        }

        Debug.Log(
            $"Room {currentRoomNumber} unlocked."
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
                forwardGateway.GetComponent<GatewayVisibility>();

            if (visibility != null)
            {
                visibility.SetVisible(false);
            }
        }
    }
}