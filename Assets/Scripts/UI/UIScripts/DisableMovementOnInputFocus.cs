using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Attach this to any InputField or TMP_InputField to automatically
/// disable PlayerWASD movement when the field is focused.
/// </summary>
public class DisableMovementOnInputFocus : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    [Header("Player Reference")]
    [Tooltip("Leave empty to auto-find via Player.Instance")]
    [SerializeField] private PlayerWASD playerWASD;

    private void Awake()
    {
        // Try to find PlayerWASD if not assigned
        if (playerWASD == null)
        {
            if (Player.Instance != null)
            {
                playerWASD = Player.Instance.GetComponent<PlayerWASD>();
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
        }
        else
        {
            Debug.LogWarning($"PlayerWASD not found! Cannot disable movement for {gameObject.name}", this);
        }
    }

    public void OnDeselect(BaseEventData eventData)
    {
        if (playerWASD != null)
        {
            playerWASD.UnlockMovement();
        }
    }

    private void OnDisable()
    {
        // Make sure to unlock movement if the input field is disabled while focused
        if (playerWASD != null)
        {
            playerWASD.UnlockMovement();
        }
    }
}
