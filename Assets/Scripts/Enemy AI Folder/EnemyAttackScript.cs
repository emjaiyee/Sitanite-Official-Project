using UnityEngine;

public class EnemyAttackScript : MonoBehaviour
{
    #region Variables Part
    [SerializeField] private EnemyStats_Data enemyStats;   
 
    private PlayerStats player;

    #endregion


    private void Awake()
    {
        player = FindFirstObjectByType<PlayerStats>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if(other.CompareTag("Player"))
        TryDamagePlayer(other);
    }

    private void TryDamagePlayer(Collider2D other)
    {
        if (player == null)
            player = other.GetComponentInParent<PlayerStats>();

        if (player == null || player.IsDead || enemyStats.EnemyMaxAttack <= 0)
            return;

        player.TakeDamage(enemyStats.EnemyMaxAttack);
        Debug.Log($"[EnemyContactDamage] {name} hit player for {enemyStats.EnemyMaxAttack}. Player HP: {player.CurrentHealth}/{player.maxHealth}");
    }
}
