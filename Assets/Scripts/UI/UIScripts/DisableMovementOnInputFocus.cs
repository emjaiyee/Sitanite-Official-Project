using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

/// <summary>
/// Attach this to any InputField or TMP_InputField to automatically
/// disable PlayerWASD movement and PlayerDash when the field is focused.
/// </summary>
public class DisableMovementOnInputFocus : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    [Header("Player Reference")]
    [Tooltip("Leave empty to auto-find via Player.Instance")]
    [SerializeField] private PlayerWASD playerWASD;
    [SerializeField] private PlayerDash playerDash;

    [Header("Input Suppression")]
    [Tooltip("Stats keybind to disable while this field is focused.")]
    [SerializeField] private InputActionReference statsAction;
    [Tooltip("Inventory keybind to disable while this field is focused.")]
    [SerializeField] private InputActionReference inventoryAction;

    private void Awake()
    {
        // Try to find player components if not assigned
        if (playerWASD == null || playerDash == null)
        {
            if (Player.Instance != null)
            {
                if (playerWASD == null)
                    playerWASD = Player.Instance.GetComponent<PlayerWASD>();
                
                if (playerDash == null)
                    playerDash = Player.Instance.GetComponent<PlayerDash>();
            }
        }

        // Validate that we have an InputField component
        var legacyInputField = GetComponent<InputField>();
        var tmpInputField = GetComponent<TMP_InputField>();

        if (legacyInputField == null && tmpInputField == null)
        {
            Debug.LogError($"DisableMovementOnInputFocus on {gameObject.name} requires an InputField or TMP_InputField component!", this);
        }
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (playerWASD != null)
        {
            playerWASD.LockMovement();
            playerWASD.LockFacingDirection();
        }
        else
        {
            Debug.LogWarning($"PlayerWASD not found! Cannot disable movement for {gameObject.name}", this);
        }

        if (playerDash != null)
        {
            playerDash.LockDash();
        }

        SetMenuActionsEnabled(false);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        if (playerWASD != null)
        {
            playerWASD.UnlockMovement();
            playerWASD.UnlockFacingDirection();
        }

        if (playerDash != null)
        {
            playerDash.UnlockDash();
        }

        SetMenuActionsEnabled(true);
    }

    private void OnDisable()
    {
        // Make sure to unlock movement if the input field is disabled while focused
        if (playerWASD != null)
        {
            playerWASD.UnlockMovement();
            playerWASD.UnlockFacingDirection();
        }

        if (playerDash != null)
        {
            playerDash.UnlockDash();
        }

        SetMenuActionsEnabled(true);
    }

    private void SetMenuActionsEnabled(bool enabled)
    {
        if (statsAction != null)
        {
            if (enabled)
                statsAction.action.Enable();
            else
                statsAction.action.Disable();
        }

        if (inventoryAction != null)
        {
            if (enabled)
                inventoryAction.action.Enable();
            else
                inventoryAction.action.Disable();
        }
    }
}
