using UnityEngine;

/// <summary>
/// Base Game Manager - Handles core game state and events
/// This is a standalone base that can be extended without dependencies
/// </summary>
public class GameManager : MonoBehaviour
{
    // Singleton instance
    public static GameManager Instance { get; private set; }

    // Game State
    private bool isPlayerDead = false;
    private bool isLevelCleared = false;

    // Properties
    public bool IsPlayerDead => isPlayerDead;
    public bool IsLevelCleared => isLevelCleared;

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Debug.Log("[GameManager] Initialized");
    }

    private void Start()
    {
        Debug.Log("[GameManager] Game Started");
    }

    /// <summary>
    /// Called when the player dies
    /// </summary>
    public void PlayerDead()
    {
        if (isPlayerDead)
            return; // Prevent multiple calls

        isPlayerDead = true;
        Debug.Log("[GameManager] Player Dead!");

        // TODO: Add death logic here (UI, restart, etc.)
        // This will be called by Player script later
    }

    /// <summary>
    /// Called when the level is completed
    /// </summary>
    public void LevelCleared()
    {
        if (isLevelCleared)
            return; // Prevent multiple calls

        isLevelCleared = true;
        Debug.Log("[GameManager] Level Cleared!");

        // TODO: Add level clear logic here (UI, next level, rewards, etc.)
        // This will be called by Level script later
    }

    /// <summary>
    /// Reset game state for new level
    /// </summary>
    public void ResetLevel()
    {
        isPlayerDead = false;
        isLevelCleared = false;
        Debug.Log("[GameManager] Level Reset");
    }

    /// <summary>
    /// Get current game state
    /// </summary>
    public void LogGameState()
    {
        Debug.Log($"[GameManager State] Player Dead: {isPlayerDead} | Level Cleared: {isLevelCleared}");
    }
}