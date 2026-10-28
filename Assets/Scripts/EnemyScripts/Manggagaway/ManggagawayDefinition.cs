using UnityEngine;
using UnityEngine.Serialization;

public enum ManggagawayAnimationState
{
    Idle, Walk, Attack, Transformation, SecondFormIdle, SecondFormWalk, SecondFormAttack, Dead
}

[System.Serializable]
public class SpriteAnimation
{
    [Tooltip("Unsliced sprite sheet that will be sliced into a grid by the custom inspector.")]
    public Texture2D spriteSheet;

    [Min(1)]
    [Tooltip("Size of each animation frame in pixels.")]
    public Vector2Int cellSize = new Vector2Int(32, 32);

    [Tooltip("Normalized pivot used when slicing this sheet. (0, 0) is bottom-left; (1, 1) is top-right.")]
    public Vector2 pivot = new Vector2(0.5f, 0.5f);

    [Min(1)]
    [Tooltip("The total number of frames in the animation.")]
    [FormerlySerializedAs("framesPerDirection")]
    public int frameCount = 1;

    [HideInInspector]
    [FormerlySerializedAs("sourceFrames")]
    public Sprite[] frames;

    public Sprite GetFrame(int frame)
    {
        return frames != null && frames.Length > 0
            ? frames[frame % frames.Length]
            : null;
    }
}

[CreateAssetMenu(menuName = "Enemies/Manggagaway Definition")]
public class ManggagawayDefinition : ScriptableObject
{
    [Min(0.1f)] public float framesPerSecond = 10f;
    public DirectionalSpriteAnimation idle = new DirectionalSpriteAnimation();
    public DirectionalSpriteAnimation walk = new DirectionalSpriteAnimation();
    public DirectionalSpriteAnimation attack = new DirectionalSpriteAnimation();
    public SpriteAnimation transformation = new SpriteAnimation();
    public DirectionalSpriteAnimation secondFormIdle = new DirectionalSpriteAnimation();
    public DirectionalSpriteAnimation secondFormWalk = new DirectionalSpriteAnimation();
    public DirectionalSpriteAnimation secondFormAttack = new DirectionalSpriteAnimation();
    public DirectionalSpriteAnimation dead = new DirectionalSpriteAnimation();

    public DirectionalSpriteAnimation GetAnimation(ManggagawayAnimationState state)
    {
        return state switch
        {
            ManggagawayAnimationState.Walk => walk,
            ManggagawayAnimationState.Attack => attack,
            ManggagawayAnimationState.SecondFormIdle => secondFormIdle,
            ManggagawayAnimationState.SecondFormWalk => secondFormWalk,
            ManggagawayAnimationState.SecondFormAttack => secondFormAttack,
            ManggagawayAnimationState.Dead => dead,
            _ => idle
        };
    }

    public static CharacterDirection ResolveDirection(CharacterDirection direction, bool secondForm)
    {
        return direction;
    }

    public int FrameCount(ManggagawayAnimationState state, CharacterDirection direction)
    {
        if (state == ManggagawayAnimationState.Transformation)
            return transformation == null || transformation.frames == null ? 0 : transformation.frames.Length;

        DirectionalSpriteAnimation animation = GetAnimation(state);
        if (animation == null)
            return 0;
        Sprite[] frames = direction switch
        {
            CharacterDirection.SouthWest => animation.southWest,
            CharacterDirection.South => animation.south,
            CharacterDirection.SouthEast => animation.southEast,
            CharacterDirection.East => animation.east,
            CharacterDirection.NorthEast => animation.northEast,
            CharacterDirection.North => animation.north,
            CharacterDirection.NorthWest => animation.northWest,
            _ => animation.west
        };
        return frames == null ? 0 : frames.Length;
    }
}
