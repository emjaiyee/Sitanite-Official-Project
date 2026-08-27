using UnityEngine;

public interface IProjectile
{
    void Initialize(
        int damage,
        DamageType damageType,
        float speed,
        float maxDistance,
        bool homing,
        LayerMask hittableLayers);

    void InitializeSkill(
        int primaryDamage,
        DamageType primaryDamageType,
        int secondaryDamage,
        DamageType secondaryDamageType,
        int tertiaryDamage,
        DamageType tertiaryDamageType,
        float speed,
        float maxDistance,
        bool homing,
        LayerMask hittableLayers);
}