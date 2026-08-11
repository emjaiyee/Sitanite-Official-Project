using UnityEngine;

public class RoomInstance : MonoBehaviour
{
    private int roomNumber;

    public int RoomNumber => roomNumber;

    public void Initialize(int number)
    {
        roomNumber = number;
    }
}