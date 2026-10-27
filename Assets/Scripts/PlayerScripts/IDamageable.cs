using UnityEngine;

public interface IDamageable
{
    /// <param name="damageSource">World position the damage came from, if known.</param>
    void TakeDamage(int amount, DamageType damageType = DamageType.Slash, Vector3? damageSource = null);
}

public static class PlayerDamage
{
    public static void Deal(IDamageable target, int amount, DamageType type, Vector3? source, WeaponAttackType attackType)
    {
        if (target is EnemyHealth enemy)
        {
            PlayerSkill playerSkill = Player.Instance != null
                ? Player.Instance.GetComponent<PlayerSkill>()
                : null;

            enemy.TakePlayerDamage(
                amount,
                type,
                source,
                attackType,
                playerSkill != null && playerSkill.IsSkillActive
            );
        }
        else
            target.TakeDamage(amount, type, source);
    }
}
 