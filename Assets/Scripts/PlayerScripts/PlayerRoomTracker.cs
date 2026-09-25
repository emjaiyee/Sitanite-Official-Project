using System;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerRoomTracker : MonoBehaviour
{
    public RoomInstance CurrentRoom { get; private set; }
    public event Action<RoomInstance> OnRoomChanged;

    private RoomManager roomManager;

    private void Update()
    {
        if (roomManager == null)
            roomManager = FindFirstObjectByType<RoomManager>();

        if (roomManager == null)
            return;

        RoomInstance detectedRoom =
            roomManager.FindRoomAtPosition(transform.position);

        if (detectedRoom == CurrentRoom)
            return;

        CurrentRoom = detectedRoom;
        roomManager.SetTrackedPlayerRoom(CurrentRoom);
        OnRoomChanged?.Invoke(CurrentRoom);
    }
}