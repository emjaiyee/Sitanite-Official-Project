using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Attach this to any UI element to disable combat input (PlayerAttack, PlayerSkill)
/// when the cursor hovers over it. Useful for preventing accidental attacks while
/// interacting with UI elements.
/// </summary>
public class DisableCombatOnUIHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Player Reference")]
    [Tooltip("Leave empty to auto-find via Player.Instance")]
    [SerializeField] private PlayerAttack playerAttack;
    [SerializeField] private PlayerSkill playerSkill;

    [Header("Settings")]
    [SerializeField] private bool disableAttack = true;
    [SerializeField] private bool disableSkill = true;

    private static int hoverCount = 0;

    /// <summary>
    /// Returns true if the cursor is currently hovering over any UI element
    /// with this component attached.
    /// </summary>
    public static bool IsHoveringUI => hoverCount > 0;

    private void Awake()
    {
        // Try to find player components if not assigned
        if ((playerAttack == null && disableAttack) || 
            (playerSkill == null && disableSkill))
        {
            if (Player.Instance != null)
            {
                if (playerAttack == null && disableAttack)
                    playerAttack = Player.Instance.GetComponent<PlayerAttack>();

                if (playerSkill == null && disableSkill)
                    playerSkill = Player.Instance.GetComponent<PlayerSkill>();
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hoverCount++;
        UpdateCombatState();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hoverCount--;
        if (hoverCount < 0)
            hoverCount = 0;

        UpdateCombatState();
    }

    private void OnDisable()
    {
        // If this UI element is disabled while being hovered, decrement the count
        if (hoverCount > 0)
        {
            hoverCount--;
            UpdateCombatState();
        }
    }

    private void OnDestroy()
    {
        // Clean up hover count if destroyed while hovered
        if (hoverCount > 0)
        {
            hoverCount--;
            UpdateCombatState();
        }
    }

    private void UpdateCombatState()
    {
        bool shouldDisable = hoverCount > 0;

        if (disableAttack && playerAttack != null)
        {
            playerAttack.enabled = !shouldDisable;
        }

        if (disableSkill && playerSkill != null)
        {
            playerSkill.enabled = !shouldDisable;
        }
    }
}
