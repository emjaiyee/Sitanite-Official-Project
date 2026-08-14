using System;
using UnityEngine;

public class InventoryGrid : MonoBehaviour
{
    [field: SerializeField] public int GridWidth { get; private set; } = 8;
    [field: SerializeField] public int GridHeight { get; private set; } = 6;

    private InventoryItem[,] gridMatrix;

    // Model Events
    public event Action<InventoryItem, Vector2Int> OnItemPlaced;
    public event Action<InventoryItem, Vector2Int> OnItemRemoved;
    public event Action<InventoryItem> OnItemRotated;

    private void Awake()
    {
        gridMatrix = new InventoryItem[GridWidth, GridHeight];
    }

    public InventoryItem GetItem(int x, int y)
    {
        if (!IsWithinBounds(x, y)) return null;
        return gridMatrix[x, y];
    }

    public bool CanPlaceItem(InventoryItem item, int startX, int startY)
    {
        int width = item.GetWidth();
        int height = item.GetHeight();

        if (!IsWithinBounds(startX, startY) || !IsWithinBounds(startX + width - 1, startY + height - 1))
            return false;

        for (int x = startX; x < startX + width; x++)
        {
            for (int y = startY; y < startY + height; y++)
            {
                if (gridMatrix[x, y] != null && gridMatrix[x, y] != item)
                    return false;
            }
        }
        return true;
    }

    public bool PlaceItem(InventoryItem item, int startX, int startY)
    {
        if (!CanPlaceItem(item, startX, startY)) return false;

        RemoveItem(item); 

        int width = item.GetWidth();
        int height = item.GetHeight();

        for (int x = startX; x < startX + width; x++)
        {
            for (int y = startY; y < startY + height; y++)
            {
                gridMatrix[x, y] = item;
            }
        }

        item.OriginPosition = new Vector2Int(startX, startY);
        OnItemPlaced?.Invoke(item, item.OriginPosition);
        return true;
    }

    public void RemoveItem(InventoryItem item)
    {
        Vector2Int pos = item.OriginPosition;
        int width = item.GetWidth();
        int height = item.GetHeight();

        bool clearedAny = false;
        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                if (gridMatrix[x, y] == item)
                {
                    gridMatrix[x, y] = null;
                    clearedAny = true;
                }
            }
        }

        if (clearedAny)
        {
            OnItemRemoved?.Invoke(item, pos);
        }
    }

    public void RotateItem(InventoryItem item)
    {
        item.Rotate();
        OnItemRotated?.Invoke(item);
    }

    public bool FindSpaceForItem(InventoryItem item, out Vector2Int position)
    {
        for (int y = 0; y <= GridHeight - item.GetHeight(); y++)
        {
            for (int x = 0; x <= GridWidth - item.GetWidth(); x++)
            {
                if (CanPlaceItem(item, x, y))
                {
                    position = new Vector2Int(x, y);
                    return true;
                }
            }
        }
        position = Vector2Int.zero;
        return false;
    }

    public bool IsWithinBounds(int x, int y)
    {
        return x >= 0 && x < GridWidth && y >= 0 && y < GridHeight;
    }
}