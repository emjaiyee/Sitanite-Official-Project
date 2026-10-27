using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class TutorialSlimeSpriteAnimator : MonoBehaviour
{
    [SerializeField] private TutorialSlimeDefinition definition;
    [SerializeField] private EnemyMelee enemyMelee;
    [SerializeField] private EnemyHealth enemyHealth;

    private SpriteRenderer spriteRenderer;
    private TutorialSlimeAnimationState state;
    private CharacterDirection direction = CharacterDirection.South;
    private float stateStartedAt;
    private Vector3 previousPosition;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (enemyMelee == null)
            enemyMelee = GetComponentInParent<EnemyMelee>();

        if (enemyHealth == null)
            enemyHealth = GetComponentInParent<EnemyHealth>();

        previousPosition = transform.position;
    }

    private void Update()
    {
        if (definition == null || spriteRenderer == null)
            return;

        TutorialSlimeAnimationState nextState = GetState();
        if (nextState != state)
        {
            state = nextState;
            stateStartedAt = Time.time;
        }

        UpdateDirection();

        DirectionalSpriteAnimation animation =
            definition.GetAnimation(state);
        if (animation == null)
            return;

        int frame = Mathf.FloorToInt(
            (Time.time - stateStartedAt) * definition.framesPerSecond
        );

        if (state == TutorialSlimeAnimationState.Dead)
        {
            frame = Mathf.Min(
                frame,
                Mathf.Max(0, definition.FrameCount(state, direction) - 1)
            );
        }

        spriteRenderer.sprite = animation.GetFrame(direction, frame);
    }

    private void UpdateDirection()
    {
        Vector2 facing = Vector2.zero;

        if (
            enemyMelee != null &&
            enemyMelee.takingAim &&
            enemyMelee.Player != null
        )
        {
            facing =
                (Vector2)enemyMelee.Player.position -
                (Vector2)transform.position;
        }
        else
        {
            facing =
                (Vector2)(transform.position - previousPosition);
        }

        previousPosition = transform.position;

        if (facing.sqrMagnitude <= 0.0001f)
            return;

        float angle = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg;
        int index = (Mathf.RoundToInt(angle / 45f) + 8) % 8;

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

    private TutorialSlimeAnimationState GetState()
    {
        if (enemyHealth != null && enemyHealth.CurrentHealth <= 0)
            return TutorialSlimeAnimationState.Dead;

        if (enemyMelee == null)
            return TutorialSlimeAnimationState.Idle;

        if (enemyMelee.takingAim)
            return TutorialSlimeAnimationState.Attack;

        return enemyMelee.CurrentState == EnemyMelee.EnemyState.Chase ||
               enemyMelee.CurrentState == EnemyMelee.EnemyState.Search ||
               enemyMelee.CurrentState == EnemyMelee.EnemyState.Locate
            ? TutorialSlimeAnimationState.Walk
            : TutorialSlimeAnimationState.Idle;
    }
}
