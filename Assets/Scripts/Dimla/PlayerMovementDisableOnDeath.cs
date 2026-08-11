using UnityEngine;

public class PlayerMovementDisableOnDeath : MonoBehaviour
{
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
        // disable movement component(s) as needed
        var mover = GetComponent<MonoBehaviour>(); // replace with your actual movement component type
        if (mover != null) mover.enabled = false;

        Debug.Log("[PlayerMovementDisableOnDeath] Player movement disabled due to death");
    }
}