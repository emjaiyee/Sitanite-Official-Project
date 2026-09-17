using UnityEngine;

public interface IWeapon
{
    string WeaponId { get; }
    bool CanAttack { get; }
    bool CanUseSkill { get; }
    bool IsTargetingSkill { get; }

    void Attack(Vector2 direction);

    void UseSkill(Vector2 direction);
    void UpdateSkillTarget(Vector3 targetPosition);
    void ConfirmSkill();
}