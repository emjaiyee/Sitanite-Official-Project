using UnityEngine;

public interface IDamageable
{
    /// <param name="damageSource">World position the damage came from, if known.</param>
    void TakeDamage(int amount, DamageType damageType = DamageType.Slash, Vector3? damageSource = null);
}
 