using UnityEngine;

[CreateAssetMenu(menuName = "Enemies/Tutorial Slime Definition")]
public class TutorialSlimeDefinition : ScriptableObject
{
    [Min(0.1f)] public float framesPerSecond = 10f;
    public DirectionalSpriteAnimation idle =
        new DirectionalSpriteAnimation();
    public DirectionalSpriteAnimation walk =
        new DirectionalSpriteAnimation();
    public DirectionalSpriteAnimation attack =
        new DirectionalSpriteAnimation();
    public DirectionalSpriteAnimation dead =
        new DirectionalSpriteAnimation();

    public DirectionalSpriteAnimation GetAnimation(
        TutorialSlimeAnimationState state)
    {
        return state switch
        {
            TutorialSlimeAnimationState.Walk => walk,
            TutorialSlimeAnimationState.Attack => attack,
            TutorialSlimeAnimationState.Dead => dead,
            _ => idle
        };
    }

    public int FrameCount(
        TutorialSlimeAnimationState state,
        CharacterDirection direction)
    {
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

public enum TutorialSlimeAnimationState
{
    Idle,
    Walk,
    Attack,
    Dead
}
