using UnityEngine;
using UnityEngine.Rendering;

public class BaseAttack : MonoBehaviour
{

    [SerializeField] private float damageAmount;
    [SerializeField] private DamageType damageType;
    [SerializeField] private float lifeTime;
    [SerializeField] private float endTime = 1f;

    private bool hasHitPlayer;
    private EnemyHealth attacker;

    public void Awake()
    {
        lifeTime = 0;
    }
    public void SetDamage(
        DamageType damageType,
        float damageAmount,
        EnemyHealth attacker)
    { 
        this.damageType = damageType;
        this.damageAmount = damageAmount;
        this.attacker = attacker;
    }

    public void Update()
    {

        if (lifeTime >= endTime)
        {
            Destroy(gameObject);
        }
        else { lifeTime += Time.deltaTime; }
    }

    private void OnTriggerEnter2D(Collider2D box)
    {

        if (box == null) return;

        if (!box.CompareTag("Player"))
            return;

        PlayerStats playerStats =
               box.GetComponentInParent<PlayerStats>();

        if (playerStats == null)
        {
            playerStats =
                box.GetComponentInChildren<PlayerStats>();
        }

        if (hasHitPlayer)
                return;

        hasHitPlayer = true;

        playerStats.TakeDamage(
           damageAmount,
           damageType,
           attacker != null ? attacker.transform.position : (Vector3?)transform.position,
           attacker
       );

        
    }


}
