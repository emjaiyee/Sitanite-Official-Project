using Unity.VisualScripting.Dependencies.Sqlite;
using UnityEngine;
using UnityEngine.Rendering;

public class BaseAttack : MonoBehaviour
{
    [Header("Component Variables")]
    [SerializeField] private Collider2D damageCollider;
    [SerializeField] private SpriteRenderer attackSprite;

    [Header("Attack Variables")]
    [SerializeField] private float damageAmount;
    [SerializeField] private DamageType damageType;

    [Header("LifeTime Variables")]
    [SerializeField] private float lifeTime;
    [SerializeField] private float endTime = 1f;

    [Header("WindUp Variables")]
    [SerializeField] private float windUpTime;
    [Range(0f,1f)]
    [SerializeField] private float windUpEnd = 0.4f;
    [SerializeField] private bool windUp;

    private bool hasHitPlayer;
    private EnemyHealth attacker;

    public void Awake()
    {
        lifeTime = 0;
        windUp = true;

        damageCollider = GetComponent<Collider2D>();
        attackSprite = GetComponent<SpriteRenderer>();

        if (damageCollider != null) damageCollider.enabled = false;
        if (attackSprite != null) attackSprite.enabled = false;
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

        if (windUpTime >= windUpEnd)
        {
            windUp = false;
            damageCollider.enabled = true;
            attackSprite.enabled = true;
        }
        else { windUpTime += Time.deltaTime; }

        if (windUp)
            return;

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
