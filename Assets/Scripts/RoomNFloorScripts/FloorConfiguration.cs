using System;
using UnityEngine;

[Serializable]
public class FloorConfiguration
{
    [Header("Floor Range")]
    [Min(1)]
    public int minimumFloor = 1;

    [Min(1)]
    public int maximumFloor = 1;

    [Header("Allowed Room Sizes")]
    public bool allowSmall = true;
    public bool allowMedium = false;
    public bool allowLarge = false;
    public bool allowXL = false;

    [Header("Room Count")]
    [Min(1)]
    public int minimumRooms = 4;

    [Min(1)]
    public int maximumRooms = 10;

    [Header("Enemy Spawning")]
    [Min(0)]
    public int minimumActiveSpawners = 1;

    [Min(0)]
    public int maximumActiveSpawners = 3;

    [Min(1)]
    public int minimumEnemyLevel = 1;

    [Min(1)]
    public int maximumEnemyLevel = 1;

    [Range(0f, 1f)]
    public float maximumEnemyLevelChance = 0.25f;

    public bool ContainsFloor(int floor)
    {
        return floor >= minimumFloor &&
               floor <= maximumFloor;
    }
}