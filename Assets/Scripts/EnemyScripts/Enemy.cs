using UnityEngine;

public enum EnemyType
{
    Enemy,
    Boss
}

[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyHealth))]
public class Enemy : MonoBehaviour
{
    [Header("Type")]
    [SerializeField] private EnemyType enemyType = EnemyType.Enemy;

    [Header("Loot")]
    [SerializeField] private Transform lootSpawnPoint;

    private EnemyHealth enemyHealth;
    private bool hasDroppedLoot;

    private void Awake()
    {
        enemyHealth = GetComponent<EnemyHealth>();
    }

    private void OnEnable()
    {
        if (enemyHealth != null)
            enemyHealth.OnEnemyDied += HandleEnemyDied;
    }

    private void OnDisable()
    {
        if (enemyHealth != null)
            enemyHealth.OnEnemyDied -= HandleEnemyDied;
    }

    private void HandleEnemyDied(GameObject deadEnemy)
    {
        if (hasDroppedLoot)
            return;

        hasDroppedLoot = true;

        EnemyLootManager.Instance?.SpawnLoot(
            lootSpawnPoint != null ? lootSpawnPoint.position : transform.position,
            enemyType
        );
    }
}