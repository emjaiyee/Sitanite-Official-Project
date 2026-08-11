using UnityEngine;

public class DisableMovementOnDeath : MonoBehaviour
{
    [Tooltip("Movement components to disable on death (drag PlayerWASD, other movement scripts)")]
    [SerializeField] private MonoBehaviour[] movementComponents;

    private void Start()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnPlayerDead += HandlePlayerDead;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnPlayerDead -= HandlePlayerDead;
    }

    private void HandlePlayerDead()
    {
        foreach (var comp in movementComponents)
        {
            if (comp != null) comp.enabled = false;
        }

        // Optionally, disable InputAction to stop reading input:
        // var playerWASD = GetComponent<PlayerWASD>();
        // if (playerWASD != null) playerWASD.enabled = false;

        Debug.Log("[DisableMovementOnDeath] Movement components disabled");
    }
}