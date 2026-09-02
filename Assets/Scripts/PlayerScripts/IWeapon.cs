using UnityEngine;

public interface IWeapon
{
    string WeaponId { get; }
    bool CanAttack { get; }
    bool CanUseSkill { get; }

    void Attack(Vector2 direction);

    void UseSkill(Vector2 direction);
}