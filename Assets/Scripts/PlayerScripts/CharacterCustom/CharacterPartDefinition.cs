using UnityEngine;

[CreateAssetMenu(menuName = "Character/Part")]
public class CharacterPartDefinition : ScriptableObject
{
    public string id;

    [SerializeField]
    protected CharacterPartType type;

    public CharacterPartType Type => type;

    public Sprite southWest;
    public Sprite south;
    public Sprite southEast;
    public Sprite east;
    public Sprite northEast;
    public Sprite north;
    public Sprite northWest;
    public Sprite west;

    public Sprite portrait;

    public Sprite GetSprite(CharacterDirection direction)
    {
        return direction switch
        {
            CharacterDirection.South => south,
            CharacterDirection.SouthWest => southWest,
            CharacterDirection.West => west,
            CharacterDirection.NorthWest => northWest,
            CharacterDirection.North => north,
            CharacterDirection.NorthEast => northEast,
            CharacterDirection.East => east,
            CharacterDirection.SouthEast => southEast,

            _ => south
        };
    }
}