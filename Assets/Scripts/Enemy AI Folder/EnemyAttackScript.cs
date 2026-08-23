using UnityEngine;

public class EnemyAttackScript : MonoBehaviour
{
    #region Variables Part
    [Header("Contact Damage")]
    [Min(0)][SerializeField] private int damage = 5;
    [Min(0.01f)][SerializeField] private float attackCooldown = 1.5f;
    [Min(0.01f)][SerializeField] private float contactRange = 0.7f;

    private PlayerStats player;
    private float nextAttackTime;
    #endregion

    /// <summary>Configures the damage, range, and cooldown for this spawned enemy.</summary>
    public void Configure(int amount, float range, float cooldown)
    {
        damage = Mathf.Max(0, amount);
        contactRange = Mathf.Max(0.01f, range);
        attackCooldown = Mathf.Max(0.01f, cooldown);
    }

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

        if (player == null || player.IsDead || damage <= 0)
            return;

        if (Time.time < nextAttackTime) return;

        nextAttackTime = Time.time + attackCooldown;
        player.TakeDamage(damage);
        Debug.Log($"[EnemyContactDamage] {name} hit player for {damage}. Player HP: {player.CurrentHealth}/{player.maxHealth}");
    }
}
