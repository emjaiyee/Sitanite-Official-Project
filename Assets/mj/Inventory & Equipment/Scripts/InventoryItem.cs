using System;
using UnityEngine;

public class InventoryItem
{
    public ItemData Data { get; }
    public int Quantity { get; set; }
    public Vector2Int OriginPosition { get; set; }
    public int RotationIndex { get; private set; }

    public InventoryItem(ItemData data, int quantity = 1)
    {
        Data = data;
        Quantity = quantity;
        RotationIndex = 0;
    }

    public int GetWidth() => RotationIndex % 2 == 0 ? Data.gridWidth : Data.gridHeight;
    public int GetHeight() => RotationIndex % 2 == 0 ? Data.gridHeight : Data.gridWidth;

    public void Rotate()
    {
        RotationIndex = (RotationIndex + 1) % 4;
    }
}
