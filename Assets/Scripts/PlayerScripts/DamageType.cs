[System.Flags]
public enum DamageType
{
    None = 0,
    Pierce = 1 << 0,
    Slash = 1 << 1,
    Blunt = 1 << 2,
    Frost = 1 << 4,
    Poison = 1 << 5,
    Lightning = 1 << 6,
    Psychic = 1 << 7,
    Physical = 1 << 8,
    Stab = 1 << 9,
    Necrosis = 1 << 10,
    Water = 1 << 11,
    Earth = 1 << 12,
    Fire = 1 << 13,
    Air = 1 << 14,
}