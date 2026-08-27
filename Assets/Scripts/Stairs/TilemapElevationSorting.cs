using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(TilemapRenderer))]
public class TilemapElevationSorting : MonoBehaviour
{
    [SerializeField] private int tilemapLevel;

    [SerializeField] private int behindPlayerOrder = 0;
    [SerializeField] private int inFrontPlayerOrder = 20;

    private TilemapRenderer renderer;

    private void Awake()
    {
        renderer = GetComponent<TilemapRenderer>();
    }

    private void OnEnable()
    {
        PlayerElevationLevel.OnElevationChanged += Refresh;
    }

    private void OnDisable()
    {
        PlayerElevationLevel.OnElevationChanged -= Refresh;
    }

    private void Refresh(int playerLevel)
    {
        if (playerLevel < tilemapLevel)
            renderer.sortingOrder = inFrontPlayerOrder;
        else
            renderer.sortingOrder = behindPlayerOrder;
    }
}