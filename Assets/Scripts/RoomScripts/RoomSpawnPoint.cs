using UnityEngine;

public class RoomSpawnPoint : MonoBehaviour
{
    [SerializeField] private int elevationLevel = 0;

    public int ElevationLevel => elevationLevel;
}