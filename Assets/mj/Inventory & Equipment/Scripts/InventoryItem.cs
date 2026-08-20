using UnityEngine;

/// <summary>
/// Runtime instance wrapper for items placed inside the inventory grid.
/// Tracks current stack state, top-left grid origin, and 90-degree orientation.
/// </summary>
public class InventoryItem
{
    #region Properties
    /// <summary>Gets the immutable base item configuration asset.</summary>
    public ItemData Data { get; }

    /// <summary>Gets or sets the current stack size of this item instance.</summary>
    public int Quantity { get; set; }

    /// <summary>Gets or sets the top-left anchor coordinate on the inventory grid.</summary>
    public Vector2Int OriginPosition { get; set; }

    /// <summary>
    /// Gets the current rotation state step (0 = 0°, 1 = 90°, 2 = 180°, 3 = 270°).
    /// </summary>
    public int RotationIndex { get; private set; }
    #endregion

    #region Constructor
    /// <summary>
    /// Creates a new grid item instance.
    /// </summary>
    /// <param name="data">ScriptableObject ItemData</param>
    /// <param name="quantity">Stack size.</param>
    public InventoryItem(ItemData data, int quantity = 1)
    {
        Data = data;
        Quantity = quantity;
        RotationIndex = 0;
    }
    #endregion

    #region Public API
    /// <summary>
    /// Calculates effective grid width based on current rotation.
    /// </summary>
    /// <returns>Width in cell units.</returns>
    public int GetWidth() => RotationIndex % 2 == 0 ? Data.gridWidth : Data.gridHeight;

    /// <summary>
    /// Calculates effective grid height based on current rotation.
    /// </summary>
    /// <returns>Height in cell units.</returns>
    public int GetHeight() => RotationIndex % 2 == 0 ? Data.gridHeight : Data.gridWidth;

    /// <summary>
    /// Rotates the item clockwise in 90-degree increments, swapping grid footprint dimensions.
    /// </summary>
    public void Rotate()
    {
        // Cycles through 4 cardinal rotation states (0 through 3).
        // Odd indices swap width and height dimensions for placement collision checks.
        RotationIndex = (RotationIndex + 1) % 4;
    }
    #endregion
}