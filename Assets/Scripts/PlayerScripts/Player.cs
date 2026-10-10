using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public struct RunStatistics
    {
        public int kills;
        public int miniBossKills;
        public int roomsCleared;
        public int floorsEntered;
    }

    public static Player Instance { get; private set; }

    [Header("Collider References")]
    [SerializeField] private Collider2D rampMovementCollider;

    public Collider2D RampMovementCollider => rampMovementCollider;

    private RunStatistics runStatistics;
    private readonly HashSet<int> enteredFloors = new HashSet<int>();
    private readonly HashSet<RoomInstance> clearedRooms = new HashSet<RoomInstance>();
    private readonly HashSet<string> rolledDungeonMemories = new HashSet<string>();
    private readonly HashSet<string> encounteredDungeonMemories = new HashSet<string>();
    private bool runFinished;

    public bool TryRollDungeonMemory(string memoryId, int floor = 0)
    {
        return !runFinished && !encounteredDungeonMemories.Contains(memoryId) && rolledDungeonMemories.Add($"{memoryId}:{floor}");
    }

    public void MarkDungeonMemoryEncountered(string memoryId)
    {
        encounteredDungeonMemories.Add(memoryId);
    }

    public void RecordEnemyDefeated(bool miniBoss)
    {
        if (runFinished)
            return;

        if (miniBoss)
            runStatistics.miniBossKills++;
        else
            runStatistics.kills++;
    }

    public void RecordRoomCleared(RoomInstance room)
    {
        if (!runFinished && room != null && clearedRooms.Add(room))
            runStatistics.roomsCleared++;
    }

    public void RecordFloorEntered(int floor)
    {
        if (!runFinished && enteredFloors.Add(floor))
            runStatistics.floorsEntered++;
    }

    public RunStatistics FinishRun()
    {
        runFinished = true;
        return runStatistics;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning(
                "Multiple Player instances found! Destroying duplicate.",
                this
            );

            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (GetComponent<EquipmentCharacterVisualSync>() == null)
        {
            gameObject.AddComponent<EquipmentCharacterVisualSync>();
        }

        if (GetComponent<PlayerRoomTracker>() == null)
            gameObject.AddComponent<PlayerRoomTracker>();

        DontDestroyOnLoad(gameObject);

        Debug.Log(
            "PLAYER AWAKE — Player.Instance has been assigned.",
            this
        );
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}