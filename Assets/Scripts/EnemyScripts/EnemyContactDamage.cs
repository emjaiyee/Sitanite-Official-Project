using UnityEngine;

public class EnemyContactDamage : MonoBehaviour
{
    [Header("Contact Damage")]
    [Min(0)][SerializeField] private int damage = 5;
    [Min(0.01f)][SerializeField] private float attackCooldown = 1.5f;
    [Min(0.01f)][SerializeField] private float contactRange = 0.7f;

    private PlayerStats player;
    private float nextAttackTime;

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
        EnsureEnemyPhysics();
    }

    private void Update()
    {
        EnsureEnemyPhysics();
        if (player == null)
            player = FindFirstObjectByType<PlayerStats>();

        if (player != null)
            TryDamagePlayerInRange();
    }

    private void TryDamagePlayerInRange()
    {
        if (player == null) return;

        Collider2D[] contacts = Physics2D.OverlapCircleAll(transform.position, contactRange, Physics2D.AllLayers);
        foreach (Collider2D contact in contacts)
        {
            if (contact.GetComponentInParent<PlayerStats>() == player)
            {
                TryDamagePlayer(contact);
                return;
            }
        }
    }

    private void EnsureEnemyPhysics()
    {
        Rigidbody2D body = GetComponent<Rigidbody2D>();
        if (body == null)
            body = gameObject.AddComponent<Rigidbody2D>();

        body.bodyType = RigidbodyType2D.Dynamic;
        body.simulated = true;
        body.gravityScale = 0f;
        body.constraints = RigidbodyConstraints2D.FreezeAll;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);
        if (colliders.Length == 0)
        {
            BoxCollider2D fallback = gameObject.AddComponent<BoxCollider2D>();
            fallback.size = GetFallbackColliderSize();
            colliders = new Collider2D[] { fallback };
        }

        foreach (Collider2D collider in colliders)
        {
            collider.enabled = true;
            collider.isTrigger = false;
        }
    }

    private Vector2 GetFallbackColliderSize()
    {
        SpriteRenderer spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
        if (spriteRenderer != null && spriteRenderer.sprite != null)
            return spriteRenderer.sprite.bounds.size;

        return new Vector2(0.5f, 0.5f);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryDamagePlayer(collision.collider);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryDamagePlayer(other);
    }

    private void TryDamagePlayer(Collider2D other)
    {
        if (player == null)
            player = other.GetComponentInParent<PlayerStats>();

        if (player == null || player.IsDead || player.CurrentHealth <= 0 || damage <= 0) return;

        float rangeSqr = contactRange * contactRange;
        if (((Vector2)player.transform.position - (Vector2)transform.position).sqrMagnitude > rangeSqr)
            return;

        if (Time.time < nextAttackTime) return;

        nextAttackTime = Time.time + attackCooldown;
        player.TakeDamage(damage);
        Debug.Log($"[EnemyContactDamage] {name} hit player for {damage}. Player HP: {player.CurrentHealth}/{player.maxHealth}");
    }
}
