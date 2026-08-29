using UnityEngine;

[CreateAssetMenu(fileName = "_ScriptableObject", menuName = "Projectiles/BaseData")]
public class Projectile_BaseData : ScriptableObject
{

    [Min(0)]
    [SerializeField] private int projectileDamage;
    
    
    [Min(0)]
    [SerializeField] private int projectilePierceCount;


    [Range(0f,5f)]
    [SerializeField] private float projectileSpeed;
    
    
    [Range(0f, 8f)]
    [SerializeField] private float projectileLifetime;



    public int ProjectileDamage => projectileDamage;

    public int ProjectilePierceCount => projectilePierceCount;

    public float ProjectileSpeed => projectileSpeed;

    public float ProjectileLifetime => projectileLifetime;







}
