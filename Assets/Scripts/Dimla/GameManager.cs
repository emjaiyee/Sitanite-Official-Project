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

    [Header("Poison")]
    [SerializeField] private float poisonTickInterval = 1f;
    [SerializeField] private int poisonDamagePerTick = 1;

    [Header("Logging")]
    [Tooltip("If true, only 'Level X is Cleared' logs will be printed. Turn off for more verbose logs.")]
    [SerializeField] private bool logOnlyLevelCleared = true;

    // Events
    public event Action OnPlayerDead;
    public event Action OnLevelCleared;
    public event Action<int> OnLevelClearedWithId;
    public event Action OnLevelFailed;

    // State
    private int currentHealth;
    private bool isPlayerDead;
    private LevelState levelState = LevelState.Playing;
    private Coroutine poisonCoroutine;

    private HashSet<int> clearedLevels = new HashSet<int>();
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

        if (initialClearedLevels != null)
        {
            foreach (var id in initialClearedLevels)
                clearedLevels.Add(id);
        }

        if (!logOnlyLevelCleared) Debug.Log("[GameManager] Initialized");
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
        if (!logOnlyLevelCleared) Debug.Log($"[GameManager] Player took {amount} damage. Health: {currentHealth}/{maxHealth}");

        if (currentHealth == 0) PlayerDead();
    }

    public void Heal(int amount)
    {
        if (isPlayerDead) return;
        if (amount <= 0) return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        if (!logOnlyLevelCleared) Debug.Log($"[GameManager] Player healed {amount}. Health: {currentHealth}/{maxHealth}");
    }

    // Poison DoT
    public void StartPoison()
    {
        if (isPlayerDead) return;
        if (poisonCoroutine != null) return;
        poisonCoroutine = StartCoroutine(PoisonCoroutine());
        if (!logOnlyLevelCleared) Debug.Log("[GameManager] Poison started");
    }

    public void StopPoison()
    {
        if (poisonCoroutine == null) return;
        StopCoroutine(poisonCoroutine);
        poisonCoroutine = null;
        if (!logOnlyLevelCleared) Debug.Log("[GameManager] Poison stopped");
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

        if (!logOnlyLevelCleared) Debug.Log("[GameManager] Player Dead!");
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
            if (!logOnlyLevelCleared) Debug.Log($"[GameManager] Level {levelId} already cleared");
            return;
        }

        clearedLevels.Add(levelId);

        // Always print the level-cleared message (user requested only this)
        Debug.Log($"[GameManager] Level {levelId} is Cleared");
        // Notify listeners
        OnLevelCleared?.Invoke();
        OnLevelClearedWithId?.Invoke(levelId);

        // Optionally set global cleared state
        LevelCleared();
    }

    // Generic LevelCleared state update (keeps same behavior as before)
    public void LevelCleared()
    {
        if (levelState != LevelState.Playing) return;

        levelState = LevelState.Cleared;
        if (!logOnlyLevelCleared) Debug.Log("[GameManager] Level Cleared (global state)!");
        OnLevelCleared?.Invoke();
    }

    // Explicit failure
    public void SetLevelFailed()
    {
        if (levelState == LevelState.Failed) return;

        levelState = LevelState.Failed;
        if (!logOnlyLevelCleared) Debug.Log("[GameManager] Level Failed!");
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

        if (!logOnlyLevelCleared) Debug.Log("[GameManager] Level Reset");
    }

    // Debug helper
    public void LogGameState()
    {
        if (logOnlyLevelCleared) return;
        Debug.Log($"[GameManager State] Player Dead: {isPlayerDead} | LevelState: {levelState} | Health: {currentHealth}/{maxHealth} | ClearedLevels: {string.Join(",", clearedLevels)}");
    }
}
