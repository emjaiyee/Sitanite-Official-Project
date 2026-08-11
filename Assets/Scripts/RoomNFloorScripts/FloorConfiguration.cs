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

    public bool ContainsFloor(int floor)
    {
        return floor >= minimumFloor &&
               floor <= maximumFloor;
    }
}