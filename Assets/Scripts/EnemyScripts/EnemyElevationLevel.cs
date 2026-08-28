using System;
using UnityEngine;

public class EnemyElevationLevel : MonoBehaviour
{
    [Header("Elevation")]
    [SerializeField] private int currentLevel = 0;

    public int CurrentLevel =>
        currentLevel;


    public event Action<int> OnElevationChanged;


    // =========================================================
    // UNITY
    // =========================================================

    private void Start()
    {
        OnElevationChanged?.Invoke(currentLevel);
    }


    // =========================================================
    // SET LEVEL
    // =========================================================

    public void SetLevel(int level)
    {
        level = Mathf.Max(0, level);


        if (currentLevel == level)
            return;


        currentLevel = level;


        Debug.Log(
            $"[EnemyElevation] {name}: " +
            $"Elevation changed to {currentLevel}"
        );


        OnElevationChanged?.Invoke(
            currentLevel
        );
    }


    // =========================================================
    // LEVEL ADJUSTMENT
    // =========================================================

    public void IncreaseLevel()
    {
        SetLevel(
            currentLevel + 1
        );
    }


    public void DecreaseLevel()
    {
        SetLevel(
            currentLevel - 1
        );
    }
}