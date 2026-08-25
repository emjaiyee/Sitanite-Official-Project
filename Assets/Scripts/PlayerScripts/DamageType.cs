[System.Flags]
public enum DamageType
{
    None = 0,
    Pierce = 1 << 0,
    Slash = 1 << 1,
    Blunt = 1 << 2,
    Burning = 1 << 3,
    Frost = 1 << 4,
    Poison = 1 << 5,
    Lightning = 1 << 6,
    Psychic = 1 << 7,
    Physical = 1 << 8,
    Stab = 1 << 9,
}