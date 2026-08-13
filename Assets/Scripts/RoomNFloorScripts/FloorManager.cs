using System.Collections.Generic;
using UnityEngine;

public class FloorManager : MonoBehaviour
{
    [Header("Current Floor")]
    [SerializeField, Min(1)]
    private int currentFloor = 1;

    [Header("Floor Configurations")]
    [SerializeField]
    private List<FloorConfiguration> floorConfigurations =
        new List<FloorConfiguration>();

    [Header("References")]
    [SerializeField]
    private RoomManager roomManager;

    public int CurrentFloor => currentFloor;

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
    }

    private void Start()
    {
        GenerateCurrentFloor();
    }

    public void GenerateCurrentFloor()
    {
        FloorConfiguration configuration =
            CurrentConfiguration;

        if (configuration == null)
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
            configuration
        );
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

        Debug.LogError(
            $"No FloorConfiguration found for Floor {floor}."
        );

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
}