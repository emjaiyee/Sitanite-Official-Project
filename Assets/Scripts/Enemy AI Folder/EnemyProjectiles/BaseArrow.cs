using UnityEngine;

public class BaseArrow : MonoBehaviour
{
    [SerializeField] protected Projectile_BaseData arrowData;
  
    [SerializeField] protected PlayerStats playerStats;
    [SerializeField] protected Transform player;
    [SerializeField] protected Rigidbody2D rb;


    //=========================================================
    // Get Reference On Instantiate
    //=========================================================

    protected virtual void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        playerStats = player.GetComponent<PlayerStats>();
        rb = GetComponent<Rigidbody2D>();
    }


    //=========================================================
    // Call Funtions 
    //=========================================================

    protected virtual void Start()
    {
        ProjectileMovement();
        DestroyProjectileAfterLifetime();
    }


    //=========================================================
    // Funtions 
    //=========================================================

    protected virtual void ProjectileMovement() 
    {
        if (player == null && rb == null)
        {
            Debug.LogError($"[BaseArrow] {name}: Player not found in the scene.");
            return;
        }


        Vector2 targetPosition = player.position;

        Vector2 direction =
            (targetPosition - (Vector2)transform.position).normalized;

        rb.linearVelocity = direction * arrowData.ProjectileSpeed;

        float angle =
            Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        transform.rotation =
            Quaternion.Euler(0f, 0f, angle);
    }

    protected void DestroyProjectileAfterLifetime()
    {
        Destroy(gameObject, arrowData.ProjectileLifetime);
    }

    //=========================================================
    // Collision Detection 
    //=========================================================

    protected virtual void OnTriggerEnter2D(Collider2D body)
    {

        if (body.CompareTag("Player"))
        {
            playerStats.TakeDamage(arrowData.ProjectileDamage);
            Destroy(gameObject);
        }

        else if (body.CompareTag("Default"))
        {
            Destroy(gameObject);
        }

    }

}
