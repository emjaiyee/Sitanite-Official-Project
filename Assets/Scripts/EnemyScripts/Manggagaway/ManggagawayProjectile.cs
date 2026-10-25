using UnityEngine;

public class ManggagawayProjectile : MonoBehaviour
{
    private Transform target;
    private BossBalance.HostileDamage damage;
    private float speed;
    private float expiresAt;
    private float radius;

    public void Initialize(Transform target, BossBalance.HostileDamage damage, float speed, float lifetime, float radius)
    {
        this.target = target;
        this.damage = damage;
        this.speed = speed;
        this.radius = radius;
        expiresAt = Time.time + lifetime;
    }

    private void Update()
    {
        if (damage == null)
            return;
        if (Time.time >= expiresAt || target == null)
        {
            Destroy(gameObject);
            return;
        }
        Vector2 direction = ((Vector2)target.position - (Vector2)transform.position).normalized;
        float distance = speed * Time.deltaTime;
        RaycastHit2D[] hits = Physics2D.CircleCastAll(transform.position, radius, direction, distance, damage.Layers);
        foreach (RaycastHit2D hit in hits)
        {
            PlayerStats player = damage.Resolve(hit.collider);
            if (player == null)
                continue;
            damage.Hit(player);
            Destroy(gameObject);
            return;
        }
        transform.position += (Vector3)(direction * distance);
        transform.right = direction;
    }
}
