using System;
using UnityEngine;

/// <summary>
/// Represents the grid matrix of the inventory system.
/// Manages occupancy data, spatial collision checks, stack merges, and model update events.
/// </summary>
public class InventoryGrid : MonoBehaviour
{
    #region Serialized Fields
    [Header("Grid")]
    [Tooltip("Total columns in the inventory grid.")]
    [field: SerializeField] public int GridWidth { get; private set; } = 8;

    [Tooltip("Total rows in the inventory grid.")]
    [field: SerializeField] public int GridHeight { get; private set; } = 6;
    #endregion

    #region Private Fields
    private InventoryItem[,] gridMatrix;
    #endregion

    #region Model Events
    /// <summary>Fired when an item is successfully placed into the matrix.</summary>
    public event Action<InventoryItem, Vector2Int> OnItemPlaced;

    /// <summary>Fired when an item is removed from the matrix.</summary>
    public event Action<InventoryItem, Vector2Int> OnItemRemoved;

    /// <summary>Fired when an item's orientation index changes.</summary>
    public event Action<InventoryItem> OnItemRotated;

    /// <summary>Fired when an existing item's quantity or state is updated.</summary>
    public event Action<InventoryItem> OnItemUpdated;
    #endregion

    #region Lifecycle
    private void Awake()
    {
        gridMatrix = new InventoryItem[GridWidth, GridHeight];
    }
    #endregion

    #region Public API - Insertion & Stacking
    /// <summary>
    /// Tries to add item on inventory, checks if its stackable with current items
    /// then searches for empty space (test unrotated, then rotated orientations).
    /// </summary>
    /// <param name="item">Target inventory item data</param>
    /// <returns>True if the item was partially or fully added to the grid; otherwise, false.</returns>
    public bool TryAddItem(InventoryItem item)
    {
        if (item == null || item.Data == null || item.Quantity <= 0) return false;

        // Attempt stack-merging with matching existing items
        if (item.Data.isStackable)
        {
            TryStackItem(item);
            if (item.Quantity <= 0) return true;
        }

        // Search for unrotated fit
        if (FindSpaceForItem(item, out Vector2Int position))
        {
            return PlaceItem(item, position.x, position.y);
        }

        // Search for rotated fit
        item.Rotate();
        if (FindSpaceForItem(item, out position))
        {
            OnItemRotated?.Invoke(item);
            return PlaceItem(item, position.x, position.y);
        }

        // Revert rotation state if no fit found
        item.Rotate();
        OnItemRotated?.Invoke(item);
        return false;
    }

    /// <summary>
    /// Validates spatial availability and places an item into matrix cells starting from top-left coordinates.
    /// </summary>
    /// <param name="item">Target inventory item data</param>
    /// <param name="startX">Target top-left column index.</param>
    /// <param name="startY">Target top-left row index.</param>
    /// <returns>True if placement succeeded; false if obstructed or out of bounds.</returns>
    public bool PlaceItem(InventoryItem item, int startX, int startY)
    {
        if (!CanPlaceItem(item, startX, startY)) return false;

        RemoveItem(item); 

        int width = item.GetWidth();
        int height = item.GetHeight();

        // Occupy all target cell coordinates across item width and height
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

    /// <summary>
    /// Removes all grid matrix item references
    /// </summary>
    /// <param name="item">Target inventory item data to remove</param>
    public void RemoveItem(InventoryItem item)
    {
        Vector2Int pos = item.OriginPosition;

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

    /// <summary>
    /// Rotates an item instance and triggers the corresponding rotation event.
    /// </summary>
    /// <param name="item">Target inventory item data to rotate</param>
    public void RotateItem(InventoryItem item)
    {
        item.Rotate();
        OnItemRotated?.Invoke(item);
    }
    #endregion

    #region Public API - Validation & Queries
    /// <summary>
    /// Retrieves the inventory item occupying the specified matrix coordinate.
    /// </summary>
    /// <param name="x">Column index.</param>
    /// <param name="y">Row index.</param>
    /// <returns>Reference to item if cell is occupied; otherwise, null.</returns>
    public InventoryItem GetItem(int x, int y)
    {
        if (!IsWithinBounds(x, y)) return null;
        return gridMatrix[x, y];
    }

    /// <summary>
    /// Checks whether an item can fit into the target location without overlapping or clipping bounds.
    /// </summary>
    /// <param name="item">Target inventory item data to check</param>
    /// <param name="startX">Target top-left column position.</param>
    /// <param name="startY">Target top-left row position.</param>
    /// <returns>True if all target cells are within bounds and unblocked.</returns>
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
                // Ignore self-references to allow picking up and re-placing items into overlapping origins
                if (gridMatrix[x, y] != null && gridMatrix[x, y] != item)
                    return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Iterates through grid to locate the first available slot that fits the item.
    /// </summary>
    /// <param name="item"> Target inventory item data to match against open cells</param>
    /// <param name="position">top-left origin coordinates if space is found.</param>
    /// <returns>True if open space exists; otherwise, false.</returns>
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

    /// <summary>
    /// Checks if given cell coordinate falls within valid bounds.
    /// </summary>
    /// <param name="x">Column coordinate.</param>
    /// <param name="y">Row coordinate.</param>
    /// <returns>True if coordinate is within range; otherwise, false.</returns>
    public bool IsWithinBounds(int x, int y)
    {
        return x >= 0 && x < GridWidth && y >= 0 && y < GridHeight;
    }
    #endregion

    #region Internal Helpers
    private void TryStackItem(InventoryItem item)
    {
        for (int y = 0; y < GridHeight; y++)
        {
            for (int x = 0; x < GridWidth; x++)
            {
                InventoryItem existingItem = gridMatrix[x, y];

                // Check top-left origin matching to prevent repeating stack logic for multi-cell items
                if (existingItem != null && existingItem.Data == item.Data && existingItem.OriginPosition == new Vector2Int(x, y))
                {
                    int maxStack = existingItem.Data.maxStackSize;
                    if (existingItem.Quantity < maxStack)
                    {
                        int spaceAvailable = maxStack - existingItem.Quantity;
                        int amountToTransfer = Mathf.Min(spaceAvailable, item.Quantity);

                        existingItem.Quantity += amountToTransfer;
                        item.Quantity -= amountToTransfer;

                        OnItemUpdated?.Invoke(existingItem);

                        if (item.Quantity <= 0) return;
                    }
                }
            }
        }
    }
    #endregion
}