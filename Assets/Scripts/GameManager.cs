using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }


    public enum FloorState
    {
        Playing,
        Cleared,
        Failed
    }


    [Header("Health")]
    [SerializeField] private int maxHealth = 10;


    [Header("Poison")]
    [SerializeField] private float poisonTickInterval = 1f;
    [SerializeField] private int poisonDamagePerTick = 1;


    [Header("Logging")]
    [Tooltip(
        "If true, only floor-clear messages will be printed. " +
        "Turn off for more verbose logs."
    )]
    [SerializeField] private bool logOnlyFloorCleared = true;


    // -------------------------------------------------
    // FLOOR EVENTS
    // -------------------------------------------------

    public event Action OnPlayerDead;

    public event Action OnFloorCleared;

    public event Action<int> OnFloorClearedWithId;

    public event Action OnFloorFailed;


    // -------------------------------------------------
    // STATE
    // -------------------------------------------------

    private int currentHealth;

    private bool isPlayerDead;

    private FloorState floorState =
        FloorState.Playing;

    private Coroutine poisonCoroutine;


    private readonly HashSet<int> clearedFloors =
        new HashSet<int>();


    [SerializeField]
    private List<int> initialClearedFloors =
        new List<int>();


    // -------------------------------------------------
    // UNITY
    // -------------------------------------------------

    private void Awake()
    {
        if (
            Instance != null &&
            Instance != this
        )
        {
            Destroy(gameObject);
            return;
        }


        Instance = this;

        DontDestroyOnLoad(gameObject);


        currentHealth = maxHealth;


        if (initialClearedFloors != null)
        {
            foreach (
                int id
                in initialClearedFloors
            )
            {
                clearedFloors.Add(id);
            }
        }


        if (!logOnlyFloorCleared)
        {
            Debug.Log(
                "[GameManager] Initialized."
            );
        }
    }


    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }


    // -------------------------------------------------
    // PUBLIC ACCESSORS
    // -------------------------------------------------

    public int CurrentHealth =>
        currentHealth;

    public int MaxHealth =>
        maxHealth;

    public bool IsPlayerDead =>
        isPlayerDead;

    public FloorState CurrentFloorState =>
        floorState;

    public IReadOnlyCollection<int> ClearedFloors =>
        clearedFloors;


    // -------------------------------------------------
    // DAMAGE / HEAL
    // -------------------------------------------------

    public void ApplyDamage(int amount)
    {
        if (isPlayerDead)
            return;

        if (amount <= 0)
            return;


        currentHealth =
            Mathf.Max(
                0,
                currentHealth - amount
            );


        if (!logOnlyFloorCleared)
        {
            Debug.Log(
                $"[GameManager] Player took " +
                $"{amount} damage. " +
                $"Health: {currentHealth}/" +
                $"{maxHealth}"
            );
        }


        if (currentHealth == 0)
        {
            PlayerDead();
        }
    }


    public void Heal(int amount)
    {
        if (isPlayerDead)
            return;

        if (amount <= 0)
            return;


        currentHealth =
            Mathf.Min(
                maxHealth,
                currentHealth + amount
            );


        if (!logOnlyFloorCleared)
        {
            Debug.Log(
                $"[GameManager] Player healed " +
                $"{amount}. " +
                $"Health: {currentHealth}/" +
                $"{maxHealth}"
            );
        }
    }


    // -------------------------------------------------
    // POISON
    // -------------------------------------------------

    public void StartPoison()
    {
        if (isPlayerDead)
            return;

        if (poisonCoroutine != null)
            return;


        poisonCoroutine =
            StartCoroutine(
                PoisonCoroutine()
            );


        if (!logOnlyFloorCleared)
        {
            Debug.Log(
                "[GameManager] Poison started."
            );
        }
    }


    public void StopPoison()
    {
        if (poisonCoroutine == null)
            return;


        StopCoroutine(
            poisonCoroutine
        );

        poisonCoroutine = null;


        if (!logOnlyFloorCleared)
        {
            Debug.Log(
                "[GameManager] Poison stopped."
            );
        }
    }


    private IEnumerator PoisonCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(
                poisonTickInterval
            );


            ApplyDamage(
                poisonDamagePerTick
            );


            if (isPlayerDead)
            {
                StopPoison();
                yield break;
            }
        }
    }


    // -------------------------------------------------
    // PLAYER DEATH
    // -------------------------------------------------

    public void PlayerDead()
    {
        if (isPlayerDead)
            return;


        isPlayerDead = true;


        SetFloorFailed();


        StopPoison();


        if (!logOnlyFloorCleared)
        {
            Debug.Log(
                "[GameManager] Player Dead!"
            );
        }


        OnPlayerDead?.Invoke();
    }


    // -------------------------------------------------
    // FLOOR CLEAR
    // -------------------------------------------------

    /// <summary>
    /// Marks a specific floor as cleared.
    ///
    /// This should be called by RoomManager once every
    /// room on the current floor has been cleared.
    /// </summary>
    public void FloorCleared(int floorId)
    {
        if (isPlayerDead)
            return;


        if (
            floorState !=
            FloorState.Playing
        )
        {
            return;
        }


        if (clearedFloors.Contains(floorId))
        {
            if (!logOnlyFloorCleared)
            {
                Debug.Log(
                    $"[GameManager] Floor " +
                    $"{floorId} already cleared."
                );
            }

            return;
        }


        clearedFloors.Add(floorId);


        // This is intentionally always printed.
        Debug.Log(
            $"[GameManager] Floor " +
            $"{floorId} is Cleared."
        );


        OnFloorCleared?.Invoke();

        OnFloorClearedWithId?.Invoke(
            floorId
        );


        // Update global floor state.
        SetFloorCleared();
    }


    /// <summary>
    /// Updates the global state to Cleared.
    ///
    /// RoomManager should normally call FloorCleared(int)
    /// rather than calling this directly.
    /// </summary>
    public void SetFloorCleared()
    {
        if (
            floorState !=
            FloorState.Playing
        )
        {
            return;
        }


        floorState =
            FloorState.Cleared;


        if (!logOnlyFloorCleared)
        {
            Debug.Log(
                "[GameManager] Floor Cleared!"
            );
        }
    }


    // -------------------------------------------------
    // FLOOR FAILURE
    // -------------------------------------------------

    public void SetFloorFailed()
    {
        if (
            floorState ==
            FloorState.Failed
        )
        {
            return;
        }


        floorState =
            FloorState.Failed;


        if (!logOnlyFloorCleared)
        {
            Debug.Log(
                "[GameManager] Floor Failed!"
            );
        }


        OnFloorFailed?.Invoke();
    }


    // -------------------------------------------------
    // FLOOR QUERIES
    // -------------------------------------------------

    public bool IsFloorCleared(int floorId)
    {
        return clearedFloors.Contains(
            floorId
        );
    }


    // -------------------------------------------------
    // RESET
    // -------------------------------------------------

    public void ResetFloor(
        bool clearAllClearedFloors = true)
    {
        isPlayerDead = false;


        floorState =
            FloorState.Playing;


        currentHealth =
            maxHealth;


        StopPoison();


        if (clearAllClearedFloors)
        {
            clearedFloors.Clear();
        }


        if (!logOnlyFloorCleared)
        {
            Debug.Log(
                "[GameManager] Floor Reset."
            );
        }
    }


    // -------------------------------------------------
    // DEBUG
    // -------------------------------------------------

    public void LogGameState()
    {
        if (logOnlyFloorCleared)
            return;

        Debug.Log(
            $"[GameManager State] Player Dead: {isPlayerDead} | " +
            $"FloorState: {floorState} | " +
            $"Health: {currentHealth}/{maxHealth} | " +
            $"ClearedFloors: {string.Join(",", clearedFloors)}"
        );
    }


    // =====================================================================
    // Class Selection support
    // =====================================================================

    [Header("Class Selection (added)")]
    [SerializeField] private PlayerClass selectedClass = PlayerClass.Warrior;

    public PlayerClass SelectedClass => selectedClass;

    public void SetClass(PlayerClass playerClass)
    {
        selectedClass = playerClass;

        if (!logOnlyFloorCleared)
        {
            Debug.Log(
                $"[GameManager] Class selected: {playerClass}"
            );
        }
    }
}