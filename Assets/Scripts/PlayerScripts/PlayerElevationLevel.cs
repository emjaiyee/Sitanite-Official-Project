using System;
using UnityEngine;

public class PlayerElevationLevel : MonoBehaviour
{
    public static PlayerElevationLevel Instance { get; private set; }

    [SerializeField] private int currentLevel = 0;

    public int CurrentLevel => currentLevel;

    public static event Action<int> OnElevationChanged;

    public static bool CanAffectTarget(Transform target)
    {
        if (target == null || Instance == null)
            return true;

        EnemyElevationLevel enemyElevation =
            target.GetComponentInParent<EnemyElevationLevel>();

        if (enemyElevation == null)
            return true;

        return Mathf.Abs(Instance.CurrentLevel - enemyElevation.CurrentLevel) <= 1;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple PlayerElevationLevel instances found!");
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        OnElevationChanged?.Invoke(currentLevel);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void SetLevel(int level)
    {
        level = Mathf.Max(0, level);

        if (currentLevel == level)
            return;

        currentLevel = level;

        Debug.Log($"Current Elevation: {currentLevel}");

        OnElevationChanged?.Invoke(currentLevel);
    }

    public void IncreaseLevel()
    {
        SetLevel(currentLevel + 1);
    }

    public void DecreaseLevel()
    {
        SetLevel(currentLevel - 1);
    }
}