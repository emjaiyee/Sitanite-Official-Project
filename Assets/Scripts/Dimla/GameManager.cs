using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum LevelState { Playing, Cleared, Failed }

    [Header("Health")]
    [SerializeField] private int maxHealth = 10;
    [SerializeField] private bool enableDebugLogs = true;

    [Header("Poison")]
    [SerializeField] private float poisonTickInterval = 1f;
    [SerializeField] private int poisonDamagePerTick = 1;

    // Events
    public event Action OnPlayerDead;
    public event Action OnLevelCleared;            // legacy: level cleared occurred
    public event Action<int> OnLevelClearedWithId; // reports which level id was cleared
    public event Action OnLevelFailed;

    // State
    private int currentHealth;
    private bool isPlayerDead;
    private LevelState levelState = LevelState.Playing;
    private Coroutine poisonCoroutine;

    // Runtime cleared level tracking (unlimited)
    private HashSet<int> clearedLevels = new HashSet<int>();

    // Optional inspector-initialized cleared levels (useful for testing)
    [SerializeField] private List<int> initialClearedLevels = new List<int>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        currentHealth = maxHealth;

        // seed clearedLevels from inspector list if provided
        if (initialClearedLevels != null)
        {
            foreach (var id in initialClearedLevels)
                clearedLevels.Add(id);
        }

        if (enableDebugLogs) Debug.Log("[GameManager] Initialized");
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // Public accessors
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsPlayerDead => isPlayerDead;
    public LevelState CurrentLevelState => levelState;
    public IReadOnlyCollection<int> ClearedLevels => clearedLevels;

    // Damage / heal API
    public void ApplyDamage(int amount)
    {
        if (isPlayerDead) return;
        if (amount <= 0) return;

        currentHealth = Mathf.Max(0, currentHealth - amount);
        if (enableDebugLogs) Debug.Log($"[GameManager] Player took {amount} damage. Health: {currentHealth}/{maxHealth}");

        if (currentHealth == 0) PlayerDead();
    }

    public void Heal(int amount)
    {
        if (isPlayerDead) return;
        if (amount <= 0) return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        if (enableDebugLogs) Debug.Log($"[GameManager] Player healed {amount}. Health: {currentHealth}/{maxHealth}");
    }

    // Poison DoT
    public void StartPoison()
    {
        if (isPlayerDead) return;
        if (poisonCoroutine != null) return;
        poisonCoroutine = StartCoroutine(PoisonCoroutine());
        if (enableDebugLogs) Debug.Log("[GameManager] Poison started");
    }

    public void StopPoison()
    {
        if (poisonCoroutine == null) return;
        StopCoroutine(poisonCoroutine);
        poisonCoroutine = null;
        if (enableDebugLogs) Debug.Log("[GameManager] Poison stopped");
    }

    private IEnumerator PoisonCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(poisonTickInterval);
            ApplyDamage(poisonDamagePerTick);
            if (isPlayerDead)
            {
                StopPoison();
                yield break;
            }
        }
    }

    // Death handling
    public void PlayerDead()
    {
        if (isPlayerDead) return;

        isPlayerDead = true;
        SetLevelFailed();
        StopPoison();

        if (enableDebugLogs) Debug.Log("[GameManager] Player Dead!");
        OnPlayerDead?.Invoke();
    }

    // Level clearing (universal)
    // Call this from level triggers with the level's integer id.
    public void PlayerSteppedOnLevel(int levelId)
    {
        if (isPlayerDead) return;                // dead player can't clear
        if (levelState != LevelState.Playing) return; // only while playing

        if (clearedLevels.Contains(levelId))
        {
            if (enableDebugLogs) Debug.Log($"[GameManager] Level {levelId} already cleared");
            return;
        }

        clearedLevels.Add(levelId);

        if (enableDebugLogs) Debug.Log($"[GameManager] Level {levelId} is Cleared");
        // Notify listeners
        OnLevelCleared?.Invoke();
        OnLevelClearedWithId?.Invoke(levelId);

        // If you want levelState to reflect a cleared session-wide state change,
        // call LevelCleared() instead of just PlayerSteppedOnLevel.
        LevelCleared();
    }

    // Generic LevelCleared state update (keeps same behavior as before)
    public void LevelCleared()
    {
        if (levelState != LevelState.Playing) return;

        levelState = LevelState.Cleared;
        if (enableDebugLogs) Debug.Log("[GameManager] Level Cleared (global state)!");
        OnLevelCleared?.Invoke();
    }

    // Explicit failure
    public void SetLevelFailed()
    {
        if (levelState == LevelState.Failed) return;

        levelState = LevelState.Failed;
        if (enableDebugLogs) Debug.Log("[GameManager] Level Failed!");
        OnLevelFailed?.Invoke();
    }

    // Query whether a specific levelId has been cleared
    public bool IsLevelCleared(int levelId)
    {
        return clearedLevels.Contains(levelId);
    }

    // Reset state for a new run
    // clearAllClearedLevels: if true, removes all recorded cleared levels
    public void ResetLevel(bool clearAllClearedLevels = true)
    {
        isPlayerDead = false;
        levelState = LevelState.Playing;
        currentHealth = maxHealth;
        StopPoison();

        if (clearAllClearedLevels)
            clearedLevels.Clear();

        if (enableDebugLogs) Debug.Log("[GameManager] Level Reset");
    }

    // Debug helper
    public void LogGameState()
    {
        if (!enableDebugLogs) return;
        Debug.Log($"[GameManager State] Player Dead: {isPlayerDead} | LevelState: {levelState} | Health: {currentHealth}/{maxHealth} | ClearedLevels: {string.Join(",", clearedLevels)}");
    }
}