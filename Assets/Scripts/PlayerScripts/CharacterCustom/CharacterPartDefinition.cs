using UnityEngine;

[System.Serializable]
public class DirectionalSpriteAnimation
{
    public Sprite[] southWest;
    public Sprite[] south;
    public Sprite[] southEast;
    public Sprite[] east;
    public Sprite[] northEast;
    public Sprite[] north;
    public Sprite[] northWest;
    public Sprite[] west;

    public Sprite GetFrame(CharacterDirection direction, int frame)
    {
        Sprite[] frames = direction switch
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

        return frames != null && frames.Length > 0
            ? frames[frame % frames.Length]
            : null;
    }
}

public enum CharacterAnimationState
{
    Idle,
    Walk,
    Running,
    Attack,
    Skill,
    Dash,
    Death
}

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

    [Header("Animation Frames")]
    public DirectionalSpriteAnimation idleAnimation;
    public DirectionalSpriteAnimation walkAnimation;
    public DirectionalSpriteAnimation runningAnimation;
    public DirectionalSpriteAnimation attackAnimation;
    public DirectionalSpriteAnimation skillAnimation;
    public DirectionalSpriteAnimation dashAnimation;
    public DirectionalSpriteAnimation deathAnimation;

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

    public Sprite GetSprite(
        CharacterDirection direction,
        CharacterAnimationState animationState,
        int frame)
    {
        DirectionalSpriteAnimation animation = animationState switch
        {
            CharacterAnimationState.Idle => idleAnimation,
            CharacterAnimationState.Walk => walkAnimation,
            CharacterAnimationState.Running => runningAnimation,
            CharacterAnimationState.Attack => attackAnimation,
            CharacterAnimationState.Skill => skillAnimation,
            CharacterAnimationState.Dash => dashAnimation,
            CharacterAnimationState.Death => deathAnimation,
            _ => null
        };

        Sprite sprite = animation == null
            ? null
            : animation.GetFrame(direction, frame);

        return sprite != null ? sprite : GetSprite(direction);
    }
}