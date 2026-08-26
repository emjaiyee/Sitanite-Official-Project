public interface IWeapon
{
    string WeaponId { get; }

    void Attack();

    void UseSkill();
}