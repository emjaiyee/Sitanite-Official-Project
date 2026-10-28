using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ManggagawaySpriteAnimator : MonoBehaviour
{
    [SerializeField] private ManggagawayDefinition definition;
    private SpriteRenderer spriteRenderer;
    private ManggagawayAnimationState state;
    private CharacterDirection direction = CharacterDirection.South;
    private float startedAt;

    public float Duration => definition == null ? 0f :
        definition.FrameCount(state, direction) / Mathf.Max(0.1f, definition.framesPerSecond);

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Play(ManggagawayAnimationState newState, Vector2 facing, bool secondForm)
    {
        if (facing.sqrMagnitude > 0.0001f)
        {
            float angle = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg;
            int index = Mathf.RoundToInt(angle / 45f);
            index = (index + 8) % 8;
            direction = index switch
            {
                0 => CharacterDirection.East,
                1 => CharacterDirection.NorthEast,
                2 => CharacterDirection.North,
                3 => CharacterDirection.NorthWest,
                4 => CharacterDirection.West,
                5 => CharacterDirection.SouthWest,
                6 => CharacterDirection.South,
                _ => CharacterDirection.SouthEast
            };
        }
        if (state != newState)
        {
            state = newState;
            startedAt = Time.time;
        }
        Render();
    }

    private void Update() => Render();

    private void Render()
    {
        if (definition == null || spriteRenderer == null)
            return;
        int count = definition.FrameCount(state, direction);
        int frame = Mathf.FloorToInt((Time.time - startedAt) * definition.framesPerSecond);
        if (state == ManggagawayAnimationState.Transformation || state == ManggagawayAnimationState.Dead)
            frame = Mathf.Min(frame, Mathf.Max(0, count - 1));
        Sprite sprite = state == ManggagawayAnimationState.Transformation
            ? definition.transformation?.GetFrame(frame)
            : definition.GetAnimation(state)?.GetFrame(direction, frame);
        if (sprite != null)
            spriteRenderer.sprite = sprite;
    }
}
