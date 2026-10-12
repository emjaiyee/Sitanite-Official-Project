using UnityEngine;

public interface IProjectileType
{
    void SetAttacker(EnemyHealth attacker);

    void Launch(
        Vector3 direction,
        int damage,
        DamageType damageType,
        float speed,
        float lifetime);
}