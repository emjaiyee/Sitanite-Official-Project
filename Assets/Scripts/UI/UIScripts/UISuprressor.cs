using UnityEngine;

/// <summary>
/// Marks a scene-local UI element that should be visually hidden
/// and stop receiving raycasts while the inventory is open.
///
/// Requires a CanvasGroup on the same GameObject.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class UISuppressor : MonoBehaviour
{
    #region Private Fields

    private CanvasGroup canvasGroup;

    private float originalAlpha;
    private bool originalInteractable;
    private bool originalBlocksRaycasts;

    #endregion

    #region Properties

    /// <summary>
    /// Gets the CanvasGroup used by this suppressor.
    /// </summary>
    public CanvasGroup CanvasGroup => canvasGroup;

    #endregion

    #region Lifecycle

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        // Capture the UI's normal state before PlayerInventory
        // ever attempts to suppress or restore it.
        SaveOriginalState();
    }

    #endregion

    #region State Management

    /// <summary>
    /// Saves the current CanvasGroup state as the normal state
    /// that should be restored when the inventory closes.
    /// </summary>
    private void SaveOriginalState()
    {
        if (canvasGroup == null)
            return;

        originalAlpha = canvasGroup.alpha;
        originalInteractable = canvasGroup.interactable;
        originalBlocksRaycasts = canvasGroup.blocksRaycasts;
    }

    #endregion

    #region Public API

    /// <summary>
    /// Hides this UI and disables its interaction while the inventory is open.
    /// </summary>
    public void Suppress()
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    /// <summary>
    /// Restores the CanvasGroup to the state it had when this
    /// UISuppressor was initialized.
    /// </summary>
    public void Restore()
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = originalAlpha;
        canvasGroup.interactable = originalInteractable;
        canvasGroup.blocksRaycasts = originalBlocksRaycasts;
    }

    #endregion
}