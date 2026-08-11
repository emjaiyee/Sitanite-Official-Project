using System;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class LevelGoalTrigger : MonoBehaviour
{
    [Tooltip("Set the level ID (any integer) in the Inspector")]
    [SerializeField] private int levelId = 1;

    [Tooltip("Assign the player's feet Transform (empty child at player's feet). If null, will search for a GameObject tagged 'Player' and use its transform.")]
    [SerializeField] private Transform playerFeet;

    private Collider2D goalCollider;
    private bool triggered;

    private void Reset()
    {
        // ensure collider is a trigger
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void Awake()
    {
        goalCollider = GetComponent<Collider2D>();
        if (playerFeet == null)
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null) playerFeet = player.transform;
        }
    }

    private void Update()
    {
        if (triggered) return;
        if (playerFeet == null) return;
        if (GameManager.Instance != null && GameManager.Instance.IsPlayerDead) return;

        // Check all colliders under the player's feet point
        var hits = Physics2D.OverlapPointAll(playerFeet.position);
        if (hits == null || hits.Length == 0) return;

        foreach (var hit in hits)
        {
            if (hit == goalCollider)
            {
                TriggerLevelClear();
                return;
            }
        }
    }

    private void TriggerLevelClear()
    {
        triggered = true;
        if (GameManager.Instance != null)
            GameManager.Instance.PlayerSteppedOnLevel(levelId);

        // optional: visual feedback or disable this trigger to avoid retrigger
        if (goalCollider != null) goalCollider.enabled = false;
    }

    // Optional editor gizmo to show feet-check point when selected
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (playerFeet != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(playerFeet.position, 0.05f);
        }
    }
#endif
}