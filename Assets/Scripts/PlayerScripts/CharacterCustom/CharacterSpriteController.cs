using UnityEngine;

public class CharacterSpriteController : MonoBehaviour
{
    [SerializeField]
    private PlayerWASD playerWASD;

    [SerializeField]
    private CharacterRenderer characterRenderer;

    [Header("Sprite Animation")]
    [Min(0.01f)]
    [SerializeField] private float idleFramesPerSecond = 4f;
    [Min(0.01f)]
    [SerializeField] private float walkFramesPerSecond = 8f;
    [Min(0.01f)]
    [SerializeField] private float runningFramesPerSecond = 12f;
    [Min(0.01f)]
    [SerializeField] private float attackFramesPerSecond = 10f;
    [Min(0.01f)]
    [SerializeField] private float skillFramesPerSecond = 10f;
    [Min(0.01f)]
    [SerializeField] private float dashFramesPerSecond = 14f;
    [Min(0.01f)]
    [SerializeField] private float deathFramesPerSecond = 6f;
    [Min(0f)]
    [SerializeField] private float attackDuration = 0.4f;
    [Min(0f)]
    [SerializeField] private float skillDuration = 0.4f;
    [Min(0f)]
    [SerializeField] private float dashDuration = 0.2f;

    private CharacterAnimationState currentAnimationState;
    private float actionEndTime;
    private float stateStartTime;
    private bool actionActive;

    private void Awake()
    {
        if (playerWASD == null)
            playerWASD = GetComponent<PlayerWASD>();

        if (characterRenderer == null)
            characterRenderer = GetComponent<CharacterRenderer>();

        stateStartTime = Time.time;
    }

    private void Update()
    {
        if (playerWASD == null || characterRenderer == null)
            return;

        CharacterDirection currentDirection = playerWASD.GetCurrentDirection();
        characterRenderer.CurrentDirection = currentDirection;

        if (actionActive && Time.time >= actionEndTime)
            actionActive = false;

        CharacterAnimationState nextState = actionActive
            ? currentAnimationState
            : playerWASD.MoveDirection.sqrMagnitude > 0.0001f
                ? playerWASD.IsSprinting
                    ? CharacterAnimationState.Running
                    : CharacterAnimationState.Walk
                : CharacterAnimationState.Idle;

        if (nextState != currentAnimationState)
            SetState(nextState);

        int frame = Mathf.FloorToInt(
            (Time.time - stateStartTime) * GetFramesPerSecond()
        );

        characterRenderer.SetAnimationState(currentAnimationState, frame);
    }

    public void PlayAttack()
    {
        PlayAction(CharacterAnimationState.Attack, attackDuration);
    }

    public void PlaySkill()
    {
        PlayAction(CharacterAnimationState.Skill, skillDuration);
    }

    public void PlayDash()
    {
        PlayAction(CharacterAnimationState.Dash, dashDuration);
    }

    public void PlayDeath()
    {
        actionActive = true;
        actionEndTime = float.PositiveInfinity;
        SetState(CharacterAnimationState.Death);
    }

    private void PlayAction(
        CharacterAnimationState animationState,
        float duration)
    {
        actionActive = true;
        actionEndTime = Time.time + duration;
        SetState(animationState);
    }

    private void SetState(CharacterAnimationState animationState)
    {
        if (currentAnimationState == animationState)
            return;

        currentAnimationState = animationState;
        stateStartTime = Time.time;
    }

    private float GetFramesPerSecond()
    {
        return currentAnimationState switch
        {
            CharacterAnimationState.Idle => idleFramesPerSecond,
            CharacterAnimationState.Walk => walkFramesPerSecond,
            CharacterAnimationState.Running => runningFramesPerSecond,
            CharacterAnimationState.Attack => attackFramesPerSecond,
            CharacterAnimationState.Skill => skillFramesPerSecond,
            CharacterAnimationState.Dash => dashFramesPerSecond,
            CharacterAnimationState.Death => deathFramesPerSecond,
            _ => walkFramesPerSecond
        };
    }
}