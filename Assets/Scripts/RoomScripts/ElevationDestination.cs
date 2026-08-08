using UnityEngine;

public class ElevationDestination : MonoBehaviour
{
    [SerializeField] private int elevationLevel = 0;

    public int ElevationLevel => elevationLevel;
}