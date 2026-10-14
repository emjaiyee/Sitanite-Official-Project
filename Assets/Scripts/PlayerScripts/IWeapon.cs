using UnityEngine;

public interface IWeapon
{
    string WeaponId { get; }
    bool CanAttack { get; }
    bool CanUseSkill { get; }
    bool IsTargetingSkill { get; }

    void Attack(Vector2 direction);
    void Attack(Vector2 direction, float damageMultiplier);
    void Attack(Vector2 direction, float damageMultiplier, bool isCounterAttack);
    void Attack(
        Vector2 direction,
        float damageMultiplier,
        bool isCounterAttack,
        bool isCriticalHit);
    void Attack(
        Vector2 direction,
        float damageMultiplier,
        bool isCounterAttack,
        bool isCriticalHit,
        GameObject criticalHitVisualPrefab);

    void UseSkill(Vector2 direction);
    void UpdateSkillTarget(Vector3 targetPosition);
    void ConfirmSkill();
    void CancelSkill();
}