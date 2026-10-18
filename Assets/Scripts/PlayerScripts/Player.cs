using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerShieldDefense))]
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
    private readonly List<DungeonMemory.DamageUse> adaptiveDamageUses = new List<DungeonMemory.DamageUse>();
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

    public void RecordAdaptiveKill(DamageType damageType, bool ranged)
    {
        if (runFinished)
            return;

        DamageType selected = DamageType.None;
        foreach (DamageType type in BossBalance.DamageTypes())
        {
            if ((damageType & type) != 0)
            {
                selected = type;
                break;
            }
        }
        if (selected == DamageType.None)
            return;

        DungeonMemory.DamageUse current = adaptiveDamageUses.Find(use => use.type == selected);
        if (current == null)
        {
            current = new DungeonMemory.DamageUse { type = selected };
            adaptiveDamageUses.Add(current);
        }
        foreach (DungeonMemory.DamageUse use in adaptiveDamageUses)
        {
            use.kills = use == current ? Mathf.Min(use.kills, int.MaxValue - 1) + 1 : Mathf.Max(0, use.kills - 1);
            if (!ranged)
                use.rangedKills = Mathf.Max(0, use.rangedKills - 1);
        }
        if (ranged)
            current.rangedKills = Mathf.Min(current.rangedKills, int.MaxValue - 1) + 1;
    }

    public DungeonMemory.Adaptation GetAdaptation(int threshold = 50)
    {
        DungeonMemory.DamageUse resistant = null;
        DungeonMemory.DamageUse ranged = null;
        long rangedTotal = 0;
        foreach (DungeonMemory.DamageUse use in adaptiveDamageUses)
        {
            if (resistant == null || use.kills > resistant.kills)
                resistant = use;
            if (ranged == null || use.rangedKills > ranged.rangedKills)
                ranged = use;
            rangedTotal += use.rangedKills;
        }
        bool swarm = ranged != null && rangedTotal >= Mathf.Max(1, threshold);
        return new DungeonMemory.Adaptation
        {
            damageType = swarm ? ranged.type : resistant != null && resistant.kills >= Mathf.Max(1, threshold) ? resistant.type : DamageType.None,
            swarm = swarm
        };
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