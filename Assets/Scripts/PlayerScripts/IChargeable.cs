public interface IChargeableWeapon
{
    /// <summary>Charge progress 0..1 while charging; 0 otherwise.</summary>
    float ChargePercent { get; }

    /// <summary>Updates the aim direction while the skill is charging.</summary>
    void UpdateSkillDirection(UnityEngine.Vector2 direction);

    /// <summary>Releases the charged skill. fullyCharged = released at max charge (extra cost paid).</summary>
    void ReleaseSkill(bool fullyCharged);
}