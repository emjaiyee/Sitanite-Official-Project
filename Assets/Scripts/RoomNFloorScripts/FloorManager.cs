using System.Collections.Generic;
using UnityEngine;

public class FloorManager : MonoBehaviour
{
    public const int TutorialFloor = 0;

    [System.Serializable]
    private class ManggagawayBossFloor
    {
        [Tooltip("The exact floor for Manggagaway. Set to 0 to leave the boss floor unassigned.")]
        [Min(0)] public int floor = 0;
    }

    [Header("Current Floor")]
    [SerializeField, Min(TutorialFloor)]
    private int currentFloor = TutorialFloor;

    [Header("Floor Configurations")]
    [SerializeField]
    private List<FloorConfiguration> floorConfigurations =
        new List<FloorConfiguration>();

    [Header("Manggagaway Boss Floor")]
    [SerializeField]
    private ManggagawayBossFloor manggagawayBossFloor = new ManggagawayBossFloor();

    [Header("References")]
    [SerializeField]
    private RoomManager roomManager;

    [SerializeField] private DungeonMemory dungeonMemory;

    public int CurrentFloor => currentFloor;
    public bool IsTutorialFloor => currentFloor == TutorialFloor;
    public bool IsManggagawayBossFloor => IsManggagawayFloor(currentFloor);

    public FloorConfiguration CurrentConfiguration
    {
        get
        {
            return GetConfigurationForFloor(currentFloor);
        }
    }

    private void Awake()
    {
        ValidateConfigurations();
        ApplyRequestedStartingFloor();
    }

    public bool HasFloor(int floor)
    {
        return floor == TutorialFloor ||
               IsManggagawayFloor(floor) ||
               GetConfigurationForFloor(floor) != null;
    }

    // Debug only: must be called before Start generates the first floor.
    public bool SetStartingFloor(int floor)
    {
        if (floor < TutorialFloor || !HasFloor(floor))
            return false;

        currentFloor = floor;
        return true;
    }

    private void Start()
    {
        GenerateCurrentFloor();
    }

    public void GenerateCurrentFloor()
    {
        FloorConfiguration configuration =
            CurrentConfiguration;

        if (
            configuration == null &&
            !IsTutorialFloor &&
            !IsManggagawayBossFloor
        )
        {
            Debug.LogError(
                $"Cannot generate Floor {currentFloor}: " +
                "no configuration exists."
            );

            return;
        }

        if (roomManager == null)
        {
            Debug.LogError(
                "FloorManager has no RoomManager assigned."
            );

            return;
        }

        roomManager.GenerateFloor(
            configuration,
            IsManggagawayBossFloor,
            IsTutorialFloor
        );

        if (roomManager.GeneratedRooms.Count > 0 && Player.Instance != null)
        {
            Player.Instance.RecordFloorEntered(currentFloor);
            if (dungeonMemory == null)
                dungeonMemory = FindFirstObjectByType<DungeonMemory>();

            if (dungeonMemory != null)
                dungeonMemory.SpawnForFloor(currentFloor, roomManager.GeneratedRooms);
        }

        FloorTextManager.ShowFloor(currentFloor);
    }

    public void EnterNextFloor()
    {
        int nextFloor = currentFloor + 1;

        if (
            nextFloor != TutorialFloor &&
            !IsManggagawayFloor(nextFloor) &&
            GetConfigurationForFloor(nextFloor) == null)
        {
            Debug.LogError(
                $"Cannot enter Floor {nextFloor}: " +
                "no configuration exists."
            );

            return;
        }

        currentFloor = nextFloor;
        GenerateCurrentFloor();
    }

    private bool IsManggagawayFloor(int floor)
    {
        return manggagawayBossFloor != null &&
               manggagawayBossFloor.floor > 0 &&
               manggagawayBossFloor.floor == floor;
    }

    private FloorConfiguration GetConfigurationForFloor(
        int floor)
    {
        foreach (
            FloorConfiguration configuration
            in floorConfigurations
        )
        {
            if (configuration == null)
            {
                continue;
            }

            if (configuration.ContainsFloor(floor))
            {
                return configuration;
            }
        }

        return null;
    }

    private void ValidateConfigurations()
    {
        for (
            int i = 0;
            i < floorConfigurations.Count;
            i++
        )
        {
            FloorConfiguration configuration =
                floorConfigurations[i];

            if (configuration == null)
                continue;

            if (
                configuration.minimumFloor >
                configuration.maximumFloor
            )
            {
                Debug.LogError(
                    $"FloorConfiguration {i} has an invalid range: " +
                    $"{configuration.minimumFloor}-" +
                    $"{configuration.maximumFloor}."
                );
            }

            if (
                configuration.minimumRooms >
                configuration.maximumRooms
            )
            {
                Debug.LogError(
                    $"FloorConfiguration {i} has invalid room count: " +
                    $"{configuration.minimumRooms}-" +
                    $"{configuration.maximumRooms}."
                );
            }

            if (
                !configuration.allowSmall &&
                !configuration.allowMedium &&
                !configuration.allowLarge &&
                !configuration.allowXL
            )
            {
                Debug.LogError(
                    $"FloorConfiguration {i} does not allow " +
                    "any room sizes."
                );
            }
        }
    }

    private void ApplyRequestedStartingFloor()
    {
        if (Player.Instance == null)
            return;

        PlayerStats playerStats =
            Player.Instance.GetComponent<PlayerStats>();

        if (
            playerStats == null ||
            !playerStats.TryConsumeRequestedStartingFloor(
                out int requestedFloor
            )
        )
        {
            return;
        }

        if (!SetStartingFloor(requestedFloor))
        {
            Debug.LogError(
                $"Cannot start on requested Floor {requestedFloor}: " +
                "no matching floor is configured.",
                this
            );
        }
    }
}