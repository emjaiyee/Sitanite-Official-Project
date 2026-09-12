using UnityEngine;
using UnityEngine.Serialization;

[System.Serializable]
public class DirectionalSpriteAnimation
{
    [Tooltip("Unsliced sprite sheet that will be sliced into a grid by the custom inspector.")]
    public Texture2D spriteSheet;

    [Min(1)]
    [Tooltip("Size of each animation frame in pixels.")]
    public Vector2Int cellSize = new Vector2Int(32, 32);

    [Min(1)]
    [Tooltip("The number of consecutive source frames assigned to each direction.")]
    public int framesPerDirection = 1;

    [HideInInspector]
    public Sprite[] sourceFrames;

    public Sprite[] southWest;
    public Sprite[] south;
    public Sprite[] southEast;
    public Sprite[] east;
    public Sprite[] northEast;
    public Sprite[] north;
    public Sprite[] northWest;
    public Sprite[] west;

    public bool TryAssignSourceFrames(out string error)
    {
        const int directionCount = 8;

        if (framesPerDirection < 1)
        {
            error = "Frames Per Direction must be at least 1.";
            return false;
        }

        int expectedFrameCount = directionCount * framesPerDirection;
        if (sourceFrames == null || sourceFrames.Length != expectedFrameCount)
        {
            int actualFrameCount = sourceFrames == null ? 0 : sourceFrames.Length;
            error = $"Expected {expectedFrameCount} source frames, but found {actualFrameCount}.";
            return false;
        }

        southWest = GetFrames(0);
        south = GetFrames(1);
        southEast = GetFrames(2);
        east = GetFrames(3);
        northEast = GetFrames(4);
        north = GetFrames(5);
        northWest = GetFrames(6);
        west = GetFrames(7);

        error = null;
        return true;
    }

    private Sprite[] GetFrames(int directionIndex)
    {
        Sprite[] frames = new Sprite[framesPerDirection];
        System.Array.Copy(
            sourceFrames,
            directionIndex * framesPerDirection,
            frames,
            0,
            framesPerDirection
        );
        return frames;
    }

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
    Melee,
    Cast,
    Ranged,
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
    [FormerlySerializedAs("attackAnimation")]
    public DirectionalSpriteAnimation meleeAnimation;
    [FormerlySerializedAs("skillAnimation")]
    public DirectionalSpriteAnimation castAnimation;
    public DirectionalSpriteAnimation rangedAnimation;
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
            CharacterAnimationState.Melee => meleeAnimation,
            CharacterAnimationState.Cast => castAnimation,
            CharacterAnimationState.Ranged => rangedAnimation,
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